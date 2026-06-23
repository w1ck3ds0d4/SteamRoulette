using System.Diagnostics;

namespace SteamRoulette.Core.Steam;

/// <summary>Launches games and opens store pages via Steam's URL protocol.</summary>
public static class GameLauncher
{
    /// <summary>Ask Steam to launch (installing first if needed) the given app.</summary>
    public static void Launch(int appId) => Open($"steam://run/{appId}");

    /// <summary>Open the game's store page in the Steam client.</summary>
    public static void OpenStorePage(int appId) => Open($"steam://store/{appId}");

    private static void Open(string uri)
    {
        // UseShellExecute lets the OS resolve the steam:// protocol handler.
        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
    }
}
