using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaywrightAutomation.Data;

public class ProcResult
{
    public int ExitCode{ get; set; }
    public string? StdOut { get; set; }
    public string? StdErr { get; set; }

    public string GetOutputSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"ExitCode={ExitCode}");
        if (!string.IsNullOrWhiteSpace(StdOut)) sb.AppendLine("StdOut: " + StdOut.Trim());
        if (!string.IsNullOrWhiteSpace(StdErr)) sb.AppendLine("StdErr: " + StdErr.Trim());
        return sb.ToString();
    }
}
