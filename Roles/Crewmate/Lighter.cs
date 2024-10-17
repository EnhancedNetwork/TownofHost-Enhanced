using AmongUs.GameOptions;
using System;
using System.Text;
using UnityEngine;
using static TOHE.Utils;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Lighter : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 8400;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem LighterVisionNormal;
    private static OptionItem LighterVisionOnLightsOut;
    private static OptionItem LighterSkillCooldown;
    private static OptionItem LighterSkillDuration;
    private static OptionItem LighterSkillMaxOfUseage;
    private static OptionItem LighterAbilityUseGainWithEachTaskCompleted;

    private static readonly Dictionary<byte, long> Timer = [];
    private static readonly Dictionary<byte, float> LighterNumOfUsed = [];

    public override void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Lighter, 1);
        LighterSkillCooldown = FloatOptionItem.Create(Id + 10, "LighterSkillCooldown", new(1f, 180f, 1f), 25f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Seconds);
        LighterSkillDuration = FloatOptionItem.Create(Id + 11, "LighterSkillDuration", new(1f, 180f, 1f), 10f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Seconds);
        LighterVisionNormal = FloatOptionItem.Create(Id + 12, "LighterVisionNormal", new(0f, 5f, 0.05f), 1.35f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Multiplier);
        LighterVisionOnLightsOut = FloatOptionItem.Create(Id +13, "LighterVisionOnLightsOut", new(0f, 5f, 0.05f), 0.5f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Multiplier);
        LighterSkillMaxOfUseage = IntegerOptionItem.Create(Id + 14, "AbilityUseLimit", new(0, 180, 1), 4, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Times);
        LighterAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 15, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        playerIdList.Clear();
        Timer.Clear();
        LighterNumOfUsed.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        LighterNumOfUsed.Add(playerId, LighterSkillMaxOfUseage.GetInt());
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        LighterNumOfUsed.Remove(playerId);
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (!lowLoad && Timer.TryGetValue(player.PlayerId, out var ltime) && ltime + LighterSkillDuration.GetInt() < nowTime)
        {
            Timer.Remove(player.PlayerId);
            if (!Options.DisableShieldAnimations.GetBool())
            {
                player.RpcGuardAndKill();
            }
            else
            {
                player.RpcResetAbilityCooldown();
            }
            player.Notify(GetString("AbilityExpired"));
            player.MarkDirtySettings();
        }
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (LighterNumOfUsed[pc.PlayerId] >= 1)
        {
            Timer.Remove(pc.PlayerId);
            Timer.Add(pc.PlayerId, GetTimeStamp());
            if (!Options.DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
            pc.Notify(GetString("AbilityInUse"), LighterSkillDuration.GetFloat());
            LighterNumOfUsed[pc.PlayerId] -= 1;
            pc.MarkDirtySettings();
        }
        else
        {
            pc.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) => Timer.Clear();
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
        if (LighterNumOfUsed[playerId] < 1) TextColor141 = Color.red;
        else TextColor141 = Color.white;
        ProgressText.Append(ColorString(TextColor14, $"({Completed14}/{taskState14.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor141, $" <color=#ffffff>-</color> {Math.Round(LighterNumOfUsed[playerId], 1)}"));
        return ProgressText.ToString();
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.IsAlive())
            LighterNumOfUsed[player.PlayerId] += LighterAbilityUseGainWithEachTaskCompleted.GetFloat();

        return true;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerInVentMaxTime = 1;
        AURoleOptions.EngineerCooldown = LighterSkillCooldown.GetFloat();

        if (Timer.Any())
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
