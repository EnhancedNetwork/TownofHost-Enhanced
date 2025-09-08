using System;
using System.Text;
using System.Text.RegularExpressions;
using AmongUs.GameOptions;
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
    public static List<RoleBucket> UnassignedBuckets = [];
    public static List<CustomRoles> UnassignedRoles = [];
    public static Dictionary<byte, RoleBucket> AssignedBuckets = [];
    public static Dictionary<byte, CustomRoles> AssignedRoles = [];
    public static List<RoleAssign.RoleAssignInfo> Roles = [];
    public static Dictionary<byte, List<CustomRoles>> DraftPools = [];
    public static List<KeyValuePair<RoleBucket, List<CustomRoles>>> UnassignedDraftPools = [];
    public static Dictionary<byte, CustomRoles> DraftedRoles = [];

    public static bool DraftActive => AssignedBuckets.Any() || AssignedRoles.Any();

    private static Dictionary<byte, CustomRoles> RoleResult => RoleAssign.RoleResult;

    public static DraftCmdResult StartDraft()
    {
        if (Options.CurrentGameMode != CustomGameMode.Standard) return DraftCmdResult.NoCurrentDraft;

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
        Logger.Info(string.Join(", ", UnassignedBuckets), "LoadedBuckets");
        Logger.Info(string.Join(", ", UnassignedRoles), "LoadedRoles");
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

            if (UnassignedRoles.Contains(item.Value))
            {
                UnassignedRoles.Remove(item.Value);
            }

            Logger.Warn($"Pre-Set Role Assigned: {pc.name} => {item.Value}", "RoleAssign");
        }

        playerCount = AllPlayers.Count;

        AssignRoleBuckets(AllPlayers, rd);

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

        RoleAssign.RoleAssignInfo[] RoleCounts = AlwaysRoles.Distinct().Select(GetAssignInfo).ToArray().AddRangeToArray([.. ChanceRoles.Distinct().Select(GetAssignInfo)]);

        int bucketCount = DraftPools.Count;

        int maxDraftRoles = Options.DraftableCount.GetInt();

        List<int> PoolIds =
        [
            .. DraftPools.Keys,
            .. Enumerable.Range(DraftPools.Keys.Max(), UnassignedDraftPools.Count)
        ];

        int bucketId = 0;
        while (AlwaysRoles.Any())
        {
            var id = PoolIds[bucketId];
            var bucket = GetBucket(id);

            var bucketRole = AlwaysRoles.FirstOrDefault(x => x.IsInRoleBucket(bucket), CustomRoles.NotAssigned);

            var info = RoleCounts.FirstOrDefault(x => x.Role == bucketRole);

            if (bucketRole != CustomRoles.NotAssigned)
            {
                GetPool(id).Add(bucketRole);

                AlwaysRoles.Remove(bucketRole);
            }
            else
            {
                Logger.Warn($"Not enough enabled roles in bucket {bucket} to assign.", "StartDraft");
                GetPool(id).Add(CustomRoles.NotAssigned);
            }

            info.AssignedCount++;
            if (info.AssignedCount >= info.MaxCount) while (ChanceRoles.Contains(bucketRole)) AlwaysRoles.Remove(bucketRole);

            bucketId++;
            if (bucketId >= bucketCount) bucketId = 0;

            if (DraftPools.All(x => x.Value.Count >= maxDraftRoles)) goto AfterPoolsAssigned;
        }

        while (ChanceRoles.Any())
        {
            var id = PoolIds[bucketId];
            var bucket = GetBucket(id);

            var bucketRole = ChanceRoles.FirstOrDefault(x => x.IsInRoleBucket(bucket), CustomRoles.NotAssigned);

            if (bucketRole != CustomRoles.NotAssigned)
            {
                GetPool(id).Add(bucketRole);

                var info = RoleCounts.FirstOrDefault(x => x.Role == bucketRole);

                for (int j = 0; j < info.SpawnChance / 5; j++)
                    ChanceRoles.Remove(bucketRole);

                info.AssignedCount++;

                if (info.AssignedCount >= info.MaxCount) while (ChanceRoles.Contains(bucketRole)) ChanceRoles.Remove(bucketRole);
            }
            else
            {
                Logger.Warn($"Not enough enabled roles in bucket {bucket} to assign.", "StartDraft");
                GetPool(id).Add(CustomRoles.NotAssigned);
            }

            bucketId++;
            if (bucketId >= bucketCount) bucketId = 0;

            if (DraftPools.All(x => x.Value.Count >= maxDraftRoles)) goto AfterPoolsAssigned;
        }

    AfterPoolsAssigned:

        Logger.Info("================================================", "PoolsAssigned");
        foreach (var pool in DraftPools)
        {
            Logger.Info($"Pool [{string.Join(", ", pool.Value)}] assigned to {pool.Key.GetPlayer().name}", "PoolAssigned");
        }
        foreach (var pool in UnassignedDraftPools)
        {
            Logger.Info($"Pool [{string.Join(", ", pool.Value)}] not assigned yet", "PoolAssigned");
        }
        Logger.Info("================================================", "PoolsAssigned");

        // foreach (var player in AllPlayers.Select(x => x.PlayerId).Except(AssignedBuckets.Keys).Except(AssignedRoles.Keys))
        // {
        //     Logger.Warn($"{player.GetPlayer().name} was not assigned a role or bucket, assigning Crewmate.", "PoolNotAssigned");
        //     AssignedRoles.Add(player, CustomRoles.CrewmateTOHE);
        // }

        return DraftCmdResult.Success;

        RoleAssign.RoleAssignInfo GetAssignInfo(CustomRoles role) => Roles.FirstOrDefault(x => x.Role == role);

        List<CustomRoles> GetPool(int index)
        {
            if (DraftPools.ContainsKey((byte)index))
            {
                return DraftPools[(byte)index];
            }
            else if (index >= DraftPools.Keys.Max() && index - DraftPools.Keys.Max() < UnassignedDraftPools.Count)
            {
                return UnassignedDraftPools[index - DraftPools.Keys.Max()].Value;
            }
            return null;
        }

        RoleBucket GetBucket(int index)
        {
            if (AssignedBuckets.ContainsKey((byte)index))
            {
                return AssignedBuckets[(byte)index];
            }
            else if (index >= AssignedBuckets.Keys.Max() && index - AssignedBuckets.Keys.Max() < UnassignedBuckets.Count)
            {
                return UnassignedBuckets[index - AssignedBuckets.Keys.Max()];
            }
            return RoleBucket.None;
        }
    }

    private static void LoadRoleBuckets()
    {
        var bucketSettings = Options.DraftBuckets;

        List<RoleBucket> RoleBuckets = [.. EnumHelper.GetAllValues<RoleBucket>().Where(x => x != RoleBucket.None)];
        List<CustomRoles> Roles = [.. CustomRolesHelper.AllRoles.Where(x => x.IsBucketableRole())];

        foreach (var setting in bucketSettings.GetOptions())
        {
            var bucketId = setting.GetInt();
            if (bucketId < RoleBuckets.Count)
            {
                var bucket = RoleBuckets[bucketId];

                UnassignedBuckets.Add(bucket);

                continue;
            }
            else if (bucketId - RoleBuckets.Count < Roles.Count)
            {
                var role = Roles[bucketId - RoleBuckets.Count];
                UnassignedRoles.Add(role);
            }
            else
            {
                Logger.Error($"Role Bucket with Id {bucketId} not found.", "LoadRoleBuckets");
            }
        }
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
            RoleBucket.NeutralChaos => CustomRoles.Pirate.GetColoredTextByRole(GetString($"RoleBucket.{bucket}")),
            RoleBucket.NeutralApocalypse => CustomRoles.Apocalypse.GetColoredTextByRole(GetString($"RoleBucket.{bucket}")),
            RoleBucket.NeutralEvil => CustomRoles.Jester.GetColoredTextByRole(GetString($"RoleBucket.{bucket}")),
            RoleBucket.NeutralKilling => CustomRoles.Traitor.GetColoredTextByRole(GetString($"RoleBucket.{bucket}")),
            RoleBucket.NeutralRandom => CustomRoles.Executioner.GetColoredTextByRole(GetString($"RoleBucket.{bucket}")),

            _ => CustomRoles.Crewmate.GetColoredTextByRole(GetString($"RoleBucket.{bucket}"))
        };

    private static void AssignRoleBuckets(List<PlayerControl> AllPlayers, IRandom rnd)
    {
        int playerCount = AllPlayers.Count;

        if (playerCount > UnassignedBuckets.Count + UnassignedRoles.Count)
        {
            Logger.Error("Not enough role buckets set, adding crewmates.", "AssignRoleBuckets");

            while (playerCount > UnassignedBuckets.Count + UnassignedRoles.Count)
            {
                UnassignedRoles.Add(CustomRoles.CrewmateTOHE);
            }
        }

        AllPlayers = [.. AllPlayers.Shuffle(rnd).Shuffle(rnd)];

        foreach (var player in AllPlayers)
        {
            if (UnassignedBuckets.Any())
            {
                var bucket = UnassignedBuckets[0];

                AssignedBuckets.Add(player.PlayerId, bucket);
                DraftPools.Add(player.PlayerId, []);

                UnassignedBuckets.Remove(bucket);
            }
            else if (UnassignedRoles.Any())
            {
                var role = UnassignedRoles[0];

                AssignedRoles.Add(player.PlayerId, role);

                UnassignedRoles.Remove(role);
            }
            else
            {
                Logger.Error("Not enough role buckets/roles to give some to all players.", "AssignRoleBuckets");
            }
        }

        // Initialize Unassigned Draft Pools
        foreach (var bucket in UnassignedBuckets)
        {
            UnassignedDraftPools.Add(new(bucket, []));
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

        var rd = IRandom.Instance;
        int playerCount = Main.AllAlivePlayerControls.Length;

        var AllPlayers = Main.AllAlivePlayerControls;

        List<CustomRoles> FinalRolesList = [];

        foreach (var id in DraftedRoles.Keys.Where(id => Utils.GetPlayerById(id) == null).ToArray()) DraftedRoles.Remove(id);

        foreach (var id in AssignedBuckets.Keys.Where(id => Utils.GetPlayerById(id) == null).ToArray())
        {
            var pool = DraftPools[id];
            UnassignedDraftPools.Add(new(AssignedBuckets[id], pool));
            DraftPools.Remove(id);

            UnassignedBuckets.Add(AssignedBuckets[id]);
            AssignedBuckets.Remove(id);
        }
        foreach (var id in AssignedRoles.Keys.Where(id => Utils.GetPlayerById(id) == null).ToArray())
        {
            UnassignedRoles.Add(AssignedRoles[id]);
            AssignedRoles.Remove(id);
        }

        foreach (var player in AllPlayers)
        {
            byte playerId = player.PlayerId;
            // Assign Role if drafted
            if (DraftedRoles.TryGetValue(playerId, out CustomRoles drafted))
            {
                RoleResult[playerId] = drafted;
            }
            // Assign role if assigned
            else if (AssignedRoles.TryGetValue(playerId, out CustomRoles assigned))
            {
                RoleResult[playerId] = assigned;
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
            // Assign role if no unassigned buckets left
            else if (UnassignedRoles.Any())
            {
                var role = UnassignedRoles.Shuffle(rd).First();

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

        List<object> unassigned = [.. UnassignedBuckets, .. UnassignedRoles];

        // foreach player that doesn't have a bucket/role assigned
        foreach (var player in Main.AllAlivePlayerControls.ExceptBy(AssignedBuckets.Keys, x => x.PlayerId).ExceptBy(AssignedRoles.Keys, x => x.PlayerId))
        {
            unassigned = [.. unassigned.Shuffle(rd)];

            var toAssign = unassigned.First();

            if (toAssign is RoleBucket bucket)
            {
                unassigned.Remove(toAssign);
                UnassignedBuckets.Remove(bucket);
                AssignedBuckets.Add(player.PlayerId, bucket);

                var pool = UnassignedDraftPools.FirstOrDefault(x => x.Key == bucket);

                DraftPools.Add(player.PlayerId, pool.Value);
                UnassignedDraftPools.Remove(pool);

                Logger.Info($"Pool [{string.Join(", ", pool.Value)}] assigned to {player.name}", "PoolAssigned");
            }
            else if (toAssign is CustomRoles role)
            {
                unassigned.Remove(toAssign);
                UnassignedRoles.Remove(role);
                AssignedRoles.Add(player.PlayerId, role);
            }
        }
        return DraftCmdResult.Success;
    }

    public static void Reset()
    {
        UnassignedBuckets = [];
        UnassignedRoles = [];
        AssignedBuckets = [];
        AssignedRoles = [];
        Roles = [];
        DraftPools = [];
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
}