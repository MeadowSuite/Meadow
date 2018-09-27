using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Meadow.Core;
using Newtonsoft.Json.Linq;

namespace Meadow.JsonRpc.Client.TransportAdapter
{
    public class HttpTransportAdapter : ITransportAdapter
    {
        public Uri EndPoint { get; protected set; }

        TimeSpan _connectTimeout = default;

        public HttpTransportAdapter(Uri endPoint, TimeSpan connectTimeout = default)
        {
            _connectTimeout = connectTimeout;
            EndPoint = endPoint;
        }

        public async Task<JObject> Request(JObject requestObject)
        {
            var msgJson = requestObject.ToString();
            var payload = new StringContent(msgJson, Encoding.UTF8, "application/json");
            var requestMsg = new HttpRequestMessage(HttpMethod.Post, EndPoint)
            {
                Content = payload
            };

            var httpClient = RootServiceProvider.GetRpcHttpClient();
            var response = await httpClient.SendAsync(requestMsg);
            var responseBody = await response.Content.ReadAsStringAsync();

            JObject jObj;
            try
            {
                jObj = JObject.Parse(responseBody);
            }
            catch
            {
                response.EnsureSuccessStatusCode();
                throw;
            }

            return jObj;
        }

        public void Dispose()
        {
            
        }
    }
}
