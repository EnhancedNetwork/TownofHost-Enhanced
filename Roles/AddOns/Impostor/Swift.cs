using TOHE.Roles.AddOns.Common;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;

public static class Swift
{
    private const int Id = 23300;
    
    public static void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Swift, canSetNum: true, tab: TabGroup.Addons);
    }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {

        if (!DisableShieldAnimations.GetBool())
            killer.RpcGuardAndKill(killer);
        
        killer.SetKillCooldown();
        
        RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
        return false;
    }
}