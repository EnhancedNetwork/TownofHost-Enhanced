using Hazel;

namespace TOHE.Modules.Rpc
{
    class RpcSyncShieldPersonDiedFirst : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SyncShieldPersonDiedFirst;

        public RpcSyncShieldPersonDiedFirst(uint rpcObjectNetId, string firstDied, string firstDiedPrevious) : base(rpcObjectNetId)
        {
            this.firstDied = firstDied;
            this.firstDiedPrevious = firstDiedPrevious;
        }

        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.Write(firstDied);
            writer.Write(firstDiedPrevious);
        }

        private readonly string firstDied;
        private readonly string firstDiedPrevious;
    }
}
