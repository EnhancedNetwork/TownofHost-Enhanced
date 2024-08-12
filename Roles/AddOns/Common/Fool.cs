using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Fool : IAddon
{
    private const int Id = 25600;
    public static bool IsEnable = false;
    public AddonTypes Type => AddonTypes.Harmful;
    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Fool, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
    }

    public void Init()
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

