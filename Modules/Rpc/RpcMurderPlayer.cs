using Hazel;

namespace TOHE.Modules.Rpc
{
    public class RpcMurderPlayer : BaseModdedRpc
    {
        public override byte RpcType => (byte)RpcCalls.MurderPlayer;
        public RpcMurderPlayer(uint rpcObjectNetId, uint targetNetId, MurderResultFlags flags) : base(rpcObjectNetId)
        {
            this.targetNetId = targetNetId;
            this.flags = flags;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.WritePacked(targetNetId);
            msg.Write((int)flags);
        }

        private readonly uint targetNetId;
        private readonly MurderResultFlags flags;
    }

    public class RpcExiled(uint rpcObjectNetId) : BaseModdedRpc(rpcObjectNetId)
    {
        public override byte RpcType => (byte)RpcCalls.Exiled;

        public override void SerializeRpcValues(MessageWriter msg)
        {
        }
    }
}
