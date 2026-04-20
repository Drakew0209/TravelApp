using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TravelApp.Models;
using TravelApp.Models.Runtime;
using TravelApp.Resources.Strings;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public sealed class BookmarksHistoryViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IBookmarkHistoryService _bookmarkHistoryService;
    private string _activeTab = "Bookmarks";
    private string _statusText = AppStrings.Loading;
    private bool _isLoading;

    public ObservableCollection<PoiModel> Bookmarks { get; } = [];
    public ObservableCollection<HistoryPoiItem> History { get; } = [];

    public bool IsBookmarksTabActive => string.Equals(_activeTab, "Bookmarks", StringComparison.Ordinal);
    public bool IsHistoryTabActive => string.Equals(_activeTab, "History", StringComparison.Ordinal);

    public bool ShowBookmarks => IsBookmarksTabActive;
    public bool ShowHistory => IsHistoryTabActive;
    public string CurrentLanguageText => $"{AppStrings.LanguagePrefix} {UserProfileService.GetLanguageDisplayText(UserProfileService.PreferredLanguage)}";

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value)
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value)
            {
                return;
            }

            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string PageTitle => AppStrings.BookmarksHistoryTitle;
    public string BookmarksTabText => $"{AppStrings.BookmarksTab} ({Bookmarks.Count})";
    public string HistoryTabText => $"{AppStrings.HistoryTab} ({History.Count})";
    public string NoBookmarkedToursYetText => AppStrings.NoBookmarkedToursYet;
    public string NoHistoryYetText => AppStrings.NoHistoryYet;
    public string OpenText => AppStrings.Open;
    public string UnsaveText => AppStrings.Unsave;
    public string RemoveText => AppStrings.Remove;
    public string ReadyText => AppStrings.Ready;
    public string LoadingText => AppStrings.Loading;
    public string CouldNotLoadDataFormat => AppStrings.CouldNotLoadData;
    public string VisitedPrefixText => AppStrings.VisitedPrefix;

    public ICommand BackCommand { get; }
    public ICommand ShowBookmarksCommand { get; }
    public ICommand ShowHistoryCommand { get; }
    public ICommand OpenBookmarkDetailCommand { get; }
    public ICommand OpenHistoryDetailCommand { get; }
    public ICommand ToggleBookmarkCommand { get; }
    public ICommand RemoveHistoryItemCommand { get; }
    public ICommand ClearHistoryCommand { get; }

    public BookmarksHistoryViewModel(IBookmarkHistoryService bookmarkHistoryService)
    {
        _bookmarkHistoryService = bookmarkHistoryService;
        _bookmarkHistoryService.Changed += OnServiceChanged;
        UserProfileService.ProfileChanged += OnProfileChanged;

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        ShowBookmarksCommand = new Command(() => SetTab("Bookmarks"));
        ShowHistoryCommand = new Command(() => SetTab("History"));
        OpenBookmarkDetailCommand = new Command<PoiModel>(async poi => await OpenDetailAsync(poi));
        OpenHistoryDetailCommand = new Command<HistoryPoiItem>(async item => await OpenDetailAsync(item?.Poi));
        ToggleBookmarkCommand = new Command<PoiModel>(async poi => await ToggleBookmarkAsync(poi));
        RemoveHistoryItemCommand = new Command<HistoryPoiItem>(async item => await RemoveHistoryAsync(item));
        ClearHistoryCommand = new Command(async () => await ClearHistoryAsync());
    }

    public void SetTab(string tab)
    {
        _activeTab = tab;
        OnPropertyChanged(nameof(IsBookmarksTabActive));
        OnPropertyChanged(nameof(IsHistoryTabActive));
        OnPropertyChanged(nameof(ShowBookmarks));
        OnPropertyChanged(nameof(ShowHistory));
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        try
        {
            var language = UserProfileService.PreferredLanguage;
            var bookmarks = await _bookmarkHistoryService.GetBookmarksAsync(language, cancellationToken);
            var history = await _bookmarkHistoryService.GetHistoryAsync(language, cancellationToken);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Bookmarks.Clear();
                foreach (var item in bookmarks)
                {
                    Bookmarks.Add(item);
                }

                History.Clear();
                foreach (var item in history)
                {
                    History.Add(item);
                }

                OnPropertyChanged(nameof(BookmarksTabText));
                OnPropertyChanged(nameof(HistoryTabText));
                OnPropertyChanged(nameof(PageTitle));

                StatusText = ReadyText;
            });
        }
        catch (Exception ex)
        {
            StatusText = string.Format(System.Globalization.CultureInfo.CurrentUICulture, CouldNotLoadDataFormat, ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OpenDetailAsync(PoiModel? poi)
    {
        if (poi is null)
        {
            return;
        }

        await _bookmarkHistoryService.AddHistoryAsync(poi);
        await Shell.Current.GoToAsync($"TourDetailPage?tourId={poi.Id}&lang={Uri.EscapeDataString(UserProfileService.PreferredLanguage)}");
    }

    private async Task ToggleBookmarkAsync(PoiModel? poi)
    {
        if (poi is null)
        {
            return;
        }

        await _bookmarkHistoryService.ToggleBookmarkAsync(poi);
    }

    private async Task RemoveHistoryAsync(HistoryPoiItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _bookmarkHistoryService.RemoveHistoryAsync(item.Poi.Id);
    }

    private async Task ClearHistoryAsync()
    {
        await _bookmarkHistoryService.ClearHistoryAsync();
    }

    private void OnServiceChanged(object? sender, EventArgs e)
    {
        _ = RefreshAsync();
    }

    private void OnProfileChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(BookmarksTabText));
        OnPropertyChanged(nameof(HistoryTabText));
        OnPropertyChanged(nameof(CurrentLanguageText));
        OnPropertyChanged(nameof(NoBookmarkedToursYetText));
        OnPropertyChanged(nameof(NoHistoryYetText));
        OnPropertyChanged(nameof(OpenText));
        OnPropertyChanged(nameof(UnsaveText));
        OnPropertyChanged(nameof(RemoveText));
        OnPropertyChanged(nameof(ReadyText));
        OnPropertyChanged(nameof(LoadingText));
        OnPropertyChanged(nameof(VisitedPrefixText));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        _bookmarkHistoryService.Changed -= OnServiceChanged;
        UserProfileService.ProfileChanged -= OnProfileChanged;
    }
}
