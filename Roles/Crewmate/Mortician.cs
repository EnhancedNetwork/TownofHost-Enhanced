using Hazel;
using System.Collections.Generic;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;
internal class Mortician : RoleBase
{
    private static readonly int Id = 8900;
    public static bool On = false;
    private static List<byte> playerIdList = [];
    public override bool IsEnable => On;
    public static bool HasEnabled => CustomRoles.Mortician.IsClassEnable();
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

    private static OptionItem ShowArrows;

    private static Dictionary<byte, string> lastPlayerName = [];
    public static Dictionary<byte, string> msgToSend = [];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Mortician);
        ShowArrows = BooleanOptionItem.Create(Id + 2, "ShowArrows", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mortician]);
    }
    public override void Init()
    {
        playerIdList = [];
        lastPlayerName = [];
        msgToSend = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        On = true;
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
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
    public override void OnTargetDead(PlayerControl killer, PlayerControl target)
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
        foreach (var pc in playerIdList.ToArray())
        {
            var player = Utils.GetPlayerById(pc);
            if (player == null || !player.IsAlive()) continue;
            LocateArrow.Add(pc, target.transform.position);
            SendRPC(pc, true, target.transform.position);
        }
    }
    public override void OnReportDeadBody(PlayerControl pc, PlayerControl target)
    {
        foreach (var apc in playerIdList.ToArray())
        {
            LocateArrow.RemoveAllTarget(apc);
            SendRPC(apc, false);
        }

        if (!pc.Is(CustomRoles.Mortician) || target == null || pc.PlayerId == target.PlayerId) return;
        lastPlayerName.TryGetValue(target.PlayerId, out var name);
        if (name == "") msgToSend.Add(pc.PlayerId, string.Format(Translator.GetString("MorticianGetNoInfo"), target.GetRealName()));
        else msgToSend.Add(pc.PlayerId, string.Format(Translator.GetString("MorticianGetInfo"), target.GetRealName(), name));
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
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
    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (Mortician.msgToSend.ContainsKey(pc.PlayerId))
            AddMsg(Mortician.msgToSend[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mortician), GetString("MorticianCheckTitle")));
    }
    public override void MeetingHudClear() => msgToSend = [];
}