
using application.Common.Interfaces;
using System.Security.Cryptography;

namespace infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
        password,
        salt,
        iterations: 100_000,
        hashAlgorithm: HashAlgorithmName.SHA256,
        outputLength: 32);

        return Convert.ToBase64String(salt) + ":" +
           Convert.ToBase64String(hash);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        string[] parts = hashedPassword.Split(':');

        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] hash = Convert.FromBase64String(parts[1]);

        byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
         password,
         salt,
         iterations: 100_000,
         hashAlgorithm: HashAlgorithmName.SHA256,
         outputLength: 32);

        return CryptographicOperations.FixedTimeEquals(
            hash,
            computedHash);
    }
}
