using Hazel;

namespace TOHE.Modules.Rpc
{
    class RpcSetFriendCode : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SetFriendCode;

        public RpcSetFriendCode(uint rpcObjectNetId, string fc) : base(rpcObjectNetId)
        {
            this.fc = fc;
        }

        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.Write(fc);
        }

        private readonly string fc;
    }
}
