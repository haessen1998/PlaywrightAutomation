using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace PlaywrightAutomation.Core.Extensions;

/// <summary>
/// Playwright 扩展方法
/// </summary>
public static class PlaywrightExtensions
{

    /// <summary>
    /// 
    /// </summary>
    /// <param name="page"></param>
    /// <param name="selector"></param>
    /// <param name="url"></param>
    /// <param name="attribute"></param>
    /// <param name="pageTimeout"></param>
    /// <param name="elementTimeout"></param>
    /// <returns></returns>
    public static async ValueTask<List<string>> GetElementList(this IPage page,
        string selector,
        string attribute = "href",
        int elementTimeout = 60000)
    {
        List<string> results = new();

        await page.WaitForSelectorAsync(selector, new()
        {
            Timeout = elementTimeout
        });

        // 获取所有匹配元素
        var elements = await page.QuerySelectorAllAsync(selector);

        // 遍历每个元素获取属性值
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


    /// <summary>
    /// 
    /// </summary>
    /// <param name="page"></param>
    /// <param name="selector"></param>
    /// <param name="readyText"></param>
    /// <param name="attribute"></param>
    /// <param name="elementTimeout"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static async ValueTask<string?> GetElement(this IPage page,
        string selector,
        string? readyText,
        WaitForSelectorState state = WaitForSelectorState.Visible,
        string attribute = "text",
        int elementTimeout = 60000)
    {
        var element = await page.WaitForSelectorAsync(selector, new()
        {
            Timeout = elementTimeout,
            State= state
        });

        if (element == null) throw new Exception("element not ready");

        string? value = attribute switch
        {
            "html" => await element.InnerHTMLAsync(),
            "text" => await element.InnerTextAsync(),
            "src" => await element.GetAttributeAsync("src"),
            "href" => await element.GetAttributeAsync("href"),
            _ => await element.GetAttributeAsync(attribute)
        };

        if (string.IsNullOrWhiteSpace(value)) throw new Exception("element not ready");

        if (!string.IsNullOrWhiteSpace(readyText) && !value.Contains(readyText)) throw new Exception("element not ready");

        return value;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="page"></param>
    /// <param name="fullPage"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public static async Task<string> TakeScreenshotAsync(this IPage page, 
        bool fullPage = true, 
        string? path = null)
    {
        var directory= !string.IsNullOrWhiteSpace(path)? 
            System.IO.Path.GetDirectoryName(path):
            Path.Combine(AppContext.BaseDirectory, "Screenshots");

        if (!string.IsNullOrWhiteSpace(directory) && !System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        var savePath = !string.IsNullOrWhiteSpace(path) ? path :
            Path.Combine(directory!, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = fullPage,
            Path = savePath
        });

        return savePath;
    }
}
