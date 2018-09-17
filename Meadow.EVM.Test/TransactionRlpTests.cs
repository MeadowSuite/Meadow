using Meadow.Core.RlpEncoding;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Xunit;

namespace Meadow.EVM.Test
{
    public class TransactionRlpTests
    {
        [Fact]
        public void RLPLogTest()
        {
            Meadow.EVM.Data_Types.Transactions.Log log = new Meadow.EVM.Data_Types.Transactions.Log((BigInteger)0x11223344, new List<BigInteger>() { 3, 2, 1 }, new byte[] { 00, 77, 00, 77 });
            RLPItem item = log.Serialize();
            byte[] data = RLP.Encode(item);
            item = RLP.Decode(data);
            log.Deserialize(item);
            item = log.Serialize();
            byte[] data2 = RLP.Encode(item);
            Assert.True(data.ValuesEqual(data2));
            Assert.Equal(0x11223344, (BigInteger)log.Address);
            Assert.True(log.Topics.Count == 3 && log.Topics[0] == 3 && log.Topics[1] == 2 && log.Topics[2] == 1);
            Assert.True(log.Data.ValuesEqual(new byte[] { 00, 77, 00, 77 }));
        }

    }
}
