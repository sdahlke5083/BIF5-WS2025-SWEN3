using System.Security.Cryptography;

namespace Paperless.REST.BLL.Security
{
    /// <summary>
    /// Provides static methods for securely hashing passwords and verifying password hashes using PBKDF2 with
    /// HMACSHA256.
    /// </summary>
    /// <remarks>The <see cref="PasswordHasher"/> class uses PBKDF2 with HMACSHA256, 100,000 iterations, a
    /// 16-byte random salt, and a 32-byte hash for password security. All methods are thread-safe and intended for use
    /// in authentication scenarios where password storage and verification are required.</remarks>
    public static class PasswordHasher
    {
        /// <summary>
        /// Generates a salted SHA-256 hash of the specified password using PBKDF2.
        /// </summary>
        /// <remarks>The returned string includes both the randomly generated salt and the derived hash,
        /// concatenated and encoded in Base64.  This method uses 100,000 iterations of PBKDF2 with SHA-256 for key
        /// derivation.</remarks>
        /// <param name="password">The password to hash. Cannot be <c>null</c>.</param>
        /// <returns>A Base64-encoded string containing the salt and hash. The output can be stored for later password
        /// verification.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="password"/> is <c>null</c>.</exception>
        public static string Hash(string password)
        {
            if (password is null) throw new ArgumentNullException(nameof(password));

            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash;
            using (var derive = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256))
            {
                hash = derive.GetBytes(32);
            }

            var combined = new byte[salt.Length + hash.Length];
            Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
            Buffer.BlockCopy(hash, 0, combined, salt.Length, hash.Length);
            return Convert.ToBase64String(combined);
        }

        /// <summary>
        /// Verifies whether the specified password matches the stored hash and salt combination.
        /// </summary>
        /// <remarks>The method expects <paramref name="storedCombinedBase64"/> to contain a 16-byte salt
        /// followed by a hash, both encoded as a single Base64 string. If the input is invalid or the password does not
        /// match, the method returns <see langword="false"/>.</remarks>
        /// <param name="providedPassword">The password to verify against the stored hash. Cannot be <see langword="null"/>.</param>
        /// <param name="storedCombinedBase64">A Base64-encoded string containing the combined salt and hash to compare against. Must be in the expected
        /// format; otherwise, verification will fail.</param>
        /// <returns><see langword="true"/> if <paramref name="providedPassword"/> matches the hash and salt in <paramref
        /// name="storedCombinedBase64"/>; otherwise, <see langword="false"/>.</returns>
        public static bool Verify(string providedPassword, string storedCombinedBase64)
        {
            if (string.IsNullOrEmpty(storedCombinedBase64)) return false;
            try
            {
                var combined = Convert.FromBase64String(storedCombinedBase64);
                if (combined.Length < 48) // 16 salt + 32 hash minimum
                    return false;

                var salt = new byte[16];
                Buffer.BlockCopy(combined, 0, salt, 0, 16);
                var hash = new byte[combined.Length - 16];
                Buffer.BlockCopy(combined, 16, hash, 0, hash.Length);

                using var derive = new Rfc2898DeriveBytes(providedPassword, salt, 100_000, HashAlgorithmName.SHA256);
                var computed = derive.GetBytes(hash.Length);

                if (computed.Length != hash.Length) return false;
                int diff = 0;
                for (int i = 0; i < hash.Length; i++) diff |= computed[i] ^ hash[i];
                return diff == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
