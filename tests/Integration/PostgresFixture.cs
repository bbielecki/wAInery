using Npgsql;
using Testcontainers.PostgreSql;

namespace wAInery.Integration.Tests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("pgvector/pgvector:pg17")
        .WithDatabase("wainery_bootstrap").WithUsername("wainery_test")
        .WithPassword(Guid.NewGuid().ToString("N"))
        .WithCreateParameterModifier(parameters =>
        {
            var ports = parameters.HostConfig?.PortBindings
                ?? throw new InvalidOperationException("Fixture port bindings are required.");
            foreach (var bindings in ports.Values)
                foreach (var binding in bindings) binding.HostIP = "127.0.0.1";
        }).Build();

    public async ValueTask InitializeAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await container.StartAsync(timeout.Token);
    }

    public async Task<string> CreateDatabaseAsync(CancellationToken ct)
    {
        var name = "wainery_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(container.GetConnectionString());
        await admin.OpenAsync(ct);
        var identifier = new NpgsqlCommandBuilder().QuoteIdentifier(name);
        await using var create = new NpgsqlCommand("CREATE DATABASE " + identifier, admin);
        await create.ExecuteNonQueryAsync(ct);
        return new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { Database = name }.ConnectionString;
    }

    public ValueTask DisposeAsync() => container.DisposeAsync();
}
