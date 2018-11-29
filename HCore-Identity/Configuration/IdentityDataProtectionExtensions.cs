// Copyright (c) 2018 KevDever. All rights reserved.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

// Adjusted for HCore by Reinhard Holzner

using Core.Identity.Database.SqlServer.Repositories;
using HCore.Identity.Database.SqlServer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.AspNetCore.DataProtection
{
    public static class IdentityDataProtectionExtensions
    {
        public static IDataProtectionBuilder PersistKeysToSqlDatabase(this IDataProtectionBuilder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(serviceProvider =>
            {
                return new ConfigureOptions<KeyManagementOptions>(options =>
                {
                    options.XmlRepository = new DataProtectionKeyRepository(serviceProvider);
                });
            });

            return builder;
        }
    }
}
