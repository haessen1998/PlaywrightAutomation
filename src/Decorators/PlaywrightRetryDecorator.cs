using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace PlaywrightAutomation.Decorators;

public class PlaywrightRetryDecorator(
    Interfaces.IPlaywrightService inner,
    IOptions<Data.RetryOptions> options,
    ILogger<PlaywrightRetryDecorator> logger) : Interfaces.IPlaywrightService
{
    public async Task EnsureInstalledAsync(IProgress<string>? progress = null, CancellationToken cancellation = default)
    {
        var retry = options.Value;

        Exception? lastError = null;

        for (int attempt = 1; attempt <= retry.MaxRetries; attempt++)
        {
            try
            {
                await inner.EnsureInstalledAsync();
            }
            catch (Exception ex) when (IsRetryable(ex))
            {
                lastError = ex;

                logger.LogWarning(ex, "尝试{attempt}/{maxRetries}失败: {message}", attempt, retry.MaxRetries, ex.Message);

                await Task.Delay(retry.RetryIntervalMs * attempt);
            }
        }

        // 所有重试失败后抛出
        throw new Exceptions.RetryFailedException($"经过{retry.MaxRetries}次重试失败", lastError);
    }

    public async Task<IBrowser> GetBrowserAsync()
    {
        var retry = options.Value;

        Exception? lastError = null;

        for (int attempt = 1; attempt <= retry.MaxRetries; attempt++)
        {
            try
            {
                var result = await inner.GetBrowserAsync();

                return result;
            }
            catch (Exception ex) when (IsRetryable(ex))
            {
                lastError = ex;

                logger.LogWarning(ex, "尝试{attempt}/{maxRetries}失败: {message}", attempt, retry.MaxRetries, ex.Message);

                await Task.Delay(retry.RetryIntervalMs * attempt);
            }
        }

        // 所有重试失败后抛出
        throw new Exceptions.RetryFailedException($"经过{retry.MaxRetries}次重试失败", lastError);
    }

    public async ValueTask<string?> GetElement(
        string url, 
        string selector, 
        string? readyText,
        WaitForSelectorState state = WaitForSelectorState.Visible,
        string attribute = "text",
        string? cookies = null)
    {
        var retry = options.Value;

        Exception? lastError = null;

        for (int attempt = 1; attempt <= retry.MaxRetries; attempt++)
        {
            try
            {
                var result = await inner.GetElement(url, selector, readyText,state, attribute, cookies);

                return result;
            }
            catch (Exception ex) when (IsRetryable(ex))
            {
                lastError = ex;

                logger.LogWarning(ex, "尝试{attempt}/{maxRetries}失败: {message}", attempt, retry.MaxRetries, ex.Message);

                await Task.Delay(retry.RetryIntervalMs * attempt);
            }
        }

        // 所有重试失败后抛出
        throw new Exceptions.RetryFailedException($"经过{retry.MaxRetries}次重试失败", lastError);
    }

    public async ValueTask<List<string>> GetElementList(
        string url, 
        string selector, 
        string attribute = "href",
        string? cookies = null)
    {
        var retry = options.Value;

        Exception? lastError = null;

        for (int attempt = 1; attempt <= retry.MaxRetries; attempt++)
        {
            try
            {
                var result = await inner.GetElementList(url, selector, attribute, cookies);

                return result;
            }
            catch (Exception ex) when (IsRetryable(ex))
            {
                lastError = ex;

                logger.LogWarning(ex, "尝试{attempt}/{maxRetries}失败: {message}", attempt, retry.MaxRetries, ex.Message);

                await Task.Delay(retry.RetryIntervalMs * attempt);
            }
        }

        // 所有重试失败后抛出
        throw new Exceptions.RetryFailedException($"经过{retry.MaxRetries}次重试失败", lastError);

    }

    /// <summary>
    /// 判定是否需要重试的异常
    /// </summary>
    private bool IsRetryable(Exception ex)
    {
        return ex is TimeoutException ||
               ex.Message.Contains("element not ready") ||
               ex.Message.Contains("element not visible") ||
               ex.Message.Contains("navigation timeout") ||
               ex.Message.Contains("page closed");
    }

}
