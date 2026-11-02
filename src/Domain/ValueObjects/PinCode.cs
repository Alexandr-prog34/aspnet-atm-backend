using System.Security.Cryptography;

namespace Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

public sealed class PinCode
{
    private const int Iterations = 210_000;
    private const int SaltLength = 16;
    private const int HashLength = 32;

    public PinCode(string rawPin)
    {
        if (string.IsNullOrWhiteSpace(rawPin) || rawPin.Length > 128)
            throw new ArgumentException("PIN must not be empty or longer than 128 characters.", nameof(rawPin));

        byte[] salt = RandomNumberGenerator.GetBytes(SaltLength);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(rawPin, salt, Iterations, HashAlgorithmName.SHA256, HashLength);
        StoredHash = $"pbkdf2-sha256:{Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private PinCode(string storedHash, bool _) => StoredHash = storedHash;

    public string StoredHash { get; }

    public static PinCode FromStoredHash(string storedHash) => new(storedHash, true);

    public bool Matches(string rawPin)
    {
        if (string.IsNullOrEmpty(rawPin)) return false;

        string[] parts = StoredHash.Split(':');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256" || !int.TryParse(parts[1], out int iterations))
            return false;

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (salt.Length != SaltLength || expected.Length != HashLength || iterations < Iterations) return false;
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(rawPin, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
