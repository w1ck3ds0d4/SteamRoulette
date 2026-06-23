using System.Text.Json;
using SteamRoulette.Core.Models;

namespace SteamRoulette.Core.Steam;

/// <summary>
/// Reads the full owned-games list from the Steam Web API
/// (IPlayerService/GetOwnedGames). Needs a Web API key and the user's 64-bit SteamID,
/// and the account's game details must be public (or the key must own the request).
/// </summary>
public sealed class WebApiLibrarySource
{
    private readonly HttpClient _http;

    public WebApiLibrarySource(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<SteamGame>> GetOwnedGamesAsync(
        string apiKey, string steamId, CancellationToken ct = default)
    {
        var url =
            "https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/" +
            $"?key={Uri.EscapeDataString(apiKey)}" +
            $"&steamid={Uri.EscapeDataString(steamId)}" +
            "&include_appinfo=1&include_played_free_games=1&format=json";

        using var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var games = new List<SteamGame>();
        if (!doc.RootElement.TryGetProperty("response", out var response) ||
            !response.TryGetProperty("games", out var arr))
        {
            // A key/steamid mismatch or a private profile returns an empty "response".
            return games;
        }

        foreach (var g in arr.EnumerateArray())
        {
            int appId = g.GetProperty("appid").GetInt32();
            string name = g.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
            int playtime = g.TryGetProperty("playtime_forever", out var pt) ? pt.GetInt32() : 0;
            long lastPlayed = g.TryGetProperty("rtime_last_played", out var rt) ? rt.GetInt64() : 0;
            string? iconHash = g.TryGetProperty("img_icon_url", out var ic) ? ic.GetString() : null;

            games.Add(new SteamGame
            {
                AppId = appId,
                Name = name,
                PlaytimeMinutes = playtime,
                LastPlayed = lastPlayed > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(lastPlayed).UtcDateTime
                    : null,
                IconUrl = !string.IsNullOrEmpty(iconHash)
                    ? $"https://media.steampowered.com/steamcommunity/public/images/apps/{appId}/{iconHash}.jpg"
                    : null,
                Installed = false,
                Source = GameSource.WebApi,
            });
        }

        return games;
    }
}
