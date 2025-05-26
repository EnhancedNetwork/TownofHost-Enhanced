using AmongUs.InnerNet.GameDataMessages;
using Hazel;

namespace TOHE.Modules.Rpc
{
    public class RpcSyncLobbyTimerVanilla : BaseRpcMessage
    {
        public override RpcCalls RpcType => RpcCalls.LobbyTimeExpiring;

        public RpcSyncLobbyTimerVanilla(uint rpcObjectNetId, int timer, bool flag) : base(rpcObjectNetId)
        {
            this.timer = timer;
            this.flag = flag;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.WritePacked(this.timer);
            msg.Write(flag);
        }

        private readonly int timer;
        private readonly bool flag;
    }

    public class RpcSyncLobbyTimerModded : BaseModdedRpc
    {
        public override CustomRPC RpcType => CustomRPC.SyncLobbyTimer;

        public RpcSyncLobbyTimerModded(uint rpcObjectNetId, int timer) : base(rpcObjectNetId)
        {
            this.timer = timer;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.WritePacked(timer);
        }

        private readonly int timer;
    }
}
