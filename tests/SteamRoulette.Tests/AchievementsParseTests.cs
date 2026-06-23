using SteamRoulette.Core.Steam;

namespace SteamRoulette.Tests;

public class AchievementsParseTests
{
    private const string SchemaJson = """
    {
      "game": {
        "gameName": "Portal 2",
        "availableGameStats": {
          "achievements": [
            { "name": "ACH_A", "displayName": "First", "description": "do a thing",
              "icon": "https://i/a.jpg", "icongray": "https://i/a_gray.jpg" },
            { "name": "ACH_B", "displayName": "Second",
              "icon": "https://i/b.jpg", "icongray": "https://i/b_gray.jpg" }
          ]
        }
      }
    }
    """;

    private const string PlayerJson = """
    {
      "playerstats": {
        "steamID": "76561198000000000",
        "gameName": "Portal 2",
        "success": true,
        "achievements": [
          { "apiname": "ACH_A", "achieved": 1, "unlocktime": 1600000000 },
          { "apiname": "ACH_B", "achieved": 0, "unlocktime": 0 }
        ]
      }
    }
    """;

    [Fact]
    public void Schema_Parses_All_Achievements_With_Icons()
    {
        var schema = AchievementsClient.ParseSchema(SchemaJson);
        Assert.Equal(2, schema.Count);
        Assert.Equal("First", schema[0].DisplayName);
        Assert.Equal("https://i/a_gray.jpg", schema[0].IconGrayUrl);
    }

    [Fact]
    public void Merge_Reflects_Player_Progress()
    {
        var schema = AchievementsClient.ParseSchema(SchemaJson);
        var progress = AchievementsClient.ParsePlayer(PlayerJson);
        var merged = AchievementsClient.Merge(620, schema, progress);

        Assert.Equal(2, merged.Total);
        Assert.Equal(1, merged.UnlockedCount);
        Assert.Equal(50, merged.PercentComplete);
        Assert.False(merged.IsComplete);

        var a = merged.Achievements.Single(x => x.ApiName == "ACH_A");
        Assert.True(a.Unlocked);
        Assert.NotNull(a.UnlockedAt);
    }

    [Fact]
    public void IsComplete_True_When_All_Unlocked()
    {
        var schema = AchievementsClient.ParseSchema(SchemaJson);
        const string allDone = """
        { "playerstats": { "success": true, "achievements": [
          { "apiname": "ACH_A", "achieved": 1, "unlocktime": 1 },
          { "apiname": "ACH_B", "achieved": 1, "unlocktime": 2 } ] } }
        """;
        var merged = AchievementsClient.Merge(620, schema, AchievementsClient.ParsePlayer(allDone));
        Assert.True(merged.IsComplete);
        Assert.Equal(100, merged.PercentComplete);
    }
}
