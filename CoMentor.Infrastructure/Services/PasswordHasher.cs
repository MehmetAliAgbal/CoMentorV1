using System.Security.Cryptography;

namespace CoMentor.Infrastructure.Services;
public static class PasswordHasher
{
    // PBKDF2: "iterations.salt.hash"
    public static string Hash(string password, int iterations = 100_000)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        try
        {
            var parts = stored.Split('.');
            var iterations = int.Parse(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var hash = Convert.FromBase64String(parts[2]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var computed = pbkdf2.GetBytes(hash.Length);
            return CryptographicOperations.FixedTimeEquals(computed, hash);
        }
        catch
        {
            return false;
        }
    }
}