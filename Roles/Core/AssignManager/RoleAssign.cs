using AmongUs.GameOptions;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using static TOHE.Modules.ShuffleListExtension;

namespace TOHE.Roles.Core.AssignManager;

public class RoleAssign
{
    public static Dictionary<byte, CustomRoles> SetRoles = [];
    public static Dictionary<PlayerControl, CustomRoles> RoleResult;
    public static CustomRoles[] AllRoles => [.. RoleResult.Values];

    enum RoleAssignType
    {
        Impostor,
        NeutralKilling,
        NonKillingNeutral,
        Crewmate
    }

    public class RoleAssignInfo(CustomRoles role, int spawnChance, int maxCount, int assignedCount = 0)
    {
        public CustomRoles Role { get => role; set => role = value; }
        public int SpawnChance { get => spawnChance; set => spawnChance = value; }
        public int MaxCount { get => maxCount; set => maxCount = value; }
        public int AssignedCount { get => assignedCount; set => assignedCount = value; }
    }

    public static void GetNeutralCounts(int NKmaxOpt, int NKminOpt, int NNKmaxOpt, int NNKminOpt, ref int ResultNKnum, ref int ResultNNKnum)
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
    }

    public static void StartSelect()
    {
        switch (Options.CurrentGameMode)
        {
            case CustomGameMode.FFA:
                RoleResult = [];
                foreach (PlayerControl pc in Main.AllAlivePlayerControls)
                {
                    RoleResult.Add(pc, CustomRoles.Killer);
                }
                return;
        }

        RoleResult = [];
        var rd = IRandom.Instance;
        int playerCount = Main.AllAlivePlayerControls.Length;
        int optImpNum = Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors);
        int optNonNeutralKillingNum = 0;
        int optNeutralKillingNum = 0;

        GetNeutralCounts(Options.NeutralKillingRolesMaxPlayer.GetInt(), Options.NeutralKillingRolesMinPlayer.GetInt(), Options.NonNeutralKillingRolesMaxPlayer.GetInt(), Options.NonNeutralKillingRolesMinPlayer.GetInt(), ref optNeutralKillingNum, ref optNonNeutralKillingNum);

        int readyRoleNum = 0;
        int readyImpNum = 0;
        int readyNonNeutralKillingNum = 0;
        int readyNeutralKillingNum = 0;

        List<CustomRoles> FinalRolesList = [];

        Dictionary<RoleAssignType, List<RoleAssignInfo>> Roles = [];

        Roles[RoleAssignType.Impostor] = [];
        Roles[RoleAssignType.NeutralKilling] = [];
        Roles[RoleAssignType.NonKillingNeutral] = [];
        Roles[RoleAssignType.Crewmate] = [];

        foreach (var id in SetRoles.Keys.Where(id => Utils.GetPlayerById(id) == null).ToArray()) SetRoles.Remove(id);

        foreach (var role in EnumHelper.GetAllValues<CustomRoles>())
        {
            int chance = role.GetMode();
            if (role.IsVanilla() || chance == 0 || role.IsAdditionRole() || role.IsGhostRole()) continue;
            switch (role)
            {
                case CustomRoles.DarkHide when GameStates.FungleIsActive:
                case CustomRoles.VengefulRomantic:
                case CustomRoles.RuthlessRomantic:
                case CustomRoles.GM:
                case CustomRoles.NotAssigned:
                    continue;
            }

            int count = role.GetCount();
            RoleAssignInfo info = new(role, chance, count);

            if (role.IsImpostor()) Roles[RoleAssignType.Impostor].Add(info);
            else if (role.IsNK()) Roles[RoleAssignType.NeutralKilling].Add(info);
            else if (role.IsNonNK()) Roles[RoleAssignType.NonKillingNeutral].Add(info);
            else Roles[RoleAssignType.Crewmate].Add(info);
        }

        if (Roles[RoleAssignType.Impostor].Count == 0 && !SetRoles.Values.Any(x => x.IsImpostor()))
        {
            Roles[RoleAssignType.Impostor].Add(new(CustomRoles.ImpostorTOHE, 100, optImpNum));
            Logger.Warn("Adding Vanilla Impostor", "CustomRoleSelector");
        }

        Logger.Info($"Number of NKs: {optNeutralKillingNum}, Number of NonNKs: {optNonNeutralKillingNum}", "NeutralNum");
        Logger.Msg("=====================================================", "AllActiveRoles");
        Logger.Info(string.Join(", ", Roles[RoleAssignType.Impostor].Select(x => $"{x.Role}: {x.SpawnChance}% - {x.MaxCount}")), "ImpRoles");
        Logger.Info(string.Join(", ", Roles[RoleAssignType.NeutralKilling].Select(x => $"{x.Role}: {x.SpawnChance}% - {x.MaxCount}")), "NKRoles");
        Logger.Info(string.Join(", ", Roles[RoleAssignType.NonKillingNeutral].Select(x => $"{x.Role}: {x.SpawnChance}% - {x.MaxCount}")), "NonNKRoles");
        Logger.Info(string.Join(", ", Roles[RoleAssignType.Crewmate].Select(x => $"{x.Role}: {x.SpawnChance}% - {x.MaxCount}")), "CrewRoles");
        Logger.Msg("=====================================================", "AllActiveRoles");

        IEnumerable<RoleAssignInfo> TempAlwaysImpRoles = Roles[RoleAssignType.Impostor].Where(x => x.SpawnChance == 100);
        IEnumerable<RoleAssignInfo> TempAlwaysNKRoles = Roles[RoleAssignType.NeutralKilling].Where(x => x.SpawnChance == 100);
        IEnumerable<RoleAssignInfo> TempAlwaysNNKRoles = Roles[RoleAssignType.NonKillingNeutral].Where(x => x.SpawnChance == 100);
        IEnumerable<RoleAssignInfo> TempAlwaysCrewRoles = Roles[RoleAssignType.Crewmate].Where(x => x.SpawnChance == 100);

        // DistinctBy - Removes duplicate roles if there are any
        // Shuffle - Shuffles all roles in the list into a randomized order
        // Take - Takes the first x roles of the list ... x is the maximum number of roles we could need of that team

        Roles[RoleAssignType.Impostor] = Roles[RoleAssignType.Impostor].Shuffle(rd).Take(optImpNum).ToList();
        Roles[RoleAssignType.NeutralKilling] = Roles[RoleAssignType.NeutralKilling].Shuffle(rd).Take(optNeutralKillingNum).ToList();
        Roles[RoleAssignType.NonKillingNeutral] = Roles[RoleAssignType.NonKillingNeutral].Shuffle(rd).Take(optNonNeutralKillingNum).ToList();
        Roles[RoleAssignType.Crewmate] = Roles[RoleAssignType.Crewmate].Shuffle(rd).Take(playerCount).ToList();

        Roles[RoleAssignType.Impostor].AddRange(TempAlwaysImpRoles);
        Roles[RoleAssignType.NeutralKilling].AddRange(TempAlwaysNKRoles);
        Roles[RoleAssignType.NonKillingNeutral].AddRange(TempAlwaysNNKRoles);
        Roles[RoleAssignType.Crewmate].AddRange(TempAlwaysCrewRoles);

        Roles[RoleAssignType.Impostor] = Roles[RoleAssignType.Impostor].DistinctBy(x => x.Role).ToList();
        Roles[RoleAssignType.NeutralKilling] = Roles[RoleAssignType.NeutralKilling].DistinctBy(x => x.Role).ToList();
        Roles[RoleAssignType.NonKillingNeutral] = Roles[RoleAssignType.NonKillingNeutral].DistinctBy(x => x.Role).ToList();
        Roles[RoleAssignType.Crewmate] = Roles[RoleAssignType.Crewmate].DistinctBy(x => x.Role).ToList();

        Logger.Msg("======================================================", "SelectedRoles");
        Logger.Info(string.Join(", ", Roles[RoleAssignType.Impostor].Select(x => x.Role.ToString())), "Selected-Impostor-Roles");
        Logger.Info(string.Join(", ", Roles[RoleAssignType.NeutralKilling].Select(x => x.Role.ToString())), "Selected-NK-Roles");
        Logger.Info(string.Join(", ", Roles[RoleAssignType.NonKillingNeutral].Select(x => x.Role.ToString())), "Selected-NonNK-Roles");
        Logger.Info(string.Join(", ", Roles[RoleAssignType.Crewmate].Select(x => x.Role.ToString())), "Selected-Crew-Roles");
        Logger.Msg("======================================================", "SelectedRoles");

        var AllPlayers = Main.AllAlivePlayerControls.ToList();

        // Players on the EAC banned list will be assigned as GM when opening rooms
        if (BanManager.CheckEACList(PlayerControl.LocalPlayer.FriendCode, PlayerControl.LocalPlayer.GetClient().GetHashedPuid()))
        {
            Main.EnableGM.Value = true;
            RoleResult[PlayerControl.LocalPlayer] = CustomRoles.GM;
            AllPlayers.Remove(PlayerControl.LocalPlayer);
        }

        // Pre-Assigned Roles By Host Are Selected First
        foreach (var item in SetRoles)
        {
            PlayerControl pc = AllPlayers.FirstOrDefault(x => x.PlayerId == item.Key);
            if (pc == null) continue;

            RoleResult[pc] = item.Value;
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
        RoleAssignInfo[] Crews = [];

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
                        AlwaysImpRoles.Add(item.Role);
                    }
                }
                else
                {
                    // Make "Spawn Chance ÷ 5 = x" (Example: 65 ÷ 5 = 13)
                    for (int j = 0; j < item.SpawnChance / 5; j++)
                    {
                        // Add 'MaxCount' (1) times
                        for (int k = 0; k < item.MaxCount; k++)
                        {
                            // Add Imp roles 'x' times (13)
                            ChanceImpRoles.Add(item.Role);
                        }
                    }
                }
            }

            RoleAssignInfo[] ImpRoleCounts = AlwaysImpRoles.Distinct().Select(GetAssignInfo).ToArray().AddRangeToArray(ChanceImpRoles.Distinct().Select(GetAssignInfo).ToArray());
            Imps = ImpRoleCounts;

            // Assign roles set to 100%
            if (readyImpNum < optImpNum)
            {
                while (AlwaysImpRoles.Count > 0)
                {
                    var selected = AlwaysImpRoles[rd.Next(0, AlwaysImpRoles.Count)];
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

            // Assign other roles when needed
            if (readyRoleNum < playerCount && readyImpNum < optImpNum)
            {
                while (ChanceImpRoles.Count > 0)
                {
                    var selectesItem = rd.Next(0, ChanceImpRoles.Count);
                    var selected = ChanceImpRoles[selectesItem];
                    var info = ImpRoleCounts.FirstOrDefault(x => x.Role == selected);

                    // Remove 'x' times
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
                            AlwaysNonNKRoles.Add(item.Role);
                        }
                    }
                    else
                    {
                        // Make "Spawn Chance ÷ 5 = x" (Example: 65 ÷ 5 = 13)
                        for (int j = 0; j < item.SpawnChance / 5; j++)
                        {
                            // Add 'MaxCount' (1) times
                            for (int k = 0; k < item.MaxCount; k++)
                            {
                                // Add Non-NK roles 'x' times (13)
                                ChanceNonNKRoles.Add(item.Role);
                            }
                        }
                    }
                }

                RoleAssignInfo[] NonNKRoleCounts = AlwaysNonNKRoles.Distinct().Select(GetAssignInfo).ToArray().AddRangeToArray(ChanceNonNKRoles.Distinct().Select(GetAssignInfo).ToArray());
                NNKs = NonNKRoleCounts;

                // Assign roles set to 100%
                if (readyNonNeutralKillingNum < optNonNeutralKillingNum)
                {
                    while (AlwaysNonNKRoles.Count > 0 && optNonNeutralKillingNum > 0)
                    {
                        var selected = AlwaysNonNKRoles[rd.Next(0, AlwaysNonNKRoles.Count)];
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

                // Assign other roles when needed
                if (readyRoleNum < playerCount && readyNonNeutralKillingNum < optNonNeutralKillingNum)
                {
                    while (ChanceNonNKRoles.Count > 0 && optNonNeutralKillingNum > 0)
                    {
                        var selectesItem = rd.Next(0, ChanceNonNKRoles.Count);
                        var selected = ChanceNonNKRoles[selectesItem];
                        var info = NonNKRoleCounts.FirstOrDefault(x => x.Role == selected);
                        
                        // Remove 'x' times
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
                            AlwaysNKRoles.Add(item.Role);
                        }
                    }
                    else
                    {
                        // Make "Spawn Chance ÷ 5 = x" (Example: 65 ÷ 5 = 13)
                        for (int j = 0; j < item.SpawnChance / 5; j++)
                        {
                            // Add 'MaxCount' (1) times
                            for (int k = 0; k < item.MaxCount; k++)
                            {
                                // Add NK roles 'x' times (13)
                                ChanceNKRoles.Add(item.Role);
                            }
                        }
                    }
                }

                RoleAssignInfo[] NKRoleCounts = AlwaysNKRoles.Distinct().Select(GetAssignInfo).ToArray().AddRangeToArray(ChanceNKRoles.Distinct().Select(GetAssignInfo).ToArray());
                NKs = NKRoleCounts;

                // Assign roles set to 100%
                if (readyNeutralKillingNum < optNeutralKillingNum)
                {
                    while (AlwaysNKRoles.Count > 0 && optNeutralKillingNum > 0)
                    {
                        var selected = AlwaysNKRoles[rd.Next(0, AlwaysNKRoles.Count)];
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

                // Assign other roles when needed
                if (readyRoleNum < playerCount && readyNeutralKillingNum < optNeutralKillingNum)
                {
                    while (ChanceNKRoles.Count > 0 && optNeutralKillingNum > 0)
                    {
                        var selectesItem = rd.Next(0, ChanceNKRoles.Count);
                        var selected = ChanceNKRoles[selectesItem];
                        var info = NKRoleCounts.FirstOrDefault(x => x.Role == selected);

                        // Remove 'x' times
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
                        AlwaysCrewRoles.Add(item.Role);
                    }
                }
                else
                {
                    // Make "Spawn Chance ÷ 5 = x" (Example: 65 ÷ 5 = 13)
                    for (int j = 0; j < item.SpawnChance / 5; j++)
                    {
                        // Add 'MaxCount' (1) times
                        for (int k = 0; k < item.MaxCount; k++)
                        {
                            // Add Crewmate roles 'x' times (13)
                            ChanceCrewRoles.Add(item.Role);
                        }
                    }
                }
            }

            RoleAssignInfo[] CrewRoleCounts = AlwaysCrewRoles.Distinct().Select(GetAssignInfo).ToArray().AddRangeToArray(ChanceCrewRoles.Distinct().Select(GetAssignInfo).ToArray());
            Crews = CrewRoleCounts;

            // Assign roles set to ALWAYS
            if (readyRoleNum < playerCount)
            {
                while (AlwaysCrewRoles.Count > 0)
                {
                    var selected = AlwaysCrewRoles[rd.Next(0, AlwaysCrewRoles.Count)];
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

            // Assign other roles when needed
            if (readyRoleNum < playerCount)
            {
                while (ChanceCrewRoles.Count > 0)
                {
                    var selectesItem = rd.Next(0, ChanceCrewRoles.Count);
                    var selected = ChanceCrewRoles[selectesItem];
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

        if (Imps.Length > 0) Logger.Info(string.Join(", ", Imps.Select(x => $"{x.Role} - {x.AssignedCount}/{x.MaxCount} ({x.SpawnChance}%)")), "ImpRoleResult");
        if (NNKs.Length > 0) Logger.Info(string.Join(", ", NNKs.Select(x => $"{x.Role} - {x.AssignedCount}/{x.MaxCount} ({x.SpawnChance}%)")), "NNKRoleResult");
        if (NKs.Length > 0) Logger.Info(string.Join(", ", NKs.Select(x => $"{x.Role} - {x.AssignedCount}/{x.MaxCount} ({x.SpawnChance}%)")), "NKRoleResult");
        if (Crews.Length > 0) Logger.Info(string.Join(", ", Crews.Select(x => $"{x.Role} - {x.AssignedCount}/{x.MaxCount} ({x.SpawnChance}%)")), "CrewRoleResult");

        if (rd.Next(0, 101) < Options.SunnyboyChance.GetInt() && FinalRolesList.Remove(CustomRoles.Jester)) FinalRolesList.Add(CustomRoles.Sunnyboy);
        if (Bard.CheckSpawn() && FinalRolesList.Remove(CustomRoles.Arrogance)) FinalRolesList.Add(CustomRoles.Bard);
        if (Bomber.CheckSpawnNuker() && FinalRolesList.Remove(CustomRoles.Bomber)) FinalRolesList.Add(CustomRoles.Nuker);

        if (Romantic.IsEnable)
        {
            if (FinalRolesList.Contains(CustomRoles.Romantic) && FinalRolesList.Contains(CustomRoles.Lovers))
                FinalRolesList.Remove(CustomRoles.Lovers);
        }

        Logger.Info(string.Join(", ", FinalRolesList.Select(x => x.ToString())), "RoleResults");

        while (AllPlayers.Count > 0 && FinalRolesList.Count > 0)
        {
            var roleId = rd.Next(0, FinalRolesList.Count);

            CustomRoles assignedRole = FinalRolesList[roleId];

            RoleResult[AllPlayers[0]] = assignedRole;
            Logger.Info($"Player：{AllPlayers[0].GetRealName()} => {assignedRole}", "RoleAssign");

            AllPlayers.RemoveAt(0);
            FinalRolesList.RemoveAt(roleId);
        }

        if (AllPlayers.Count > 0)
            Logger.Warn("Role assignment error: There are players who have not been assigned a role", "RoleAssign");
        if (FinalRolesList.Count > 0)
            Logger.Warn("Team assignment error: There is an unassigned team", "RoleAssign");
        return;

        RoleAssignInfo GetAssignInfo(CustomRoles role) => Roles.Values.FirstOrDefault(x => x.Any(y => y.Role == role))?.FirstOrDefault(x => x.Role == role);
    }

    public static int addScientistNum;
    public static int addEngineerNum;
    public static int addShapeshifterNum;
    public static void CalculateVanillaRoleCount()
    {
        // Calculate the number of base roles
        addEngineerNum = 0;
        addScientistNum = 0;
        addShapeshifterNum = 0;
        foreach (var role in AllRoles)
        {
            switch (role.GetVNRole())
            {
                case CustomRoles.Scientist:
                    addScientistNum++;
                    break;
                case CustomRoles.Engineer:
                    addEngineerNum++;
                    break;
                case CustomRoles.Shapeshifter:
                    addShapeshifterNum++;
                    break;
            }
        }
    }
}
