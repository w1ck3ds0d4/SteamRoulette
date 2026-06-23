using Microsoft.Win32;

namespace SteamRoulette.Core.Steam;

/// <summary>Locates the Steam install and its library folders on Windows.</summary>
public static class SteamPaths
{
    /// <summary>
    /// Best-effort Steam install directory: the registry first (handles non-default
    /// installs), then the common default locations. Null if Steam can't be found.
    /// </summary>
    public static string? FindSteamPath()
    {
        // HKCU is set per-user by the Steam client and is the most reliable source.
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            if (key?.GetValue("SteamPath") is string p && Directory.Exists(p))
                return Path.GetFullPath(p);
        }
        catch { /* registry unavailable - fall through */ }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
            if (key?.GetValue("InstallPath") is string p && Directory.Exists(p))
                return Path.GetFullPath(p);
        }
        catch { /* registry unavailable - fall through */ }

        foreach (var candidate in new[]
                 {
                     @"C:\Program Files (x86)\Steam",
                     @"C:\Program Files\Steam",
                 })
        {
            if (Directory.Exists(candidate)) return candidate;
        }

        return null;
    }

    /// <summary>
    /// Every Steam library folder (the main install plus any extra drives configured in
    /// libraryfolders.vdf). Each returned path contains a "steamapps" subdirectory.
    /// </summary>
    public static IReadOnlyList<string> LibraryFolders(string steamPath)
    {
        var folders = new List<string> { steamPath };

        var vdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (File.Exists(vdf))
        {
            try
            {
                var root = VdfParser.Parse(File.ReadAllText(vdf))["libraryfolders"];
                if (root is not null)
                {
                    foreach (var (_, entry) in root.Children)
                    {
                        if (entry["path"]?.Value is string path && Directory.Exists(path))
                            folders.Add(path);
                    }
                }
            }
            catch { /* malformed vdf - just use what we have */ }
        }

        return folders
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
