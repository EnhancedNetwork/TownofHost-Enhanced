using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOHE.Modules.Rpc
{
    class RpcSetDeathReason : BaseModdedRpc
    {
        public override CustomRPC RpcType => CustomRPC.SetDeathReason;

        public RpcSetDeathReason(uint rpcObjectNetId, byte playerId, PlayerState.DeathReason deathReason) : base(rpcObjectNetId)
        {

        }

        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.Write(playerId);
            writer.Write((int)deathReason);
        }

        private readonly byte playerId;
        private readonly PlayerState.DeathReason deathReason;
    }
}
