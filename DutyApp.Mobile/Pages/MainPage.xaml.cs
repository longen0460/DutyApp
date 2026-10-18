using DutyApp.Mobile.Services;

namespace DutyApp.Mobile.Pages;

public partial class MainPage : ContentPage
{
    private readonly ApiClient _api;
    private readonly Session _session;

    public MainPage(ApiClient api, Session session)
    {
        InitializeComponent();
        _api = api;
        _session = session;
        UpdateThemeIcon();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        WelcomeLabel.Text = $"Добро пожаловать, {_session.Login} ({_session.Role})";
        UpdateThemeIcon();
    }

    private void UpdateThemeIcon()
    {
        ThemeIcon.Text = Application.Current!.UserAppTheme == AppTheme.Dark ? "☀️" : "🌙";
    }

    private async void OnStudentsClicked(object sender, EventArgs e) =>
        await Navigation.PushAsync(new StudentsPage(_api, _session));

    private async void OnBalanceClicked(object sender, EventArgs e) =>
        await Navigation.PushAsync(new BalancePage(_api, _session));

    private async void OnHistoryClicked(object sender, EventArgs e) =>
        await Navigation.PushAsync(new HistoryPage(_api, _session));

    private async void OnUsersClicked(object sender, EventArgs e) =>
        await Navigation.PushAsync(new UsersPage(_api, _session));

    private void OnThemeToggle(object sender, EventArgs e)
    {
        var next = Application.Current!.UserAppTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
        Application.Current.UserAppTheme = next;
        Preferences.Set("theme", next == AppTheme.Dark ? "Dark" : "Light");
        UpdateThemeIcon();
    }

    private void OnLogout(object sender, EventArgs e)
    {
        _session.Clear();
        Application.Current!.MainPage = new NavigationPage(new LoginPage(_api, _session));
    }
}