using SteamRoulette.Core.Models;
using SteamRoulette.Core.Roulette;
using SteamRoulette.Core.Steam;

namespace SteamRoulette.Tests;

public class RatingFilterTests
{
    [Fact]
    public void Category_Filter_Matches_Any_Listed_Category()
    {
        var games = new List<SteamGame>
        {
            new() { AppId = 1, Installed = true, Categories = { "Single-player" } },
            new() { AppId = 2, Installed = true, Categories = { "Co-op", "Single-player" } },
            new() { AppId = 3, Installed = true }, // categories unknown
        };
        var pool = new GameRoulette().Filter(games, new RouletteFilter { Category = "Co-op" });
        Assert.Equal(new[] { 2 }, pool.Select(g => g.AppId));
    }

    [Fact]
    public void MinMetacritic_Excludes_Lower_And_Unknown()
    {
        var games = new List<SteamGame>
        {
            new() { AppId = 1, Installed = true, MetacriticScore = 90 },
            new() { AppId = 2, Installed = true, MetacriticScore = 70 },
            new() { AppId = 3, Installed = true, MetacriticScore = null },
        };
        var pool = new GameRoulette().Filter(games, new RouletteFilter { MinMetacritic = 80 });
        Assert.Equal(new[] { 1 }, pool.Select(g => g.AppId));
    }

    [Fact]
    public void ParseReviews_Computes_Positive_Percent()
    {
        const string json = """
        { "success": 1, "query_summary": {
            "review_score_desc": "Very Positive", "total_positive": 480,
            "total_negative": 20, "total_reviews": 500 } }
        """;
        var r = SteamStoreClient.ParseReviews(json);
        Assert.NotNull(r);
        Assert.Equal("Very Positive", r!.Description);
        Assert.Equal(96, r.PositivePercent);
        Assert.Equal(500, r.Total);
    }

    [Fact]
    public void ParseReviews_Null_When_No_Reviews()
    {
        Assert.Null(SteamStoreClient.ParseReviews("""{ "query_summary": { "total_reviews": 0 } }"""));
    }
}
