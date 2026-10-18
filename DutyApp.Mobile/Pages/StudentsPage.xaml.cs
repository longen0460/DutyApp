using DutyApp.Mobile.Models;
using DutyApp.Mobile.Services;

namespace DutyApp.Mobile.Pages;

public partial class StudentsPage : ContentPage
{
    private readonly ApiClient _api;
    private readonly Session _session;

    public StudentsPage(ApiClient api, Session session)
    {
        InitializeComponent();
        _api = api;
        _session = session;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        AddPanel.IsVisible = _session.CanEditGroup;
        await LoadData();
    }

    private async Task LoadData()
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        ListView.ItemsSource = await _api.GetStudentsAsync();
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
    }

    private async void OnRefresh(object sender, EventArgs e) => await LoadData();

    private async void OnMenu(object sender, EventArgs e)
    {
        // Открываем главное меню
        await Navigation.PushAsync(new MainPage(_api, _session));
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameEntry.Text)) return;

        var student = await _api.AddStudentAsync(NameEntry.Text.Trim());
        if (student != null)
        {
            NameEntry.Text = "";
            await LoadData();
        }
        else
        {
            await DisplayAlert("Ошибка", "Не удалось добавить студента", "ОК");
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        var id = (int)btn.CommandParameter;

        bool confirm = await DisplayAlert("Удаление", "Удалить этого студента?", "Да", "Нет");
        if (confirm)
        {
            await _api.DeleteStudentAsync(id);
            await LoadData();
        }
    }
}