using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Mundane : IAddon
{
    private const int Id = 26700;
    public AddonTypes Type => AddonTypes.Harmful;

    public static OptionItem CanBeOnCrew;
    public static OptionItem CanBeOnNeutral;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Mundane, canSetNum: true, tab: TabGroup.Addons);
        CanBeOnCrew = BooleanOptionItem.Create(Id + 11, "CrewCanBeMundane", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mundane]);
        CanBeOnNeutral = BooleanOptionItem.Create(Id + 12, "NeutralCanBeMundane", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mundane]);
    }
    public static bool OnGuess(PlayerControl pc)
    {
        if (pc == null || !pc.Is(CustomRoles.Mundane)) return true;

        return (pc.GetPlayerTaskState().IsTaskFinished);
    }
}