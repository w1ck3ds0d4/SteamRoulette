using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using SteamRoulette.Core;

namespace SteamRoulette.App;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        KeyBox.Text = settings.WebApiKey ?? "";
        SteamIdBox.Text = settings.SteamId ?? "";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _settings.WebApiKey = Blank(KeyBox.Text);
        _settings.SteamId = Blank(SteamIdBox.Text);
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Link_Navigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
