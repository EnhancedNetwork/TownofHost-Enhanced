using static TOHE.Options;

namespace TOHE.Roles.AddOns.Crewmate;

public class Nimble
{
    private const int Id = 19700;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Nimble, canSetNum: true, tab: TabGroup.Addons);
    }
}