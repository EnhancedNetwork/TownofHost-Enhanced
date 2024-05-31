
namespace TOHE.Roles.AddOns.Common;

public static class Seer
{
    private const int Id = 20000;

    public static OptionItem ImpCanBeSeer;
    public static OptionItem CrewCanBeSeer;
    public static OptionItem NeutralCanBeSeer;

    public static void SetupCustomOptions()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Seer, canSetNum: true);
        ImpCanBeSeer = BooleanOptionItem.Create("ImpCanBeSeer", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Seer]);
        CrewCanBeSeer = BooleanOptionItem.Create("CrewCanBeSeer", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Seer]);
        NeutralCanBeSeer = BooleanOptionItem.Create("NeutralCanBeSeer", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Seer]);
    }
}
