using Hazel;

namespace TOHE.Modules.Rpc
{
    class RpcPlayCustomSound : BaseModdedRpc
    {
        public override CustomRPC RpcType => CustomRPC.PlayCustomSound;
        public RpcPlayCustomSound(uint rpcObjectNetId, string soundName) : base(rpcObjectNetId)
        {
            this.soundName = soundName;
        }
        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.Write(soundName);
        }
        private readonly string soundName;
    }
}
