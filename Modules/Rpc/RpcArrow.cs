using Hazel;
using UnityEngine;

namespace TOHE.Modules.Rpc
{
    class RpcArrow : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.Arrow;
        public RpcArrow(uint rpcObjectNetId, bool isTargetArrow, int index, byte playerId, byte? targetId, Vector3? vector) : base(rpcObjectNetId)
        {
            this.isTargetArrow = isTargetArrow;
            this.index = index;

            if (isTargetArrow)
            {
                this.playerId = playerId;
                this.targetId = targetId;
            }
            else
            {
                this.playerId = playerId;
                this.vector = vector;
            }
        }
        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(isTargetArrow);
            msg.WritePacked(index);

            if (isTargetArrow)
            {
                msg.Write(playerId);
                msg.Write(targetId ?? byte.MaxValue);
            }
            else
            {
                msg.Write(playerId);
                msg.Write(vector.HasValue ? vector.Value : Vector3.zero);
            }
        }

        private readonly bool isTargetArrow;
        private readonly int index;
        private readonly byte playerId;
        private readonly byte? targetId;
        private readonly Vector3? vector;
    }
}
