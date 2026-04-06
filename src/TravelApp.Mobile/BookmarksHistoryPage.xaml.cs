using Microsoft.Extensions.DependencyInjection;
using TravelApp.ViewModels;

namespace TravelApp;

public partial class BookmarksHistoryPage : ContentPage, IQueryAttributable
{
    private readonly BookmarksHistoryViewModel _viewModel;

    public BookmarksHistoryPage()
    {
        InitializeComponent();
        _viewModel = MauiProgram.Services.GetRequiredService<BookmarksHistoryViewModel>();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshAsync();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("tab", out var tab))
        {
            return;
        }

        if (tab is string tabValue)
        {
            _viewModel.SetTab(tabValue.Equals("history", StringComparison.OrdinalIgnoreCase) ? "History" : "Bookmarks");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.Dispose();
    }
}
