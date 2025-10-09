using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using AmongUs.GameOptions;
using Cpp2IL.Core.Extensions;
using Rewired;
using TOHE.Roles.Core.AssignManager;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using TOHE.Roles.Vanilla;

using static TOHE.Translator;

namespace TOHE.Roles.Core.DraftAssign;

public static class DraftAssign
{
    public static List<RoleSlot> UnassignedSlots = [];
    public static Dictionary<byte, RoleSlot> AssignedSlots = [];
    public static List<RoleAssign.RoleAssignInfo> Roles = [];
    public static Dictionary<byte, List<CustomRoles>> DraftPools = [];
    public static List<KeyValuePair<RoleSlot, List<CustomRoles>>> UnassignedDraftPools = [];
    public static Dictionary<byte, CustomRoles> DraftedRoles = [];

    public static bool DraftActive => AssignedSlots.Any();
    public static bool CanStartDraft => UnassignedSlots.Any();

    private static Dictionary<byte, CustomRoles> RoleResult => RoleAssign.RoleResult;

    public static DraftCmdResult StartDraft()
    {
        if (Options.CurrentGameMode != CustomGameMode.Standard) return DraftCmdResult.NoCurrentDraft;

        Reset();

        var rd = IRandom.Instance;
        int playerCount = Main.AllAlivePlayerControls.Length;

        foreach (var role in EnumHelper.GetAllValues<CustomRoles>())
        {
            // Logger.Info(role.ToString(), "LoadDraftRoles");
            int chance = role.GetMode();

            if (role.IsVanilla() || chance == 0 || role.IsAdditionRole() || role.IsGhostRole()) continue;
            switch (role)
            {
                case CustomRoles.Stalker when GameStates.FungleIsActive:
                case CustomRoles.Doctor when Options.EveryoneCanSeeDeathReason.GetBool():
                case CustomRoles.VengefulRomantic:
                case CustomRoles.RuthlessRomantic:
                case CustomRoles.GM:
                case CustomRoles.NotAssigned:
                case CustomRoles.NiceMini:
                case CustomRoles.EvilMini:
                case CustomRoles.Runner:
                case CustomRoles.PhantomTOHE when NarcManager.IsNarcAssigned():
                    continue;
            }

            int count = role.GetCount();
            RoleAssign.RoleAssignInfo info = new(role, chance, count);

            if (role is CustomRoles.Mini)
            {
                if (Mini.CheckSpawnEvilMini())
                {
                    info.Role = CustomRoles.EvilMini;
                }
                else
                {
                    info.Role = CustomRoles.NiceMini;
                }
            }

            Roles.Add(info);
            Logger.Info($"Added {info.Role} with chance {info.SpawnChance} and count {info.MaxCount}", "LoadDraftRoles");
        }

        if (!Options.DisableHiddenRoles.GetBool())
        {
            var roleInfo = Roles.FirstOrDefault(x => x.Role == CustomRoles.Jester);
            while (Sunnyboy.CheckSpawn() && Roles.Remove(roleInfo)) Roles.Add(new(CustomRoles.Sunnyboy, roleInfo.SpawnChance, roleInfo.MaxCount));
            roleInfo = Roles.FirstOrDefault(x => x.Role == CustomRoles.Arrogance);
            while (Bard.CheckSpawn() && Roles.Remove(roleInfo)) Roles.Add(new(CustomRoles.Bard, roleInfo.SpawnChance, roleInfo.MaxCount));
            roleInfo = Roles.FirstOrDefault(x => x.Role == CustomRoles.Knight);
            if (Requiter.CheckSpawn() && Roles.Remove(roleInfo)) Roles.Add(new(CustomRoles.Requiter, roleInfo.SpawnChance, roleInfo.MaxCount));
        }

        if (Romantic.HasEnabled)
        {
            var roleInfo = Roles.FirstOrDefault(x => x.Role == CustomRoles.Romantic);
            var roleInfo2 = Roles.FirstOrDefault(x => x.Role == CustomRoles.Lovers);
            if (Roles.Contains(roleInfo) && Roles.Contains(roleInfo2))
                Roles.Remove(roleInfo2);
        }

        LoadRoleBuckets();

        Logger.Msg("======================================================", "DraftBucketsLoaded");
        Logger.Info(string.Join(", ", UnassignedSlots), "LoadedSlots");
        Logger.Msg("======================================================", "DraftBucketsLoaded");

        var AllPlayers = Main.AllPlayerControls.ToList();

        // Players on the EAC banned list will be assigned as GM when opening rooms
        if (BanManager.CheckEACList(PlayerControl.LocalPlayer.FriendCode, PlayerControl.LocalPlayer.GetClient().GetHashedPuid()))
        {
            Logger.Warn("Host presets in BanManager.CheckEACList", "EAC");
            Main.EnableGM.Value = true;
            DraftedRoles[PlayerControl.LocalPlayer.PlayerId] = CustomRoles.GM;
            AllPlayers.Remove(PlayerControl.LocalPlayer);
        }

        foreach (var player in Main.AllPlayerControls)
        {
            if (player == null) continue;

            if (TagManager.AssignGameMaster(player.FriendCode))
            {
                Logger.Info($"Assign Game Master due to tag for [{player.PlayerId}]{player.name}", "TagManager");
                DraftedRoles[player.PlayerId] = CustomRoles.GM;
                RoleAssign.SetRoles.Remove(player.PlayerId);
                AllPlayers.Remove(player);
            }
            else if (Main.EnableGM.Value && player.IsHost())
            {
                DraftedRoles[PlayerControl.LocalPlayer.PlayerId] = CustomRoles.GM;
                RoleAssign.SetRoles.Remove(PlayerControl.LocalPlayer.PlayerId);
                AllPlayers.Remove(PlayerControl.LocalPlayer);
            }
        }

        // Pre-Assigned Roles By Host Are Selected First
        foreach (var item in RoleAssign.SetRoles)
        {
            PlayerControl pc = Utils.GetPlayerById(item.Key);
            if (pc == null) continue;

            DraftedRoles[item.Key] = item.Value;
            AllPlayers.Remove(pc);

            Logger.Warn($"Pre-Set Role Assigned: {pc.name} => {item.Value}", "RoleAssign");
        }

        playerCount = AllPlayers.Count;

        AssignRoleSlots(AllPlayers, rd);

        List<CustomRoles> AlwaysRoles = [];
        List<CustomRoles> ChanceRoles = [];

        for (int i = 0; i < Roles.Count; i++)
        {
            RoleAssign.RoleAssignInfo item = Roles[i];

            if (item.SpawnChance == 100)
            {
                for (int j = 0; j < item.MaxCount; j++)
                {
                    AlwaysRoles.Add(item.Role);
                }
            }
            else
            {
                for (int k = 0; k < item.MaxCount; k++)
                {
                    // Make "Spawn Chance ÷ 5 = x" (Example: 65 ÷ 5 = 13)
                    for (int j = 0; j < item.SpawnChance / 5; j++)
                    {
                        // Add roles 'x' times (13)
                        ChanceRoles.Add(item.Role);
                    }
                }
            }
        }

        AlwaysRoles = [.. AlwaysRoles.Shuffle(rd)];
        ChanceRoles = [.. ChanceRoles.Shuffle(rd)];

        Logger.Info($"{string.Join(", ", Roles.Select(x => x.Role))} {Roles.Count}", "Roles");
        Logger.Info($"{string.Join(", ", AlwaysRoles)} {AlwaysRoles.Count}", "AlwaysRoles");
        Logger.Info($"{string.Join(", ", ChanceRoles)} {ChanceRoles.Count}", "ChanceRoles");

        int poolCount = DraftPools.Count + UnassignedDraftPools.Count;

        int maxDraftRoles = Options.DraftableCount.GetInt();

        int draftPoolsMaxKey = DraftPools.Keys.Any() ? DraftPools.Keys.Max() : -1;

        List<int> PoolIds =
        [
            .. DraftPools.Keys,
            .. Enumerable.Range(draftPoolsMaxKey + 1, UnassignedDraftPools.Count)
        ];

        PoolIds = [.. PoolIds.Shuffle(rd)];

        Logger.Info("Always Roles Assign Started", "StartDraft");

        bool twiceOver = false;

        int slotId = 0;
        bool assignedAny = false;
        while (AlwaysRoles.Any())
        {
            var id = PoolIds[slotId];
            var slot = GetSlot(id);

            var slotRole = GetRoleFromSlot(AlwaysRoles, slot);

            var info = Roles.FirstOrDefault(x => x.Role == slotRole);

            if (slotRole != CustomRoles.NotAssigned)
            {
                assignedAny = true;
                GetPool(id).Add(slotRole);

                AlwaysRoles.Remove(slotRole);

                info.AssignedCount++;
                if (info.AssignedCount >= info.MaxCount) while (AlwaysRoles.Contains(slotRole)) AlwaysRoles.Remove(slotRole);
            }

            Logger.Info($"Assigned {slotRole} to pool {id}", "StartDraft");

            slotId++;
            if (slotId >= poolCount)
            {
                slotId = 0;
                if (DraftPools.Any(x => x.Value.Count >= maxDraftRoles) || !assignedAny)
                {
                    if (!twiceOver && assignedAny) twiceOver = true;
                    else if (!assignedAny) break;
                    else goto AfterPoolsAssigned;
                }
            }

            if (DraftPools.All(x => x.Value.Count >= maxDraftRoles)) goto AfterPoolsAssigned;
        }

        Logger.Info("Always Roles Assign Finished", "StartDraft");
        twiceOver = false;

        Dictionary<int, bool> hasRolesFound = PoolIds.ToDictionary(x => x, x => true);
        while (ChanceRoles.Any())
        {
            var id = PoolIds[slotId];
            var slot = GetSlot(id);

            var slotRole = GetRoleFromSlot(ChanceRoles, slot);

            if (slotRole != CustomRoles.NotAssigned)
            {
                GetPool(id).Add(slotRole);

                var info = Roles.FirstOrDefault(x => x.Role == slotRole);

                for (int j = 0; j < info.SpawnChance / 5; j++)
                    ChanceRoles.Remove(slotRole);

                info.AssignedCount++;
                if (info.AssignedCount >= info.MaxCount) while (ChanceRoles.Contains(slotRole)) ChanceRoles.Remove(slotRole);
            }
            else
            {
                hasRolesFound[id] = false;
            }

            slotId++;
            if (slotId >= poolCount)
            {
                slotId = 0;
                if (DraftPools.Any(x => x.Value.Count >= maxDraftRoles))
                {
                    if (!twiceOver) twiceOver = true;
                    else goto AfterPoolsAssigned;
                }
            }

            if (DraftPools.All(x => x.Value.Count >= maxDraftRoles)) goto AfterPoolsAssigned;
            if (hasRolesFound.Any(x => !x.Value)) break;
        }

        foreach (var id in hasRolesFound.Where(x => !x.Value))
        {
            var slot = GetSlot(id.Key);

            Logger.SendInGame($"Not enough enabled roles in slot {slot} to assign.");
        }

    AfterPoolsAssigned:

        Logger.Info("================================================", "PoolsAssigned");
        foreach (var pool in DraftPools)
        {
            Logger.Info($"Pool [{string.Join(", ", pool.Value)}] assigned to {pool.Key.GetPlayer().GetRealName}", "PoolAssigned");
        }
        foreach (var pool in UnassignedDraftPools)
        {
            Logger.Info($"Pool [{string.Join(", ", pool.Value)}] not assigned yet", "PoolAssigned");
        }
        Logger.Info("================================================", "PoolsAssigned");

        return DraftCmdResult.Success;

        List<CustomRoles> GetPool(int index)
        {
            int draftPoolsMaxKey = DraftPools.Keys.Any() ? DraftPools.Keys.Max() : -1;
            if (DraftPools.ContainsKey((byte)index))
            {
                return DraftPools[(byte)index];
            }
            else if (index >= draftPoolsMaxKey && index - draftPoolsMaxKey < UnassignedDraftPools.Count)
            {
                return UnassignedDraftPools[index - draftPoolsMaxKey].Value;
            }
            return null;
        }

        RoleSlot GetSlot(int index)
        {
            if (AssignedSlots.ContainsKey((byte)index))
            {
                return AssignedSlots[(byte)index];
            }
            else if (index >= AssignedSlots.Keys.Max() && index - AssignedSlots.Keys.Max() < UnassignedSlots.Count)
            {
                return UnassignedSlots[index - AssignedSlots.Keys.Max()];
            }
            throw new IndexOutOfRangeException($"Slot Index {index} out of range of slot count {AssignedSlots.Count + UnassignedSlots.Count}");
        }

        CustomRoles GetRoleFromSlot(List<CustomRoles> roles, RoleSlot slot)
        {
            var rolesFound = roles.Where(x => x.IsInRoleSlot(slot)).ToList();
            // Logger.Info($"Found {string.Join(",", rolesFound)} for slot {slot}", "StartDraft");
            return rolesFound.FirstOrDefault(defaultValue: CustomRoles.NotAssigned);
        }
    }

    private const string ROLEDECK_FOLDER_NAME = "TOHE-DATA/RoleDecks";
    public static void LoadRoleDecks()
    {
        RoleDecks.Clear();
        if (!Directory.Exists(ROLEDECK_FOLDER_NAME)) Directory.CreateDirectory(ROLEDECK_FOLDER_NAME);
        DirectoryInfo deckDir = new(ROLEDECK_FOLDER_NAME);

        var buckets = EnumHelper.GetAllValues<RoleBucket>().Select(x => x.ToString().ToLower());
        Logger.Info($"Buckets: {string.Join(",", buckets)}", "LoadRoleDecks");

        foreach (var deck in deckDir.GetFiles("*.roledeck"))
        {
            LoadRoleDeck(deck.Name, deck.FullName);
        }
    }

    public static readonly Dictionary<string, List<RoleSlot>> RoleDecks = [];
    private static void LoadRoleDeck(string fileName, string path)
    {
        string deckName = fileName.Replace(".roledeck", "");
        List<RoleSlot> slots = [];
        using StreamReader reader = new(path, Encoding.GetEncoding("UTF-8"));
        string line;

        while ((line = reader.ReadLine()) != null)
        {
            string[] parts = line.Split('|');
            RoleSlot slot = new([], []);
            foreach (var part in parts)
            {
                string p = part.Trim().ToLower().Replace(" ", "");
                RoleBucket bucket = EnumHelper.GetAllValues<RoleBucket>().FirstOrDefault(x => x.ToString().ToLower() == p, RoleBucket.None);
                if (bucket != RoleBucket.None)
                {
                    slot.Buckets.Add(bucket);
                    continue;
                }

                CustomRoles role = ChatCommands.ParseRole(p);
                if (role != CustomRoles.NotAssigned)
                {
                    slot.Roles.Add(role);
                    continue;
                }
                Logger.Warn($"No bucket or role found for: {part}", "LoadRoleDeck");
            }
            slots.Add(slot);
        }

        RoleDecks[deckName] = slots;
    }

    private static void LoadRoleBuckets()
    {
        UnassignedSlots.Clear();

        string deck = Options.DraftDeck.GetString();

        var slots = RoleDecks[deck];

        foreach (var slot in slots)
        {
            if (!slot.Buckets.Any() && !slot.Roles.Any())
            {
                Logger.SendInGame($"One or more lines of deck {deck} are empty.");
                return;
            }
        }

        UnassignedSlots.AddRange(slots);
    }

    public static string ToColoredString(this RoleBucket bucket)
        => bucket switch
        {
            RoleBucket.ImpostorCommon or RoleBucket.ImpostorRandom or RoleBucket.ImpostorHindering
                or RoleBucket.ImpostorConcealing or RoleBucket.ImpostorSupport or RoleBucket.ImpostorKilling
                    => CustomRoles.Impostor.GetColoredTextByRole(GetString($"RoleBucket.{bucket}")),

            RoleBucket.CrewmateBasic or RoleBucket.CrewmateCommon or RoleBucket.CrewmateKilling
                or RoleBucket.CrewmatePower or RoleBucket.CrewmateRandom or RoleBucket.CrewmateSupport
                    => CustomRoles.Crewmate.GetColoredTextByRole(GetString($"RoleBucket.{bucket}")),

            RoleBucket.CovenCommon or RoleBucket.CovenKilling or RoleBucket.CovenPower
                or RoleBucket.CovenRandom or RoleBucket.CovenTrickery or RoleBucket.CovenUtility
                    => CustomRoles.Coven.GetColoredTextByRole(GetString($"RoleBucket.{bucket}")),

            RoleBucket.NeutralBenign => CustomRoles.Amnesiac.GetColoredTextByRole(GetString($"RoleBucket.{bucket}")),
            RoleBucket.NeutralPariah => CustomRoles.Pariah.GetColoredTextByRole(GetString($"RoleBucket.{bucket}")),
            RoleBucket.NeutralChaos => CustomRoles.Pirate.GetColoredTextByRole(GetString($"RoleBucket.{bucket}")),
            RoleBucket.NeutralApocalypse => CustomRoles.Apocalypse.GetColoredTextByRole(GetString($"RoleBucket.{bucket}")),
            RoleBucket.NeutralEvil => CustomRoles.Jester.GetColoredTextByRole(GetString($"RoleBucket.{bucket}")),
            RoleBucket.NeutralKilling => CustomRoles.Traitor.GetColoredTextByRole(GetString($"RoleBucket.{bucket}")),
            RoleBucket.NeutralRandom => CustomRoles.Executioner.GetColoredTextByRole(GetString($"RoleBucket.{bucket}")),

            _ => CustomRoles.Crewmate.GetColoredTextByRole(GetString($"RoleBucket.{bucket}"))
        };

    private static void AssignRoleSlots(List<PlayerControl> AllPlayers, IRandom rnd)
    {
        int playerCount = AllPlayers.Count;

        if (playerCount > UnassignedSlots.Count)
        {
            Logger.Warn("Not enough role slots set, adding crewmates.", "AssignRoleSlots");

            while (playerCount > UnassignedSlots.Count)
            {
                UnassignedSlots.Add(new([], [CustomRoles.CrewmateTOHE]));
            }
        }

        AllPlayers = [.. AllPlayers.Shuffle(rnd).Shuffle(rnd)];

        foreach (var player in AllPlayers)
        {
            if (UnassignedSlots.Any())
            {
                var slot = UnassignedSlots[0];

                AssignedSlots.Add(player.PlayerId, slot);
                DraftPools.Add(player.PlayerId, []);

                UnassignedSlots.RemoveAt(0);
            }
            else
            {
                Logger.Error("Not enough role slots to give some to all players.", "AssignRoleSlots");
            }
        }

        foreach (var slot in UnassignedSlots)
        {
            UnassignedDraftPools.Add(new(slot, []));
        }
    }

    public static void StartSelect()
    {
        switch (Options.CurrentGameMode)
        {
            case CustomGameMode.FFA:
                foreach (PlayerControl pc in Main.AllPlayerControls)
                {
                    if (Main.EnableGM.Value && pc.IsHost())
                    {
                        RoleResult[pc.PlayerId] = CustomRoles.GM;
                        continue;
                    }
                    else if (TagManager.AssignGameMaster(pc.FriendCode))
                    {
                        RoleResult[pc.PlayerId] = CustomRoles.GM;
                        Logger.Info($"Assign Game Master due to tag for [{pc.PlayerId}]{pc.name}", "TagManager");
                        continue;
                    }
                    RoleResult[pc.PlayerId] = CustomRoles.Killer;
                }
                return;

            case CustomGameMode.SpeedRun:
                foreach (PlayerControl pc in Main.AllPlayerControls)
                {
                    if (Main.EnableGM.Value && pc.IsHost())
                    {
                        RoleResult[pc.PlayerId] = CustomRoles.GM;
                        continue;
                    }
                    else if (TagManager.AssignGameMaster(pc.FriendCode))
                    {
                        RoleResult[pc.PlayerId] = CustomRoles.GM;
                        Logger.Info($"Assign Game Master due to tag for [{pc.PlayerId}]{pc.name}", "TagManager");
                        continue;
                    }
                    RoleResult[pc.PlayerId] = CustomRoles.Runner;
                }
                return;
        }

        if (!CanStartDraft)
        {
            RoleAssign.StartSelect();
            return;
        }
        Logger.Info("Started draft assign roles.", "DraftAssign");
        var rd = IRandom.Instance;
        int playerCount = Main.AllAlivePlayerControls.Length;

        var AllPlayers = Main.AllAlivePlayerControls;

        List<CustomRoles> FinalRolesList = [];

        foreach (var id in DraftedRoles.Keys.Where(id => Utils.GetPlayerById(id) == null).ToArray()) DraftedRoles.Remove(id);

        foreach (var id in AssignedSlots.Keys.Where(id => Utils.GetPlayerById(id) == null).ToArray())
        {
            var pool = DraftPools[id];
            UnassignedDraftPools.Add(new(AssignedSlots[id], pool));
            DraftPools.Remove(id);

            UnassignedSlots.Add(AssignedSlots[id]);
            AssignedSlots.Remove(id);
        }

        foreach (var player in AllPlayers)
        {
            byte playerId = player.PlayerId;
            // Assign Role if drafted
            if (DraftedRoles.TryGetValue(playerId, out CustomRoles drafted))
            {
                RoleResult[playerId] = drafted;
            }
            // Assign role if not drafted
            else if (DraftPools.TryGetValue(playerId, out var pool))
            {
                pool = [.. pool.Shuffle(rd)];

                RoleResult[playerId] = pool.First();
            }
            // Assign role if draft bucket not assigned
            else if (UnassignedDraftPools.Any())
            {
                var role = UnassignedDraftPools.Shuffle(rd).First().Value.Shuffle(rd).First();

                RoleResult[playerId] = role;
            }
            // Assign crewmate if no unassigned roles left
            else
            {
                RoleResult[playerId] = CustomRoles.CrewmateTOHE;
            }
        }

        return;
    }

    public static DraftCmdResult AddPlayersToDraft()
    {
        if (!DraftActive) return DraftCmdResult.NoCurrentDraft;

        var rd = IRandom.Instance;

        // foreach player that doesn't have a bucket/role assigned
        foreach (var player in Main.AllAlivePlayerControls.ExceptBy(AssignedSlots.Keys, x => x.PlayerId))
        {
            var unassigned = UnassignedSlots.Shuffle(rd).ToList();

            var toAssign = unassigned.First();

            UnassignedSlots.Remove(toAssign);
            AssignedSlots.Add(player.PlayerId, toAssign);

            var pool = UnassignedDraftPools.FirstOrDefault(x => x.Key == toAssign);

            DraftPools.Add(player.PlayerId, pool.Value);
            UnassignedDraftPools.Remove(pool);

            Logger.Info($"Pool [{string.Join(", ", pool.Value)}] assigned to {player.name}", "PoolAssigned");
        }
        return DraftCmdResult.Success;
    }

    public static void Reset()
    {
        UnassignedSlots.Clear();
        AssignedSlots.Clear();
        Roles = [];
        DraftPools = [];
        UnassignedDraftPools = [];
        DraftedRoles = [];
    }

    public static List<CustomRoles> GetDraftPool(this PlayerControl player) => DraftPools[player.PlayerId];

    public static string GetFormattedDraftPool(this PlayerControl player)
    {
        StringBuilder sb = new();

        int i = 1;
        foreach (var role in player.GetDraftPool())
        {
            sb.Append('\n');
            sb.Append(i++).Append(". ");
            sb.Append(role.ToColoredString());
        }

        return sb.ToString();
    }

    [Obfuscation(Exclude = true)]
    public enum DraftCmdResult
    {
        Success,
        NoCurrentDraft,
        DraftRemoved
    }
    public static (DraftCmdResult, CustomRoles) DraftRole(this PlayerControl player, int index)
    {
        if (DraftPools.TryGetValue(player.PlayerId, out var pool))
        {
            if (pool.Count > index - 1)
            {
                DraftedRoles[player.PlayerId] = pool[index - 1];
                return (DraftCmdResult.Success, pool[index - 1]);
            }
            else
            {
                DraftedRoles.Remove(player.PlayerId);
                return (DraftCmdResult.DraftRemoved, CustomRoles.NotAssigned);
            }
        }
        else
        {
            return (DraftCmdResult.NoCurrentDraft, CustomRoles.NotAssigned);
        }
    }

    public static (DraftCmdResult, CustomRoles) DraftRole(this PlayerControl player, CustomRoles role)
    {
        if (DraftPools.TryGetValue(player.PlayerId, out var pool))
        {
            if (pool.Contains(role))
            {
                DraftedRoles[player.PlayerId] = role;
                return (DraftCmdResult.Success, role);
            }
            else
            {
                DraftedRoles.Remove(player.PlayerId);
                return (DraftCmdResult.DraftRemoved, CustomRoles.NotAssigned);
            }
        }
        else
        {
            return (DraftCmdResult.NoCurrentDraft, CustomRoles.NotAssigned);
        }
    }

    public static void SendDraftDescription(this PlayerControl player, int index)
    {
        byte playerId = player.PlayerId;
        var result = CustomRoles.NotAssigned;
        if (DraftPools.TryGetValue(player.PlayerId, out List<CustomRoles> pool))
        {
            if (index < pool.Count)
                result = pool[index];
        }

        if (result == CustomRoles.NotAssigned)
            return;

        var Des = result.GetInfoLong();
        var title = "▲" + $"<color=#ffffff>" + result.GetRoleTitle() + "</color>\n";
        var Conf = new StringBuilder();
        string rlHex = Utils.GetRoleColorCode(result);
        if (Options.CustomRoleSpawnChances.ContainsKey(result))
        {
            Utils.ShowChildrenSettings(Options.CustomRoleSpawnChances[result], ref Conf);
            var cleared = Conf.ToString();
            var Setting = $"<color={rlHex}>{GetString(result.ToString())} {GetString("Settings:")}</color>\n";
            Conf.Clear().Append($"<color=#ffffff>" + $"<size={ChatCommands.Csize}>" + Setting + cleared + "</size>" + "</color>");

        }
        // Show role info
        Utils.SendMessage(Des, playerId, title, noReplay: true);

        // Show role settings
        Utils.SendMessage("", playerId, Conf.ToString(), noReplay: true);
    }

    public static void SendDeckList(this PlayerControl player)
    {
        string deckName = Options.DraftDeck.GetString();
        var deck = RoleDecks[deckName];

        var title = "▲" + $"<color=#ffffff>" + deckName + "</color>\n";

        string template = GetString("DraftDeckTemplate");

        var slots = deck.Select(x => x.ToColoredString()).ToList();
        var slotsFormatted = string.Join("\n- ", slots);

        Utils.SendMessage(string.Format(template, slotsFormatted), player.PlayerId, title, noReplay: true);
    }
}

public class RoleSlot(HashSet<RoleBucket> buckets, HashSet<CustomRoles> roles)
{
    public HashSet<RoleBucket> Buckets = buckets;
    public HashSet<CustomRoles> Roles = roles;

    public List<string> GetStrings()
    {
        List<string> result = [];
        Buckets.ForEach(x => result.Add(x.ToString()));
        Roles.ForEach(x => result.Add(x.ToString()));
        return result;
    }
    public List<string> GetColoredStrings()
    {
        List<string> result = [];
        Buckets.ForEach(x => result.Add(x.ToColoredString()));
        Roles.ForEach(x => result.Add(x.ToColoredString()));
        return result;
    }
    public override string ToString()
    {
        return string.Join("|", GetStrings());
    }
    public string ToColoredString()
    {
        return string.Join("|", GetColoredStrings());
    }
}