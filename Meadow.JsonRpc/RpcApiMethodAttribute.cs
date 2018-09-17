using System;

namespace Meadow.JsonRpc
{
    public class RpcApiMethodAttribute : Attribute
    {
        public readonly RpcApiMethod Method;

        public RpcApiMethodAttribute(RpcApiMethod method)
        {
            Method = method;
        }

        public RpcApiMethodAttribute(string method)
        {
            Method = RpcApiMethods.Create(method);
        }
    }
}
