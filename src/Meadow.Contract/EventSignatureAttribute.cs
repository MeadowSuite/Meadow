using System;

namespace Meadow.Contract
{
    public class EventSignatureAttribute : Attribute
    {
        public readonly string Signature;

        public EventSignatureAttribute(string signature)
        {
            Signature = signature;
        }
    }
}

