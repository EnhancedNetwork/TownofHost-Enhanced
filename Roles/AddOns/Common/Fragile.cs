using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Fragile : IAddon
{
    public CustomRoles Role => CustomRoles.Fragile;
    private const int Id = 20600;
    public AddonTypes Type => AddonTypes.Harmful;

    private static OptionItem ImpCanKillFragile;
    private static OptionItem CrewCanKillFragile;
    private static OptionItem NeutralCanKillFragile;
    private static OptionItem CovenCanKillFragile;
    private static OptionItem FragileKillerLunge;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Fragile, canSetNum: true, teamSpawnOptions: true);
        ImpCanKillFragile = BooleanOptionItem.Create(Id + 13, "ImpCanKillFragile", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        CrewCanKillFragile = BooleanOptionItem.Create(Id + 14, "CrewCanKillFragile", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        NeutralCanKillFragile = BooleanOptionItem.Create(Id + 15, "NeutralCanKillFragile", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        CovenCanKillFragile = BooleanOptionItem.Create(Id + 17, "CovenCanKillFragile", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        FragileKillerLunge = BooleanOptionItem.Create(Id + 16, "FragileKillerLunge", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    public static bool KillFragile(PlayerControl killer, PlayerControl target)
    {
        if (target == null || !target.IsAlive()) return false;
        var killerRole = killer.GetCustomRole();
        if ((killerRole.IsImpostorTeamV3() && ImpCanKillFragile.GetBool())
            || (killerRole.IsNeutral() && NeutralCanKillFragile.GetBool())
            || (killerRole.IsCrewmate() && CrewCanKillFragile.GetBool())
            || (killerRole.IsCoven() && CovenCanKillFragile.GetBool()))
        {
            target.SetDeathReason(PlayerState.DeathReason.Shattered);
            if (FragileKillerLunge.GetBool() && !killer.Is(CustomRoles.Swift))
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

