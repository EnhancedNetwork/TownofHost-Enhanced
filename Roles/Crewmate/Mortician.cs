﻿using Hazel;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;
internal class Mortician : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 8900;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem ShowArrows;

    private static readonly Dictionary<byte, string> lastPlayerName = [];
    private static readonly Dictionary<byte, string> msgToSend = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Mortician);
        ShowArrows = BooleanOptionItem.Create(Id + 2, "ShowArrows", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mortician]);
    }
    public override void Init()
    {
        playerIdList.Clear();
        lastPlayerName.Clear();
        msgToSend.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.CheckDeadBodyOthers.Add(CheckDeadBody);
        }
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
    private void CheckDeadBody(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (inMeeting || target.IsDisconnected()) return;

        Vector2 pos = target.transform.position;
        float minDis = float.MaxValue;
        string minName = "";
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.PlayerId == target.PlayerId) continue;
            var dis = Utils.GetDistance(pc.transform.position, pos);
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
    public override void OnReportDeadBody(PlayerControl pc, NetworkedPlayerInfo target)
    {
        foreach (var apc in playerIdList)
        {
            LocateArrow.RemoveAllTarget(apc);
            SendRPC(apc, false);
        }

        if (pc == null || target == null || target.Object == null || !pc.Is(CustomRoles.Mortician) || pc.PlayerId == target.PlayerId) return;
        lastPlayerName.TryGetValue(target.PlayerId, out var name);
        if (name == "") msgToSend.TryAdd(pc.PlayerId, string.Format(GetString("MorticianGetNoInfo"), target.PlayerName));
        else msgToSend.TryAdd(pc.PlayerId, string.Format(GetString("MorticianGetInfo"), target.PlayerName, name));
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
    {
        if (isForMeeting) return string.Empty;

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
        if (msgToSend.ContainsKey(pc.PlayerId))
            AddMsg(msgToSend[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mortician), GetString("MorticianCheckTitle")));
    }
    public override void MeetingHudClear() => msgToSend.Clear();
}