// based on https://github.com/dotnet/aspnetcore/blob/eec80e1c63963802a215d58b3907d6e46090fc28/src/Identity/Extensions.Core/src/PasswordHasher.cs#L18

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace HCore.Identity.Security
{
    public interface INonRandomPasswordHasher
    {
        /// <summary>
        /// Returns a hashed representation of the supplied <paramref name="password"/> for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose password is to be hashed.</param>
        /// <param name="password">The password to hash.</param>
        /// <returns>A hashed representation of the supplied <paramref name="password"/> for the specified <paramref name="user"/>.</returns>
        string HashPassword(string password, string salt);
    }
}
