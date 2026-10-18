using DutyApp.Mobile.Pages;
using DutyApp.Mobile.Services;

namespace DutyApp.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        var savedTheme = Preferences.Get("theme", "Light");
        UserAppTheme = savedTheme == "Dark" ? AppTheme.Dark : AppTheme.Light;

        var session = new Session();
        var api = new ApiClient(session);

        if (session.Load())
        {
            // Уже вошел ранее — сразу на главную
            MainPage = new NavigationPage(new MainPage(api, session));
        }
        else
        {
            MainPage = new NavigationPage(new LoginPage(api, session));
        }
    }
}