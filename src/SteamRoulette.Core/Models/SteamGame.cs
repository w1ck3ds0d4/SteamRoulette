namespace SteamRoulette.Core.Models;

/// <summary>Where a game record came from when the library was loaded.</summary>
public enum GameSource
{
    /// <summary>Read from a local appmanifest_*.acf (installed game).</summary>
    Local,

    /// <summary>Returned by the Steam Web API (owned game, may not be installed).</summary>
    WebApi,
}

/// <summary>A single game in the user's Steam library.</summary>
public sealed class SteamGame
{
    public int AppId { get; set; }
    public string Name { get; set; } = "";

    /// <summary>Total playtime in minutes. 0 when unknown (e.g. local-only with no Web API).</summary>
    public int PlaytimeMinutes { get; set; }

    /// <summary>True when an appmanifest for this game exists in a Steam library folder.</summary>
    public bool Installed { get; set; }

    public string? InstallDir { get; set; }

    /// <summary>Full path to the game's install folder, when known (installed games).</summary>
    public string? InstallPath { get; set; }

    public long SizeOnDiskBytes { get; set; }

    /// <summary>Last time the game was played, if known.</summary>
    public DateTime? LastPlayed { get; set; }

    /// <summary>Small library icon URL, when the Web API supplied an icon hash.</summary>
    public string? IconUrl { get; set; }

    public GameSource Source { get; set; }

    // ---- enrichment: filled in lazily from the store + achievement APIs --------------
    public List<string> Genres { get; set; } = new();
    public List<string> Categories { get; set; } = new();

    /// <summary>Whether the game has Steam achievements. Null while not yet known.</summary>
    public bool? HasAchievements { get; set; }

    public int? AchievementTotal { get; set; }
    public int? AchievementUnlocked { get; set; }

    public string? ShortDescription { get; set; }
    public string? ReleaseDate { get; set; }
    public int? MetacriticScore { get; set; }

    /// <summary>Steam review summary text, e.g. "Very Positive". Null until fetched.</summary>
    public string? ReviewSummary { get; set; }
    public int? ReviewPositivePercent { get; set; }
    public int? ReviewCount { get; set; }

    /// <summary>True/false once achievement progress is known; null while unknown.</summary>
    public bool? AchievementsComplete =>
        AchievementTotal is int t && AchievementUnlocked is int u ? t > 0 && u >= t : null;

    public double PlaytimeHours => PlaytimeMinutes / 60.0;

    /// <summary>Tick when installed, empty otherwise. Convenience for list binding.</summary>
    public string InstalledMark => Installed ? "✔" : "";

    /// <summary>Compact achievement progress for the list column: "✓ 30", "25/50", or "".</summary>
    public string AchievementSummary
    {
        get
        {
            if (AchievementTotal is not int t || t == 0) return "";
            int u = AchievementUnlocked ?? 0;
            return u >= t ? $"✓ {t}" : $"{u}/{t}";
        }
    }

    /// <summary>Capsule/header image shown on the store; derived purely from the appid.</summary>
    public string HeaderImageUrl =>
        $"https://cdn.cloudflare.steamstatic.com/steam/apps/{AppId}/header.jpg";

    public override string ToString() => $"{Name} ({AppId})";
}
