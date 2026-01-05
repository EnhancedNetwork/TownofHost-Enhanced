using Hazel;

namespace TOHE.Modules.Rpc
{
    public class RpcSyncAbilityUseLimit : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SyncAbilityUseLimit;

        public RpcSyncAbilityUseLimit(uint rpcObjectNetId, byte playerId, float limit) : base(rpcObjectNetId)
        {
            this.playerId = playerId;
            this.limit = limit;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(playerId);
            msg.Write(limit);
        }

        private readonly byte playerId;
        private readonly float limit;
    }
}
