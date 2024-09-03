
namespace TOHE.Roles.AddOns.Impostor;

public class Circumvent : IAddon
{
    private const int Id = 22600;
    public AddonTypes Type => AddonTypes.Impostor;

    public void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Circumvent, canSetNum: true, tab: TabGroup.Addons);
    }

    public static bool CantUseVent(PlayerControl player) => player.Is(CustomRoles.Circumvent);
}