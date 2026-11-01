Schema compatibility and provider/consumer tests; fixtures are synthetic and license-safe.

Run `dotnet test --project tests/Contracts/wAInery.Contracts.Tests.csproj --configuration Release`
or `dotnet test --solution wAInery.slnx --configuration Release`. The repository
selects the native Microsoft.Testing.Platform runner for .NET 10 in global.json.
Tests include producer JSON Schema evaluation and strict scoped event consumers.
