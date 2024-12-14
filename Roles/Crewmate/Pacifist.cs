using AmongUs.GameOptions;
using System;
using System.Text;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Impostor;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Pacifist : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Pacifist;
    private const int Id = 9200;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Pacifist);
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    public override bool BlockMoveInVent(PlayerControl pc) => true;
    //==================================================================\\

    private static OptionItem PacifistCooldown;
    private static OptionItem PacifistMaxOfUseage;
    private static OptionItem PacifistAbilityUseGainWithEachTaskCompleted;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Pacifist);
        PacifistCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.AbilityCooldown, new(1f, 180f, 1f), 30f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pacifist])
            .SetValueFormat(OptionFormat.Seconds);
        PacifistMaxOfUseage = IntegerOptionItem.Create(Id + 11, GeneralOption.SkillLimitTimes, new(0, 20, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pacifist])
            .SetValueFormat(OptionFormat.Times);
        PacifistAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(9204, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pacifist])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = PacifistMaxOfUseage.GetInt();
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (AbilityLimit < 1)
        {
            pc.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
        }
        else
        {
            AbilityLimit -= 1;
            if (!DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);

            Main.AllAlivePlayerControls.Where(x =>
            pc.Is(CustomRoles.Madmate)
                ? (x.CanUseKillButton() && x.GetCustomRole().IsCrewmate())
                : x.CanUseKillButton()
            ).Do(x =>
            {
                x.RPCPlayCustomSound("Dove");
                x.ResetKillCooldown();
                x.SetKillCooldown();

                if (x.Is(CustomRoles.Mercenary))
                { Mercenary.ClearSuicideTimer(); }

                x.Notify(ColorString(GetRoleColor(CustomRoles.Pacifist), GetString("PacifistSkillNotify")));
            });
            pc.RPCPlayCustomSound("Dove");
            pc.Notify(string.Format(GetString("PacifistOnGuard"), AbilityLimit));
        }
    }
    public override bool CheckBootFromVent(PlayerPhysics physics, int ventId)
        => AbilityLimit < 1;

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.IsAlive())
            AbilityLimit += PacifistAbilityUseGainWithEachTaskCompleted.GetFloat();

        return true;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = PacifistCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var taskState5 = Main.PlayerStates?[playerId].TaskState;
        Color TextColor5;
        var TaskCompleteColor5 = Color.green;
        var NonCompleteColor5 = Color.yellow;
        var NormalColor5 = taskState5.IsTaskFinished ? TaskCompleteColor5 : NonCompleteColor5;
        TextColor5 = comms ? Color.gray : NormalColor5;
        string Completed5 = comms ? "?" : $"{taskState5.CompletedTasksCount}";
        Color TextColor51;
        if (AbilityLimit < 1) TextColor51 = Color.red;
        else TextColor51 = Color.white;
        ProgressText.Append(ColorString(TextColor5, $"({Completed5}/{taskState5.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor51, $" <color=#ffffff>-</color> {Math.Round(AbilityLimit, 1)}"));
        return ProgressText.ToString();
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.AbilityButton.buttonLabelText.text = GetString("PacifistVentButtonText");
    }
}
