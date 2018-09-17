using Meadow.EVM.Data_Types.Block;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.EVM.Data_Types.Chain
{
    public abstract class ConsensusBase
    {
        public abstract void Initialize(State.State state, Block.Block block);
        public abstract bool CheckProof(State.State state, BlockHeader blockHeader);
        public abstract List<BlockHeader> GetUncleCandidates(Chain.PoW.ChainPoW chain, State.State state);
        public abstract bool VerifyUncles(State.State state, Block.Block block);
        public abstract void Finalize(State.State state, Block.Block block);
    }
}
