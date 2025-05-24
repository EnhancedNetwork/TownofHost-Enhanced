using Hazel;

namespace TOHE.Modules.Rpc
{
    public class RpcSyncAllPlayerNames : BaseModdedRpc
    {
        public override CustomRPC RpcType => CustomRPC.SyncAllPlayerNames;

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
}
