using Microsoft.Maui.Storage;

namespace MyJournalApp.Services;

/// <summary>
/// Service wrapper for MAUI SecureStorage with graceful error handling.
/// Provides safe read/write operations for sensitive app data.
/// </summary>
public class SecureStorageService
{
    /// <summary>
    /// Retrieves a value from secure storage by key.
    /// Returns null if key doesn't exist or value is corrupted.
    /// </summary>
    /// <param name="key">The key to retrieve the value for.</param>
    /// <returns>The stored value, or null if not found or corrupted.</returns>
    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await SecureStorage.GetAsync(key);
        }
        catch (Exception)
        {
            // Handle missing or corrupted values gracefully
            return null;
        }
    }

    /// <summary>
    /// Stores a value in secure storage with the specified key.
    /// </summary>
    /// <param name="key">The key to store the value under.</param>
    /// <param name="value">The value to store.</param>
    public async Task SetAsync(string key, string value)
    {
        try
        {
            await SecureStorage.SetAsync(key, value);
        }
        catch (Exception)
        {
            // Log error in production, but fail silently for now
            // This prevents app crashes if secure storage is unavailable
        }
    }

    /// <summary>
    /// Removes a value from secure storage by key.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    public Task RemoveAsync(string key)
    {
        try
        {
            SecureStorage.Remove(key);
        }
        catch (Exception)
        {
            // Handle errors gracefully
        }
        return Task.CompletedTask;
    }
}


