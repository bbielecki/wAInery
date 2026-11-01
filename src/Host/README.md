# Host (integration-owner boundary)

The MVP uses one deployable .NET process for API/CLI entry points and durable worker orchestration; individual domain agents must not edit host composition independently.

Gate1 implements technical startup/readiness only (#32). Set WAINERY_DB_CONNECTION
privately, explicitly run `dotnet run --project src/Host --configuration Release -- --migrate`,
then `dotnet run --project src/Host --configuration Release --no-launch-profile`.
Normal startup never migrates: current compiled journal plus vector/btree_gist
extensions required. HTTP loopback only, default127.0.0.1:5100;
WAINERY_LISTEN_URL can select another HTTP loopback URL, including127.0.0.1:0.
Kestrel:Endpoints configuration is rejected and endpoint reload disabled to prevent
overriding the binding at startup or by later file changes.

Built-in ASP.NET Core health: /health/live→200 Healthy independent of DB after
startup; /health/ready→200 Healthy or503 Unhealthy, bounded read-only journal and
extension probe. No database exception/credentials in health output. This does
not certify every physical table or business decision readiness. No owner
consumer/worker is registered before its approved implementation milestone.
