using Npgsql;
using wAInery.Persistence;

namespace wAInery.Integration.Tests;

public sealed class IntegrityTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid WorkspaceA = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid WorkspaceB = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid BlockA = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid BlockB = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid BatchA = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid BatchB = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid VesselA = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid VesselA2 = Guid.Parse("30000000-0000-0000-0000-000000000003");
    private static readonly Guid SourceA = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid SourceB = Guid.Parse("40000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task Upgrade_preserves_valid_existing_history()
    {
        var connectionString = await fixture.CreateDatabaseAsync(Ct);
        await using var connection = await PreparedDatabase(connectionString, foundationOnly: true);
        await Occupy(connection, VesselA, "2026-04-18T10:00Z", "2026-04-18T11:00Z");
        var original = Guid.NewGuid();
        await Record(connection, "vineyard", WorkspaceA, original, null);
        await Record(connection, "vineyard", WorkspaceA, Guid.NewGuid(), original);
        Assert.Equal(1, await new MigrationRunner().ApplyAsync(connectionString, Ct));
        await using var count = new NpgsqlCommand("SELECT count(*) FROM vineyard.operations", connection);
        Assert.Equal(2L, await count.ExecuteScalarAsync(Ct));
    }

    [Fact]
    public async Task Invalid_existing_history_blocks_upgrade_without_deleting_data()
    {
        var connectionString = await fixture.CreateDatabaseAsync(Ct);
        await using var connection = await PreparedDatabase(connectionString, foundationOnly: true);
        await Occupy(connection, VesselA, "2026-04-18T10:00Z", "2026-04-18T11:00Z");
        await Occupy(connection, VesselA, "2026-04-18T10:30Z", "2026-04-18T11:30Z");
        var error = await Assert.ThrowsAsync<PostgresException>(() => new MigrationRunner().ApplyAsync(connectionString, Ct));
        Assert.Equal(PostgresErrorCodes.ExclusionViolation, error.SqlState);
        await using var rows = new NpgsqlCommand("SELECT count(*) FROM winery.batch_vessel_occupancy", connection);
        Assert.Equal(2L, await rows.ExecuteScalarAsync(Ct));
        await using var history = new NpgsqlCommand("SELECT count(*) FROM processing.schema_migrations", connection);
        Assert.Equal(1L, await history.ExecuteScalarAsync(Ct));
    }

    [Fact]
    public async Task Vessel_overlap_is_rejected_but_adjacent_half_open_windows_and_other_vessels_work()
    {
        await using var connection = await PreparedDatabase();
        await Occupy(connection, VesselA, "2026-04-18T10:00Z", "2026-04-18T11:00Z");
        var error = await Assert.ThrowsAsync<PostgresException>(() => Occupy(connection, VesselA, "2026-04-18T10:30Z", "2026-04-18T11:30Z"));
        Assert.Equal(PostgresErrorCodes.ExclusionViolation, error.SqlState);
        await Occupy(connection, VesselA, "2026-04-18T11:00Z", "2026-04-18T12:00Z");
        await Occupy(connection, VesselA2, "2026-04-18T10:00Z", "2026-04-18T12:00Z");
    }

    [Theory]
    [InlineData("vineyard")]
    [InlineData("winery")]
    [InlineData("observations")]
    public async Task Corrections_stay_in_their_workspace(string owner)
    {
        await using var connection = await PreparedDatabase();
        var own = Guid.NewGuid();
        var foreign = Guid.NewGuid();
        await Record(connection, owner, WorkspaceA, own, null);
        await Record(connection, owner, WorkspaceB, foreign, null);
        await Record(connection, owner, WorkspaceA, Guid.NewGuid(), own);
        var error = await Assert.ThrowsAsync<PostgresException>(() => Record(connection, owner, WorkspaceA, Guid.NewGuid(), foreign));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, error.SqlState);
    }

    [Fact]
    public async Task Open_occupancy_blocks_future_use_until_it_is_closed()
    {
        await using var connection = await PreparedDatabase();
        await Occupy(connection, VesselA, "2026-04-18T10:00Z", null);
        var error = await Assert.ThrowsAsync<PostgresException>(() => Occupy(connection, VesselA, "2026-04-18T11:00Z", null));
        Assert.Equal(PostgresErrorCodes.ExclusionViolation, error.SqlState);
        await Execute(connection, "UPDATE winery.batch_vessel_occupancy SET valid_to='2026-04-18T11:00Z'");
        await Occupy(connection, VesselA, "2026-04-18T11:00Z", null);
    }

    [Fact]
    public async Task Unused_definition_cannot_be_deleted_and_republished()
    {
        await using var connection = await PreparedDatabase();
        await Execute(connection, """
            INSERT INTO decisions.prediction_definitions(code,version,model_kind,input_dependencies,materiality_policy,output_contract,active)
            VALUES ('P-UNUSED','1','RULE','[]','{}','fixture:v1',false);
            """);
        var error = await Assert.ThrowsAsync<PostgresException>(() => Execute(connection,
            "DELETE FROM decisions.prediction_definitions WHERE code='P-UNUSED'"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
    }

    [Theory]
    [InlineData("vineyard")]
    [InlineData("winery")]
    [InlineData("observations")]
    public async Task A_record_cannot_correct_itself(string owner)
    {
        await using var connection = await PreparedDatabase();
        var id = Guid.NewGuid();
        var error = await Assert.ThrowsAsync<PostgresException>(() => Record(connection, owner, WorkspaceA, id, id));
        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
    }

    [Fact]
    public async Task Published_definition_payload_is_immutable_while_active_selection_can_change()
    {
        await using var connection = await PreparedDatabase();
        await Execute(connection, """
            INSERT INTO decisions.prediction_definitions(code,version,model_kind,input_dependencies,materiality_policy,output_contract,active)
            VALUES ('P-IMMUTABLE','1','RULE','[]','{}','fixture:v1',true);
            INSERT INTO decisions.prediction_states(workspace_id,prediction_code,prediction_definition_version,subject_module,subject_kind,subject_id,horizon_start,horizon_end,status)
            VALUES ('00000000-0000-0000-0000-000000000001','P-IMMUTABLE','1','Winery','WineBatch','20000000-0000-0000-0000-000000000001','2026-04-18T10:00Z','2026-04-19T10:00Z','INSUFFICIENT_DATA');
            """);
        var error = await Assert.ThrowsAsync<PostgresException>(() => Execute(connection,
            "UPDATE decisions.prediction_definitions SET materiality_policy='{\"changed\":true}' WHERE code='P-IMMUTABLE'"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        await Execute(connection, """
            INSERT INTO decisions.prediction_definitions(code,version,model_kind,input_dependencies,materiality_policy,output_contract,active)
            VALUES ('P-IMMUTABLE','2','RULE','[]','{}','fixture:v1',false);
            UPDATE decisions.prediction_definitions SET active=false WHERE code='P-IMMUTABLE' AND version='1';
            UPDATE decisions.prediction_definitions SET active=true WHERE code='P-IMMUTABLE' AND version='2';
            """);
        await using var current = new NpgsqlCommand("SELECT version FROM decisions.prediction_definitions WHERE code='P-IMMUTABLE' AND active", connection);
        Assert.Equal("2", await current.ExecuteScalarAsync(Ct));
    }

    private async Task<NpgsqlConnection> PreparedDatabase(string? connectionString = null, bool foundationOnly = false)
    {
        connectionString ??= await fixture.CreateDatabaseAsync(Ct);
        var catalog = foundationOnly ? new[] { MigrationRunner.BuiltInMigrations[0] } : null;
        await new MigrationRunner(catalog).ApplyAsync(connectionString, Ct);
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Ct);
        try
        {
            await Execute(connection, """
                INSERT INTO registry.workspaces(id,display_name) VALUES
                  ('00000000-0000-0000-0000-000000000001','fixture-A'),('00000000-0000-0000-0000-000000000002','fixture-B');
                INSERT INTO vineyard.blocks(id,workspace_id,name) VALUES
                  ('10000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000001','block-A'),
                  ('10000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000002','block-B');
                INSERT INTO winery.batches(id,workspace_id,code,state) VALUES
                  ('20000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000001','batch-A','ACTIVE'),
                  ('20000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000002','batch-B','ACTIVE');
                INSERT INTO winery.vessels(id,workspace_id,name,type) VALUES
                  ('30000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000001','vessel-A','FIXTURE'),
                  ('30000000-0000-0000-0000-000000000003','00000000-0000-0000-0000-000000000001','vessel-A2','FIXTURE');
                INSERT INTO registry.sources(id,workspace_id,source_kind,display_name,adapter_kind,rights_class) VALUES
                  ('40000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000001','MANUAL','fixture-A','FIXTURE','PRIVATE_OPERATIONAL'),
                  ('40000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000002','MANUAL','fixture-B','FIXTURE','PRIVATE_OPERATIONAL');
                INSERT INTO registry.measurement_types(code,canonical_unit,value_kind,description)
                  VALUES ('fixture_sugar','Brix','NUMERIC','synthetic fixture only');
                """);
            return connection;
        }
        catch { await connection.DisposeAsync(); throw; }
    }

    private static async Task Record(NpgsqlConnection connection, string owner, Guid workspace, Guid id, Guid? corrects)
    {
        var sql = owner switch
        {
            "vineyard" => "INSERT INTO vineyard.operations(id,workspace_id,block_id,operation_type,performed_at,confirmed_by,corrects_operation_id) VALUES (@id,@workspace,@subject,'FIXTURE','2026-04-18T10:00Z','fixture-owner',@corrects)",
            "winery" => "INSERT INTO winery.operations(id,workspace_id,batch_id,operation_type,performed_at,confirmed_by,corrects_operation_id) VALUES (@id,@workspace,@subject,'FIXTURE','2026-04-18T10:00Z','fixture-owner',@corrects)",
            "observations" => "INSERT INTO observations.measurements(id,workspace_id,subject_module,subject_kind,subject_id,measurement_type,numeric_value,unit,observed_at,source_id,source_external_key,quality,corrected_measurement_id) VALUES (@id,@workspace,'Winery','WineBatch',@subject,'fixture_sugar',1,'Brix','2026-04-18T10:00Z',@source,@external,'VALID',@corrects)",
            _ => throw new ArgumentException("Unknown fixture owner.")
        };
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("workspace", workspace);
        command.Parameters.AddWithValue("subject", owner == "vineyard" ? (workspace == WorkspaceA ? BlockA : BlockB) : (workspace == WorkspaceA ? BatchA : BatchB));
        command.Parameters.Add(new NpgsqlParameter("corrects", NpgsqlTypes.NpgsqlDbType.Uuid) { Value = (object?)corrects ?? DBNull.Value });
        if (owner == "observations")
        {
            command.Parameters.AddWithValue("source", workspace == WorkspaceA ? SourceA : SourceB);
            command.Parameters.AddWithValue("external", id.ToString());
        }
        await command.ExecuteNonQueryAsync(Ct);
    }

    private static async Task Occupy(NpgsqlConnection connection, Guid vessel, string from, string? to)
    {
        await using var command = new NpgsqlCommand("INSERT INTO winery.batch_vessel_occupancy(workspace_id,batch_id,vessel_id,valid_from,valid_to) VALUES (@workspace,@batch,@vessel,@from,@to)", connection);
        command.Parameters.AddWithValue("workspace", WorkspaceA);
        command.Parameters.AddWithValue("batch", BatchA);
        command.Parameters.AddWithValue("vessel", vessel);
        command.Parameters.AddWithValue("from", DateTimeOffset.Parse(from, System.Globalization.CultureInfo.InvariantCulture));
        command.Parameters.Add(new NpgsqlParameter("to", NpgsqlTypes.NpgsqlDbType.TimestampTz)
        {
            Value = to is null ? DBNull.Value : DateTimeOffset.Parse(to, System.Globalization.CultureInfo.InvariantCulture)
        });
        await command.ExecuteNonQueryAsync(Ct);
    }

    private static async Task Execute(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(Ct);
    }
}
