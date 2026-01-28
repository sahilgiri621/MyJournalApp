using MyJournalApp.Services;
using Microsoft.Extensions.Logging;
using SQLitePCL;

namespace MyJournalApp;

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
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Register services as singletons
        builder.Services.AddSingleton<SecureStorageService>();
        builder.Services.AddSingleton<ThemeService>();
        builder.Services.AddSingleton<JournalService>();
        builder.Services.AddSingleton<MarkdownService>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<PdfExportService>();

        Batteries.Init();
        var app = builder.Build();

        // Initialize services on app start
        var themeService = app.Services.GetRequiredService<ThemeService>();
        _ = Task.Run(async () => await themeService.LoadThemeAsync());

        var journalService = app.Services.GetRequiredService<JournalService>();
        _ = Task.Run(async () => await journalService.InitializeAsync());

        var authService = app.Services.GetRequiredService<AuthService>();
        _ = Task.Run(async () => await authService.InitializeAsync());

        return app;
    }
}

