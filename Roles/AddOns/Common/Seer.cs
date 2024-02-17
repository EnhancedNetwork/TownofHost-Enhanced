
namespace TOHE.Roles.AddOns.Common;

public static class Seer
{
    private static readonly int Id = 20000;

    public static OptionItem ImpCanBeSeer;
    public static OptionItem CrewCanBeSeer;
    public static OptionItem NeutralCanBeSeer;

    public static void SetupCustomOptions()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Seer, canSetNum: true);
        ImpCanBeSeer = BooleanOptionItem.Create(Id + 10, "ImpCanBeSeer", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Seer]);
        CrewCanBeSeer = BooleanOptionItem.Create(Id + 11, "CrewCanBeSeer", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Seer]);
        NeutralCanBeSeer = BooleanOptionItem.Create(Id + 12, "NeutralCanBeSeer", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Seer]);
    }
}
