using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Meadow.Networking.Extensions
{
    public static class IPAddressExtensions
    {
        #region Functions
        /// <summary>
        /// Obtains the IP address as a string, compatible for use in a URI (IPv6 addresses enclosed in square brackets).
        /// Reference: https://tools.ietf.org/html/rfc2732
        /// </summary>
        /// <param name="address">The IP address to format as a string.</param>
        /// <returns>Returns the IP address as a string, compatible for use in a Uri.</returns>
        public static string ToUriCompatibleString(this IPAddress address)
        {
            return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? $"[{address}]" : address.ToString();
        }
        #endregion
    }
}
