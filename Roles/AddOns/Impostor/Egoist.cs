using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;

public class Egoist : IAddon
{
    public CustomRoles Role => CustomRoles.Egoist;
    private const int Id = 23500;
    public AddonTypes Type => AddonTypes.Impostor;

    public static OptionItem ImpEgoistVisibalToAllies;
    public static OptionItem EgoistCountAsConverted;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Egoist, canSetNum: true, tab: TabGroup.Addons);
        ImpEgoistVisibalToAllies = BooleanOptionItem.Create(Id + 10, "ImpEgoistVisibalToAllies", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Egoist]);
        EgoistCountAsConverted = BooleanOptionItem.Create(Id + 11, "EgoistCountAsConverted", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Egoist]);
    }

    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}
