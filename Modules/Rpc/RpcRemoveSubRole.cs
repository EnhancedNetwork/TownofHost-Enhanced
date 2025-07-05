using Hazel;

namespace TOHE.Modules.Rpc
{
    class RpcRemoveSubRole : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.RemoveSubRole;

        public RpcRemoveSubRole(uint rpcObjectNetId, byte playerId, CustomRoles addon) : base(rpcObjectNetId)
        {
            this.playerId = playerId;
            this.addon = addon;
        }

        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.Write(playerId);
            writer.Write((int)addon);
        }

        private readonly byte playerId;
        private readonly CustomRoles addon;
    }
}
