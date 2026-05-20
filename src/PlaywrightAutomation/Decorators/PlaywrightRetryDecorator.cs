using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using PlaywrightAutomation.Data;
using PlaywrightAutomation.Exceptions;
using PlaywrightAutomation.Interfaces;

namespace PlaywrightAutomation.Decorators;

public class PlaywrightRetryDecorator(
    IPlaywrightService inner,
    IOptions<RetryOptions> options,
    ILogger<PlaywrightRetryDecorator> logger) : IPlaywrightService
{
    public Task EnsureInstalledAsync(IProgress<string>? progress = null, CancellationToken cancellation = default)
    {
        return ExecuteAsync(
            () => inner.EnsureInstalledAsync(progress, cancellation),
            nameof(EnsureInstalledAsync),
            cancellation);
    }

    public Task<IBrowser> GetBrowserAsync()
    {
        return ExecuteAsync(inner.GetBrowserAsync, nameof(GetBrowserAsync));
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
        return ExecuteAsync(
            () => inner.GetCustomBrowserAsync(headless, mode, server, channel, args, slow, persistent),
            nameof(GetCustomBrowserAsync));
    }

    public Task<T> RunPageAsync<T>(
        Func<IPage, CancellationToken, Task<T>> action,
        string? url = null,
        IEnumerable<Cookie>? cookies = null,
        BrowserNewContextOptions? contextOptions = null,
        CancellationToken cancellation = default)
    {
        return ExecuteAsync(
            () => inner.RunPageAsync(action, url, cookies, contextOptions, cancellation),
            nameof(RunPageAsync),
            cancellation);
    }

    public Task RunPageAsync(
        Func<IPage, CancellationToken, Task> action,
        string? url = null,
        IEnumerable<Cookie>? cookies = null,
        BrowserNewContextOptions? contextOptions = null,
        CancellationToken cancellation = default)
    {
        return ExecuteAsync(
            () => inner.RunPageAsync(action, url, cookies, contextOptions, cancellation),
            nameof(RunPageAsync),
            cancellation);
    }

    public ValueTask<string?> GetElement(
        string url,
        string selector,
        string? readyText,
        WaitForSelectorState state = WaitForSelectorState.Visible,
        string attribute = "text",
        string? cookies = null)
    {
        return new ValueTask<string?>(ExecuteAsync(
            async () => await inner.GetElement(url, selector, readyText, state, attribute, cookies),
            nameof(GetElement)));
    }

    public ValueTask<string?> GetElement(
        string url,
        string selector,
        string? readyText,
        IEnumerable<Cookie> browserCookies,
        WaitForSelectorState state = WaitForSelectorState.Visible,
        string attribute = "text")
    {
        return new ValueTask<string?>(ExecuteAsync(
            async () => await inner.GetElement(url, selector, readyText, browserCookies, state, attribute),
            nameof(GetElement)));
    }

    public ValueTask<List<string>> GetElementList(
        string url,
        string selector,
        string attribute = "href",
        string? cookies = null)
    {
        return new ValueTask<List<string>>(ExecuteAsync(
            async () => await inner.GetElementList(url, selector, attribute, cookies),
            nameof(GetElementList)));
    }

    public ValueTask<List<string>> GetElementList(
        string url,
        string selector,
        IEnumerable<Cookie> browserCookies,
        string attribute = "href")
    {
        return new ValueTask<List<string>>(ExecuteAsync(
            async () => await inner.GetElementList(url, selector, browserCookies, attribute),
            nameof(GetElementList)));
    }

    public ValueTask DisposeAsync()
    {
        return inner.DisposeAsync();
    }

    private async Task ExecuteAsync(Func<Task> action, string operation, CancellationToken cancellation = default)
    {
        await ExecuteAsync(
            async () =>
            {
                await action();
                return true;
            },
            operation,
            cancellation);
    }

    private async Task<T> ExecuteAsync<T>(Func<Task<T>> action, string operation, CancellationToken cancellation = default)
    {
        var retry = options.Value;
        var maxRetries = Math.Max(1, retry.MaxRetries);
        Exception? lastError = null;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            cancellation.ThrowIfCancellationRequested();

            try
            {
                return await action();
            }
            catch (Exception ex) when (IsRetryable(ex))
            {
                lastError = ex;

                if (attempt >= maxRetries)
                {
                    break;
                }

                logger.LogWarning(ex, "{operation} attempt {attempt}/{maxRetries} failed with retryable automation error.", operation, attempt, maxRetries);
                await Task.Delay(Math.Max(0, retry.RetryIntervalMs) * attempt, cancellation);
            }
            catch (AutomationException)
            {
                throw;
            }
        }

        throw new RetryFailedException($"{operation} failed after {maxRetries} attempt(s).", lastError);
    }

    private static bool IsRetryable(Exception ex)
    {
        return ex is AutomationException { Retryable: true };
    }
}
