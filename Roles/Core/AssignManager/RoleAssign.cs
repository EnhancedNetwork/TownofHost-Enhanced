using AmongUs.GameOptions;
using TOHE.Roles.Double;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;

namespace TOHE.Roles.Core.AssignManager;

public class RoleAssign
{
    public static Dictionary<byte, CustomRoles> SetRoles = [];
    public static Dictionary<byte, CustomRoles> RoleResult = [];
    public static CustomRoles[] AllRoles => [.. RoleResult.Values];

    [Obfuscation(Exclude = true)]
    enum RoleAssignType
    {
        Impostor,
        NeutralKilling,
        NonKillingNeutral,
        NeutralApocalypse,
        Coven,
        Crewmate,

        None
    }

    public static void OnInit()
    {
        RoleResult.Clear();
        SetRoles.Clear();
    }

    public class RoleAssignInfo(CustomRoles role, int spawnChance, int maxCount, int assignedCount = 0)
    {
        public CustomRoles Role { get => role; set => role = value; }
        public int SpawnChance { get => spawnChance; set => spawnChance = value; }
        public int MaxCount { get => maxCount; set => maxCount = value; }
        public int AssignedCount { get => assignedCount; set => assignedCount = value; }
    }

    public static void GetNeutralCounts(int NKmaxOpt, int NKminOpt, int NNKmaxOpt, int NNKminOpt, int NAmaxOpt, int NAminOpt, ref int ResultNKnum, ref int ResultNNKnum, ref int ResultNAnum)
    {
        var rd = IRandom.Instance;

        if (NNKmaxOpt > 0 && NNKmaxOpt >= NNKminOpt)
        {
            ResultNNKnum = rd.Next(NNKminOpt, NNKmaxOpt + 1);
        }

        if (NKmaxOpt > 0 && NKmaxOpt >= NKminOpt)
        {
            ResultNKnum = rd.Next(NKminOpt, NKmaxOpt + 1);
        }

        if (NAmaxOpt > 0 && NAmaxOpt >= NAminOpt)
        {
            ResultNAnum = rd.Next(NAminOpt, NAmaxOpt + 1);
        }
    }
    public static void GetCovenCounts(int CVmaxOpt, int CVminOpt, ref int ResultCVnum)
    {
        var rd = IRandom.Instance;

        if (CVmaxOpt > 0 && CVmaxOpt >= CVminOpt)
        {
            ResultCVnum = rd.Next(CVminOpt, CVmaxOpt + 1);
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
                        Logger.Info($"Assign Game Master due to tag for [{pc.PlayerId}]{pc.GetRealName()}", "TagManager");
                        continue;
                    }
                    RoleResult[pc.PlayerId] = CustomRoles.Killer;
                }
                return;
            case CustomGameMode.UltimateTeam:
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
                        Logger.Info($"Assign Game Master due to tag for [{pc.PlayerId}]{pc.GetRealName()}", "TagManager");
                        continue;
                    }
                    RoleResult = [];
                    RoleResult = UltimateTeam.SetRoles();
                }
                return;
            case CustomGameMode.TrickorTreat:
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
                        Logger.Info($"Assign Game Master due to tag for [{pc.PlayerId}]{pc.GetRealName()}", "TagManager");
                        continue;
                    }
                    RoleResult = [];
                    RoleResult = TrickorTreat.SetRoles();
                }
                return;
            case CustomGameMode.CandR:
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
                        Logger.Info($"Assign Game Master due to tag for [{pc.PlayerId}]{pc.GetRealName()}", "TagManager");
                        continue;
                    }
                    RoleResult = [];
                    RoleResult = CopsAndRobbersManager.SetRoles();
                }
                return;

        }

        var rd = IRandom.Instance;
        int playerCount = Main.AllAlivePlayerControls.Length;
        int optImpNum = Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors);
        int optNonNeutralKillingNum = 0;
        int optNeutralKillingNum = 0;
        int optNeutralApocalypseNum = 0;
        int optCovenNum = 0;

        GetNeutralCounts(Options.NeutralKillingRolesMaxPlayer.GetInt(), Options.NeutralKillingRolesMinPlayer.GetInt(), Options.NonNeutralKillingRolesMaxPlayer.GetInt(), Options.NonNeutralKillingRolesMinPlayer.GetInt(), Options.NeutralApocalypseRolesMaxPlayer.GetInt(), Options.NeutralApocalypseRolesMinPlayer.GetInt(), ref optNeutralKillingNum, ref optNonNeutralKillingNum, ref optNeutralApocalypseNum);
        GetCovenCounts(Options.CovenRolesMaxPlayer.GetInt(), Options.CovenRolesMinPlayer.GetInt(), ref optCovenNum);

        int readyRoleNum = 0;
        int readyImpNum = 0;
        int readyNonNeutralKillingNum = 0;
        int readyNeutralKillingNum = 0;
        int readyNeutralApocalypseNum = 0;
        int readyCovenNum = 0;

        List<CustomRoles> FinalRolesList = [];

        Dictionary<RoleAssignType, List<RoleAssignInfo>> Roles = [];

        Roles[RoleAssignType.Impostor] = [];
        Roles[RoleAssignType.NeutralKilling] = [];
        Roles[RoleAssignType.NonKillingNeutral] = [];
        Roles[RoleAssignType.NeutralApocalypse] = [];
        Roles[RoleAssignType.Coven] = [];
        Roles[RoleAssignType.Crewmate] = [];

        foreach (var id in SetRoles.Keys.Where(id => Utils.GetPlayerById(id) == null).ToArray()) SetRoles.Remove(id);

        foreach (var role in EnumHelper.GetAllValues<CustomRoles>())
        {
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
                    continue;
            }

            int count = role.GetCount();
            RoleAssignInfo info = new(role, chance, count);

            if (role is CustomRoles.Mini)
            {
                if (Mini.CheckSpawnEvilMini())
                {
                    info = new(CustomRoles.EvilMini, chance, count);
                    Roles[RoleAssignType.Impostor].Add(info);
                }
                else
                {
                    info = new(CustomRoles.NiceMini, chance, count);
                    Roles[RoleAssignType.Crewmate].Add(info);
                }
                continue;
            }

            if (role.IsImpostor()) Roles[RoleAssignType.Impostor].Add(info);
            else if (role.IsNK()) Roles[RoleAssignType.NeutralKilling].Add(info);
            else if (role.IsNA()) Roles[RoleAssignType.NeutralApocalypse].Add(info);
            else if (role.IsNonNK()) Roles[RoleAssignType.NonKillingNeutral].Add(info);
            else if (role.IsCoven()) Roles[RoleAssignType.Coven].Add(info);
            else Roles[RoleAssignType.Crewmate].Add(info);
        }

        Logger.Info($"Number of NKs: {optNeutralKillingNum}, Number of NonNKs: {optNonNeutralKillingNum}", "NeutralNum");
        Logger.Msg("=====================================================", "AllActiveRoles");
        Logger.Info(string.Join(", ", Roles[RoleAssignType.Impostor].Select(x => $"{x.Role}: {x.SpawnChance}% - {x.MaxCount}")), "ImpRoles");
        Logger.Info(string.Join(", ", Roles[RoleAssignType.NeutralKilling].Select(x => $"{x.Role}: {x.SpawnChance}% - {x.MaxCount}")), "NKRoles");
        Logger.Info(string.Join(", ", Roles[RoleAssignType.NeutralApocalypse].Select(x => $"{x.Role}: {x.SpawnChance}% - {x.MaxCount}")), "NARoles");
        Logger.Info(string.Join(", ", Roles[RoleAssignType.NonKillingNeutral].Select(x => $"{x.Role}: {x.SpawnChance}% - {x.MaxCount}")), "NonNKRoles");
        Logger.Info(string.Join(", ", Roles[RoleAssignType.Coven].Select(x => $"{x.Role}: {x.SpawnChance}% - {x.MaxCount}")), "CovenRoles");
        Logger.Info(string.Join(", ", Roles[RoleAssignType.Crewmate].Select(x => $"{x.Role}: {x.SpawnChance}% - {x.MaxCount}")), "CrewRoles");
        Logger.Msg("=====================================================", "AllActiveRoles");

        IEnumerable<RoleAssignInfo> TempAlwaysImpRoles = Roles[RoleAssignType.Impostor].Where(x => x.SpawnChance == 100);
        IEnumerable<RoleAssignInfo> TempAlwaysNKRoles = Roles[RoleAssignType.NeutralKilling].Where(x => x.SpawnChance == 100);
        IEnumerable<RoleAssignInfo> TempAlwaysNARoles = Roles[RoleAssignType.NeutralApocalypse].Where(x => x.SpawnChance == 100);
        IEnumerable<RoleAssignInfo> TempAlwaysNNKRoles = Roles[RoleAssignType.NonKillingNeutral].Where(x => x.SpawnChance == 100);
        IEnumerable<RoleAssignInfo> TempAlwaysCovenRoles = Roles[RoleAssignType.Coven].Where(x => x.SpawnChance == 100);
        IEnumerable<RoleAssignInfo> TempAlwaysCrewRoles = Roles[RoleAssignType.Crewmate].Where(x => x.SpawnChance == 100);

        // DistinctBy - Removes duplicate Roles if there are any
        // Shuffle - Shuffles all Roles in the list into a randomized order
        // Take - Takes the first X Roles of the list ... X is the maximum number of Roles we could need of that team

        Roles[RoleAssignType.Impostor] = Roles[RoleAssignType.Impostor].Shuffle(rd).Shuffle(rd).Take(optImpNum).ToList();
        Roles[RoleAssignType.NeutralKilling] = Roles[RoleAssignType.NeutralKilling].Shuffle(rd).Shuffle(rd).Take(optNeutralKillingNum).ToList();
        Roles[RoleAssignType.NeutralApocalypse] = Roles[RoleAssignType.NeutralApocalypse].Shuffle(rd).Shuffle(rd).Take(optNeutralApocalypseNum).ToList();
        Roles[RoleAssignType.NonKillingNeutral] = Roles[RoleAssignType.NonKillingNeutral].Shuffle(rd).Shuffle(rd).Take(optNonNeutralKillingNum).ToList();
        Roles[RoleAssignType.Coven] = Roles[RoleAssignType.Coven].Shuffle(rd).Shuffle(rd).Take(optCovenNum).ToList();
        Roles[RoleAssignType.Crewmate] = Roles[RoleAssignType.Crewmate].Shuffle(rd).Shuffle(rd).Take(playerCount).ToList();

        Roles[RoleAssignType.Impostor].AddRange(TempAlwaysImpRoles);
        Roles[RoleAssignType.NeutralKilling].AddRange(TempAlwaysNKRoles);
        Roles[RoleAssignType.NeutralApocalypse].AddRange(TempAlwaysNARoles);
        Roles[RoleAssignType.NonKillingNeutral].AddRange(TempAlwaysNNKRoles);
        Roles[RoleAssignType.Coven].AddRange(TempAlwaysCovenRoles);
        Roles[RoleAssignType.Crewmate].AddRange(TempAlwaysCrewRoles);

        Roles[RoleAssignType.Impostor] = Roles[RoleAssignType.Impostor].DistinctBy(x => x.Role).ToList();
        Roles[RoleAssignType.NeutralKilling] = Roles[RoleAssignType.NeutralKilling].DistinctBy(x => x.Role).ToList();
        Roles[RoleAssignType.NeutralApocalypse] = Roles[RoleAssignType.NeutralApocalypse].DistinctBy(x => x.Role).ToList();
        Roles[RoleAssignType.NonKillingNeutral] = Roles[RoleAssignType.NonKillingNeutral].DistinctBy(x => x.Role).ToList();
        Roles[RoleAssignType.Coven] = Roles[RoleAssignType.Coven].DistinctBy(x => x.Role).ToList();
        Roles[RoleAssignType.Crewmate] = Roles[RoleAssignType.Crewmate].DistinctBy(x => x.Role).ToList();

        Logger.Msg("======================================================", "SelectedRoles");
        Logger.Info(string.Join(", ", Roles[RoleAssignType.Impostor].Select(x => x.Role.ToString())), "Selected-Impostor-Roles");
        Logger.Info(string.Join(", ", Roles[RoleAssignType.NeutralKilling].Select(x => x.Role.ToString())), "Selected-NK-Roles");
        Logger.Info(string.Join(", ", Roles[RoleAssignType.NeutralApocalypse].Select(x => x.Role.ToString())), "Selected-NA-Roles");
        Logger.Info(string.Join(", ", Roles[RoleAssignType.NonKillingNeutral].Select(x => x.Role.ToString())), "Selected-NonNK-Roles");
        Logger.Info(string.Join(", ", Roles[RoleAssignType.Coven].Select(x => x.Role.ToString())), "Selected-Coven-Roles");
        Logger.Info(string.Join(", ", Roles[RoleAssignType.Crewmate].Select(x => x.Role.ToString())), "Selected-Crew-Roles");
        Logger.Msg("======================================================", "SelectedRoles");

        var AllPlayers = Main.AllPlayerControls.ToList();

        // Players on the EAC banned list will be assigned as GM when opening rooms
        if (BanManager.CheckEACList(PlayerControl.LocalPlayer.FriendCode, PlayerControl.LocalPlayer.GetClient().GetHashedPuid()))
        {
            Logger.Warn("Host presets in BanManager.CheckEACList", "EAC");
            Main.EnableGM.Value = true;
            RoleResult[PlayerControl.LocalPlayer.PlayerId] = CustomRoles.GM;
            AllPlayers.Remove(PlayerControl.LocalPlayer);
        }
        if (Main.EnableGM.Value)
        {
            RoleResult[PlayerControl.LocalPlayer.PlayerId] = CustomRoles.GM;
            AllPlayers.Remove(PlayerControl.LocalPlayer);
            SetRoles.Remove(PlayerControl.LocalPlayer.PlayerId);
        }
        foreach (var item in SetRoles)
        {
            PlayerControl playerControl = Utils.GetPlayerById(item.Key);
            if (playerControl == null) continue;
            if (TagManager.AssignGameMaster(playerControl.FriendCode))
            {
                Logger.Info($"Assign Game Master due to tag for [{item.Key}]{playerControl.GetRealName()}", "TagManager");
                AllPlayers.Remove(playerControl);
                SetRoles.Remove(playerControl.PlayerId);
                RoleResult[playerControl.PlayerId] = CustomRoles.GM;
            }
        }

        AllPlayers.RemoveAll(x => ChatCommands.Spectators.Contains(x.PlayerId));
        RoleResult.AddRange(ChatCommands.Spectators.ToDictionary(x => x, _ => CustomRoles.GM));
        
        // Pre-Assigned Roles by Host are selected first
        foreach (var item in SetRoles)
        {
            PlayerControl pc = Utils.GetPlayerById(item.Key);
            if (pc == null) continue;

            RoleResult[item.Key] = item.Value;
            AllPlayers.Remove(pc);

            if (item.Value.IsImpostor())
            {
                Roles[RoleAssignType.Impostor].Where(x => x.Role == item.Value).Do(x => x.AssignedCount++);
                readyImpNum++;
            }
            else if (item.Value.IsNK())
            {
                Roles[RoleAssignType.NeutralKilling].Where(x => x.Role == item.Value).Do(x => x.AssignedCount++);
                readyNeutralKillingNum++;
            }
            else if (item.Value.IsNA())
            {
                Roles[RoleAssignType.NeutralApocalypse].Where(x => x.Role == item.Value).Do(x => x.AssignedCount++);
                readyNeutralApocalypseNum++;
            }
            else if (item.Value.IsCoven())
            {
                Roles[RoleAssignType.Coven].Where(x => x.Role == item.Value).Do(x => x.AssignedCount++);
                readyCovenNum++;
            }
            else if (item.Value.IsNonNK())
            {
                Roles[RoleAssignType.NonKillingNeutral].Where(x => x.Role == item.Value).Do(x => x.AssignedCount++);
                readyNonNeutralKillingNum++;
            }

            readyRoleNum++;

            Logger.Warn($"Pre-Set Role Assigned: {pc.GetRealName()} => {item.Value}", "RoleAssign");
        }

        RoleAssignInfo[] Imps = [];
        RoleAssignInfo[] NNKs = [];
        RoleAssignInfo[] NKs = [];
        RoleAssignInfo[] NAs = [];
        RoleAssignInfo[] Covs = [];
        RoleAssignInfo[] Crews = [];

        List<RoleAssignType> KillingFractions = [];

        bool spawnNK = false;
        bool spawnNA = false;
        bool spawnCoven = false;

        if (Roles[RoleAssignType.NeutralKilling].Count > 0)
        {
            KillingFractions.Add(RoleAssignType.NeutralKilling);
        }
        if (Roles[RoleAssignType.NeutralApocalypse].Count > 0)
        {
            KillingFractions.Add(RoleAssignType.NeutralApocalypse);
        }
        if (Roles[RoleAssignType.Coven].Count > 0)
        {
            KillingFractions.Add(RoleAssignType.Coven);
        }

        var randomType = Options.SpawnOneRandomKillingFraction.GetBool()
            ? KillingFractions.RandomElement() : RoleAssignType.None;

        switch (randomType)
        {
            case RoleAssignType.NeutralKilling:
                spawnNK = true;
                break;
            case RoleAssignType.NeutralApocalypse:
                spawnNA = true;
                break;
            case RoleAssignType.Coven:
                spawnCoven = true;
                break;
            default:
                spawnNK = true;
                spawnNA = true;
                spawnCoven = true;
                break;
        }

        // Impostor Roles
        {
            List<CustomRoles> AlwaysImpRoles = [];
            List<CustomRoles> ChanceImpRoles = [];
            for (int i = 0; i < Roles[RoleAssignType.Impostor].Count; i++)
            {
                RoleAssignInfo item = Roles[RoleAssignType.Impostor][i];

                if (item.SpawnChance == 100)
                {
                    for (int j = 0; j < item.MaxCount; j++)
                    {
                        // Don't add if Host has assigned this role by using '/up'
                        if (SetRoles.ContainsValue(item.Role))
                        {
                            var playerId = SetRoles.FirstOrDefault(x => x.Value == item.Role).Key;
                            SetRoles.Remove(playerId);
                            continue;
                        }

                        AlwaysImpRoles.Add(item.Role);
                    }
                }
                else
                {
                    // Add 'MaxCount' (1) times
                    for (int k = 0; k < item.MaxCount; k++)
                    {
                        // Don't add if Host has assigned this role by using '/up'
                        if (SetRoles.ContainsValue(item.Role))
                        {
                            var playerId = SetRoles.FirstOrDefault(x => x.Value == item.Role).Key;
                            SetRoles.Remove(playerId);
                            continue;
                        }

                        // Make "Spawn Chance ÷ 5 = X" (Example: 65 ÷ 5 = 13)
                        for (int j = 0; j < item.SpawnChance / 5; j++)
                        {
                            // Add Imp roles 'x' times (13)
                            ChanceImpRoles.Add(item.Role);
                        }
                    }
                }
            }

            RoleAssignInfo[] ImpRoleCounts = AlwaysImpRoles.Distinct().Select(GetAssignInfo).ToArray().AddRangeToArray(ChanceImpRoles.Distinct().Select(GetAssignInfo).ToArray());
            Imps = ImpRoleCounts;

            // Assign Roles set to 100%
            if (readyImpNum < optImpNum)
            {
                while (AlwaysImpRoles.Any())
                {
                    var selected = AlwaysImpRoles.RandomElement();
                    var info = ImpRoleCounts.FirstOrDefault(x => x.Role == selected);
                    AlwaysImpRoles.Remove(selected);
                    if (info.AssignedCount >= info.MaxCount) continue;

                    FinalRolesList.Add(selected);
                    info.AssignedCount++;
                    readyRoleNum++;
                    readyImpNum++;

                    Imps = ImpRoleCounts;

                    if (readyRoleNum >= playerCount) goto EndOfAssign;
                    if (readyImpNum >= optImpNum) break;
                }
            }

            // Assign other Roles when needed
            if (readyRoleNum < playerCount && readyImpNum < optImpNum)
            {
                while (ChanceImpRoles.Any())
                {
                    var selected = ChanceImpRoles.RandomElement();
                    var info = ImpRoleCounts.FirstOrDefault(x => x.Role == selected);

                    // Remove 'X' times
                    for (int j = 0; j < info.SpawnChance / 5; j++)
                        ChanceImpRoles.Remove(selected);

                    FinalRolesList.Add(selected);
                    info.AssignedCount++;
                    readyRoleNum++;
                    readyImpNum++;

                    Imps = ImpRoleCounts;

                    if (info.AssignedCount >= info.MaxCount)
                        while (ChanceImpRoles.Contains(selected))
                            ChanceImpRoles.Remove(selected);

                    if (readyRoleNum >= playerCount) goto EndOfAssign;
                    if (readyImpNum >= optImpNum) break;
                }
            }
        }

        // Neutral Roles
        {
            // Neutral Non-Killing Roles
            {
                List<CustomRoles> AlwaysNonNKRoles = [];
                List<CustomRoles> ChanceNonNKRoles = [];
                for (int i = 0; i < Roles[RoleAssignType.NonKillingNeutral].Count; i++)
                {
                    RoleAssignInfo item = Roles[RoleAssignType.NonKillingNeutral][i];

                    if (item.SpawnChance == 100)
                    {
                        for (int j = 0; j < item.MaxCount; j++)
                        {
                            // Don't add if Host has assigned this role by using '/up'
                            if (SetRoles.ContainsValue(item.Role))
                            {
                                var playerId = SetRoles.FirstOrDefault(x => x.Value == item.Role).Key;
                                SetRoles.Remove(playerId);
                                continue;
                            }

                            AlwaysNonNKRoles.Add(item.Role);
                        }
                    }
                    else
                    {
                        // Add 'MaxCount' (1) times
                        for (int k = 0; k < item.MaxCount; k++)
                        {
                            // Don't add if Host has assigned this Role by using '/up'
                            if (SetRoles.ContainsValue(item.Role))
                            {
                                var playerId = SetRoles.FirstOrDefault(x => x.Value == item.Role).Key;
                                SetRoles.Remove(playerId);
                                continue;
                            }

                            // Make "Spawn Chance ÷ 5 = X" (Example: 65 ÷ 5 = 13)
                            for (int j = 0; j < item.SpawnChance / 5; j++)
                            {
                                // Add Non-NK roles 'X' times (13)
                                ChanceNonNKRoles.Add(item.Role);
                            }
                        }
                    }
                }

                RoleAssignInfo[] NonNKRoleCounts = AlwaysNonNKRoles.Distinct().Select(GetAssignInfo).ToArray().AddRangeToArray(ChanceNonNKRoles.Distinct().Select(GetAssignInfo).ToArray());
                NNKs = NonNKRoleCounts;

                // Assign Roles set to 100%
                if (readyNonNeutralKillingNum < optNonNeutralKillingNum)
                {
                    while (AlwaysNonNKRoles.Any() && optNonNeutralKillingNum > 0)
                    {
                        var selected = AlwaysNonNKRoles.RandomElement();
                        var info = NonNKRoleCounts.FirstOrDefault(x => x.Role == selected);
                        AlwaysNonNKRoles.Remove(selected);
                        if (info.AssignedCount >= info.MaxCount) continue;

                        FinalRolesList.Add(selected);
                        info.AssignedCount++;
                        readyRoleNum++;
                        readyNonNeutralKillingNum++;

                        NNKs = NonNKRoleCounts;

                        if (readyRoleNum >= playerCount) goto EndOfAssign;
                        if (readyNonNeutralKillingNum >= optNonNeutralKillingNum) break;
                    }
                }

                // Assign other Roles when needed
                if (readyRoleNum < playerCount && readyNonNeutralKillingNum < optNonNeutralKillingNum)
                {
                    while (ChanceNonNKRoles.Any() && optNonNeutralKillingNum > 0)
                    {
                        var selected = ChanceNonNKRoles.RandomElement();
                        var info = NonNKRoleCounts.FirstOrDefault(x => x.Role == selected);

                        // Remove 'X' times
                        for (int j = 0; j < info.SpawnChance / 5; j++)
                            ChanceNonNKRoles.Remove(selected);

                        FinalRolesList.Add(selected);
                        info.AssignedCount++;
                        readyRoleNum++;
                        readyNonNeutralKillingNum++;

                        NNKs = NonNKRoleCounts;

                        if (info.AssignedCount >= info.MaxCount)
                            while (ChanceNonNKRoles.Contains(selected))
                                ChanceNonNKRoles.Remove(selected);

                        if (readyRoleNum >= playerCount) goto EndOfAssign;
                        if (readyNonNeutralKillingNum >= optNonNeutralKillingNum) break;
                    }
                }
            }

            // Neutral Killing Roles
            if (spawnNK)
            {
                List<CustomRoles> AlwaysNKRoles = [];
                List<CustomRoles> ChanceNKRoles = [];
                for (int i = 0; i < Roles[RoleAssignType.NeutralKilling].Count; i++)
                {
                    RoleAssignInfo item = Roles[RoleAssignType.NeutralKilling][i];

                    if (item.SpawnChance == 100)
                    {
                        for (int j = 0; j < item.MaxCount; j++)
                        {
                            // Don't add if Host has assigned this role by using '/up'
                            if (SetRoles.ContainsValue(item.Role))
                            {
                                var playerId = SetRoles.FirstOrDefault(x => x.Value == item.Role).Key;
                                SetRoles.Remove(playerId);
                                continue;
                            }

                            AlwaysNKRoles.Add(item.Role);
                        }
                    }
                    else
                    {
                        // Add 'MaxCount' (1) times
                        for (int k = 0; k < item.MaxCount; k++)
                        {
                            // Don't add if Host has assigned this role by using '/up'
                            if (SetRoles.ContainsValue(item.Role))
                            {
                                var playerId = SetRoles.FirstOrDefault(x => x.Value == item.Role).Key;
                                SetRoles.Remove(playerId);
                                continue;
                            }

                            // Make "Spawn Chance ÷ 5 = X" (Example: 65 ÷ 5 = 13)
                            for (int j = 0; j < item.SpawnChance / 5; j++)
                            {
                                // Add NK roles 'x' times (13)
                                ChanceNKRoles.Add(item.Role);
                            }
                        }
                    }
                }

                RoleAssignInfo[] NKRoleCounts = AlwaysNKRoles.Distinct().Select(GetAssignInfo).ToArray().AddRangeToArray(ChanceNKRoles.Distinct().Select(GetAssignInfo).ToArray());
                NKs = NKRoleCounts;

                // Assign Roles set to 100%
                if (readyNeutralKillingNum < optNeutralKillingNum)
                {
                    while (AlwaysNKRoles.Any() && optNeutralKillingNum > 0)
                    {
                        var selected = AlwaysNKRoles.RandomElement();
                        var info = NKRoleCounts.FirstOrDefault(x => x.Role == selected);
                        AlwaysNKRoles.Remove(selected);
                        if (info.AssignedCount >= info.MaxCount) continue;

                        FinalRolesList.Add(selected);
                        info.AssignedCount++;
                        readyRoleNum++;
                        readyNeutralKillingNum++;

                        NKs = NKRoleCounts;

                        if (readyRoleNum >= playerCount) goto EndOfAssign;
                        if (readyNeutralKillingNum >= optNeutralKillingNum) break;
                    }
                }

                // Assign other Roles when needed
                if (readyRoleNum < playerCount && readyNeutralKillingNum < optNeutralKillingNum)
                {
                    while (ChanceNKRoles.Any() && optNeutralKillingNum > 0)
                    {
                        var selected = ChanceNKRoles.RandomElement();
                        var info = NKRoleCounts.FirstOrDefault(x => x.Role == selected);

                        // Remove 'X' times
                        for (int j = 0; j < info.SpawnChance / 5; j++)
                            ChanceNKRoles.Remove(selected);

                        FinalRolesList.Add(selected);
                        info.AssignedCount++;
                        readyRoleNum++;
                        readyNeutralKillingNum++;

                        NKs = NKRoleCounts;

                        if (info.AssignedCount >= info.MaxCount) while (ChanceNKRoles.Contains(selected)) ChanceNKRoles.Remove(selected);

                        if (readyRoleNum >= playerCount) goto EndOfAssign;
                        if (readyNeutralKillingNum >= optNeutralKillingNum) break;
                    }
                }
            }
            
            // Neutral Apocalypse Roles
            if (spawnNA)
            {
                List<CustomRoles> AlwaysNARoles = [];
                List<CustomRoles> ChanceNARoles = [];
                for (int i = 0; i < Roles[RoleAssignType.NeutralApocalypse].Count; i++)
                {
                    RoleAssignInfo item = Roles[RoleAssignType.NeutralApocalypse][i];

                    if (item.SpawnChance == 100)
                    {
                        for (int j = 0; j < item.MaxCount; j++)
                        {
                            // Don't add if Host has assigned this role by using '/up'
                            if (SetRoles.ContainsValue(item.Role))
                            {
                                var playerId = SetRoles.FirstOrDefault(x => x.Value == item.Role).Key;
                                SetRoles.Remove(playerId);
                                continue;
                            }

                            AlwaysNARoles.Add(item.Role);
                        }
                    }
                    else
                    {
                        // Add 'MaxCount' (1) times
                        for (int k = 0; k < item.MaxCount; k++)
                        {
                            // Don't add if Host has assigned this role by using '/up'
                            if (SetRoles.ContainsValue(item.Role))
                            {
                                var playerId = SetRoles.FirstOrDefault(x => x.Value == item.Role).Key;
                                SetRoles.Remove(playerId);
                                continue;
                            }

                            // Make "Spawn Chance ÷ 5 = X" (Example: 65 ÷ 5 = 13)
                            for (int j = 0; j < item.SpawnChance / 5; j++)
                            {
                                // Add NA roles 'X' times (13)
                                ChanceNARoles.Add(item.Role);
                            }
                        }
                    }
                }

                RoleAssignInfo[] NARoleCounts = AlwaysNARoles.Distinct().Select(GetAssignInfo).ToArray().AddRangeToArray(ChanceNARoles.Distinct().Select(GetAssignInfo).ToArray());
                NAs = NARoleCounts;

                // Assign Roles set to 100%
                if (readyNeutralApocalypseNum < optNeutralApocalypseNum)
                {
                    while (AlwaysNARoles.Any() && optNeutralApocalypseNum > 0)
                    {
                        var selected = AlwaysNARoles[rd.Next(0, AlwaysNARoles.Count)];
                        var info = NARoleCounts.FirstOrDefault(x => x.Role == selected);
                        AlwaysNARoles.Remove(selected);
                        if (info.AssignedCount >= info.MaxCount) continue;

                        FinalRolesList.Add(selected);
                        info.AssignedCount++;
                        readyRoleNum++;
                        readyNeutralApocalypseNum++;

                        NAs = NARoleCounts;

                        if (readyRoleNum >= playerCount) goto EndOfAssign;
                        if (readyNeutralApocalypseNum >= optNeutralApocalypseNum) break;
                    }
                }

                // Assign other Roles when needed
                if (readyRoleNum < playerCount && readyNeutralApocalypseNum < optNeutralApocalypseNum)
                {
                    while (ChanceNARoles.Any() && optNeutralApocalypseNum > 0)
                    {
                        var selectesItem = rd.Next(0, ChanceNARoles.Count);
                        var selected = ChanceNARoles[selectesItem];
                        var info = NARoleCounts.FirstOrDefault(x => x.Role == selected);

                        // Remove 'X' times
                        for (int j = 0; j < info.SpawnChance / 5; j++)
                            ChanceNARoles.Remove(selected);

                        FinalRolesList.Add(selected);
                        info.AssignedCount++;
                        readyRoleNum++;
                        readyNeutralApocalypseNum++;

                        NAs = NARoleCounts;

                        if (info.AssignedCount >= info.MaxCount) while (ChanceNARoles.Contains(selected)) ChanceNARoles.Remove(selected);

                        if (readyRoleNum >= playerCount) goto EndOfAssign;
                        if (readyNeutralApocalypseNum >= optNeutralApocalypseNum) break;
                    }
                }
            }
        }
        
        // Coven Roles
        if (spawnCoven)
        {
            {
                List<CustomRoles> AlwaysCVRoles = [];
                List<CustomRoles> ChanceCVRoles = [];
                for (int i = 0; i < Roles[RoleAssignType.Coven].Count; i++)
                {
                    RoleAssignInfo item = Roles[RoleAssignType.Coven][i];

                    if (item.SpawnChance == 100)
                    {
                        for (int j = 0; j < item.MaxCount; j++)
                        {
                            // Don't add if Host has assigned this role by using '/up'
                            if (SetRoles.ContainsValue(item.Role))
                            {
                                var playerId = SetRoles.FirstOrDefault(x => x.Value == item.Role).Key;
                                SetRoles.Remove(playerId);
                                continue;
                            }

                            AlwaysCVRoles.Add(item.Role);
                        }
                    }
                    else
                    {
                        // Add 'MaxCount' (1) times
                        for (int k = 0; k < item.MaxCount; k++)
                        {
                            // Don't add if Host has assigned this role by using '/up'
                            if (SetRoles.ContainsValue(item.Role))
                            {
                                var playerId = SetRoles.FirstOrDefault(x => x.Value == item.Role).Key;
                                SetRoles.Remove(playerId);
                                continue;
                            }

                            // Make "Spawn Chance ÷ 5 = X" (Example: 65 ÷ 5 = 13)
                            for (int j = 0; j < item.SpawnChance / 5; j++)
                            {
                                // Add coven roles 'X' times (13)
                                ChanceCVRoles.Add(item.Role);
                            }
                        }
                    }
                }

                RoleAssignInfo[] CVRoleCounts = AlwaysCVRoles.Distinct().Select(GetAssignInfo).ToArray().AddRangeToArray(ChanceCVRoles.Distinct().Select(GetAssignInfo).ToArray());
                Covs = CVRoleCounts;

                // Assign Roles set to 100%
                if (readyCovenNum < optCovenNum)
                {
                    while (AlwaysCVRoles.Any() && optCovenNum > 0)
                    {
                        var selected = AlwaysCVRoles[rd.Next(0, AlwaysCVRoles.Count)];
                        var info = CVRoleCounts.FirstOrDefault(x => x.Role == selected);
                        AlwaysCVRoles.Remove(selected);
                        if (info.AssignedCount >= info.MaxCount) continue;

                        FinalRolesList.Add(selected);
                        info.AssignedCount++;
                        readyRoleNum++;
                        readyCovenNum++;

                        Covs = CVRoleCounts;

                        if (readyRoleNum >= playerCount) goto EndOfAssign;
                        if (readyCovenNum >= optCovenNum) break;
                    }
                }

                // Assign other Roles when needed
                if (readyRoleNum < playerCount && readyCovenNum < optCovenNum)
                {
                    while (ChanceCVRoles.Any() && optCovenNum > 0)
                    {
                        var selectesItem = rd.Next(0, ChanceCVRoles.Count);
                        var selected = ChanceCVRoles[selectesItem];
                        var info = CVRoleCounts.FirstOrDefault(x => x.Role == selected);

                        // Remove 'X' times
                        for (int j = 0; j < info.SpawnChance / 5; j++)
                            ChanceCVRoles.Remove(selected);

                        FinalRolesList.Add(selected);
                        info.AssignedCount++;
                        readyRoleNum++;
                        readyCovenNum++;

                        Covs = CVRoleCounts;

                        if (info.AssignedCount >= info.MaxCount) while (ChanceCVRoles.Contains(selected)) ChanceCVRoles.Remove(selected);

                        if (readyRoleNum >= playerCount) goto EndOfAssign;
                        if (readyCovenNum >= optCovenNum) break;
                    }
                }
            }
        }
        // Crewmate Roles
        {
            List<CustomRoles> AlwaysCrewRoles = [];
            List<CustomRoles> ChanceCrewRoles = [];
            for (int i = 0; i < Roles[RoleAssignType.Crewmate].Count; i++)
            {
                RoleAssignInfo item = Roles[RoleAssignType.Crewmate][i];

                if (item.SpawnChance == 100)
                {
                    for (int j = 0; j < item.MaxCount; j++)
                    {
                        // Don't add if Host has assigned this role by using '/up'
                        if (SetRoles.ContainsValue(item.Role))
                        {
                            var playerId = SetRoles.FirstOrDefault(x => x.Value == item.Role).Key;
                            SetRoles.Remove(playerId);
                            continue;
                        }

                        AlwaysCrewRoles.Add(item.Role);
                    }
                }
                else
                {
                    // Add 'MaxCount' (1) times
                    for (int k = 0; k < item.MaxCount; k++)
                    {
                        // Don't add if Host has assigned this role by using '/up'
                        if (SetRoles.ContainsValue(item.Role))
                        {
                            var playerId = SetRoles.FirstOrDefault(x => x.Value == item.Role).Key;
                            SetRoles.Remove(playerId);
                            continue;
                        }

                        // Make "Spawn Chance ÷ 5 = X" (Example: 65 ÷ 5 = 13)
                        for (int j = 0; j < item.SpawnChance / 5; j++)
                        {
                            // Add Crew roles 'x' times (13)
                            ChanceCrewRoles.Add(item.Role);
                        }
                    }
                }
            }

            RoleAssignInfo[] CrewRoleCounts = AlwaysCrewRoles.Distinct().Select(GetAssignInfo).ToArray().AddRangeToArray(ChanceCrewRoles.Distinct().Select(GetAssignInfo).ToArray());
            Crews = CrewRoleCounts;

            // Assign Roles set to ALWAYS
            if (readyRoleNum < playerCount)
            {
                while (AlwaysCrewRoles.Any())
                {
                    var selected = AlwaysCrewRoles.RandomElement();
                    var info = CrewRoleCounts.FirstOrDefault(x => x.Role == selected);
                    AlwaysCrewRoles.Remove(selected);
                    if (info.AssignedCount >= info.MaxCount) continue;

                    FinalRolesList.Add(selected);
                    info.AssignedCount++;
                    readyRoleNum++;

                    Crews = CrewRoleCounts;

                    if (readyRoleNum >= playerCount) goto EndOfAssign;
                }
            }

            // Assign other Roles when needed
            if (readyRoleNum < playerCount)
            {
                while (ChanceCrewRoles.Any())
                {
                    var selected = ChanceCrewRoles.RandomElement();
                    var info = CrewRoleCounts.FirstOrDefault(x => x.Role == selected);

                    // Remove 'x' times
                    for (int j = 0; j < info.SpawnChance / 5; j++)
                        ChanceCrewRoles.Remove(selected);

                    FinalRolesList.Add(selected);
                    info.AssignedCount++;
                    readyRoleNum++;

                    Crews = CrewRoleCounts;

                    if (info.AssignedCount >= info.MaxCount) while (ChanceCrewRoles.Contains(selected)) ChanceCrewRoles.Remove(selected);

                    if (readyRoleNum >= playerCount) goto EndOfAssign;
                }
            }
        }

    EndOfAssign:

        if (Imps.Any()) Logger.Info(string.Join(", ", Imps.Select(x => $"{x.Role} - {x.AssignedCount}/{x.MaxCount} ({x.SpawnChance}%)")), "ImpRoleResult");
        if (NNKs.Any()) Logger.Info(string.Join(", ", NNKs.Select(x => $"{x.Role} - {x.AssignedCount}/{x.MaxCount} ({x.SpawnChance}%)")), "NNKRoleResult");
        if (NKs.Any()) Logger.Info(string.Join(", ", NKs.Select(x => $"{x.Role} - {x.AssignedCount}/{x.MaxCount} ({x.SpawnChance}%)")), "NKRoleResult");
        if (NAs.Any()) Logger.Info(string.Join(", ", NKs.Select(x => $"{x.Role} - {x.AssignedCount}/{x.MaxCount} ({x.SpawnChance}%)")), "NARoleResult");
        if (Covs.Any()) Logger.Info(string.Join(", ", Covs.Select(x => $"{x.Role} - {x.AssignedCount}/{x.MaxCount} ({x.SpawnChance}%)")), "CovRoleResult");
        if (Crews.Any()) Logger.Info(string.Join(", ", Crews.Select(x => $"{x.Role} - {x.AssignedCount}/{x.MaxCount} ({x.SpawnChance}%)")), "CrewRoleResult");

        if (Sunnyboy.CheckSpawn() && FinalRolesList.Remove(CustomRoles.Jester)) FinalRolesList.Add(CustomRoles.Sunnyboy);
        if (Bard.CheckSpawn() && FinalRolesList.Remove(CustomRoles.Arrogance)) FinalRolesList.Add(CustomRoles.Bard);

        if (Romantic.HasEnabled)
        {
            if (FinalRolesList.Contains(CustomRoles.Romantic) && FinalRolesList.Contains(CustomRoles.Lovers))
                FinalRolesList.Remove(CustomRoles.Lovers);
        }

        // If Roles are very few, add vanilla Crewmate Roles
        if (AllPlayers.Count > FinalRolesList.Count)
        {
            while (FinalRolesList.Count < AllPlayers.Count)
            {
                FinalRolesList.Add(CustomRoles.CrewmateTOHO);
            }
        }

        Logger.Info(string.Join(", ", FinalRolesList.Select(x => x.ToString())), "RoleResults");

        while (AllPlayers.Any() && FinalRolesList.Any())
        {
            // Shuffle all Players list
            if (AllPlayers.Count > 2)
                AllPlayers = AllPlayers.Shuffle(rd).Shuffle(rd).ToList();

            // Shuffle final Roles list
            if (FinalRolesList.Count > 2)
                FinalRolesList = FinalRolesList.Shuffle(rd).Shuffle(rd).ToList();

            // Select random Role and Player from list
            var randomPlayer = AllPlayers.RandomElement();
            var assignedRole = FinalRolesList.RandomElement();

            // Assign random Role for random Player
            RoleResult[randomPlayer.PlayerId] = assignedRole;
            Logger.Info($"Player：{randomPlayer.GetRealName()} => {assignedRole}", "RoleAssign");

            // Remove random Role and Player from list
            AllPlayers.Remove(randomPlayer);
            FinalRolesList.Remove(assignedRole);
        }

        if (AllPlayers.Any())
            Logger.Warn("Role assignment warirng: There are players who have not been assigned a role", "RoleAssign");
        if (FinalRolesList.Any())
            Logger.Warn("Team assignment warirng: There is an unassigned team", "RoleAssign");
        return;

        RoleAssignInfo GetAssignInfo(CustomRoles role) => Roles.Values.FirstOrDefault(x => x.Any(y => y.Role == role))?.FirstOrDefault(x => x.Role == role);
    }

    public static int AddScientistNum;
    public static int AddEngineerNum;
    public static int AddShapeshifterNum;
    public static int AddNoisemakerNum;
    public static int AddPhantomNum;
    public static int AddTrackerNum;
    public static void CalculateVanillaRoleCount()
    {
        // Calculate the number of base Roles
        AddEngineerNum = 0;
        AddScientistNum = 0;
        AddShapeshifterNum = 0;
        AddNoisemakerNum = 0;
        AddPhantomNum = 0;
        AddTrackerNum = 0;
        foreach (var role in AllRoles)
        {
            switch (role.GetVNRole())
            {
                case CustomRoles.Scientist:
                    AddScientistNum++;
                    break;
                case CustomRoles.Engineer:
                    AddEngineerNum++;
                    break;
                case CustomRoles.Shapeshifter:
                    AddShapeshifterNum++;
                    break;
                case CustomRoles.Noisemaker:
                    AddNoisemakerNum++;
                    break;
                case CustomRoles.Phantom:
                    AddPhantomNum++;
                    break;
                case CustomRoles.Tracker:
                    AddTrackerNum++;
                    break;
            }
        }
    }
}
