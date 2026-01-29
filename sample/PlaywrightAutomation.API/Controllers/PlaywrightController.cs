using Microsoft.AspNetCore.Mvc;
using Microsoft.Playwright;
using PlaywrightAutomation.Interfaces;

namespace PlaywrightAutomation.API.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class PlaywrightController(
    ILogger<PlaywrightController> logger,
    IPlaywrightService playwrightService) : ControllerBase
{
    #region 获取元素

    /// <summary>
    /// 获取指定 URL 的元素列表
    /// </summary>
    /// <param name="url">目标 URL</param>
    /// <param name="selector">CSS 选择器</param>
    /// <param name="attribute">属性名称（如 href 或 text）</param>
    /// <returns>元素列表</returns>
    [HttpGet]
    public async Task<IActionResult> GetElementList(string url, string selector, string attribute)
    {
        try
        {
            var result = await playwrightService.GetElementList(url, selector, attribute);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取元素列表失败");
            return StatusCode(500, $"获取元素列表失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取指定 URL 的元素内容
    /// </summary>
    /// <param name="url">目标 URL</param>
    /// <param name="selector">CSS 选择器</param>
    /// <param name="attribute">属性名称（如 html 或 text）</param>
    /// <returns>元素内容</returns>
    [HttpGet]
    public async Task<IActionResult> GetElement(string url, string selector, string attribute)
    {
        try
        {
            var result = await playwrightService.GetElement(url, selector, string.Empty, attribute: attribute);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取元素内容失败");
            return StatusCode(500, $"获取元素内容失败: {ex.Message}");
        }
    }
    #endregion

}
