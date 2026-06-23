using System.Net.Http;
using System.Text.Json;
using SteamRoulette.Core.Models;

namespace SteamRoulette.Core.Steam;

/// <summary>
/// Builds a game's achievement set + the player's progress by combining
/// GetSchemaForGame (definitions + icons) with GetPlayerAchievements (unlock state).
/// Needs a Web API key and the player's 64-bit SteamID.
/// </summary>
public sealed class AchievementsClient
{
    private readonly HttpClient _http;

    public AchievementsClient(HttpClient http) => _http = http;

    public async Task<GameAchievements?> GetAsync(
        int appId, string apiKey, string steamId64, CancellationToken ct = default)
    {
        var schemaUrl =
            $"https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?key={Uri.EscapeDataString(apiKey)}&appid={appId}";
        var schemaJson = await GetStringOrNull(schemaUrl, ct);
        if (schemaJson is null) return null;

        var schema = ParseSchema(schemaJson);
        if (schema.Count == 0) return new GameAchievements { AppId = appId }; // game has no achievements

        var playerUrl =
            $"https://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v1/?appid={appId}&key={Uri.EscapeDataString(apiKey)}&steamid={Uri.EscapeDataString(steamId64)}";
        var playerJson = await GetStringOrNull(playerUrl, ct);
        var progress = playerJson is null
            ? new Dictionary<string, (bool, long)>()
            : ParsePlayer(playerJson);

        return Merge(appId, schema, progress);
    }

    private async Task<string?> GetStringOrNull(string url, CancellationToken ct)
    {
        try
        {
            using var r = await _http.GetAsync(url, ct);
            return r.IsSuccessStatusCode ? await r.Content.ReadAsStringAsync(ct) : null;
        }
        catch
        {
            return null; // private profile / no stats / transient error -> treat as unknown
        }
    }

    public static List<Achievement> ParseSchema(string json)
    {
        var list = new List<Achievement>();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("game", out var game) &&
            game.TryGetProperty("availableGameStats", out var stats) &&
            stats.TryGetProperty("achievements", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var a in arr.EnumerateArray())
            {
                var ach = new Achievement
                {
                    ApiName = a.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                    DisplayName = a.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? "" : "",
                    Description = a.TryGetProperty("description", out var de) ? de.GetString() : null,
                    IconUrl = a.TryGetProperty("icon", out var ic) ? ic.GetString() : null,
                    IconGrayUrl = a.TryGetProperty("icongray", out var icg) ? icg.GetString() : null,
                };
                if (!string.IsNullOrEmpty(ach.ApiName)) list.Add(ach);
            }
        }
        return list;
    }

    public static Dictionary<string, (bool achieved, long unlockTime)> ParsePlayer(string json)
    {
        var map = new Dictionary<string, (bool, long)>();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("playerstats", out var ps) &&
            ps.TryGetProperty("achievements", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var a in arr.EnumerateArray())
            {
                if (!a.TryGetProperty("apiname", out var n) || n.GetString() is not { } name) continue;
                bool achieved = a.TryGetProperty("achieved", out var ac) && ac.TryGetInt32(out var v) && v == 1;
                long t = a.TryGetProperty("unlocktime", out var ut) && ut.TryGetInt64(out var tv) ? tv : 0;
                map[name] = (achieved, t);
            }
        }
        return map;
    }

    public static GameAchievements Merge(
        int appId, List<Achievement> schema, Dictionary<string, (bool achieved, long unlockTime)> progress)
    {
        foreach (var a in schema)
        {
            if (!progress.TryGetValue(a.ApiName, out var p)) continue;
            a.Unlocked = p.achieved;
            a.UnlockedAt = p.unlockTime > 0
                ? DateTimeOffset.FromUnixTimeSeconds(p.unlockTime).UtcDateTime
                : null;
        }
        return new GameAchievements { AppId = appId, Achievements = schema };
    }
}
