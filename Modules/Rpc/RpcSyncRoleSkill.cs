using Hazel;

namespace TOHE.Modules.Rpc
{
    public class RpcSyncRoleSkill : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SyncRoleSkill;
        public RpcSyncRoleSkill(uint rpcObjectNetId, uint player, MessageWriter writer) : base(rpcObjectNetId)
        {
            this.player = player;
            this.writer = writer;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.WritePacked(player);
            msg.Write(writer, false);

            writer.Recycle();
            // There are chances that the writer can't be recycled. May cause memory leak.
        }

        private readonly uint player;
        private readonly MessageWriter writer;
    }
}
