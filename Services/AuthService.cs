using System.Security.Cryptography;

namespace MyJournalApp.Services;

public class AuthService
{
    private const string PinHashKey = "journal_pin_hash";
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    private readonly SecureStorageService _secureStorage;
    private string? _storedHash;

    public AuthService(SecureStorageService secureStorage)
    {
        _secureStorage = secureStorage;
    }

    public bool IsUnlocked { get; private set; } = true;
    public bool IsPinSet => !string.IsNullOrWhiteSpace(_storedHash);

    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        _storedHash = await _secureStorage.GetAsync(PinHashKey);
        IsUnlocked = string.IsNullOrWhiteSpace(_storedHash);
        NotifyStateChanged();
    }

    public async Task<bool> UnlockAsync(string pin)
    {
        if (!IsPinSet)
        {
            IsUnlocked = true;
            NotifyStateChanged();
            return true;
        }

        if (!VerifyPin(pin))
        {
            return false;
        }

        IsUnlocked = true;
        NotifyStateChanged();
        return true;
    }

    public async Task SetPinAsync(string pin)
    {
        var value = HashPin(pin);
        await _secureStorage.SetAsync(PinHashKey, value);
        _storedHash = value;
        IsUnlocked = true;
        NotifyStateChanged();
    }

    public async Task<bool> ChangePinAsync(string currentPin, string newPin)
    {
        if (!VerifyPin(currentPin))
        {
            return false;
        }

        await SetPinAsync(newPin);
        return true;
    }

    public async Task<bool> ClearPinAsync(string pin)
    {
        if (!VerifyPin(pin))
        {
            return false;
        }

        await _secureStorage.RemoveAsync(PinHashKey);
        _storedHash = null;
        IsUnlocked = true;
        NotifyStateChanged();
        return true;
    }

    public void Lock()
    {
        if (!IsPinSet)
        {
            return;
        }

        IsUnlocked = false;
        NotifyStateChanged();
    }

    private string HashPin(string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private bool VerifyPin(string pin)
    {
        if (string.IsNullOrWhiteSpace(_storedHash))
        {
            return false;
        }

        var parts = _storedHash.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expected = Convert.FromBase64String(parts[1]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}

