using Hazel;

namespace TOHE.Modules.Rpc
{
    class RpcSyncSpeed : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SyncSpeedPlayer;

        public RpcSyncSpeed(uint rpcObjectNetId, byte playerId, float speed) : base(rpcObjectNetId)
        {
            this.playerId = playerId;
            this.speed = speed;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(playerId);
            msg.Write(speed);
        }

        private readonly byte playerId;
        private readonly float speed;
    }
}
