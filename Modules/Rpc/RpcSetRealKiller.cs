using Hazel;

namespace TOHE.Modules.Rpc
{
    class RpcSetRealKiller : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SetRealKiller;
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
