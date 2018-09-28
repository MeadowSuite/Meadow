using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.EVM.Configuration
{
    // Source: https://github.com/ethereum/wiki/wiki/Releases

    /// <summary>
    /// Indicates all releases/forks of Ethereum.
    /// </summary>
    public enum EthereumRelease
    {
        /// <summary>
        /// (1.0) Ethereum first production-net release (still considered beta) (7/30/2015)
        /// </summary>
        Frontier,
        /// <summary>
        /// (2.0) Ethereum revision which marked the exit from a beta to a stable release (3/14/2016)
        /// </summary>
        Homestead,
        /// <summary>
        /// (2.1) Ethereum revision which corrected the DAO hack.
        /// </summary>
        DAO,
        /// <summary>
        /// (2.2) Ethereum revision also known as Anti-DoS, introduced new gas rules and secure trie access to avoid targetted resource exhaustion.
        /// </summary>
        TangerineWhistle,
        /// <summary>
        /// (2.3) Ethereum revision also known as State-clearing.
        /// </summary>
        SpuriousDragon,
        /// <summary>
        /// (3.0) Ethereum revision (phase 1) of Metropolis release. Introduces various privacy and functionality features. (This is often refered to as just Metropolis).
        /// </summary>
        Byzantium,
        /// <summary>
        /// (Work in progress) (3.1) Ethereum revision (phase 2) of Metropolis release.
        /// </summary>
        WIP_Constantinople,
        /// <summary>
        /// (Work in progress) (4.0) Ethereum revision meant to move from Proof-of-Work consensus to Proof-of-Stake consensus.
        /// </summary>
        WIP_Serenity,
    }
}
