using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;
using TravelApp.Resources.Strings;
using TravelApp.Services;
using TravelApp.Services.Abstractions;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace TravelApp;

public sealed class QrScannerPage : ContentPage
{
    private readonly IQrCodeParserService _qrCodeParserService;
    private readonly IAnalyticsTrackingService _analyticsTrackingService;
    private readonly CameraBarcodeReaderView _scannerView;
    private readonly Label _statusLabel;
    private bool _isHandlingScan;
    private readonly bool _showManualFallback;

    public QrScannerPage()
    {
        _qrCodeParserService = MauiProgram.Services.GetRequiredService<IQrCodeParserService>();
        _analyticsTrackingService = MauiProgram.Services.GetRequiredService<IAnalyticsTrackingService>();

        _scannerView = new CameraBarcodeReaderView
        {
            IsDetecting = true,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormats.All,
                AutoRotate = true,
                Multiple = false
            }
        };
        _scannerView.BarcodesDetected += OnBarcodesDetected;

        _statusLabel = new Label
        {
            Text = AppStrings.ReadyToReceiveQr,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#B51A50"),
            HorizontalTextAlignment = TextAlignment.Center
        };

        _showManualFallback = ShouldShowManualFallback();

        Content = BuildContent();
        BackgroundColor = Colors.Black;
        Shell.SetNavBarIsVisible(this, false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isHandlingScan = false;
        _scannerView.IsDetecting = true;
        UpdateStatus(AppStrings.ReadyToReceiveQr, Color.FromArgb("#B51A50"), Color.FromArgb("#FFF1F6"));

        var permission = await Permissions.RequestAsync<Permissions.Camera>();
        if (permission != PermissionStatus.Granted)
        {
            _scannerView.IsDetecting = false;
            await DisplayAlert(AppStrings.QrCodeTitle, AppStrings.CameraPermissionRequired, "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    protected override void OnDisappearing()
    {
        _scannerView.IsDetecting = false;
        base.OnDisappearing();
    }

    private View BuildContent()
    {
        var topBar = new Grid
        {
            Padding = new Thickness(16, 52, 16, 12),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        var closeButton = new Border
        {
            WidthRequest = 44,
            HeightRequest = 44,
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb("#E31667"),
            StrokeShape = new RoundRectangle { CornerRadius = 22 }
        };
        closeButton.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Shell.Current.GoToAsync("..")) });
        closeButton.Content = new Label
        {
            Text = AppStrings.QrBackIcon,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        var titleBlock = new VerticalStackLayout
        {
            Spacing = 2,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        titleBlock.Children.Add(new Label
        {
            Text = AppStrings.QrScannerTitle,
            FontSize = 24,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center
        });
        titleBlock.Children.Add(new Label
        {
            Text = AppStrings.QrScannerSubtitle,
            FontSize = 13,
            TextColor = Color.FromArgb("#D9DDE8"),
            HorizontalTextAlignment = TextAlignment.Center
        });

        var qrBadge = new Border
        {
            WidthRequest = 44,
            HeightRequest = 44,
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb("#FFFFFF22"),
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            Content = new Label
            {
                Text = AppStrings.QrCodeTitle,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };

        topBar.Add(closeButton);
        topBar.Add(titleBlock);
        topBar.Add(qrBadge);
        Grid.SetColumn(titleBlock, 1);
        Grid.SetColumn(qrBadge, 2);

        var scannerGrid = new Grid();
        scannerGrid.Children.Add(_scannerView);
        scannerGrid.Children.Add(new Border
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            StrokeThickness = 0,
            BackgroundColor = Colors.Transparent,
            Content = new Label
            {
                Text = AppStrings.QrScanInstruction,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                Padding = new Thickness(14, 6),
                BackgroundColor = Color.FromArgb("#66000000"),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        });
        scannerGrid.Children.Add(new Grid
        {
            BackgroundColor = Color.FromArgb("#77000000"),
            Children =
            {
                new Border
                {
                    Stroke = Color.FromArgb("#E31667"),
                    StrokeThickness = 2,
                    StrokeShape = new RoundRectangle { CornerRadius = 24 },
                    WidthRequest = 260,
                    HeightRequest = 260,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    BackgroundColor = Colors.Transparent
                }
            }
        });

        var infoCard = new Border
        {
            Margin = new Thickness(16, 0, 16, 16),
            Padding = 16,
            StrokeThickness = 0,
            BackgroundColor = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 24 }
        };
        var infoStack = new VerticalStackLayout { Spacing = 12 };

        infoStack.Children.Add(new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12
        });
        var guideIcon = new Border
        {
            WidthRequest = 44,
            HeightRequest = 44,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            BackgroundColor = Color.FromArgb("#FFF1F6"),
            Content = new Label
            {
                Text = AppStrings.QrGuideIcon,
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#E31667"),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };
        infoStack.Children.Add(guideIcon);

        var guideTextStack = new VerticalStackLayout { Spacing = 4 };
        guideTextStack.Children.Add(new Label
        {
            Text = AppStrings.WebAdminQr,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1B1F28")
        });
        guideTextStack.Children.Add(new Label
        {
            Text = AppStrings.QrScannerSubtitle,
            FontSize = 13,
            TextColor = Color.FromArgb("#5D6472"),
            LineBreakMode = LineBreakMode.WordWrap
        });
        guideTextStack.Children.Add(BuildChipRow());
        infoStack.Children.Add(guideTextStack);

        var statusCard = new Border
        {
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb("#FFF1F6"),
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Padding = new Thickness(12, 10)
        };
        statusCard.Content = _statusLabel;
        infoStack.Children.Add(statusCard);

        if (_showManualFallback)
        {
            infoStack.Children.Add(BuildFallbackSection());
        }

        infoCard.Content = infoStack;

        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };
        root.Children.Add(topBar);
        root.Children.Add(scannerGrid);
        root.Children.Add(infoCard);
        Grid.SetRow(scannerGrid, 1);
        Grid.SetRow(infoCard, 2);

        return root;
    }

    private View BuildChipRow()
    {
        var chips = new HorizontalStackLayout
        {
            Spacing = 8,
            Margin = new Thickness(0, 4, 0, 0)
        };

        chips.Children.Add(CreateChip(AppStrings.WebAdminQr));
        chips.Children.Add(CreateChip(AppStrings.OfflineOk));
        chips.Children.Add(CreateChip(AppStrings.PoiIdAndLang));

        return chips;
    }

    private static View CreateChip(string text)
    {
        return new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 999 },
            BackgroundColor = Color.FromArgb("#F2F4F8"),
            Padding = new Thickness(10, 5),
            Content = new Label
            {
                Text = text,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#4E5668")
            }
        };
    }

    private void UpdateStatus(string text, Color textColor, Color backgroundColor)
    {
        _statusLabel.Text = text;
        _statusLabel.TextColor = textColor;

        if (_statusLabel.Parent is Border border)
        {
            border.BackgroundColor = backgroundColor;
        }
    }

    private View BuildFallbackSection()
    {
        var container = new VerticalStackLayout
        {
            Spacing = 10,
            Margin = new Thickness(0, 8, 0, 0)
        };

        container.Children.Add(new BoxView
        {
            HeightRequest = 1,
            Color = Color.FromArgb("#E8EBF2")
        });

        container.Children.Add(new Label
        {
            Text = AppStrings.EmulatorFallbackTitle,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1B1F28")
        });

        var pasteButton = new Button
        {
            Text = AppStrings.PasteQrLink,
            BackgroundColor = Color.FromArgb("#F7F8FB"),
            TextColor = Color.FromArgb("#1B1F28"),
            BorderColor = Color.FromArgb("#D6DCE8"),
            BorderWidth = 1,
            CornerRadius = 16,
            HeightRequest = 44
        };
        pasteButton.Clicked += async (_, _) => await PasteQrContentAsync();

        var inputButton = new Button
        {
            Text = AppStrings.EnterQrManually,
            BackgroundColor = Color.FromArgb("#E31667"),
            TextColor = Colors.White,
            CornerRadius = 16,
            HeightRequest = 44
        };
        inputButton.Clicked += async (_, _) => await PromptQrContentAsync();

        var buttonsGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        buttonsGrid.Children.Add(pasteButton);
        buttonsGrid.Children.Add(inputButton);
        Grid.SetColumn(inputButton, 1);

        container.Children.Add(buttonsGrid);

        container.Children.Add(new Label
        {
            Text = AppStrings.EmulatorFallbackDescription,
            FontSize = 12,
            TextColor = Color.FromArgb("#6E7380"),
            LineBreakMode = LineBreakMode.WordWrap
        });

        return container;
    }

    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_isHandlingScan)
        {
            return;
        }

        var scannedText = e.Results?.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(scannedText))
        {
            return;
        }

        _isHandlingScan = true;
        _scannerView.IsDetecting = false;

        MainThread.BeginInvokeOnMainThread(async () => await HandleScanAsync(scannedText));
    }

    private async Task HandleScanAsync(string scannedText)
    {
        try
        {
            UpdateStatus(AppStrings.ProcessingQr, Color.FromArgb("#B51A50"), Color.FromArgb("#FFF1F6"));

            var poiId = _qrCodeParserService.TryParsePoiId(scannedText);
            if (!poiId.HasValue)
            {
                UpdateStatus(AppStrings.CannotReadPoiId, Color.FromArgb("#8B1E3F"), Color.FromArgb("#FFE7EE"));
                await DisplayAlert(AppStrings.QrCodeTitle, AppStrings.InvalidQrFormat, "OK");
                _isHandlingScan = false;
                _scannerView.IsDetecting = true;
                return;
            }

            UpdateStatus(string.Format(System.Globalization.CultureInfo.CurrentUICulture, AppStrings.QrOpenedPoiFormat, poiId.Value), Color.FromArgb("#0E7A55"), Color.FromArgb("#EAF8F1"));
            _ = _analyticsTrackingService.TrackQrScannedAsync(poiId.Value, scannedText, UserProfileService.PreferredLanguage, CancellationToken.None);
            await Shell.Current.GoToAsync("..");
            await Task.Delay(120);
            await Shell.Current.GoToAsync($"TourDetailPage?tourId={poiId.Value}");
        }
        catch (Exception ex)
        {
            UpdateStatus(AppStrings.QrOpenError, Color.FromArgb("#8B1E3F"), Color.FromArgb("#FFE7EE"));
            await DisplayAlert(AppStrings.QrCodeTitle, string.Format(System.Globalization.CultureInfo.CurrentUICulture, AppStrings.CouldNotDownloadTour, ex.Message), "OK");
            _isHandlingScan = false;
            _scannerView.IsDetecting = true;
        }
    }

    private async Task PromptQrContentAsync()
    {
        var qrContent = await DisplayPromptAsync(
            AppStrings.QrCodeTitle,
            AppStrings.InvalidQrFormat,
            accept: AppStrings.Process,
            cancel: AppStrings.Cancel,
            placeholder: AppStrings.QrPromptPlaceholder);

        if (string.IsNullOrWhiteSpace(qrContent))
        {
            return;
        }

        await HandleScanAsync(qrContent);
    }

    private async Task PasteQrContentAsync()
    {
        var qrContent = await Clipboard.Default.GetTextAsync();
        if (string.IsNullOrWhiteSpace(qrContent))
        {
            await DisplayAlert(AppStrings.NotEnoughClipboardText, AppStrings.NoClipboardContent, "OK");
            return;
        }

        await HandleScanAsync(qrContent);
    }

    private static bool ShouldShowManualFallback()
    {
        return Debugger.IsAttached || DeviceInfo.DeviceType == DeviceType.Virtual;
    }
}
