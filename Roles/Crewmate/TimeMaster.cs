using AmongUs.GameOptions;
using System;
using System.Text;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;
using TOHE.Roles.Core;

namespace TOHE.Roles.Crewmate;

internal class TimeMaster : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 9900;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.TimeMaster);
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem TimeMasterSkillCooldown;
    private static OptionItem TimeMasterSkillDuration;
    private static OptionItem TimeMasterMaxUses;
    private static OptionItem TimeMasterAbilityUseGainWithEachTaskCompleted;

    private static readonly Dictionary<byte, Vector2> TimeMasterBackTrack = [];
    private static readonly Dictionary<byte, int> TimeMasterNum = [];
    private static readonly Dictionary<byte, long> TimeMasterInProtect = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.TimeMaster);
        TimeMasterSkillCooldown = FloatOptionItem.Create(Id + 10, "TimeMasterSkillCooldown", new(1f, 180f, 1f), 20f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeMaster])
            .SetValueFormat(OptionFormat.Seconds);
        TimeMasterSkillDuration = FloatOptionItem.Create(Id + 11, "TimeMasterSkillDuration", new(1f, 180f, 1f), 20f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeMaster])
            .SetValueFormat(OptionFormat.Seconds);
        TimeMasterMaxUses = IntegerOptionItem.Create(Id + 12, "TimeMasterMaxUses", new(0, 20, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeMaster])
            .SetValueFormat(OptionFormat.Times);
        TimeMasterAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id+ 13, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeMaster])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        TimeMasterBackTrack.Clear();
        TimeMasterNum.Clear();
        TimeMasterInProtect.Clear();
    }
    public override void Add(byte playerId)
    {
        TimeMasterNum.TryAdd(playerId, 0);
        AbilityLimit = TimeMasterMaxUses.GetInt();
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = TimeMasterSkillCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.IsAlive())
            AbilityLimit += TimeMasterAbilityUseGainWithEachTaskCompleted.GetFloat();

        return true;
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.AbilityButton.buttonLabelText.text = GetString("TimeMasterVentButtonText");
    }
    public override void OnFixedUpdateLowLoad(PlayerControl player)
    {
        if (TimeMasterInProtect.TryGetValue(player.PlayerId, out var vtime) && vtime + TimeMasterSkillDuration.GetInt() < GetTimeStamp())
        {
            TimeMasterInProtect.Remove(player.PlayerId);
            if (!DisableShieldAnimations.GetBool()) player.RpcGuardAndKill();
            else player.RpcResetAbilityCooldown();
            player.Notify(GetString("TimeMasterSkillStop"));
        }
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (TimeMasterInProtect.ContainsKey(target.PlayerId) && killer.PlayerId != target.PlayerId)
            if (TimeMasterInProtect[target.PlayerId] + TimeMasterSkillDuration.GetInt() >= GetTimeStamp(DateTime.UtcNow))
            {
                foreach (var player in Main.AllPlayerControls)
                {
                    if (!killer.Is(CustomRoles.Pestilence) && TimeMasterBackTrack.TryGetValue(player.PlayerId, out var position))
                    {
                        if (player.CanBeTeleported())
                        {
                            player.RpcTeleport(position);
                        }
                    }
                }
                killer.SetKillCooldown(target: target, forceAnime: true);
                return false;
            }
        return true;
    }
    public override void OnEnterVent(PlayerControl pc, Vent AirConditioning)
    {
        if (AbilityLimit >= 1)
        {
            AbilityLimit -= 1;
            TimeMasterInProtect.Remove(pc.PlayerId);
            TimeMasterInProtect.Add(pc.PlayerId, GetTimeStamp());

            if (!pc.IsModded())
            {
                pc.RpcGuardAndKill(pc);
            }
            pc.Notify(GetString("TimeMasterOnGuard"), TimeMasterSkillDuration.GetFloat());

            foreach (var player in Main.AllPlayerControls)
            {
                if (TimeMasterBackTrack.TryGetValue(player.PlayerId, out var position))
                {
                    if (player.CanBeTeleported() && player.PlayerId != pc.PlayerId)
                    {
                        player.RpcTeleport(position);
                    }
                    else if (pc == player)
                    {
                        player?.MyPhysics?.RpcBootFromVent(Main.LastEnteredVent.TryGetValue(player.PlayerId, out var vent) ? vent.Id : player.PlayerId);
                    }

                    TimeMasterBackTrack.Remove(player.PlayerId);
                }
                else
                {
                    TimeMasterBackTrack.Add(player.PlayerId, player.GetCustomPosition());
                }
            }
        }
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var taskState6 = Main.PlayerStates?[playerId].TaskState;
        Color TextColor6;
        var TaskCompleteColor6 = Color.green;
        var NonCompleteColor6 = Color.yellow;
        var NormalColor6 = taskState6.IsTaskFinished ? TaskCompleteColor6 : NonCompleteColor6;
        TextColor6 = comms ? Color.gray : NormalColor6;
        string Completed6 = comms ? "?" : $"{taskState6.CompletedTasksCount}";
        Color TextColor61;
        if (AbilityLimit < 1) TextColor61 = Color.red;
        else TextColor61 = Color.white;
        ProgressText.Append(ColorString(TextColor6, $"({Completed6}/{taskState6.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor61, $" <color=#ffffff>-</color> {Math.Round(AbilityLimit, 1)}"));
        return ProgressText.ToString();
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Time Master");
}
