using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using TOHE.Roles.Crewmate;
using UnityEngine;

namespace TOHE.Roles.Impostor;

internal class Undertaker : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 4900;
    private static HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    //==================================================================\\

    private static OptionItem SSCooldown;
    private static OptionItem KillCooldown;
    private static OptionItem FreezeTime;

    private static Dictionary<byte, Vector2> MarkedLocation = [];
    
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

    public override void Init()
    {
        playerIdList = [];
        DefaultSpeed = new();
        MarkedLocation = [];
    }

    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        MarkedLocation[playerId] = ExtendedPlayerControl.GetBlackRoomPosition();
        DefaultSpeed = Main.AllPlayerSpeed[playerId];
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = SSCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    private static void SendRPC(byte playerId)
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

    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool shapeshifting, bool shapeshiftIsHidden)
    {
        if (!shapeshifter.IsAlive()) return;
        if (!shapeshifting && !shapeshiftIsHidden) return;

        var shapeshifterId = shapeshifter.PlayerId;
        MarkedLocation[shapeshifterId] = shapeshifter.GetCustomPosition();
        SendRPC(shapeshifterId);

        if (shapeshiftIsHidden)
            shapeshifter.Notify(Translator.GetString("RejectShapeshift.AbilityWasUsed"), time: 2f);
    }

    private static void FreezeUndertaker(PlayerControl player)
    {
        Main.AllPlayerSpeed[player.PlayerId] = Main.MinSpeed;
        ReportDeadBodyPatch.CanReport[player.PlayerId] = false;
        player.MarkDirtySettings();
        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[player.PlayerId] = DefaultSpeed;
            ReportDeadBodyPatch.CanReport[player.PlayerId] = true;
            player.MarkDirtySettings();
        }, FreezeTime.GetFloat(), "Freeze Undertaker");
    }

    private static bool HasMarkedLoc(byte playerId) => MarkedLocation[playerId] != ExtendedPlayerControl.GetBlackRoomPosition();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!HasMarkedLoc(killer.PlayerId)) return true;

        if (target.Is(CustomRoles.Bait)) return true;
        if (Guardian.CannotBeKilled(target)) return true;
        if (target.Is(CustomRoles.Opportunist) && target.AllTasksCompleted() && Options.OppoImmuneToAttacksWhenTasksDone.GetBool()) return false;

        if (target.CanBeTeleported())
        {
            target.RpcTeleport(MarkedLocation[killer.PlayerId]);
            
            target.SetRealKiller(killer);
            target.RpcMurderPlayerV3(target);

            killer.SetKillCooldown();
            MarkedLocation[killer.PlayerId] = ExtendedPlayerControl.GetBlackRoomPosition();
            
            SendRPC(killer.PlayerId);
            FreezeUndertaker(killer);
            
            killer.SyncSettings();
        }
        return false;
    }

    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    {
        foreach(var playerId in MarkedLocation.Keys)
        {
            MarkedLocation[playerId] = ExtendedPlayerControl.GetBlackRoomPosition();
            Main.AllPlayerSpeed[playerId] = DefaultSpeed;
            SendRPC(playerId);
        }
    }
}

