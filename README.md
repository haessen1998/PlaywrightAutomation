# PlaywrightAutomation

PlaywrightAutomation 是一个为 .NET 应用封装的 Playwright 库，便于通过依赖注入与托管服务在应用中执行浏览器自动化 / 测试任务。

## 安装

``` bash
dotnet add package Haessen.PlaywrightAutomation --version 1.57.0
```

## 快速开始（示例）

下面示例展示了典型的注册与使用方式，基于 `Program.cs` / `Startup.cs` 和一个控制器示例。

`Program.cs`（或 `Startup.cs`）：

``` csharp
var builder = WebApplication.CreateBuilder(args);

// 注册 Playwright 服务
builder.AddPlaywrightDefaults();
builder.Services.AddControllers();

var app = builder.Build(); 
app.MapControllers(); 
app.Run();

```

示例控制器：

``` csharp

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
            var result = await playwrightService.GetElement(url, selector, string.Empty, attribute);
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

```

## 文档与仓库

更多文档请参见仓库主页：https://github.com/haessen1998/PlaywrightAutomation

## 许可证

本项目使用 MIT 许可证（详见 `LICENSE.txt`）。