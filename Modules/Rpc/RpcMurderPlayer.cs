using AmongUs.InnerNet.GameDataMessages;
using Hazel;

namespace TOHE.Modules.Rpc
{
    public class RpcMurderPlayer : BaseRpcMessage
    {
        public override RpcCalls RpcType => RpcCalls.MurderPlayer;
        public RpcMurderPlayer(uint rpcObjectNetId, uint targetNetId, MurderResultFlags flags) : base(rpcObjectNetId)
        {
            this.targetNetId = targetNetId;
            this.flags = flags;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.WritePacked(targetNetId);
            msg.Write((int)flags);
        }

        private readonly uint targetNetId;
        private readonly MurderResultFlags flags;
    }

    public class RpcExiled(uint rpcObjectNetId) : BaseRpcMessage(rpcObjectNetId)
    {
        public override RpcCalls RpcType => RpcCalls.Exiled;

        public override void SerializeRpcValues(MessageWriter msg)
        {
        }
    }
}
