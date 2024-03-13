using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class VoidBallot
{
    private static readonly int Id = 21100;

    public static OptionItem ImpCanBeVoidBallot;
    public static OptionItem CrewCanBeVoidBallot;
    public static OptionItem NeutralCanBeVoidBallot;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.VoidBallot, canSetNum: true);
        ImpCanBeVoidBallot = BooleanOptionItem.Create(Id + 10, "ImpCanBeVoidBallot", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.VoidBallot]);
        CrewCanBeVoidBallot = BooleanOptionItem.Create(Id + 11, "CrewCanBeVoidBallot", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.VoidBallot]);
        NeutralCanBeVoidBallot = BooleanOptionItem.Create(Id + 12, "NeutralCanBeVoidBallot", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.VoidBallot]);
    }
}