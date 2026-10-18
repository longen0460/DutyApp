using Microsoft.Extensions.Logging;
using DutyApp.Mobile.Services;
using DutyApp.Mobile.Pages;

namespace DutyApp.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Регистрируем только сервисы
        builder.Services.AddSingleton<Session>();
        builder.Services.AddSingleton<ApiClient>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}