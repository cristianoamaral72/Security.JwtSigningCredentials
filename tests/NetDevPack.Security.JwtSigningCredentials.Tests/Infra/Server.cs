﻿using System;
using NetDevPack.Security.JwtSigningCredentials.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace NetDevPack.Security.JwtSigningCredentials.Tests.Infra
{
    public class Server
    {
        public TestServer CreateServer(bool useCache = true)
        {
            return new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddJwksManager();
                    if (useCache)
                        services.AddMemoryCache();

                })
                .Configure(app =>
                {
                    app.UseJwksDiscovery();
                    app.Map(new PathString("/renew"), x => x.UseMiddleware<JwkRenewMiddleware>());
                }));
        }

        public HttpClient CreateClient(bool useCache = true)
        {
            return CreateServer(useCache).CreateClient();
        }
    }
}
