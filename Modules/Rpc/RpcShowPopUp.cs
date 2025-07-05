using Hazel;

namespace TOHE.Modules.Rpc
{
    class RpcShowPopUp : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.ShowPopUp;
        public RpcShowPopUp(uint netId, string message, string title) : base(netId)
        {
            this.message = message;
            this.title = title;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(message);
            msg.Write(title);
        }

        private readonly string message;
        private readonly string title;
    }
}
