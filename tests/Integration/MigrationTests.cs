using Npgsql;
using System.Diagnostics;
using wAInery.Persistence;

namespace wAInery.Integration.Tests;

public sealed class MigrationTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static int MigrationCount => MigrationRunner.BuiltInMigrations.Count;

    [Fact]
    public async Task Host_migration_command_applies_and_then_replays_without_secret_output()
    {
        var connectionString = await fixture.CreateDatabaseAsync(Ct);
        var first = await RunHost(connectionString);
        Assert.Equal(0, first.ExitCode);
        Assert.Contains($"Applied {MigrationCount} migration(s).", first.Output);
        var second = await RunHost(connectionString);
        Assert.Equal(0, second.ExitCode);
        Assert.Contains("Applied 0 migration(s).", second.Output);
        var output = first.Output + first.Error + second.Output + second.Error;
        var password = new NpgsqlConnectionStringBuilder(connectionString).Password!;
        Assert.False(output.Contains(connectionString, StringComparison.Ordinal), "Connection string appeared in output.");
        Assert.False(output.Contains(password, StringComparison.Ordinal), "Password appeared in output.");
    }

    [Fact]
    public async Task Host_requires_explicit_database_configuration()
    {
        var result = await RunHost(null);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Set WAINERY_DB_CONNECTION", result.Error);
    }

    [Fact]
    public async Task Host_reports_connection_errors_without_echoing_configuration()
    {
        const string invalid = "Host=127.0.0.1;UnsupportedKey=fixture-private-marker";
        var result = await RunHost(invalid);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Migration failed", result.Error);
        Assert.False((result.Output + result.Error).Contains("fixture-private-marker", StringComparison.Ordinal),
            "Configuration value appeared in diagnostics.");
    }

    [Fact]
    public async Task Foundation_executes_on_real_pgvector_and_second_run_is_a_noop()
    {
        var connectionString = await fixture.CreateDatabaseAsync(Ct);
        var runner = new MigrationRunner();
        Assert.Equal(MigrationCount, await runner.ApplyAsync(connectionString, Ct));
        Assert.Equal(0, await runner.ApplyAsync(connectionString, Ct));
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Ct);
        Assert.Equal(3, await Scalar<int>(connection, "SELECT vector_dims('[1,2,3]'::vector)"));
        Assert.Equal(10L, await Scalar<long>(connection,
            "SELECT count(*) FROM information_schema.schemata WHERE schema_name IN ('registry','connectivity','vineyard','winery','observations','weather','knowledge','decisions','ai_execution','processing')"));
    }

    [Fact]
    public async Task Concurrent_migrators_apply_exactly_once()
    {
        var connectionString = await fixture.CreateDatabaseAsync(Ct);
        var results = await Task.WhenAll(new MigrationRunner().ApplyAsync(connectionString, Ct), new MigrationRunner().ApplyAsync(connectionString, Ct));
        Assert.Equal(new[] { 0, MigrationCount }, results.Order().ToArray());
    }

    [Fact]
    public async Task Changed_applied_payload_is_rejected_without_changing_history()
    {
        var connectionString = await fixture.CreateDatabaseAsync(Ct);
        await new MigrationRunner().ApplyAsync(connectionString, Ct);
        var original = MigrationRunner.BuiltInMigrations[0];
        var changed = original with { Sql = original.Sql + "\n-- changed payload" };
        var catalog = MigrationRunner.BuiltInMigrations.Select(migration => migration.Version == original.Version ? changed : migration);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new MigrationRunner(catalog).ApplyAsync(connectionString, Ct));
        Assert.Equal(0, await new MigrationRunner().ApplyAsync(connectionString, Ct));
    }

    [Fact]
    public async Task Failed_pending_migration_rolls_back_ddl_and_journal_together()
    {
        var connectionString = await fixture.CreateDatabaseAsync(Ct);
        await new MigrationRunner().ApplyAsync(connectionString, Ct);
        var broken = new SqlMigration(MigrationCount + 1, $"{MigrationCount + 1:D3}_broken.sql", "CREATE TABLE processing.rollback_probe(id int); SELECT 1/0;");
        var runner = new MigrationRunner(MigrationRunner.BuiltInMigrations.Append(broken));
        var error = await Assert.ThrowsAsync<PostgresException>(() => runner.ApplyAsync(connectionString, Ct));
        Assert.Equal(PostgresErrorCodes.DivisionByZero, error.SqlState);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Ct);
        Assert.True(await Scalar<bool>(connection, "SELECT to_regclass('processing.rollback_probe') IS NULL"));
        Assert.Equal((long)MigrationCount, await Scalar<long>(connection, "SELECT count(*) FROM processing.schema_migrations"));
    }

    [Fact]
    public async Task Unknown_database_migration_version_fails_closed()
    {
        var connectionString = await fixture.CreateDatabaseAsync(Ct);
        await new MigrationRunner().ApplyAsync(connectionString, Ct);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Ct);
        await using var insert = new NpgsqlCommand("INSERT INTO processing.schema_migrations(version,name,checksum) VALUES (999,'future.sql',repeat('0',64))", connection);
        await insert.ExecuteNonQueryAsync(Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new MigrationRunner().ApplyAsync(connectionString, Ct));
    }

    [Fact]
    public async Task Prediction_definition_versions_coexist_but_only_one_is_active()
    {
        var connectionString = await fixture.CreateDatabaseAsync(Ct);
        await new MigrationRunner().ApplyAsync(connectionString, Ct);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Ct);
        const string values = "('P-TEST',@version,'RULE','[]','{}','fixture:v1',@active)";
        foreach (var version in new[] { "1", "2" })
        {
            await using var insert = new NpgsqlCommand("INSERT INTO decisions.prediction_definitions(code,version,model_kind,input_dependencies,materiality_policy,output_contract,active) VALUES " + values, connection);
            insert.Parameters.AddWithValue("version", version);
            insert.Parameters.AddWithValue("active", version == "2");
            await insert.ExecuteNonQueryAsync(Ct);
        }
        Assert.Equal(2L, await Scalar<long>(connection, "SELECT count(*) FROM decisions.prediction_definitions"));
        await using var conflict = new NpgsqlCommand("UPDATE decisions.prediction_definitions SET active=true WHERE version='1'", connection);
        var error = await Assert.ThrowsAsync<PostgresException>(() => conflict.ExecuteNonQueryAsync(Ct));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, error.SqlState);
    }

    private static async Task<T> Scalar<T>(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync(Ct) ?? throw new InvalidOperationException("Scalar was null."));
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunHost(string? connectionString)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        var configuration = root.Parent!.Name;
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "wAInery.slnx"))) root = root.Parent;
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add(Path.Combine(root!.FullName, "src/Host/bin", configuration, "net10.0/wAInery.Host.dll"));
        start.ArgumentList.Add("--migrate");
        if (connectionString is null) start.Environment.Remove("WAINERY_DB_CONNECTION");
        else start.Environment["WAINERY_DB_CONNECTION"] = connectionString;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Host process did not start.");
        try
        {
            var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var error = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            return (process.ExitCode, await output, await error);
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
    }
}
