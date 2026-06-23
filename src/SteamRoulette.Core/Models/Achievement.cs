using System.Text.Json.Serialization;

namespace SteamRoulette.Core.Models;

/// <summary>A single achievement from a game's schema, with the player's unlock state.</summary>
public sealed class Achievement
{
    public string ApiName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Description { get; set; }

    /// <summary>Coloured icon shown when unlocked.</summary>
    public string? IconUrl { get; set; }

    /// <summary>Grey icon shown while locked.</summary>
    public string? IconGrayUrl { get; set; }

    public bool Unlocked { get; set; }
    public DateTime? UnlockedAt { get; set; }
}

/// <summary>A game's full achievement set merged with the player's progress.</summary>
public sealed class GameAchievements
{
    public int AppId { get; set; }
    public List<Achievement> Achievements { get; set; } = new();

    [JsonIgnore]
    public int Total => Achievements.Count;

    [JsonIgnore]
    public int UnlockedCount => Achievements.Count(a => a.Unlocked);

    [JsonIgnore]
    public double PercentComplete => Total == 0 ? 0 : 100.0 * UnlockedCount / Total;

    /// <summary>True only when the game has achievements and every one is unlocked.</summary>
    [JsonIgnore]
    public bool IsComplete => Total > 0 && UnlockedCount == Total;
}
