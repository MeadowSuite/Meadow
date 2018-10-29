using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.Networking.Protocol.RLPx.Messages
{
    public interface IRLPxMessage
    {
        byte[] Serialize();
        bool Deserialize(byte[] data);
    }
}
