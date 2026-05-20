using PlaywrightAutomation.Interfaces;

namespace PlaywrightAutomation.Services;

public sealed class PlaywrightContextAccessor : IPlaywrightContextAccessor
{
    private readonly AsyncLocal<PlaywrightContext?> _current = new();

    public PlaywrightContext? Current => _current.Value;

    public IDisposable BeginScope(PlaywrightContext context)
    {
        var previous = _current.Value;
        _current.Value = context;

        return new DisposeAction(() =>
        {
            _current.Value = previous;
        });
    }

    private sealed class DisposeAction(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}

public sealed record PlaywrightContext(
    string? RequestId = null,
    bool Trace = false,
    int RetryAttempt = 1,
    bool Commit = false
);
