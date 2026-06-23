using SteamRoulette.Core.Models;
using SteamRoulette.Core.Steam;

namespace SteamRoulette.Tests;

public class JsonDiskCacheTests : IDisposable
{
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "SteamRouletteTests_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Roundtrips_A_Value()
    {
        var cache = new JsonDiskCache(_dir);
        cache.Set("meta_620", new GameMetadata { AppId = 620, Genres = { "Action" } });

        var got = cache.Get<GameMetadata>("meta_620", TimeSpan.FromMinutes(5));
        Assert.NotNull(got);
        Assert.Equal(620, got!.AppId);
        Assert.Contains("Action", got.Genres);
    }

    [Fact]
    public void Misses_When_Expired()
    {
        var cache = new JsonDiskCache(_dir);
        cache.Set("ach_1", new GameMetadata { AppId = 1 });
        // TTL of zero means anything already written is already stale.
        Assert.Null(cache.Get<GameMetadata>("ach_1", TimeSpan.Zero));
    }

    [Fact]
    public void Misses_When_Absent()
    {
        var cache = new JsonDiskCache(_dir);
        Assert.Null(cache.Get<GameMetadata>("nope", TimeSpan.FromMinutes(5)));
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }
}
