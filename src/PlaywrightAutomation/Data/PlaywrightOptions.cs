namespace PlaywrightAutomation.Data;

public class PlaywrightOptions
{
    public bool Headless { get; set; } = true;

    public PlaywrightConnectionMode Mode { get; set; } = PlaywrightConnectionMode.Default;

    public string? Server { get; set; }

    public string? Channel { get; set; }

    public int PageIntervalMs { get; set; } = 60_000;

    public int ElementIntervalMs { get; set; } = 60_000;

    public int VideoIntervalMs { get; set; } = 60_000;

    public float? Slow { get; set; } = 0;

    public string[]? Args { get; set; }
}

public enum PlaywrightConnectionMode
{
    Default,
    Local,
    Cdp,
    Ws
}
