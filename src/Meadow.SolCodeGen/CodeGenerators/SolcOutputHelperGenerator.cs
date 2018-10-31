using Meadow.Contract;
using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.SolCodeGen.CodeGenerators
{
    class SolcOutputHelperGenerator : GeneratorBase
    {
        readonly byte[] _codebaseHash;

        public SolcOutputHelperGenerator(byte[] codebaseHash, string @namespace) : base(@namespace)
        {
            _codebaseHash = codebaseHash;
        }

        protected override string GenerateUsingDeclarations()
        {
            var hashHex = HexUtil.GetHexFromBytes(_codebaseHash);
            var sourceAttrTypeName = typeof(GeneratedSolcDataAttribute).FullName;
            var assemblyAttr = $"[assembly: {sourceAttrTypeName}(\"{hashHex}\")]";
            return assemblyAttr;
        }

        protected override string GenerateClassDef()
        {
            return string.Empty;
        }

    }
}
