using DutyApp.Mobile.Services;

namespace DutyApp.Mobile.Pages;

public partial class BalancePage : ContentPage
{
    private readonly ApiClient _api;
    private readonly Session _session;

    public BalancePage(ApiClient api, Session session)
    {
        InitializeComponent();
        _api = api;
        _session = session;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        ListView.ItemsSource = await _api.GetBalanceAsync();
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
    }
}