
namespace TOHE.Roles.AddOns.Impostor;

public class Circumvent : IAddon
{
    public CustomRoles Role => CustomRoles.Circumvent;
    private const int Id = 22600;
    public AddonTypes Type => AddonTypes.Impostor;

    public void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Circumvent, canSetNum: true, tab: TabGroup.Addons);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    public static bool CantUseVent(PlayerControl player) => player.Is(CustomRoles.Circumvent);
}
