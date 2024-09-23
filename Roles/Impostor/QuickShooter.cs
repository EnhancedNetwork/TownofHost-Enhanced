using AmongUs.GameOptions;
using System;
using TOHE.Modules;
using TOHE.Roles.Core;

namespace TOHE.Roles.Impostor;

internal class QuickShooter : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 2200;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.QuickShooter);
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem MeetingReserved;
    private static OptionItem ShapeshiftCooldown;

    private bool Storaging = false;
    private readonly Dictionary<byte, int> NewSL = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.QuickShooter);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 35f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.QuickShooter])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 12, "QuickShooterShapeshiftCooldown", new(0f, 180f, 2.5f), 15f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.QuickShooter])
            .SetValueFormat(OptionFormat.Seconds);
        MeetingReserved = IntegerOptionItem.Create(Id + 14, "MeetingReserved", new(0, 15, 1), 2, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.QuickShooter])
            .SetValueFormat(OptionFormat.Pieces);
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(0);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public override void SetKillCooldown(byte id)
    {
        Main.AllPlayerKillCooldown[id] = (Storaging || id.GetAbilityUseLimit() < 1) ? KillCooldown.GetFloat() : 0.03f;
        Storaging = false;
    }

    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        if (shapeshifter.PlayerId == target.PlayerId) return false;

        var killTimer = shapeshifter.GetKillTimer();
        Logger.Info($"Kill Timer: {killTimer}", "QuickShooter");

        if (killTimer <= 0)
        {
            shapeshifter.RpcIncreaseAbilityUseLimitBy(1);

            resetCooldown = false;
            Storaging = true;
            shapeshifter.ResetKillCooldown();
            shapeshifter.SetKillCooldown();

            shapeshifter.Notify(Translator.GetString("QuickShooterStoraging"));
        }
        return false;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (_Player == null) return;

        NewSL[_Player.PlayerId] = Math.Clamp((int)_Player.GetAbilityUseLimit(), 0, MeetingReserved.GetInt());
        _Player.SetAbilityUseLimit(NewSL[_state.PlayerId]);
        
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() > 0)
        {
            killer.RpcRemoveAbilityUse();
        }
        return true;
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton?.OverrideText(Translator.GetString("QuickShooterShapeshiftText"));
        hud.AbilityButton?.SetUsesRemaining((int)playerId.GetAbilityUseLimit());
    }
}