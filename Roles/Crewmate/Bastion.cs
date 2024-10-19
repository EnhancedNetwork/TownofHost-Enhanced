using AmongUs.GameOptions;
using System;
using System.Text;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Bastion : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 10200;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem BombsClearAfterMeeting;
    private static OptionItem BastionBombCooldown;
    private static OptionItem BastionAbilityUseGainWithEachTaskCompleted;
    private static OptionItem BastionMaxBombs;

    public static readonly HashSet<int> BombedVents = [];

    public override void SetupCustomOption()
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
        playerIdList.Clear();
        BombedVents.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        AbilityLimit = BastionMaxBombs.GetInt();
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerInVentMaxTime = 1;
        AURoleOptions.EngineerCooldown = BastionBombCooldown.GetFloat();
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.IsAlive())
            AbilityLimit += BastionAbilityUseGainWithEachTaskCompleted.GetFloat();
        
        return true;
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
        if (AbilityLimit < 1) TextColor151 = Color.red;
        else TextColor151 = Color.white;
        ProgressText.Append(ColorString(TextColor15, $"({Completed15}/{taskState15.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor151, $" <color=#777777>-</color> {Math.Round(AbilityLimit, 1)}"));
        return ProgressText.ToString();
    }
    public override bool OnCoEnterVentOthers(PlayerPhysics physics, int ventId)
    {
        if (!BombedVents.Contains(ventId)) return false;

        var pc = physics.myPlayer;
        if (pc.Is(Custom_Team.Crewmate) && !pc.Is(CustomRoles.Bastion) && !pc.IsCrewVenter() && !CopyCat.playerIdList.Contains(pc.PlayerId) && !Main.TasklessCrewmate.Contains(pc.PlayerId)) 
        {
            Logger.Info("Crewmate enter in bombed vent, bombed is cancel", "Bastion.OnCoEnterVentOther");
            return false;
        }
        else if (pc.Is(CustomRoles.DoubleAgent))
        {
            Logger.Info("DoubleAgent enter in bombed vent, bombed is cancel", "Bastion.OnCoEnterVentOther");
            return false;
        }
        else if (pc.IsTransformedNeutralApocalypse())
        {
            Logger.Info("Horseman enter in bombed vent, bombed is cancel", "Bastion.OnCoEnterVentOther");
            return false;
        }
        else
        {
            _ = new LateTask(() =>
            {
                var bastion = _Player;
                bastion.Notify(GetString("BastionNotify"));
                pc.Notify(GetString("EnteredBombedVent"));

                pc.SetDeathReason(PlayerState.DeathReason.Bombed);
                pc.RpcMurderPlayer(pc);
                pc.SetRealKiller(bastion);
                BombedVents.Remove(ventId);
            }, 0.5f, "Player bombed by Bastion");
            return true;
        }
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (AbilityLimit >= 1)
        {
            AbilityLimit--;
            SendSkillRPC();
            if (!BombedVents.Contains(vent.Id)) BombedVents.Add(vent.Id);
            pc.Notify(GetString("VentBombSuccess"));
        }
        else
        {
            pc.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (BombsClearAfterMeeting.GetBool())
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
