using Microsoft.Playwright;
using PlaywrightAutomation.Data;

namespace PlaywrightAutomation.Interfaces;

public interface IPlaywrightService : IAsyncDisposable
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

    Task<T> RunPageAsync<T>(
        Func<IPage, CancellationToken, Task<T>> action,
        string? url = null,
        IEnumerable<Cookie>? cookies = null,
        BrowserNewContextOptions? contextOptions = null,
        CancellationToken cancellation = default);

    Task RunPageAsync(
        Func<IPage, CancellationToken, Task> action,
        string? url = null,
        IEnumerable<Cookie>? cookies = null,
        BrowserNewContextOptions? contextOptions = null,
        CancellationToken cancellation = default);

    ValueTask<List<string>> GetElementList(
        string url,
        string selector,
        string attribute = "href",
        string? cookies = null);

    ValueTask<List<string>> GetElementList(
        string url,
        string selector,
        IEnumerable<Cookie> browserCookies,
        string attribute = "href");

    ValueTask<string?> GetElement(
        string url,
        string selector,
        string? readyText,
        WaitForSelectorState state = WaitForSelectorState.Visible,
        string attribute = "text",
        string? cookies = null);

    ValueTask<string?> GetElement(
        string url,
        string selector,
        string? readyText,
        IEnumerable<Cookie> browserCookies,
        WaitForSelectorState state = WaitForSelectorState.Visible,
        string attribute = "text");
}
