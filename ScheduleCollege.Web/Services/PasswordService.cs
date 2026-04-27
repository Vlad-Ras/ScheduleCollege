using System.Security.Cryptography;

namespace ScheduleCollege.Web.Services;

public static class PasswordService
{
    public static string CreateHash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string savedHash)
    {
        var parts = savedHash.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] expected = Convert.FromBase64String(parts[1]);
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
