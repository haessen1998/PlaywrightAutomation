using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using PlaywrightAutomation.Data;
using PlaywrightAutomation.Exceptions;
using PlaywrightAutomation.Extensions;
using PlaywrightAutomation.Interfaces;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace PlaywrightAutomation.Services;

public class PlaywrightService(IOptions<PlaywrightOptions> options) : IPlaywrightService
{
    private readonly ConcurrentDictionary<IBrowser, byte> _ownedBrowsers = new();
    private readonly ConcurrentDictionary<IBrowserContext, byte> _ownedPersistentContexts = new();
    private readonly SemaphoreSlim _playwrightLock = new(1, 1);
    private readonly SemaphoreSlim _configuredBrowserLock = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _disposed;

    private async Task<IPlaywright> GetPlaywrightAsync()
    {
        ThrowIfDisposed();

        if (_playwright != null)
        {
            return _playwright;
        }

        await _playwrightLock.WaitAsync();
        try
        {
            _playwright ??= await Playwright.CreateAsync();
            return _playwright;
        }
        catch (Exception ex)
        {
            throw new AutomationException("Playwright runtime initialization failed.", AutomationFailureCategory.ExternalSystemError, true, ex);
        }
        finally
        {
            _playwrightLock.Release();
        }
    }

    private async Task<IBrowser> LaunchBrowserAsync(
        bool headless,
        PlaywrightConnectionMode mode,
        string? server,
        string? channel,
        string[]? args,
        float? slow)
    {
        var playwright = await GetPlaywrightAsync();

        try
        {
            var browser = mode switch
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
                    IgnoreDefaultArgs = ["--enable-automation"],
                    Args = args ?? ["--no-sandbox", "--disable-gpu"]
                }),
                PlaywrightConnectionMode.Ws => await playwright.Chromium.ConnectAsync(server ?? "ws://playwright-server:3000/"),
                PlaywrightConnectionMode.Cdp => await playwright.Chromium.ConnectOverCDPAsync(server ?? "http://playwright-server:9222/"),
                _ => throw new NotSupportedException($"Unsupported Playwright connection mode: {mode}.")
            };

            TrackBrowser(browser);
            return browser;
        }
        catch (Exception ex) when (ex is not AutomationException)
        {
            throw new AutomationException("Browser initialization failed.", AutomationFailureCategory.ExternalSystemError, true, ex);
        }
    }

    private async Task<IBrowser> LaunchPersistentBrowserAsync(
        bool headless,
        PlaywrightConnectionMode mode,
        string? channel,
        string[]? args,
        float? slow)
    {
        var playwright = await GetPlaywrightAsync();
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");

        try
        {
            var browserContext = mode switch
            {
                PlaywrightConnectionMode.Default => await playwright.Chromium.LaunchPersistentContextAsync(dataDirectory, new()
                {
                    SlowMo = slow,
                    Headless = headless,
                    Timeout = 0
                }),
                PlaywrightConnectionMode.Local => await playwright.Chromium.LaunchPersistentContextAsync(dataDirectory, new()
                {
                    Channel = channel ?? "msedge",
                    SlowMo = slow,
                    Headless = headless,
                    Timeout = 0,
                    IgnoreDefaultArgs = ["--enable-automation"],
                    Args = args ?? ["--no-sandbox", "--disable-gpu"]
                }),
                _ => throw new NotSupportedException("Persistent browser contexts are supported only for local launches.")
            };

            _ownedPersistentContexts.TryAdd(browserContext, 0);

            if (browserContext.Browser == null)
            {
                throw new AutomationException("Persistent browser context did not expose a browser instance.", AutomationFailureCategory.ExternalSystemError, false);
            }

            TrackBrowser(browserContext.Browser);
            return browserContext.Browser;
        }
        catch (Exception ex) when (ex is not AutomationException)
        {
            throw new AutomationException("Persistent browser initialization failed.", AutomationFailureCategory.ExternalSystemError, true, ex);
        }
    }

    public async Task<IBrowser> GetBrowserAsync()
    {
        ThrowIfDisposed();

        if (_browser is { IsConnected: true })
        {
            return _browser;
        }

        await _configuredBrowserLock.WaitAsync();
        try
        {
            if (_browser is { IsConnected: true })
            {
                return _browser;
            }

            var settings = options.Value;
            _browser = await LaunchBrowserAsync(
                settings.Headless,
                settings.Mode,
                settings.Server,
                settings.Channel,
                settings.Args,
                settings.Slow);

            return _browser;
        }
        finally
        {
            _configuredBrowserLock.Release();
        }
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
        ThrowIfDisposed();

        return persistent
            ? LaunchPersistentBrowserAsync(headless, mode, channel, args, slow)
            : LaunchBrowserAsync(headless, mode, server, channel, args, slow);
    }

    public async Task<T> RunPageAsync<T>(
        Func<IPage, CancellationToken, Task<T>> action,
        string? url = null,
        IEnumerable<Cookie>? cookies = null,
        BrowserNewContextOptions? contextOptions = null,
        CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ThrowIfDisposed();

        try
        {
            var browser = await GetBrowserAsync();

            await using var context = await browser.NewContextAsync(contextOptions);
            await AddCookiesAsync(context, url, cookies);

            var page = await context.NewPageAsync();

            if (!string.IsNullOrWhiteSpace(url))
            {
                await page.GotoAsync(url, new()
                {
                    Timeout = options.Value.PageIntervalMs
                });
            }

            return await action(page, cancellation);
        }
        catch (AutomationException)
        {
            throw;
        }
        catch (PlaywrightException ex)
        {
            throw new AutomationException("Page automation failed.", AutomationFailureCategory.ExternalSystemError, true, ex);
        }
    }

    public Task RunPageAsync(
        Func<IPage, CancellationToken, Task> action,
        string? url = null,
        IEnumerable<Cookie>? cookies = null,
        BrowserNewContextOptions? contextOptions = null,
        CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        return RunPageAsync(
            async (page, token) =>
            {
                await action(page, token);
                return true;
            },
            url,
            cookies,
            contextOptions,
            cancellation);
    }

    public ValueTask<List<string>> GetElementList(
        string url,
        string selector,
        string attribute = "href",
        string? cookies = null)
    {
        var browserCookies = ParseCookieHeader(cookies, url);
        return GetElementListCore(url, selector, browserCookies, attribute);
    }

    public ValueTask<List<string>> GetElementList(
        string url,
        string selector,
        IEnumerable<Cookie> browserCookies,
        string attribute = "href")
    {
        return GetElementListCore(url, selector, browserCookies, attribute);
    }

    private async ValueTask<List<string>> GetElementListCore(
        string url,
        string selector,
        IEnumerable<Cookie>? cookies,
        string attribute)
    {
        return await RunPageAsync(
            (page, _) => page.GetElementList(selector, attribute, options.Value.ElementIntervalMs).AsTask(),
            url,
            cookies);
    }

    public ValueTask<string?> GetElement(
        string url,
        string selector,
        string? readyText,
        WaitForSelectorState state = WaitForSelectorState.Visible,
        string attribute = "text",
        string? cookies = null)
    {
        var browserCookies = ParseCookieHeader(cookies, url);
        return GetElementCore(url, selector, readyText, browserCookies, state, attribute);
    }

    public ValueTask<string?> GetElement(
        string url,
        string selector,
        string? readyText,
        IEnumerable<Cookie> browserCookies,
        WaitForSelectorState state = WaitForSelectorState.Visible,
        string attribute = "text")
    {
        return GetElementCore(url, selector, readyText, browserCookies, state, attribute);
    }

    private async ValueTask<string?> GetElementCore(
        string url,
        string selector,
        string? readyText,
        IEnumerable<Cookie>? cookies,
        WaitForSelectorState state,
        string attribute)
    {
        return await RunPageAsync(
            (page, _) => page.GetElement(selector, readyText, state, attribute, options.Value.ElementIntervalMs).AsTask(),
            url,
            cookies);
    }

    public async Task EnsureInstalledAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellation = default)
    {
        var toolDirectory = Path.Combine(AppContext.BaseDirectory, "tools");
        var scriptDirectory = Path.Combine(AppContext.BaseDirectory, "scripts");

        Directory.CreateDirectory(toolDirectory);
        Directory.CreateDirectory(scriptDirectory);

        var markerPath = GetInstallMarkerPath(toolDirectory);
        if (File.Exists(markerPath))
        {
            progress?.Report("Playwright browsers already marked as installed. Skipping install.");
            return;
        }

        progress?.Report("Checking for .NET SDK...");

        var dotnetResult = await ProcessExtensions.RunProcessAsync("dotnet", "--version", timeoutMs: 30_000, cancellation: cancellation);

        if (dotnetResult.ExitCode != 0)
        {
            progress?.Report("Failed to find .NET SDK.");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var psUrl = "https://dot.net/v1/dotnet-install.ps1";
                var psPath = Path.Combine(scriptDirectory, "dotnet-install.ps1");

                progress?.Report("Downloading dotnet-install script...");
                await ProcessExtensions.DownloadFileAsync(psUrl, psPath, cancellation);

                progress?.Report("Installing .NET SDK...");
                var result = await ProcessExtensions.RunProcessAsync(
                    "powershell",
                    ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", psPath, "-Channel", "LTS"],
                    10 * 60_000,
                    cancellation);

                if (result.ExitCode != 0)
                {
                    throw new InstallFailedException($"Dotnet install failed: {result.GetOutputSummary()}\r\nPlease visit https://dotnet.microsoft.com/en-us/download for manual installation.");
                }
            }
            else
            {
                var shUrl = "https://dot.net/v1/dotnet-install.sh";
                var shPath = Path.Combine(Path.GetTempPath(), "dotnet-install.sh");

                progress?.Report("Downloading dotnet-install script...");
                await ProcessExtensions.DownloadFileAsync(shUrl, shPath, cancellation);

                ProcessExtensions.MakeExecutable(shPath);

                progress?.Report("Installing .NET SDK...");
                var result = await ProcessExtensions.RunProcessAsync(
                    shPath,
                    ["--channel", "LTS"],
                    10 * 60_000,
                    cancellation);

                if (result.ExitCode != 0)
                {
                    throw new InstallFailedException($"Dotnet install (sh) failed: {result.GetOutputSummary()}\r\nPlease visit https://dotnet.microsoft.com/en-us/download for manual installation.");
                }
            }

            progress?.Report("Installed .NET SDK.");
        }
        else
        {
            progress?.Report($".NET SDK found: {dotnetResult.StdOut?.Trim()}");
        }

        progress?.Report("Checking for Playwright CLI...");

        var cliExec = FindPlaywrightCli(toolDirectory);

        if (string.IsNullOrWhiteSpace(cliExec))
        {
            progress?.Report("Playwright CLI not found in tools. Installing to tools using dotnet tool install...");

            var cliResult = await ProcessExtensions.RunProcessAsync(
                "dotnet",
                ["tool", "install", "--tool-path", Path.GetFullPath(toolDirectory), "Microsoft.Playwright.CLI"],
                timeoutMs: 10 * 60_000,
                cancellation: cancellation);

            if (cliResult.ExitCode != 0)
            {
                throw new InstallFailedException("Failed to install Microsoft.Playwright.CLI: " + cliResult.GetOutputSummary());
            }

            cliExec = FindPlaywrightCli(toolDirectory);

            if (string.IsNullOrWhiteSpace(cliExec))
            {
                throw new InstallFailedException("Installed tool but couldn't find playwright executable in tools directory.");
            }

            ProcessExtensions.MakeExecutable(cliExec);
            progress?.Report("Installed Playwright CLI to tools: " + cliExec);
        }
        else
        {
            progress?.Report("Playwright CLI found in tools: " + cliExec);
        }

        var installArgs = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? new List<string> { "install", "--with-deps" }
            : new List<string> { "install" };

        progress?.Report($"Running Playwright CLI: {cliExec} {string.Join(' ', installArgs)}");

        var res = await ProcessExtensions.RunProcessAsync(cliExec, installArgs, timeoutMs: 10 * 60_000, cancellation: cancellation);

        if (res.ExitCode != 0)
        {
            throw new InstallFailedException("Playwright install failed: " + res.GetOutputSummary());
        }

        Directory.CreateDirectory(Path.GetDirectoryName(markerPath)!);
        await File.WriteAllTextAsync(markerPath, DateTimeOffset.UtcNow.ToString("O"), cancellation);

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

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var context in _ownedPersistentContexts.Keys)
        {
            try
            {
                await context.CloseAsync();
                await ((IAsyncDisposable)context).DisposeAsync();
            }
            catch
            {
                // Best-effort cleanup during service shutdown.
            }
        }

        foreach (var browser in _ownedBrowsers.Keys)
        {
            try
            {
                if (browser.IsConnected)
                {
                    await browser.CloseAsync();
                }

                await ((IAsyncDisposable)browser).DisposeAsync();
            }
            catch
            {
                // Best-effort cleanup during service shutdown.
            }
        }

        if (_playwright != null)
        {
            _playwright.Dispose();
        }

        _playwrightLock.Dispose();
        _configuredBrowserLock.Dispose();
    }

    private async Task AddCookiesAsync(IBrowserContext context, string? url, IEnumerable<Cookie>? cookies)
    {
        var normalized = NormalizeCookies(cookies, url);

        if (normalized.Count > 0)
        {
            await context.AddCookiesAsync(normalized);
        }
    }

    private static IReadOnlyList<Cookie> ParseCookieHeader(string? cookies, string? url)
    {
        if (string.IsNullOrWhiteSpace(cookies))
        {
            return [];
        }

        var origin = GetOrigin(url);
        var parsed = new List<Cookie>();

        foreach (var pair in cookies.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var index = pair.IndexOf('=');
            if (index <= 0)
            {
                throw new AutomationException($"Invalid cookie pair: '{pair}'.", AutomationFailureCategory.ValidationFailed, false);
            }

            parsed.Add(new Cookie
            {
                Name = pair[..index].Trim(),
                Value = pair[(index + 1)..].Trim(),
                Url = origin
            });
        }

        return parsed;
    }

    private static IReadOnlyList<Cookie> NormalizeCookies(IEnumerable<Cookie>? cookies, string? url)
    {
        if (cookies == null)
        {
            return [];
        }

        var origin = string.IsNullOrWhiteSpace(url) ? null : GetOrigin(url);
        var normalized = new List<Cookie>();

        foreach (var cookie in cookies)
        {
            if (string.IsNullOrWhiteSpace(cookie.Name))
            {
                throw new AutomationException("Cookie name cannot be empty.", AutomationFailureCategory.ValidationFailed, false);
            }

            if (string.IsNullOrWhiteSpace(cookie.Url) &&
                string.IsNullOrWhiteSpace(cookie.Domain) &&
                !string.IsNullOrWhiteSpace(origin))
            {
                cookie.Url = origin;
            }

            normalized.Add(cookie);
        }

        return normalized;
    }

    private static string GetOrigin(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var targetUri))
        {
            throw new AutomationException("A valid absolute url is required when cookies are provided.", AutomationFailureCategory.ValidationFailed, false);
        }

        return targetUri.GetLeftPart(UriPartial.Authority);
    }

    private static string? FindPlaywrightCli(string toolDirectory)
    {
        if (!Directory.Exists(toolDirectory))
        {
            return null;
        }

        var candidates = Directory.GetFiles(toolDirectory, "playwright*", SearchOption.TopDirectoryOnly);
        if (candidates.Length == 0)
        {
            candidates = Directory.GetFiles(toolDirectory, "*", SearchOption.AllDirectories);
        }

        foreach (var candidate in candidates)
        {
            var name = Path.GetFileName(candidate);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (name.Equals("playwright.exe", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("playwright.cmd", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("playwright.ps1", StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }
            else if (name == "playwright" || name == "playwright.sh")
            {
                ProcessExtensions.MakeExecutable(candidate);
                return candidate;
            }
        }

        return null;
    }

    private static string GetInstallMarkerPath(string toolDirectory)
    {
        var playwrightVersion = typeof(Playwright).Assembly.GetName().Version?.ToString() ?? "unknown";
        var os = RuntimeInformation.OSDescription
            .Replace(' ', '_')
            .Replace('/', '_')
            .Replace('\\', '_');
        var architecture = RuntimeInformation.OSArchitecture.ToString();

        return Path.Combine(toolDirectory, ".playwright-install", $"{playwrightVersion}_{os}_{architecture}.ok");
    }

    private void TrackBrowser(IBrowser browser)
    {
        _ownedBrowsers.TryAdd(browser, 0);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(PlaywrightService));
    }
}
