using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Watcher
{
    private const int Id = 20400;
    
    public static OptionItem ImpCanBeWatcher;
    public static OptionItem CrewCanBeWatcher;
    public static OptionItem NeutralCanBeWatcher;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Watcher, canSetNum: true);
        ImpCanBeWatcher = BooleanOptionItem.Create("ImpCanBeWatcher", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Watcher]);
        CrewCanBeWatcher = BooleanOptionItem.Create("CrewCanBeWatcher", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Watcher]);
        NeutralCanBeWatcher = BooleanOptionItem.Create("NeutralCanBeWatcher", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Watcher]);
    }

    public static void RevealVotes(IGameOptions opt) => opt.SetBool(BoolOptionNames.AnonymousVotes, false);
}

