using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Meadow.Core.Utils
{
    public static class ServerEndpointParser
    {
        public static Uri Parse(string host, int? port = null)
        {
            var networkHost = host;

            if (!networkHost.Contains(":/"))
            { 
                networkHost = "http://" + networkHost;
            }

            if (!Uri.TryCreate(networkHost, UriKind.Absolute, out var hostUri))
            {
                throw new ArgumentException($"Invalid network host / URI specified: '{networkHost}'");
            }

            var uriBuilder = new UriBuilder(hostUri);

            bool portSpecifiedInHost = host.Contains(":" + uriBuilder.Port);

            if (port.GetValueOrDefault() == 0 && !portSpecifiedInHost)
            {
                throw new ArgumentException($"The port is not specified in '{host}'. The default port for protocol '{uriBuilder.Scheme}' is '{uriBuilder.Uri.Port}'.");
            }

            if (port.GetValueOrDefault() != 0)
            {
                if (portSpecifiedInHost)
                {
                    throw new ArgumentException($"A port is specified in both {nameof(host)}={uriBuilder.Port} and {nameof(port)}={port}.");
                }
                else
                {
                    uriBuilder.Port = port.GetValueOrDefault();
                }
            }

            //var result = $"{uriBuilder.Scheme}://{uriBuilder.Host}:{uriBuilder.Port}";
            return uriBuilder.Uri;
        }
    }
}
