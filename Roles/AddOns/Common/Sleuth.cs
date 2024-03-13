using System.Collections.Generic;

namespace TOHE.Roles.AddOns.Common;

public static class Sleuth
{
    private static readonly int Id = 20100;

    public static OptionItem ImpCanBeSleuth;
    public static OptionItem CrewCanBeSleuth;
    public static OptionItem NeutralCanBeSleuth;
    public static OptionItem SleuthCanKnowKillerRole;
    
    public static Dictionary<byte, string> SleuthNotify = [];

    public static void SetupCustomOptions()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Sleuth, canSetNum: true);
        ImpCanBeSleuth = BooleanOptionItem.Create(Id + 10, "ImpCanBeSleuth", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sleuth]);
        CrewCanBeSleuth = BooleanOptionItem.Create(Id + 11, "CrewCanBeSleuth", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sleuth]);
        NeutralCanBeSleuth = BooleanOptionItem.Create(Id + 12, "NeutralCanBeSleuth", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sleuth]);
        SleuthCanKnowKillerRole = BooleanOptionItem.Create(Id + 13, "SleuthCanKnowKillerRole", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sleuth]);
    }

    public static void Init()
    {
        SleuthNotify = [];
    }
    public static void Clear()
    {
        SleuthNotify.Clear();
    }
}
