using Hazel;

namespace TOHE.Modules.Rpc
{
    class RpcSyncFFAPlayer : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SyncFFAPlayer;
        public RpcSyncFFAPlayer(uint netId, byte playerId, int score) : base(netId)
        {
            this.playerId = playerId;
            this.score = score;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(playerId);
            msg.Write(score);
        }

        private readonly byte playerId;
        private readonly int score;
    }
    class RpcSyncFFANameNotify : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SyncFFANameNotify;
        public RpcSyncFFANameNotify(uint netId, string name) : base(netId)
        {
            this.name = name;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(name);
        }

        private readonly string name;
    }
    class RpcSyncSpeedRunStates : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SyncSpeedRunStates;

        public RpcSyncSpeedRunStates(uint rpcObjectNetId, MessageWriter writer) : base(rpcObjectNetId)
        {
            this.writer = writer;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(writer, false);
            writer.Recycle();
            // There are chances that the writer can't be recycled. May cause memory leak.
        }

        private readonly MessageWriter writer;
    }
}
