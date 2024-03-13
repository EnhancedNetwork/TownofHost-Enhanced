using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Schizophrenic
{
    private static readonly int Id = 22400;

    public static OptionItem CanBeImp;
    public static OptionItem CanBeCrew;
    public static OptionItem DualVotes;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Schizophrenic, canSetNum: true);
        CanBeImp = BooleanOptionItem.Create(Id + 10, "ImpCanBeSchizophrenic", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Schizophrenic]);
        CanBeCrew = BooleanOptionItem.Create(Id + 11, "CrewCanBeSchizophrenic", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Schizophrenic]);
        DualVotes = BooleanOptionItem.Create(Id + 12, "DualVotes", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Schizophrenic]);
    }

    public static bool IsExistInGame(PlayerControl player) => player.Is(CustomRoles.Schizophrenic);
}

