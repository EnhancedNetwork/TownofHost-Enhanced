using AmongUs.GameOptions;
using System;
using TOHE.Modules;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class TimeMaster : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.TimeMaster;
    private const int Id = 9900;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.TimeMaster);
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    public override bool BlockMoveInVent(PlayerControl pc) => true;
    //==================================================================\\

    private static OptionItem TimeMasterSkillCooldown;
    private static OptionItem TimeMasterSkillDuration;
    private static OptionItem TimeMasterMaxUses;

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
        TimeMasterAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 13, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeMaster])
            .SetValueFormat(OptionFormat.Times);
        OverrideTasksData.Create(Id + 14, TabGroup.CrewmateRoles, CustomRoles.TimeMaster);
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
        playerId.SetAbilityUseLimit(TimeMasterMaxUses.GetInt());
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = TimeMasterSkillCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.AbilityButton.buttonLabelText.text = GetString("TimeMasterVentButtonText");
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (!lowLoad && TimeMasterInProtect.TryGetValue(player.PlayerId, out var vtime) && vtime + TimeMasterSkillDuration.GetInt() < nowTime)
        {
            TimeMasterInProtect.Remove(player.PlayerId);
            if (!DisableShieldAnimations.GetBool()) player.RpcGuardAndKill();
            else player.RpcResetAbilityCooldown();
            player.Notify(GetString("TimeMasterSkillStop"));
        }
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (TimeMasterInProtect.TryGetValue(target.PlayerId, out var timer) && killer.PlayerId != target.PlayerId)
            if (timer + TimeMasterSkillDuration.GetInt() >= GetTimeStamp(DateTime.UtcNow))
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
    public override void OnEnterVent(PlayerControl pc, Vent currentVent)
    {
        if (pc.GetAbilityUseLimit() >= 1)
        {
            pc.RpcRemoveAbilityUse();

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
                    TimeMasterBackTrack[player.PlayerId] = player.GetCustomPosition();
                }
            }
        }
        else
        {
            pc.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
        }
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Time Master");
}
