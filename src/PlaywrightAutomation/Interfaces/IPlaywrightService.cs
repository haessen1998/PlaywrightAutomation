using Microsoft.Playwright;
using PlaywrightAutomation.Data;

namespace PlaywrightAutomation.Interfaces;

public interface IPlaywrightService
{
    Task EnsureInstalledAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellation = default);

    Task<IBrowser> GetBrowserAsync();

    Task<IBrowser> GetCustomBrowserAsync(
        bool headless = true,
        PlaywrightConnectionMode mode = PlaywrightConnectionMode.Default,
        string? server = "http://localhost:9222/",
        string? channel = "chrome",
        string[]? args = null,
        int slow = 100,
        bool persistent = false);

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
