using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Stubborn
{
    private const int Id = 22500;

    public static OptionItem ImpCanBeStubborn;
    public static OptionItem CrewCanBeStubborn;
    public static OptionItem NeutralCanBeStubborn;

    public static void SetupCustomOptions()
    {

        SetupAdtRoleOptions(Id, CustomRoles.Stubborn, canSetNum: true);
        ImpCanBeStubborn = BooleanOptionItem.Create("ImpCanBeStubborn", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Stubborn]);
        CrewCanBeStubborn = BooleanOptionItem.Create("CrewCanBeStubborn", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Stubborn]);
        NeutralCanBeStubborn = BooleanOptionItem.Create("NeutralCanBeStubborn", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Stubborn]);
    }
}

