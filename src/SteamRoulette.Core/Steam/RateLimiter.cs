namespace SteamRoulette.Core.Steam;

/// <summary>
/// Serializes async callers so consecutive calls are spaced at least
/// <c>minInterval</c> apart - used to stay under Steam's per-IP rate limits.
/// </summary>
public sealed class RateLimiter
{
    private readonly TimeSpan _minInterval;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTime _lastUtc = DateTime.MinValue;

    public RateLimiter(TimeSpan minInterval) => _minInterval = minInterval;

    public async Task WaitAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var due = _lastUtc + _minInterval;
            var now = DateTime.UtcNow;
            if (due > now) await Task.Delay(due - now, ct);
            _lastUtc = DateTime.UtcNow;
        }
        finally
        {
            _gate.Release();
        }
    }
}
