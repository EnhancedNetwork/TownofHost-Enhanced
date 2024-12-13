using AmongUs.GameOptions;
using System;
using System.Text;
using TOHE.Modules;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Grenadier : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 8200;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Grenadier);
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    public override bool BlockMoveInVent(PlayerControl pc) => true;
    //==================================================================\\

    private static readonly Dictionary<byte, long> GrenadierBlinding = [];
    private static readonly Dictionary<byte, long> MadGrenadierBlinding = [];

    private static OptionItem GrenadierSkillCooldown;
    private static OptionItem GrenadierSkillDuration;
    private static OptionItem GrenadierCauseVision;
    private static OptionItem GrenadierCanAffectNeutral;
    private static OptionItem GrenadierSkillMaxOfUseage;
    private static OptionItem GrenadierAbilityUseGainWithEachTaskCompleted;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Grenadier);
        GrenadierSkillCooldown = FloatOptionItem.Create(Id + 10, "GrenadierSkillCooldown", new(1f, 180f, 1f), 25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
            .SetValueFormat(OptionFormat.Seconds);
        GrenadierSkillDuration = FloatOptionItem.Create(Id + 11, "GrenadierSkillDuration", new(1f, 60f, 1f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
            .SetValueFormat(OptionFormat.Seconds);
        GrenadierCauseVision = FloatOptionItem.Create(Id + 12, "GrenadierCauseVision", new(0f, 5f, 0.05f), 0.3f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
            .SetValueFormat(OptionFormat.Multiplier);
        GrenadierCanAffectNeutral = BooleanOptionItem.Create(Id + 13, "GrenadierCanAffectNeutral", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier]);
        GrenadierSkillMaxOfUseage = FloatOptionItem.Create(Id + 14, "GrenadierSkillMaxOfUseage", new(0, 20, 1), 2, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
            .SetValueFormat(OptionFormat.Times);
        GrenadierAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 15, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
            .SetValueFormat(OptionFormat.Times);
    }

    public override void Init()
    {
        GrenadierBlinding.Clear();
        MadGrenadierBlinding.Clear();
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = GrenadierSkillMaxOfUseage.GetFloat();
        //CustomRoleManager.OnFixedUpdateLowLoadOthers.Add(OnGrenaderFixOthers);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = GrenadierSkillCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public static void ApplyGameOptionsForOthers(IGameOptions opt, PlayerControl player)
    {
        // Grenadier or Mad Grenadier enter the vent
        if ((GrenadierBlinding.Any() &&
            (CustomRolesHelper.IsNarcImpV3(player) ||
            (CustomRolesHelper.IsNarcNeutral(player) && GrenadierCanAffectNeutral.GetBool()))
            )
            || (MadGrenadierBlinding.Any() && !player.GetCustomRole().IsImpostorTeam() && !player.Is(CustomRoles.Madmate)))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, GrenadierCauseVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, GrenadierCauseVision.GetFloat());
        }
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.IsAlive())
        {
            AbilityLimit += GrenadierAbilityUseGainWithEachTaskCompleted.GetFloat();
            SendSkillRPC();
        }

        return true;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        GrenadierBlinding.Clear();
        MadGrenadierBlinding.Clear();
    }

    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (AbilityLimit >= 1)
        {
            if (pc.Is(CustomRoles.Madmate))
            {
                MadGrenadierBlinding.Remove(pc.PlayerId);
                MadGrenadierBlinding.Add(pc.PlayerId, GetTimeStamp());
                Main.AllPlayerControls.Where(x => x.IsModded())
                    .Where(x => !x.GetCustomRole().IsImpostorTeam() && !x.Is(CustomRoles.Madmate))
                    .Do(x => x.RPCPlayCustomSound("FlashBang"));
            }
            else
            {
                GrenadierBlinding.Remove(pc.PlayerId);
                GrenadierBlinding.Add(pc.PlayerId, GetTimeStamp());
                Main.AllPlayerControls.Where(x => x.IsModded())
                    .Where(x => CustomRolesHelper.IsNarcImpV3(x) || (CustomRolesHelper.IsNarcNeutral(x) && GrenadierCanAffectNeutral.GetBool()))
                    .Do(x => x.RPCPlayCustomSound("FlashBang"));
            }
            if (!DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
            pc.RPCPlayCustomSound("FlashBang");
            pc.Notify(GetString("AbilityInUse"), GrenadierSkillDuration.GetFloat());
            AbilityLimit -= 1;
            SendSkillRPC();
            MarkEveryoneDirtySettings();
        }
        else
        {
            pc.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
        }
    }

    public static bool stopGrenadierSkill = false;
    public static bool stopMadGrenadierSkill = false;
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (lowLoad) return;
        if (!GrenadierBlinding.ContainsKey(player.PlayerId) && !MadGrenadierBlinding.ContainsKey(player.PlayerId)) return;

        var nowStamp = GetTimeStamp();
        if (GrenadierBlinding.TryGetValue(player.PlayerId, out var grenadierTime) && grenadierTime + GrenadierSkillDuration.GetInt() < nowStamp)
        {
            GrenadierBlinding.Remove(player.PlayerId);
            stopGrenadierSkill = true;
        }
        if (MadGrenadierBlinding.TryGetValue(player.PlayerId, out var madGrenadierTime) && madGrenadierTime + GrenadierSkillDuration.GetInt() < nowStamp)
        {
            MadGrenadierBlinding.Remove(player.PlayerId);
            stopMadGrenadierSkill = true;
        }
        if (stopGrenadierSkill || stopMadGrenadierSkill)
        {
            if (!DisableShieldAnimations.GetBool())
            {
                player.RpcGuardAndKill();
            }
            else
            {
                player.RpcResetAbilityCooldown();
            }
            player.Notify(GetString("AbilityExpired"));
            MarkEveryoneDirtySettings();
            stopGrenadierSkill = false;
            stopMadGrenadierSkill = false;
        }
    }

    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var taskState3 = Main.PlayerStates?[playerId].TaskState;
        Color TextColor3;
        var TaskCompleteColor3 = Color.green;
        var NonCompleteColor3 = Color.yellow;
        var NormalColor3 = taskState3.IsTaskFinished ? TaskCompleteColor3 : NonCompleteColor3;
        TextColor3 = comms ? Color.gray : NormalColor3;
        string Completed3 = comms ? "?" : $"{taskState3.CompletedTasksCount}";
        Color TextColor31;
        if (AbilityLimit < 1) TextColor31 = Color.red;
        else TextColor31 = Color.white;
        ProgressText.Append(ColorString(TextColor3, $"({Completed3}/{taskState3.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor31, $" <color=#ffffff>-</color> {Math.Round(AbilityLimit, 1)}"));
        return ProgressText.ToString();
    }

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.AbilityButton.buttonLabelText.text = GetString("GrenadierVentButtonText");
    }
}
