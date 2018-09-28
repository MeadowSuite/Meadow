using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Meadow.JsonRpc.Client.TransportAdapter
{
    public interface ITransportAdapter : IDisposable
    {
        Uri EndPoint { get; }
        Task<JObject> Request(JObject requestObject);
    }
}
