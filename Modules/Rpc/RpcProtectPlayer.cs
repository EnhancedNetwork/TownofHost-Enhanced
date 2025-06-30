using Hazel;

namespace TOHE.Modules.Rpc
{
    public class RpcProtectPlayer : BaseModdedRpc
    {
        public override byte RpcType => (byte)RpcCalls.ProtectPlayer;

        public RpcProtectPlayer(uint rpcObjectNetId, uint targetNetId, int colorId) : base(rpcObjectNetId)
        {
            this.targetNetId = targetNetId;
            this.colorId = colorId;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.WritePacked(targetNetId);
            msg.Write(colorId);
        }

        private readonly uint targetNetId;
        private readonly int colorId;
    }

}
