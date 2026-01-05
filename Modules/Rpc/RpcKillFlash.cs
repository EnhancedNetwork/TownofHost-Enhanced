using Hazel;

namespace TOHE.Modules.Rpc
{
    class RpcKillFlash : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.KillFlash;
        public RpcKillFlash(uint netId, uint seerId, bool doKillSound) : base(netId)
        {
            this.seerId = seerId;
            this.doKillSound = doKillSound;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.WritePacked(seerId);
            msg.Write(doKillSound);
        }

        private readonly uint seerId;
        private readonly bool doKillSound;
    }
}
