using static TOHE.Options;

namespace TOHE.Roles.AddOns.Crewmate;

public class Rascal
{
    private static readonly int Id = 20800;

    private static OptionItem RascalAppearAsMadmate;
    
    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Rascal, canSetNum: true, tab: TabGroup.Addons);
        RascalAppearAsMadmate = BooleanOptionItem.Create(Id + 10, "RascalAppearAsMadmate", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Rascal]);
    }

    public static bool AppearAsMadmate(PlayerControl player) => RascalAppearAsMadmate.GetBool() && player.Is(CustomRoles.Rascal);
}
