using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaywrightAutomation.Core.Data;

public class RetryOptions
{
    public int MaxRetries { get; set; } = 3;

    public int RetryIntervalMs { get; set; } = 10000;
}
