using Hazel;

namespace TOHE.Modules.Rpc
{
    class RpcSetDeathReason : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SetDeathReason;

        public RpcSetDeathReason(uint rpcObjectNetId, byte playerId, PlayerState.DeathReason deathReason) : base(rpcObjectNetId)
        {
            this.playerId = playerId;
            this.deathReason = deathReason;
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
