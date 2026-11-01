using Npgsql;

namespace wAInery.Persistence;

// One read-only statement/snapshot. This checks journal compatibility/extensions,
// not every physical table or application business readiness; never migrates.
public sealed class MigrationCompatibilityProbe(NpgsqlDataSource dataSource)
{
    public async Task<bool> IsCompatibleAsync(CancellationToken ct = default)
    {
        using var bounded = CancellationTokenSource.CreateLinkedTokenSource(ct);
        bounded.CancelAfter(TimeSpan.FromSeconds(5));
        await using var connection = await dataSource.OpenConnectionAsync(bounded.Token);
        await using var command = new NpgsqlCommand("""
            SELECT version,name,checksum,
                EXISTS(SELECT 1 FROM pg_extension WHERE extname='vector') AND
                EXISTS(SELECT 1 FROM pg_extension WHERE extname='btree_gist')
            FROM processing.schema_migrations ORDER BY version
            """, connection) { CommandTimeout = 5 };
        await using var reader = await command.ExecuteReaderAsync(bounded.Token);
        var catalog = MigrationRunner.BuiltInMigrations;
        var index = 0;
        while (await reader.ReadAsync(bounded.Token))
        {
            if (index >= catalog.Count || !reader.GetBoolean(3)
                || reader.GetInt32(0) != catalog[index].Version
                || reader.GetString(1) != catalog[index].Name
                || reader.GetString(2) != catalog[index].Checksum) return false;
            index++;
        }
        return index == catalog.Count;
    }
}
