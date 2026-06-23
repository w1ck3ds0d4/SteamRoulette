using System.Text.Json;
using System.Text.Json.Serialization;
using SteamRoulette.Core.Models;

namespace SteamRoulette.Core;

/// <summary>
/// User settings, persisted as JSON under %APPDATA%\SteamRoulette. Holds the optional Web
/// API key + SteamID and the last-used roulette filter.
/// </summary>
public sealed class AppSettings
{
    public string? WebApiKey { get; set; }
    public string? SteamId { get; set; }
    public RouletteFilter LastFilter { get; set; } = new();

    [JsonIgnore]
    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SteamRoulette", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch { /* corrupt/locked settings - start fresh rather than crash */ }
        return new AppSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
    }
}
