using static TOHE.Options;

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
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Visionary);
    }

    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target)
    {
        if (!seer.IsAlive() || !target.IsAlive() || target.Data.IsDead) return string.Empty;

        var customRole = target.GetCustomRole();

        foreach (var SubRole in target.GetCustomSubRoles())
        {
            if (SubRole is CustomRoles.Charmed
                or CustomRoles.Infected
                or CustomRoles.Contagious
                or CustomRoles.Egoist
                or CustomRoles.Recruit
                or CustomRoles.Soulless)
                return Main.roleColors[CustomRoles.Knight];
        }

        if (customRole.IsImpostorTeamV2() || customRole.IsMadmate() || target.Is(CustomRoles.Rascal) || target.Is(CustomRoles.Madmate))
        {
            return Main.roleColors[CustomRoles.Impostor];
        }

        if ((customRole.IsCrewmateTeamV2() || target.Is(CustomRoles.Admired)) && !target.Is(CustomRoles.Rebel))
        {
            return Main.roleColors[CustomRoles.Bait];
        }

        return Main.roleColors[CustomRoles.Knight];
    }
}
