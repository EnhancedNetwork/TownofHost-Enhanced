using TOHE.Roles.Neutral;

namespace TOHE.Roles.Impostor;

internal class Visionary : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Visionary;
    private const int Id = 3900;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Visionary);
    }

    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target)
    {
        if (!seer.IsAlive() || !target.IsAlive() || target.Data.IsDead) return string.Empty;

        var customRole = target.GetCustomRole();

        if (Lich.IsCursed(target)) return "7f8c8d";

        foreach (var SubRole in target.GetCustomSubRoles())
        {
            if (SubRole is CustomRoles.Charmed
                or CustomRoles.Infected
                or CustomRoles.Contagious
                or CustomRoles.Egoist
                or CustomRoles.Recruit
                or CustomRoles.Soulless)
                return "7f8c8d";
            if (SubRole is CustomRoles.Admired)
            {
                return Main.roleColors[CustomRoles.Bait];
            }
        }

        if (Main.PlayerStates[target.PlayerId].IsNecromancer)
        {
            return Main.roleColors[CustomRoles.Coven];
        }

        if (customRole.IsImpostorTeamV2() || customRole.IsMadmate())
        {
            return Main.roleColors[CustomRoles.Impostor];
        }

        if (customRole.IsCrewmate())
        {
            return Main.roleColors[CustomRoles.Bait];
        }

        if (customRole.IsCoven() || customRole.Equals(CustomRoles.Enchanted))
        {
            return Main.roleColors[CustomRoles.Coven];
        }

        return "7f8c8d";
    }
}
