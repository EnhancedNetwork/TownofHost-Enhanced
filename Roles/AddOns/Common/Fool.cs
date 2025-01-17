using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Fool : IAddon
{
    public CustomRoles Role => CustomRoles.Fool;
    private const int Id = 25600;
    public static bool IsEnable = false;
    public AddonTypes Type => AddonTypes.Harmful;

    private static readonly HashSet<byte> playerList = [];
    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Fool, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
    }

    public void Init()
    {
        IsEnable = false;
        playerList.Clear();
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    {
        playerList.Add(playerId);
        IsEnable = true;
    }
    public void Remove(byte playerId)
    {
        playerList.Remove(playerId);

        if (!playerList.Any())
            IsEnable = false;
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

