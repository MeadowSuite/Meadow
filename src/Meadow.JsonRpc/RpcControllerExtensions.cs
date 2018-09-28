using Meadow.Core.Utils;
using System.Linq;
using System.Reflection;

namespace Meadow.JsonRpc
{
    public static class RpcControllerExtensions
    {
        /// <summary>
        /// Returns a list of rpc methods that are not defined in the interface.
        /// </summary>
        public static RpcApiMethod[] GetUndefinedRpcMethods(this IRpcController controller)
        {
            var methodAttrs = controller
                .GetType()
                .GetInterfaces()
                .Concat(new[] { controller.GetType() })
                .Distinct()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Select(m => m.GetCustomAttribute<RpcApiMethodAttribute>(inherit: true))
                .Where(a => a != null)
                .Select(a => a.Method)
                .ToArray();

            var rpcDefs = EnumExtensions.GetValues<RpcApiMethod>();
            var undefined = rpcDefs.Except(methodAttrs).ToArray();
            return undefined;
        }
    }
}
