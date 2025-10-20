using AmongUs.GameOptions;
using System;
using UnityEngine;
using TOHE.Modules;
using static TOHE.Translator;
using static TOHE.Utils;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class Lighter : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Lighter;
    private const int Id = 8400;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    public override bool BlockMoveInVent(PlayerControl pc) => true;
    //==================================================================\\

    private static OptionItem LighterVisionNormal;
    private static OptionItem LighterVisionOnLightsOut;
    private static OptionItem LighterSkillCooldown;
    private static OptionItem LighterSkillDuration;
    private static OptionItem LighterSkillMaxOfUseage;

    private long Timer;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Lighter, 1);
        LighterSkillCooldown = FloatOptionItem.Create(Id + 10, "LighterSkillCooldown", new(1f, 180f, 1f), 25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Seconds);
        LighterSkillDuration = FloatOptionItem.Create(Id + 11, "LighterSkillDuration", new(1f, 180f, 1f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Seconds);
        LighterVisionNormal = FloatOptionItem.Create(Id + 12, "LighterVisionNormal", new(0f, 5f, 0.05f), 1.35f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Multiplier);
        LighterVisionOnLightsOut = FloatOptionItem.Create(Id + 13, "LighterVisionOnLightsOut", new(0f, 5f, 0.05f), 0.5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Multiplier);
        LighterSkillMaxOfUseage = IntegerOptionItem.Create(Id + 14, "AbilityUseLimit", new(0, 180, 1), 4, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Times);
        LighterAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 15, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Times);
        OverrideTasksData.Create(Id + 16, TabGroup.CrewmateRoles, CustomRoles.Lighter);

    }
    public override void Init()
    {
        Timer = 0;
    }
    public override void Add(byte playerId)
    {
        Timer = 0;
        playerId.SetAbilityUseLimit(LighterSkillMaxOfUseage.GetInt());
    }
    public override void Remove(byte playerId)
    {
        Timer = 0;
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (!lowLoad && Timer != 0 && Timer + LighterSkillDuration.GetInt() < nowTime)
        {
            Timer = 0;
            if (!DisableShieldAnimations.GetBool())
            {
                player.RpcGuardAndKill();
            }
            else
            {
                player.RpcResetAbilityCooldown();
            }
            player.Notify(string.Format(GetString("AbilityExpired"), Math.Round(player.GetAbilityUseLimit(), 1)));
            player.MarkDirtySettings();
        }
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (pc.GetAbilityUseLimit() >= 1)
        {
            Timer = GetTimeStamp();
            if (!DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
            pc.Notify(GetString("AbilityInUse"), LighterSkillDuration.GetFloat());
            pc.RpcRemoveAbilityUse();
            pc.MarkDirtySettings();
        }
        else
        {
            pc.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) => Timer = 0;

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerInVentMaxTime = 1;
        AURoleOptions.EngineerCooldown = LighterSkillCooldown.GetFloat();

        if (Timer != 0)
        {
            opt.SetVision(false);
            if (IsActive(SystemTypes.Electrical)) opt.SetFloat(FloatOptionNames.CrewLightMod, LighterVisionOnLightsOut.GetFloat() * 5);
            else opt.SetFloat(FloatOptionNames.CrewLightMod, LighterVisionNormal.GetFloat());
        }
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.AbilityButton.buttonLabelText.text = GetString("LighterVentButtonText");
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Lighter");
}
