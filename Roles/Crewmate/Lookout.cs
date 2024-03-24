using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Lookout : RoleBase
{
    public const int Id = 11800;
    public static bool On = false;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

    public static void SetupCustomOptions()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Lookout);
    }
    
    public override void Init()
    {
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;

        if (!seer.IsAlive() || !seen.IsAlive()) return string.Empty;

        return ColorString(GetRoleColor(CustomRoles.Lookout), " " + seen.PlayerId.ToString()) + " ";
    }
}
