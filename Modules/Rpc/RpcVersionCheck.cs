using Hazel;


namespace TOHE.Modules.Rpc
{
    public class RpcVersionCheck(uint rpcObjectNetId) : BaseModdedRpc(rpcObjectNetId)
    {
        public override byte RpcType => (byte)CustomRPC.VersionCheck;

        public override void SerializeRpcValues(MessageWriter writer)
        {
            var hostId = AmongUsClient.Instance.HostId;
            var cheating = Main.VersionCheat.Value;

            writer.Write(cheating ? Main.playerVersion[hostId].version.ToString() : Main.PluginVersion);
            writer.Write(cheating ? Main.playerVersion[hostId].tag : $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})");
            writer.Write(cheating ? Main.playerVersion[hostId].forkId : Main.ForkId);
            writer.Write(cheating);
        }
    }

    public class RpcRequestRetryVersionCheck(uint rpcObjectNetId) : BaseModdedRpc(rpcObjectNetId)
    {
        public override byte RpcType => (byte)CustomRPC.RequestRetryVersionCheck;

        public override void SerializeRpcValues(MessageWriter writer)
        {
        }
    }
}
