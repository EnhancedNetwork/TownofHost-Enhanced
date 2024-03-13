using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Watcher
{
    private static readonly int Id = 20400;
    
    public static OptionItem ImpCanBeWatcher;
    public static OptionItem CrewCanBeWatcher;
    public static OptionItem NeutralCanBeWatcher;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Watcher, canSetNum: true);
        ImpCanBeWatcher = BooleanOptionItem.Create(Id + 10, "ImpCanBeWatcher", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Watcher]);
        CrewCanBeWatcher = BooleanOptionItem.Create(Id + 11, "CrewCanBeWatcher", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Watcher]);
        NeutralCanBeWatcher = BooleanOptionItem.Create(Id + 12, "NeutralCanBeWatcher", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Watcher]);
    }

    public static void RevealVotes(IGameOptions opt) => opt.SetBool(BoolOptionNames.AnonymousVotes, false);
}

