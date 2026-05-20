using Microsoft.Playwright;
using PlaywrightAutomation.Exceptions;

namespace PlaywrightAutomation.Extensions;

/// <summary>
/// Playwright 扩展方法
/// </summary>
public static class PlaywrightExtensions
{
    public static async ValueTask<List<string>> GetElementList(
        this IPage page,
        string selector,
        string attribute = "href",
        int elementTimeout = 60_000)
    {
        List<string> results = new();

        try
        {
            await page.WaitForSelectorAsync(selector, new()
            {
                Timeout = elementTimeout
            });
        }
        catch (PlaywrightException ex)
        {
            throw new AutomationException(
                $"Element list selector was not ready: {selector}.",
                AutomationFailureCategory.ElementNotFound,
                true,
                ex);
        }

        var elements = await page.QuerySelectorAllAsync(selector);

        foreach (var element in elements)
        {
            string? value = attribute switch
            {
                "html" => await element.InnerHTMLAsync(),
                "text" => await element.InnerTextAsync(),
                "src" => await element.GetAttributeAsync("src"),
                "href" => await element.GetAttributeAsync("href"),
                _ => await element.GetAttributeAsync(attribute)
            };

            results.Add(value ?? string.Empty);
        }

        return results;
    }

    public static async ValueTask<string?> GetElement(
        this IPage page,
        string selector,
        string? readyText,
        WaitForSelectorState state = WaitForSelectorState.Visible,
        string attribute = "text",
        int elementTimeout = 60_000)
    {
        IElementHandle? element;

        try
        {
            element = await page.WaitForSelectorAsync(selector, new()
            {
                Timeout = elementTimeout,
                State = state
            });
        }
        catch (PlaywrightException ex)
        {
            throw new AutomationException(
                $"Element selector was not ready: {selector}.",
                AutomationFailureCategory.ElementNotFound,
                true,
                ex);
        }

        if (element == null)
        {
            throw new AutomationException(
                $"Element selector was not ready: {selector}.",
                AutomationFailureCategory.ElementNotFound,
                true);
        }

        string? value = attribute switch
        {
            "html" => await element.InnerHTMLAsync(),
            "text" => await element.InnerTextAsync(),
            "src" => await element.GetAttributeAsync("src"),
            "href" => await element.GetAttributeAsync("href"),
            _ => await element.GetAttributeAsync(attribute)
        };

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new AutomationException(
                $"Element value was empty: {selector}.",
                AutomationFailureCategory.ValidationFailed,
                true);
        }

        if (!string.IsNullOrWhiteSpace(readyText) && !value.Contains(readyText))
        {
            throw new AutomationException(
                $"Element value did not contain the expected ready text: {selector}.",
                AutomationFailureCategory.ValidationFailed,
                true);
        }

        return value;
    }

    public static async Task<string> TakeScreenshotAsync(
        this IPage page,
        bool fullPage = true,
        string? path = null)
    {
        var directory = !string.IsNullOrWhiteSpace(path)
            ? Path.GetDirectoryName(path)
            : Path.Combine(AppContext.BaseDirectory, "Screenshots");

        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var savePath = !string.IsNullOrWhiteSpace(path)
            ? path
            : Path.Combine(directory!, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = fullPage,
            Path = savePath
        });

        return savePath;
    }

    public static async Task WaitUntilAsync(
        Func<Task<bool>> predicate,
        TimeSpan timeout,
        TimeSpan interval,
        string timeoutMessage)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await predicate())
            {
                return;
            }

            await Task.Delay(interval);
        }

        throw new AutomationException(
            timeoutMessage,
            AutomationFailureCategory.PageTimeout,
            true);
    }
}
