using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Npgsql;
using wAInery.Persistence;

namespace wAInery.Integration.Tests;

// Real owned child processes and fixture databases. Readiness is technical only.
public sealed class HostReadinessTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private const string PrivateMarker = "SYNTHETIC_HOST_PRIVATE_MARKER";
    private const string DynamicLoopback = "http://127.0.0.1:0";

    [Fact]
    public Task Normal_host_rejects_missing_database_before_listening()
        => AssertRejected(null);

    [Fact]
    public Task Malformed_database_configuration_exits_one_without_echoing_values()
        => AssertRejected("Host=127.0.0.1;UnsupportedKey=" + PrivateMarker);

    [Fact]
    public async Task An_unmigrated_database_is_rejected_without_automatic_migration()
    {
        var connectionString = await fixture.CreateDatabaseAsync(Ct);
        await AssertRejected(connectionString, password: Password(connectionString));
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Ct);
        await using var query = new NpgsqlCommand("SELECT to_regclass('processing.schema_migrations') IS NULL", connection);
        Assert.True((bool)(await query.ExecuteScalarAsync(Ct) ?? false));
    }

    [Fact]
    public async Task Changed_migration_history_is_rejected_at_startup_without_private_diagnostics()
    {
        var connectionString = await MigratedDatabase();
        await SetJournal(connectionString, valid: false);
        await AssertRejected(connectionString, password: Password(connectionString));
    }

    [Fact]
    public async Task Missing_vector_extension_is_rejected_even_when_journal_is_current()
    {
        var connectionString = await MigratedDatabase();
        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync(Ct);
            // Only this disposable fixture database is changed, never a user DB.
            await using var remove = new NpgsqlCommand("DROP EXTENSION vector CASCADE", connection);
            await remove.ExecuteNonQueryAsync(Ct);
        }
        await AssertRejected(connectionString, password: Password(connectionString));
    }

    [Theory]
    [InlineData("nonloopback")]
    [InlineData("https")]
    [InlineData("malformed")]
    public async Task Invalid_listen_configuration_is_rejected_before_binding(string variant)
    {
        var connectionString = await MigratedDatabase();
        var listen = variant switch
        {
            "nonloopback" => "http://0.0.0.0:0",
            "https" => "https://127.0.0.1:0",
            _ => "not-a-valid-uri"
        };
        await AssertRejected(connectionString, listen, Password(connectionString));
    }

    [Fact]
    public async Task Real_health_changes_with_journal_while_liveness_stays_available_and_repair_recovers()
    {
        var connectionString = await MigratedDatabase();
        var password = Password(connectionString);
        var host = OwnedHost.Start(CreateStart(connectionString));
        try
        {
            var startup = await host.WaitForStartup(Ct);
            Assert.True(startup.Listening, "Configured technical Host exited before listening.");
            await host.WaitUntilApplicationStarted(Ct);
            var address = AssertLoopback(host, startup.Address);
            Assert.NotEqual(5100, address.Port); // Explicit :0 must not fall back to the fixed default.
            using var http = NewClient(address);
            await AssertHealth(http, "/health/live", HttpStatusCode.OK, connectionString, password);
            await AssertHealth(http, "/health/ready", HttpStatusCode.OK, connectionString, password);
            await SetJournal(connectionString, valid: false);
            await AssertHealth(http, "/health/ready", HttpStatusCode.ServiceUnavailable, connectionString, password);
            await AssertHealth(http, "/health/live", HttpStatusCode.OK, connectionString, password);
            await SetJournal(connectionString, valid: true);
            await AssertHealth(http, "/health/ready", HttpStatusCode.OK, connectionString, password);
            AssertLoopback(host, startup.Address);
        }
        finally
        {
            await host.DisposeAsync();
            AssertSafe(host.CapturedText, connectionString, password);
        }
    }

    [Theory]
    [InlineData("environment-urls")]
    [InlineData("argument-urls")]
    [InlineData("environment-endpoint")]
    [InlineData("argument-endpoint")]
    public async Task Alternate_configuration_cannot_make_the_listener_public(string variant)
    {
        var connectionString = await MigratedDatabase();
        var password = Password(connectionString);
        var start = CreateStart(connectionString);
        const string publicAddress = "http://0.0.0.0:0";
        if (variant == "environment-urls") start.Environment["ASPNETCORE_URLS"] = publicAddress;
        if (variant == "environment-endpoint") start.Environment["Kestrel__Endpoints__FixtureOverride__Url"] = publicAddress;
        if (variant is "argument-urls" or "argument-endpoint")
        {
            start.ArgumentList.Add(variant == "argument-urls" ? "--urls" : "--Kestrel:Endpoints:FixtureOverride:Url");
            start.ArgumentList.Add(publicAddress);
        }
        var host = OwnedHost.Start(start);
        try
        {
            var startup = await host.WaitForStartup(Ct);
            if (!startup.Listening)
            {
                Assert.Equal(1, startup.ExitCode);
                Assert.False(host.SawListeningLog, "Rejected override had already bound a listener.");
                return;
            }
            await host.WaitUntilApplicationStarted(Ct);
            var address = AssertLoopback(host, startup.Address);
            Assert.NotEqual(5100, address.Port);
            using var http = NewClient(address);
            await AssertHealth(http, "/health/ready", HttpStatusCode.OK, connectionString, password);
            AssertLoopback(host, startup.Address);
        }
        finally
        {
            await host.DisposeAsync();
            AssertSafe(host.CapturedText, connectionString, password);
        }
    }

    [Fact]
    public async Task Runtime_database_exception_is_private_and_readiness_recovers()
    {
        var connectionString = await MigratedDatabase();
        var password = Password(connectionString);
        var host = OwnedHost.Start(CreateStart(connectionString));
        try
        {
            var startup = await host.WaitForStartup(Ct);
            Assert.True(startup.Listening);
            await host.WaitUntilApplicationStarted(Ct);
            using var http = NewClient(AssertLoopback(host, startup.Address));
            await AssertHealth(http, "/health/ready", HttpStatusCode.OK, connectionString, password);
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(Ct);
            await using (var failure = new NpgsqlCommand("""
                ALTER TABLE processing.schema_migrations RENAME TO fixture_journal;
                CREATE FUNCTION processing.fixture_probe_failure() RETURNS integer LANGUAGE plpgsql AS
                    $$ BEGIN RAISE EXCEPTION 'SYNTHETIC_HOST_PRIVATE_MARKER'; END $$;
                CREATE VIEW processing.schema_migrations AS
                    SELECT processing.fixture_probe_failure() AS version, ''::text AS name, ''::text AS checksum;
                """, connection)) await failure.ExecuteNonQueryAsync(Ct);
            await AssertHealth(http, "/health/ready", HttpStatusCode.ServiceUnavailable, connectionString, password);
            await AssertHealth(http, "/health/live", HttpStatusCode.OK, connectionString, password);
            await using (var repair = new NpgsqlCommand("""
                DROP VIEW processing.schema_migrations;
                DROP FUNCTION processing.fixture_probe_failure();
                ALTER TABLE processing.fixture_journal RENAME TO schema_migrations;
                """, connection)) await repair.ExecuteNonQueryAsync(Ct);
            await AssertHealth(http, "/health/ready", HttpStatusCode.OK, connectionString, password);
        }
        finally
        {
            await host.DisposeAsync();
            AssertSafe(host.CapturedText, connectionString, password);
            Assert.False(host.CapturedText.Contains("fixture_probe_failure", StringComparison.Ordinal));
        }
    }

    private async Task<string> MigratedDatabase()
    {
        var connectionString = await fixture.CreateDatabaseAsync(Ct);
        await new MigrationRunner().ApplyAsync(connectionString, Ct);
        return connectionString;
    }

    [Fact]
    public async Task Changed_endpoint_file_after_startup_cannot_add_a_listener()
    {
        var connectionString = await MigratedDatabase();
        var ownedRoot = Directory.CreateTempSubdirectory("wainery-host-reload-");
        try
        {
            var configPath = Path.Combine(ownedRoot.FullName, "appsettings.json");
            await File.WriteAllTextAsync(configPath, "{}", Ct);
            var start = CreateStart(connectionString);
            start.ArgumentList.Add("--contentRoot");
            start.ArgumentList.Add(ownedRoot.FullName);
            var host = OwnedHost.Start(start);
            try
            {
                var startup = await host.WaitForStartup(Ct);
                Assert.True(startup.Listening);
                await host.WaitUntilApplicationStarted(Ct);
                using var original = NewClient(AssertLoopback(host, startup.Address));
                await AssertHealth(original, "/health/ready", HttpStatusCode.OK, connectionString, Password(connectionString));
                // Prove a reload bypass safely with a second LOOPBACK address;
                // never publish a public listener, even during the initial RED.
                var reservation = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
                reservation.Start();
                var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
                reservation.Stop();
                var extra = new Uri($"http://127.0.0.1:{port}");
                var changed = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Kestrel = new { Endpoints = new { LateOverride = new { Url = extra.AbsoluteUri } } }
                });
                await File.WriteAllTextAsync(configPath, changed, Ct);
                using var unexpected = NewClient(extra);
                var window = Stopwatch.StartNew();
                var bound = false;
                while (window.Elapsed < TimeSpan.FromSeconds(8))
                {
                    try
                    {
                        using var response = await unexpected.GetAsync("/health/live", Ct);
                        bound = response.StatusCode == HttpStatusCode.OK;
                        if (bound) break;
                    }
                    catch (HttpRequestException) { }
                    await Task.Delay(100, Ct);
                }
                Assert.False(bound, "Changed endpoint file added an unapproved listener after startup.");
                await AssertHealth(original, "/health/ready", HttpStatusCode.OK, connectionString, Password(connectionString));
            }
            finally
            {
                await host.DisposeAsync();
                AssertSafe(host.CapturedText, connectionString, Password(connectionString));
            }
        }
        finally
        {
            var temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!ownedRoot.FullName.StartsWith(temp, StringComparison.OrdinalIgnoreCase)
                || !ownedRoot.Name.StartsWith("wainery-host-reload-", StringComparison.Ordinal)
                || (ownedRoot.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Owned test directory cleanup boundary changed.");
            ownedRoot.Delete(recursive: true);
        }
    }

    private static async Task SetJournal(string connectionString, bool valid)
    {
        var migration = MigrationRunner.BuiltInMigrations.Last();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Ct);
        await using var update = new NpgsqlCommand("UPDATE processing.schema_migrations SET name=@name,checksum=@checksum WHERE version=@version", connection);
        update.Parameters.AddWithValue("name", valid ? migration.Name : PrivateMarker);
        update.Parameters.AddWithValue("checksum", valid ? migration.Checksum : new string('f', 64));
        update.Parameters.AddWithValue("version", migration.Version);
        Assert.Equal(1, await update.ExecuteNonQueryAsync(Ct));
    }

    private static async Task AssertRejected(string? connectionString, string listen = DynamicLoopback, string? password = null)
    {
        var host = OwnedHost.Start(CreateStart(connectionString, listen));
        try
        {
            var startup = await host.WaitForStartup(Ct);
            Assert.False(startup.Listening || host.SawListeningLog,
                "Host started listening despite invalid required startup configuration.");
            Assert.Equal(1, startup.ExitCode);
        }
        finally
        {
            await host.DisposeAsync();
            AssertSafe(host.CapturedText, connectionString, password);
        }
    }

    private static async Task AssertHealth(HttpClient client, string path, HttpStatusCode expected, string connectionString, string? password)
    {
        using var response = await client.GetAsync(path, Ct);
        var body = await response.Content.ReadAsStringAsync(Ct);
        AssertSafe(body, connectionString, password);
        Assert.Equal(expected, response.StatusCode);
        var expectedBody = expected == HttpStatusCode.OK ? "Healthy" : "Unhealthy";
        Assert.True(string.Equals(expectedBody, body.Trim(), StringComparison.Ordinal),
            "Health response did not contain the expected safe built-in status.");
    }

    private static HttpClient NewClient(Uri address)
        => new(new HttpClientHandler { UseProxy = false }) { BaseAddress = address, Timeout = TimeSpan.FromSeconds(5) };

    private static Uri AssertLoopback(OwnedHost host, Uri? address)
    {
        Assert.NotNull(address);
        Assert.True(host.ListeningAddresses.Count > 0, "No owned dynamic listening URL was discovered.");
        Assert.True(host.ListeningAddresses.All(uri => uri.IsLoopback && uri.Scheme == Uri.UriSchemeHttp && uri.Port > 0),
            "Host announced a nonloopback, non-HTTP or unresolved listener.");
        return address!;
    }

    private static string? Password(string connectionString) => new NpgsqlConnectionStringBuilder(connectionString).Password;

    private static void AssertSafe(string text, string? connectionString, string? password)
    {
        Assert.False(text.Contains(PrivateMarker, StringComparison.Ordinal), "Private marker appeared in Host output/response.");
        if (!string.IsNullOrEmpty(connectionString))
            Assert.False(text.Contains(connectionString, StringComparison.Ordinal), "Connection string appeared in Host output/response.");
        if (!string.IsNullOrEmpty(password))
            Assert.False(text.Contains(password, StringComparison.Ordinal), "Password appeared in Host output/response.");
    }

    private static ProcessStartInfo CreateStart(string? connectionString, string listen = DynamicLoopback)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        var configuration = root.Parent!.Name;
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "wAInery.slnx"))) root = root.Parent;
        if (root is null) throw new InvalidOperationException("Repository root not found.");
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = root.FullName
        };
        start.ArgumentList.Add(Path.Combine(root.FullName, "src/Host/bin", configuration, "net10.0/wAInery.Host.dll"));
        // Remove inherited listener knobs only in the child environment, then
        // introduce a single explicit adversarial override in its named test.
        foreach (var key in start.Environment.Keys.Where(key =>
            key.Contains("Kestrel", StringComparison.OrdinalIgnoreCase) ||
            key is "ASPNETCORE_URLS" or "DOTNET_URLS" or "URLS" or "ASPNETCORE_HTTP_PORTS" or "ASPNETCORE_HTTPS_PORTS").ToArray())
            start.Environment.Remove(key);
        if (connectionString is null) start.Environment.Remove("WAINERY_DB_CONNECTION");
        else start.Environment["WAINERY_DB_CONNECTION"] = connectionString;
        start.Environment["WAINERY_LISTEN_URL"] = listen;
        start.Environment["DOTNET_ENVIRONMENT"] = "Production";
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        start.Environment["Logging__LogLevel__Default"] = "Information";
        start.Environment["Logging__LogLevel__Microsoft.Hosting.Lifetime"] = "Information";
        start.Environment["Logging__Console__FormatterName"] = "simple";
        start.Environment["Logging__Console__FormatterOptions__ColorBehavior"] = "Disabled";
        return start;
    }

    private sealed record Startup(bool Listening, Uri? Address, int? ExitCode);

    private sealed class OwnedHost : IAsyncDisposable
    {
        private readonly Process process;
        private readonly ConcurrentQueue<string> lines = new();
        private readonly ConcurrentQueue<Uri> addresses = new();
        private readonly TaskCompletionSource<Uri?> listening = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> applicationStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int sawListening;
        private bool disposed;
        public string CapturedText => string.Join(Environment.NewLine, lines);
        public IReadOnlyCollection<Uri> ListeningAddresses => addresses.ToArray();
        public bool SawListeningLog => Volatile.Read(ref sawListening) != 0;

        private OwnedHost(ProcessStartInfo start)
        {
            process = new Process { StartInfo = start };
            process.OutputDataReceived += (_, data) => Capture(data.Data);
            process.ErrorDataReceived += (_, data) => Capture(data.Data);
        }

        public static OwnedHost Start(ProcessStartInfo start)
        {
            var owned = new OwnedHost(start);
            var started = false;
            try
            {
                if (!owned.process.Start()) throw new InvalidOperationException("Owned Host process did not start.");
                started = true;
                owned.process.BeginOutputReadLine();
                owned.process.BeginErrorReadLine();
                return owned;
            }
            catch
            {
                try { if (started && !owned.process.HasExited) owned.process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) when (owned.process.HasExited) { }
                owned.process.Dispose();
                throw;
            }
        }

        private void Capture(string? line)
        {
            if (line is null) return;
            lines.Enqueue(line);
            const string marker = "Now listening on:";
            var offset = line.IndexOf(marker, StringComparison.Ordinal);
            if (offset >= 0)
            {
                Interlocked.Exchange(ref sawListening, 1);
                Uri.TryCreate(line[(offset + marker.Length)..].Trim(), UriKind.Absolute, out var address);
                if (address is not null) addresses.Enqueue(address);
                listening.TrySetResult(address);
            }
            if (line.Contains("Application started.", StringComparison.Ordinal)) applicationStarted.TrySetResult(true);
        }

        public async Task<Startup> WaitForStartup(CancellationToken ct)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            var exited = process.WaitForExitAsync(timeout.Token);
            var completed = await Task.WhenAny(exited, listening.Task).WaitAsync(timeout.Token);
            if (completed == listening.Task) return new(true, await listening.Task, null);
            await exited;
            return new(false, null, process.ExitCode);
        }

        public async Task WaitUntilApplicationStarted(CancellationToken ct)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            var exited = process.WaitForExitAsync(timeout.Token);
            var completed = await Task.WhenAny(exited, applicationStarted.Task).WaitAsync(timeout.Token);
            if (completed == exited)
            {
                await exited;
                throw new InvalidOperationException("Owned Host exited before application startup completed.");
            }
            await applicationStarted.Task;
        }

        public async ValueTask DisposeAsync()
        {
            if (disposed) return;
            disposed = true;
            try
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) when (process.HasExited) { }
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await process.WaitForExitAsync(cleanup.Token);
            }
            finally { process.Dispose(); }
        }
    }
}
