using wAInery.Persistence;
using wAInery.Host;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Npgsql;

if (args.Contains("--migrate", StringComparer.Ordinal))
{
    var connectionString = Environment.GetEnvironmentVariable("WAINERY_DB_CONNECTION");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.Error.WriteLine("Set WAINERY_DB_CONNECTION before running --migrate.");
        Environment.ExitCode = 1;
        return;
    }
    try
    {
        var applied = await new MigrationRunner().ApplyAsync(connectionString);
        Console.WriteLine($"Applied {applied} migration(s).");
    }
    catch (Exception error) when (error is Npgsql.NpgsqlException or InvalidOperationException or ArgumentException)
    {
        Console.Error.WriteLine($"Migration failed ({error.GetType().Name}). Verify connection settings and compiled migration history.");
        Environment.ExitCode = 1;
    }
    return;
}

try
{
    var connectionString = Environment.GetEnvironmentVariable("WAINERY_DB_CONNECTION");
    if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException();
    var configuredUrl = Environment.GetEnvironmentVariable("WAINERY_LISTEN_URL") ?? "http://127.0.0.1:5100";
    if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var listen) || listen.Scheme != Uri.UriSchemeHttp
        || !listen.IsLoopback || listen.UserInfo.Length != 0 || listen.AbsolutePath != "/"
        || listen.Query.Length != 0 || listen.Fragment.Length != 0) throw new InvalidOperationException();

    var builder = WebApplication.CreateBuilder(args);
    // Kestrel:Endpoints overrides UseUrls; reject that separate configuration path.
    if (builder.Configuration.GetSection("Kestrel:Endpoints").Exists()) throw new InvalidOperationException();
    builder.WebHost.UseUrls(listen.AbsoluteUri);
    builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
    builder.Services.AddSingleton<MigrationCompatibilityProbe>();
    builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

    await using var app = builder.Build();
    if (!await app.Services.GetRequiredService<MigrationCompatibilityProbe>().IsCompatibleAsync())
        throw new InvalidOperationException();
    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
    await app.RunAsync();
}
catch (Exception)
{
    Console.Error.WriteLine("Host failed. Verify local configuration, database and compiled migration history.");
    Environment.ExitCode = 1;
}
