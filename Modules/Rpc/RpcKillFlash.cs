using Hazel;

namespace TOHE.Modules.Rpc
{
    class RpcKillFlash : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.KillFlash;
        public RpcKillFlash(uint netId, bool doKillSound) : base(netId)
        {
            this.doKillSound = doKillSound;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(doKillSound);
        }

        private readonly bool doKillSound;
    }
}
