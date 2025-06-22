using Hazel;

namespace TOHE.Modules.Rpc
{
    public class RpcSyncAllPlayerNames : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SyncAllPlayerNames;

        public RpcSyncAllPlayerNames(uint rpcObjectNetId) : base(rpcObjectNetId)
        {
        }

        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.WritePacked(Main.AllPlayerNames.Count);
            foreach (var name in Main.AllPlayerNames)
            {
                writer.Write(name.Key);
                writer.Write(name.Value);
            }
            writer.WritePacked(Main.AllClientRealNames.Count);
            foreach (var name in Main.AllClientRealNames)
            {
                writer.Write(name.Key);
                writer.Write(name.Value);
            }
        }
    }

    public class RpcSyncNameNotify : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SyncNameNotify;
        public RpcSyncNameNotify(uint rpcObjectNetId, byte playerId, bool addOrRemove, string name, float time) : base(rpcObjectNetId)
        {
            this.playerId = playerId;
            this.addOrRemove = addOrRemove;
            this.name = name;
            this.time = time;
        }
        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.Write(playerId);
            writer.Write(addOrRemove);

            if (addOrRemove)
            {
                writer.Write(name);
                writer.Write(time);
            }
        }

        private readonly byte playerId;
        private readonly bool addOrRemove;
        private readonly string name;
        private readonly float time;
    }

    class RpcSetNameColorData : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SetNameColorData;
        public RpcSetNameColorData(uint netId, byte playerId, byte targetId, string colorCode) : base(netId)
        {
            this.playerId = playerId;
            this.targetId = targetId;
            this.colorCode = colorCode;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(playerId);
            msg.Write(targetId);
            msg.Write(colorCode);
        }

        private readonly byte playerId;
        private readonly byte targetId;
        private readonly string colorCode;
    }

}
