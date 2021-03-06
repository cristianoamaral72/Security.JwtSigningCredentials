using NetDevPack.Security.JwtSigningCredentials.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using NetDevPack.Security.JwtSigningCredentials.Jwks;

namespace NetDevPack.Security.JwtSigningCredentials.Store.EntityFrameworkCore
{
    internal class DatabaseJsonWebKeyStore<TContext> : IJsonWebKeyStore
        where TContext : DbContext, ISecurityKeyContext
    {
        private readonly TContext _context;
        private readonly IOptions<JwksOptions> _options;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<DatabaseJsonWebKeyStore<TContext>> _logger;

        public DatabaseJsonWebKeyStore(TContext context, ILogger<DatabaseJsonWebKeyStore<TContext>> logger, IOptions<JwksOptions> options, IMemoryCache memoryCache)
        {
            _context = context;
            _options = options;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public void Save(SecurityKeyWithPrivate securityParamteres)
        {
            _context.SecurityKeys.Add(securityParamteres);

            _logger.LogInformation($"Saving new SecurityKeyWithPrivate {securityParamteres.Id}", typeof(TContext).Name);
            _context.SaveChanges();
            ClearCache();
        }

        public SecurityKeyWithPrivate GetCurrentKey()
        {
            if (!_memoryCache.TryGetValue(JwkContants.CurrentJwkCache, out SecurityKeyWithPrivate credentials))
            {
                credentials = _context.SecurityKeys.OrderByDescending(d => d.CreationDate).AsNoTracking().FirstOrDefault();
                // Set cache options.
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    // Keep in cache for this time, reset time if accessed.
                    .SetSlidingExpiration(_options.Value.CacheTime);

                _memoryCache.Set(JwkContants.CurrentJwkCache, credentials, cacheEntryOptions);
            }
            // Put logger in a local such that `this` isn't captured.
            return _context.SecurityKeys.OrderByDescending(d => d.CreationDate).AsNoTracking().FirstOrDefault();
        }

        public IReadOnlyCollection<SecurityKeyWithPrivate> Get(int quantity = 5)
        {
            if (!_memoryCache.TryGetValue(JwkContants.JwksCache, out IReadOnlyCollection<SecurityKeyWithPrivate> keys))
            {
                keys = _context.SecurityKeys.OrderByDescending(d => d.CreationDate).Take(quantity).AsNoTracking().ToList().AsReadOnly();
                // Set cache options.
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    // Keep in cache for this time, reset time if accessed.
                    .SetSlidingExpiration(_options.Value.CacheTime);

                _memoryCache.Set(JwkContants.JwksCache, keys, cacheEntryOptions);
            }

            return _context.SecurityKeys.OrderByDescending(d => d.CreationDate).Take(quantity).AsNoTracking().ToList().AsReadOnly();
        }
        public bool NeedsUpdate()
        {
            var current = GetCurrentKey();
            if (current == null)
                return true;

            return current.CreationDate.AddDays(_options.Value.DaysUntilExpire) < DateTime.UtcNow.Date;
        }

        public void Clear()
        {
            foreach (var securityKeyWithPrivate in _context.SecurityKeys)
            {
                _context.SecurityKeys.Remove(securityKeyWithPrivate);
            }

            _context.SaveChanges();
            ClearCache();
        }


        public void Update(SecurityKeyWithPrivate securityKeyWithPrivate)
        {
            _context.SecurityKeys.Update(securityKeyWithPrivate);
            _context.SaveChanges();
            ClearCache();
        }

        private void ClearCache()
        {
            _memoryCache.Remove(JwkContants.JwksCache);
            _memoryCache.Remove(JwkContants.CurrentJwkCache);
        }
    }
}