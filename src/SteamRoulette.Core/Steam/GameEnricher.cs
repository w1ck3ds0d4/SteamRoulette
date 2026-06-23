using System.Net.Http;
using SteamRoulette.Core.Models;

namespace SteamRoulette.Core.Steam;

/// <summary>
/// Fetches store metadata + achievement progress for games, served from a disk cache and
/// rate-limited so enriching a large library does not hammer Steam. Each call returns
/// quickly on a cache hit; on a miss it throttles, fetches, and caches.
/// </summary>
public sealed class GameEnricher
{
    private static readonly TimeSpan MetadataTtl = TimeSpan.FromDays(30);
    private static readonly TimeSpan AchievementsTtl = TimeSpan.FromHours(12);

    private readonly SteamStoreClient _store;
    private readonly AchievementsClient _achievements;
    private readonly JsonDiskCache _cache;
    private readonly RateLimiter _storeLimit;
    private readonly RateLimiter _apiLimit;

    public GameEnricher(HttpClient http, JsonDiskCache? cache = null)
    {
        _store = new SteamStoreClient(http);
        _achievements = new AchievementsClient(http);
        _cache = cache ?? new JsonDiskCache(JsonDiskCache.DefaultDirectory);
        // The store endpoint is strict (~200 / 5 min); the Web API is generous.
        _storeLimit = new RateLimiter(TimeSpan.FromMilliseconds(1500));
        _apiLimit = new RateLimiter(TimeSpan.FromMilliseconds(250));
    }

    public async Task<GameMetadata?> GetMetadataAsync(int appId, CancellationToken ct = default)
    {
        if (_cache.Get<GameMetadata>($"meta_{appId}", MetadataTtl) is { } cached) return cached;
        await _storeLimit.WaitAsync(ct);
        var meta = await _store.GetMetadataAsync(appId, ct);
        if (meta is not null) _cache.Set($"meta_{appId}", meta);
        return meta;
    }

    public async Task<GameAchievements?> GetAchievementsAsync(
        int appId, string apiKey, string steamId64, CancellationToken ct = default)
    {
        if (_cache.Get<GameAchievements>($"ach_{appId}", AchievementsTtl) is { } cached) return cached;
        await _apiLimit.WaitAsync(ct);
        var ach = await _achievements.GetAsync(appId, apiKey, steamId64, ct);
        if (ach is not null) _cache.Set($"ach_{appId}", ach);
        return ach;
    }
}
