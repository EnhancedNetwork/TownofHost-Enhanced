using AmongUs.GameOptions;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.AddOns.Common;
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
        if (GameStates.IsHideNSeek || player == null || player.Data.Disconnected || GhostGetPreviousRole.ContainsKey(player.PlayerId)) return;

        var getplrRole = player.GetCustomRole();
        if (getplrRole is CustomRoles.GM) return;

        var IsCrewmate = getplrRole.IsCrewmate() && !player.IsAnySubRole(x => x.IsConverted());
        var IsImpostor = getplrRole.IsImpostor() && !player.IsAnySubRole(x => x.IsConverted());
        var IsNeutral = getplrRole.IsNeutral();

        if (getplrRole.IsGhostRole() || player.IsAnySubRole(x => x.IsGhostRole() || x == CustomRoles.Gravestone) || Options.CustomGhostRoleCounts.Count <= 0) return;
        
        GhostGetPreviousRole.TryAdd(player.PlayerId, getplrRole);
        if (GhostGetPreviousRole.ContainsKey(player.PlayerId)) Logger.Info($"Succesfully added {player.GetRealName()}/{player.GetCustomRole()}", "GhostAssignPatch.GhostPreviousRole");
        else Logger.Warn($"Adding {player.GetRealName()} was unsuccessful", "GhostAssignPatch.GhostPreviousRole");

        List<CustomRoles> HauntedList = [];
        List<CustomRoles> ImpHauntedList = [];

        CustomRoles ChosenRole = CustomRoles.NotAssigned;


        foreach (var ghostRole in Options.CustomGhostRoleCounts.Keys.Where(x => x.GetMode() > 0))
        { 
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
                player.RpcSetCustomRole(ChosenRole);
                player.RpcSetRole(RoleTypes.GuardianAngel);
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
                player.RpcSetCustomRole(ChosenRole);
                player.RpcSetRole(RoleTypes.GuardianAngel);
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
        GhostGetPreviousRole = [];
    }
    public static void Add()
    {
        Options.CustomGhostRoleCounts.Keys.Do(ghostRole
            => getCount.TryAdd(ghostRole, ghostRole.GetCount())); // Add new count Instance (Optionitem gets constantly refreshed)
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
