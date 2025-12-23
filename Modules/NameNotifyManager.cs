using Hazel;
using TOHE.Modules.Rpc;
using UnityEngine;

namespace TOHE;

public static class NameNotifyManager
{
    public static Dictionary<byte, Dictionary<string, long>> Notifies = [];
    private static long LastUpdate;

    public static void Reset()
    {
        Notifies = [];
    }

    public static void Notify(this PlayerControl pc, string text, float time = 6f, bool overrideAll = false, bool log = true, SendOption sendOption = SendOption.Reliable)
    {
        if (!AmongUsClient.Instance.AmHost || pc == null) return;
        if (!GameStates.IsInTask) return;

        text = text.Trim();
        if (!text.Contains("<color=") && !text.Contains("</color>") && !text.Contains("<#")) text = Utils.ColorString(Color.white, text);
        if (!text.Contains("<size=")) text = $"<size=1.9>{text}</size>";

        long expireTS = Utils.TimeStamp + (long)time;

        if (overrideAll || !Notifies.TryGetValue(pc.PlayerId, out Dictionary<string, long> notifies))
            Notifies[pc.PlayerId] = new() { { text, expireTS } };
        else
            notifies[text] = expireTS;

        if (pc.IsNonHostModdedClient()) SendRPC(pc.PlayerId, text, expireTS, overrideAll, sendOption);
        Utils.NotifyRoles(SpecifySeer: pc, SpecifyTarget: pc);
        if (log) Logger.Info($"New name notify for {pc.GetNameWithRole().RemoveHtmlTags()}: {text} ({time}s)", "Name Notify");
    }

    public static void OnFixedUpdate()
    {
        if (!GameStates.IsInTask)
        {
            Reset();
            return;
        }

        long now = Utils.TimeStamp;
        if (now == LastUpdate) return;
        LastUpdate = now;

        List<byte> toNotify = [];

        foreach ((byte id, Dictionary<string, long> notifies) in Notifies)
        {
            List<string> toRemove = [];

            notifies.DoIf(x => x.Value <= now, x => toRemove.Add(x.Key));

            toRemove.ForEach(x => notifies.Remove(x));
            if (toRemove.Count > 0) toNotify.Add(id);
        }

        if (toNotify.Count == 0 || !AmongUsClient.Instance.AmHost) return;

        toNotify.ToValidPlayers().ForEach(x => Utils.NotifyRoles(SpecifySeer: x, SpecifyTarget: x));
    }

    public static bool GetNameNotify(PlayerControl player, out string name)
    {
        name = string.Empty;
        if (!Notifies.TryGetValue(player.PlayerId, out Dictionary<string, long> notifies)) return false;

        name = string.Join('\n', notifies.OrderBy(x => x.Value).Select(x => x.Key));
        return true;
    }

    private static void SendRPC(byte playerId, string text, long expireTS, bool overrideAll, SendOption sendOption) // Only sent when adding a new notification
    {
        if (!AmongUsClient.Instance.AmHost) return;

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncNameNotify, sendOption);
        writer.Write(playerId);
        writer.Write(text);
        writer.Write(expireTS.ToString());
        writer.Write(overrideAll);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void SendRPC(CustomRpcSender sender, byte playerId, string text, long expireTS, bool overrideAll)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        sender.AutoStartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncNameNotify);
        sender.Write(playerId);
        sender.Write(text);
        sender.Write(expireTS.ToString());
        sender.Write(overrideAll);
        sender.EndRpc();
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        if (AmongUsClient.Instance.AmHost) return;

        byte playerId = reader.ReadByte();
        string text = reader.ReadString();
        long expireTS = long.Parse(reader.ReadString());
        bool overrideAll = reader.ReadBoolean();

        if (overrideAll || !Notifies.TryGetValue(playerId, out Dictionary<string, long> notifies))
            Notifies[playerId] = new() { { text, expireTS } };
        else
            notifies[text] = expireTS;

        Logger.Info($"New name notify for {Main.AllPlayerNames[playerId]}: {text} ({expireTS - Utils.TimeStamp}s)", "Name Notify");
    }
}
