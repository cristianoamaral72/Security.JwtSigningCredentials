using NetDevPack.Security.JwtSigningCredentials;
using NetDevPack.Security.JwtSigningCredentials.Interfaces;
using NetDevPack.Security.JwtSigningCredentials.Store.FileSystem;
using Microsoft.Extensions.Options;
using System.IO;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Builder extension methods for registering crypto services
    /// </summary>
    public static class FileSystemStoreSigningCredentialsExtensions
    {
        /// <summary>
        /// Sets the signing credential.
        /// </summary>
        /// <returns></returns>
        public static IJwksBuilder PersistKeysToFileSystem(this IJwksBuilder builder, DirectoryInfo directory)
        {
            
            builder.Services.AddScoped<IJsonWebKeyStore, FileSystemStore>(provider => new FileSystemStore(directory, provider.GetService<IOptions<JwksOptions>>(), provider.GetService<IMemoryCache>()));

            return builder;
        }
    }
}