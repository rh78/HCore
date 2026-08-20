// see https://github.com/openiddict/openiddict-core/blob/dev/src/OpenIddict.Core/Managers/OpenIddictApplicationManager.cs

using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace HCore.Identity.Helpers
{
    public class OpenIddictHashHelper
    {
        public static string HashClientSecret(string secret)
        {
            const int Iterations = 10_000;
            var algorithm = HashAlgorithmName.SHA256;

            var salt = RandomNumberGenerator.GetBytes(128 / 8);
            var key = Rfc2898DeriveBytes.Pbkdf2(secret, salt, Iterations, algorithm, 256 / 8);

            var payload = new byte[13 + salt.Length + key.Length];

            payload[0] = 0x01;                                                // format marker
            
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(1, 4), 1u);  // 0=SHA1, 1=SHA256, 2=SHA512
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(5, 4), (uint)Iterations);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(9, 4), (uint)salt.Length);

            salt.CopyTo(payload, 13);
            key.CopyTo(payload, 13 + salt.Length);

            return Convert.ToBase64String(payload);
        }
    }
}
