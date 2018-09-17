using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.Core.RlpEncoding
{
    public interface IRLPSerializable
    {
        RLPItem Serialize();
        void Deserialize(RLPItem item);
    }
}
