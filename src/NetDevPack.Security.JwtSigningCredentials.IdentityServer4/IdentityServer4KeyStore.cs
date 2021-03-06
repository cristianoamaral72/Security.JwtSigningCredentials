using IdentityServer4.Models;
using IdentityServer4.Stores;
using NetDevPack.Security.JwtSigningCredentials.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JsonWebKey = Microsoft.IdentityModel.Tokens.JsonWebKey;

namespace NetDevPack.Security.JwtSigningCredentials.IdentityServer4
{
    internal class IdentityServer4KeyStore : IValidationKeysStore, ISigningCredentialStore
    {
        private readonly IJsonWebKeySetService _keyService;
        private readonly IMemoryCache _memoryCache;
        private readonly IOptions<JwksOptions> _options;

        /// <summary>Constructor for IdentityServer4KeyStore.</summary>
        /// <param name="keyService"></param>
        /// <param name="memoryCache"></param>
        /// <param name="options"></param>
        public IdentityServer4KeyStore(IJsonWebKeySetService keyService, IMemoryCache memoryCache, IOptions<JwksOptions> options)
        {
            _keyService = keyService;
            _memoryCache = memoryCache;
            _options = options;
        }

        /// <summary>Returns the current signing key.</summary>
        /// <returns></returns>
        public Task<SigningCredentials> GetSigningCredentialsAsync()
        {
            return Task.FromResult(_keyService.GetCurrent());
        }

        /// <summary>Returns all the validation keys.</summary>
        /// <returns></returns>
        public Task<IEnumerable<SecurityKeyInfo>> GetValidationKeysAsync()
        {
            return Task.FromResult(_keyService.GetLastKeysCredentials(_options.Value.AlgorithmsToKeep).Select(s => new SecurityKeyInfo()
            {
                Key = s,
                SigningAlgorithm = s.Alg
            }));
        }
    }
}