using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SteamRoulette.App;

/// <summary>Interaction logic for App.xaml.</summary>
public partial class App : Application
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SteamRoulette", "crash.log");

    public App()
    {
        // A background enrichment glitch should never take the whole app down. Log every
        // unhandled error and keep the UI alive where we safely can.
        DispatcherUnhandledException += (_, e) =>
        {
            Log("Dispatcher", e.Exception);
            e.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log("AppDomain", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log("Task", e.Exception);
            e.SetObserved();
        };
    }

    private static void Log(string source, Exception? ex)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, $"[{DateTime.Now:O}] {source}: {ex}\n\n");
        }
        catch
        {
            // Never let logging itself throw.
        }
    }
}
