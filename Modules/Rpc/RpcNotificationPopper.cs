using Hazel;

namespace TOHE.Modules.Rpc
{
    class RpcNotificationPopper : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.NotificationPopper;
        public RpcNotificationPopper(uint netId, int index, bool playSound) : base(netId)
        {
            this.index = index;
            this.playSound = playSound;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.WritePacked(index);
            msg.Write(playSound);
        }

        private readonly int index;
        private readonly bool playSound;
    }
}
