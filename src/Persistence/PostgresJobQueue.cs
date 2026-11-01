using System.Data;
using Npgsql;
using wAInery.BuildingBlocks;

namespace wAInery.Persistence;

public sealed record JobRequest(WorkspaceId Workspace, string Kind, string ScopeKey, long DesiredRevision,
    DateTimeOffset NotBefore, int Priority = 0);
public sealed record JobLease(Guid Id, WorkspaceId Workspace, string Kind, string ScopeKey, long Revision,
    Guid Token, DateTimeOffset LeaseUntil, int Attempt);
public enum ProcessingFailure { HandlerFailure, InvalidPayload }

public sealed class PostgresJobQueue(NpgsqlDataSource dataSource, int maxAttempts = 5)
{
    private readonly NpgsqlDataSource source = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    private readonly int attemptLimit = maxAttempts is >= 1 and <= 20 ? maxAttempts : throw new ArgumentOutOfRangeException(nameof(maxAttempts));

    public async Task EnqueueAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, JobRequest request, CancellationToken ct)
    {
        RequireTransaction(connection, transaction);
        ArgumentNullException.ThrowIfNull(request);
        if (request.Workspace.Value == Guid.Empty || string.IsNullOrWhiteSpace(request.Kind) || string.IsNullOrWhiteSpace(request.ScopeKey)
            || request.Kind.Length > 128 || request.ScopeKey.Length > 1024 || request.DesiredRevision < 0
            || request.NotBefore == DateTimeOffset.MinValue || request.NotBefore == DateTimeOffset.MaxValue)
            throw new ArgumentException("Invalid scoped job request.");
        await using var command = new NpgsqlCommand("""
            INSERT INTO processing.jobs AS j(workspace_id,job_kind,scope_key,desired_revision,status,priority,not_before)
            VALUES (@workspace,@kind,@scope,@revision,'PENDING',@priority,@due)
            ON CONFLICT(workspace_id,job_kind,scope_key) DO UPDATE SET
              desired_revision=excluded.desired_revision,
              status=CASE WHEN j.status='RUNNING' THEN 'RUNNING' ELSE 'PENDING' END,
              priority=CASE WHEN j.status IN ('SUCCEEDED','DEAD') THEN excluded.priority ELSE least(j.priority,excluded.priority) END,
              not_before=CASE WHEN j.status IN ('SUCCEEDED','DEAD') THEN excluded.not_before ELSE least(j.not_before,excluded.not_before) END,
              attempts=CASE WHEN j.status='RUNNING' THEN j.attempts ELSE 0 END,
              error_code=NULL,updated_at=clock_timestamp()
            WHERE excluded.desired_revision>j.desired_revision
            """, connection, transaction) { CommandTimeout = 30 };
        command.Parameters.AddWithValue("workspace", request.Workspace.Value);
        command.Parameters.AddWithValue("kind", request.Kind);
        command.Parameters.AddWithValue("scope", request.ScopeKey);
        command.Parameters.AddWithValue("revision", request.DesiredRevision);
        command.Parameters.AddWithValue("priority", request.Priority);
        command.Parameters.AddWithValue("due", request.NotBefore.ToUniversalTime());
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<JobLease?> ClaimAsync(string workerId, TimeSpan leaseDuration, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerId);
        if (workerId.Length > 200 || leaseDuration <= TimeSpan.Zero || leaseDuration > TimeSpan.FromMinutes(15))
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            WITH candidate AS (
              SELECT id,(status='RUNNING' AND desired_revision>claimed_revision) AS fresh
              FROM processing.jobs
              WHERE (status IN ('PENDING','RETRY') AND not_before<=clock_timestamp())
                 OR (status='RUNNING' AND lease_until<=clock_timestamp())
              ORDER BY priority,not_before,id FOR UPDATE SKIP LOCKED LIMIT 1
            ), claimed AS (
              UPDATE processing.jobs AS j SET
                status=CASE WHEN j.attempts>=@limit AND NOT c.fresh THEN 'DEAD' ELSE 'RUNNING' END,
                attempts=CASE WHEN c.fresh THEN 1 WHEN j.attempts>=@limit THEN j.attempts ELSE j.attempts+1 END,
                lease_owner=CASE WHEN j.attempts>=@limit AND NOT c.fresh THEN NULL ELSE @worker END,
                lease_until=CASE WHEN j.attempts>=@limit AND NOT c.fresh THEN NULL ELSE clock_timestamp()+@duration END,
                lease_token=CASE WHEN j.attempts>=@limit AND NOT c.fresh THEN NULL ELSE @token END,
                claimed_revision=CASE WHEN j.attempts>=@limit AND NOT c.fresh THEN NULL ELSE j.desired_revision END,
                error_code=CASE WHEN j.attempts>=@limit AND NOT c.fresh THEN 'ATTEMPTS_EXHAUSTED' ELSE NULL END,
                updated_at=clock_timestamp()
              FROM candidate AS c WHERE j.id=c.id RETURNING j.*
            )
            SELECT id,workspace_id,job_kind,scope_key,claimed_revision,lease_token,lease_until,attempts
            FROM claimed WHERE status='RUNNING'
            """, connection) { CommandTimeout = 30 };
        command.Parameters.AddWithValue("limit", attemptLimit);
        command.Parameters.AddWithValue("worker", workerId);
        command.Parameters.AddWithValue("duration", leaseDuration);
        command.Parameters.AddWithValue("token", Guid.NewGuid());
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct)
            ? new(reader.GetGuid(0), new(reader.GetGuid(1)), reader.GetString(2), reader.GetString(3), reader.GetInt64(4),
                reader.GetGuid(5), reader.GetFieldValue<DateTimeOffset>(6), reader.GetInt32(7)) : null;
    }

    public async Task<bool> CompleteAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, JobLease lease, CancellationToken ct)
    {
        RequireTransaction(connection, transaction);
        if (!await LockLease(connection, transaction, lease, ct)) return false;
        await using var command = new NpgsqlCommand("""
            UPDATE processing.jobs SET
              status=CASE WHEN desired_revision=@revision THEN 'SUCCEEDED' ELSE 'PENDING' END,
              attempts=CASE WHEN desired_revision=@revision THEN attempts ELSE 0 END,
              not_before=CASE WHEN desired_revision=@revision THEN not_before ELSE clock_timestamp() END,
              lease_owner=NULL,lease_until=NULL,lease_token=NULL,claimed_revision=NULL,
              error_code=NULL,updated_at=clock_timestamp()
            WHERE id=@id AND workspace_id=@workspace AND job_kind=@kind AND scope_key=@scope
              AND status='RUNNING' AND lease_token=@token AND claimed_revision=@revision AND lease_until>clock_timestamp()
            RETURNING status='SUCCEEDED'
            """, connection, transaction) { CommandTimeout = 30 };
        BindLease(command, lease);
        return await command.ExecuteScalarAsync(ct) is true;
    }

    public async Task<bool> FailAsync(JobLease lease, ProcessingFailure failure, CancellationToken ct)
    {
        if (!Enum.IsDefined(failure)) throw new ArgumentOutOfRangeException(nameof(failure));
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        if (!await LockLease(connection, transaction, lease, ct)) return false;
        await using var command = new NpgsqlCommand("""
            UPDATE processing.jobs SET
              status=CASE WHEN desired_revision>@revision THEN 'PENDING' WHEN attempts>=@limit THEN 'DEAD' ELSE 'RETRY' END,
              not_before=CASE WHEN desired_revision>@revision THEN clock_timestamp()
                ELSE clock_timestamp()+make_interval(secs=>least(300,power(2,attempts))::double precision) END,
              attempts=CASE WHEN desired_revision>@revision THEN 0 ELSE attempts END,
              error_code=CASE WHEN desired_revision>@revision THEN NULL ELSE @error END,
              lease_owner=NULL,lease_until=NULL,lease_token=NULL,claimed_revision=NULL,updated_at=clock_timestamp()
            WHERE id=@id AND workspace_id=@workspace AND job_kind=@kind AND scope_key=@scope
              AND status='RUNNING' AND lease_token=@token AND claimed_revision=@revision AND lease_until>clock_timestamp()
            RETURNING id
            """, connection, transaction) { CommandTimeout = 30 };
        BindLease(command, lease);
        command.Parameters.AddWithValue("limit", attemptLimit);
        command.Parameters.AddWithValue("error", failure == ProcessingFailure.HandlerFailure ? "HANDLER_FAILURE" : "INVALID_PAYLOAD");
        var changed = await command.ExecuteScalarAsync(ct) is Guid;
        await transaction.CommitAsync(ct);
        return changed;
    }

    private static async Task<bool> LockLease(NpgsqlConnection connection, NpgsqlTransaction transaction, JobLease lease, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(lease);
        // Lock first, then check expiry in the UPDATE, after any row-lock wait.
        await using var command = new NpgsqlCommand("""
            SELECT id FROM processing.jobs WHERE id=@id AND workspace_id=@workspace AND job_kind=@kind AND scope_key=@scope
              AND status='RUNNING' AND lease_token=@token AND claimed_revision=@revision FOR UPDATE
            """, connection, transaction) { CommandTimeout = 30 };
        BindLease(command, lease);
        return await command.ExecuteScalarAsync(ct) is Guid;
    }

    private static void BindLease(NpgsqlCommand command, JobLease lease)
    {
        command.Parameters.AddWithValue("id", lease.Id);
        command.Parameters.AddWithValue("workspace", lease.Workspace.Value);
        command.Parameters.AddWithValue("kind", lease.Kind);
        command.Parameters.AddWithValue("scope", lease.ScopeKey);
        command.Parameters.AddWithValue("token", lease.Token);
        command.Parameters.AddWithValue("revision", lease.Revision);
    }

    private void RequireTransaction(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);
        if (connection.State != ConnectionState.Open || !ReferenceEquals(connection, transaction.Connection))
            throw new ArgumentException("A matching active caller transaction is required.");
        var configured = new NpgsqlConnectionStringBuilder(source.ConnectionString);
        var actual = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
        var database = string.IsNullOrEmpty(configured.Database) ? configured.Username : configured.Database;
        if (string.IsNullOrEmpty(database) || !string.Equals(configured.Host, actual.Host, StringComparison.Ordinal)
            || configured.Port != actual.Port || !string.Equals(database, connection.Database, StringComparison.Ordinal))
            throw new ArgumentException("Caller transaction must target the configured queue endpoint and database.");
    }
}
