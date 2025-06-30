using Hazel;

namespace TOHE.Modules.Rpc
{
    class RpcShowChat : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.ShowChat;

        public RpcShowChat(uint rpcObjectNetId, int ownerId) : base(rpcObjectNetId)
        {
            this.ownerId = ownerId;
        }

        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.WritePacked(ownerId);
            writer.Write(true);
        }

        private readonly int ownerId;
    }
}
