using SteamRoulette.Core.Models;

namespace SteamRoulette.Core.Steam;

/// <summary>Outcome of a library load, including which source actually supplied the data.</summary>
public sealed record LibraryResult(
    IReadOnlyList<SteamGame> Games,
    bool UsedWebApi,
    string? Warning);

/// <summary>
/// Loads the library from the best available source: the Web API when a key + SteamID are
/// configured (full owned list with playtime, installed flag merged in from disk), falling
/// back to local appmanifest parsing otherwise or when the Web API call fails.
/// </summary>
public sealed class LibraryLoader
{
    private readonly WebApiLibrarySource _web;
    private readonly LocalLibrarySource _local;

    public LibraryLoader(WebApiLibrarySource web, LocalLibrarySource local)
    {
        _web = web;
        _local = local;
    }

    public async Task<LibraryResult> LoadAsync(AppSettings settings, CancellationToken ct = default)
    {
        var installed = _local.GetInstalledGames();
        var installedById = installed.ToDictionary(g => g.AppId);

        bool haveCredentials =
            !string.IsNullOrWhiteSpace(settings.WebApiKey) &&
            !string.IsNullOrWhiteSpace(settings.SteamId);

        if (haveCredentials)
        {
            try
            {
                var owned = await _web.GetOwnedGamesAsync(settings.WebApiKey!, settings.SteamId!, ct);
                if (owned.Count > 0)
                {
                    foreach (var game in owned)
                    {
                        if (!installedById.TryGetValue(game.AppId, out var local)) continue;
                        game.Installed = true;
                        game.InstallDir = local.InstallDir;
                        game.InstallPath = local.InstallPath;
                        game.SizeOnDiskBytes = local.SizeOnDiskBytes;
                        game.LastPlayed ??= local.LastPlayed;
                    }

                    var ordered = owned.OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase).ToList();
                    return new LibraryResult(ordered, UsedWebApi: true, Warning: null);
                }

                return new LibraryResult(installed, UsedWebApi: false,
                    Warning: "Web API returned no games (private profile or wrong SteamID?). Showing installed games only.");
            }
            catch (Exception ex)
            {
                return new LibraryResult(installed, UsedWebApi: false,
                    Warning: $"Web API failed ({ex.Message}). Showing installed games only.");
            }
        }

        var warning = installed.Count == 0
            ? "No installed games found and no Web API key set. Add a key + SteamID in Settings, or install a game."
            : null;
        return new LibraryResult(installed, UsedWebApi: false, warning);
    }
}
