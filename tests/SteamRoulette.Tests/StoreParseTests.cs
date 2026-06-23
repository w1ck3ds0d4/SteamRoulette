using SteamRoulette.Core.Steam;

namespace SteamRoulette.Tests;

public class StoreParseTests
{
    private const string PortalJson = """
    {
      "620": {
        "success": true,
        "data": {
          "name": "Portal 2",
          "short_description": "The Portal 2 single-player campaign.",
          "header_image": "https://cdn/620/header.jpg",
          "metacritic": { "score": 95 },
          "categories": [
            { "id": 2, "description": "Single-player" },
            { "id": 9, "description": "Co-op" },
            { "id": 22, "description": "Steam Achievements" }
          ],
          "genres": [
            { "id": "1", "description": "Action" },
            { "id": "25", "description": "Adventure" }
          ],
          "release_date": { "coming_soon": false, "date": "Apr 18, 2011" },
          "developers": ["Valve"],
          "publishers": ["Valve"]
        }
      }
    }
    """;

    [Fact]
    public void Parses_Genres_Categories_And_Metadata()
    {
        var meta = SteamStoreClient.ParseAppDetails(PortalJson, 620);

        Assert.NotNull(meta);
        Assert.Equal(620, meta!.AppId);
        Assert.Contains("Action", meta.Genres);
        Assert.Contains("Adventure", meta.Genres);
        Assert.Contains("Co-op", meta.Categories);
        Assert.Equal(95, meta.MetacriticScore);
        Assert.Equal("Apr 18, 2011", meta.ReleaseDate);
        Assert.Contains("Valve", meta.Developers);
    }

    [Fact]
    public void Detects_Has_Achievements_From_Category_22()
    {
        var meta = SteamStoreClient.ParseAppDetails(PortalJson, 620);
        Assert.True(meta!.HasAchievements);
    }

    [Fact]
    public void Returns_Null_When_Success_False()
    {
        const string json = """{ "999": { "success": false } }""";
        Assert.Null(SteamStoreClient.ParseAppDetails(json, 999));
    }

    [Fact]
    public void Returns_Null_When_Appid_Missing()
    {
        const string json = """{ "111": { "success": true, "data": { "name": "X" } } }""";
        Assert.Null(SteamStoreClient.ParseAppDetails(json, 620));
    }
}
