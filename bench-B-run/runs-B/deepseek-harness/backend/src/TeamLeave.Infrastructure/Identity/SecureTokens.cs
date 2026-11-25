using System.Security.Cryptography;
using System.Text;

namespace TeamLeave.Infrastructure.Identity;

public static class SecureTokens
{
    public static string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return "rt_" + Base64Url(bytes);
    }

    public static byte[] Hash(string token)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(token));
    }

    public static string CreateTemporaryPassword(int length = 20)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }

        return new string(chars);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
