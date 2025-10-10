using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace QuantConnect.InteractiveBrokers.ToolBox.Security;

public interface ICredentialStore
{
    Task SaveAsync(string key, string secret, CancellationToken cancellationToken = default);
    Task<string?> LoadAsync(string key, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}

public sealed class CredentialStore : ICredentialStore
{
    private readonly ISecretProtector _protector;
    private readonly string _storeDirectory;

    public CredentialStore(string? storeDirectory = null, ISecretProtector? protector = null)
    {
        _storeDirectory = storeDirectory ?? GetDefaultStoreDirectory();
        _protector = protector ?? SecretProtectorFactory.CreateDefault(_storeDirectory);
    }

    public async Task SaveAsync(string key, string secret, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(secret);

        Directory.CreateDirectory(_storeDirectory);

        var filePath = GetFilePath(key);
        var protectedBytes = _protector.Protect(Encoding.UTF8.GetBytes(secret));

        var tempPath = filePath + ".tmp";
        await File.WriteAllBytesAsync(tempPath, protectedBytes, cancellationToken).ConfigureAwait(false);

        File.Move(tempPath, filePath, overwrite: true);
    }

    public async Task<string?> LoadAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
        {
            return null;
        }

        var protectedBytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
        var unprotected = _protector.Unprotect(protectedBytes);
        return Encoding.UTF8.GetString(unprotected);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var filePath = GetFilePath(key);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    private string GetFilePath(string key)
    {
        var sanitized = string.Join("_", key.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        return Path.Combine(_storeDirectory, sanitized + ".bin");
    }

    private static string GetDefaultStoreDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "QuantConnect", "InteractiveBrokers", "Credentials");
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".quantconnect", "interactivebrokers", "credentials");
    }
}

public interface ISecretProtector
{
    byte[] Protect(byte[] plaintext);
    byte[] Unprotect(byte[] protectedData);
}

public static class SecretProtectorFactory
{
    public static ISecretProtector CreateDefault(string storeDirectory)
    {
        // Use cross-platform AES-based protector on all platforms to avoid runtime-only
        // types like ProtectedData and UnixFileMode which may not be available at compile time
        return new CrossPlatformSecretProtector(Path.Combine(storeDirectory, "secret.key"));
    }
}

// WindowsSecretProtector intentionally removed to avoid referencing
// ProtectedData/DataProtectionScope which are platform-specific at compile time.

internal sealed class CrossPlatformSecretProtector : ISecretProtector
{
    private readonly string _keyPath;
    private byte[]? _cachedKey;

    public CrossPlatformSecretProtector(string keyPath)
    {
        _keyPath = keyPath;
    }

    public byte[] Protect(byte[] plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = GetOrCreateKey();
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        ms.Write(aes.IV);
        using (var cryptoStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write, leaveOpen: true))
        {
            cryptoStream.Write(plaintext, 0, plaintext.Length);
            cryptoStream.FlushFinalBlock();
        }

        return ms.ToArray();
    }

    public byte[] Unprotect(byte[] protectedData)
    {
        using var aes = Aes.Create();
        aes.Key = GetOrCreateKey();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var ms = new MemoryStream(protectedData);
        var iv = new byte[aes.BlockSize / 8];
        if (ms.Read(iv, 0, iv.Length) != iv.Length)
        {
            throw new InvalidOperationException("Invalid protected payload - IV missing");
        }

        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        using var cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var result = new MemoryStream();
        cryptoStream.CopyTo(result);
        return result.ToArray();
    }

    private byte[] GetOrCreateKey()
    {
        if (_cachedKey is { Length: > 0 })
        {
            return _cachedKey;
        }

        if (File.Exists(_keyPath))
        {
            _cachedKey = File.ReadAllBytes(_keyPath);
            return _cachedKey;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_keyPath)!);
        var key = RandomNumberGenerator.GetBytes(32);
        File.WriteAllBytes(_keyPath, key);
        TryHardenPermissions(_keyPath);
        _cachedKey = key;
        return key;
    }

    private static void TryHardenPermissions(string path)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                File.SetAttributes(path, FileAttributes.Hidden);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                // Try to set file permissions to 600 using chmod as a best-effort
                try
                {
                    var psi = new ProcessStartInfo("chmod", $"600 \"{path}\"") { UseShellExecute = false };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(1000);
                }
                catch
                {
                    // ignore
                }
            }
        }
        catch
        {
            // Best effort – ignore failures because tests may run on file systems that do not support these operations.
        }
    }
}
