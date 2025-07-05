using Hazel;
using TOHE;
using TOHE.Modules.Rpc;

class RpcSyncDeadPassedMeetingList : BaseModdedRpc
{
    public override byte RpcType => (byte)CustomRPC.SyncDeadPassedMeetingList;
    public RpcSyncDeadPassedMeetingList(uint netId, HashSet<byte> deadList) : base(netId)
    {
        this.deadList = deadList;
    }

    public override void SerializeRpcValues(MessageWriter msg)
    {
        msg.WritePacked(deadList.Count);
        foreach (var dead in deadList)
        {
            msg.Write(dead);
        }
    }

    private readonly HashSet<byte> deadList;
}
