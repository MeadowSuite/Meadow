using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.JsonRpc
{
    // These defaults are not derived or based on anything reasonable specification. They are 
    // not reliable to use in any given situation and can be dependent on implementation, chain, fork, etc.
    // They are only here for use in test projects since this is a commonly referenced project.
    // TODO: Move these into something like a Meadow.TestCommon project
    public static class ArbitraryDefaults
    {
        public const long DEFAULT_GAS_LIMIT = 5_000_000;
        public const long DEFAULT_GAS_PRICE = 100_000_000_000;
       
    }
}
