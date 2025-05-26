using Hazel;

namespace TOHE.Modules.Rpc
{
    class RpcSetKillTimer : BaseModdedRpc
    {
        public override CustomRPC RpcType => CustomRPC.SetKillTimer;

        public RpcSetKillTimer(uint rpcObjectNetId, float timer) : base(rpcObjectNetId)
        {
            this.timer = timer;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(timer);
        }

        private readonly float timer;
    }
}
