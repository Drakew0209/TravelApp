using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TravelApp.Models;
using TravelApp.Models.Contracts;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public class TourDetailViewModel : INotifyPropertyChanged
{
    private readonly Dictionary<string, string> _speechTextsByLanguage = new(StringComparer.OrdinalIgnoreCase);
    private readonly ObservableCollection<SpeechLanguageOption> _speechLanguages = [];
    private PoiModel? _tour;
    private PoiDto? _currentPoiDto;
    private string _speechTextInput = string.Empty;
    private string _selectedSpeechLanguageCode = "vi";
    private bool _isSavingSpeechText;
    private bool _suppressSpeechTextAutoSave;
    private bool _isSpeechLanguageMenuOpen;
    private bool _isBookmarked;
    private CancellationTokenSource? _speechTextAutoSaveCts;
    private readonly IPoiApiClient _poiApiClient;
    private readonly ILocalDatabaseService _localDatabaseService;
    private readonly IAudioLibraryService _audioLibraryService;
    private readonly IBookmarkHistoryService _bookmarkHistoryService;
    private readonly ITourRouteCatalogService _tourRouteCatalogService;
    private readonly TravelApp.Services.Runtime.TourRouteCacheService _tourRouteCacheService;

    public PoiModel? Tour
    {
        get => _tour;
        private set
        {
            _tour = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProviderName));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(Credit));
            OnPropertyChanged(nameof(SpeechTextInput));
        }
    }

    public bool IsBookmarked
    {
        get => _isBookmarked;
        private set
        {
            if (_isBookmarked == value)
            {
                return;
            }

            _isBookmarked = value;
            OnPropertyChanged();
        }
    }

    public string SpeechTextInput
    {
        get => _speechTextInput;
        set
        {
            if (_speechTextInput == value)
            {
                return;
            }

            _speechTextInput = value;
            OnPropertyChanged();

            if (!_suppressSpeechTextAutoSave)
            {
                ScheduleSpeechTextAutoSave();
            }
        }
    }

    public bool IsSavingSpeechText
    {
        get => _isSavingSpeechText;
        private set
        {
            if (_isSavingSpeechText == value)
            {
                return;
            }

            _isSavingSpeechText = value;
            OnPropertyChanged();
        }
    }

    public string ProviderName => Tour?.Provider ?? "TravelApp";
    public string Description => Tour?.SpeechText ?? Tour?.Description ?? "This tour is available daily and includes the most iconic landmarks in the area.";
    public string Credit => Tour?.Credit ?? string.Empty;
    public string SelectedSpeechLanguageDisplayText => GetLanguageDisplayText(SelectedSpeechLanguageCode);
    public ObservableCollection<SpeechLanguageOption> SpeechLanguages => _speechLanguages;

    public ICommand BackCommand { get; }
    public ICommand ViewTourCommand { get; }
    public ICommand SaveSpeechTextCommand { get; }
    public ICommand DownloadTourCommand { get; }
    public ICommand ToggleBookmarkCommand { get; }
    public ICommand ToggleSpeechLanguageMenuCommand { get; }
    public ICommand CloseSpeechLanguageMenuCommand { get; }
    public ICommand SelectSpeechLanguageCommand { get; }

    public string SelectedSpeechLanguageCode
    {
        get => _selectedSpeechLanguageCode;
        private set
        {
            var normalized = NormalizeLanguageCode(value);
            if (string.Equals(_selectedSpeechLanguageCode, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _selectedSpeechLanguageCode = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedSpeechLanguageDisplayText));
        }
    }

    private async Task ToggleBookmarkAsync()
    {
        if (Tour is null)
        {
            return;
        }

        try
        {
            await _bookmarkHistoryService.ToggleBookmarkAsync(Tour, CancellationToken.None);
            IsBookmarked = await _bookmarkHistoryService.IsBookmarkedAsync(Tour.Id, CancellationToken.None);
            await Shell.Current.DisplayAlert("Bookmarks", IsBookmarked ? "Tour đã được lưu vào bookmarks." : "Tour đã được xóa khỏi bookmarks.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Không thể cập nhật bookmark: {ex.Message}", "OK");
        }
    }

    public bool IsSpeechLanguageMenuOpen
    {
        get => _isSpeechLanguageMenuOpen;
        private set
        {
            if (_isSpeechLanguageMenuOpen == value)
            {
                return;
            }

            _isSpeechLanguageMenuOpen = value;
            OnPropertyChanged();
        }
    }

    public TourDetailViewModel(ITourRouteCatalogService tourRouteCatalogService, IPoiApiClient poiApiClient, ILocalDatabaseService localDatabaseService, IAudioLibraryService audioLibraryService, IBookmarkHistoryService bookmarkHistoryService, TravelApp.Services.Runtime.TourRouteCacheService tourRouteCacheService)
    {
        _tourRouteCatalogService = tourRouteCatalogService;
        _poiApiClient = poiApiClient;
        _localDatabaseService = localDatabaseService;
        _audioLibraryService = audioLibraryService;
        _bookmarkHistoryService = bookmarkHistoryService;
        _tourRouteCacheService = tourRouteCacheService;
        BackCommand = new Command(async () =>
        {
            await StopAsync();
            await Shell.Current.GoToAsync("..");
        });
        ViewTourCommand = new Command(async () =>
        {
            if (Tour is null)
            {
                return;
            }

            await SaveSpeechTextAsync(showConfirmation: false);
            await Shell.Current.GoToAsync($"TourMapRoutePage?tourId={Tour.Id}&poiId={Tour.Id}&lang={Uri.EscapeDataString(SelectedSpeechLanguageCode)}");
        });
        SaveSpeechTextCommand = new Command(async () => await SaveSpeechTextAsync());
        DownloadTourCommand = new Command(async () => await DownloadTourAsync());
        ToggleBookmarkCommand = new Command(async () => await ToggleBookmarkAsync());
        ToggleSpeechLanguageMenuCommand = new Command(() => IsSpeechLanguageMenuOpen = !IsSpeechLanguageMenuOpen);
        CloseSpeechLanguageMenuCommand = new Command(() => IsSpeechLanguageMenuOpen = false);
        SelectSpeechLanguageCommand = new Command<SpeechLanguageOption>(async option => await SelectSpeechLanguageAsync(option));
    }

    private async Task DownloadTourAsync()
    {
        if (Tour is null)
        {
            return;
        }

        try
        {
            var downloaded = await _audioLibraryService.DownloadAsync(Tour.Id, SelectedSpeechLanguageCode, CancellationToken.None);
            var message = downloaded
                ? $"Tour '{Tour.Title}' đã được thêm vào mục download."
                : $"Tour '{Tour.Title}' đã có trong download hoặc đang chờ tải.";

            await Shell.Current.DisplayAlert("Download tour", message, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Không thể download tour: {ex.Message}", "OK");
        }
    }

    public Task PersistSpeechTextAsync()
    {
        CancelSpeechTextAutoSave();
        return SaveSpeechTextAsync(showConfirmation: false);
    }

    public async Task StopAsync()
    {
        CancelSpeechTextAutoSave();
        await SaveSpeechTextAsync(showConfirmation: false);
    }

    public void Load(string? tourId)
    {
        if (!int.TryParse(tourId, out var id))
            return;

        _ = LoadAsync(id);
    }

    private async Task LoadAsync(int id)
    {
        _suppressSpeechTextAutoSave = true;
        try
        {
            PoiMobileDto? cachedPoi = null;

            try
            {
                var localPois = await _localDatabaseService.GetPoisAsync(UserProfileService.PreferredLanguage, cancellationToken: CancellationToken.None);
                cachedPoi = localPois.FirstOrDefault(x => x.Id == id);
            }
            catch
            {
            }

            try
            {
                var dto = await _tourRouteCatalogService.ResolvePoiAsync(id, UserProfileService.PreferredLanguage);
                if (dto is not null)
                {
                    if (cachedPoi is not null && !string.IsNullOrWhiteSpace(cachedPoi.SpeechText))
                    {
                        dto.SpeechText = cachedPoi.SpeechText;
                        dto.SpeechTextLanguageCode = cachedPoi.SpeechTextLanguageCode;
                        dto.SpeechTexts = cachedPoi.SpeechTexts.Select(x => new PoiSpeechTextDto(x.LanguageCode, x.Text)).ToList();
                    }

                    _currentPoiDto = dto;
                    Tour = MapPoi(dto);
                    SetLoadedSpeechTexts(dto.SpeechTexts, dto.SpeechTextLanguageCode, dto.SpeechText ?? dto.Description, dto.PrimaryLanguage);
                    IsBookmarked = await _bookmarkHistoryService.IsBookmarkedAsync(id, CancellationToken.None);
                    return;
                }
            }
            catch
            {
            }

            _currentPoiDto = null;
            if (cachedPoi is not null)
            {
                var cachedModel = new PoiModel
                {
                    Id = cachedPoi.Id,
                    Title = cachedPoi.Title,
                    Subtitle = cachedPoi.Subtitle,
                    ImageUrl = cachedPoi.ImageUrl,
                    Location = cachedPoi.Location,
                    Distance = string.Empty,
                    Duration = string.Empty,
                    Description = cachedPoi.Description,
                    Provider = null,
                    Credit = null,
                    SpeechText = cachedPoi.SpeechText
                };

                _currentPoiDto = new PoiDto
                {
                    Id = cachedPoi.Id,
                    Title = cachedPoi.Title,
                    Subtitle = cachedPoi.Subtitle,
                    ImageUrl = cachedPoi.ImageUrl,
                    Location = cachedPoi.Location,
                    Latitude = cachedPoi.Latitude,
                    Longitude = cachedPoi.Longitude,
                    GeofenceRadiusMeters = cachedPoi.GeofenceRadiusMeters,
                    Distance = string.Empty,
                    Duration = string.Empty,
                    Description = cachedPoi.Description,
                    Provider = null,
                    Credit = null,
                    Category = cachedPoi.Category,
                    PrimaryLanguage = cachedPoi.PrimaryLanguage,
                    SpeechText = cachedPoi.SpeechText,
                    SpeechTextLanguageCode = cachedPoi.SpeechTextLanguageCode,
                    Localizations = [],
                    AudioAssets = cachedPoi.AudioAssets.Select(audio => new PoiAudioDto(audio.LanguageCode, audio.AudioUrl, audio.Transcript, audio.IsGenerated)).ToList(),
                    SpeechTexts = cachedPoi.SpeechTexts.Select(x => new PoiSpeechTextDto(x.LanguageCode, x.Text)).ToList()
                };

                Tour = cachedModel;
                SetLoadedSpeechTexts(_currentPoiDto.SpeechTexts, _currentPoiDto.SpeechTextLanguageCode, _currentPoiDto.SpeechText ?? _currentPoiDto.Description, _currentPoiDto.PrimaryLanguage);
                IsBookmarked = await _bookmarkHistoryService.IsBookmarkedAsync(id, CancellationToken.None);
                return;
            }

            Tour = null;
            SpeechTextInput = string.Empty;
            IsBookmarked = false;
        }
        finally
        {
            _suppressSpeechTextAutoSave = false;
        }
    }

    private async Task SaveSpeechTextAsync(bool showConfirmation = true)
    {
        if (Tour is null || _currentPoiDto is null || IsSavingSpeechText)
        {
            return;
        }

        IsSavingSpeechText = true;
        try
        {
            var selectedLanguage = NormalizeLanguageCode(SelectedSpeechLanguageCode);
            var speechText = SpeechTextInput?.Trim();
            if (!string.IsNullOrWhiteSpace(selectedLanguage))
            {
                _speechTextsByLanguage[selectedLanguage] = speechText ?? string.Empty;
            }

            var speechTexts = _speechTextsByLanguage
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => new PoiSpeechTextDto(x.Key, x.Value.Trim()))
                .OrderBy(x => x.LanguageCode, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var request = new UpsertPoiRequestDto(
                _currentPoiDto.Title,
                _currentPoiDto.Subtitle,
                _currentPoiDto.ImageUrl,
                _currentPoiDto.Location,
                _currentPoiDto.Latitude,
                _currentPoiDto.Longitude,
                _currentPoiDto.GeofenceRadiusMeters,
                _currentPoiDto.Description,
                _currentPoiDto.Category,
                _currentPoiDto.PrimaryLanguage,
                _currentPoiDto.Duration,
                _currentPoiDto.Provider,
                _currentPoiDto.Credit,
                speechText,
                selectedLanguage,
                _currentPoiDto.Localizations,
                _currentPoiDto.AudioAssets,
                speechTexts);

            await _localDatabaseService.SavePoisAsync([
                new PoiMobileDto
                {
                    Id = _currentPoiDto.Id,
                    Title = _currentPoiDto.Title,
                    Subtitle = _currentPoiDto.Subtitle,
                    Description = _currentPoiDto.Description,
                    LanguageCode = _currentPoiDto.PrimaryLanguage,
                    PrimaryLanguage = _currentPoiDto.PrimaryLanguage,
                    ImageUrl = _currentPoiDto.ImageUrl,
                    Location = _currentPoiDto.Location,
                    Latitude = _currentPoiDto.Latitude,
                    Longitude = _currentPoiDto.Longitude,
                    GeofenceRadiusMeters = _currentPoiDto.GeofenceRadiusMeters ?? 100,
                    Category = _currentPoiDto.Category ?? string.Empty,
                    SpeechText = speechText,
                    SpeechTextLanguageCode = selectedLanguage,
                    AudioAssets = _currentPoiDto.AudioAssets.Select(audio => new PoiAudioMobileDto
                    {
                        LanguageCode = audio.LanguageCode,
                        AudioUrl = audio.AudioUrl,
                        Transcript = audio.Transcript,
                        IsGenerated = audio.IsGenerated
                    }).ToList(),
                    SpeechTexts = speechTexts.Select(x => new PoiSpeechTextMobileDto { LanguageCode = x.LanguageCode, Text = x.Text }).ToList()
                }
            ], CancellationToken.None);

            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                await _poiApiClient.UpdateAsync(_currentPoiDto.Id, request);
            }

            await _tourRouteCacheService.InvalidateAsync(_currentPoiDto.Id, null, CancellationToken.None);

            _suppressSpeechTextAutoSave = true;
            _currentPoiDto.SpeechText = speechText;
            _currentPoiDto.SpeechTextLanguageCode = selectedLanguage;
            _currentPoiDto.SpeechTexts = speechTexts;
            Tour.SpeechText = string.IsNullOrWhiteSpace(speechText) ? null : speechText;
            OnPropertyChanged(nameof(Tour));
            OnPropertyChanged(nameof(Description));
            SpeechTextInput = speechText ?? string.Empty;
            _suppressSpeechTextAutoSave = false;
            if (showConfirmation)
            {
                await Shell.Current.DisplayAlert("Saved", "Text to speech đã được lưu.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Không lưu được text TTS: {ex.Message}", "OK");
        }
        finally
        {
            IsSavingSpeechText = false;
        }
    }

    private void ScheduleSpeechTextAutoSave()
    {
        CancelSpeechTextAutoSave();
        _speechTextAutoSaveCts = new CancellationTokenSource();

        var token = _speechTextAutoSaveCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(800, token);
                await SaveSpeechTextAsync(showConfirmation: false);
            }
            catch (OperationCanceledException)
            {
            }
        }, token);
    }

    private void CancelSpeechTextAutoSave()
    {
        if (_speechTextAutoSaveCts is null)
        {
            return;
        }

        _speechTextAutoSaveCts.Cancel();
        _speechTextAutoSaveCts.Dispose();
        _speechTextAutoSaveCts = null;
    }

    private async Task SelectSpeechLanguageAsync(SpeechLanguageOption? option)
    {
        if (option is null)
        {
            return;
        }

        IsSpeechLanguageMenuOpen = false;
        await PersistSpeechTextAsync();

        SelectedSpeechLanguageCode = NormalizeLanguageCode(option.LanguageCode);
        UpdateSelectedLanguageFlags();
        ApplySpeechTextForSelectedLanguage();
    }

    private void SetLoadedSpeechTexts(IReadOnlyList<PoiSpeechTextDto> speechTexts, string? selectedLanguageHint, string? fallbackText, string? primaryLanguage)
    {
        _speechTextsByLanguage.Clear();

        foreach (var speechText in speechTexts)
        {
            var languageCode = NormalizeLanguageCode(speechText.LanguageCode);
            if (string.IsNullOrWhiteSpace(languageCode) || string.IsNullOrWhiteSpace(speechText.Text))
            {
                continue;
            }

            _speechTextsByLanguage[languageCode] = speechText.Text.Trim();
        }

        if (_speechTextsByLanguage.Count == 0 && !string.IsNullOrWhiteSpace(fallbackText))
        {
            var defaultLanguage = NormalizeLanguageCode(selectedLanguageHint ?? primaryLanguage ?? "vi");
            _speechTextsByLanguage[defaultLanguage] = fallbackText.Trim();
        }

        if (!_speechTextsByLanguage.ContainsKey("vi") && !string.IsNullOrWhiteSpace(fallbackText))
        {
            _speechTextsByLanguage["vi"] = fallbackText.Trim();
        }

        var persistedLanguage = NormalizeLanguageCode(selectedLanguageHint ?? primaryLanguage);
        SelectedSpeechLanguageCode = !string.IsNullOrWhiteSpace(persistedLanguage) && _speechTextsByLanguage.ContainsKey(persistedLanguage)
            ? persistedLanguage
            : _speechTextsByLanguage.ContainsKey("vi")
                ? "vi"
                : NormalizeLanguageCode(_speechTextsByLanguage.Keys.FirstOrDefault() ?? "vi");

        UpdateSelectedLanguageFlags();
        ApplySpeechTextForSelectedLanguage();
        _ = RefreshSpeechLanguagesAsync();
    }

    private void ApplySpeechTextForSelectedLanguage()
    {
        var text = GetSpeechTextForLanguage(SelectedSpeechLanguageCode);

        _suppressSpeechTextAutoSave = true;
        SpeechTextInput = text;
        _suppressSpeechTextAutoSave = false;

        if (Tour is not null)
        {
            Tour.SpeechText = string.IsNullOrWhiteSpace(text) ? null : text;
            OnPropertyChanged(nameof(Tour));
            OnPropertyChanged(nameof(Description));
        }
    }

    private string GetSpeechTextForLanguage(string languageCode)
    {
        var normalized = NormalizeLanguageCode(languageCode);
        return _speechTextsByLanguage.TryGetValue(normalized, out var text)
            ? text
            : string.Empty;
    }

    private void UpdateSelectedLanguageFlags()
    {
        foreach (var language in _speechLanguages)
        {
            language.IsSelected = string.Equals(language.LanguageCode, SelectedSpeechLanguageCode, StringComparison.OrdinalIgnoreCase);
        }
    }

    private async Task RefreshSpeechLanguagesAsync()
    {
        if (_speechLanguages.Count > 0)
        {
            UpdateSelectedLanguageFlags();
            return;
        }

        try
        {
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var items = new List<SpeechLanguageOption>();

            foreach (var code in _speechTextsByLanguage.Keys.Concat([SelectedSpeechLanguageCode, UserProfileService.PreferredLanguage, "vi"]))
            {
                AddLanguageCode(code, items, codes);
            }

            foreach (var locale in locales)
            {
                AddLanguageCode(locale.Language, items, codes);
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _speechLanguages.Clear();
                foreach (var item in items.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase))
                {
                    item.IsSelected = string.Equals(item.LanguageCode, SelectedSpeechLanguageCode, StringComparison.OrdinalIgnoreCase);
                    _speechLanguages.Add(item);
                }
            });
        }
        catch
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _speechLanguages.Clear();
                foreach (var code in _speechTextsByLanguage.Keys.Concat([SelectedSpeechLanguageCode, "vi"]).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    _speechLanguages.Add(new SpeechLanguageOption
                    {
                        LanguageCode = NormalizeLanguageCode(code),
                        DisplayName = GetLanguageDisplayText(code),
                        IsSelected = string.Equals(NormalizeLanguageCode(code), SelectedSpeechLanguageCode, StringComparison.OrdinalIgnoreCase)
                    });
                }
            });
        }
    }

    private static void AddLanguageCode(string? languageCode, ICollection<SpeechLanguageOption> items, ISet<string> codes)
    {
        var normalized = NormalizeLanguageCode(languageCode);
        if (string.IsNullOrWhiteSpace(normalized) || !codes.Add(normalized))
        {
            return;
        }

        items.Add(new SpeechLanguageOption
        {
            LanguageCode = normalized,
            DisplayName = GetLanguageDisplayText(normalized)
        });
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode)
            ? string.Empty
            : languageCode.Trim().ToLowerInvariant();
    }

    private static string GetLanguageDisplayText(string? languageCode)
    {
        var normalized = NormalizeLanguageCode(languageCode);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "--";
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(normalized);
            return string.IsNullOrWhiteSpace(culture.NativeName)
                ? normalized.ToUpperInvariant()
                : $"{culture.NativeName} ({normalized.ToUpperInvariant()})";
        }
        catch
        {
            return normalized.ToUpperInvariant();
        }
    }

    private static PoiModel MapPoi(PoiDto dto)
    {
        return new PoiModel
        {
            Id = dto.Id,
            Title = dto.Title,
            Subtitle = dto.Subtitle,
            ImageUrl = dto.ImageUrl,
            Location = dto.Location,
            Distance = dto.Distance,
            Duration = dto.Duration,
            Description = dto.Description,
            Provider = dto.Provider,
            Credit = dto.Credit,
            SpeechText = dto.SpeechText
        };
    }

    private static bool IsStaleCentralParkPoi(PoiDto dto)
    {
        return ContainsCentralParkText(dto.Title)
               || ContainsCentralParkText(dto.Description)
               || ContainsCentralParkText(dto.Location);
    }

    private static bool ContainsCentralParkText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("Central Park", StringComparison.OrdinalIgnoreCase)
               || value.Contains("New York", StringComparison.OrdinalIgnoreCase)
               || value.Contains("USA", StringComparison.OrdinalIgnoreCase);
    }

    private static PoiDto MergePoiDto(PoiDto source, PoiModel localPoi)
    {
        return new PoiDto
        {
            Id = source.Id,
            Title = localPoi.Title,
            Subtitle = localPoi.Subtitle,
            ImageUrl = localPoi.ImageUrl,
            Location = localPoi.Location,
            Latitude = source.Latitude,
            Longitude = source.Longitude,
            GeofenceRadiusMeters = source.GeofenceRadiusMeters,
            Distance = source.Distance,
            Duration = localPoi.Duration,
            Description = localPoi.Description,
            Provider = localPoi.Provider,
            Credit = localPoi.Credit,
            Category = source.Category,
            PrimaryLanguage = source.PrimaryLanguage,
            SpeechText = localPoi.SpeechText ?? source.SpeechText ?? localPoi.Description,
            Localizations = source.Localizations,
            AudioAssets = source.AudioAssets,
            SpeechTextLanguageCode = localPoi.SpeechText is not null ? source.SpeechTextLanguageCode : source.SpeechTextLanguageCode,
            SpeechTexts = source.SpeechTexts
        };
    }

    private static PoiDto BuildPoiDtoFromLocalPoi(PoiModel localPoi)
    {
        return new PoiDto
        {
            Id = localPoi.Id,
            Title = localPoi.Title,
            Subtitle = localPoi.Subtitle,
            ImageUrl = localPoi.ImageUrl,
            Location = localPoi.Location,
            Latitude = 0,
            Longitude = 0,
            GeofenceRadiusMeters = 100,
            Distance = string.Empty,
            Duration = localPoi.Duration,
            Description = localPoi.Description,
            Provider = localPoi.Provider,
            Credit = localPoi.Credit,
            Category = null,
            PrimaryLanguage = UserProfileService.PreferredLanguage,
            SpeechText = localPoi.SpeechText ?? localPoi.Description,
            Localizations = [],
            AudioAssets = [],
            SpeechTextLanguageCode = "vi",
            SpeechTexts = [new PoiSpeechTextDto("vi", localPoi.SpeechText ?? localPoi.Description ?? string.Empty)]
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class SpeechLanguageOption : INotifyPropertyChanged
{
    private bool _isSelected;

    public string LanguageCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
