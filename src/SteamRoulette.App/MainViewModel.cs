using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using SteamRoulette.Core;
using SteamRoulette.Core.Models;
using SteamRoulette.Core.Roulette;
using SteamRoulette.Core.Steam;

namespace SteamRoulette.App;

/// <summary>Backing state for the main window: library, filters, the current pick, status.</summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    /// <summary>Sentinel shown in the genre dropdown for "no genre filter".</summary>
    public const string AnyGenre = "All genres";

    /// <summary>Sentinel shown in the category dropdown for "no category filter".</summary>
    public const string AnyCategory = "All categories";

    private readonly LibraryLoader _loader;
    private readonly GameEnricher _enricher;
    private readonly GameRoulette _roulette = new();
    private readonly AppSettings _settings;
    private List<SteamGame> _all = new();
    private CancellationTokenSource? _enrichCts;

    /// <summary>The games matching the current filters (what the list shows).</summary>
    public ObservableCollection<SteamGame> Games { get; } = new();

    /// <summary>Genres discovered during enrichment, used to populate the genre dropdown.</summary>
    public ObservableCollection<string> Genres { get; } = new() { AnyGenre };

    /// <summary>Categories discovered during enrichment (Single-player, Co-op, ...).</summary>
    public ObservableCollection<string> Categories { get; } = new() { AnyCategory };

    public MainViewModel(LibraryLoader loader, GameEnricher enricher, AppSettings settings)
    {
        _loader = loader;
        _enricher = enricher;
        _settings = settings;

        var f = settings.LastFilter;
        _installedOnly = f.InstalledOnly;
        _unplayedOnly = f.UnplayedOnly;
        _weightTowardBacklog = f.WeightTowardUnplayed;
        _maxHoursText = f.MaxPlaytimeMinutes is int m ? (m / 60).ToString() : "";
        _excludeDaysText = f.ExcludeRecentDays?.ToString() ?? "";
        _requireAchievements = f.RequireAchievements;
        _onlyIncomplete = f.OnlyIncompleteAchievements;
        _minMetacriticText = f.MinMetacritic?.ToString() ?? "";
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

    private string _genre = AnyGenre;
    public string Genre { get => _genre; set { if (Set(ref _genre, value)) ApplyFilter(); } }

    private bool _requireAchievements;
    public bool RequireAchievements { get => _requireAchievements; set { if (Set(ref _requireAchievements, value)) ApplyFilter(); } }

    private bool _onlyIncomplete;
    public bool OnlyIncomplete { get => _onlyIncomplete; set { if (Set(ref _onlyIncomplete, value)) ApplyFilter(); } }

    private string _category = AnyCategory;
    public string Category { get => _category; set { if (Set(ref _category, value)) ApplyFilter(); } }

    private string _minMetacriticText = "";
    public string MinMetacriticText { get => _minMetacriticText; set { if (Set(ref _minMetacriticText, value)) ApplyFilter(); } }

    private string _enrichStatus = "";
    public string EnrichStatus { get => _enrichStatus; set => Set(ref _enrichStatus, value); }

    // ---- current pick + status -----------------------------------------------------

    private SteamGame? _picked;
    public SteamGame? Picked
    {
        get => _picked;
        set
        {
            if (!Set(ref _picked, value)) return;
            RaisePickedProps();
        }
    }

    public bool HasPick => _picked is not null;
    public string PickedTitle => _picked?.Name ?? "Hit “Surprise me” to roll a game";
    public string PickedSubtitle
    {
        get
        {
            if (_picked is null) return "Set filters, then spin the wheel.";
            var bits = new List<string> { _picked.Installed ? "Installed" : "Not installed" };
            if (_picked.LastPlayed is DateTime lp) bits.Add($"last played {lp:d MMM yyyy}");
            return string.Join("  ·  ", bits);
        }
    }

    // ---- rolled-game stats panel ---------------------------------------------------

    public IReadOnlyList<string> PickedGenres => _picked?.Genres ?? (IReadOnlyList<string>)Array.Empty<string>();
    public bool HasGenres => _picked is { Genres.Count: > 0 };

    public bool HasDescription => !string.IsNullOrWhiteSpace(_picked?.ShortDescription);
    public string PickedDescription => _picked?.ShortDescription ?? "";

    public bool HasRelease => !string.IsNullOrWhiteSpace(_picked?.ReleaseDate);
    public string PickedRelease => _picked?.ReleaseDate ?? "";

    public bool HasMetacritic => _picked?.MetacriticScore is not null;
    public string PickedMetacritic => _picked?.MetacriticScore?.ToString() ?? "";

    public bool HasRating => _picked?.ReviewPositivePercent is not null;
    public string PickedRating
    {
        get
        {
            if (_picked?.ReviewPositivePercent is not int p) return "";
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(_picked.ReviewSummary)) parts.Add(_picked.ReviewSummary!);
            parts.Add($"{p}% positive");
            if (_picked.ReviewCount is int c) parts.Add($"{c:N0} reviews");
            return string.Join("  ·  ", parts);
        }
    }

    public string PickedPlaytime => _picked is null
        ? ""
        : _picked.PlaytimeMinutes > 0 ? $"{_picked.PlaytimeHours:0.0} hours" : "Never played";

    public bool HasPickedAchievements => _picked?.AchievementTotal is int t && t > 0;
    public string PickedAchievementText
    {
        get
        {
            if (_picked?.AchievementTotal is not int t || t == 0) return "";
            int u = _picked.AchievementUnlocked ?? 0;
            return $"{u} / {t} unlocked  ({100.0 * u / t:0}%)";
        }
    }
    public double PickedAchievementPercent =>
        _picked?.AchievementTotal is int t && t > 0 ? 100.0 * (_picked.AchievementUnlocked ?? 0) / t : 0;

    private void RaisePickedProps()
    {
        foreach (var name in new[]
                 {
                     nameof(HasPick), nameof(PickedTitle), nameof(PickedSubtitle), nameof(PickedGenres),
                     nameof(HasGenres), nameof(HasDescription), nameof(PickedDescription), nameof(HasRelease),
                     nameof(PickedRelease), nameof(HasMetacritic), nameof(PickedMetacritic), nameof(PickedPlaytime),
                     nameof(HasPickedAchievements), nameof(PickedAchievementText), nameof(PickedAchievementPercent),
                     nameof(HasRating), nameof(PickedRating),
                 })
            OnPropertyChanged(name);
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
            StartEnrichment();
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
        _ = EnsureEnrichedAsync(pick);
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
        Genre = string.IsNullOrWhiteSpace(Genre) || Genre == AnyGenre ? null : Genre,
        RequireAchievements = RequireAchievements,
        OnlyIncompleteAchievements = OnlyIncomplete,
        Category = string.IsNullOrWhiteSpace(Category) || Category == AnyCategory ? null : Category,
        MinMetacritic = int.TryParse(MinMetacriticText, out var mc) && mc > 0 ? mc : null,
    };

    private void ApplyFilter()
    {
        var pool = _roulette.Filter(_all, BuildFilter());
        Games.Clear();
        foreach (var g in pool) Games.Add(g);
        CountText = $"{Games.Count} of {_all.Count} games match";
    }

    // ---- background enrichment -----------------------------------------------------

    private void StartEnrichment()
    {
        _enrichCts?.Cancel();
        _enrichCts = new CancellationTokenSource();
        _ = EnrichAsync(_all.ToList(), _enrichCts.Token);
    }

    /// <summary>
    /// Fills in genres / categories / achievement progress in the background. Started from
    /// the UI thread without ConfigureAwait(false), so continuations resume on the UI
    /// thread and it is safe to touch Games/Genres directly. The awaits yield, so the UI
    /// stays responsive through the rate-limited scan; results are cached to disk.
    /// </summary>
    private async Task EnrichAsync(List<SteamGame> games, CancellationToken ct)
    {
        try
        {
            // Pass 1: store metadata (genres, categories, has-achievements).
            for (int i = 0; i < games.Count; i++)
            {
                if (ct.IsCancellationRequested) return;
                var g = games[i];
                try
                {
                    var meta = await _enricher.GetMetadataAsync(g.AppId, ct);
                    if (meta is not null)
                    {
                        g.Genres = meta.Genres;
                        g.Categories = meta.Categories;
                        g.HasAchievements = meta.HasAchievements;
                        g.ShortDescription = meta.ShortDescription;
                        g.ReleaseDate = meta.ReleaseDate;
                        g.MetacriticScore = meta.MetacriticScore;
                        foreach (var genre in meta.Genres)
                            if (!Genres.Contains(genre)) Genres.Add(genre);
                        foreach (var cat in meta.Categories)
                            if (!Categories.Contains(cat)) Categories.Add(cat);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
                catch { /* skip a game we couldn't read */ }

                if ((i + 1) % 10 == 0 || i + 1 == games.Count)
                {
                    EnrichStatus = $"Loading game details… {i + 1}/{games.Count}";
                    ApplyFilter();
                }
            }

            // Pass 2: achievement progress - needs a key + SteamID, only for games that have any.
            if (!string.IsNullOrWhiteSpace(_settings.WebApiKey) &&
                !string.IsNullOrWhiteSpace(_settings.SteamId))
            {
                var withAch = games.Where(g => g.HasAchievements == true).ToList();
                for (int i = 0; i < withAch.Count; i++)
                {
                    if (ct.IsCancellationRequested) return;
                    var g = withAch[i];
                    try
                    {
                        var ach = await _enricher.GetAchievementsAsync(
                            g.AppId, _settings.WebApiKey!, _settings.SteamId!, ct);
                        if (ach is not null)
                        {
                            g.AchievementTotal = ach.Total;
                            g.AchievementUnlocked = ach.UnlockedCount;
                        }
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
                    catch { }

                    if ((i + 1) % 10 == 0 || i + 1 == withAch.Count)
                    {
                        EnrichStatus = $"Loading achievements… {i + 1}/{withAch.Count}";
                        ApplyFilter();
                    }
                }
            }

            EnrichStatus = "";
            ApplyFilter();
        }
        catch (OperationCanceledException) { /* reload or close cancelled the scan */ }
    }

    /// <summary>
    /// Make sure the just-picked game has its store + achievement data so the stats panel
    /// is fully populated even if the background scan hasn't reached it yet. Cache-first,
    /// so it is instant when the game is already enriched.
    /// </summary>
    private async Task EnsureEnrichedAsync(SteamGame game)
    {
        try
        {
            if (game.Genres.Count == 0 || game.HasAchievements is null)
            {
                var meta = await _enricher.GetMetadataAsync(game.AppId);
                if (meta is not null)
                {
                    game.Genres = meta.Genres;
                    game.Categories = meta.Categories;
                    game.HasAchievements = meta.HasAchievements;
                    game.ShortDescription = meta.ShortDescription;
                    game.ReleaseDate = meta.ReleaseDate;
                    game.MetacriticScore = meta.MetacriticScore;
                }
            }
            if (game.HasAchievements == true && game.AchievementTotal is null &&
                !string.IsNullOrWhiteSpace(_settings.WebApiKey) && !string.IsNullOrWhiteSpace(_settings.SteamId))
            {
                var ach = await _enricher.GetAchievementsAsync(
                    game.AppId, _settings.WebApiKey!, _settings.SteamId!);
                if (ach is not null)
                {
                    game.AchievementTotal = ach.Total;
                    game.AchievementUnlocked = ach.UnlockedCount;
                }
            }
            if (game.ReviewPositivePercent is null)
            {
                var rev = await _enricher.GetReviewsAsync(game.AppId);
                if (rev is not null)
                {
                    game.ReviewSummary = rev.Description;
                    game.ReviewPositivePercent = rev.PositivePercent;
                    game.ReviewCount = rev.Total;
                }
            }
        }
        catch { /* best effort; the panel just shows what we have */ }

        if (ReferenceEquals(game, _picked)) RaisePickedProps();
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
