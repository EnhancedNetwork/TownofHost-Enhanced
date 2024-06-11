using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Autopsy
{
    private const int Id = 18600;

    public static OptionItem ImpCanBeAutopsy;
    public static OptionItem CrewCanBeAutopsy;
    public static OptionItem NeutralCanBeAutopsy;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Autopsy, canSetNum: true);
        ImpCanBeAutopsy = BooleanOptionItem.Create("ImpCanBeAutopsy", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Autopsy]);
        CrewCanBeAutopsy = BooleanOptionItem.Create("CrewCanBeAutopsy", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Autopsy]);
        NeutralCanBeAutopsy = BooleanOptionItem.Create("NeutralCanBeAutopsy", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Autopsy]);
    }
}