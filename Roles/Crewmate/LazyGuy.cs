using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class LazyGuy : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 6800;
    private static bool On = false;
    public override bool IsEnable => On;
    public static bool HasEnabled => CustomRoles.LazyGuy.IsClassEnable();
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    //==================================================================\\

    public static void SetupCustomOptions()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.LazyGuy);
    }
    public override void Init()
    {
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
    }
}
