// Copyright (c) 2018 KevDever. All rights reserved.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

// Adjusted for HCore by Reinhard Holzner

using HCore.Identity.Database.SqlServer;
using HCore.Identity.Database.SqlServer.Models.Impl;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Core.Identity.Database.SqlServer.Repositories
{
    /// <summary>
    /// This implementation of the IXmlRepository is modeled on the built-in RedisXmlRepository: https://github.com/aspnet/DataProtection/blob/master/src/Microsoft.AspNetCore.DataProtection.Redis/RedisXmlRepository.cs
    /// </summary>
    public class DataProtectionKeyRepository : IXmlRepository
    {
        private readonly IServiceProvider _serviceProvider;

        public DataProtectionKeyRepository(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            var serviceScopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

            using (var scope = serviceScopeFactory.CreateScope())
            {
                using (var context = scope.ServiceProvider.GetRequiredService<SqlServerIdentityDbContext>())
                {
                    context.Database.Migrate();
                }
            }
        }

        public IReadOnlyCollection<XElement> GetAllElements() => GetAllElementsCore().ToList().AsReadOnly();

        /// <summary>
        /// See the comments in the Redis file mentioned above about failure modes.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<XElement> GetAllElementsCore()
        {
            var serviceScopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

            using (var scope = serviceScopeFactory.CreateScope())
            {
                using (var context = scope.ServiceProvider.GetRequiredService<SqlServerIdentityDbContext>())
                {
                    foreach (var element in context.DataProtectionKeys)
                        yield return XElement.Parse(element.XmlData);
                }
            }            
        }

        /// <summary>
        /// If a key is already in the database, then update it; else, add it.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="friendlyName"></param>
        public void StoreElement(XElement element, string friendlyName)
        {
            var serviceScopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

            using (var scope = serviceScopeFactory.CreateScope())
            {
                using (var context = scope.ServiceProvider.GetRequiredService<SqlServerIdentityDbContext>())
                {
                    var entity = context.DataProtectionKeys.Find(friendlyName);
                    if (entity is null)
                    {
                        entity = new DataProtectionKeyModel
                        {
                            FriendlyName = friendlyName,
                        };

                        context.DataProtectionKeys.Add(entity);
                    }

                    entity.XmlData = element.ToString(SaveOptions.DisableFormatting);
                    context.SaveChanges();
                }
            }
        }
    }
}
