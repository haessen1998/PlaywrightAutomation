using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using PlaywrightAutomation.Data;

namespace PlaywrightAutomation.Decorators;

public class PlaywrightLoggingDecorator(
    Interfaces.IPlaywrightService inner,
    ILogger<PlaywrightLoggingDecorator> logger) : Interfaces.IPlaywrightService
{
    public async Task EnsureInstalledAsync(IProgress<string>? progress = null, CancellationToken cancellation = default)
    {
        await inner.EnsureInstalledAsync(progress, cancellation);

        logger.LogDebug("Ensure installed");
    }

    public async Task<IBrowser> GetBrowserAsync()
    {
        logger.LogDebug("Get broswer");

        var result = await inner.GetBrowserAsync();

        return result;
    }

    public async Task<IBrowser> GetCustomBrowserAsync(
        bool headless = true, 
        PlaywrightConnectionMode mode = PlaywrightConnectionMode.Default, 
        string? server = "http://localhost:9222/", 
        string? channel = "chrome", 
        string[]? args = null, 
        int slow = 100, 
        bool persistent = false)
    {
        logger.LogDebug("Get custom broswer");

        var result = await inner.GetCustomBrowserAsync(headless, mode, server, channel, args, slow, persistent);

        return result;
    }

    public async ValueTask<string?> GetElement(
        string url, 
        string selector, 
        string? readyText,
        WaitForSelectorState state = WaitForSelectorState.Visible,
        string attribute = "text",
        string? cookies = null)
    {
        logger.LogInformation("Getting element from url: {url}, selector: {selector}", url, selector);

        var result = await inner.GetElement(url, selector, readyText, state, attribute, cookies);

        logger.LogInformation("Get element: {element}", result);

        return result;
    }

    public async ValueTask<List<string>> GetElementList(
        string url, 
        string selector, 
        string attribute = "href",
        string? cookies = null)
    {
        logger.LogInformation("Getting elements from url: {url}, selector: {selector}", url, selector);

        var result = await inner.GetElementList(url, selector, attribute, cookies);

        logger.LogInformation("Get elements: \n{element}", string.Join("\n", result));

        return result;
    }
}

