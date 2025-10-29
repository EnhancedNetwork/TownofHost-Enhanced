using Hazel;
using System.Text;

namespace TOHE.Roles.Core.AssignManager;

public static class GhostRoleAssign
{
    public static Dictionary<byte, CustomRoles> GhostGetPreviousRole = [];
    private static readonly Dictionary<CustomRoles, int> getCount = [];

    private static IRandom Rnd => IRandom.Instance;
    private static bool GetChance(this CustomRoles role) => role.GetMode() == 100 || Rnd.Next(1, 100) <= role.GetMode();
    private static int ImpCount = 0;
    private static int CrewCount = 0;
    private static int NeutralCount = 0;

    public static Dictionary<byte, CustomRoles> forceRole = [];

    private static readonly List<CustomRoles> HauntedList = [];
    private static readonly List<CustomRoles> ImpHauntedList = [];
    private static readonly List<CustomRoles> NeutHauntedList = [];
    public static void GhostAssignPatch(PlayerControl player)
    {
        if (GameStates.IsHideNSeek
            || Options.CurrentGameMode == CustomGameMode.FFA
            || Options.CurrentGameMode == CustomGameMode.CandR
            || Options.CurrentGameMode == CustomGameMode.UltimateTeam
            || Options.CurrentGameMode == CustomGameMode.TrickorTreat
            || player == null
            || player.Data == null
            || player.Data.Disconnected
            || GhostGetPreviousRole.ContainsKey(player.PlayerId)) return;

        if (forceRole.TryGetValue(player.PlayerId, out CustomRoles forcerole))
        {
            Logger.Info($" Debug set {player.GetRealName()}'s role to {forcerole}", "GhostAssignPatch");
            player.GetRoleClass()?.OnRemove(player.PlayerId);
            player.RpcSetCustomRole(forcerole);
            player.GetRoleClass().OnAdd(player.PlayerId);
            forceRole.Remove(player.PlayerId);
            getCount[forcerole]--;
            return;
        }

        var getplrRole = player.GetCustomRole();

        // Neutral Apocalypse can't get ghost roles
        if (getplrRole.IsNA() || getplrRole.IsTNA() && !Main.PlayerStates[player.PlayerId].IsNecromancer) return;

        // Coven Ghost Roles don't exist yet
        if (getplrRole.IsCoven() && !Main.PlayerStates[player.PlayerId].IsNecromancer) return;
        if (Main.PlayerStates[player.PlayerId].IsNecromancer)
        {
            GhostGetPreviousRole[player.PlayerId] = CustomRoles.Necromancer;
            return;
        }

        // Roles can win after death, should not get ghost roles
        if (getplrRole is CustomRoles.GM
            or CustomRoles.Nemesis
            or CustomRoles.Bankrupt
            or CustomRoles.Retributionist
            or CustomRoles.NiceMini
            or CustomRoles.Romantic
            or CustomRoles.Jester
            or CustomRoles.Follower
            or CustomRoles.Specter
            or CustomRoles.Sunnyboy
            or CustomRoles.Innocent
            or CustomRoles.Workaholic
            or CustomRoles.Cultist
            or CustomRoles.Lawyer
            or CustomRoles.Provocateur
            or CustomRoles.Virus
            or CustomRoles.Jackal
            or CustomRoles.Sidekick
            or CustomRoles.PlagueDoctor) return;

        var IsNeutralAllowed = !player.IsAnySubRole(x => x.IsConverted()) || Options.ConvertedCanBecomeGhost.GetBool();
        var CheckNeutral = player.GetCustomRole().IsNeutral() && Options.NeutralCanBecomeGhost.GetBool();
        var IsCrewmate = ((getplrRole.IsCrewmate() || player.Is(CustomRoles.Admired)) && IsNeutralAllowed) || CheckNeutral;
        var IsImpostor = (getplrRole.IsImpostor() && (IsNeutralAllowed || player.Is(CustomRoles.Madmate))) || CheckNeutral;
        var IsNeutral = (getplrRole.IsNeutral() && IsNeutralAllowed) || CheckNeutral;

        if (getplrRole.IsGhostRole() || player.IsAnySubRole(x => x.IsGhostRole() || x == CustomRoles.Gravestone) || !Options.CustomGhostRoleCounts.Any()) return;

        if (IsImpostor && ImpCount >= Options.MaxImpGhost.GetInt() || IsNeutral && NeutralCount >= Options.MaxNeutralGhost.GetInt() || IsCrewmate && CrewCount >= Options.MaxCrewGhost.GetInt()) return;

        GhostGetPreviousRole[player.PlayerId] = getplrRole;

        HauntedList.Clear();
        NeutHauntedList.Clear();
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
            if (ghostRole.IsNeutral())
            {
                if (NeutHauntedList.Contains(ghostRole) && getCount[ghostRole] <= 0)
                    NeutHauntedList.Remove(ghostRole);

                if (NeutHauntedList.Contains(ghostRole) || getCount[ghostRole] <= 0)
                    continue;

                if (ghostRole.GetChance()) NeutHauntedList.Add(ghostRole);
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
        if (IsNeutral)
        {
            if (NeutHauntedList.Any())
            {
                var rnd = IRandom.Instance;
                int randindx = rnd.Next(NeutHauntedList.Count);
                ChosenRole = NeutHauntedList[randindx];

            }
            if (ChosenRole.IsGhostRole())
            {
                NeutralCount++;
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
    public static void CreateGAMessage(PlayerControl __instance)
    {
        Utils.NotifyRoles(SpecifyTarget: __instance);
        _ = new LateTask(() =>
        {

            __instance.RpcResetAbilityCooldown();

            if (Options.SendRoleDescriptionFirstMeeting.GetBool())
            {
                var host = PlayerControl.LocalPlayer;
                var name = host.Data.PlayerName;
                var lp = __instance;
                var sb = new StringBuilder();
                var conf = new StringBuilder();
                var role = __instance.GetCustomRole();
                var rlHex = Utils.GetRoleColorCode(role);
                sb.Append(Utils.GetRoleTitle(role) + lp.GetRoleInfo(true));
                if (Options.CustomRoleSpawnChances.TryGetValue(role, out var opt))
                    Utils.ShowChildrenSettings(Options.CustomRoleSpawnChances[role], ref conf);
                var cleared = conf.ToString();
                conf.Clear().Append($"<size={ChatCommands.Csize}>" + $"<color={rlHex}>{Translator.GetString(role.ToString())} {Translator.GetString("Settings:")}</color>\n" + cleared + "</size>");

                var writer = CustomRpcSender.Create("SendGhostRoleInfo", SendOption.None);
                writer.StartMessage(__instance.GetClientId());
                {
                    writer.StartRpc(host.NetId, (byte)RpcCalls.SetName)
                        .Write(host.Data.NetId)
                        .Write(Utils.ColorString(Utils.GetRoleColor(role), Translator.GetString("GhostTransformTitle")))
                        .EndRpc();
                    writer.StartRpc(host.NetId, (byte)RpcCalls.SendChat)
                        .Write(sb.ToString())
                        .EndRpc();
                    writer.StartRpc(host.NetId, (byte)RpcCalls.SendChat)
                        .Write(conf.ToString())
                        .EndRpc();
                    writer.StartRpc(host.NetId, (byte)RpcCalls.SetName)
                        .Write(host.Data.NetId)
                        .Write(name)
                        .EndRpc();
                }
                writer.EndMessage();
                writer.SendMessage();

                // Utils.SendMessage(sb.ToString(), __instance.PlayerId, Utils.ColorString(Utils.GetRoleColor(role), GetString("GhostTransformTitle")));

            }

        }, 0.1f, $"SetGuardianAngel for playerId: {__instance.PlayerId}");
    }


}
