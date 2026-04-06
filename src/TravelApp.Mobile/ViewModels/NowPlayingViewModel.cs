using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TravelApp.Models.Runtime;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public sealed class NowPlayingViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IAudioPlayerService _audioPlayerService;

    private bool _isPlaying;
    private string _poiTitle = "Chưa phát audio";

    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            if (_isPlaying == value)
            {
                return;
            }

            _isPlaying = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(ActionButtonText));
        }
    }

    public string PoiTitle
    {
        get => _poiTitle;
        private set
        {
            if (_poiTitle == value)
            {
                return;
            }

            _poiTitle = value;
            OnPropertyChanged();
        }
    }

    public string StatusText => IsPlaying ? "Đang phát" : "Đã dừng";

    public string ActionButtonText => IsPlaying ? "Stop" : "Back";

    public ICommand BackCommand { get; }
    public ICommand ActionCommand { get; }

    public NowPlayingViewModel(IAudioPlayerService audioPlayerService)
    {
        _audioPlayerService = audioPlayerService;

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        ActionCommand = new Command(async () =>
        {
            if (IsPlaying)
            {
                await _audioPlayerService.StopAsync();
                return;
            }

            await Shell.Current.GoToAsync("..");
        });

        _audioPlayerService.PlaybackStateChanged += OnPlaybackStateChanged;
        ApplyState(_audioPlayerService.IsPlaying, _audioPlayerService.CurrentPoiTitle);
    }

    private void OnPlaybackStateChanged(object? sender, AudioPlaybackStateChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() => ApplyState(e.IsPlaying, e.PoiTitle));
    }

    private void ApplyState(bool isPlaying, string? poiTitle)
    {
        IsPlaying = isPlaying;
        PoiTitle = isPlaying ? (string.IsNullOrWhiteSpace(poiTitle) ? "Địa điểm hiện tại" : poiTitle) : "Chưa phát audio";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        _audioPlayerService.PlaybackStateChanged -= OnPlaybackStateChanged;
    }
}
