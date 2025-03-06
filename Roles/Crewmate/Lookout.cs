using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Lookout : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Lookout;
    private const int Id = 11800;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateInvestigative;
    //==================================================================\\

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Lookout);
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (!seer.IsAlive() || !seen.IsAlive()) return string.Empty;

        return ColorString(GetRoleColor(CustomRoles.Lookout), $" {seen.Data.PlayerId}");
    }
}
