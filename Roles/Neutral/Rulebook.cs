using static TOHE.Options;
using static TOHE.CovenManager;
using TOHE.Modules;

namespace TOHE.Roles.Neutral;
internal class Rulebook : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Rulebook;
    private const int Id = 34100;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem MaxRemovals;
    private static OptionItem ErasesRoleOfKiller;

    public static readonly Dictionary<byte, byte> Provoked = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Rulebook);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 100f, 2.5f), 20f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Rulebook])
            .SetValueFormat(OptionFormat.Seconds);
        MaxRemovals = IntegerOptionItem.Create(Id + 11, "MaxRemovals341", new(1, 5, 1), 3, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Rulebook])
            .SetValueFormat(OptionFormat.Times);
        ErasesRoleOfKiller = BooleanOptionItem.Create(Id + 12, "ErasesRole341", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Rulebook]);
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(MaxRemovals.GetInt());

        // Double Trigger
        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (!ErasesRoleOfKiller.GetBool()) return true;
        if (killer.GetCustomRole().IsCrewmate())
        {
            killer.RpcChangeRoleBasis(CustomRoles.Crewmate);
            killer.RpcSetCustomRole(CustomRoles.Crewmate);
        }
        if (killer.GetCustomRole().IsNeutral())
        {
            if (killer.GetCustomRole().IsTNA())
            {
                return true;
            }
            killer.RpcChangeRoleBasis(CustomRoles.Bankrupt);
            killer.RpcSetCustomRole(CustomRoles.Bankrupt);
        }
        if (killer.GetCustomRole().IsImpostor())
        {
            killer.RpcSetCustomRole(CustomRoles.Madmate);
            killer.RpcChangeRoleBasis(CustomRoles.Crewmate);
            killer.RpcSetCustomRole(CustomRoles.Crewmate);
        }
        if (killer.GetCustomRole().IsCoven())
        {
            if (HasNecronomicon(killer))
            {
                return true;
            }
            killer.RpcSetCustomRole(CustomRoles.Enchanted);
            killer.RpcChangeRoleBasis(CustomRoles.Crewmate);
            killer.RpcSetCustomRole(CustomRoles.Crewmate);
        }
        return true;
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.CheckDoubleTrigger(target, () => {} ))
        {
            return true;
        }
        if (killer.GetAbilityUseLimit() < 1)
        {
            return true;
        }
        killer.RpcRemoveAbilityUse();
        if (target.GetCustomRole().IsTasklessCrewmate())
        {
            target.RpcChangeRoleBasis(CustomRoles.Crewmate);
            target.RpcSetCustomRole(CustomRoles.Crewmate);
        }
        if (target.GetCustomRole().IsNeutralKillerTeam())
        {
            if (target.GetCustomRole().IsTNA())
            {
                killer.RpcGuardAndKill(killer);
                return false;
            }
            target.RpcChangeRoleBasis(CustomRoles.Bankrupt);
            target.RpcSetCustomRole(CustomRoles.Bankrupt);
        }
        if (target.GetCustomRole().IsImpostor())
        {
            killer.RpcGuardAndKill(killer);
            return false;
        }
        if (target.GetCustomRole().IsCoven())
        {
            if (HasNecronomicon(target))
            {
                killer.RpcGuardAndKill(killer);
                return false;
            }
            target.RpcSetCustomRole(CustomRoles.Enchanted);
            target.RpcChangeRoleBasis(CustomRoles.Crewmate);
            target.RpcSetCustomRole(CustomRoles.Crewmate);
        }
        killer.RpcGuardAndKill(killer);
        return false;
    }
}
