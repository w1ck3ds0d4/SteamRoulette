using SteamRoulette.Core.Models;

namespace SteamRoulette.Core.Steam;

/// <summary>Reads installed games by parsing appmanifest_*.acf files on disk (no key needed).</summary>
public sealed class LocalLibrarySource
{
    // Steam ships these as hidden "apps" in every library; they are not real games.
    private static readonly HashSet<int> NonGameAppIds = new() { 228980 /* Steamworks Common Redistributables */ };

    /// <summary>
    /// All installed games across every Steam library folder. Returns an empty list when
    /// Steam can't be located rather than throwing.
    /// </summary>
    public IReadOnlyList<SteamGame> GetInstalledGames(string? steamPath = null)
    {
        steamPath ??= SteamPaths.FindSteamPath();
        var games = new List<SteamGame>();
        if (steamPath is null) return games;

        foreach (var library in SteamPaths.LibraryFolders(steamPath))
        {
            var appsDir = Path.Combine(library, "steamapps");
            if (!Directory.Exists(appsDir)) continue;

            foreach (var manifest in Directory.EnumerateFiles(appsDir, "appmanifest_*.acf"))
            {
                if (TryParseManifest(manifest) is { } game && !NonGameAppIds.Contains(game.AppId))
                    games.Add(game);
            }
        }

        return games
            .GroupBy(g => g.AppId)
            .Select(g => g.First())
            .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static SteamGame? TryParseManifest(string path)
    {
        try
        {
            var state = VdfParser.Parse(File.ReadAllText(path))["AppState"];
            if (state is null || !int.TryParse(state["appid"]?.Value, out var appId))
                return null;

            long.TryParse(state["LastPlayed"]?.Value, out var lastPlayed);
            long.TryParse(state["SizeOnDisk"]?.Value, out var size);

            return new SteamGame
            {
                AppId = appId,
                Name = state["name"]?.Value ?? $"App {appId}",
                Installed = true,
                InstallDir = state["installdir"]?.Value,
                SizeOnDiskBytes = size,
                LastPlayed = lastPlayed > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(lastPlayed).UtcDateTime
                    : null,
                Source = GameSource.Local,
            };
        }
        catch
        {
            return null; // skip an unreadable/locked manifest
        }
    }
}
