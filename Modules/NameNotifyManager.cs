using Hazel;
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
        Utils.NotifyRoles(SpecifySeer: pc, ForceLoop: false);

        if (sendInLog) Logger.Info($"New name notify for {pc.GetNameWithRole().RemoveHtmlTags()}: {text} ({time}s)", "Name Notify");
    }
    public static void OnFixedUpdate(PlayerControl player)
    {
        if (!GameStates.IsInTask)
        {
            Reset();
            return;
        }
        if (Notice.TryGetValue(player.PlayerId, out var notifies) && notifies.TimeStamp < Utils.GetTimeStamp())
        {
            Notice.Remove(player.PlayerId);
            Utils.NotifyRoles(SpecifySeer: player, ForceLoop: false);
        }
    }
    public static bool GetNameNotify(PlayerControl player, out string name)
    {
        name = string.Empty;
        if (!Notice.TryGetValue(player.PlayerId, out var notifies)) return false;
        name = notifies.Text;
        return true;
    }
    private static void SendRPC(byte playerId)
    {
        var player = playerId.GetPlayer();
        if (!AmongUsClient.Instance.AmHost || !player.IsNonHostModdedClient()) return;

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncNameNotify, SendOption.Reliable, player.GetClientId());
        writer.Write(playerId);
        if (Notice.TryGetValue(playerId, out var notifies))
        {
            writer.Write(true);
            writer.Write(notifies.Text);
            writer.Write(notifies.TimeStamp - Utils.TimeStamp);
        }
        else writer.Write(false);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
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
