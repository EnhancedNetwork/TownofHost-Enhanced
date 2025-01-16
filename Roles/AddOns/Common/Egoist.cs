using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Egoist : IAddon
{
    public CustomRoles Role => CustomRoles.Egoist;
    private const int Id = 23500;
    public AddonTypes Type => AddonTypes.Misc;

    public static OptionItem CrewCanBeEgoist;
    public static OptionItem ImpCanBeEgoist;
    public static OptionItem ImpEgoistVisibalToAllies;
    public static OptionItem EgoistCountAsConverted;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Egoist, canSetNum: true, tab: TabGroup.Addons);
        CrewCanBeEgoist = BooleanOptionItem.Create(Id + 10, "CrewCanBeEgoist", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Egoist]);
        ImpCanBeEgoist = BooleanOptionItem.Create(Id + 11, "ImpCanBeEgoist", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Egoist]);
        ImpEgoistVisibalToAllies = BooleanOptionItem.Create(Id + 12, "ImpEgoistVisibalToAllies", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Egoist]);
        EgoistCountAsConverted = BooleanOptionItem.Create(Id + 13, "EgoistCountAsConverted", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Egoist]);
    }

    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}
