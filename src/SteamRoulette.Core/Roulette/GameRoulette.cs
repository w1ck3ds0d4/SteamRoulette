using SteamRoulette.Core.Models;

namespace SteamRoulette.Core.Roulette;

/// <summary>Applies a <see cref="RouletteFilter"/> and randomly picks a game.</summary>
public sealed class GameRoulette
{
    private readonly Random _rng;

    /// <param name="rng">Inject a seeded Random for deterministic tests; defaults to shared.</param>
    public GameRoulette(Random? rng = null) => _rng = rng ?? Random.Shared;

    /// <summary>The games that satisfy every active constraint in <paramref name="filter"/>.</summary>
    public IReadOnlyList<SteamGame> Filter(IEnumerable<SteamGame> games, RouletteFilter filter)
    {
        var now = DateTime.UtcNow;
        return games.Where(g =>
        {
            if (filter.InstalledOnly && !g.Installed) return false;
            if (filter.UnplayedOnly && g.PlaytimeMinutes > 0) return false;
            if (filter.MaxPlaytimeMinutes is int max && g.PlaytimeMinutes > max) return false;
            if (filter.ExcludeRecentDays is int days && g.LastPlayed is DateTime last &&
                (now - last).TotalDays < days) return false;
            if (!string.IsNullOrWhiteSpace(filter.NameContains) &&
                !g.Name.Contains(filter.NameContains, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrWhiteSpace(filter.Genre) &&
                !g.Genres.Any(x => x.Equals(filter.Genre, StringComparison.OrdinalIgnoreCase))) return false;
            if (filter.RequireAchievements && g.HasAchievements != true) return false;
            if (filter.OnlyIncompleteAchievements &&
                !(g.HasAchievements == true && g.AchievementsComplete == false)) return false;
            if (!string.IsNullOrWhiteSpace(filter.Category) &&
                !g.Categories.Any(x => x.Equals(filter.Category, StringComparison.OrdinalIgnoreCase))) return false;
            if (filter.MinMetacritic is int minMc && (g.MetacriticScore is null || g.MetacriticScore < minMc)) return false;
            return true;
        }).ToList();
    }

    /// <summary>Pick one game from the filtered pool, or null if nothing qualifies.</summary>
    public SteamGame? Pick(IEnumerable<SteamGame> games, RouletteFilter filter)
    {
        var pool = Filter(games, filter);
        if (pool.Count == 0) return null;

        if (!filter.WeightTowardUnplayed)
            return pool[_rng.Next(pool.Count)];

        // Weighted draw: weight falls off as playtime rises, so the backlog wins more often.
        var weights = pool.Select(g => 1.0 / (1.0 + g.PlaytimeHours)).ToArray();
        double target = _rng.NextDouble() * weights.Sum();
        for (int i = 0; i < pool.Count; i++)
        {
            target -= weights[i];
            if (target <= 0) return pool[i];
        }
        return pool[^1]; // floating-point guard
    }
}
