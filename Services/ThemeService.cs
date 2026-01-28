namespace MyJournalApp.Services;

/// <summary>
/// Service for managing light and dark theme preferences.
/// Persists theme selection using SecureStorageService.
/// </summary>
public class ThemeService
{
    private const string ThemeKey = "theme_preference";
    private const string LightTheme = "light";
    private const string DarkTheme = "dark";

    private readonly SecureStorageService _secureStorage;
    private string _currentTheme = LightTheme;

    public ThemeService(SecureStorageService secureStorage)
    {
        _secureStorage = secureStorage;
    }

    /// <summary>
    /// Gets the current theme (light or dark).
    /// </summary>
    public string GetCurrentTheme() => _currentTheme;

    public event Action? OnChange;

    /// <summary>
    /// Toggles between light and dark theme.
    /// </summary>
    public async Task ToggleThemeAsync()
    {
        if (_currentTheme == LightTheme)
        {
            await SetThemeAsync(DarkTheme);
        }
        else
        {
            await SetThemeAsync(LightTheme);
        }
    }

    /// <summary>
    /// Sets the theme to the specified value.
    /// </summary>
    /// <param name="theme">The theme to set (light or dark).</param>
    public async Task SetThemeAsync(string theme)
    {
        _currentTheme = theme;
        await _secureStorage.SetAsync(ThemeKey, theme);
        NotifyStateChanged();
    }

    /// <summary>
    /// Loads the saved theme preference from secure storage.
    /// Called on app startup.
    /// </summary>
    public async Task LoadThemeAsync()
    {
        var savedTheme = await _secureStorage.GetAsync(ThemeKey);
        if (!string.IsNullOrEmpty(savedTheme) && (savedTheme == LightTheme || savedTheme == DarkTheme))
        {
            _currentTheme = savedTheme;
        }
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}

