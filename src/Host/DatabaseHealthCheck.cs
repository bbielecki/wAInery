using Microsoft.Extensions.Diagnostics.HealthChecks;
using wAInery.Persistence;

namespace wAInery.Host;

internal sealed class DatabaseHealthCheck(MigrationCompatibilityProbe probe) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await probe.IsCompatibleAsync(cancellationToken)
                ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Do not attach Npgsql/configuration exceptions to health logs/results.
            return HealthCheckResult.Unhealthy();
        }
    }
}
