using DutyApp.Mobile.Services;

namespace DutyApp.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly ApiClient _api;
    private readonly Session _session;

    public LoginPage(ApiClient api, Session session)
    {
        InitializeComponent();
        _api = api;
        _session = session;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        ErrorLabel.Text = "";
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        var role = await _api.LoginAsync(LoginEntry.Text?.Trim() ?? "", PasswordEntry.Text ?? "");

        LoadingIndicator.IsVisible = false;
        LoadingIndicator.IsRunning = false;

        if (role == null)
        {
            ErrorLabel.Text = "Неверный логин/пароль или сервер недоступен.";
            return;
        }

        _session.Save();
        Application.Current!.MainPage = new NavigationPage(new MainPage(_api, _session));
    }
}