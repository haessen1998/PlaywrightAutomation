# PlaywrightAutomation

PlaywrightAutomation 是一个面向 .NET 应用的 Playwright 封装库，提供依赖注入、浏览器生命周期管理、快捷页面执行、元素读取、重试、日志、依赖安装和健康检查等能力。

## 安装

```bash
dotnet add package Automation.Playwright --version 1.*.*
```

## 注册服务

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddPlaywrightDefaults();
builder.AddPlaywrightHealthCheck();

builder.Services.AddControllers();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapControllers();
app.Run();
```

也可以直接传入配置对象：

```csharp
builder.AddPlaywrightDefaults(
    new RetryOptions
    {
        MaxRetries = 3,
        RetryIntervalMs = 1000
    },
    new PlaywrightOptions
    {
        Headless = true,
        Mode = PlaywrightConnectionMode.Default,
        PageIntervalMs = 60000,
        ElementIntervalMs = 10000
    });
```

## appsettings.json 示例

```json
{
  "Retry": {
    "MaxRetries": 3,
    "RetryIntervalMs": 1000
  },
  "Playwright": {
    "Headless": true,
    "Mode": "Default",
    "Server": "",
    "Channel": "",
    "PageIntervalMs": 60000,
    "ElementIntervalMs": 10000,
    "VideoIntervalMs": 60000,
    "Slow": 0,
    "Args": [ "--no-sandbox", "--disable-gpu" ]
  }
}
```

`Mode` 支持：

- `Default`：启动 Playwright 自带 Chromium。
- `Local`：启动本机浏览器，可通过 `Channel` 指定 `msedge`、`chrome` 等。
- `Ws`：通过 WebSocket 连接远程 Playwright server。
- `Cdp`：通过 Chrome DevTools Protocol 连接浏览器。

## 浏览器生命周期

`GetBrowserAsync()` 会返回按配置创建的共享浏览器实例，适合多数 Web API 场景复用。

`GetCustomBrowserAsync()` 每次调用都会创建或连接一个独立浏览器，并由服务统一追踪和释放，适合并发启动多个浏览器、使用不同 channel、headless、CDP/WS 地址或 persistent context 的场景。

```csharp
var chrome = await playwrightService.GetCustomBrowserAsync(
    headless: true,
    mode: PlaywrightConnectionMode.Local,
    channel: "chrome");

var edge = await playwrightService.GetCustomBrowserAsync(
    headless: false,
    mode: PlaywrightConnectionMode.Local,
    channel: "msedge");
```

`IPlaywrightService` 实现了 `IAsyncDisposable`，通过依赖注入注册时会在宿主关闭时释放它创建的浏览器、persistent context 和 Playwright runtime。

## 快捷页面执行

推荐使用 `RunPageAsync` 编写更灵活的自动化逻辑。它会自动创建 browser context、注入 cookie、打开页面，并在执行完成后释放 context。

```csharp
var title = await playwrightService.RunPageAsync(
    async (page, cancellation) =>
    {
        return await page.TitleAsync();
    },
    url: "https://example.com");
```

也可以传入标准 Playwright `Cookie`：

```csharp
var cookies = new[]
{
    new Cookie
    {
        Name = "session",
        Value = "token",
        Url = "https://example.com"
    }
};

var text = await playwrightService.RunPageAsync(
    async (page, cancellation) =>
    {
        return await page.Locator("body").InnerTextAsync();
    },
    url: "https://example.com",
    cookies: cookies);
```

## 元素读取

保留了常用的快捷读取方法：

```csharp
var text = await playwrightService.GetElement(
    url: "https://example.com",
    selector: "h1",
    readyText: null,
    attribute: "text");

var links = await playwrightService.GetElementList(
    url: "https://example.com",
    selector: "a",
    attribute: "href");
```

字符串 cookie 仍然支持：

```csharp
var result = await playwrightService.GetElement(
    url: "https://example.com",
    selector: "#profile",
    readyText: null,
    attribute: "text",
    cookies: "session=token; theme=dark");
```

标准 `Cookie` 类型也可以直接用于元素读取：

```csharp
var result = await playwrightService.GetElement(
    url: "https://example.com",
    selector: "#profile",
    readyText: null,
    browserCookies: cookies,
    attribute: "text");
```

## 安装 Playwright 依赖

可以在应用启动或管理端点中调用：

```csharp
await playwrightService.EnsureInstalledAsync();
```

该方法会检查 .NET SDK、安装本地 Playwright CLI，并执行 `playwright install`。安装成功后会写入本地标记文件；同一 Playwright 版本、系统和架构下再次调用会跳过重复安装。

## 重试与异常

库内置重试装饰器。当前重试策略依赖 `AutomationException.Retryable`，不会再依赖异常 message 字符串匹配。元素等待、页面等待等可恢复失败会抛出 `AutomationException` 并标记为可重试。

日志装饰器默认只记录 host、selector、attribute、结果数量和文本长度，不直接输出完整页面内容，减少敏感信息泄露风险。

## 健康检查

注册 `builder.AddPlaywrightHealthCheck()` 后，可以通过 ASP.NET Core HealthChecks 检查配置浏览器是否可创建且已连接：

```csharp
builder.AddPlaywrightDefaults();
builder.AddPlaywrightHealthCheck("playwright");

var app = builder.Build();
app.MapHealthChecks("/health");
```

## Tracing 与截图

可以直接使用 tracing 方法：

```csharp
var browser = await playwrightService.GetBrowserAsync();
await using var context = await browser.NewContextAsync();

await playwrightService.StartTracingAsync(context);

var page = await context.NewPageAsync();
await page.GotoAsync("https://example.com");

await playwrightService.StopTracingAsync(context, "trace.zip");
```

截图可以使用扩展方法：

```csharp
var path = await page.TakeScreenshotAsync(fullPage: true);
```

## 许可证

本项目使用 MIT 许可证，详见 `LICENSE.txt`。
