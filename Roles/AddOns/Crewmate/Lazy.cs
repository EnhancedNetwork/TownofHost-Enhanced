using static TOHE.Options;

namespace TOHE.Roles.AddOns.Crewmate;

public class Lazy : IAddon
{
    public CustomRoles Role => CustomRoles.Lazy;
    private const int Id = 19300;
    public AddonTypes Type => AddonTypes.Helpful;

    private static OptionItem TasklessCrewCanBeLazy;
    private static OptionItem TaskBasedCrewCanBeLazy;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Lazy, canSetNum: true);
        TasklessCrewCanBeLazy = BooleanOptionItem.Create(Id + 10, "TasklessCrewCanBeLazy", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lazy]);
        TaskBasedCrewCanBeLazy = BooleanOptionItem.Create(Id + 11, "TaskBasedCrewCanBeLazy", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lazy]);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    public static bool CheckConflicts(PlayerControl player)
    {
        if (player.Is(CustomRoles.Ghoul)
            || player.Is(CustomRoles.LazyGuy))
            return false;

        if (player.GetCustomRole().IsNeutral()
            || player.GetCustomRole().IsImpostor()
            || (player.GetCustomRole().IsTasklessCrewmate() && !TasklessCrewCanBeLazy.GetBool())
            || (player.GetCustomRole().IsTaskBasedCrewmate() && !TaskBasedCrewCanBeLazy.GetBool()))
            return false;

        return true;
    }
}
