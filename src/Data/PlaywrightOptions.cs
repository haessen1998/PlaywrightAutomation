using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaywrightAutomation.Core.Data;

public class PlaywrightOptions
{
    public bool Headless { get; set; }

    public PlaywrightConnectionMode Mode { get; set; }

    public string? Server { get; set; }

    public string? Channel { get; set; }

    public int PageIntervalMs { get; set; }

    public int ElementIntervalMs { get; set; }

    public int VideoIntervalMs { get; set; }
}

public enum PlaywrightConnectionMode
{
    Default,
    Local,
    Cdp,
    Ws
}
