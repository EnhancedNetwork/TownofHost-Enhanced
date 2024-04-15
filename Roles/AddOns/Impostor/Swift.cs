using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;

public static class Swift
{
    private static readonly int Id = 23300;
    
    public static void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Swift, canSetNum: true, tab: TabGroup.Addons);
    }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.Pestilence)) return false;
        target.RpcMurderPlayerV3(target);
        if (!DisableShieldAnimations.GetBool())
            killer.RpcGuardAndKill(killer);
        killer.SetKillCooldown();
        target.SetRealKiller(killer);
        RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
        return false;
    }
}