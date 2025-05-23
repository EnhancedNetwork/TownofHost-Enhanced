using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOHE.Modules.Rpc
{
    class RpcSetRealKiller : BaseModdedRpc
    {
        public override CustomRPC RpcType => CustomRPC.SetRealKiller;
        public RpcSetRealKiller(uint rpcObjectNetId, byte playerId, byte killerId) : base(rpcObjectNetId)
        {
            this.playerId = playerId;
            this.killerId = killerId;
        }
        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.Write(playerId);
            writer.Write(killerId);
        }

        private readonly byte playerId;
        private readonly byte killerId;
    }
}
