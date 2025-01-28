using TOHE.Roles.Double;
using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;
using System.Diagnostics.Metrics;
using TOHE.Modules;
using AmongUs.GameOptions;

namespace TOHE.Roles.Neutral;
internal class Contaminator : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Contaminator;
    private const int Id = 33600;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    public bool IsSwitching = false;
    public static HashSet<byte> TargetList = [];

    public static OptionItem ContaminationRadius;
    public static OptionItem CheckLimitOpt;
    public static OptionItem AbilityUsesPerTaskCompleted;
    public static OptionItem MassacreKillCooldown;

    public override void Init()
    {
        TargetList.Clear();
    }

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Contaminator);
        ContaminationRadius = FloatOptionItem.Create(Id + 10, "ContaminationRadius", new(0.5f, 1.5f, 0.1f), 1.3f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Contaminator])
            .SetValueFormat(OptionFormat.Multiplier);
        CheckLimitOpt = IntegerOptionItem.Create(Id + 11, "ContaminatorSkillLimit", new(0, 4, 1), 1, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Contaminator])
            .SetValueFormat(OptionFormat.Times);
        ContaminatorAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 12, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Contaminator])
            .SetValueFormat(OptionFormat.Times);
        MassacreKillCooldown = FloatOptionItem.Create(Id + 13, "MassacreKillCooldown", new(0f, 20f, 1f), 5f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Contaminator])
            .SetValueFormat(OptionFormat.Seconds);
        OverrideTasksData.Create(Id + 20, TabGroup.NeutralRoles, CustomRoles.Contaminator);
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(CheckLimitOpt.GetFloat());
    }

    public override void OnEnterVent(PlayerControl player, Vent vent)
    {
        if (player.GetAbilityUseLimit() >= 1)
        {
            player.RpcRemoveAbilityUse();
            Logger.Info("Contamination", "Contaminated");
            _ = new LateTask(() =>
            {
                foreach (var killing in Main.AllAlivePlayerControls)
                {
                    if (killing == player) continue;

                    if (killing.IsTransformedNeutralApocalypse()) continue;
                    else if ((killing.Is(CustomRoles.NiceMini) || killing.Is(CustomRoles.EvilMini)) && Mini.Age < 18) continue;

                    if (GetDistance(player.transform.position, killing.transform.position) <= ContaminationRadius.GetFloat())
                    {
                        TargetList.Add(killing.PlayerId);
                    }
                }
            }, 0.1f, "Contaminator Bug Fix");
        }
        else
        {
            player.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
        }
    }

    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if ((!seer.IsAlive() || seer.Is(CustomRoles.Contaminator)) && TargetList.Contains(target.PlayerId))
        {
            return ColorString(GetRoleColor(CustomRoles.Contaminator), "◈");
        }
        if ((!seer.IsAlive() || seer.Is(CustomRoles.Massacre)) && TargetList.Contains(target.PlayerId))
        {
            return ColorString(GetRoleColor(CustomRoles.Massacre), "◈");
        }
        else if (seer.IsAlive() && TargetList.Contains(target.PlayerId) && _state.PlayerId == target.PlayerId)
        {
            return ColorString(GetRoleColor(CustomRoles.Contaminator), "◈");
        }

        return string.Empty;
    }

    public override bool CheckVote(PlayerControl voter, PlayerControl target)
    {
        if (voter == target)
        {
            SendMessage(GetString("ContaminatorSwitch"), voter.PlayerId, ColorString(GetRoleColor(CustomRoles.Contaminator), GetString("ContaminatorTitle")));
            IsSwitching = true;
            return false;
        }
        return true;
    }

    public override void OnMeetingHudStart(PlayerControl pc)
    {
        SendMessage(GetString("ContaminatorVoteSelf"), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Contaminator), GetString("ContaminatorTitle")));
    }

    public override void AfterMeetingTasks()
    {
        PlayerControl player = _Player;
        if (IsSwitching)
        {
            player.RpcChangeRoleBasis(CustomRoles.Massacre);
            player.RpcSetCustomRole(CustomRoles.Massacre, true);
            IsSwitching = false;
        }
    }
}
internal class Massacre : RoleBase
{
    public override CustomRoles Role => CustomRoles.Massacre;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    public override bool CanUseKillButton(PlayerControl player) => true;
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (Contaminator.TargetList.Contains(target.PlayerId))
        {
            return true;
        }
        killer.SetDeathReason(PlayerState.DeathReason.Misfire);
        killer.RpcMurderPlayer(killer);
        return false;
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Contaminator.MassacreKillCooldown.GetFloat();
}
