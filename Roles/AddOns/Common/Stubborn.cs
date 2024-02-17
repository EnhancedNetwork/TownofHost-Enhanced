using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Stubborn
{
    private static readonly int Id = 22500;

    public static OptionItem ImpCanBeStubborn;
    public static OptionItem CrewCanBeStubborn;
    public static OptionItem NeutralCanBeStubborn;

    public static void SetupCustomOptions()
    {

        SetupAdtRoleOptions(Id, CustomRoles.Stubborn, canSetNum: true);
        ImpCanBeStubborn = BooleanOptionItem.Create(Id + 10, "ImpCanBeStubborn", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Stubborn]);
        CrewCanBeStubborn = BooleanOptionItem.Create(Id + 11, "CrewCanBeStubborn", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Stubborn]);
        NeutralCanBeStubborn = BooleanOptionItem.Create(Id + 12, "NeutralCanBeStubborn", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Stubborn]);
    }
}

