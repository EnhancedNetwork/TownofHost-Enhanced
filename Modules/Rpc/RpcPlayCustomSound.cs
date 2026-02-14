using Hazel;

namespace TOHE.Modules.Rpc
{
    class RpcPlayCustomSound : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.PlayCustomSound;
        public RpcPlayCustomSound(uint rpcObjectNetId, string soundName, float volume, float pitch) : base(rpcObjectNetId)
        {
            this.soundName = soundName;
            this.volume = volume;
            this.pitch = pitch;
        }
        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.Write(soundName);
            writer.Write(volume);
            writer.Write(pitch);
        }
        private readonly string soundName;
        private readonly float volume;
        private readonly float pitch;
    }
}
