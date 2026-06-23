using System.Text.Json;

namespace SteamRoulette.Core.Steam;

/// <summary>
/// A tiny JSON-on-disk cache with per-entry TTL (based on file modified time). Used to
/// avoid re-fetching slow, rate-limited Steam metadata on every launch.
/// </summary>
public sealed class JsonDiskCache
{
    private readonly string _dir;

    public JsonDiskCache(string dir)
    {
        _dir = dir;
        Directory.CreateDirectory(_dir);
    }

    /// <summary>%LOCALAPPDATA%\SteamRoulette\cache.</summary>
    public static string DefaultDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SteamRoulette", "cache");

    public T? Get<T>(string key, TimeSpan ttl) where T : class
    {
        var path = PathFor(key);
        if (!File.Exists(path)) return null;
        if (DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > ttl) return null;
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path));
        }
        catch
        {
            return null; // corrupt entry -> treat as a miss
        }
    }

    public void Set<T>(string key, T value)
    {
        try
        {
            File.WriteAllText(PathFor(key), JsonSerializer.Serialize(value));
        }
        catch
        {
            // A cache write failure must never break enrichment.
        }
    }

    private string PathFor(string key) => Path.Combine(_dir, key + ".json");
}
