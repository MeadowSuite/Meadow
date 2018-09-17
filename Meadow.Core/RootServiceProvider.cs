using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Meadow.Core
{
    /// <summary>
    /// The main static instance for this lib's ServiceProvider.
    /// DI (dependency-injection) is quickly becoming a required component for using new 
    /// Microsoft libs. For example the new HttpClientFactory can only be used with DI.
    /// </summary>
    public static class RootServiceProvider
    {
        static readonly ServiceProvider _serviceProvider;

        public const string RPC_CLIENT_CONFIG = "RPC_CLIENT_CONFIG";

        static RootServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient(RPC_CLIENT_CONFIG, new Action<HttpClient>(ConfigureHttpClient));
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        static void ConfigureHttpClient(HttpClient client)
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        }

        public static IHttpClientFactory GetHttpClientFactory() => _serviceProvider.GetRequiredService<IHttpClientFactory>();

        public static HttpClient GetRpcHttpClient() => GetHttpClientFactory().CreateClient(RPC_CLIENT_CONFIG);
    }
}
