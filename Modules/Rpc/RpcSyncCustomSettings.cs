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
            // is single
            writer.Write(false);
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
    
    class RpcSyncCustomSettingsSingle : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SyncCustomSettings;

        public RpcSyncCustomSettingsSingle(uint rpcObjectNetId, int id, int value) : base(rpcObjectNetId)
        {
            this.id = id;
            this.value = value;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(true);
            msg.WritePacked(id);
            msg.WritePacked(value);
        }

        private readonly int id;
        private readonly int value;
    }
}
