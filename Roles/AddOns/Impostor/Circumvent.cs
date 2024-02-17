
namespace TOHE.Roles.AddOns.Impostor;

public static class Circumvent
{
    private static readonly int Id = 22600;

    public static void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Circumvent, canSetNum: true, tab: TabGroup.Addons);
    }

    public static bool CantUseVent(PlayerControl player) => player.Is(CustomRoles.Circumvent);
}