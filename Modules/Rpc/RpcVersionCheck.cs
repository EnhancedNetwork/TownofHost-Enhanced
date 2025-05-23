using Hazel;


namespace TOHE.Modules.Rpc
{
    public class RpcVersionCheck : BaseModdedRpc
    {
        public override CustomRPC RpcType => CustomRPC.VersionCheck;

        public RpcVersionCheck(uint rpcObjectNetId) : base(rpcObjectNetId)
        {
        }

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
}
