using System;
using System.Text;

public static class RandomPasswordGenerator
{
    private static readonly Random _random = new Random();
    private const string Characters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^_";

    public static string GenerateRandomPassword(int length)
    {
        var password = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            char randomChar = Characters[_random.Next(Characters.Length)];
            password.Append(randomChar);
        }

        return password.ToString();
    }
}