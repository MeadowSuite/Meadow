using Meadow.Contract;
using Meadow.Core.EthTypes;
using Meadow.JsonRpc.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Meadow.UnitTestTemplate.Test
{

    public class UInt256DataAttribute : Attribute, ITestDataSource
    {
        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            return GetInts().Select(n => new object[] { n.a, n.b }).ToArray();
        }

        public IEnumerable<(UInt256 a, UInt256 b)> GetInts()
        {
            yield return (0, 0);
            yield return (1, 1);
            yield return (0, 1);
            yield return (1, 0);
            yield return (0, UInt256.MaxValue);
            yield return (UInt256.MaxValue, 0);
            yield return (UInt256.MaxValue, UInt256.MaxValue);
            yield return (UInt256.MaxValue, 22);
            yield return (UInt256.MaxValue, 32);
            yield return (UInt256.MaxValue, 1);
            yield return (32, 32);
            yield return (32, 0);
            yield return (12345, 12345);
            yield return (ulong.MaxValue, 12345);
            yield return (12345, ulong.MaxValue);
        }

        public string GetDisplayName(MethodInfo methodInfo, object[] data)
        {
            return $"{methodInfo.Name} ({string.Join(", ", data)})";
        }
    }

    [TestClass]
    public class BitShiftingTests : ContractTest
    {
        protected BitShifting _contract;

        protected override async Task BeforeEach()
        {
            _contract = await BitShifting.New(RpcClient);
        }


        [TestMethod]
        [UInt256Data]
        public async Task BitwiseAnd(UInt256 a, UInt256 b)
        {
            UInt256 sol = await _contract.bitwiseAnd(a, b).Call();
            UInt256 res = a & b;
            Assert.AreEqual(sol, res);
        }

        [TestMethod]
        [UInt256Data]
        public async Task BitwiseOr(UInt256 a, UInt256 b)
        {
            UInt256 sol = await _contract.bitwiseOr(a, b).Call();
            UInt256 res = a | b;
            Assert.AreEqual(sol, res);
        }

        [TestMethod]
        [UInt256Data]
        public async Task BitwiseXor(UInt256 a, UInt256 b)
        {
            UInt256 sol = await _contract.bitwiseXor(a, b).Call();
            UInt256 res = a ^ b;
            Assert.AreEqual(sol, res);
        }

        [TestMethod]
        [UInt256Data]
        public async Task BitwiseNegation(UInt256 a, UInt256 b)
        {
            UInt256 sol = await _contract.bitwiseNegation(a).Call();
            UInt256 res = ~a;
            Assert.AreEqual(sol, res);
        }

        [TestMethod]
        [UInt256Data]
        public async Task BitwiseLeftShift(UInt256 a, UInt256 b)
        {
            if (b > int.MaxValue)
            {
                return;
            }

            UInt256 sol = await _contract.bitwiseLeftShift(a, b).Call();
            UInt256 res = a << (int)b;
            Assert.AreEqual(sol, res);
        }

        [TestMethod]
        [UInt256Data]
        public async Task BitwiseRightShift(UInt256 a, UInt256 b)
        {
            if (b > int.MaxValue)
            {
                return;
            }

            UInt256 sol = await _contract.bitwiseRightShift(a, b).Call();
            UInt256 res = a >> (int)b;
            Assert.AreEqual(sol, res);
        }

        
    }

    
}
