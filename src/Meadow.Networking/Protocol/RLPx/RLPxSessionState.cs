using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.Networking.Protocol.RLPx
{
    /// <summary>
    /// Indicates the state of the current RLPx session.
    /// </summary>
    public enum RLPxSessionState
    {
        Initial,
        AuthenticationCompleted,
        AcknowledgementCompleted,
        EstablishedEncryption
    }
}
