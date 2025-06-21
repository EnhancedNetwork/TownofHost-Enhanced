using Hazel;
using TOHE.Modules.Rpc;
using TOHE;
using Il2CppInterop.Generator.Extensions;

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
