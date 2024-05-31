using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Mundane
{
    private const int Id = 26700;

    public static OptionItem CanBeOnCrew;
    public static OptionItem CanBeOnNeutral;

    public static void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Mundane, canSetNum: true, tab: TabGroup.Addons);
        CanBeOnCrew = BooleanOptionItem.Create("CrewCanBeMundane", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mundane]);
        CanBeOnNeutral = BooleanOptionItem.Create("NeutralCanBeMundane", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mundane]);
    }
    public static bool OnGuess(PlayerControl pc)
    {
        if (pc == null || !pc.Is(CustomRoles.Mundane)) return true;

        return (pc.GetPlayerTaskState().IsTaskFinished);
    }
}