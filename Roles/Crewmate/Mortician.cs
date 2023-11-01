using Hazel;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;
public static class Mortician
{
    private static readonly int Id = 7400;
    private static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    private static OptionItem ShowArrows;

    private static Dictionary<byte, string> lastPlayerName = new();
    public static Dictionary<byte, string> msgToSend = new();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Mortician);
        ShowArrows = BooleanOptionItem.Create(Id + 2, "ShowArrows", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mortician]);
    }
    public static void Init()
    {
        playerIdList = new();
        lastPlayerName = new();
        msgToSend = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;
    }
    private static void SendRPC(byte playerId, bool add, Vector3 loc = new())
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetMorticianArrow, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(add);
        if (add)
        {
            writer.Write(loc.x);
            writer.Write(loc.y);
            writer.Write(loc.z);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        bool add = reader.ReadBoolean();
        if (add)
            LocateArrow.Add(playerId, new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
        else
            LocateArrow.RemoveAllTarget(playerId);
    }
    public static void OnPlayerDead(PlayerControl target)
    {
        Vector2 pos = target.transform.position;
        float minDis = float.MaxValue;
        string minName = "";
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.PlayerId == target.PlayerId) continue;
            var dis = Vector2.Distance(pc.transform.position, pos);
            if (dis < minDis && dis < 1.5f)
            {
                minDis = dis;
                minName = pc.GetRealName();
            }
        }

        lastPlayerName.TryAdd(target.PlayerId, minName);
        foreach (var pc in playerIdList)
        {
            var player = Utils.GetPlayerById(pc);
            if (player == null || !player.IsAlive()) continue;
            LocateArrow.Add(pc, target.transform.position);
            SendRPC(pc, true, target.transform.position);
        }
    }
    public static void OnReportDeadBody(PlayerControl pc, GameData.PlayerInfo target)
    {
        foreach (var apc in playerIdList)
        {
            LocateArrow.RemoveAllTarget(apc);
            SendRPC(apc, false);
        }

        if (!pc.Is(CustomRoles.Mortician) || target == null || pc.PlayerId == target.PlayerId) return;
        lastPlayerName.TryGetValue(target.PlayerId, out var name);
        if (name == "") msgToSend.Add(pc.PlayerId, string.Format(Translator.GetString("MorticianGetNoInfo"), target.PlayerName));
        else msgToSend.Add(pc.PlayerId, string.Format(Translator.GetString("MorticianGetInfo"), target.PlayerName, name));
    }
    public static string GetTargetArrow(PlayerControl seer, PlayerControl target = null)
    {
        if (ShowArrows.GetBool())
        {
            if (!seer.Is(CustomRoles.Mortician)) return "";
            if (target != null && seer.PlayerId != target.PlayerId) return "";
            if (GameStates.IsMeeting) return "";
            return Utils.ColorString(Color.white, LocateArrow.GetArrows(seer));
        }
        else return "";
    }
}