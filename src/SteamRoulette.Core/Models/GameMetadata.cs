namespace SteamRoulette.Core.Models;

/// <summary>
/// Store-side metadata for a game, parsed from Steam's public appdetails endpoint.
/// Cached to disk since it rarely changes.
/// </summary>
public sealed class GameMetadata
{
    public int AppId { get; set; }
    public string? ShortDescription { get; set; }
    public string? HeaderImage { get; set; }
    public string? ReleaseDate { get; set; }
    public int? MetacriticScore { get; set; }

    public List<string> Genres { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public List<string> Developers { get; set; } = new();
    public List<string> Publishers { get; set; } = new();

    /// <summary>True when the store lists category "Steam Achievements" (id 22).</summary>
    public bool HasAchievements { get; set; }
}
