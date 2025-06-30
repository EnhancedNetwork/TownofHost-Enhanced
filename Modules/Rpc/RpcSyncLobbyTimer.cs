using Hazel;

namespace TOHE.Modules.Rpc
{
    public class RpcSyncLobbyTimerVanilla : BaseModdedRpc
    {
        public override byte RpcType => (byte)RpcCalls.LobbyTimeExpiring;

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
        public override byte RpcType => (byte)CustomRPC.SyncLobbyTimer;

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
