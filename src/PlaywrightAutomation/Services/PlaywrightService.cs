using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using PlaywrightAutomation.Data;
using PlaywrightAutomation.Exceptions;
using PlaywrightAutomation.Extensions;
using PlaywrightAutomation.Interfaces;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PlaywrightAutomation.Services;

public class PlaywrightService(
    IOptions<PlaywrightOptions> options) : IPlaywrightService
{
    private IBrowser? _browser;

    private async Task InitializeAsync(
        bool headless = true,
        PlaywrightConnectionMode mode = PlaywrightConnectionMode.Local,
        string? server = "http://localhost:9222/",
        string? channel = "msedge",
        int slow = 100)
    {
        var playwright = await Playwright.CreateAsync();

        _browser = mode switch
        {
            PlaywrightConnectionMode.Default => await playwright.Chromium.LaunchAsync(new()
            {
                SlowMo = slow,
                Headless = headless,
                Timeout = 0
            }),
            PlaywrightConnectionMode.Local => await playwright.Chromium.LaunchAsync(new()
            {
                Channel = channel ?? "msedge",
                SlowMo = slow,
                Headless = headless,
                Timeout = 0,
                IgnoreDefaultArgs = [ "--enable-automation" ],
                Args = [ "--no-sandbox", "--disable-gpu" ]
            }),
            PlaywrightConnectionMode.Ws => await playwright.Chromium.ConnectAsync(server ?? "ws://playwright-server:3000/"),
            PlaywrightConnectionMode.Cdp => await playwright.Chromium.ConnectOverCDPAsync(server ?? "http://playwright-server:9222/"),
            _ => await playwright.Chromium.ConnectOverCDPAsync(server ?? "http://localhost:9222/")
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="url"></param>
    /// <param name="selector"></param>
    /// <param name="attribute"></param>
    /// <returns></returns>
    public async ValueTask<List<string>> GetElementList(
        string url,
        string selector,
        string attribute = "href",
        string? cookies = null)
    {
        //var page = await GetPageAsync(url, cookies);
        var browser = await GetBrowserAsync();

        await using var context = await browser!.NewContextAsync();

        if (!string.IsNullOrWhiteSpace(cookies) && !string.IsNullOrWhiteSpace(url))
        {
            var targetUri = new Uri(url);

            string origin = $"{targetUri.Scheme}://{targetUri.Host}";

            // 解析 cookie 字符串 "a=xxx; b=xxx" -> Cookie 对象数组
            var cookieObjects = cookies
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Select(pair =>
                {
                    var idx = pair.IndexOf('=');
                    if (idx <= 0) return null;
                    var name = pair.Substring(0, idx).Trim();
                    var value = pair.Substring(idx + 1).Trim();

                    // 特殊处理 token 字段，避免泄露
                    if (name == "token") return null;

                    return new Cookie
                    {
                        Name = name,
                        Value = value,
                        // 指定 Url 更可靠（避免 Domain 设置错误）；Playwright 会把 cookie 关联到该 URL 的域
                        Url = origin,
                        // Path = "/"
                        // Url = baseUrl,
                    };
                })
                .Where(c => c != null)
                .ToArray();

            // 在导航之前把 cookie 添加到 context
            await context.AddCookiesAsync(cookieObjects);

            // 验证 cookie 是否已被添加到 context（比 document.cookie 更可靠，能看到 HttpOnly）
            var saved = await context.CookiesAsync([origin]);

        }

        var page = await context.NewPageAsync();

        if (!string.IsNullOrWhiteSpace(url))
        {
            // 尝试加载页面并等待元素
            await page.GotoAsync(url, new()
            {
                Timeout = options.Value.PageIntervalMs
            });
        }

        return await page.GetElementList(selector, attribute, options.Value.ElementIntervalMs);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="url"></param>
    /// <param name="selector"></param>
    /// <param name="readyText"></param>
    /// <param name="attribute"></param>
    /// <returns></returns>
    public async ValueTask<string?> GetElement(
        string url,
        string selector,
        string? readyText,
        WaitForSelectorState state = WaitForSelectorState.Visible,
        string attribute = "text",
        string? cookies = null)
    {
        //var page = await GetPageAsync(url, cookies);

        var browser = await GetBrowserAsync();

        await using var context = await browser!.NewContextAsync();

        if (!string.IsNullOrWhiteSpace(cookies) && !string.IsNullOrWhiteSpace(url))
        {
            var targetUri = new Uri(url);

            string origin = $"{targetUri.Scheme}://{targetUri.Host}";

            // 解析 cookie 字符串 "a=xxx; b=xxx" -> Cookie 对象数组
            var cookieObjects = cookies
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Select(pair =>
                {
                    var idx = pair.IndexOf('=');
                    if (idx <= 0) return null;
                    var name = pair.Substring(0, idx).Trim();
                    var value = pair.Substring(idx + 1).Trim();

                    // 特殊处理 token 字段，避免泄露
                    if (name == "token") return null;

                    return new Cookie
                    {
                        Name = name,
                        Value = value,
                        // 指定 Url 更可靠（避免 Domain 设置错误）；Playwright 会把 cookie 关联到该 URL 的域
                        Url = origin,
                        // Path = "/"
                        // Url = baseUrl,
                    };
                })
                .Where(c => c != null)
                .ToArray();

            // 在导航之前把 cookie 添加到 context
            await context.AddCookiesAsync(cookieObjects);

            // 验证 cookie 是否已被添加到 context（比 document.cookie 更可靠，能看到 HttpOnly）
            var saved = await context.CookiesAsync([origin]);

        }

        var page = await context.NewPageAsync();

        if (!string.IsNullOrWhiteSpace(url))
        {
            // 尝试加载页面并等待元素
            await page.GotoAsync(url, new()
            {
                Timeout = options.Value.PageIntervalMs
            });
        }

        return await page.GetElement(selector, readyText, state, attribute, options.Value.ElementIntervalMs);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
            await ((IAsyncDisposable)_browser).DisposeAsync();
        }
    }

    public async Task<IBrowser> GetBrowserAsync()
    {
        var settings = options.Value;

        if (_browser == null)
        {
            await InitializeAsync(settings.Headless, settings.Mode, settings.Server, settings.Channel);
        }

        return _browser ?? throw new InvalidOperationException("Browser初始化失败");
    }

    public async Task EnsureInstalledAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellation = default)
    {
        // 校验 .net sdk 安装
        var toolDirectory = Path.Combine(AppContext.BaseDirectory, "tools");
        var scriptDirectory = Path.Combine(AppContext.BaseDirectory, "scripts");

        if (!string.IsNullOrWhiteSpace(toolDirectory) && !System.IO.Directory.Exists(toolDirectory))
        {
            System.IO.Directory.CreateDirectory(toolDirectory);
        }

        if (!string.IsNullOrWhiteSpace(scriptDirectory) && !System.IO.Directory.Exists(scriptDirectory))
        {
            System.IO.Directory.CreateDirectory(scriptDirectory);
        }

        // 1) Find or install dotnet
        progress?.Report("Checking for .NET SDK...");

        var dotnetResult = await ProcessExtensions.RunProcessAsync("dotnet", "--version", timeoutMs: 30_000, cancellation: cancellation);

        if (dotnetResult.ExitCode!=0)
        {
            progress?.Report("Failed to find .NET SDK.");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Use dotnet-install.ps1
                string psUrl = "https://dot.net/v1/dotnet-install.ps1";

                string psPath = Path.Combine(scriptDirectory, "dotnet-install.ps1");

                progress?.Report("Downloading dotnet-install script...");

                await ProcessExtensions.DownloadFileAsync(psUrl, psPath, cancellation);

                progress?.Report("Downloaded dotnet-install script.");

                // Run powershell to install
                string installArgs = $"-NoProfile -ExecutionPolicy Bypass -File \"{psPath}\" -Channel LTS";

                progress?.Report("Installing .NET SDK...");

                var result = await ProcessExtensions.RunProcessAsync("powershell", installArgs, 10 * 60_000,cancellation: cancellation);

                if (result.ExitCode != 0)
                {
                    // 当下载安装失败时，推荐用户从网站 https://dotnet.microsoft.com/en-us/download 手动下载安装
                    throw new InstallFailedException($"Dotnet install failed: {result.GetOutputSummary()}\r\nPlease visit https://dotnet.microsoft.com/en-us/download for manual installation.");
                }

                progress?.Report("Installed .NET SDK.");
            }
            else
            {
                // Linux / macOS -> use dotnet-install.sh
                string shUrl = "https://dot.net/v1/dotnet-install.sh";

                string shPath = Path.Combine(Path.GetTempPath(), "dotnet-install.sh");

                progress?.Report("Downloading dotnet-install script...");

                await ProcessExtensions.DownloadFileAsync(shUrl, shPath, cancellation);

                progress?.Report("Downloaded dotnet-install script.");

                ProcessExtensions.MakeExecutable(shPath);

                progress?.Report("Made dotnet-install script executable.");

                progress?.Report("Installing .NET SDK...");

                var result = await ProcessExtensions.RunProcessAsync(shPath, $"--channel LTS", 10 * 60_000, cancellation: cancellation);

                if (result.ExitCode != 0)
                {
                    // 当下载安装失败时，推荐用户从网站 https://dotnet.microsoft.com/en-us/download 手动下载安装
                    throw new InstallFailedException($"Dotnet install (sh) failed: {result.GetOutputSummary()}\r\nPlease visit https://dotnet.microsoft.com/en-us/download for manual installation.");
                }

                progress?.Report("Installed .NET SDK.");
            }

        }
        else
        {
            progress?.Report($".NET SDK found: {dotnetResult.StdOut.Trim()}");
        }   

        // 2) Ensure Playwright CLI exists in tools
        progress?.Report("Checking for Playwright CLI...");

        // Determine if there's an existing Playwright shim in tools
        string[] candidates = Directory.Exists(toolDirectory)
            ? Directory.GetFiles(toolDirectory, "playwright*", SearchOption.TopDirectoryOnly)
            : Array.Empty<string>();

        string cliExec = null;

        if (candidates.Length > 0)
        {
            // prefer exact executable names
            foreach (var c in candidates)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Path.GetFileName(c).StartsWith("playwright", StringComparison.OrdinalIgnoreCase))
                {
                    cliExec = c;
                }
                else if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && (Path.GetFileName(c) == "playwright" || Path.GetFileName(c) == "playwright.sh"))
                {
                    ProcessExtensions.MakeExecutable(c);
                    cliExec = c;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(cliExec))
        {
            progress?.Report("Playwright CLI not found in tools. Installing to tools using dotnet tool install...");

            // Use the provided dotnetExe to run the install
            string installArgs = $"tool install --tool-path \"{Path.GetFullPath(toolDirectory)}\" Microsoft.Playwright.CLI";

            var cliResult = await ProcessExtensions.RunProcessAsync("dotnet", installArgs, timeoutMs: 10 * 60_000, cancellation: cancellation); // allow long time

            if (cliResult.ExitCode != 0)
            {
                throw new InstallFailedException("Failed to install Microsoft.Playwright.CLI: " + cliResult.GetOutputSummary());
            }

            // After install, detect shim
            candidates = Directory.GetFiles(toolDirectory, "playwright*", SearchOption.TopDirectoryOnly);

            if (candidates.Length == 0)
            {
                // Some systems create a "playwright" file inside tools/.store; try to search deeper
                candidates = Directory.GetFiles(toolDirectory, "*", SearchOption.AllDirectories);
            }
            if (candidates.Length == 0)
                throw new InstallFailedException("Installed tool but couldn't find playwright executable in tools directory.");

            foreach (var c in candidates)
            {
                var name = Path.GetFileName(c);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (name.Equals("playwright.exe", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("playwright.cmd", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("playwright.ps1", StringComparison.OrdinalIgnoreCase))
                    {
                        cliExec = c;
                        break;
                    }
                }
                else
                {
                    if (name == "playwright")
                    {
                        cliExec = c;
                        break;
                    }
                }
            }
            // fallback to first candidate under top folder
            if (string.IsNullOrWhiteSpace(cliExec))
            {
                cliExec = Directory.GetFiles(toolDirectory, "*", SearchOption.TopDirectoryOnly).FirstOrDefault();
            }
            if (string.IsNullOrWhiteSpace(cliExec))
                throw new InstallFailedException("Could not locate playwright executable after installation.");

            ProcessExtensions.MakeExecutable(cliExec);

            progress?.Report("Installed Playwright CLI to tools: " + cliExec);
        }
        else
        {
            progress?.Report("Playwright CLI found in tools: " + cliExec);
        }

        // Build args: on Linux use --with-deps
        string args = "install";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            args += " --with-deps";

        // On Windows the shim may be a .cmd or .ps1 that expects to run directly; that's fine.
        progress?.Report($"Running Playwright CLI: {cliExec} {args}");

        var res = await ProcessExtensions.RunProcessAsync(cliExec, args, timeoutMs: 10 * 60_000, cancellation: cancellation);

        if (res.ExitCode != 0)
            throw new InstallFailedException("Playwright install failed: " + res.GetOutputSummary());

        progress?.Report("Playwright install succeeded.");
    }

    public async Task StartTracingAsync(IBrowserContext context, string title = "trace")
    {
        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
    }

    public async Task StopTracingAsync(IBrowserContext context, string path)
    {
        await context.Tracing.StopAsync(new TracingStopOptions
        {
            Path = path
        });
    }
}

