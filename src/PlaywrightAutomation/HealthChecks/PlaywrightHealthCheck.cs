using Microsoft.Extensions.Diagnostics.HealthChecks;
using PlaywrightAutomation.Interfaces;

namespace PlaywrightAutomation.HealthChecks;

public sealed class PlaywrightHealthCheck(IPlaywrightService playwrightService) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var browser = await playwrightService.GetBrowserAsync();

            return browser.IsConnected
                ? HealthCheckResult.Healthy("Playwright browser is connected.")
                : HealthCheckResult.Unhealthy("Playwright browser is not connected.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Playwright browser check failed.", ex);
        }
    }
}
