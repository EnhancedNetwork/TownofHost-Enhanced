using static TOHE.Options;

namespace TOHE.Roles.AddOns.Crewmate;

public class Lazy
{
    private static readonly int Id = 19300;

    private static OptionItem TasklessCrewCanBeLazy;
    private static OptionItem TaskBasedCrewCanBeLazy;
    
    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Lazy, canSetNum: true);
        TasklessCrewCanBeLazy = BooleanOptionItem.Create(Id + 10, "TasklessCrewCanBeLazy", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lazy]);
        TaskBasedCrewCanBeLazy = BooleanOptionItem.Create(Id + 11, "TaskBasedCrewCanBeLazy", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lazy]);
    }

    public static bool CheckConflicts(PlayerControl player)
    {
        if (player.Is(CustomRoles.Ghoul)
            || player.Is(CustomRoles.Needy))
            return false;

        if (player.GetCustomRole().IsNeutral() 
            || player.GetCustomRole().IsImpostor() 
            || (player.GetCustomRole().IsTasklessCrewmate() && !TasklessCrewCanBeLazy.GetBool())
            || (player.GetCustomRole().IsTaskBasedCrewmate() && !TaskBasedCrewCanBeLazy.GetBool()))
            return false;

        return true;
    }
}