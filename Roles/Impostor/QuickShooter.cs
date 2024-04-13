using AmongUs.GameOptions;
using Hazel;
using System;
using UnityEngine;

namespace TOHE.Roles.Impostor;

internal class QuickShooter : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 2200;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem MeetingReserved;
    private static OptionItem ShapeshiftCooldown;

    private static readonly Dictionary<byte, int> ShotLimit = [];

    private static bool Storaging = false;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.QuickShooter);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 35f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.QuickShooter])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 12, "QuickShooterShapeshiftCooldown", new(0f, 180f, 2.5f), 15f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.QuickShooter])
            .SetValueFormat(OptionFormat.Seconds);
        MeetingReserved = IntegerOptionItem.Create(Id + 14, "MeetingReserved", new(0, 15, 1), 2, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.QuickShooter])
            .SetValueFormat(OptionFormat.Pieces);
    }
    public override void Init()
    {
        playerIdList.Clear();
        ShotLimit.Clear();
        Storaging = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        ShotLimit.TryAdd(playerId, 0);
    }

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.QuickShooter);
        writer.Write(playerId);
        writer.Write(ShotLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte QuickShooterId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        ShotLimit.TryAdd(QuickShooterId, Limit);
        ShotLimit[QuickShooterId] = Limit;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public override void SetKillCooldown(byte id)
    {
        Main.AllPlayerKillCooldown[id] = (Storaging || ShotLimit[id] < 1) ? KillCooldown.GetFloat() : 0.01f;
        Storaging = false;
    }

    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        if (shapeshifter.PlayerId == target.PlayerId) return false;

        if (shapeshifter.killTimer == 0)
        {
            ShotLimit[shapeshifter.PlayerId]++;
            SendRPC(shapeshifter.PlayerId);

            resetCooldown = false;
            Storaging = true;
            shapeshifter.ResetKillCooldown();
            shapeshifter.SetKillCooldown();

            shapeshifter.Notify(Translator.GetString("QuickShooterStoraging"));
            Logger.Info($"{Utils.GetPlayerById(shapeshifter.PlayerId)?.GetNameWithRole()} : shot limit: {ShotLimit[shapeshifter.PlayerId]}", "QuickShooter");
        }
        return false;
    }

    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    {
        Dictionary<byte, int> NewSL = [];

        foreach (var sl in ShotLimit)
            NewSL.Add(sl.Key, Math.Clamp(sl.Value, 0, MeetingReserved.GetInt()));

        foreach (var sl in NewSL)
        {
            ShotLimit[sl.Key] = sl.Value;
            SendRPC(sl.Key);
        }
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        ShotLimit.TryAdd(killer.PlayerId, 0);
        ShotLimit[killer.PlayerId]--;
        ShotLimit[killer.PlayerId] = Math.Max(ShotLimit[killer.PlayerId], 0);
        SendRPC(killer.PlayerId);

        return true;
    }

    public override string GetProgressText(byte playerId, bool comms)
        => Utils.ColorString(ShotLimit.ContainsKey(playerId) && ShotLimit[playerId] > 0
            ? Utils.GetRoleColor(CustomRoles.QuickShooter).ShadeColor(0.25f) 
            : Color.gray, ShotLimit.TryGetValue(playerId, out var shotLimit) 
                ? $"({shotLimit})" : "Invalid");

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton?.OverrideText(Translator.GetString("QuickShooterShapeshiftText"));
        hud.AbilityButton?.SetUsesRemaining(ShotLimit.TryGetValue(playerId, out var qx) ? qx : 0);
    }
}