// Wave 0 local-only host; no external public endpoints, business logic or adapters yet.
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:5100");
var app = builder.Build();
app.MapGet("/health/live", () => Results.Ok(new { status = "ok", service = "wAInery", phase = "scaffold" }));
app.Run();
