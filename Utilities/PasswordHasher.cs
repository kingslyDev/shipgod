using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace ShipmentFinishGood.Utilities;

public static class PasswordHasher
{
    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] subkey = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, 10000, 32);
        return Convert.ToBase64String(salt.Concat(subkey).ToArray());
    }

    public static bool Verify(string hashed, string provided)
    {
        try
        {
            var bytes = Convert.FromBase64String(hashed);
            var salt = bytes.Take(16).ToArray();
            var stored = bytes.Skip(16).ToArray();
            var generated = KeyDerivation.Pbkdf2(provided, salt, KeyDerivationPrf.HMACSHA256, 10000, 32);
            return stored.SequenceEqual(generated);
        }
        catch { return false; }
    }
}
