using Hazel;
using TOHE.Modules.Rpc;
using UnityEngine;

namespace TOHE;

public static class NameNotifyManager
{
    public static readonly Dictionary<byte, (string Text, long TimeStamp)> Notice = [];
    public static void Reset() => Notice.Clear();
    public static bool Notifying(this PlayerControl pc) => Notice.ContainsKey(pc.PlayerId);
    public static void Notify(this PlayerControl pc, string text, float time = 5f, bool sendInLog = true)
    {
        if (!AmongUsClient.Instance.AmHost || pc == null) return;
        if (!GameStates.IsInTask) return;
        if (!text.Contains("<color=") && !text.Contains("</color>")) text = Utils.ColorString(Color.white, text);
        if (!text.Contains("<size=")) text = $"<size=1.9>{text}</size>";

        Notice.Remove(pc.PlayerId);
        Notice.Add(pc.PlayerId, new(text, Utils.TimeStamp + (long)time));

        SendRPC(pc.PlayerId);
        Utils.NotifyRoles(SpecifySeer: pc, SpecifyTarget: pc);

        if (sendInLog) Logger.Info($"New name notify for {pc.GetNameWithRole().RemoveHtmlTags()}: {text} ({time}s)", "Name Notify");
    }
    public static void OnFixedUpdate(PlayerControl player)
    {
        if (!GameStates.IsInTask)
        {
            if (Notice.Any()) Notice.Clear();
            return;
        }

        if (Notice.ContainsKey(player.PlayerId) && Notice[player.PlayerId].TimeStamp < Utils.GetTimeStamp())
        {
            Notice.Remove(player.PlayerId);
            Utils.NotifyRoles(SpecifySeer: player, ForceLoop: false);
        }
    }
    public static bool GetNameNotify(PlayerControl player, out string name)
    {
        name = string.Empty;
        if (!Notice.TryGetValue(player.PlayerId, out (string Text, long TimeStamp) value)) return false;
        name = value.Text;
        return true;
    }
    private static void SendRPC(byte playerId)
    {
        var player = playerId.GetPlayer();
        if (player == null || !AmongUsClient.Instance.AmHost || !player.IsNonHostModdedClient()) return;

        var message = new RpcSyncNameNotify(
            PlayerControl.LocalPlayer.NetId,
            playerId,
            Notice.ContainsKey(playerId),
            Notice.ContainsKey(playerId) ? Notice[playerId].Text : string.Empty,
            Notice.ContainsKey(playerId) ? Notice[playerId].TimeStamp - Utils.GetTimeStamp() : 0f);
        RpcUtils.LateSpecificSendMessage(message, player.OwnerId);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        Notice.Remove(PlayerId);
        long now = Utils.GetTimeStamp();
        if (reader.ReadBoolean())
            Notice.Add(PlayerId, new(reader.ReadString(), now + (long)reader.ReadSingle()));
        Logger.Info($"New name notify for {Main.AllPlayerNames[PlayerId]}: {Notice[PlayerId].Text} ({Notice[PlayerId].TimeStamp - now}s)", "Name Notify");
    }
}
