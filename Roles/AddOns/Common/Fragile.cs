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
            if (killer.Is(CustomRoles.FragileHunter))
            {
                if (!CustomWinnerHolder.CheckForConvertedWinner(killer.PlayerId))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.FragileHunter);
                    CustomWinnerHolder.WinnerIds.Add(killer.PlayerId);
                }
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (pc.PlayerId != killer.PlayerId)
                    {
                        var deathReason = pc.PlayerId == killer.PlayerId ?
                            PlayerState.DeathReason.Overtired : PlayerState.DeathReason.Shattered;

                        pc.SetDeathReason(deathReason);
                        pc.RpcMurderPlayer(pc);
                        pc.SetRealKiller(killer);
                    }
                }
            }
            else
            {
                Main.PlayerStates[killer.PlayerId].RemoveSubRole(CustomRoles.FragileHunter);
            }
            return true;
        }

        return false;
    }
}

