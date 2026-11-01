using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace wAInery.Persistence;

public sealed record SqlMigration(int Version, string Name, string Sql)
{
    public string Checksum => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(Sql.Replace("\r\n", "\n", StringComparison.Ordinal)))).ToLowerInvariant();
}

public sealed class MigrationRunner
{
    public static IReadOnlyList<SqlMigration> BuiltInMigrations { get; } = LoadEmbedded();
    private readonly ImmutableArray<SqlMigration> migrations;

    public MigrationRunner(IEnumerable<SqlMigration>? catalog = null)
    {
        migrations = (catalog ?? BuiltInMigrations).OrderBy(migration => migration.Version).ToImmutableArray();
        if (migrations.IsEmpty || migrations.Where((migration, index) => migration.Version != index + 1
            || string.IsNullOrWhiteSpace(migration.Name) || string.IsNullOrWhiteSpace(migration.Sql)).Any())
            throw new ArgumentException("Migration catalog must be a non-empty contiguous immutable history.");
    }

    public async Task<int> ApplyAsync(string connectionString, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await Execute("SELECT pg_advisory_xact_lock(1450088293)");
        await Execute("""
            CREATE SCHEMA IF NOT EXISTS processing;
            CREATE TABLE IF NOT EXISTS processing.schema_migrations (
                version integer PRIMARY KEY, name text NOT NULL, checksum char(64) NOT NULL,
                applied_at timestamptz NOT NULL DEFAULT now()
            );
            """);

        var applied = new List<(int Version, string Name, string Checksum)>();
        await using (var command = new NpgsqlCommand("SELECT version,name,checksum FROM processing.schema_migrations ORDER BY version", connection, transaction) { CommandTimeout = 60 })
        await using (var reader = await command.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) applied.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));

        for (var index = 0; index < applied.Count; index++)
        {
            var row = applied[index];
            if (index >= migrations.Length || row.Version != migrations[index].Version
                || row.Name != migrations[index].Name || row.Checksum != migrations[index].Checksum)
                throw new InvalidOperationException("Database migration history differs from the compiled immutable catalog.");
        }

        foreach (var migration in migrations.Skip(applied.Count))
        {
            await Execute(migration.Sql);
            await using var journal = new NpgsqlCommand(
                "INSERT INTO processing.schema_migrations(version,name,checksum) VALUES (@version,@name,@checksum)", connection, transaction) { CommandTimeout = 60 };
            journal.Parameters.AddWithValue("version", migration.Version);
            journal.Parameters.AddWithValue("name", migration.Name);
            journal.Parameters.AddWithValue("checksum", migration.Checksum);
            await journal.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return migrations.Length - applied.Count;

        async Task Execute(string sql)
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction) { CommandTimeout = 60 };
            await command.ExecuteNonQueryAsync(ct);
        }
    }

    private static IReadOnlyList<SqlMigration> LoadEmbedded()
    {
        var assembly = typeof(MigrationRunner).Assembly;
        var prefix = assembly.GetName().Name + ".Migrations.";
        var result = new List<SqlMigration>();
        foreach (var resource in assembly.GetManifestResourceNames().Where(name => name.StartsWith(prefix, StringComparison.Ordinal)))
        {
            var name = resource[prefix.Length..];
            using var stream = assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException("Embedded migration missing.");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            result.Add(new SqlMigration(int.Parse(name.AsSpan(0, name.IndexOf('_')), CultureInfo.InvariantCulture), name, reader.ReadToEnd()));
        }
        return result.OrderBy(migration => migration.Version).ToImmutableArray();
    }
}
