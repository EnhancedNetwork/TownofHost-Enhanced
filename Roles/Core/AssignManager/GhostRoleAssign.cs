using AmongUs.GameOptions;
using HarmonyLib;
using MonoMod.Cil;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles._Ghosts_.Impostor;
using TOHE.Roles._Ghosts_.Crewmate;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;

namespace TOHE.Roles.Core.AssignManager;

public static class GhostRoleAssign
{
    public static Dictionary<byte, CustomRoles> GhostGetPreviousRole = [];
    private static readonly Dictionary<CustomRoles, int> getCount = [];

    private static readonly IRandom Rnd = IRandom.Instance;
    private static bool GetChance(this CustomRoles role) => role.GetMode() == 100 || Rnd.Next(1, 100) <= role.GetMode();
    private static int ImpCount = 0;
    private static int CrewCount = 0;

    private static readonly List<CustomRoles> HauntedList = [];
    private static readonly List<CustomRoles> ImpHauntedList = [];
    public static void GhostAssignPatch(PlayerControl player)
    {
        if (GameStates.IsHideNSeek || player == null || player.Data.Disconnected || GhostGetPreviousRole.ContainsKey(player.PlayerId)) return;

        var getplrRole = player.GetCustomRole();
        if (getplrRole is CustomRoles.GM or CustomRoles.Nemesis or CustomRoles.Retributionist) return;

        var IsNeutralAllowed = !player.IsAnySubRole(x => x.IsConverted() || x is CustomRoles.Madmate) || Options.ConvertedCanBecomeGhost.GetBool();
        var IsCrewmate = getplrRole.IsCrewmate() && IsNeutralAllowed;
        var IsImpostor = getplrRole.IsImpostor() && IsNeutralAllowed;

        if (getplrRole.IsGhostRole() || player.IsAnySubRole(x => x.IsGhostRole() || x == CustomRoles.Gravestone) || Options.CustomGhostRoleCounts.Count <= 0) return;

        if (IsImpostor && ImpCount >= Options.MaxImpGhost.GetInt() || IsCrewmate && CrewCount >= Options.MaxCrewGhost.GetInt()) return;

            GhostGetPreviousRole.TryAdd(player.PlayerId, getplrRole);

        HauntedList.Clear();
        ImpHauntedList.Clear();

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
                CrewCount++;
                getCount[ChosenRole]--; // Only deduct if role has been set.
                player.RpcSetCustomRole(ChosenRole);
                player.GetRoleClass().Add(player.PlayerId);
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
                ImpCount++;
                getCount[ChosenRole]--;
                player.RpcSetCustomRole(ChosenRole);
                player.GetRoleClass().Add(player.PlayerId);
            }
            return;
        }

    }
    public static void Init() 
    {
        CrewCount = 0;
        ImpCount = 0;
        getCount.Clear(); 
        GhostGetPreviousRole.Clear();
    }
    public static void Add()
    {
        Options.CustomGhostRoleCounts.Keys.Do(ghostRole
            => getCount.TryAdd(ghostRole, ghostRole.GetCount())); // Add new count Instance (Optionitem gets constantly refreshed)
    }
}
