using Microsoft.Playwright;

namespace PlaywrightAutomation.Core.Interfaces;

public interface IPlaywrightService
{
    Task EnsureInstalledAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellation = default);

    Task<IBrowser> GetBrowserAsync();

    ValueTask<List<string>> GetElementList(
        string url,
        string selector,
        string attribute = "href",
        string? cookies = null);

    ValueTask<string?> GetElement(
        string url,
        string selector,
        string? readyText,
        WaitForSelectorState state = WaitForSelectorState.Visible,
        string attribute = "text",
        string? cookies = null);
}
