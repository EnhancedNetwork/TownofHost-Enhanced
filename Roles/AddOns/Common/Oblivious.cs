using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Oblivious
{
    private static readonly int Id = 20700;

    public static OptionItem ImpCanBeOblivious;
    public static OptionItem CrewCanBeOblivious;
    public static OptionItem NeutralCanBeOblivious;
    public static OptionItem ObliviousBaitImmune;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Oblivious, canSetNum: true);
        ImpCanBeOblivious = BooleanOptionItem.Create(Id + 10, "ImpCanBeOblivious", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Oblivious]);
        CrewCanBeOblivious = BooleanOptionItem.Create(Id + 11, "CrewCanBeOblivious", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Oblivious]);
        NeutralCanBeOblivious = BooleanOptionItem.Create(Id + 12, "NeutralCanBeOblivious", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Oblivious]);
        ObliviousBaitImmune = BooleanOptionItem.Create(Id + 13, "ObliviousBaitImmune", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Oblivious]);
    }


}

