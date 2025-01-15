using TOHE.Roles.AddOns.Common;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;

public class Swift : IAddon
{
    public CustomRoles Role => CustomRoles.Swift;
    private const int Id = 23300;
    public AddonTypes Type => AddonTypes.Experimental;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Swift, canSetNum: true, tab: TabGroup.Addons);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (!DisableShieldAnimations.GetBool())
            killer.RpcGuardAndKill(killer);

        if (target.Is(CustomRoles.Trapper))
            killer.TrapperKilled(target);

        killer.SetKillCooldown();

        RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
        return false;
    }
}
