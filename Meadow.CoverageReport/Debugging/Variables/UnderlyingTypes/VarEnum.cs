using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using Meadow.CoverageReport.AstTypes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Variables.UnderlyingTypes
{
    public class VarEnum : VarBase
    {
        #region Properties
        public AstEnumDefinition EnumDefinition { get; }
        #endregion

        #region Constructors
        public VarEnum(AstUserDefinedTypeName type) : base(type)
        {
            // Set our enum definition
            EnumDefinition = AstParser.GetNode<AstEnumDefinition>(type.ReferencedDeclaration);

            // Determine how many bytes is needed to address the enum. The enum size is 
            // dependent on the amount of bytes needed to index all enum options.
            int enumSize = 0;
            int highestMemberIndex = EnumDefinition.Members.Length - 1;
            while (highestMemberIndex > 0)
            {
                // Add one to our byte count, and shift over.
                highestMemberIndex >>= 8;
                enumSize++;
            }

            // Initialize our bounds
            InitializeBounds(1, enumSize);
        }
        #endregion

        #region Functions
        public override object ParseData(Memory<byte> data)
        {
            // If there is no data, it can be a singly addressed value, so we return the first enum definition member.
            if (data.Length == 0)
            {
                return EnumDefinition.Members.Length > 0 ? EnumDefinition.Members[0].Name : "";
            }
            else
            {
                // Otherwise we parse our enum value from this data.
                BigInteger index = BigIntegerConverter.GetBigInteger(data.Span, false, SizeBytes);

                // Verify our enum index
                if (index >= EnumDefinition.Members.Length)
                {
                    throw new VarResolvingException("Could not resolve enum because value exceeded enum member count.");
                }

                // Our enum index is valid, return our enum member name
                return EnumDefinition.Members[(int)index].Name;
            }
        }
        #endregion
    }
}
