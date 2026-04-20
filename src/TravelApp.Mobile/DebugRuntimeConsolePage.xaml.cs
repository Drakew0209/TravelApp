using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using TravelApp.Resources.Strings;
using TravelApp.Services.Abstractions;

namespace TravelApp;

public partial class DebugRuntimeConsolePage : ContentPage
{
    private readonly ILogService _logService;

    public ObservableCollection<string> LogLines { get; } = [];
    public string PageTitle => AppStrings.DebugRuntimeTitle;
    public string DebugModeText => AppStrings.DebugMode;
    public string ClearText => AppStrings.Clear;
    public string ClearLogsA11yText => $"{AppStrings.Clear} {AppStrings.Logs}";

    public DebugRuntimeConsolePage()
    {
        InitializeComponent();
        _logService = MauiProgram.Services.GetRequiredService<ILogService>();
        BindingContext = this;

        DebugModeSwitch.IsToggled = _logService.IsEnabled;
        DebugModeSwitch.Toggled += OnDebugModeToggled;
        StatusLabel.Text = BuildStatus();

        foreach (var entry in _logService.GetLogs())
        {
            LogLines.Add(FormatEntry(entry));
        }

        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(DebugModeText));
        OnPropertyChanged(nameof(ClearText));
        OnPropertyChanged(nameof(ClearLogsA11yText));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _logService.LogAdded += OnLogAdded;
        StatusLabel.Text = BuildStatus();
        ScrollToBottom();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _logService.LogAdded -= OnLogAdded;
    }

    private void OnLogAdded(object? sender, Models.Runtime.RuntimeLogEntry entry)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LogLines.Add(FormatEntry(entry));
            StatusLabel.Text = BuildStatus();
            ScrollToBottom();
        });
    }

    private void OnDebugModeToggled(object? sender, ToggledEventArgs e)
    {
        _logService.IsEnabled = e.Value;
        StatusLabel.Text = BuildStatus();
    }

    private void OnClearClicked(object? sender, EventArgs e)
    {
        _logService.Clear();
        LogLines.Clear();
        StatusLabel.Text = BuildStatus();
    }

    private string BuildStatus()
    {
        return $"{AppStrings.DebugMode}: {(_logService.IsEnabled ? AppStrings.On : AppStrings.Off)} | {AppStrings.Logs}: {LogLines.Count}";
    }

    private static string FormatEntry(Models.Runtime.RuntimeLogEntry entry)
    {
        return $"[{entry.TimestampUtc:HH:mm:ss}] [{entry.Source}] {entry.Message}";
    }

    private void ScrollToBottom()
    {
        if (LogLines.Count == 0)
        {
            return;
        }

        LogsCollectionView.ScrollTo(LogLines.Count - 1, position: ScrollToPosition.End, animate: false);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
