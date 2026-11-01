using Npgsql;
using wAInery.Persistence;

namespace wAInery.Integration.Tests;

// G1-08a / #25. Synthetic shared-processing invariants, not a Decisions handler.
// IO: earliest NotBefore/lower Priority coalesce; new terminal revision resets
// schedule/attempt budget; DB retry backoff is positive and capped at 300 seconds.
public sealed class JobQueueTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid WorkspaceA = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid WorkspaceB = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private const string Kind = "fixture-recompute";
    private const string Scope = "synthetic-block-A";
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);
    private static DateTimeOffset Due => DateTimeOffset.UtcNow.AddMinutes(-1);

    [Fact]
    public async Task Enqueue_rolls_back_with_the_callers_transaction()
    {
        await using var dataSource = await PreparedDatabase();
        var queue = new PostgresJobQueue(dataSource);
        await using var connection = await dataSource.OpenConnectionAsync(Ct);
        await using (var transaction = await connection.BeginTransactionAsync(Ct))
        {
            await queue.EnqueueAsync(connection, transaction, Request(1), Ct);
            await transaction.RollbackAsync(Ct);
        }
        Assert.Equal(0L, await Scalar<long>(dataSource, "SELECT count(*) FROM processing.jobs"));
    }

    [Fact]
    public async Task Scoped_dedupe_coalesces_to_monotonic_revision()
    {
        await using var dataSource = await PreparedDatabase();
        var queue = new PostgresJobQueue(dataSource);
        await Enqueue(dataSource, queue, Request(5));
        await Enqueue(dataSource, queue, Request(3));
        await Enqueue(dataSource, queue, Request(5));
        await Enqueue(dataSource, queue, Request(7));
        await Enqueue(dataSource, queue, new(new(WorkspaceB), Kind, Scope, 1, Due));
        await Enqueue(dataSource, queue, new(new(WorkspaceA), "another-fixture-kind", Scope, 2, Due));
        Assert.Equal(3L, await Scalar<long>(dataSource, "SELECT count(*) FROM processing.jobs"));
        Assert.Equal(7L, (await State(dataSource)).Revision);
        Assert.Equal("PENDING", (await State(dataSource)).Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Caller_transaction_must_target_the_queues_configured_database(bool complete)
    {
        await using var configured = await PreparedDatabase();
        await using var other = await PreparedDatabase();
        var queue = new PostgresJobQueue(configured);
        var otherQueue = new PostgresJobQueue(other);
        await Enqueue(other, otherQueue, Request(1));
        var lease = await Claim(otherQueue);
        await using var connection = await other.OpenConnectionAsync(Ct);
        await using var transaction = await connection.BeginTransactionAsync(Ct);
        if (complete)
            await Assert.ThrowsAsync<ArgumentException>(() => queue.CompleteAsync(connection, transaction, lease, Ct));
        else
            await Assert.ThrowsAsync<ArgumentException>(() => queue.EnqueueAsync(connection, transaction, Request(2), Ct));
        await transaction.RollbackAsync(Ct);
        Assert.Equal(0L, await Scalar<long>(configured, "SELECT count(*) FROM processing.jobs"));
        Assert.Equal("RUNNING", (await State(other)).Status);
        Assert.Equal(1L, (await State(other)).Revision);
    }

    [Fact]
    public async Task Pending_updates_preserve_earliest_schedule_and_highest_urgency()
    {
        await using var dataSource = await PreparedDatabase();
        var queue = new PostgresJobQueue(dataSource);
        await Enqueue(dataSource, queue, new(new(WorkspaceA), Kind, Scope, 1, DateTimeOffset.UtcNow.AddHours(1), 10));
        await Enqueue(dataSource, queue, new(new(WorkspaceA), Kind, Scope, 2, Due, -5));
        await Enqueue(dataSource, queue, new(new(WorkspaceA), Kind, Scope, 1, DateTimeOffset.UtcNow.AddHours(2), 100));
        await Enqueue(dataSource, queue, new(new(WorkspaceA), Kind, "other-scope", 1, Due, 0));
        var lease = await Claim(queue);
        Assert.Equal(Scope, lease.ScopeKey);
        Assert.Equal(2L, lease.Revision);
        Assert.Equal(-5, (await State(dataSource)).Priority);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Terminal_same_or_old_revision_is_a_noop_but_new_input_gets_fresh_budget(bool dead)
    {
        await using var dataSource = await PreparedDatabase();
        var queue = new PostgresJobQueue(dataSource, maxAttempts: 1);
        await Enqueue(dataSource, queue, new(new(WorkspaceA), Kind, Scope, 1, Due, -10));
        var first = await Claim(queue);
        if (dead) Assert.True(await queue.FailAsync(first, ProcessingFailure.HandlerFailure, Ct));
        else Assert.True(await Complete(dataSource, queue, first));
        var terminal = dead ? "DEAD" : "SUCCEEDED";
        Assert.Equal(terminal, (await State(dataSource)).Status);
        await Enqueue(dataSource, queue, Request(1));
        await Enqueue(dataSource, queue, Request(0));
        Assert.Null(await queue.ClaimAsync("fixture-worker", LeaseDuration, Ct));
        Assert.Equal(terminal, (await State(dataSource)).Status);
        Assert.False(await Complete(dataSource, queue, first));

        // A new terminal revision uses its requested schedule, not old urgency.
        await Enqueue(dataSource, queue, new(new(WorkspaceA), Kind, Scope, 2, DateTimeOffset.UtcNow.AddHours(1), 10));
        var pending = await State(dataSource);
        Assert.Equal("PENDING", pending.Status);
        Assert.Equal(0, pending.Attempts);
        Assert.Equal(10, pending.Priority);
        Assert.Null(await queue.ClaimAsync("fixture-worker", LeaseDuration, Ct));
        await MakeReady(dataSource, first.Id);
        var next = await Claim(queue);
        Assert.Equal(first.Id, next.Id);
        Assert.Equal(2L, next.Revision);
        Assert.Equal(1, next.Attempt);
    }

    [Fact]
    public async Task Future_job_is_not_claimed_until_due()
    {
        await using var dataSource = await PreparedDatabase();
        var queue = new PostgresJobQueue(dataSource);
        await Enqueue(dataSource, queue, new(new(WorkspaceA), Kind, Scope, 1, DateTimeOffset.UtcNow.AddHours(1)));
        Assert.Null(await queue.ClaimAsync("fixture-worker", LeaseDuration, Ct));
        Assert.Equal(0, (await State(dataSource)).Attempts);
        await Execute(dataSource, "UPDATE processing.jobs SET not_before=clock_timestamp()-interval '1 second'");
        Assert.Equal(1, (await Claim(queue)).Attempt);
    }

    [Fact]
    public async Task Locked_job_does_not_block_claiming_another_ready_scope()
    {
        await using var dataSource = await PreparedDatabase();
        var queue = new PostgresJobQueue(dataSource);
        await Enqueue(dataSource, queue, new(new(WorkspaceA), Kind, Scope, 1, Due, -1));
        await Enqueue(dataSource, queue, new(new(WorkspaceA), Kind, "unlocked-scope", 1, Due));
        await using var lockedConnection = await dataSource.OpenConnectionAsync(Ct);
        await using var lockTransaction = await lockedConnection.BeginTransactionAsync(Ct);
        await using var rowLock = new NpgsqlCommand(
            "SELECT id FROM processing.jobs WHERE scope_key=@scope FOR UPDATE", lockedConnection, lockTransaction);
        rowLock.Parameters.AddWithValue("scope", Scope);
        Assert.NotNull(await rowLock.ExecuteScalarAsync(Ct));
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(10));
        var lease = await queue.ClaimAsync("unblocked-worker", LeaseDuration, deadline.Token);
        Assert.NotNull(lease);
        Assert.Equal("unlocked-scope", lease.ScopeKey);
        await lockTransaction.RollbackAsync(Ct);
        Assert.Equal(Scope, (await Claim(queue)).ScopeKey);
    }

    [Fact]
    public async Task Concurrent_workers_claim_distinct_ready_jobs()
    {
        await using var dataSource = await PreparedDatabase();
        var queue = new PostgresJobQueue(dataSource);
        await Enqueue(dataSource, queue, Request(1));
        await Enqueue(dataSource, queue, new(new(WorkspaceA), Kind, "synthetic-block-B", 1, Due));
        var claims = await Task.WhenAll(
            queue.ClaimAsync("worker-A", LeaseDuration, Ct),
            queue.ClaimAsync("worker-B", LeaseDuration, Ct));
        Assert.All(claims, claim => Assert.NotNull(claim));
        Assert.Equal(2, claims.Select(claim => claim!.Id).Distinct().Count());
        Assert.Equal(2, claims.Select(claim => claim!.Token).Distinct().Count());
        Assert.Null(await queue.ClaimAsync("worker-C", LeaseDuration, Ct));
    }

    [Fact]
    public async Task Concurrent_claims_of_one_job_produce_one_lease()
    {
        await using var dataSource = await PreparedDatabase();
        var queue = new PostgresJobQueue(dataSource);
        await Enqueue(dataSource, queue, Request(1));
        var claims = await Task.WhenAll(
            queue.ClaimAsync("worker-A", LeaseDuration, Ct),
            queue.ClaimAsync("worker-B", LeaseDuration, Ct));
        Assert.Single(claims, claim => claim is not null);
    }

    [Fact]
    public async Task Expired_lease_cannot_complete_and_restart_with_same_worker_is_token_fenced()
    {
        await using var dataSource = await PreparedDatabase();
        var firstQueue = new PostgresJobQueue(dataSource);
        await Enqueue(dataSource, firstQueue, Request(1));
        var old = await Claim(firstQueue);
        await Expire(dataSource, old.Id);
        Assert.False(await Complete(dataSource, firstQueue, old));
        Assert.False(await firstQueue.FailAsync(old, ProcessingFailure.HandlerFailure, Ct));
        var restartedQueue = new PostgresJobQueue(dataSource);
        var current = await Claim(restartedQueue); // Same worker string, new process object.
        Assert.Equal(old.Id, current.Id);
        Assert.NotEqual(Guid.Empty, current.Token);
        Assert.NotEqual(old.Token, current.Token);
        Assert.Equal(2, current.Attempt);
        Assert.False(await Complete(dataSource, firstQueue, old));
        Assert.False(await firstQueue.FailAsync(old, ProcessingFailure.InvalidPayload, Ct));
        Assert.True(await Complete(dataSource, restartedQueue, current));
    }

    [Theory]
    [InlineData("workspace")]
    [InlineData("token")]
    [InlineData("revision")]
    public async Task Wrong_scope_token_or_claimed_revision_cannot_complete_or_fail(string changed)
    {
        await using var dataSource = await PreparedDatabase();
        var queue = new PostgresJobQueue(dataSource);
        await Enqueue(dataSource, queue, Request(1));
        var actual = await Claim(queue);
        var invalid = new JobLease(actual.Id,
            changed == "workspace" ? new(WorkspaceB) : actual.Workspace, actual.Kind, actual.ScopeKey,
            changed == "revision" ? actual.Revision + 1 : actual.Revision,
            changed == "token" ? Guid.NewGuid() : actual.Token, actual.LeaseUntil, actual.Attempt);
        Assert.False(await Complete(dataSource, queue, invalid));
        Assert.False(await queue.FailAsync(invalid, ProcessingFailure.HandlerFailure, Ct));
        Assert.Equal("RUNNING", (await State(dataSource)).Status);
        Assert.Equal(1, (await State(dataSource)).Attempts);
        Assert.True(await Complete(dataSource, queue, actual));
    }

    [Fact]
    public async Task New_revision_during_run_rejects_old_publication_and_requeues_latest_input()
    {
        await using var dataSource = await PreparedDatabase();
        var queue = new PostgresJobQueue(dataSource);
        await Enqueue(dataSource, queue, Request(1));
        var old = await Claim(queue);
        await Enqueue(dataSource, queue, Request(2));
        Assert.Equal("RUNNING", (await State(dataSource)).Status);
        // This synthetic caller obeys Complete=false: no real Decisions CAS is claimed.
        Assert.False(await CompleteAndPublish(dataSource, queue, old, commit: true));
        Assert.Equal(0L, await Scalar<long>(dataSource, "SELECT count(*) FROM processing.job_fixture_publications"));
        Assert.Equal("PENDING", (await State(dataSource)).Status);
        var current = await Claim(queue);
        Assert.Equal(2L, current.Revision);
        Assert.Equal(1, current.Attempt);
        Assert.False(await Complete(dataSource, queue, old));
        Assert.True(await CompleteAndPublish(dataSource, queue, current, commit: true));
        Assert.Equal(1L, await Scalar<long>(dataSource, "SELECT count(*) FROM processing.job_fixture_publications"));
        Assert.Equal(2L, await Scalar<long>(dataSource, "SELECT revision FROM processing.job_fixture_publications"));
    }

    [Fact]
    public async Task Completion_and_synthetic_publication_rollback_in_the_same_transaction()
    {
        await using var dataSource = await PreparedDatabase();
        var queue = new PostgresJobQueue(dataSource);
        await Enqueue(dataSource, queue, Request(1));
        var lease = await Claim(queue);
        Assert.True(await CompleteAndPublish(dataSource, queue, lease, commit: false));
        Assert.Equal("RUNNING", (await State(dataSource)).Status);
        Assert.Equal(0L, await Scalar<long>(dataSource, "SELECT count(*) FROM processing.job_fixture_publications"));
        Assert.True(await CompleteAndPublish(dataSource, queue, lease, commit: true));
        Assert.Equal("SUCCEEDED", (await State(dataSource)).Status);
        Assert.False(await Complete(dataSource, queue, lease));
        Assert.Equal(1L, await Scalar<long>(dataSource, "SELECT count(*) FROM processing.job_fixture_publications"));
    }

    [Theory]
    [InlineData(ProcessingFailure.HandlerFailure)]
    [InlineData(ProcessingFailure.InvalidPayload)]
    public async Task Failure_has_bounded_db_backoff_and_becomes_dead_at_attempt_limit(ProcessingFailure failure)
    {
        await using var dataSource = await PreparedDatabase();
        var queue = new PostgresJobQueue(dataSource, maxAttempts: 2);
        await Enqueue(dataSource, queue, Request(1));
        var first = await Claim(queue);
        Assert.True(await queue.FailAsync(first, failure, Ct));
        Assert.Equal("RETRY", (await State(dataSource)).Status);
        Assert.NotNull((await State(dataSource)).ErrorCode);
        Assert.True(await Scalar<bool>(dataSource, """
            SELECT not_before > updated_at AND
              not_before <= clock_timestamp()+interval '300 seconds'
            FROM processing.jobs
            """));
        Assert.False(await queue.FailAsync(first, failure, Ct));
        await MakeReady(dataSource, first.Id);
        var last = await Claim(queue);
        Assert.Equal(2, last.Attempt);
        Assert.True(await queue.FailAsync(last, failure, Ct));
        Assert.Equal("DEAD", (await State(dataSource)).Status);
        Assert.Null(await queue.ClaimAsync("fixture-worker", LeaseDuration, Ct));
    }

    [Fact]
    public async Task New_desired_revision_bypasses_old_run_failure_and_backoff()
    {
        await using var dataSource = await PreparedDatabase();
        var queue = new PostgresJobQueue(dataSource, maxAttempts: 1);
        await Enqueue(dataSource, queue, Request(1));
        var old = await Claim(queue);
        await Enqueue(dataSource, queue, Request(2));
        Assert.True(await queue.FailAsync(old, ProcessingFailure.HandlerFailure, Ct));
        var pending = await State(dataSource);
        Assert.Equal("PENDING", pending.Status);
        Assert.Equal(0, pending.Attempts);
        var current = await Claim(queue);
        Assert.Equal(2L, current.Revision);
        Assert.Equal(1, current.Attempt);
    }

    [Fact]
    public async Task Expired_final_attempt_becomes_dead_without_another_claim()
    {
        await using var dataSource = await PreparedDatabase();
        var queue = new PostgresJobQueue(dataSource, maxAttempts: 2);
        await Enqueue(dataSource, queue, Request(1));
        var first = await Claim(queue);
        await Expire(dataSource, first.Id);
        var last = await Claim(queue);
        Assert.Equal(2, last.Attempt);
        await Expire(dataSource, last.Id);
        Assert.Null(await queue.ClaimAsync("fixture-worker", LeaseDuration, Ct));
        Assert.Equal("DEAD", (await State(dataSource)).Status);
        Assert.Equal(2, (await State(dataSource)).Attempts);
        Assert.False(await Complete(dataSource, queue, last));
    }

    [Fact]
    public async Task Expired_old_run_does_not_exhaust_new_revision_attempt_budget()
    {
        await using var dataSource = await PreparedDatabase();
        var queue = new PostgresJobQueue(dataSource, maxAttempts: 1);
        await Enqueue(dataSource, queue, Request(1));
        var old = await Claim(queue);
        await Enqueue(dataSource, queue, Request(2));
        await Expire(dataSource, old.Id);
        var current = await Claim(queue);
        Assert.Equal(2L, current.Revision);
        Assert.Equal(1, current.Attempt);
        Assert.NotEqual(old.Token, current.Token);
    }

    private static JobRequest Request(long revision) => new(new(WorkspaceA), Kind, Scope, revision, Due);

    private static async Task<JobLease> Claim(PostgresJobQueue queue)
    {
        var lease = await queue.ClaimAsync("fixture-worker", LeaseDuration, Ct);
        Assert.NotNull(lease);
        return lease;
    }

    private static async Task Enqueue(NpgsqlDataSource source, PostgresJobQueue queue, JobRequest request)
    {
        await using var connection = await source.OpenConnectionAsync(Ct);
        await using var transaction = await connection.BeginTransactionAsync(Ct);
        await queue.EnqueueAsync(connection, transaction, request, Ct);
        await transaction.CommitAsync(Ct);
    }

    private static async Task<bool> Complete(NpgsqlDataSource source, PostgresJobQueue queue, JobLease lease)
    {
        await using var connection = await source.OpenConnectionAsync(Ct);
        await using var transaction = await connection.BeginTransactionAsync(Ct);
        var accepted = await queue.CompleteAsync(connection, transaction, lease, Ct);
        await transaction.CommitAsync(Ct);
        return accepted;
    }

    private static async Task<bool> CompleteAndPublish(NpgsqlDataSource source, PostgresJobQueue queue, JobLease lease, bool commit)
    {
        await using var connection = await source.OpenConnectionAsync(Ct);
        await using var transaction = await connection.BeginTransactionAsync(Ct);
        var accepted = await queue.CompleteAsync(connection, transaction, lease, Ct);
        if (accepted)
        {
            await using var insert = new NpgsqlCommand(
                "INSERT INTO processing.job_fixture_publications(job_id,revision) VALUES (@id,@revision)", connection, transaction);
            insert.Parameters.AddWithValue("id", lease.Id);
            insert.Parameters.AddWithValue("revision", lease.Revision);
            await insert.ExecuteNonQueryAsync(Ct);
        }
        if (commit) await transaction.CommitAsync(Ct);
        else await transaction.RollbackAsync(Ct);
        return accepted;
    }

    private sealed record JobState(string Status, long Revision, int Attempts, int Priority, string? ErrorCode);

    private static async Task<JobState> State(NpgsqlDataSource source)
    {
        await using var connection = await source.OpenConnectionAsync(Ct);
        await using var query = new NpgsqlCommand("""
            SELECT status,desired_revision,attempts,priority,error_code FROM processing.jobs
            WHERE workspace_id=@workspace AND job_kind=@kind AND scope_key=@scope
            """, connection);
        query.Parameters.AddWithValue("workspace", WorkspaceA);
        query.Parameters.AddWithValue("kind", Kind);
        query.Parameters.AddWithValue("scope", Scope);
        await using var reader = await query.ExecuteReaderAsync(Ct);
        Assert.True(await reader.ReadAsync(Ct));
        return new(reader.GetString(0), reader.GetInt64(1), reader.GetInt32(2),
            reader.GetInt32(3), reader.IsDBNull(4) ? null : reader.GetString(4));
    }

    private static Task Expire(NpgsqlDataSource source, Guid id)
        => ChangeTime(source, id, "UPDATE processing.jobs SET lease_until=clock_timestamp()-interval '1 second' WHERE id=@id");

    private static Task MakeReady(NpgsqlDataSource source, Guid id)
        => ChangeTime(source, id, "UPDATE processing.jobs SET not_before=clock_timestamp()-interval '1 second' WHERE id=@id");

    private static async Task ChangeTime(NpgsqlDataSource source, Guid id, string sql)
    {
        await using var connection = await source.OpenConnectionAsync(Ct);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        Assert.Equal(1, await command.ExecuteNonQueryAsync(Ct));
    }

    private static async Task<T> Scalar<T>(NpgsqlDataSource source, string sql)
    {
        await using var connection = await source.OpenConnectionAsync(Ct);
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync(Ct) ?? throw new InvalidOperationException("Fixture scalar was null."));
    }

    private static async Task Execute(NpgsqlDataSource source, string sql)
    {
        await using var connection = await source.OpenConnectionAsync(Ct);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(Ct);
    }

    private async Task<NpgsqlDataSource> PreparedDatabase()
    {
        var connectionString = await fixture.CreateDatabaseAsync(Ct);
        await new MigrationRunner().ApplyAsync(connectionString, Ct);
        var dataSource = NpgsqlDataSource.Create(connectionString);
        try
        {
            await Execute(dataSource, """
                INSERT INTO registry.workspaces(id,display_name) VALUES
                ('00000000-0000-0000-0000-000000000001','synthetic-A'),
                ('00000000-0000-0000-0000-000000000002','synthetic-B');
                CREATE TABLE processing.job_fixture_publications(
                  job_id uuid NOT NULL, revision bigint NOT NULL, PRIMARY KEY(job_id,revision));
                """);
            return dataSource;
        }
        catch { await dataSource.DisposeAsync(); throw; }
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Completion_or_failure_rechecks_expiry_after_waiting_for_a_row_lock(bool fail)
    {
        await using var dataSource = await PreparedDatabase();
        var queue = new PostgresJobQueue(dataSource);
        await Enqueue(dataSource, queue, Request(1));
        var lease = await Claim(queue);
        // Commit the short deadline BEFORE the blocker locks. Its transaction
        // must not update the tuple: an UPDATE could mask the old bug via EPQ.
        await ChangeTime(dataSource, lease.Id,
            "UPDATE processing.jobs SET lease_until=clock_timestamp()+interval '5 seconds' WHERE id=@id");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        await using var blocker = await dataSource.OpenConnectionAsync(deadline.Token);
        await using var blockerTransaction = await blocker.BeginTransactionAsync(deadline.Token);
        Task<bool>? operation = null;
        var released = false;
        var observed = false;
        try
        {
            await using var takeLock = new NpgsqlCommand(
                "SELECT pg_backend_pid() FROM processing.jobs WHERE id=@id FOR UPDATE", blocker, blockerTransaction);
            takeLock.Parameters.AddWithValue("id", lease.Id);
            var blockerPid = (int)(await takeLock.ExecuteScalarAsync(deadline.Token)
                ?? throw new InvalidOperationException("Synthetic job was not found."));
            operation = fail
                ? queue.FailAsync(lease, ProcessingFailure.HandlerFailure, deadline.Token)
                : CompleteAfterLockWait(dataSource, queue, lease, deadline.Token);
            await using var observer = await dataSource.OpenConnectionAsync(deadline.Token);
            await ObserveLockWaitWhileLeaseValid(observer, blockerPid, lease.Id, operation, deadline.Token);
            // Wait on the DB clock, capped at five seconds. The blocker only
            // rolls back afterwards; no UPDATE forces predicate re-evaluation.
            await using (var expire = new NpgsqlCommand("""
                SELECT pg_sleep(LEAST(5.0,
                  GREATEST(0.0,extract(epoch FROM (lease_until-clock_timestamp()))+0.05))::double precision)
                FROM processing.jobs WHERE id=@id
                """, observer) { CommandTimeout = 10 })
            {
                expire.Parameters.AddWithValue("id", lease.Id);
                await expire.ExecuteNonQueryAsync(deadline.Token);
            }
            await using (var expired = new NpgsqlCommand(
                "SELECT lease_until<=clock_timestamp() FROM processing.jobs WHERE id=@id", observer))
            {
                expired.Parameters.AddWithValue("id", lease.Id);
                Assert.Equal(true, await expired.ExecuteScalarAsync(deadline.Token));
            }
            await blockerTransaction.RollbackAsync(deadline.Token);
            released = true;
            var accepted = await operation;
            observed = true;
            Assert.False(accepted);
            Assert.Equal("RUNNING", (await State(dataSource)).Status);
            Assert.Equal(0L, await Scalar<long>(dataSource, "SELECT count(*) FROM processing.job_fixture_publications"));
        }
        finally
        {
            try
            {
                if (!released)
                {
                    using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await blockerTransaction.RollbackAsync(cleanup.Token);
                }
            }
            finally
            {
                deadline.Cancel();
                if (operation is not null && !observed)
                {
                    try { await operation; }
                    catch (Exception) when (deadline.IsCancellationRequested)
                    {
                        // A body/setup assertion already failed. Observe the
                        // cancelled/faulted worker and preserve that failure.
                    }
                }
            }
        }
    }

    private static async Task ObserveLockWaitWhileLeaseValid(NpgsqlConnection observer, int blockerPid,
        Guid jobId, Task<bool> operation, CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            await using var probe = new NpgsqlCommand("""
                SELECT j.lease_until>clock_timestamp(), EXISTS(
                  SELECT 1 FROM pg_stat_activity AS activity
                  WHERE activity.datname=current_database() AND activity.wait_event_type='Lock'
                    AND @blocker=ANY(pg_blocking_pids(activity.pid)))
                FROM processing.jobs AS j WHERE j.id=@id
                """, observer);
            probe.Parameters.AddWithValue("blocker", blockerPid);
            probe.Parameters.AddWithValue("id", jobId);
            await using var reader = await probe.ExecuteReaderAsync(ct);
            Assert.True(await reader.ReadAsync(ct));
            Assert.True(reader.GetBoolean(0),
                "Lease expired before a lock wait was observed; timing precondition was not established.");
            if (reader.GetBoolean(1)) return;
            Assert.False(operation.IsCompleted, "Worker finished before its row-lock wait was observed.");
            await Task.Delay(TimeSpan.FromMilliseconds(25), ct);
        }
    }

    private static async Task<bool> CompleteAfterLockWait(NpgsqlDataSource source, PostgresJobQueue queue,
        JobLease lease, CancellationToken ct)
    {
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var accepted = await queue.CompleteAsync(connection, transaction, lease, ct);
        if (accepted)
        {
            await using var publish = new NpgsqlCommand(
                "INSERT INTO processing.job_fixture_publications(job_id,revision) VALUES (@id,@revision)", connection, transaction);
            publish.Parameters.AddWithValue("id", lease.Id);
            publish.Parameters.AddWithValue("revision", lease.Revision);
            await publish.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return accepted;
    }

}
