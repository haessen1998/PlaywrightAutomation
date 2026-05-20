using PlaywrightAutomation.Data;
using PlaywrightAutomation.Services;


namespace PlaywrightAutomation.Interfaces;


public interface IPlaywrightContextAccessor
{
    PlaywrightContext? Current { get; }

    IDisposable BeginScope(PlaywrightContext context);
}
