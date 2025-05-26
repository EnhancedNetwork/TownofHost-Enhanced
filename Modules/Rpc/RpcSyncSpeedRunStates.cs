using Hazel;

namespace TOHE.Modules.Rpc
{
    public class RpcSyncSpeedRunStates : BaseModdedRpc
    {
        public override CustomRPC RpcType => CustomRPC.SyncSpeedRunStates;

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
