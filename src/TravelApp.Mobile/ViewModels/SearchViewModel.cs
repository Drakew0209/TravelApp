using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Text;
using TravelApp.Models;
using TravelApp.Models.Contracts;
using TravelApp.Resources.Strings;
using TravelApp.Services;
using TravelApp.Services.Api;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public class SearchViewModel : INotifyPropertyChanged
{
    private readonly List<SearchDestinationItem> _allDestinations = [];
    private string _searchQuery = string.Empty;
    private bool _isFilterOpen;
    private bool _popularMostRated;
    private bool _tourEnabled = true;
    private bool _museumEnabled = true;
    private bool _questEnabled = true;
    private readonly IPoiApiClient _poiApiClient;
    private readonly ILocalDatabaseService _localDatabaseService;
    private readonly ApiClientOptions _apiOptions;

    private static bool IsVietnamese => UserProfileService.PreferredLanguage.StartsWith("vi", StringComparison.OrdinalIgnoreCase);

    public ObservableCollection<SearchDestinationItem> PopularDestinations { get; }
    public ObservableCollection<SearchDestinationItem> SearchResults { get; }
    public ObservableCollection<TourTypeOption> TourTypes { get; }

    public string SearchTitleText => AppStrings.SearchTitle;
    public string CurrentLanguageText => $"{AppStrings.LanguagePrefix} {UserProfileService.GetLanguageDisplayText(UserProfileService.PreferredLanguage)}";
    public string SearchPlaceholderText => AppStrings.SearchPlaceholder;
    public string SearchHeaderText => string.IsNullOrWhiteSpace(SearchQuery) ? AppStrings.PopularDestinationsTitle : AppStrings.SearchResultsTitle;
    public string FilterTitleText => AppStrings.FilterTitle;
    public string PopularMostRatedGuidesText => $"★   {AppStrings.PopularAndMostRatedGuides}";
    public string FilterByCategoryText => $"▲   {AppStrings.FilterByCategory}";
    public string FilterByTourTypeText => $"🅰   {AppStrings.FilterByTourType}";
    public string TourCategoryText => IsVietnamese ? "Tour" : "Tour";
    public string MuseumCategoryText => IsVietnamese ? "Bảo tàng" : "Museum";
    public string QuestCategoryText => IsVietnamese ? "Nhiệm vụ" : "Quest";
    public string ApplyFilterText => AppStrings.ApplyFilter;
    public string ClearFiltersText => AppStrings.ClearFilters;

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery == value)
            {
                return;
            }

            _searchQuery = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SearchHeaderText));
            RebuildSearchResults();
        }
    }

    public bool IsFilterOpen
    {
        get => _isFilterOpen;
        set
        {
            if (_isFilterOpen == value) return;
            _isFilterOpen = value;
            OnPropertyChanged();
        }
    }

    public bool PopularMostRated
    {
        get => _popularMostRated;
        set
        {
            if (_popularMostRated == value) return;
            _popularMostRated = value;
            OnPropertyChanged();
        }
    }

    public bool TourEnabled
    {
        get => _tourEnabled;
        set
        {
            if (_tourEnabled == value) return;
            _tourEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool MuseumEnabled
    {
        get => _museumEnabled;
        set
        {
            if (_museumEnabled == value) return;
            _museumEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool QuestEnabled
    {
        get => _questEnabled;
        set
        {
            if (_questEnabled == value) return;
            _questEnabled = value;
            OnPropertyChanged();
        }
    }

    public ICommand BackCommand { get; }
    public ICommand OpenFilterCommand { get; }
    public ICommand CloseFilterCommand { get; }
    public ICommand ApplyFilterCommand { get; }
    public ICommand ClearFiltersCommand { get; }
    public ICommand ToggleTourTypeCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand OpenDestinationCommand { get; }

    public SearchViewModel(IPoiApiClient poiApiClient, ILocalDatabaseService localDatabaseService, ApiClientOptions apiOptions)
    {
        _poiApiClient = poiApiClient;
        _localDatabaseService = localDatabaseService;
        _apiOptions = apiOptions;
        PopularDestinations = [];
        SearchResults = [];
        TourTypes = new ObservableCollection<TourTypeOption>
        {
            new() { Name = AppStrings.CarTour, IsSelected = true },
            new() { Name = AppStrings.WalkingTour, IsSelected = true },
            new() { Name = AppStrings.BikeTour, IsSelected = true },
            new() { Name = AppStrings.BusTour, IsSelected = true },
            new() { Name = AppStrings.BoatTour, IsSelected = true },
            new() { Name = AppStrings.RunningTour, IsSelected = true },
            new() { Name = AppStrings.TrainTour, IsSelected = true },
            new() { Name = AppStrings.HorseRidingTour, IsSelected = true }
        };

        UserProfileService.ProfileChanged += OnProfileChanged;

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        OpenFilterCommand = new Command(() => IsFilterOpen = true);
        CloseFilterCommand = new Command(() => IsFilterOpen = false);
        ApplyFilterCommand = new Command(() => IsFilterOpen = false);
        SearchCommand = new Command(() => ApplySearch());
        OpenDestinationCommand = new Command<SearchDestinationItem>(async item => await OpenDestinationAsync(item));
        ToggleTourTypeCommand = new Command<TourTypeOption>(option =>
        {
            if (option is null) return;
            option.IsSelected = !option.IsSelected;
        });
        ClearFiltersCommand = new Command(() =>
        {
            PopularMostRated = false;
            TourEnabled = false;
            MuseumEnabled = false;
            QuestEnabled = false;
            foreach (var type in TourTypes)
            {
                type.IsSelected = false;
            }
        });

        _ = LoadDestinationsAsync();
    }

    private void OnProfileChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(SearchTitleText));
        OnPropertyChanged(nameof(CurrentLanguageText));
        OnPropertyChanged(nameof(SearchPlaceholderText));
        OnPropertyChanged(nameof(SearchHeaderText));
        OnPropertyChanged(nameof(FilterTitleText));
        OnPropertyChanged(nameof(PopularMostRatedGuidesText));
        OnPropertyChanged(nameof(FilterByCategoryText));
        OnPropertyChanged(nameof(FilterByTourTypeText));
        OnPropertyChanged(nameof(TourCategoryText));
        OnPropertyChanged(nameof(MuseumCategoryText));
        OnPropertyChanged(nameof(QuestCategoryText));
        OnPropertyChanged(nameof(ApplyFilterText));
        OnPropertyChanged(nameof(ClearFiltersText));

        var localizedTourTypes = new[]
        {
            AppStrings.CarTour,
            AppStrings.WalkingTour,
            AppStrings.BikeTour,
            AppStrings.BusTour,
            AppStrings.BoatTour,
            AppStrings.RunningTour,
            AppStrings.TrainTour,
            AppStrings.HorseRidingTour
        };

        for (var i = 0; i < TourTypes.Count && i < localizedTourTypes.Length; i++)
        {
            TourTypes[i].Name = localizedTourTypes[i];
        }

        _ = LoadDestinationsAsync();
    }

    private async Task LoadDestinationsAsync()
    {
        try
        {
            var language = UserProfileService.PreferredLanguage;
            var cachedPois = await _localDatabaseService.GetPoisAsync(language);
            var cachedDestinations = BuildDestinations(cachedPois.Select(MapPoi).ToList());

            if (cachedDestinations.Count > 0)
            {
                ApplyDestinations(cachedDestinations);
            }

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                return;
            }

            var onlinePois = await _poiApiClient.GetAllAsync(language);
            if (onlinePois.Count == 0)
            {
                return;
            }

            await _localDatabaseService.SavePoisAsync(onlinePois.Select(MapPoiToMobileDto));
            var destinations = BuildDestinations(onlinePois.Select(MapPoiOnline).ToList());
            if (destinations.Count > 0)
            {
                ApplyDestinations(destinations);
            }
        }
        catch
        {
            if (_allDestinations.Count == 0)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PopularDestinations.Clear();
                    SearchResults.Clear();
                });
            }
        }
    }

    private void ApplyDestinations(IReadOnlyList<SearchDestinationItem> destinations)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _allDestinations.Clear();
            _allDestinations.AddRange(destinations);

            PopularDestinations.Clear();
            foreach (var destination in destinations.Take(2))
            {
                PopularDestinations.Add(destination);
            }

            RebuildSearchResults();
        });
    }

    private List<SearchDestinationItem> BuildDestinations(IReadOnlyList<PoiModel> pois)
    {
        var destinations = new List<SearchDestinationItem>();

        destinations.AddRange(pois
            .GroupBy(GetDestinationGroupKey)
            .Select(g => BuildDestinationItem(g.Key, g.ToList())));

        return destinations;
    }

    private static string GetDestinationGroupKey(PoiModel poi)
    {
        return FirstNonEmpty(
            poi.Category,
            poi.Subtitle,
            poi.Provider,
            poi.Location,
            poi.Title,
            "Unknown");
    }

    private SearchDestinationItem BuildDestinationItem(string name, IReadOnlyList<PoiModel> pois)
    {
        return new SearchDestinationItem
        {
            Name = name,
            Type = "DESTINATION",
            Count = pois.Count,
            ImageUrl = FirstNonEmpty(pois.FirstOrDefault()?.ImageUrl, "https://placehold.co/1200x600/png?text=Travel+App"),
            FirstPoiId = pois.MinBy(p => p.Id)?.Id ?? 0,
            SearchText = string.Join(" ", pois.SelectMany(p => new[] { p.Title, p.Subtitle, p.Description, p.Location, p.Provider }).Where(x => !string.IsNullOrWhiteSpace(x)))
        };
    }

    private PoiModel MapPoi(PoiMobileDto poi)
    {
        return new PoiModel
        {
            Id = poi.Id,
            Title = poi.Title,
            Subtitle = poi.Subtitle,
            ImageUrl = NormalizeImageUrl(poi.ImageUrl),
            Location = poi.Location,
            Latitude = poi.Latitude,
            Longitude = poi.Longitude,
            Distance = poi.DistanceMeters.HasValue ? $"{poi.DistanceMeters.Value:F0} m" : string.Empty,
            Duration = string.Empty,
            Description = poi.Description,
            Provider = string.Empty,
            Credit = string.Empty,
            Category = poi.Category
        };
    }

    private PoiMobileDto MapPoiToMobileDto(PoiDto poi)
    {
        return new PoiMobileDto
        {
            Id = poi.Id,
            Title = poi.Title,
            Subtitle = poi.Subtitle ?? string.Empty,
            Description = poi.Description ?? string.Empty,
            LanguageCode = poi.PrimaryLanguage ?? string.Empty,
            PrimaryLanguage = poi.PrimaryLanguage ?? string.Empty,
            ImageUrl = NormalizeImageUrl(poi.ImageUrl),
            Location = poi.Location,
            Latitude = poi.Latitude,
            Longitude = poi.Longitude,
            GeofenceRadiusMeters = poi.GeofenceRadiusMeters ?? 100,
            Category = poi.Category ?? string.Empty,
            SpeechText = poi.SpeechText,
            SpeechTextLanguageCode = poi.SpeechTextLanguageCode,
            UpdatedAtUtc = poi.UpdatedAtUtc,
            AudioAssets = poi.AudioAssets.Select(x => new PoiAudioMobileDto
            {
                LanguageCode = x.LanguageCode,
                AudioUrl = x.AudioUrl,
                Transcript = x.Transcript,
                IsGenerated = x.IsGenerated
            }).ToList(),
            SpeechTexts = poi.SpeechTexts.Select(x => new PoiSpeechTextMobileDto
            {
                LanguageCode = x.LanguageCode,
                Text = x.Text
            }).ToList()
        };
    }

    private PoiModel MapPoiOnline(PoiDto poi)
    {
        return new PoiModel
        {
            Id = poi.Id,
            Title = poi.Title,
            Subtitle = poi.Subtitle,
            ImageUrl = NormalizeImageUrl(poi.ImageUrl),
            Location = poi.Location,
            Latitude = poi.Latitude,
            Longitude = poi.Longitude,
            Distance = string.Empty,
            Duration = poi.Duration,
            Description = poi.Description,
            Provider = poi.Provider,
            Credit = poi.Credit,
            Category = poi.Category
        };
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    private string NormalizeImageUrl(string? imageUrl)
    {
        var normalized = ResourceUrlHelper.Normalize(imageUrl, _apiOptions.BaseUrl);
        return string.IsNullOrWhiteSpace(normalized) ? "https://placehold.co/1200x600/png?text=Travel+App" : normalized;
    }

    private void ApplySearch()
    {
        OnPropertyChanged(nameof(SearchHeaderText));
        RebuildSearchResults();
        IsFilterOpen = false;
    }

    private void RebuildSearchResults()
    {
        var query = NormalizeText(SearchQuery);
        IReadOnlyList<SearchDestinationItem> source = string.IsNullOrWhiteSpace(query)
            ? PopularDestinations.ToList()
            : _allDestinations.Where(item => NormalizeText(item.SearchText).Contains(query, StringComparison.OrdinalIgnoreCase) || NormalizeText(item.Name).Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            SearchResults.Clear();
            foreach (var item in source)
            {
                SearchResults.Add(item);
            }
        });
    }

    private async Task OpenDestinationAsync(SearchDestinationItem? item)
    {
        if (item is null || item.FirstPoiId <= 0)
        {
            return;
        }

        var languageCode = Uri.EscapeDataString(UserProfileService.PreferredLanguage);
        await Shell.Current.GoToAsync($"TourDetailPage?tourId={item.FirstPoiId}&lang={languageCode}");
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new System.Text.StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class SearchDestinationItem
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public int Count { get; set; }
    public required string ImageUrl { get; set; }
    public int FirstPoiId { get; set; }
    public string SearchText { get; set; } = string.Empty;
}

public class TourTypeOption : INotifyPropertyChanged
{
    private bool _isSelected;
    private string _name = string.Empty;

    public required string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;
            _name = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
