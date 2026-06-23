using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SteamRoulette.Core;
using SteamRoulette.Core.Models;
using SteamRoulette.Core.Roulette;
using SteamRoulette.Core.Steam;

namespace SteamRoulette.App;

/// <summary>Backing state for the main window: library, filters, the current pick, status.</summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly LibraryLoader _loader;
    private readonly GameRoulette _roulette = new();
    private readonly AppSettings _settings;
    private List<SteamGame> _all = new();

    /// <summary>The games matching the current filters (what the list shows).</summary>
    public ObservableCollection<SteamGame> Games { get; } = new();

    public MainViewModel(LibraryLoader loader, AppSettings settings)
    {
        _loader = loader;
        _settings = settings;

        var f = settings.LastFilter;
        _installedOnly = f.InstalledOnly;
        _unplayedOnly = f.UnplayedOnly;
        _weightTowardBacklog = f.WeightTowardUnplayed;
        _maxHoursText = f.MaxPlaytimeMinutes is int m ? (m / 60).ToString() : "";
        _excludeDaysText = f.ExcludeRecentDays?.ToString() ?? "";
    }

    // ---- filter inputs (each re-filters the list as it changes) --------------------

    private bool _installedOnly;
    public bool InstalledOnly { get => _installedOnly; set { if (Set(ref _installedOnly, value)) ApplyFilter(); } }

    private bool _unplayedOnly;
    public bool UnplayedOnly { get => _unplayedOnly; set { if (Set(ref _unplayedOnly, value)) ApplyFilter(); } }

    private bool _weightTowardBacklog;
    public bool WeightTowardBacklog { get => _weightTowardBacklog; set => Set(ref _weightTowardBacklog, value); }

    private string _maxHoursText = "";
    public string MaxHoursText { get => _maxHoursText; set { if (Set(ref _maxHoursText, value)) ApplyFilter(); } }

    private string _excludeDaysText = "";
    public string ExcludeDaysText { get => _excludeDaysText; set { if (Set(ref _excludeDaysText, value)) ApplyFilter(); } }

    private string _search = "";
    public string Search { get => _search; set { if (Set(ref _search, value)) ApplyFilter(); } }

    // ---- current pick + status -----------------------------------------------------

    private SteamGame? _picked;
    public SteamGame? Picked
    {
        get => _picked;
        set
        {
            if (!Set(ref _picked, value)) return;
            OnPropertyChanged(nameof(HasPick));
            OnPropertyChanged(nameof(PickedTitle));
            OnPropertyChanged(nameof(PickedSubtitle));
        }
    }

    public bool HasPick => _picked is not null;
    public string PickedTitle => _picked?.Name ?? "Hit “Surprise me” to roll a game";
    public string PickedSubtitle
    {
        get
        {
            if (_picked is null) return "";
            var bits = new List<string> { _picked.Installed ? "Installed" : "Not installed" };
            if (_picked.PlaytimeMinutes > 0) bits.Add($"{_picked.PlaytimeHours:0.0} h played");
            else if (_picked.Source == GameSource.WebApi) bits.Add("never played");
            if (_picked.LastPlayed is DateTime lp) bits.Add($"last played {lp:d MMM yyyy}");
            return string.Join("  ·  ", bits);
        }
    }

    private bool _busy;
    public bool Busy { get => _busy; set { if (Set(ref _busy, value)) OnPropertyChanged(nameof(NotBusy)); } }
    public bool NotBusy => !_busy;

    private string _status = "Ready.";
    public string Status { get => _status; set => Set(ref _status, value); }

    private string _countText = "";
    public string CountText { get => _countText; set => Set(ref _countText, value); }

    // ---- actions -------------------------------------------------------------------

    public async Task LoadAsync()
    {
        Busy = true;
        Status = "Loading library…";
        try
        {
            var result = await _loader.LoadAsync(_settings);
            _all = result.Games.ToList();
            ApplyFilter();
            Status = result.Warning
                     ?? $"Loaded {_all.Count} games via {(result.UsedWebApi ? "Steam Web API" : "local files")}.";
        }
        catch (Exception ex)
        {
            Status = "Load failed: " + ex.Message;
        }
        finally
        {
            Busy = false;
        }
    }

    public void Spin()
    {
        var pick = _roulette.Pick(_all, BuildFilter());
        if (pick is null)
        {
            Status = _all.Count == 0
                ? "No games loaded yet."
                : "No games match the current filters — loosen them and spin again.";
            return;
        }
        Picked = pick;
        Status = $"Rolled: {pick.Name}";
    }

    public void Launch()
    {
        if (_picked is null) return;
        try
        {
            GameLauncher.Launch(_picked.AppId);
            Status = $"Launching {_picked.Name} via Steam…";
        }
        catch (Exception ex)
        {
            Status = "Launch failed: " + ex.Message;
        }
    }

    /// <summary>Persist the current filter so it is restored next launch.</summary>
    public void PersistFilter()
    {
        _settings.LastFilter = BuildFilter();
        _settings.Save();
    }

    private RouletteFilter BuildFilter() => new()
    {
        InstalledOnly = InstalledOnly,
        UnplayedOnly = UnplayedOnly,
        WeightTowardUnplayed = WeightTowardBacklog,
        MaxPlaytimeMinutes = int.TryParse(MaxHoursText, out var h) && h > 0 ? h * 60 : null,
        ExcludeRecentDays = int.TryParse(ExcludeDaysText, out var d) && d > 0 ? d : null,
        NameContains = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
    };

    private void ApplyFilter()
    {
        var pool = _roulette.Filter(_all, BuildFilter());
        Games.Clear();
        foreach (var g in pool) Games.Add(g);
        CountText = $"{Games.Count} of {_all.Count} games match";
    }

    // ---- INotifyPropertyChanged ----------------------------------------------------

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
