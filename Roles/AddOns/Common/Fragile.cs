using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Fragile
{
    private const int Id = 20600;

    public static OptionItem ImpCanBeFragile;
    public static OptionItem CrewCanBeFragile;
    public static OptionItem NeutralCanBeFragile;
    public static OptionItem ImpCanKillFragile;
    private static OptionItem CrewCanKillFragile;
    private static OptionItem NeutralCanKillFragile;
    private static OptionItem FragileKillerLunge;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Fragile, canSetNum: true);
        ImpCanBeFragile = BooleanOptionItem.Create("ImpCanBeFragile", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        CrewCanBeFragile = BooleanOptionItem.Create("CrewCanBeFragile", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        NeutralCanBeFragile = BooleanOptionItem.Create("NeutralCanBeFragile", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        ImpCanKillFragile = BooleanOptionItem.Create("ImpCanKillFragile", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        CrewCanKillFragile = BooleanOptionItem.Create("CrewCanKillFragile", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        NeutralCanKillFragile = BooleanOptionItem.Create("NeutralCanKillFragile", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        FragileKillerLunge = BooleanOptionItem.Create("FragileKillerLunge", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
    }

    public static bool KillFragile(PlayerControl killer, PlayerControl target)
    {
        var killerRole = killer.GetCustomRole();
        if ((killerRole.IsImpostorTeamV3() && ImpCanKillFragile.GetBool())
            || (killerRole.IsNeutral() && NeutralCanKillFragile.GetBool())
            || (killerRole.IsCrewmate() && CrewCanKillFragile.GetBool()))
        {
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Shattered;
            if (FragileKillerLunge.GetBool())
            {
                killer.RpcMurderPlayer(target);
            }
            else
            {
                target.RpcMurderPlayer(target);
            }
            target.SetRealKiller(killer);
            killer.ResetKillCooldown();
            return true;
        }

        return false;
    }
}

