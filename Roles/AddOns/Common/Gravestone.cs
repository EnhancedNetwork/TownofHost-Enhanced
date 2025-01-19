using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Gravestone : IAddon
{
    public CustomRoles Role => CustomRoles.Gravestone;
    private const int Id = 22100;
    public AddonTypes Type => AddonTypes.Mixed;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Gravestone, canSetNum: true, teamSpawnOptions: true);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    public static bool EveryoneKnowRole(PlayerControl player) => player.Is(CustomRoles.Gravestone) && !player.IsAlive();
}

