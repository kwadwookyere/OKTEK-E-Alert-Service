using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace EAlertService.Infrastructure.Encryption;

public interface IEncryptionService
{
    string Encrypt(string clearText);
    string Decrypt(string cipherText);
}

public class EncryptionService : IEncryptionService
{
    private const int IvSize = 16;
    private readonly byte[] _key;

    public EncryptionService(IConfiguration configuration)
    {
        var keyString = configuration["Encryption:Key"] ?? "A6D9A6D9A6D9A6D9A6D9A6D9A6D9A6D9";
        _key = Encoding.UTF8.GetBytes(keyString);
    }

    /// <summary>
    /// Encrypts clearText using AES-CBC with a randomly generated IV.
    /// The IV is prepended to the ciphertext (first 16 bytes of the Base64 output).
    /// </summary>
    public string Encrypt(string clearText)
    {
        using Aes aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV(); // Bug fix #1: random IV per encryption — never reuse a fixed IV

        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, IvSize); // Prepend IV so Decrypt can recover it

        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(clearText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// Decrypts a ciphertext produced by Encrypt().
    /// Supports both the new format (IV-prefixed) and the legacy all-zero IV format
    /// to allow gradual re-encryption of existing DB records.
    /// </summary>
    public string Decrypt(string cipherText)
    {
        var data = Convert.FromBase64String(cipherText);

        // New format: first 16 bytes are the IV, rest is ciphertext
        if (data.Length > IvSize)
        {
            try
            {
                using Aes aes = Aes.Create();
                aes.Key = _key;
                aes.IV = data[..IvSize];

                using var ms = new MemoryStream(data[IvSize..]);
                using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);
                return sr.ReadToEnd();
            }
            catch (CryptographicException)
            {
                // Fall through to legacy path
            }
        }

        // Legacy fallback: zero IV (for records encrypted before this fix)
        using Aes legacyAes = Aes.Create();
        legacyAes.Key = _key;
        legacyAes.IV = new byte[IvSize];

        using var legacyMs = new MemoryStream(data);
        using var legacyCs = new CryptoStream(legacyMs, legacyAes.CreateDecryptor(), CryptoStreamMode.Read);
        using var legacySr = new StreamReader(legacyCs);
        return legacySr.ReadToEnd();
    }
}
