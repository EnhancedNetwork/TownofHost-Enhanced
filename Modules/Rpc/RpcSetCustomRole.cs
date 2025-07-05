using Hazel;

namespace TOHE.Modules.Rpc
{
    public class RpcSetCustomRole : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SetCustomRole;

        public RpcSetCustomRole(uint rpcObjectNetId, byte playerId, CustomRoles role) : base(rpcObjectNetId)
        {
            this.playerId = playerId;
            this.role = role;
        }

        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.Write(playerId);
            writer.WritePacked((int)role);
        }

        private readonly byte playerId;
        private readonly CustomRoles role;
    }

    public class RpcSyncPlayerSetting : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SyncPlayerSetting;
        public RpcSyncPlayerSetting(uint rpcObjectNetId, byte playerId, CustomRoles role) : base(rpcObjectNetId)
        {
            this.playerId = playerId;
            this.role = role;
        }
        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.Write(playerId);
            writer.WritePacked((int)role);
        }

        private readonly byte playerId;
        private readonly CustomRoles role;
    }
}
