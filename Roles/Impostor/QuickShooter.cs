using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System;
using TOHE.Modules;
using TOHE.Roles.Core;
using UnityEngine;

namespace TOHE.Roles.Impostor;

internal class QuickShooter : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.QuickShooter;
    private const int Id = 2200;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.QuickShooter);
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem MeetingReserved;
    private static OptionItem LimitBySSCoolDown;
    private static OptionItem SSCoolDown;

    private bool Storaging = false;
    private readonly Dictionary<byte, int> NewSL = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.QuickShooter);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 35f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.QuickShooter])
            .SetValueFormat(OptionFormat.Seconds);
        LimitBySSCoolDown = BooleanOptionItem.Create(Id + 11, "QucikShooterLimitBySS", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.QuickShooter]);
        SSCoolDown = FloatOptionItem.Create(Id + 12, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(LimitBySSCoolDown)
            .SetValueFormat(OptionFormat.Seconds);
        MeetingReserved = IntegerOptionItem.Create(Id + 14, "MeetingReserved", new(0, 15, 1), 2, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.QuickShooter])
            .SetValueFormat(OptionFormat.Pieces);
    }

    public override void Add(byte playerId)
    {
        AbilityLimit = 0;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = LimitBySSCoolDown.GetBool() ? SSCoolDown.GetFloat() : 0.01f;
    }

    public override void SetKillCooldown(byte id)
    {
        Main.AllPlayerKillCooldown[id] = (Storaging || AbilityLimit < 1) ? KillCooldown.GetFloat() : 0.1f;
        Storaging = false;
    }

    public void SendRPC(bool timer = false)
    {
        var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, _Player.GetClientId());
        writer.WriteNetObject(_Player);
        writer.Write((byte)AbilityLimit);

        if (_Player == null) { timer = false; }
        writer.Write(timer);
        if (timer)
            writer.Write(_Player.GetKillTimer());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        AbilityLimit = reader.ReadByte();
        var shouldtime = reader.ReadBoolean();
        float timer = 0f;
        if (shouldtime)
        {
            timer = reader.ReadSingle();
        }

        if (pc.AmOwner && shouldtime)
            DestroyableSingleton<HudManager>.Instance.AbilityButton.SetCoolDown(timer, 0.01f);
    }

    public override void UnShapeShiftButton(PlayerControl shapeshifter)
    {
        var killTimer = shapeshifter.GetKillTimer();
        Logger.Info($"Kill Timer: {killTimer}", "QuickShooter");

        if (killTimer <= 0)
        {
            AbilityLimit++;
            SendRPC();

            Storaging = true;
            shapeshifter.ResetKillCooldown();
            shapeshifter.SetKillCooldown();

            shapeshifter.Notify(Translator.GetString("QuickShooterStoraging"));
            Logger.Info($"{Utils.GetPlayerById(shapeshifter.PlayerId)?.GetNameWithRole()} : shot limit: {AbilityLimit}", "QuickShooter");
        }
        else
        {
            shapeshifter.Notify(Translator.GetString("QuickShooterFailed"));
            SendRPC(true);
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        NewSL[_state.PlayerId] = Math.Clamp((int)AbilityLimit, 0, MeetingReserved.GetInt());

        AbilityLimit = NewSL[_state.PlayerId];
        SendRPC();
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        AbilityLimit--;
        AbilityLimit = Math.Max(AbilityLimit, 0);
        SendRPC();

        return true;
    }

    public override string GetProgressText(byte playerId, bool comms)
        => Utils.ColorString(AbilityLimit > 0
            ? Utils.GetRoleColor(CustomRoles.QuickShooter).ShadeColor(0.25f)
            : Color.gray, $"({AbilityLimit})");

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton?.OverrideText(Translator.GetString("QuickShooterShapeshiftText"));
        hud.AbilityButton?.SetUsesRemaining((int)AbilityLimit);
    }
}
