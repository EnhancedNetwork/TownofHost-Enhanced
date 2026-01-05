using Hazel;

namespace TOHE.Modules.Rpc
{
    class RpcAntiBlackout : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.AntiBlackout;

        public RpcAntiBlackout(uint rpcObjectNetId, byte playerId, string reason, string sourseError) : base(rpcObjectNetId)
        {
            this.playerId = playerId;
            this.reason = reason;
            this.sourseError = sourseError;
        }

        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.Write(playerId);
            writer.Write(reason);
            writer.Write(sourseError);
        }

        private readonly byte playerId;
        private readonly string reason;
        private readonly string sourseError;
    }
}
