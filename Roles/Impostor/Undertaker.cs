using Hazel;
using System.Collections.Generic;
using TOHE.Roles.Crewmate;
using UnityEngine;

namespace TOHE.Roles.Impostor;

public static class Undertaker
{
    private static readonly int Id = 4900;
    private static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static OptionItem SSCooldown;
    public static OptionItem KillCooldown;
    public static OptionItem FreezeTime;

    public static Dictionary<byte, Vector2> MarkedLocation = new();
    private static float DefaultSpeed = new();

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Undertaker);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Undertaker])
            .SetValueFormat(OptionFormat.Seconds);
        SSCooldown = FloatOptionItem.Create(Id + 11, "ShapeshiftCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Undertaker])
            .SetValueFormat(OptionFormat.Seconds);
        FreezeTime = FloatOptionItem.Create(Id + 13, "UndertakerFreezeDuration", new(1f, 5f, 1f), 5, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Undertaker])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public static void Init()
    {
        playerIdList = new();
        IsEnable = false;
        DefaultSpeed = new();
        MarkedLocation = new();
    }

    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;
        MarkedLocation[playerId] = ExtendedPlayerControl.GetBlackRoomPosition();
        DefaultSpeed = Main.AllPlayerSpeed[playerId];
    }

    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = SSCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UndertakerLocationSync, SendOption.Reliable, -1);
        writer.Write(playerId);
        var xLoc = MarkedLocation[playerId].x;
        writer.Write(xLoc);
        var yLoc = MarkedLocation[playerId].y;
        writer.Write(yLoc);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        float xLoc = reader.ReadSingle();
        float yLoc = reader.ReadSingle();

        if (MarkedLocation.ContainsKey(PlayerId))
            MarkedLocation[PlayerId] = new Vector2(xLoc,yLoc);
        else
            MarkedLocation.Add(PlayerId, ExtendedPlayerControl.GetBlackRoomPosition());
    }

    public static void OnShapeshift(PlayerControl pc, bool IsShapeshifting)
    {
        if (!IsEnable || !pc.IsAlive() || !IsShapeshifting) return;
        MarkedLocation[pc.PlayerId] = pc.GetCustomPosition();
        SendRPC(pc.PlayerId);
    }

    public static void FreezeUndertaker(PlayerControl player)
    {
        Main.AllPlayerSpeed[player.PlayerId] = Main.MinSpeed;
        ReportDeadBodyPatch.CanReport[player.PlayerId] = false;
        player.MarkDirtySettings();
        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[player.PlayerId] = DefaultSpeed;
            ReportDeadBodyPatch.CanReport[player.PlayerId] = true;
            player.MarkDirtySettings();
        }, FreezeTime.GetFloat(), "FreezeUndertaker");
    }

    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

    public static bool HasMarkedLoc(byte playerId) => MarkedLocation[playerId] != ExtendedPlayerControl.GetBlackRoomPosition();

    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {

        if (!IsThisRole(killer.PlayerId)) return true;
        if (target.Is(CustomRoles.Bait)) return true;
        if (target.Is(CustomRoles.Pestilence)) return true;
        if (target.Is(CustomRoles.Guardian) && target.AllTasksCompleted()) return true;
        if (target.Is(CustomRoles.Opportunist) && target.AllTasksCompleted() && Options.OppoImmuneToAttacksWhenTasksDone.GetBool()) return false;
        if (target.Is(CustomRoles.Veteran) && Main.VeteranInProtect.ContainsKey(target.PlayerId)) return true;
        if (Medic.ProtectList.Contains(target.PlayerId)) return false;

        if (!HasMarkedLoc(killer.PlayerId)) return true;

        if (target.CanBeTeleported())
        {
            target.RpcTeleport(MarkedLocation[killer.PlayerId]);
            target.SetRealKiller(killer);
            Main.PlayerStates[target.PlayerId].SetDead();
            target.RpcMurderPlayerV3(target);
            killer.SetKillCooldown();
            MarkedLocation[killer.PlayerId] = ExtendedPlayerControl.GetBlackRoomPosition();
            SendRPC(killer.PlayerId);
            FreezeUndertaker(killer);
            killer.SyncSettings();
        }
        return false;
    }
    
    public static void OnReportDeadBody()
    {
        foreach(var playerId in MarkedLocation.Keys)
        {
            MarkedLocation[playerId] = ExtendedPlayerControl.GetBlackRoomPosition();
            Main.AllPlayerSpeed[playerId] = DefaultSpeed;
            SendRPC(playerId);
        }
    }
}

