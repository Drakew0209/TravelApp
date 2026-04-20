using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using TravelApp.Models;
using TravelApp.Models.Contracts;
using TravelApp.Models.Runtime;
using TravelApp.Resources.Strings;
using TravelApp.Services;
using TravelApp.Services.Api;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public class TourDetailViewModel : INotifyPropertyChanged
{
    private readonly Dictionary<string, string> _speechTextsByLanguage = new(StringComparer.OrdinalIgnoreCase);
    private readonly ObservableCollection<SpeechLanguageOption> _speechLanguages = [];
    private PoiModel? _tour;
    private PoiDto? _currentPoiDto;
    private string _speechTextInput = string.Empty;
    private string _selectedSpeechLanguageCode = string.Empty;
    private bool _isSavingSpeechText;
    private bool _suppressSpeechTextAutoSave;
    private bool _hasPendingSpeechTextChanges;
    private bool _isSpeechLanguageMenuOpen;
    private bool _isBookmarked;
    private bool _canEditSpeechText;
    private int? _currentTourId;
    private string _lastLoadedPreferredLanguage = string.Empty;
    private CancellationTokenSource? _speechTextAutoSaveCts;
    private readonly HashSet<int> _tourDownloadPoiIds = [];
    private readonly HashSet<int> _tourDownloadCompletedPoiIds = [];
    private int _tourDownloadTotalCount;
    private int _tourDownloadSeenEventCount;
    private bool _isTourDownloading;
    private double _tourDownloadProgress;
    private string _tourDownloadStatusText = string.Empty;
    private readonly IPoiApiClient _poiApiClient;
    private readonly ILocalDatabaseService _localDatabaseService;
    private readonly IAudioLibraryService _audioLibraryService;
    private readonly IBookmarkHistoryService _bookmarkHistoryService;
    private readonly IAnalyticsTrackingService _analyticsTrackingService;
    private readonly IPoiQrCodeService _poiQrCodeService;
    private readonly IEndpointSettingsService _endpointSettingsService;
    private readonly ITourRouteCatalogService _tourRouteCatalogService;
    private readonly TravelApp.Services.Runtime.TourRouteCacheService _tourRouteCacheService;
    private readonly ApiClientOptions _apiOptions;
    private ImageSource? _qrCodeImageSource;
    private string _qrShareLink = string.Empty;
    private string _qrShareWarningText = string.Empty;

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
            OnPropertyChanged(nameof(CanDownloadTour));
        }
    }

    public bool CanEditSpeechText
    {
        get => _canEditSpeechText;
        private set
        {
            if (_canEditSpeechText == value)
            {
                return;
            }

            _canEditSpeechText = value;
            OnPropertyChanged();
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
            if (!_suppressSpeechTextAutoSave)
            {
                _hasPendingSpeechTextChanges = true;
            }
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

    public string ProviderName => Tour?.Provider ?? string.Empty;
    public string Description => Tour?.SpeechText ?? Tour?.Description ?? string.Empty;
    public string Credit => Tour?.Credit ?? string.Empty;
    public string SelectedSpeechLanguageDisplayText => GetLanguageDisplayText(SelectedSpeechLanguageCode);
    public ObservableCollection<SpeechLanguageOption> SpeechLanguages => _speechLanguages;
    public string BookmarkedLabel => AppStrings.BookmarkedBadge;
    public string DownloadTourText => AppStrings.DownloadTour;
    public string DownloadingAllTourText => AppStrings.DownloadingAllTour;
    public string QrShareTitleText => AppStrings.QrShareTitle;
    public string QrShareSubtitleText => AppStrings.QrShareSubtitle;
    public string WebAdminPayloadText => AppStrings.WebAdminPayload;
    public string ShareLinkText => AppStrings.ShareLink;
    public string CopyLinkText => AppStrings.CopyLink;
    public string DescriptionSectionText => AppStrings.Description;
    public string ProvidedByText => AppStrings.ProvidedBy;
    public string SpeechLanguageText => AppStrings.SpeechLanguage;
    public string SaveTtsText => AppStrings.SaveTtsText;
    public string SpeechTextPlaceholder => AppStrings.EnterSpeechTextPlaceholder;
    public string OwnerSpeechTextNotice => AppStrings.SpeechTextPermissionNotice;
    public string ViewTourText => AppStrings.ViewTour;
    public string ChooseSpeechLanguageText => AppStrings.ChooseTtsLanguage;
    public string BookmarkSavedMessageText => AppStrings.BookmarkSavedMessage;
    public string BookmarkRemovedMessageText => AppStrings.BookmarkRemovedMessage;
    public string CouldNotUpdateBookmarkText => AppStrings.CouldNotUpdateBookmark;
    public string CouldNotDownloadTourText => AppStrings.CouldNotDownloadTour;
    public ImageSource? QrCodeImageSource
    {
        get => _qrCodeImageSource;
        private set
        {
            if (ReferenceEquals(_qrCodeImageSource, value))
            {
                return;
            }

            _qrCodeImageSource = value;
            OnPropertyChanged();
        }
    }

    public string QrShareLink
    {
        get => _qrShareLink;
        private set
        {
            if (_qrShareLink == value)
            {
                return;
            }

            _qrShareLink = value;
            OnPropertyChanged();
        }
    }

    public string QrShareWarningText
    {
        get => _qrShareWarningText;
        private set
        {
            if (_qrShareWarningText == value)
            {
                return;
            }

            _qrShareWarningText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasQrShareWarning));
        }
    }

    public bool HasQrCode => !string.IsNullOrWhiteSpace(QrShareLink) && QrCodeImageSource is not null;
    public bool HasQrShareWarning => !string.IsNullOrWhiteSpace(QrShareWarningText);
    public bool HasQrShareSection => HasQrCode || HasQrShareWarning;

    public bool IsTourDownloading
    {
        get => _isTourDownloading;
        private set
        {
            if (_isTourDownloading == value)
            {
                return;
            }

            _isTourDownloading = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanDownloadTour));
        }
    }

    public bool CanDownloadTour => Tour is not null && !IsTourDownloading;

    public double TourDownloadProgress
    {
        get => _tourDownloadProgress;
        private set
        {
            var clamped = Math.Clamp(value, 0d, 1d);
            if (Math.Abs(_tourDownloadProgress - clamped) < 0.0001)
            {
                return;
            }

            _tourDownloadProgress = clamped;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TourDownloadProgressText));
        }
    }

    public string TourDownloadProgressText => _tourDownloadTotalCount <= 0
        ? string.Empty
        : string.Format(CultureInfo.CurrentUICulture, AppStrings.AudioQueueText, _tourDownloadCompletedPoiIds.Count, _tourDownloadTotalCount);

    public string TourDownloadSeenText => _tourDownloadTotalCount <= 0
        ? string.Empty
        : $"{_tourDownloadSeenEventCount}/{_tourDownloadTotalCount}";

    public string TourDownloadStatusText
    {
        get => _tourDownloadStatusText;
        private set
        {
            if (_tourDownloadStatusText == value)
            {
                return;
            }

            _tourDownloadStatusText = value;
            OnPropertyChanged();
        }
    }

    public ICommand BackCommand { get; }
    public ICommand ViewTourCommand { get; }
    public ICommand SaveSpeechTextCommand { get; }
    public ICommand DownloadTourCommand { get; }
    public ICommand ToggleBookmarkCommand { get; }
    public ICommand ToggleSpeechLanguageMenuCommand { get; }
    public ICommand CloseSpeechLanguageMenuCommand { get; }
    public ICommand SelectSpeechLanguageCommand { get; }
    public ICommand ShareLinkCommand { get; }
    public ICommand CopyLinkCommand { get; }

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
            await Shell.Current.DisplayAlert(AppStrings.Bookmarks, IsBookmarked ? AppStrings.BookmarkSavedMessage : AppStrings.BookmarkRemovedMessage, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(AppStrings.ValidationTitle, string.Format(CultureInfo.CurrentUICulture, AppStrings.CouldNotUpdateBookmark, ex.Message), "OK");
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

    public TourDetailViewModel(ITourRouteCatalogService tourRouteCatalogService, IPoiApiClient poiApiClient, ILocalDatabaseService localDatabaseService, IAudioLibraryService audioLibraryService, IBookmarkHistoryService bookmarkHistoryService, IAnalyticsTrackingService analyticsTrackingService, IPoiQrCodeService poiQrCodeService, IEndpointSettingsService endpointSettingsService, TravelApp.Services.Runtime.TourRouteCacheService tourRouteCacheService, ApiClientOptions apiOptions)
    {
        _tourRouteCatalogService = tourRouteCatalogService;
        _poiApiClient = poiApiClient;
        _localDatabaseService = localDatabaseService;
        _audioLibraryService = audioLibraryService;
        _bookmarkHistoryService = bookmarkHistoryService;
        _analyticsTrackingService = analyticsTrackingService;
        _poiQrCodeService = poiQrCodeService;
        _endpointSettingsService = endpointSettingsService;
        _tourRouteCacheService = tourRouteCacheService;
        _apiOptions = apiOptions;
        _lastLoadedPreferredLanguage = NormalizeLanguageCode(UserProfileService.PreferredLanguage);
        _audioLibraryService.DownloadProgressChanged += OnDownloadProgressChanged;
        _endpointSettingsService.SettingsChanged += OnEndpointSettingsChanged;
        UserProfileService.ProfileChanged += OnUserProfileChanged;
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
        ShareLinkCommand = new Command(async () => await ShareLinkAsync());
        CopyLinkCommand = new Command(async () => await CopyLinkAsync());

        UpdateSpeechTextPermission();
    }

    private async Task DownloadTourAsync()
    {
        if (Tour is null || IsTourDownloading)
        {
            return;
        }

        try
        {
            var requestedLanguage = string.IsNullOrWhiteSpace(SelectedSpeechLanguageCode)
                ? UserProfileService.PreferredLanguage
                : SelectedSpeechLanguageCode;

            var route = await _tourRouteCatalogService.GetRouteAsync(Tour.Id, requestedLanguage, CancellationToken.None);
            var poiIds = route?.Waypoints.Select(x => x.Poi.Id).Distinct().ToList() ?? [Tour.Id];

            BeginTourDownloadSession(poiIds);
            TourDownloadStatusText = string.Format(CultureInfo.CurrentUICulture, AppStrings.ToQueueFormat, TourDownloadProgressText);

            var queued = await _audioLibraryService.DownloadManyAsync(poiIds, requestedLanguage, CancellationToken.None);
            TourDownloadStatusText = queued > 0
                ? string.Format(CultureInfo.CurrentUICulture, AppStrings.QueuedFormat, queued, _tourDownloadTotalCount)
                : AppStrings.AudioAlreadyAvailable;

            if (_tourDownloadCompletedPoiIds.Count >= _tourDownloadTotalCount && _tourDownloadTotalCount > 0)
            {
                CompleteTourDownloadSession(AppStrings.TourDownloadCompleted);
            }
        }
        catch (Exception ex)
        {
            EndTourDownloadSession();
            await Shell.Current.DisplayAlert(AppStrings.ValidationTitle, string.Format(CultureInfo.CurrentUICulture, AppStrings.CouldNotDownloadTour, ex.Message), "OK");
        }
    }

    public Task PersistSpeechTextAsync()
    {
        if (!_hasPendingSpeechTextChanges)
        {
            return Task.CompletedTask;
        }

        CancelSpeechTextAutoSave();
        return SaveSpeechTextAsync(showConfirmation: false);
    }

    public async Task StopAsync()
    {
        if (!_hasPendingSpeechTextChanges)
        {
            return;
        }

        CancelSpeechTextAutoSave();
        await SaveSpeechTextAsync(showConfirmation: false);
    }

    private void BeginTourDownloadSession(IEnumerable<int> poiIds)
    {
        _tourDownloadPoiIds.Clear();
        _tourDownloadCompletedPoiIds.Clear();
        foreach (var poiId in poiIds.Distinct())
        {
            _tourDownloadPoiIds.Add(poiId);
        }

        _tourDownloadTotalCount = _tourDownloadPoiIds.Count;
        _tourDownloadSeenEventCount = 0;
        IsTourDownloading = _tourDownloadTotalCount > 0;
        TourDownloadProgress = 0;
        TourDownloadStatusText = _tourDownloadTotalCount > 0
            ? string.Format(CultureInfo.CurrentUICulture, AppStrings.AudioQueueText, 0, _tourDownloadTotalCount)
            : string.Empty;
        OnPropertyChanged(nameof(TourDownloadProgressText));
        OnPropertyChanged(nameof(TourDownloadSeenText));
    }

    private void EndTourDownloadSession()
    {
        _tourDownloadPoiIds.Clear();
        _tourDownloadCompletedPoiIds.Clear();
        _tourDownloadTotalCount = 0;
        _tourDownloadSeenEventCount = 0;
        IsTourDownloading = false;
        TourDownloadProgress = 0;
        TourDownloadStatusText = string.Empty;
        OnPropertyChanged(nameof(TourDownloadProgressText));
        OnPropertyChanged(nameof(TourDownloadSeenText));
    }

    private void CompleteTourDownloadSession(string statusText)
    {
        TourDownloadProgress = 1;
        TourDownloadStatusText = statusText;
        IsTourDownloading = false;
        OnPropertyChanged(nameof(TourDownloadProgressText));
        OnPropertyChanged(nameof(TourDownloadSeenText));
    }

    private void OnDownloadProgressChanged(object? sender, AudioDownloadProgressChangedEventArgs e)
    {
        if (_tourDownloadTotalCount <= 0 || e.PoiId == 0 || !_tourDownloadPoiIds.Contains(e.PoiId))
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!string.IsNullOrWhiteSpace(e.Message))
            {
                TourDownloadStatusText = e.Message;
            }

            if (e.IsCompleted && _tourDownloadCompletedPoiIds.Add(e.PoiId))
            {
                _tourDownloadSeenEventCount++;
                TourDownloadProgress = (double)_tourDownloadCompletedPoiIds.Count / _tourDownloadTotalCount;
                OnPropertyChanged(nameof(TourDownloadProgressText));
                OnPropertyChanged(nameof(TourDownloadSeenText));
            }

            if (_tourDownloadCompletedPoiIds.Count >= _tourDownloadTotalCount)
            {
                CompleteTourDownloadSession(AppStrings.TourDownloadCompleted);
            }
        });
    }

    private async void OnUserProfileChanged(object? sender, EventArgs e)
    {
        var currentPreferredLanguage = NormalizeLanguageCode(UserProfileService.PreferredLanguage);
        if (_currentTourId.HasValue && !string.Equals(_lastLoadedPreferredLanguage, currentPreferredLanguage, StringComparison.OrdinalIgnoreCase))
        {
            _lastLoadedPreferredLanguage = currentPreferredLanguage;
            await RefreshAsync();
            return;
        }

        OnPropertyChanged(nameof(ProviderName));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(Credit));
        OnPropertyChanged(nameof(SelectedSpeechLanguageDisplayText));
        OnPropertyChanged(nameof(BookmarkedLabel));
        OnPropertyChanged(nameof(DownloadTourText));
        OnPropertyChanged(nameof(DownloadingAllTourText));
        OnPropertyChanged(nameof(QrShareTitleText));
        OnPropertyChanged(nameof(QrShareSubtitleText));
        OnPropertyChanged(nameof(WebAdminPayloadText));
        OnPropertyChanged(nameof(ShareLinkText));
        OnPropertyChanged(nameof(CopyLinkText));
        OnPropertyChanged(nameof(DescriptionSectionText));
        OnPropertyChanged(nameof(ProvidedByText));
        OnPropertyChanged(nameof(SpeechLanguageText));
        OnPropertyChanged(nameof(SaveTtsText));
        OnPropertyChanged(nameof(SpeechTextPlaceholder));
        OnPropertyChanged(nameof(OwnerSpeechTextNotice));
        OnPropertyChanged(nameof(ViewTourText));
        OnPropertyChanged(nameof(ChooseSpeechLanguageText));
        OnPropertyChanged(nameof(TourDownloadProgressText));
        OnPropertyChanged(nameof(TourDownloadSeenText));
        OnPropertyChanged(nameof(TourDownloadStatusText));
    }

    public void Load(string? tourId, string? languageCode = null)
    {
        if (!int.TryParse(tourId, out var id))
            return;

        _currentTourId = id;
        _lastLoadedPreferredLanguage = NormalizeLanguageCode(languageCode ?? UserProfileService.PreferredLanguage);
        _ = LoadAsync(id, _lastLoadedPreferredLanguage);
    }

    public Task RefreshAsync()
    {
        if (!_currentTourId.HasValue)
        {
            return Task.CompletedTask;
        }

        var selectedLanguage = SelectedSpeechLanguageCode;
        return RefreshAndRestoreSelectionAsync(_currentTourId.Value, selectedLanguage);
    }

    private async Task RefreshAndRestoreSelectionAsync(int tourId, string? selectedLanguage)
    {
        await LoadAsync(tourId, UserProfileService.PreferredLanguage);

        var normalized = NormalizeLanguageCode(selectedLanguage);
        if (!string.IsNullOrWhiteSpace(normalized) && _speechTextsByLanguage.ContainsKey(normalized))
        {
            SelectedSpeechLanguageCode = normalized;
            UpdateSelectedLanguageFlags();
            ApplySpeechTextForSelectedLanguage();
        }
    }

    private async Task LoadAsync(int id, string? languageCode = null)
    {
        var requestedLanguage = NormalizeLanguageCode(languageCode ?? UserProfileService.PreferredLanguage);
        _suppressSpeechTextAutoSave = true;
        try
        {
            try
            {
                var dto = await _tourRouteCatalogService.ResolvePoiAsync(id, requestedLanguage);
                if (dto is not null)
                {
                    _currentPoiDto = dto;
                    Tour = MapPoi(dto);
                    SetLoadedSpeechTexts(dto.SpeechTexts, dto.SpeechTextLanguageCode, dto.SpeechText ?? dto.Description, dto.PrimaryLanguage, dto.Localizations);
                    RefreshQrShareData();
                    _ = _analyticsTrackingService.TrackPoiViewedAsync(dto.Id, dto.PrimaryLanguage, CancellationToken.None);
                    _hasPendingSpeechTextChanges = false;
                    IsBookmarked = await _bookmarkHistoryService.IsBookmarkedAsync(id, CancellationToken.None);
                    _lastLoadedPreferredLanguage = requestedLanguage;
                    return;
                }
            }
            catch
            {
            }

            PoiMobileDto? cachedPoi = null;
            try
            {
                var localPois = await _localDatabaseService.GetPoisAsync(requestedLanguage, cancellationToken: CancellationToken.None);
                cachedPoi = localPois.FirstOrDefault(x => x.Id == id);
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
                SetLoadedSpeechTexts(_currentPoiDto.SpeechTexts, _currentPoiDto.SpeechTextLanguageCode, _currentPoiDto.SpeechText ?? _currentPoiDto.Description, _currentPoiDto.PrimaryLanguage, []);
                RefreshQrShareData();
                _ = _analyticsTrackingService.TrackPoiViewedAsync(cachedPoi.Id, cachedPoi.PrimaryLanguage, CancellationToken.None);
                _hasPendingSpeechTextChanges = false;
                IsBookmarked = await _bookmarkHistoryService.IsBookmarkedAsync(id, CancellationToken.None);
                _lastLoadedPreferredLanguage = requestedLanguage;
                return;
            }

            Tour = null;
            SpeechTextInput = string.Empty;
            _hasPendingSpeechTextChanges = false;
            IsBookmarked = false;
            RefreshQrShareData();
        }
        finally
        {
            _suppressSpeechTextAutoSave = false;
        }
    }

    private async Task SaveSpeechTextAsync(bool showConfirmation = true)
    {
        if (!CanEditSpeechText)
        {
            return;
        }

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
                speechText,
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
                    Description = speechText,
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
            _currentPoiDto.Description = speechText;
            _currentPoiDto.SpeechText = speechText;
            _currentPoiDto.SpeechTextLanguageCode = selectedLanguage;
            _currentPoiDto.SpeechTexts = speechTexts;
            Tour.SpeechText = string.IsNullOrWhiteSpace(speechText) ? null : speechText;
            Tour.Description = string.IsNullOrWhiteSpace(speechText) ? string.Empty : speechText;
            OnPropertyChanged(nameof(Tour));
            OnPropertyChanged(nameof(Description));
            SpeechTextInput = speechText ?? string.Empty;
            _hasPendingSpeechTextChanges = false;
            _suppressSpeechTextAutoSave = false;
            if (showConfirmation)
            {
                await Shell.Current.DisplayAlert(AppStrings.Save, AppStrings.TtsSavedMessage, "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(AppStrings.ValidationTitle, string.Format(CultureInfo.CurrentUICulture, AppStrings.CouldNotSaveTtsText, ex.Message), "OK");
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
        if (_hasPendingSpeechTextChanges)
        {
            await PersistSpeechTextAsync();
        }

        SelectedSpeechLanguageCode = NormalizeLanguageCode(option.LanguageCode);
        UserProfileService.PreferredLanguage = SelectedSpeechLanguageCode;
        UpdateSelectedLanguageFlags();
        ApplySpeechTextForSelectedLanguage();
    }

    private void SetLoadedSpeechTexts(IReadOnlyList<PoiSpeechTextDto> speechTexts, string? selectedLanguageHint, string? fallbackText, string? primaryLanguage, IReadOnlyList<PoiLocalizationDto>? localizations)
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

        foreach (var localization in localizations ?? [])
        {
            var languageCode = NormalizeLanguageCode(localization.LanguageCode);
            if (string.IsNullOrWhiteSpace(languageCode) || _speechTextsByLanguage.ContainsKey(languageCode))
            {
                continue;
            }

            var generatedText = BuildGeneratedSpeechText(localization.Title, localization.Subtitle, localization.Description);
            if (!string.IsNullOrWhiteSpace(generatedText))
            {
                _speechTextsByLanguage[languageCode] = generatedText;
            }
        }

        if (_speechTextsByLanguage.Count == 0 && !string.IsNullOrWhiteSpace(fallbackText))
        {
            var defaultLanguage = NormalizeLanguageCode(UserProfileService.PreferredLanguage ?? selectedLanguageHint ?? primaryLanguage);
            _speechTextsByLanguage[defaultLanguage] = fallbackText.Trim();
        }

        var preferredLanguage = NormalizeLanguageCode(UserProfileService.PreferredLanguage);
        var persistedLanguage = NormalizeLanguageCode(selectedLanguageHint ?? primaryLanguage);

        SelectedSpeechLanguageCode = !string.IsNullOrWhiteSpace(preferredLanguage) && _speechTextsByLanguage.ContainsKey(preferredLanguage)
            ? preferredLanguage
            : !string.IsNullOrWhiteSpace(persistedLanguage) && _speechTextsByLanguage.ContainsKey(persistedLanguage)
                ? persistedLanguage
                : NormalizeLanguageCode(_speechTextsByLanguage.Keys.FirstOrDefault());

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

        RefreshQrShareData();
    }

    private static string BuildGeneratedSpeechText(string title, string? subtitle, string? description)
    {
        var parts = new[] { title, subtitle ?? string.Empty, description ?? string.Empty }
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return parts.Length == 0 ? string.Empty : string.Join(". ", parts);
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
            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var items = new List<SpeechLanguageOption>();

            foreach (var code in UserProfileService.SupportedLanguages)
            {
                AddLanguageCode(code, items, codes);
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
                foreach (var code in UserProfileService.SupportedLanguages)
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
        return LanguageCodeNormalizer.NormalizeToLocaleCode(languageCode);
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

    private PoiDto MergePoiDto(PoiDto source, PoiModel localPoi)
    {
        return new PoiDto
        {
            Id = source.Id,
            Title = localPoi.Title,
            Subtitle = localPoi.Subtitle,
            ImageUrl = NormalizeImageUrl(localPoi.ImageUrl),
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

    private PoiDto BuildPoiDtoFromLocalPoi(PoiModel localPoi)
    {
        return new PoiDto
        {
            Id = localPoi.Id,
            Title = localPoi.Title,
            Subtitle = localPoi.Subtitle,
            ImageUrl = NormalizeImageUrl(localPoi.ImageUrl),
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

    private void OnEndpointSettingsChanged(object? sender, EventArgs e)
    {
        if (_currentPoiDto is null)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(RefreshQrShareData);
    }

    private string NormalizeImageUrl(string? imageUrl)
    {
        var normalized = ResourceUrlHelper.Normalize(imageUrl, _apiOptions.BaseUrl);
        return string.IsNullOrWhiteSpace(normalized) ? "https://placehold.co/1200x800/png?text=Tour+Preview" : normalized;
    }

    private void RefreshQrShareData()
    {
        if (_currentPoiDto is null || _currentPoiDto.Id <= 0)
        {
            QrShareLink = string.Empty;
            QrCodeImageSource = null;
            QrShareWarningText = string.Empty;
            OnPropertyChanged(nameof(HasQrCode));
            return;
        }

        try
        {
            var qrLanguage = GetQrLanguageCode();
            var link = _poiQrCodeService.BuildPoiShareLink(_currentPoiDto.Id, qrLanguage);
            var qrBytes = _poiQrCodeService.GeneratePoiQrCodePng(link);

            QrShareLink = link;
            QrCodeImageSource = ImageSource.FromStream(() => new MemoryStream(qrBytes));
            QrShareWarningText = string.Empty;
            OnPropertyChanged(nameof(HasQrCode));
            OnPropertyChanged(nameof(HasQrShareSection));
        }
        catch (Exception ex)
        {
            QrShareLink = string.Empty;
            QrCodeImageSource = null;
            QrShareWarningText = ex.Message;
            OnPropertyChanged(nameof(HasQrCode));
            OnPropertyChanged(nameof(HasQrShareSection));
        }
    }

    private string GetQrLanguageCode()
    {
        var selected = NormalizeLanguageCode(SelectedSpeechLanguageCode);
        if (!string.IsNullOrWhiteSpace(selected))
        {
            return selected;
        }

        if (!string.IsNullOrWhiteSpace(_currentPoiDto?.SpeechTextLanguageCode))
        {
            return NormalizeLanguageCode(_currentPoiDto.SpeechTextLanguageCode);
        }

        if (!string.IsNullOrWhiteSpace(_currentPoiDto?.PrimaryLanguage))
        {
            return NormalizeLanguageCode(_currentPoiDto.PrimaryLanguage);
        }

        return NormalizeLanguageCode(UserProfileService.PreferredLanguage);
    }

    private async Task ShareLinkAsync()
    {
        if (string.IsNullOrWhiteSpace(QrShareLink))
        {
            return;
        }

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = Tour?.Title ?? AppStrings.AppName,
            Uri = QrShareLink,
            Text = QrShareLink
        });
    }

    private async Task CopyLinkAsync()
    {
        if (string.IsNullOrWhiteSpace(QrShareLink))
        {
            return;
        }

        await Clipboard.Default.SetTextAsync(QrShareLink);
        if (Shell.Current is not null)
        {
            await Shell.Current.DisplayAlert(AppStrings.CopyLink, QrShareLink, "OK");
        }
    }

    private void UpdateSpeechTextPermission()
    {
        CanEditSpeechText = UserProfileService.CanEditSpeechText;
    }

    public void Dispose()
    {
        UserProfileService.ProfileChanged -= OnUserProfileChanged;
        _audioLibraryService.DownloadProgressChanged -= OnDownloadProgressChanged;
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
