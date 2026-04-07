using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public class SearchViewModel : INotifyPropertyChanged
{
    private bool _isFilterOpen;
    private bool _popularMostRated;
    private bool _tourEnabled = true;
    private bool _museumEnabled = true;
    private bool _questEnabled = true;
    private readonly IPoiApiClient _poiApiClient;

    public ObservableCollection<SearchDestinationItem> PopularDestinations { get; }
    public ObservableCollection<TourTypeOption> TourTypes { get; }

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

    public SearchViewModel(IPoiApiClient poiApiClient)
    {
        _poiApiClient = poiApiClient;
        PopularDestinations = [];
        TourTypes = new ObservableCollection<TourTypeOption>
        {
            new() { Name = "Car tour", IsSelected = true },
            new() { Name = "Walking tour", IsSelected = true },
            new() { Name = "Bike tour", IsSelected = true },
            new() { Name = "Bus tour", IsSelected = true },
            new() { Name = "Boat tour", IsSelected = true },
            new() { Name = "Running tour", IsSelected = true },
            new() { Name = "Train tour", IsSelected = true },
            new() { Name = "Horse riding tour", IsSelected = true }
        };

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        OpenFilterCommand = new Command(() => IsFilterOpen = true);
        CloseFilterCommand = new Command(() => IsFilterOpen = false);
        ApplyFilterCommand = new Command(() => IsFilterOpen = false);
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

    private async Task LoadDestinationsAsync()
    {
        try
        {
            var language = UserProfileService.PreferredLanguage;
            var pois = await _poiApiClient.GetAllAsync(language);

            // Group POIs by location and create destination items
            var destinations = pois
                .GroupBy(p => p.Subtitle)
                .Select(g => new SearchDestinationItem
                {
                    Name = g.Key ?? "Unknown",
                    Type = "DESTINATION",
                    Count = g.Count(),
                    ImageUrl = g.FirstOrDefault()?.ImageUrl ?? "https://images.unsplash.com/photo-1488646953014-85cb44e25828?w=1200&h=600&fit=crop"
                })
                .ToList();

            if (destinations.Count == 0)
            {
                LoadMockDestinations();
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PopularDestinations.Clear();
                    foreach (var destination in destinations)
                    {
                        PopularDestinations.Add(destination);
                    }
                });
            }
        }
        catch
        {
            LoadMockDestinations();
        }
    }

    private void LoadMockDestinations()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            PopularDestinations.Clear();
            PopularDestinations.Add(new() { Name = "🍲 Ho Chi Minh Food Tour", Type = "DESTINATION", Count = 3, ImageUrl = "https://images.unsplash.com/photo-1564078516577-e37020a4c3f0?w=1200&h=600&fit=crop" });
            PopularDestinations.Add(new() { Name = "🍜 Hanoi Food Tour", Type = "DESTINATION", Count = 3, ImageUrl = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=1200&h=600&fit=crop" });
        });
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
}

public class TourTypeOption : INotifyPropertyChanged
{
    private bool _isSelected;

    public required string Name { get; set; }

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
