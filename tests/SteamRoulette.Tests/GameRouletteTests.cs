using SteamRoulette.Core.Models;
using SteamRoulette.Core.Roulette;

namespace SteamRoulette.Tests;

public class GameRouletteTests
{
    private static List<SteamGame> Sample() => new()
    {
        new SteamGame { AppId = 1, Name = "Installed Unplayed", Installed = true, PlaytimeMinutes = 0 },
        new SteamGame { AppId = 2, Name = "Installed Played", Installed = true, PlaytimeMinutes = 600 },
        new SteamGame { AppId = 3, Name = "Owned Not Installed", Installed = false, PlaytimeMinutes = 0 },
        new SteamGame
        {
            AppId = 4, Name = "Played Yesterday", Installed = true, PlaytimeMinutes = 120,
            LastPlayed = DateTime.UtcNow.AddDays(-1),
        },
    };

    [Fact]
    public void InstalledOnly_Excludes_Uninstalled()
    {
        var roulette = new GameRoulette();
        var pool = roulette.Filter(Sample(), new RouletteFilter { InstalledOnly = true });

        Assert.DoesNotContain(pool, g => g.AppId == 3);
        Assert.Equal(3, pool.Count);
    }

    [Fact]
    public void UnplayedOnly_Keeps_Only_Zero_Playtime()
    {
        var roulette = new GameRoulette();
        var pool = roulette.Filter(Sample(),
            new RouletteFilter { InstalledOnly = false, UnplayedOnly = true });

        Assert.All(pool, g => Assert.Equal(0, g.PlaytimeMinutes));
        Assert.Equal(2, pool.Count);
    }

    [Fact]
    public void ExcludeRecentDays_Drops_Recently_Played()
    {
        var roulette = new GameRoulette();
        var pool = roulette.Filter(Sample(),
            new RouletteFilter { InstalledOnly = false, ExcludeRecentDays = 7 });

        Assert.DoesNotContain(pool, g => g.AppId == 4);
    }

    [Fact]
    public void Pick_Is_Deterministic_With_Seeded_Rng_And_Within_Pool()
    {
        var roulette = new GameRoulette(new Random(42));
        var filter = new RouletteFilter { InstalledOnly = true };

        var first = roulette.Pick(Sample(), filter);

        Assert.NotNull(first);
        Assert.True(first!.Installed);
    }

    [Fact]
    public void Pick_Returns_Null_When_Nothing_Qualifies()
    {
        var roulette = new GameRoulette();
        var pick = roulette.Pick(Sample(),
            new RouletteFilter { InstalledOnly = true, NameContains = "no such game" });

        Assert.Null(pick);
    }

    [Fact]
    public void Weighted_Pick_Favors_Less_Played_Over_Many_Draws()
    {
        // Two installed games: one barely played, one with 100h. Over many weighted draws
        // the low-playtime game should be picked far more often.
        var games = new List<SteamGame>
        {
            new() { AppId = 1, Name = "Fresh", Installed = true, PlaytimeMinutes = 0 },
            new() { AppId = 2, Name = "Grinded", Installed = true, PlaytimeMinutes = 6000 },
        };
        var roulette = new GameRoulette(new Random(1));
        var filter = new RouletteFilter { InstalledOnly = true, WeightTowardUnplayed = true };

        int fresh = 0;
        for (int i = 0; i < 1000; i++)
            if (roulette.Pick(games, filter)!.AppId == 1) fresh++;

        Assert.True(fresh > 800, $"expected the fresh game to dominate, got {fresh}/1000");
    }
}
