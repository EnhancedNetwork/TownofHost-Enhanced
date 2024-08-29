namespace TOHE.Roles.Core.AssignManager;

public static class GhostRoleAssign
{
    public static Dictionary<byte, CustomRoles> GhostGetPreviousRole = [];
    private static readonly Dictionary<CustomRoles, int> getCount = [];

    private static readonly IRandom Rnd = IRandom.Instance;
    private static bool GetChance(this CustomRoles role) => role.GetMode() == 100 || Rnd.Next(1, 100) <= role.GetMode();
    private static int ImpCount = 0;
    private static int CrewCount = 0;

    public static Dictionary<byte, CustomRoles> forceRole = [];

    private static readonly List<CustomRoles> HauntedList = [];
    private static readonly List<CustomRoles> ImpHauntedList = [];
    public static void GhostAssignPatch(PlayerControl player)
    {
        if (GameStates.IsHideNSeek  
            || Options.CurrentGameMode == CustomGameMode.FFA 
            || player == null 
            || player.Data.Disconnected 
            || GhostGetPreviousRole.ContainsKey(player.PlayerId)
            || player.HasDesyncRole()) return;
        if (forceRole.TryGetValue(player.PlayerId, out CustomRoles forcerole)) {
            Logger.Info($" Debug set {player.GetRealName()}'s role to {forcerole}", "GhostAssignPatch");
            player.GetRoleClass()?.OnRemove(player.PlayerId);
            player.RpcSetCustomRole(forcerole);
            player.GetRoleClass().OnAdd(player.PlayerId);
            forceRole.Remove(player.PlayerId);
            getCount[forcerole]--;
            return;
        }



        var getplrRole = player.GetCustomRole();
        if (getplrRole is CustomRoles.GM or CustomRoles.Nemesis or CustomRoles.Retributionist or CustomRoles.NiceMini) return;

        var IsNeutralAllowed = !player.IsAnySubRole(x => x.IsConverted()) || Options.ConvertedCanBecomeGhost.GetBool();
        var IsCrewmate = (getplrRole.IsCrewmate() || player.Is(CustomRoles.Admired)) && IsNeutralAllowed;
        var IsImpostor = (getplrRole.IsImpostor()) && (IsNeutralAllowed || player.Is(CustomRoles.Madmate));

        if (getplrRole.IsGhostRole() || player.IsAnySubRole(x => x.IsGhostRole() || x == CustomRoles.Gravestone) || !Options.CustomGhostRoleCounts.Any()) return;

        if (IsImpostor && ImpCount >= Options.MaxImpGhost.GetInt() || IsCrewmate && CrewCount >= Options.MaxCrewGhost.GetInt()) return;

            GhostGetPreviousRole.TryAdd(player.PlayerId, getplrRole);


        HauntedList.Clear();
        ImpHauntedList.Clear();

        CustomRoles ChosenRole = CustomRoles.NotAssigned;


        foreach (var ghostRole in getCount.Keys.Where(x => x.GetMode() > 0))
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
            if (HauntedList.Any())
            {
                var rnd = IRandom.Instance;
                int randindx = rnd.Next(HauntedList.Count);
                ChosenRole = HauntedList[randindx];

            }
            if (ChosenRole.IsGhostRole())
            {
                CrewCount++;
                getCount[ChosenRole]--; // Only deduct if role has been set.
                player.GetRoleClass().OnRemove(player.PlayerId);
                player.RpcSetCustomRole(ChosenRole);
                player.GetRoleClass().OnAdd(player.PlayerId);
            }
            return;
        }

        if (IsImpostor)
        {
            if (ImpHauntedList.Any())
            {
                var rnd = IRandom.Instance;
                int randindx = rnd.Next(ImpHauntedList.Count);
                ChosenRole = ImpHauntedList[randindx];

            }
            if (ChosenRole.IsGhostRole())
            {
                ImpCount++;
                getCount[ChosenRole]--;
                player.GetRoleClass().OnRemove(player.PlayerId);
                player.RpcSetCustomRole(ChosenRole);
                player.GetRoleClass().OnAdd(player.PlayerId);
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
        if (Options.CustomGhostRoleCounts.Any())
            Options.CustomGhostRoleCounts.Keys.Do(ghostRole
                => getCount.TryAdd(ghostRole, ghostRole.GetCount())); // Add new count Instance (Optionitem gets constantly refreshed)



        foreach (var role in getCount)
        {
            Logger.Info($"Logged: {role.Key} / {role.Value}", "GhostAssignPatch.Add.GetCount");
        }
    }
}
