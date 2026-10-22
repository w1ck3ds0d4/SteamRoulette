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

    /// <summary>Creates a limiter that spaces released callers at least <paramref name="minInterval"/> apart.</summary>
    /// <param name="minInterval">Minimum time between two consecutive released callers.</param>
    public RateLimiter(TimeSpan minInterval) => _minInterval = minInterval;

    /// <summary>
    /// Waits until at least <c>minInterval</c> has elapsed since the previous released caller,
    /// so callers are serialized in FIFO-ish order through the semaphore. Throws
    /// <see cref="OperationCanceledException"/> if <paramref name="ct"/> is cancelled while
    /// queued or delaying.
    /// </summary>
    /// <param name="ct">Cancels the wait.</param>
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
