using System;
using System.Security.Cryptography;
using System.Text;

namespace ITBees.UserManager.Controllers;

public static class TokenLogHelper
{
    public static string Sha256(string value)
    {
        using (var sha = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            var hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }

    public static string Prefix(string value, int len = 12)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= len ? value : value.Substring(0, len);
    }
}