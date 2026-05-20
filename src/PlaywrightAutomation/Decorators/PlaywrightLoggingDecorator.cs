using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using PlaywrightAutomation.Data;
using PlaywrightAutomation.Interfaces;

namespace PlaywrightAutomation.Decorators;

public class PlaywrightLoggingDecorator(
    IPlaywrightService inner,
    ILogger<PlaywrightLoggingDecorator> logger) : IPlaywrightService
{
    public Task EnsureInstalledAsync(IProgress<string>? progress = null, CancellationToken cancellation = default)
    {
        logger.LogDebug("Ensuring Playwright dependencies are installed.");
        return inner.EnsureInstalledAsync(progress, cancellation);
    }

    public Task<IBrowser> GetBrowserAsync()
    {
        logger.LogDebug("Getting configured browser.");
        return inner.GetBrowserAsync();
    }

    public Task<IBrowser> GetCustomBrowserAsync(
        bool headless = true,
        PlaywrightConnectionMode mode = PlaywrightConnectionMode.Default,
        string? server = "http://localhost:9222/",
        string? channel = "chrome",
        string[]? args = null,
        int slow = 100,
        bool persistent = false)
    {
        logger.LogDebug("Getting custom browser. Mode: {mode}, Channel: {channel}, Headless: {headless}, Persistent: {persistent}", mode, channel, headless, persistent);
        return inner.GetCustomBrowserAsync(headless, mode, server, channel, args, slow, persistent);
    }

    public Task<T> RunPageAsync<T>(
        Func<IPage, CancellationToken, Task<T>> action,
        string? url = null,
        IEnumerable<Cookie>? cookies = null,
        BrowserNewContextOptions? contextOptions = null,
        CancellationToken cancellation = default)
    {
        var cookieList = cookies as IReadOnlyCollection<Cookie> ?? cookies?.ToArray();
        logger.LogDebug("Running page automation. Host: {host}, Cookies: {cookieCount}", GetHost(url), cookieList?.Count ?? 0);
        return inner.RunPageAsync(action, url, cookieList, contextOptions, cancellation);
    }

    public Task RunPageAsync(
        Func<IPage, CancellationToken, Task> action,
        string? url = null,
        IEnumerable<Cookie>? cookies = null,
        BrowserNewContextOptions? contextOptions = null,
        CancellationToken cancellation = default)
    {
        var cookieList = cookies as IReadOnlyCollection<Cookie> ?? cookies?.ToArray();
        logger.LogDebug("Running page automation. Host: {host}, Cookies: {cookieCount}", GetHost(url), cookieList?.Count ?? 0);
        return inner.RunPageAsync(action, url, cookieList, contextOptions, cancellation);
    }

    public async ValueTask<string?> GetElement(
        string url,
        string selector,
        string? readyText,
        WaitForSelectorState state = WaitForSelectorState.Visible,
        string attribute = "text",
        string? cookies = null)
    {
        logger.LogInformation("Getting element. Host: {host}, Selector: {selector}, Attribute: {attribute}", GetHost(url), selector, attribute);

        var result = await inner.GetElement(url, selector, readyText, state, attribute, cookies);

        logger.LogInformation("Element read completed. Host: {host}, Selector: {selector}, HasValue: {hasValue}, Length: {length}", GetHost(url), selector, !string.IsNullOrEmpty(result), result?.Length ?? 0);
        return result;
    }

    public async ValueTask<string?> GetElement(
        string url,
        string selector,
        string? readyText,
        IEnumerable<Cookie> browserCookies,
        WaitForSelectorState state = WaitForSelectorState.Visible,
        string attribute = "text")
    {
        var cookieList = browserCookies as IReadOnlyCollection<Cookie> ?? browserCookies.ToArray();
        logger.LogInformation("Getting element. Host: {host}, Selector: {selector}, Attribute: {attribute}, Cookies: {cookieCount}", GetHost(url), selector, attribute, cookieList.Count);

        var result = await inner.GetElement(url, selector, readyText, cookieList, state, attribute);

        logger.LogInformation("Element read completed. Host: {host}, Selector: {selector}, HasValue: {hasValue}, Length: {length}", GetHost(url), selector, !string.IsNullOrEmpty(result), result?.Length ?? 0);
        return result;
    }

    public async ValueTask<List<string>> GetElementList(
        string url,
        string selector,
        string attribute = "href",
        string? cookies = null)
    {
        logger.LogInformation("Getting elements. Host: {host}, Selector: {selector}, Attribute: {attribute}", GetHost(url), selector, attribute);

        var result = await inner.GetElementList(url, selector, attribute, cookies);

        logger.LogInformation("Element list read completed. Host: {host}, Selector: {selector}, Count: {count}", GetHost(url), selector, result.Count);
        return result;
    }

    public async ValueTask<List<string>> GetElementList(
        string url,
        string selector,
        IEnumerable<Cookie> browserCookies,
        string attribute = "href")
    {
        var cookieList = browserCookies as IReadOnlyCollection<Cookie> ?? browserCookies.ToArray();
        logger.LogInformation("Getting elements. Host: {host}, Selector: {selector}, Attribute: {attribute}, Cookies: {cookieCount}", GetHost(url), selector, attribute, cookieList.Count);

        var result = await inner.GetElementList(url, selector, cookieList, attribute);

        logger.LogInformation("Element list read completed. Host: {host}, Selector: {selector}, Count: {count}", GetHost(url), selector, result.Count);
        return result;
    }

    public ValueTask DisposeAsync()
    {
        return inner.DisposeAsync();
    }

    private static string? GetHost(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : null;
    }
}
