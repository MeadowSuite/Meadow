using Meadow.JsonRpc.Types;
using JsonDiffPatchDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using Meadow.Core.Utils;

namespace Meadow.JsonRpc.Test
{
    public class RpcSerialization
    {
        [Fact]
        public void BlockObject()
        {
            var blockJson = File.ReadAllText("TestData/rpc_block_object.json");
            var block = JsonConvert.DeserializeObject<Block>(blockJson, new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });
            var toJson = JsonConvert.SerializeObject(block, Formatting.Indented);

            var jdp = new JsonDiffPatch();
            var diff = JObject.Parse(jdp.Diff(blockJson, toJson));
            var diffStr = diff.ToString(Formatting.Indented);
            foreach (var item in diff)
            {
                var vals = item.Value.Values();
                var b1 = HexUtil.HexToBytes(vals[0].ToString()).ToHexString(hexPrefix: true);
                var b2 = HexUtil.HexToBytes(vals[1].ToString()).ToHexString(hexPrefix: true);
                Assert.Equal(b1, b2);
            }
        }

    }
}
