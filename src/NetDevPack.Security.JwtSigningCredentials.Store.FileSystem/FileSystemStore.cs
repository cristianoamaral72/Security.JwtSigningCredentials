using NetDevPack.Security.JwtSigningCredentials.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using NetDevPack.Security.JwtSigningCredentials.Jwks;

namespace NetDevPack.Security.JwtSigningCredentials.Store.FileSystem
{
    public class FileSystemStore : IJsonWebKeyStore
    {
        private readonly IOptions<JwksOptions> _options;
        private readonly IMemoryCache _memoryCache;
        public DirectoryInfo KeysPath { get; }

        public FileSystemStore(DirectoryInfo keysPath, IOptions<JwksOptions> options, IMemoryCache memoryCache)
        {
            _options = options;
            _memoryCache = memoryCache;
            KeysPath = keysPath;
        }

        private string GetCurrentFile()
        {
            return Path.Combine(KeysPath.FullName, $"{_options.Value.KeyPrefix}current.key");
        }

        public void Save(SecurityKeyWithPrivate securityParamteres)
        {
            if (!KeysPath.Exists)
                KeysPath.Create();

            // Datetime it's just to be easy searchable.
            if (File.Exists(GetCurrentFile()))
                File.Copy(GetCurrentFile(), Path.Combine(Path.GetDirectoryName(GetCurrentFile()), $"{_options.Value.KeyPrefix}old-{DateTime.Now:yyyy-MM-dd}-{Guid.NewGuid()}.key"));

            File.WriteAllText(GetCurrentFile(), JsonSerializer.Serialize(securityParamteres, new JsonSerializerOptions() { IgnoreNullValues = true }));
            ClearCache();
        }

        public bool NeedsUpdate()
        {
            return !File.Exists(GetCurrentFile()) || File.GetCreationTimeUtc(GetCurrentFile()).AddDays(_options.Value.DaysUntilExpire) < DateTime.UtcNow.Date;
        }

        public void Update(SecurityKeyWithPrivate securityKeyWithPrivate)
        {
            foreach (var fileInfo in KeysPath.GetFiles("*.key"))
            {
                var key = GetKey(fileInfo.FullName);
                if (key.Id != securityKeyWithPrivate.Id) continue;

                File.WriteAllText(fileInfo.FullName, JsonSerializer.Serialize(securityKeyWithPrivate, new JsonSerializerOptions() { IgnoreNullValues = true }));
                break;
            }
            ClearCache();
        }


        public SecurityKeyWithPrivate GetCurrentKey()
        {
            if (!_memoryCache.TryGetValue(JwkContants.CurrentJwkCache, out SecurityKeyWithPrivate credentials))
            {
                credentials = GetKey(GetCurrentFile());
                // Set cache options.
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    // Keep in cache for this time, reset time if accessed.
                    .SetSlidingExpiration(_options.Value.CacheTime);

                _memoryCache.Set(JwkContants.CurrentJwkCache, credentials, cacheEntryOptions);
            }

            return credentials;
        }

        private SecurityKeyWithPrivate GetKey(string file)
        {
            if (!File.Exists(file)) throw new FileNotFoundException("Check configuration - cannot find auth key file: " + file);
            var keyParams = JsonSerializer.Deserialize<SecurityKeyWithPrivate>(File.ReadAllText(file));
            return keyParams;

        }

        public IReadOnlyCollection<SecurityKeyWithPrivate> Get(int quantity = 5)
        {
            if (!_memoryCache.TryGetValue(JwkContants.JwksCache, out IReadOnlyCollection<SecurityKeyWithPrivate> keys))
            {
                keys = KeysPath.GetFiles("*.key")
                    .Take(quantity)
                    .Select(s => s.FullName)
                    .Select(GetKey).ToList().AsReadOnly();

                // Set cache options.
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    // Keep in cache for this time, reset time if accessed.
                    .SetSlidingExpiration(_options.Value.CacheTime);

                _memoryCache.Set(JwkContants.JwksCache, keys, cacheEntryOptions);
            }

            return keys;
        }

        public void Clear()
        {
            if (KeysPath.Exists)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                foreach (var fileInfo in KeysPath.GetFiles($"*.key"))
                {
                    fileInfo.Delete();
                }
            }
        }


        private void ClearCache()
        {
            _memoryCache.Remove(JwkContants.JwksCache);
            _memoryCache.Remove(JwkContants.CurrentJwkCache);
        }
    }
}
