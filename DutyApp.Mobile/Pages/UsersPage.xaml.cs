using DutyApp.Mobile.Services;

namespace DutyApp.Mobile.Pages;

public partial class UsersPage : ContentPage
{
    private readonly ApiClient _api;
    private readonly Session _session;

    public UsersPage(ApiClient api, Session session)
    {
        InitializeComponent();
        _api = api;
        _session = session;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_session.IsAdmin)
        {
            await DisplayAlert("Доступ запрещен", "Только администратор может управлять пользователями", "ОК");
            await Navigation.PopAsync();
            return;
        }
        await LoadUsers();
    }

    private async Task LoadUsers()
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        var users = await _api.GetUsersAsync();
        ListView.ItemsSource = users;
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
    }

    private async void OnCreateClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(LoginEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            await DisplayAlert("Ошибка", "Заполните логин и пароль", "ОК");
            return;
        }
        if (RolePicker.SelectedItem == null)
        {
            await DisplayAlert("Ошибка", "Выберите роль", "ОК");
            return;
        }

        var ok = await _api.CreateUserAsync(LoginEntry.Text.Trim(), PasswordEntry.Text, RolePicker.SelectedItem.ToString()!);
        if (ok)
        {
            LoginEntry.Text = "";
            PasswordEntry.Text = "";
            RolePicker.SelectedItem = null;
            await LoadUsers();
        }
        else
        {
            await DisplayAlert("Ошибка", "Не удалось создать пользователя (возможно, логин занят)", "ОК");
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        var id = (int)btn.CommandParameter;
        bool confirm = await DisplayAlert("Удаление", "Удалить этого пользователя?", "Да", "Нет");
        if (confirm)
        {
            var ok = await _api.DeleteUserAsync(id);
            if (ok) await LoadUsers();
            else await DisplayAlert("Ошибка", "Не удалось удалить", "ОК");
        }
    }
}