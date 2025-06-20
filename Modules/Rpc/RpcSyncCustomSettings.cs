using Hazel;

namespace TOHE.Modules.Rpc
{
    class RpcSyncCustomSettings : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SyncCustomSettings;

        public RpcSyncCustomSettings(uint rpcObjectNetId, int startAmount, int lastAmount, List<OptionItem> options) : base(rpcObjectNetId)
        {
            this.startAmount = startAmount;
            this.lastAmount = lastAmount;
            this.options = options;
        }

        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.WritePacked(startAmount);
            writer.WritePacked(lastAmount);
            foreach (var option in options.ToArray())
            {
                writer.WritePacked(option.GetValue());
            }
        }

        private readonly int startAmount, lastAmount;
        private readonly List<OptionItem> options;
    }
}
