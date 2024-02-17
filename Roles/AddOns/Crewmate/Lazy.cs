using static TOHE.Options;

namespace TOHE.Roles.AddOns.Crewmate;

public class Lazy
{
    private static readonly int Id = 19300;

    public static OptionItem TasklessCrewCanBeLazy;
    public static OptionItem TaskBasedCrewCanBeLazy;
    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Lazy, canSetNum: true);
        TasklessCrewCanBeLazy = BooleanOptionItem.Create(Id + 10, "TasklessCrewCanBeLazy", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lazy]);
        TaskBasedCrewCanBeLazy = BooleanOptionItem.Create(Id + 11, "TaskBasedCrewCanBeLazy", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lazy]);
    }
}