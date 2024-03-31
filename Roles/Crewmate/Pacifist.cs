using System;
using System.Text;
using UnityEngine;
using AmongUs.GameOptions;
using TOHE.Roles.Impostor;
using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Pacifist : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 9200;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    //==================================================================\\

    private static OptionItem PacifistCooldown;
    private static OptionItem PacifistMaxOfUseage;
    private static OptionItem PacifistAbilityUseGainWithEachTaskCompleted;

    private static readonly Dictionary<byte, float> PacifistNumOfUsed = [];

    public static void SetupCustomOptions()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Pacifist);
        PacifistCooldown = FloatOptionItem.Create(Id + 10, "PacifistCooldown", new(1f, 180f, 1f), 30f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pacifist])
            .SetValueFormat(OptionFormat.Seconds);
        PacifistMaxOfUseage = IntegerOptionItem.Create(Id + 11, "PacifistMaxOfUseage", new(0, 20, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pacifist])
            .SetValueFormat(OptionFormat.Times);
        PacifistAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(9204, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pacifist])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        playerIdList.Clear();
        PacifistNumOfUsed.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        PacifistNumOfUsed.Add(playerId, PacifistMaxOfUseage.GetInt());
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (PacifistNumOfUsed[pc.PlayerId] < 1)
        {
            pc?.MyPhysics?.RpcBootFromVent(vent.Id);
            pc.Notify(GetString("PacifistMaxUsage"));
        }
        else
        {
            PacifistNumOfUsed[pc.PlayerId] -= 1;
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
            pc.Notify(string.Format(GetString("PacifistOnGuard"), PacifistNumOfUsed[pc.PlayerId]));
        }
    }
    public override bool CheckBootFromVent(PlayerPhysics physics, int ventId)
        => PacifistNumOfUsed.TryGetValue(physics.myPlayer.PlayerId, out var count) && count < 1;

    public override void OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (!player.IsAlive()) return;
        PacifistNumOfUsed[player.PlayerId] += PacifistAbilityUseGainWithEachTaskCompleted.GetFloat();
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
        if (PacifistNumOfUsed[playerId] < 1) TextColor51 = Color.red;
        else TextColor51 = Color.white;
        ProgressText.Append(ColorString(TextColor5, $"({Completed5}/{taskState5.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor51, $" <color=#ffffff>-</color> {Math.Round(PacifistNumOfUsed[playerId], 1)}"));
        return ProgressText.ToString();
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.AbilityButton.buttonLabelText.text = GetString("PacifistVentButtonText");
    }
}
