using Microsoft.Extensions.DependencyInjection;

namespace NetDevPack.Security.JwtSigningCredentials.Tests.Warmups
{
    /// <summary>
    /// 
    /// </summary>
    public class WarmupInMemoryStore : IWarmupTest
    {
        public WarmupInMemoryStore()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();

            serviceCollection.AddJwksManager().PersistKeysInMemory();
            Services = serviceCollection.BuildServiceProvider();
        }
        public ServiceProvider Services { get; set; }
    }
}