using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Fool
{
    private static readonly int Id = 25600;
    public static bool IsEnable = false;

    public static OptionItem ImpCanBeFool;
    public static OptionItem CrewCanBeFool;
    public static OptionItem NeutralCanBeFool;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Fool, canSetNum: true, tab: TabGroup.Addons);
        ImpCanBeFool = BooleanOptionItem.Create(Id + 10, "ImpCanBeFool", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fool]);
        CrewCanBeFool = BooleanOptionItem.Create(Id + 11, "CrewCanBeFool", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fool]);
        NeutralCanBeFool = BooleanOptionItem.Create(Id + 12, "NeutralCanBeFool", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fool]);
    }

    public static void Init()
    {
        IsEnable = false;
    }
    public static void Add()
    {
        IsEnable = true;
    }

    public static bool BlockFixSabotage(PlayerControl player, SystemTypes systemType)
    {
        if (!player.Is(CustomRoles.Fool)) return false;

        if (!Main.MeetingIsStarted && systemType != SystemTypes.Sabotage &&
            (systemType is
                SystemTypes.Reactor or
                SystemTypes.Laboratory or
                SystemTypes.HeliSabotage or
                SystemTypes.LifeSupp or
                SystemTypes.Comms or
                SystemTypes.Electrical))
        {
            return true;
        }

        return false;
    }
}

