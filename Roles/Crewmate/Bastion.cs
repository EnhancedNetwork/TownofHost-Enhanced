using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Bastion : RoleBase
{
    private const int Id = 10200;
    private static bool On = false;
    public override bool IsEnable => On;
    public static bool HasEnabled => CustomRoles.Bastion.IsClassEnable();
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;

    private static OptionItem BombsClearAfterMeeting;
    private static OptionItem BastionBombCooldown;
    private static OptionItem BastionAbilityUseGainWithEachTaskCompleted;
    private static OptionItem BastionMaxBombs;

    private static List<int> BombedVents = [];
    private static float BastionNumberOfAbilityUses = 0;

    public static void SetupCustomOptions()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Bastion, 1);
        BombsClearAfterMeeting = BooleanOptionItem.Create(Id + 10, "BombsClearAfterMeeting", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bastion]);
        BastionBombCooldown = FloatOptionItem.Create(Id + 11, "BombCooldown", new(0, 180, 1), 15, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bastion])
            .SetValueFormat(OptionFormat.Seconds);
        BastionAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(10204, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bastion])
            .SetValueFormat(OptionFormat.Times);
        BastionMaxBombs = IntegerOptionItem.Create(Id + 12, "BastionMaxBombs", new(1, 20, 1), 5, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bastion]);
    }
    public override void Init()
    {
        BombedVents = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        BastionNumberOfAbilityUses = BastionMaxBombs.GetInt();
        On = true;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerInVentMaxTime = 1;
        AURoleOptions.EngineerCooldown = BastionBombCooldown.GetFloat();
    }
    public override void OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (!player.IsAlive()) return;
        BastionNumberOfAbilityUses += BastionAbilityUseGainWithEachTaskCompleted.GetFloat();
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var taskState15 = Main.PlayerStates?[playerId].TaskState;
        Color TextColor15;
        var TaskCompleteColor15 = Color.green;
        var NonCompleteColor15 = Color.yellow;
        var NormalColor15 = taskState15.IsTaskFinished ? TaskCompleteColor15 : NonCompleteColor15;
        TextColor15 = comms ? Color.gray : NormalColor15;
        string Completed15 = comms ? "?" : $"{taskState15.CompletedTasksCount}";
        Color TextColor151;
        if (BastionNumberOfAbilityUses < 1) TextColor151 = Color.red;
        else TextColor151 = Color.white;
        ProgressText.Append(ColorString(TextColor15, $"({Completed15}/{taskState15.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor151, $" <color=#777777>-</color> {Math.Round(BastionNumberOfAbilityUses, 1)}"));
        return ProgressText.ToString();
    }
    public static bool OnOthersEnterVent(PlayerPhysics __instance, int ventId)
    {
        if (!BombedVents.Contains(ventId)) return false;

        var pc = __instance.myPlayer;
        if (pc.GetCustomRole().IsCrewmate() && !pc.Is(CustomRoles.Bastion) && !pc.IsCrewVenter() && !CopyCat.playerIdList.Contains(pc.PlayerId) && !Main.TasklessCrewmate.Contains(pc.PlayerId)) 
        {
            return false;
        }
        else
        {
            _ = new LateTask(() =>
            {
                foreach (var bastion in Main.AllAlivePlayerControls.Where(bastion => bastion.GetCustomRole() == CustomRoles.Bastion).ToArray())
                {
                    pc.SetRealKiller(bastion);
                    bastion.Notify(GetString("BastionNotify"));
                    pc.Notify(GetString("EnteredBombedVent"));

                    Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                    pc.RpcMurderPlayerV3(pc);
                    BombedVents.Remove(ventId);
                }
            }, 0.5f, "Player bombed by Bastion");
            return true;
        }
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (BastionNumberOfAbilityUses >= 1)
        {
            BastionNumberOfAbilityUses -= 1;
            if (!BombedVents.Contains(vent.Id)) BombedVents.Add(vent.Id);
            pc.Notify(GetString("VentBombSuccess"));
        }
        else
        {
            pc.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    {
        if (Bastion.BombsClearAfterMeeting.GetBool())
        {
            BombedVents.Clear();
        }
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.AbilityButton.buttonLabelText.text = GetString("BastionVentButtonText");
    }
}
