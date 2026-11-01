using wAInery.Persistence;

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

// Local-only host; business endpoints and runtime workers are later increments.
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:5100");
var app = builder.Build();
app.MapGet("/health/live", () => Results.Ok(new { status = "ok", service = "wAInery", phase = "scaffold" }));
app.Run();
