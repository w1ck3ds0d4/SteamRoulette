using System.Net.Http;
using System.Text.Json;
using SteamRoulette.Core.Models;

namespace SteamRoulette.Core.Steam;

/// <summary>
/// Reads store metadata (genres, categories, description, art, Metacritic) from Steam's
/// public appdetails endpoint. No API key required; the endpoint is rate-limited, so
/// callers should throttle + cache (see <see cref="GameEnricher"/>).
/// </summary>
/// <summary>Steam user-review summary for a game (e.g. "Very Positive", 96%, 50000 reviews).</summary>
public sealed record ReviewSummary(string Description, int PositivePercent, int Total);

public sealed class SteamStoreClient
{
    private readonly HttpClient _http;

    public SteamStoreClient(HttpClient http) => _http = http;

    public async Task<GameMetadata?> GetMetadataAsync(int appId, CancellationToken ct = default)
    {
        var url = $"https://store.steampowered.com/api/appdetails?appids={appId}&l=english";
        using var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;
        var json = await resp.Content.ReadAsStringAsync(ct);
        return ParseAppDetails(json, appId);
    }

    /// <summary>The Steam user-review summary (the "Very Positive" rating), via the public appreviews endpoint.</summary>
    public async Task<ReviewSummary?> GetReviewsAsync(int appId, CancellationToken ct = default)
    {
        var url = $"https://store.steampowered.com/appreviews/{appId}?json=1&language=all&purchase_type=all&num_per_page=0";
        using var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;
        var json = await resp.Content.ReadAsStringAsync(ct);
        return ParseReviews(json);
    }

    /// <summary>Pure parser for the appreviews query_summary. Null when there are no reviews.</summary>
    public static ReviewSummary? ParseReviews(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("query_summary", out var qs)) return null;
        int total = qs.TryGetProperty("total_reviews", out var tr) && tr.TryGetInt32(out var t) ? t : 0;
        if (total <= 0) return null;
        int positive = qs.TryGetProperty("total_positive", out var tp) && tp.TryGetInt32(out var p) ? p : 0;
        string desc = qs.TryGetProperty("review_score_desc", out var d) ? d.GetString() ?? "" : "";
        int percent = (int)Math.Round(100.0 * positive / total);
        return new ReviewSummary(desc, percent, total);
    }

    /// <summary>Pure parser, split out so it can be unit-tested without the network.</summary>
    public static GameMetadata? ParseAppDetails(string json, int appId)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        // The response is keyed by the appid as a string; "success" can be false for
        // delisted/region-locked apps.
        if (!root.TryGetProperty(appId.ToString(), out var entry)) return null;
        if (!entry.TryGetProperty("success", out var ok) || !ok.GetBoolean()) return null;
        if (!entry.TryGetProperty("data", out var data)) return null;

        var meta = new GameMetadata { AppId = appId };
        if (data.TryGetProperty("short_description", out var sd)) meta.ShortDescription = sd.GetString();
        if (data.TryGetProperty("header_image", out var hi)) meta.HeaderImage = hi.GetString();
        if (data.TryGetProperty("release_date", out var rd) && rd.TryGetProperty("date", out var rdd))
            meta.ReleaseDate = rdd.GetString();
        if (data.TryGetProperty("metacritic", out var mc) && mc.TryGetProperty("score", out var sc)
            && sc.TryGetInt32(out var score))
            meta.MetacriticScore = score;

        meta.Genres = ReadDescriptions(data, "genres");
        meta.Categories = ReadDescriptions(data, "categories");
        meta.Developers = ReadStringArray(data, "developers");
        meta.Publishers = ReadStringArray(data, "publishers");
        meta.HasAchievements = meta.Categories.Any(
            c => c.Equals("Steam Achievements", StringComparison.OrdinalIgnoreCase));
        return meta;
    }

    private static List<string> ReadDescriptions(JsonElement data, string prop)
    {
        var list = new List<string>();
        if (data.TryGetProperty(prop, out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var e in arr.EnumerateArray())
                if (e.TryGetProperty("description", out var d) && d.GetString() is { } s) list.Add(s);
        return list;
    }

    private static List<string> ReadStringArray(JsonElement data, string prop)
    {
        var list = new List<string>();
        if (data.TryGetProperty(prop, out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var e in arr.EnumerateArray())
                if (e.GetString() is { } s) list.Add(s);
        return list;
    }
}
