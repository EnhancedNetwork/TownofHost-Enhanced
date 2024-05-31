using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Gravestone
{
    private const int Id = 22100;
    
    public static OptionItem ImpCanBeGravestone;
    public static OptionItem CrewCanBeGravestone;
    public static OptionItem NeutralCanBeGravestone;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Gravestone, canSetNum: true);
        ImpCanBeGravestone = BooleanOptionItem.Create("ImpCanBeGravestone", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Gravestone]);
        CrewCanBeGravestone = BooleanOptionItem.Create("CrewCanBeGravestone", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Gravestone]);
        NeutralCanBeGravestone = BooleanOptionItem.Create("NeutralCanBeGravestone", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Gravestone]);
    }
    public static bool EveryoneKnowRole(PlayerControl player) => player.Is(CustomRoles.Gravestone) && !player.IsAlive();
}

