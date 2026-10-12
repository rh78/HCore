using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace HCore.Web.Providers.Impl
{
    public class AesGcmHttpsCertificateEncryptionProviderImpl : IHttpsCertificateEncryptionProvider
    {
        private const int SaltSize = 16;
        private const int NonceSize = 12;
        private const int TagSize = 16;

        private const int SaltOffset = 0;
        private const int NonceOffset = SaltOffset + SaltSize;
        private const int TagOffset = NonceOffset + NonceSize;
        private const int HeaderSize = TagOffset + TagSize;

        // AES-256

        private const int KeySize = 32;

        // OWASP PBKDF2-HMAC-SHA256 recommendation

        private const int Iterations = 600_000;

        private readonly byte[] _masterKey;

        public AesGcmHttpsCertificateEncryptionProviderImpl(IConfiguration configuration)
        {
            var password = configuration["Identity:Encryption:Password"];

            if (string.IsNullOrEmpty(password))
            {
                throw new Exception("Identity encryption password is not set");
            }

            var salt = configuration["Identity:Encryption:Salt"];

            if (string.IsNullOrEmpty(salt))
            {
                throw new Exception("Identity encryption salt is not set");
            }

            var saltBytes = Encoding.UTF8.GetBytes(salt);

            _masterKey = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, KeySize);
        }

        public string EncryptHttpsCertificate(byte[] httpsCertificateBytes)
        {
            ArgumentNullException.ThrowIfNull(httpsCertificateBytes);

            var data = Encrypt(httpsCertificateBytes);

            return Convert.ToBase64String(data);
        }

        public string EncryptHttpsCertificatePassword(string httpsCertificatePassword)
        {
            ArgumentException.ThrowIfNullOrEmpty(httpsCertificatePassword);

            var passwordBytes = Encoding.UTF8.GetBytes(httpsCertificatePassword);

            var data = Encrypt(passwordBytes);

            return Convert.ToBase64String(data);
        }

        private byte[] Encrypt(byte[] data)
        {
            var encryptedData = new byte[HeaderSize + data.Length];

            var salt = encryptedData.AsSpan(SaltOffset, SaltSize);
            var nonce = encryptedData.AsSpan(NonceOffset, NonceSize);
            var tag = encryptedData.AsSpan(TagOffset, TagSize);
            var ciphertext = encryptedData.AsSpan(HeaderSize);

            RandomNumberGenerator.Fill(salt);
            RandomNumberGenerator.Fill(nonce);

            Span<byte> key = stackalloc byte[KeySize];

            try
            {
                DeriveKey(salt, key);

                using var aesGcm = new AesGcm(key, TagSize);

                aesGcm.Encrypt(nonce, data, ciphertext, tag);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }

            return encryptedData;
        }

        private void DeriveKey(ReadOnlySpan<byte> salt, Span<byte> key)
        {
            HKDF.DeriveKey(HashAlgorithmName.SHA256, _masterKey, key, salt, info: default);
        }

        public byte[] DecryptHttpsCertificate(string encryptedHttpsCertificate)
        {
            ArgumentException.ThrowIfNullOrEmpty(encryptedHttpsCertificate);

            var data = Convert.FromBase64String(encryptedHttpsCertificate);

            return Decrypt(data);
        }

        public string DecryptHttpsCertificatePassword(string encryptedHttpsCertificatePassword)
        {
            ArgumentException.ThrowIfNullOrEmpty(encryptedHttpsCertificatePassword);

            var data = Convert.FromBase64String(encryptedHttpsCertificatePassword);

            var passwordBytes = Decrypt(data);

            return Encoding.UTF8.GetString(passwordBytes);
        }

        private byte[] Decrypt(byte[] data)
        {
            if (data.Length < HeaderSize)
            {
                throw new CryptographicException("Encrypted data is malformed");
            }

            var salt = data.AsSpan(SaltOffset, SaltSize);
            var nonce = data.AsSpan(NonceOffset, NonceSize);
            var tag = data.AsSpan(TagOffset, TagSize);
            var ciphertext = data.AsSpan(HeaderSize);

            var decryptedData = new byte[ciphertext.Length];

            Span<byte> key = stackalloc byte[KeySize];

            try
            {
                DeriveKey(salt, key);

                using var aesGcm = new AesGcm(key, TagSize);

                aesGcm.Decrypt(nonce, ciphertext, tag, decryptedData);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }

            return decryptedData;
        }
    }
}
