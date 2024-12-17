using AmongUs.GameOptions;
using System;
using System.Text;
using UnityEngine;
using static TOHE.Translator;
using static TOHE.Utils;

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
    private static OptionItem LighterAbilityUseGainWithEachTaskCompleted;

    private long Timer;

    public override void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Lighter, 1);
        LighterSkillCooldown = FloatOptionItem.Create(Id + 10, "LighterSkillCooldown", new(1f, 180f, 1f), 25f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Seconds);
        LighterSkillDuration = FloatOptionItem.Create(Id + 11, "LighterSkillDuration", new(1f, 180f, 1f), 10f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Seconds);
        LighterVisionNormal = FloatOptionItem.Create(Id + 12, "LighterVisionNormal", new(0f, 5f, 0.05f), 1.35f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Multiplier);
        LighterVisionOnLightsOut = FloatOptionItem.Create(Id + 13, "LighterVisionOnLightsOut", new(0f, 5f, 0.05f), 0.5f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Multiplier);
        LighterSkillMaxOfUseage = IntegerOptionItem.Create(Id + 14, "AbilityUseLimit", new(0, 180, 1), 4, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Times);
        LighterAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 15, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        Timer = 0;
    }
    public override void Add(byte playerId)
    {
        Timer = 0;
        AbilityLimit = LighterSkillMaxOfUseage.GetInt();
    }
    public override void Remove(byte playerId)
    {
        Timer = 0;
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (!lowLoad && Timer != 0 && Timer + LighterSkillDuration.GetInt() < nowTime)
        {
            Timer = 0;
            if (!Options.DisableShieldAnimations.GetBool())
            {
                player.RpcGuardAndKill();
            }
            else
            {
                player.RpcResetAbilityCooldown();
            }
            player.Notify(string.Format(GetString("AbilityExpired"), Math.Round(AbilityLimit, 1)));
            player.MarkDirtySettings();
        }
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (AbilityLimit >= 1)
        {
            Timer = GetTimeStamp();
            if (!Options.DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
            pc.Notify(GetString("AbilityInUse"), LighterSkillDuration.GetFloat());
            AbilityLimit--;
            pc.MarkDirtySettings();
        }
        else
        {
            pc.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
        }

        SendSkillRPC();
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) => Timer = 0;
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var taskState14 = Main.PlayerStates?[playerId].TaskState;
        Color TextColor14;
        var TaskCompleteColor14 = Color.green;
        var NonCompleteColor14 = Color.yellow;
        var NormalColor14 = taskState14.IsTaskFinished ? TaskCompleteColor14 : NonCompleteColor14;
        TextColor14 = comms ? Color.gray : NormalColor14;
        string Completed14 = comms ? "?" : $"{taskState14.CompletedTasksCount}";
        Color TextColor141;
        if (AbilityLimit < 1) TextColor141 = Color.red;
        else TextColor141 = Color.white;
        ProgressText.Append(ColorString(TextColor14, $"({Completed14}/{taskState14.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor141, $" <color=#ffffff>-</color> {Math.Round(AbilityLimit, 1)}"));
        return ProgressText.ToString();
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.IsAlive())
            AbilityLimit += LighterAbilityUseGainWithEachTaskCompleted.GetFloat();

        return true;
    }
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
