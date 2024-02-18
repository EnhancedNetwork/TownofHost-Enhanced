using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Fragile
{
    private static readonly int Id = 20600;

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
        ImpCanBeFragile = BooleanOptionItem.Create(Id + 10, "ImpCanBeFragile", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        CrewCanBeFragile = BooleanOptionItem.Create(Id + 11, "CrewCanBeFragile", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        NeutralCanBeFragile = BooleanOptionItem.Create(Id + 12, "NeutralCanBeFragile", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        ImpCanKillFragile = BooleanOptionItem.Create(Id + 13, "ImpCanKillFragile", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        CrewCanKillFragile = BooleanOptionItem.Create(Id + 14, "CrewCanKillFragile", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        NeutralCanKillFragile = BooleanOptionItem.Create(Id + 15, "NeutralCanKillFragile", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        FragileKillerLunge = BooleanOptionItem.Create(Id + 16, "FragileKillerLunge", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
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
                killer.RpcMurderPlayerV3(target);
            }
            else
            {
                target.RpcMurderPlayerV3(target);
            }
            target.SetRealKiller(killer);
            killer.ResetKillCooldown();
            return true;
        }

        return false;
    }
}

