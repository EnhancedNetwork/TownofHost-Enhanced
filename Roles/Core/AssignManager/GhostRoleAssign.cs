using AmongUs.GameOptions;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles._Ghosts_.Impostor;
using TOHE.Roles.Crewmate;

namespace TOHE.Roles.Core.AssignManager;

public static class GhostRoleAssign
{
    public static Dictionary<byte, CustomRoles> GhostGetPreviousRole = [];
    private static Dictionary<CustomRoles, int> getCount = [];
    public static void GhostAssignPatch(PlayerControl player)
    {
        if (GameStates.IsHideNSeek || player == null || player.Data.Disconnected) return;

        var getplrRole = player.GetCustomRole();
        if (getplrRole is CustomRoles.GM) return;

        if (getplrRole.IsGhostRole() || player.IsAnySubRole(x => x.IsGhostRole()) || Options.CustomGhostRoleCounts.Count <= 0) return;
        
        GhostGetPreviousRole.Add(player.PlayerId, getplrRole);

        List<CustomRoles> HauntedList = [];
        List<CustomRoles> RateHauntedList = [];

        List<CustomRoles> ImpHauntedList = [];
        List<CustomRoles> ImpRateHauntedList = [];

        CustomRoles ChosenRole = CustomRoles.NotAssigned;

        bool IsSetRole = false;

        var IsCrewmate = getplrRole.IsCrewmate();
        var IsImpostor = getplrRole.IsImpostor();
        var IsNeutral = getplrRole.IsNeutral();

        foreach (var ghostRole in Options.CustomGhostRoleCounts.Keys) if (ghostRole.GetMode() == 2)
            {
                if (ghostRole.IsCrewmate())
                {
                    if (HauntedList.Contains(ghostRole) && getCount[ghostRole] <= 0)
                        HauntedList.Remove(ghostRole);

                    if (HauntedList.Contains(ghostRole) || getCount[ghostRole] <= 0)
                        continue;

                    HauntedList.Add(ghostRole); continue;
                }
                if (ghostRole.IsImpostor())
                {
                    if (ImpHauntedList.Contains(ghostRole) && getCount[ghostRole] <= 0)
                        ImpHauntedList.Remove(ghostRole);

                    if (ImpHauntedList.Contains(ghostRole) || getCount[ghostRole] <= 0)
                        continue;

                    ImpHauntedList.Add(ghostRole);
                }
            }
        foreach (var ghostRole in Options.CustomGhostRoleCounts.Keys) if (ghostRole.GetMode() == 1)
            {
                if (ghostRole.IsCrewmate())
                {
                    if (RateHauntedList.Contains(ghostRole) && getCount[ghostRole] <= 0)
                        RateHauntedList.Remove(ghostRole);

                    if (RateHauntedList.Contains(ghostRole) || getCount[ghostRole] <= 0)
                        continue;

                    RateHauntedList.Add(ghostRole); continue;
                }
                if (ghostRole.IsImpostor())
                {
                    if (ImpRateHauntedList.Contains(ghostRole) && getCount[ghostRole] <= 0)
                        ImpRateHauntedList.Remove(ghostRole);

                    if (ImpRateHauntedList.Contains(ghostRole) || getCount[ghostRole] <= 0)
                        continue;

                    ImpRateHauntedList.Add(ghostRole);
                }
            }

        if (IsCrewmate)
        {
            if (HauntedList.Count > 0)
            {
                var rnd = IRandom.Instance;
                int randindx = rnd.Next(HauntedList.Count);
                ChosenRole = HauntedList[randindx];
                IsSetRole = true;

            }
            else if (RateHauntedList.Count > 0 && !IsSetRole)
            {
                var rnd = IRandom.Instance;
                int randindx = rnd.Next(RateHauntedList.Count);
                ChosenRole = RateHauntedList[randindx];

            }
            if (ChosenRole.IsGhostRole())
            {
                getCount[ChosenRole]--; // Only deduct if role has been set.
                player.RpcSetRole(RoleTypes.GuardianAngel);
                player.RpcSetCustomRole(ChosenRole);
                player.AddPlayerId(ChosenRole);
                player.RpcResetAbilityCooldown();
            }
            return;
        }

        if (IsImpostor)
        {
            if (ImpHauntedList.Count > 0)
            {
                var rnd = IRandom.Instance;
                int randindx = rnd.Next(ImpHauntedList.Count);
                ChosenRole = ImpHauntedList[randindx];
                IsSetRole = true;

            }
            else if (ImpRateHauntedList.Count > 0 && !IsSetRole)
            {
                var rnd = IRandom.Instance;
                int randindx = rnd.Next(ImpRateHauntedList.Count);
                ChosenRole = ImpRateHauntedList[randindx];

            }
            if (ChosenRole.IsGhostRole())
            {
                getCount[ChosenRole]--;
                player.RpcSetRole(RoleTypes.GuardianAngel);
                player.RpcSetCustomRole(ChosenRole);
                player.AddPlayerId(ChosenRole);
                player.RpcResetAbilityCooldown();
            }
            return;
        }

        if (IsNeutral)
        {
            return;
        }

    }
    public static void Init() 
    {
        getCount = []; // Remove oldcount
    }
    public static void Add()
    {
        Options.CustomGhostRoleCounts.Keys.Do(ghostRole
            => getCount.Add(ghostRole, ghostRole.GetCount())); // Add new count Instance (Optionitem gets constantly refreshed)
    }
    public static void AddPlayerId(this PlayerControl target, CustomRoles GhostRole)
    {
        switch (GhostRole)
        {
            case CustomRoles.Retributionist:
                Retributionist.Add(target.PlayerId);
                break;
             case CustomRoles.Nemesis:
                Nemesis.Add(target.PlayerId);
                break;
            case CustomRoles.Warden:
                Warden.Add(target.PlayerId);
                break;
        }
    }
}
