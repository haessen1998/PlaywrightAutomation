using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace PlaywrightAutomation.Extensions;

/// <summary>
/// Process 扩展方法
/// </summary>
public static class ProcessExtensions
{
    public static async Task<Data.ProcResult> RunProcessAsync(
        string fileName, 
        string arguments, 
        int timeoutMs = 120_000, 
        CancellationToken cancellation = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);

        if (timeoutMs > 0)
        {
            cts.CancelAfter(timeoutMs);
        }

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        var result = new Data.ProcResult();

        try
        {
            using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            proc.OutputDataReceived += (_, e) => 
            { 
                if (e.Data != null)
                { 
                    stdout.AppendLine(e.Data); 
                }
            };

            proc.ErrorDataReceived += (_, e) => 
            { 
                if (e.Data != null)
                { 
                    stderr.AppendLine(e.Data); 
                } 
            };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            // Wait with cancellation
            while (!proc.HasExited)
            {
                await Task.Delay(200, cts.Token).ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnRanToCompletion);
            }

            result.ExitCode = proc.ExitCode;
            result.StdOut = stdout.ToString();
            result.StdErr = stderr.ToString();
            return result;
        }
        catch (OperationCanceledException)
        {
            throw new Exception($"Process '{fileName} {arguments}' timed out or cancelled.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to start/execute process '{fileName} {arguments}': {ex.Message}", ex);
        }
    }

    public static async Task DownloadFileAsync(string url, string destFile, CancellationToken cancellation)
    {
        try
        {
            using var http = new HttpClient();
            using var resp = await http.GetAsync(url, cancellation);
            resp.EnsureSuccessStatusCode();
            using var fs = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None);
            await resp.Content.CopyToAsync(fs);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to download {url}: {ex.Message}", ex);
        }
    }

    public static void MakeExecutable(string path)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x \"{path}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
                p.WaitForExit();
            }
            catch
            {
                // best-effort
            }
        }
    }
}
