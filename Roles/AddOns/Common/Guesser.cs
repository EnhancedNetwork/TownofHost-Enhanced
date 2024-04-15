using static TOHE.Options;
using UnityEngine;

namespace TOHE.Roles.AddOns.Common;

public static class Guesser
{
    private static readonly int Id = 22200;

    public static OptionItem ImpCanBeGuesser;
    public static OptionItem CrewCanBeGuesser;
    public static OptionItem NeutralCanBeGuesser;
    public static OptionItem GCanGuessAdt;
    public static OptionItem GCanGuessTaskDoneSnitch;
    public static OptionItem GTryHideMsg;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Guesser, canSetNum: true, tab: TabGroup.Addons);
        ImpCanBeGuesser = BooleanOptionItem.Create(Id + 10, "ImpCanBeGuesser", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        CrewCanBeGuesser = BooleanOptionItem.Create(Id + 11, "CrewCanBeGuesser", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        NeutralCanBeGuesser = BooleanOptionItem.Create(Id + 12, "NeutralCanBeGuesser", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        GCanGuessAdt = BooleanOptionItem.Create(Id+ 13, "GCanGuessAdt", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        GCanGuessTaskDoneSnitch = BooleanOptionItem.Create(Id + 14, "GCanGuessTaskDoneSnitch", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        GTryHideMsg = BooleanOptionItem.Create(Id + 15, "GuesserTryHideMsg", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser])
            .SetColor(Color.green);
    }
}

