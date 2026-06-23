namespace SteamRoulette.Core.Models;

/// <summary>Constraints applied before the roulette picks a game.</summary>
public sealed class RouletteFilter
{
    /// <summary>Only consider games that are installed on disk.</summary>
    public bool InstalledOnly { get; set; } = true;

    /// <summary>Only consider games with zero recorded playtime (needs Web API data).</summary>
    public bool UnplayedOnly { get; set; }

    /// <summary>Exclude games with more than this many minutes played. Null = no cap.</summary>
    public int? MaxPlaytimeMinutes { get; set; }

    /// <summary>Exclude games played within the last N days. Null = no exclusion.</summary>
    public int? ExcludeRecentDays { get; set; }

    /// <summary>Only consider games whose name contains this text (case-insensitive).</summary>
    public string? NameContains { get; set; }

    /// <summary>Bias the pick toward less-played games instead of a flat random draw.</summary>
    public bool WeightTowardUnplayed { get; set; }

    /// <summary>Only games tagged with this genre (case-insensitive). Null/empty = any.</summary>
    public string? Genre { get; set; }

    /// <summary>Only games that have Steam achievements.</summary>
    public bool RequireAchievements { get; set; }

    /// <summary>Only games that have achievements and aren't 100% unlocked yet.</summary>
    public bool OnlyIncompleteAchievements { get; set; }
}
