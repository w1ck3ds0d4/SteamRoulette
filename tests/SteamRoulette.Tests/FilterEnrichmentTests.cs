using SteamRoulette.Core.Models;
using SteamRoulette.Core.Roulette;

namespace SteamRoulette.Tests;

public class FilterEnrichmentTests
{
    private static List<SteamGame> Games() => new()
    {
        new SteamGame
        {
            AppId = 1, Name = "Action No-Ach", Installed = true,
            Genres = { "Action" }, HasAchievements = false,
        },
        new SteamGame
        {
            AppId = 2, Name = "RPG Half Done", Installed = true,
            Genres = { "RPG", "Action" }, HasAchievements = true,
            AchievementTotal = 50, AchievementUnlocked = 25,
        },
        new SteamGame
        {
            AppId = 3, Name = "RPG 100%", Installed = true,
            Genres = { "RPG" }, HasAchievements = true,
            AchievementTotal = 30, AchievementUnlocked = 30,
        },
        new SteamGame
        {
            AppId = 4, Name = "Unenriched", Installed = true, // genres unknown, achievements unknown
        },
    };

    [Fact]
    public void Genre_Filter_Matches_Any_Listed_Genre()
    {
        var pool = new GameRoulette().Filter(Games(), new RouletteFilter { Genre = "Action" });
        Assert.Equal(new[] { 1, 2 }, pool.Select(g => g.AppId).OrderBy(x => x));
    }

    [Fact]
    public void RequireAchievements_Keeps_Only_Confirmed_True()
    {
        var pool = new GameRoulette().Filter(Games(), new RouletteFilter { RequireAchievements = true });
        // App 1 is false, App 4 is unknown -> both excluded.
        Assert.Equal(new[] { 2, 3 }, pool.Select(g => g.AppId).OrderBy(x => x));
    }

    [Fact]
    public void OnlyIncomplete_Keeps_Achievement_Games_Below_100()
    {
        var pool = new GameRoulette().Filter(Games(), new RouletteFilter { OnlyIncompleteAchievements = true });
        Assert.Equal(new[] { 2 }, pool.Select(g => g.AppId)); // 3 is 100%, 1 has none, 4 unknown
    }

    [Fact]
    public void AchievementsComplete_Is_Null_When_Progress_Unknown()
    {
        var game = new SteamGame { HasAchievements = true };
        Assert.Null(game.AchievementsComplete);
    }
}
