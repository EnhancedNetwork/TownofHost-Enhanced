using AmongUs.GameOptions;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;

namespace TOHE.Roles.Core.AssignManager;

public static class GhostRoleAssign
{
    public static Dictionary<byte, CustomRoles> GhostGetPreviousRole = [];
    private static Dictionary<CustomRoles, int> getCount = [];

    private static readonly IRandom Rnd = IRandom.Instance;
    private static bool GetChance(this CustomRoles role) => role.GetMode() == 100 || Rnd.Next(1, 100) <= role.GetMode();

    public static void GhostAssignPatch(PlayerControl player)
    {
        if (GameStates.IsHideNSeek || player == null || player.Data.Disconnected) return;

        var getplrRole = player.GetCustomRole();
        if (getplrRole is CustomRoles.GM or CustomRoles.Nemesis or CustomRoles.Retributionist) return;

        if (getplrRole.IsGhostRole() || player.IsAnySubRole(x => x.IsGhostRole()) || Options.CustomGhostRoleCounts.Count <= 0) return;
        
        GhostGetPreviousRole.Add(player.PlayerId, getplrRole);

        List<CustomRoles> HauntedList = [];
        List<CustomRoles> ImpHauntedList = [];

        CustomRoles ChosenRole = CustomRoles.NotAssigned;

        var IsCrewmate = getplrRole.IsCrewmate();
        var IsImpostor = getplrRole.IsImpostor();
        var IsNeutral = getplrRole.IsNeutral();

        foreach (var ghostRole in Options.CustomGhostRoleCounts.Keys.Where(x => x.GetMode() > 0))
        { 
            // For each time a player dies, a ghostrole will have another shot at getting in.
            // Imagine 3 groups: "I want first!" 100-75% // "I want mid-game" 75-45% // "Final Savior" 45%-0%. 
            if (ghostRole.IsCrewmate())
            {
                if (HauntedList.Contains(ghostRole) && getCount[ghostRole] <= 0)
                        HauntedList.Remove(ghostRole);

                if (HauntedList.Contains(ghostRole) || getCount[ghostRole] <= 0)
                        continue;
                    
                if (ghostRole.GetChance()) HauntedList.Add(ghostRole); 
            }
            if (ghostRole.IsImpostor())
            {
                if (ImpHauntedList.Contains(ghostRole) && getCount[ghostRole] <= 0)
                        ImpHauntedList.Remove(ghostRole);

                if (ImpHauntedList.Contains(ghostRole) || getCount[ghostRole] <= 0)
                        continue;

                if (ghostRole.GetChance()) ImpHauntedList.Add(ghostRole); 
            }
        }

        if (IsCrewmate)
        {
            if (HauntedList.Count > 0)
            {
                var rnd = IRandom.Instance;
                int randindx = rnd.Next(HauntedList.Count);
                ChosenRole = HauntedList[randindx];

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
            case CustomRoles.Hawk:
                Hawk.Add(target.PlayerId);
                break;
             case CustomRoles.Bloodmoon:
                Bloodmoon.Add(target.PlayerId);
                break;
            case CustomRoles.Warden:
                Warden.Add(target.PlayerId);
                break;
        }
    }
}
