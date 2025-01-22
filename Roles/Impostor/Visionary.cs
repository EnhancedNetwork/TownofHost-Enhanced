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
                return seer.Is(CustomRoles.Admired) || seer.Is(CustomRoles.Narc) ?
                "00ffff" : "7f8c8d";
        }

        if (Main.PlayerStates[target.PlayerId].IsNecromancer)
        {
            return "ac42f2";
        }

        if (customRole.IsImpostorTeamV2() || customRole.IsMadmate())
        {
            return "ff1919";
        }

        if (customRole.IsCrewmate())
        {
            return "00ffff";
        }

        if (customRole.IsCoven() || customRole.Equals(CustomRoles.Enchanted))
        {
            return "ac42f2";
        }

        return Main.roleColors[CustomRoles.Knight];
    }
}
