using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using SteamRoulette.Core;
using SteamRoulette.Core.Models;
using SteamRoulette.Core.Steam;

namespace SteamRoulette.App;

public partial class MainWindow : Window
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

    private readonly AppSettings _settings;
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        DarkTitleBar.Apply(this);

        _settings = AppSettings.Load();
        var loader = new LibraryLoader(new WebApiLibrarySource(Http), new LocalLibrarySource());
        var enricher = new GameEnricher(Http);
        _vm = new MainViewModel(loader, enricher, _settings);
        DataContext = _vm;

        Loaded += async (_, _) => await _vm.LoadAsync();
        Closing += (_, _) => _vm.PersistFilter();
    }

    private async void Reload_Click(object sender, RoutedEventArgs e) => await _vm.LoadAsync();

    private void Spin_Click(object sender, RoutedEventArgs e) => _vm.Spin();

    private void Launch_Click(object sender, RoutedEventArgs e) => _vm.Launch();

    private void GamesList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (GamesList.SelectedItem is SteamGame game)
            _vm.Picked = game; // select it as the pick; user can then Launch
    }

    private async void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_settings) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _settings.Save();
            await _vm.LoadAsync(); // re-load now that credentials may have changed
        }
    }
}
