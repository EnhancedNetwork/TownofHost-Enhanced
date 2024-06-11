using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Oblivious
{
    private const int Id = 20700;

    public static OptionItem ImpCanBeOblivious;
    public static OptionItem CrewCanBeOblivious;
    public static OptionItem NeutralCanBeOblivious;
    public static OptionItem ObliviousBaitImmune;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Oblivious, canSetNum: true);
        ImpCanBeOblivious = BooleanOptionItem.Create("ImpCanBeOblivious", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Oblivious]);
        CrewCanBeOblivious = BooleanOptionItem.Create("CrewCanBeOblivious", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Oblivious]);
        NeutralCanBeOblivious = BooleanOptionItem.Create("NeutralCanBeOblivious", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Oblivious]);
        ObliviousBaitImmune = BooleanOptionItem.Create("ObliviousBaitImmune", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Oblivious]);
    }

}