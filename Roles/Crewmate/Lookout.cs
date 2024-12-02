using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Lookout : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 11800;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();

    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Lookout);
    }

    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (!seer.IsAlive() || !seen.IsAlive()) return string.Empty;

        return ColorString(GetRoleColor(CustomRoles.Lookout), $" {seen.Data.PlayerId}");
    }
}
