using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Paranoia
{
    private const int Id = 22400;

    public static OptionItem CanBeImp;
    public static OptionItem CanBeCrew;
    public static OptionItem DualVotes;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Paranoia, canSetNum: true);
        CanBeImp = BooleanOptionItem.Create(Id + 10, "ImpCanBeParanoia", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Paranoia]);
        CanBeCrew = BooleanOptionItem.Create(Id + 11, "CrewCanBeParanoia", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Paranoia]);
        DualVotes = BooleanOptionItem.Create(Id + 12, "DualVotes", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Paranoia]);
    }

    public static bool IsExistInGame(PlayerControl player) => player.Is(CustomRoles.Paranoia);
}

