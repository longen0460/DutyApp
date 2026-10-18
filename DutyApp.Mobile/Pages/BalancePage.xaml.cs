using DutyApp.Mobile.Models;
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
        await LoadData();
    }

    private async Task LoadData()
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        ListView.ItemsSource = await _api.GetBalanceAsync();
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
    }

    private async void OnAssignClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Дежурные", "Назначить двоих дежурных на сегодня?", "Да", "Нет");
        if (!confirm) return;

        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;

        var result = await _api.AutoAssignAsync(DateTime.Today, DateTime.Today, 2, false);

        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;

        if (result != null && result.Count > 0)
        {
            await DisplayAlert("Успех", "Дежурные на сегодня назначены!", "ОК");
            await LoadData();
        }
        else
        {
            await DisplayAlert("Ошибка", "Не удалось назначить. Возможно, нет студентов.", "ОК");
        }
    }

    private async void OnStudentTapped(object sender, TappedEventArgs e)
    {
        var border = (Border)sender;
        var item = (BalanceItem)border.BindingContext;

        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        var history = await _api.GetStudentHistoryAsync(item.Id);
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;

        if (history.Count == 0)
        {
            await DisplayAlert(item.FullName, "Ещё не дежурил", "ОК");
            return;
        }

        var dates = string.Join("\n", history.Select(h => h.Date.ToString("dd.MM.yyyy")));
        await DisplayAlert(item.FullName, $"Дежурства:\n{dates}", "ОК");
    }
}