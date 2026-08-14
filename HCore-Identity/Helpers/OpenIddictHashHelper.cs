// see https://github.com/openiddict/openiddict-core/blob/dev/src/OpenIddict.Core/Managers/OpenIddictApplicationManager.cs

using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace HCore.Identity.Helpers
{
    public class OpenIddictHashHelper
    {
        public static string HashClientSecret(
            string secret,
            HashAlgorithmName? hashAlgorithm = null,
            int iterations = 100_000,
            int keyBits = 512,
            int saltBits = 256
        )
        {
            ArgumentException.ThrowIfNullOrEmpty(secret);

            var algorithm = hashAlgorithm ?? HashAlgorithmName.SHA512;

            var salt = RandomNumberGenerator.GetBytes(saltBits / 8);
            var key = Rfc2898DeriveBytes.Pbkdf2(secret, salt, iterations, algorithm, keyBits / 8);

            var length = 1 + sizeof(uint) * 3 + salt.Length + key.Length;
            var payload = new byte[length];

            payload[0] = 0x01;

            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(1, sizeof(uint)),
                algorithm == HashAlgorithmName.SHA1 ? 0u :
                algorithm == HashAlgorithmName.SHA256 ? 1u :
                algorithm == HashAlgorithmName.SHA512 ? 2u :
                throw new NotSupportedException("Only SHA1, SHA256 and SHA512 are supported."));

            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(5, sizeof(uint)), (uint)iterations);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(9, sizeof(uint)), (uint)salt.Length);

            salt.CopyTo(payload, 13);

            key.CopyTo(payload, 13 + salt.Length);

            return Convert.ToBase64String(payload);
        }
    }
}
