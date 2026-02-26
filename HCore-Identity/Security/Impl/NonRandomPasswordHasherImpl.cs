// based on https://github.com/dotnet/aspnetcore/blob/eec80e1c63963802a215d58b3907d6e46090fc28/src/Identity/Extensions.Core/src/PasswordHasher.cs#L18

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HCore.Identity.Security;

public class NonRandomPasswordHasherImpl : INonRandomPasswordHasher
{
    /* =======================
     * HASHED PASSWORD FORMATS
     * =======================
     *
     * Version 3:
     * PBKDF2 with HMAC-SHA512, 128-bit salt, 256-bit subkey, 100000 iterations.
     * Format: { 0x01, prf (UInt32), iter count (UInt32), salt length (UInt32), salt, subkey }
     * (All UInt32s are stored big-endian.)
     */

    private readonly int _iterCount;

    private static readonly PasswordHasherOptions DefaultOptions = new PasswordHasherOptions();

    /// <summary>
    /// Creates a new instance of <see cref="PasswordHasher{TUser}"/>.
    /// </summary>
    /// <param name="optionsAccessor">The options for this instance.</param>
    public NonRandomPasswordHasherImpl(IOptions<PasswordHasherOptions>? optionsAccessor = null)
    {
        var options = optionsAccessor?.Value ?? DefaultOptions;

        _iterCount = options.IterationCount;
        if (_iterCount < 1)
        {
            throw new InvalidOperationException("Invalid password hasher iteration count");
        }
    }

    /// <summary>
    /// Returns a hashed representation of the supplied <paramref name="password"/> for the specified <paramref name="user"/>.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <param name="salt">The salt to be used.</param>
    /// <returns>A hashed representation of the supplied <paramref name="password"/> for the specified <paramref name="user"/>.</returns>
    public virtual string HashPassword(string password, string salt)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentNullException(nameof(password));
        }

        return Convert.ToBase64String(HashPasswordV3(password, salt));
    }

    private byte[] HashPasswordV3(string password, string salt)
    {
        return HashPasswordV3(password, 
            prf: KeyDerivationPrf.HMACSHA512,
            iterCount: _iterCount,
            saltSize: 128 / 8,
            numBytesRequested: 256 / 8,
            salt);
    }

    private static byte[] HashPasswordV3(string password, KeyDerivationPrf prf, int iterCount, int saltSize, int numBytesRequested, string salt)
    {
        byte[] saltBytes = Encoding.UTF8.GetBytes(salt);
        byte[] subkey = KeyDerivation.Pbkdf2(password, saltBytes, prf, iterCount, numBytesRequested);

        var outputBytes = new byte[13 + salt.Length + subkey.Length];
        outputBytes[0] = 0x01; // format marker
        WriteNetworkByteOrder(outputBytes, 1, (uint)prf);
        WriteNetworkByteOrder(outputBytes, 5, (uint)iterCount);
        WriteNetworkByteOrder(outputBytes, 9, (uint)saltSize);
        Buffer.BlockCopy(saltBytes, 0, outputBytes, 13, salt.Length);
        Buffer.BlockCopy(subkey, 0, outputBytes, 13 + saltSize, subkey.Length);
        return outputBytes;
    }

    private static void WriteNetworkByteOrder(byte[] buffer, int offset, uint value)
    {
        buffer[offset + 0] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)(value >> 0);
    }
}