using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class CursedWolf : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.CursedWolf;
    private const int Id = 1100;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.CursedWolf);
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem GuardSpellTimes;
    private static OptionItem KillAttacker;


    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.CursedWolf);
        GuardSpellTimes = IntegerOptionItem.Create(Id + 2, "CursedWolfGuardSpellTimes", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.CursedWolf])
            .SetValueFormat(OptionFormat.Times);
        KillAttacker = BooleanOptionItem.Create(Id + 3, GeneralOption.KillAttackerWhenAbilityRemaining, true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.CursedWolf]);
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = GuardSpellTimes.GetInt();
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (killer == target || AbilityLimit <= 0) return true;
        if (killer.IsTransformedNeutralApocalypse()) return true;

        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(target);

        AbilityLimit -= 1;
        SendSkillRPC();

        if (KillAttacker.GetBool() && target.RpcCheckAndMurder(killer, true))
        {
            Logger.Info($"{target.GetNameWithRole()} Spell Count: {AbilityLimit}", "Cursed Wolf");
            killer.SetDeathReason(PlayerState.DeathReason.Curse);
            killer.RpcMurderPlayer(killer);
            killer.SetRealKiller(target);
        }
        return false;
    }

    public override string GetProgressText(byte PlayerId, bool comms) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.CursedWolf), $"({AbilityLimit})");
}
