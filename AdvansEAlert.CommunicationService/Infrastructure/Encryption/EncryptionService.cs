using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace AdvansEAlert.CommunicationService.Infrastructure.Encryption;

public interface IEncryptionService
{
    string Encrypt(string clearText);
    string Decrypt(string cipherText);
}

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv = new byte[16]; // Fixed IV for simplicity in this centralized service, or store IV with cipher text

    public EncryptionService(IConfiguration configuration)
    {
        var keyString = configuration["Encryption:Key"] ?? "A6D9A6D9A6D9A6D9A6D9A6D9A6D9A6D9";
        _key = Encoding.UTF8.GetBytes(keyString);
    }

    public string Encrypt(string clearText)
    {
        using Aes aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using MemoryStream ms = new MemoryStream();
        using CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using (StreamWriter sw = new StreamWriter(cs))
        {
            sw.Write(clearText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        using Aes aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using MemoryStream ms = new MemoryStream(Convert.FromBase64String(cipherText));
        using CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using StreamReader sr = new StreamReader(cs);
        
        return sr.ReadToEnd();
    }
}
