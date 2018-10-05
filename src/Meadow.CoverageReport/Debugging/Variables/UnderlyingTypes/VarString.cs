using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using Meadow.CoverageReport.AstTypes;
using Meadow.CoverageReport.Debugging.Variables.Enums;
using Meadow.CoverageReport.Debugging.Variables.Storage;
using Meadow.JsonRpc.Client;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Variables.UnderlyingTypes
{
    public class VarString : VarDynamicBytes
    {
        #region Constructors
        public VarString(AstElementaryTypeName type, VarLocation location) : base(type, location)
        {
        }
        #endregion

        #region Functions
        public override object ParseDereferencedFromMemory(Memory<byte> memory, int offset)
        {
            // Obtain our value as we would dynamic bytes, but as a UTF-8 string
            Memory<byte> value = (Memory<byte>)base.ParseDereferencedFromMemory(memory, offset);

            // Obtain a string value
            string result = Encoding.UTF8.GetString(value.ToArray());

            // Return our result
            return result;
        }

        public override object ParseFromStorage(StorageManager storageManager, StorageLocation storageLocation, IJsonRpcClient rpcClient = null)
        {
            // Obtain our value as we would dynamic bytes, but as a UTF-8 string
            Memory<byte> value = (Memory<byte>)base.ParseFromStorage(storageManager, storageLocation, rpcClient);

            // Obtain a string value
            string result = Encoding.UTF8.GetString(value.ToArray());

            // Return our result
            return result;
        }
        #endregion
    }
}
