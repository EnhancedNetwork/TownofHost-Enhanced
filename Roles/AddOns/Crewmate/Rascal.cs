using static TOHE.Options;

namespace TOHE.Roles.AddOns.Crewmate;

public class Rascal : IAddon
{
    public CustomRoles Role => CustomRoles.Rascal;
    private const int Id = 20800;
    public AddonTypes Type => AddonTypes.Harmful;

    private static OptionItem RascalAppearAsMadmate;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Rascal, canSetNum: true, tab: TabGroup.Addons);
        RascalAppearAsMadmate = BooleanOptionItem.Create(Id + 10, "RascalAppearAsMadmate", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Rascal]);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    public static bool AppearAsMadmate(PlayerControl player) => RascalAppearAsMadmate.GetBool() && player.Is(CustomRoles.Rascal);
}
