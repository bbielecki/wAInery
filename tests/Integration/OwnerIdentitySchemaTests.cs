using Npgsql;
using wAInery.Persistence;

namespace wAInery.Integration.Tests;

public sealed class OwnerIdentitySchemaTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Upgrade_preserves_legacy_identity_and_records_only_migration_known_history()
    {
        var database = await fixture.CreateDatabaseAsync(Ct);
        await new MigrationRunner(MigrationRunner.BuiltInMigrations.Take(4)).ApplyAsync(database, Ct);
        await using var connection = new NpgsqlConnection(database);
        await connection.OpenAsync(Ct);
        var workspace = Guid.NewGuid();
        await Exec(connection, "INSERT INTO registry.workspaces(id,display_name) VALUES (@w,'synthetic')", workspace);
        var block = Guid.NewGuid();
        await using (var insert = new NpgsqlCommand("INSERT INTO vineyard.blocks(id,workspace_id,name,area_m2) VALUES (@id,@w,'legacy',12.25)", connection))
        {
            insert.Parameters.AddWithValue("id", block);
            insert.Parameters.AddWithValue("w", workspace);
            await insert.ExecuteNonQueryAsync(Ct);
        }
        await Exec(connection, "INSERT INTO weather.locations(workspace_id,name,latitude,longitude) VALUES (@w,'legacy',50.25,19.75)", workspace);
        var before = await Scalar<DateTime>(connection, "SELECT clock_timestamp()");
        Assert.Equal(MigrationRunner.BuiltInMigrations.Count - 4, await new MigrationRunner().ApplyAsync(database, Ct));
        await using var read = new NpgsqlCommand("SELECT b.public_id,b.area_m2,r.recorded_at,r.version FROM vineyard.blocks b JOIN vineyard.block_revisions r ON (r.workspace_id,r.block_id)=(b.workspace_id,b.id) WHERE b.id=@id", connection);
        read.Parameters.AddWithValue("id", block);
        await using (var reader = await read.ExecuteReaderAsync(Ct))
        {
            Assert.True(await reader.ReadAsync(Ct));
            Assert.Equal(block.ToString(), reader.GetString(0));
            Assert.Equal(12.25m, reader.GetDecimal(1));
            Assert.True(reader.GetDateTime(2) >= before);
            Assert.Equal(1, reader.GetInt64(3));
            Assert.False(await reader.ReadAsync(Ct));
        }
        Assert.True(await Scalar<bool>(connection, "SELECT public_id=id::text AND version=1 AND latitude=50.25 AND longitude=19.75 FROM weather.locations"));
        foreach (var mutation in new[] { "UPDATE vineyard.block_revisions SET name='rewritten'", "DELETE FROM vineyard.block_revisions" })
        {
            await using var invalid = new NpgsqlCommand(mutation, connection);
            var error = await Assert.ThrowsAsync<PostgresException>(() => invalid.ExecuteNonQueryAsync(Ct));
            Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        }
        Assert.Equal(0, await new MigrationRunner().ApplyAsync(database, Ct));
    }

    [Fact]
    public async Task Opaque_ids_and_revisions_are_workspace_scoped()
    {
        var database = await fixture.CreateDatabaseAsync(Ct);
        await new MigrationRunner().ApplyAsync(database, Ct);
        await using var connection = new NpgsqlConnection(database);
        await connection.OpenAsync(Ct);
        var workspace = Guid.NewGuid();
        var other = Guid.NewGuid();
        await Exec(connection, "INSERT INTO registry.workspaces(id,display_name) VALUES (@w,'synthetic')", workspace);
        await Exec(connection, "INSERT INTO registry.workspaces(id,display_name) VALUES (@w,'other')", other);
        const string insert = "INSERT INTO vineyard.blocks(workspace_id,public_id,name) VALUES (@w,'opaque:block/A',gen_random_uuid()::text)";
        await Exec(connection, insert, workspace);
        await Exec(connection, insert, other);
        var duplicate = await Assert.ThrowsAsync<PostgresException>(() => Exec(connection, insert, workspace));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicate.SqlState);
        var blank = await Assert.ThrowsAsync<PostgresException>(() => Exec(connection,
            "INSERT INTO weather.locations(workspace_id,public_id,name,latitude,longitude) VALUES (@w,'  ','invalid',0,0)", workspace));
        Assert.Equal(PostgresErrorCodes.CheckViolation, blank.SqlState);
        await using var wrongHistory = new NpgsqlCommand("INSERT INTO vineyard.block_revisions(workspace_id,block_id,version,recorded_at,name) SELECT @other,id,1,now(),name FROM vineyard.blocks WHERE workspace_id=@w", connection);
        wrongHistory.Parameters.AddWithValue("w", workspace);
        wrongHistory.Parameters.AddWithValue("other", other);
        var scope = await Assert.ThrowsAsync<PostgresException>(() => wrongHistory.ExecuteNonQueryAsync(Ct));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, scope.SqlState);
    }

    [Theory]
    [InlineData("vineyard")]
    [InlineData("weather")]
    public async Task Command_receipts_enforce_workspace_key_and_fingerprint(string schema)
    {
        var database = await fixture.CreateDatabaseAsync(Ct);
        await new MigrationRunner().ApplyAsync(database, Ct);
        await using var connection = new NpgsqlConnection(database);
        await connection.OpenAsync(Ct);
        var workspace = Guid.NewGuid();
        await Exec(connection, "INSERT INTO registry.workspaces(id,display_name) VALUES (@w,'synthetic')", workspace);
        // Schema comes only from these fixed test cases, never user input.
        var insert = $"INSERT INTO {schema}.command_receipts(workspace_id,idempotency_key,request_sha256,result) VALUES (@w,'fixture-key',repeat('a',64),'{{}}')";
        await Exec(connection, insert, workspace);
        var other = Guid.NewGuid();
        await Exec(connection, "INSERT INTO registry.workspaces(id,display_name) VALUES (@w,'other')", other);
        await Exec(connection, insert, other);
        var duplicate = await Assert.ThrowsAsync<PostgresException>(() => Exec(connection, insert, workspace));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicate.SqlState);
        var invalid = await Assert.ThrowsAsync<PostgresException>(() => Exec(connection,
            $"INSERT INTO {schema}.command_receipts(workspace_id,idempotency_key,request_sha256,result) VALUES (@w,'other','bad','{{}}')", workspace));
        Assert.Equal(PostgresErrorCodes.CheckViolation, invalid.SqlState);
    }

    [Fact]
    public async Task Accepted_decimal_precision_is_preserved_by_owner_columns()
    {
        var database = await fixture.CreateDatabaseAsync(Ct);
        await new MigrationRunner().ApplyAsync(database, Ct);
        await using var connection = new NpgsqlConnection(database);
        await connection.OpenAsync(Ct);
        var workspace = Guid.NewGuid();
        await Exec(connection, "INSERT INTO registry.workspaces(id,display_name) VALUES (@w,'synthetic')", workspace);
        await Exec(connection, "INSERT INTO vineyard.blocks(workspace_id,name,area_m2) VALUES (@w,'precision',12.123456789012)", workspace);
        await Exec(connection, "INSERT INTO weather.locations(workspace_id,name,latitude,longitude,elevation_m) VALUES (@w,'precision',50.123456789012,19.123456789012,123.123456789012)", workspace);
        Assert.Equal(12.123456789012m, await Scalar<decimal>(connection, "SELECT area_m2 FROM vineyard.blocks"));
        Assert.Equal(50.123456789012m, await Scalar<decimal>(connection, "SELECT latitude FROM weather.locations"));
        Assert.Equal(19.123456789012m, await Scalar<decimal>(connection, "SELECT longitude FROM weather.locations"));
        Assert.Equal(123.123456789012m, await Scalar<decimal>(connection, "SELECT elevation_m FROM weather.locations"));
    }

    private static async Task Exec(NpgsqlConnection connection, string sql, Guid workspace)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("w", workspace);
        await command.ExecuteNonQueryAsync(Ct);
    }
    private static async Task<T> Scalar<T>(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync(Ct))!;
    }
}
