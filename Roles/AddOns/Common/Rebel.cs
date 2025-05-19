using AmongUs.GameOptions;
using TOHE.Roles.Core.AssignManager;
using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.AddOns.Common;

public static class RebelManager
{
    /// <summary>
    /// List of roles that can be Rebel
    /// </summary>
    public static List<CustomRoles> SelectedRebelRoles()
    {
        var list = new List<CustomRoles>();
        foreach (var role in CustomRolesHelper.AllRoles
                                    .Where(r => r.IsEnable() && !r.IsVanilla() && !r.IsGhostRole() && !r.IsAdditionRole()))
        {
            if (!role.IsCrewmate() || RoleAssign.SetRoles.ContainsValue(role)) continue;
            if (role is CustomRoles.Altruist && !CanWinAfterDeath.GetBool()) continue;
            if (role is CustomRoles.Marshall && !MarshallCanBeRebel.GetBool()) continue;
            if (role is CustomRoles.Overseer && !OverseerCanBeRebel.GetBool()) continue;
            if (role is CustomRoles.Swapper && !SwapperCanBeRebel.GetBool()) continue;
            if (role is CustomRoles.Cleanser && !CleanserCanBeRebel.GetBool()) continue;
            if (role is CustomRoles.Reverie && !ReverieCanBeRebel.GetBool()) continue;
            if (role is CustomRoles.Sheriff && !SheriffCanBeRebel.GetBool()) continue;
            if (role is CustomRoles.Dictator && (!DictatorCanBeRebel.GetBool() || !CanWinAfterDeath.GetBool())) continue;
            if (role is CustomRoles.Retributionist && (!RetributionistCanBeRebel.GetBool() || !CanWinAfterDeath.GetBool())) continue;
            if (role is CustomRoles.Snitch or CustomRoles.Admirer or CustomRoles.NiceMini or CustomRoles.Vigilante or CustomRoles.CopyCat) continue;

            list.Add(role);
        }
        return list;
    }

    //===========================SETUP================================\\
    public static CustomRoles RoleForRebelToSpawnAs;
    public static bool IsRebelAssigned() => RoleForRebelToSpawnAs != CustomRoles.NotAssigned;
    //==================================================================\\

    public static OptionItem RebelSpawnChance;
    public static OptionItem SheriffCanBeRebel;
    public static OptionItem MarshallCanBeRebel;
    public static OptionItem OverseerCanBeRebel;
    public static OptionItem DictatorCanBeRebel;
    public static OptionItem RetributionistCanBeRebel;
    public static OptionItem SwapperCanBeRebel;
    public static OptionItem CleanserCanBeRebel;
    public static OptionItem ReverieCanBeRebel;
    public static OptionItem CanWinAfterDeath;
    public static OptionItem RebelHasImpVision;

    public static void SetUpOptionsForRebel(int id = 31900, CustomRoles role = CustomRoles.Rebel, CustomGameMode customGameMode = CustomGameMode.Standard, TabGroup tab = TabGroup.Addons)
    {
        var spawnOption = StringOptionItem.Create(id, role.ToString(), EnumHelper.GetAllNames<RatesZeroOne>(), 0, tab, false).SetColor(GetRoleColor(role))
            .SetHeader(true)
            .SetGameMode(customGameMode) as StringOptionItem;

        RebelSpawnChance = IntegerOptionItem.Create(id + 2, "AdditionRolesSpawnRate", new(0, 100, 5), 65, tab, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(customGameMode);
        
        SheriffCanBeRebel = BooleanOptionItem.Create(id + 3, "SheriffCanBeRebel", false, tab, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(customGameMode);
        
        MarshallCanBeRebel = BooleanOptionItem.Create(id + 4, "MarshallCanBeRebel", false, tab, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(customGameMode);
        
        OverseerCanBeRebel = BooleanOptionItem.Create(id + 5, "OverseerCanBeRebel", false, tab, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(customGameMode);
        
        DictatorCanBeRebel = BooleanOptionItem.Create(id + 6, "DictatorCanBeRebel", false, tab, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(customGameMode);
        
        RetributionistCanBeRebel = BooleanOptionItem.Create(id + 7, "RetributionistCanBeRebel", false, tab, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(customGameMode);
        
        SwapperCanBeRebel = BooleanOptionItem.Create(id + 8, "SwapperCanBeRebel", false, tab, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(customGameMode);
        
        CleanserCanBeRebel = BooleanOptionItem.Create(id + 9, "CleanserCanBeRebel", false, tab, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(customGameMode);
        
        ReverieCanBeRebel = BooleanOptionItem.Create(id + 10, "ReverieCanBeRebel", false, tab, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(customGameMode);

        CanWinAfterDeath = BooleanOptionItem.Create(id + 11, "CanWinAfterDeath", false, tab, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(customGameMode);

        RebelHasImpVision = BooleanOptionItem.Create(id + 12, "ImpostorVision", true, tab, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(customGameMode);


        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, 1, 1), 1, tab, false)
            .SetParent(spawnOption)
            .SetHidden(true)
            .SetGameMode(customGameMode);

        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }

    public static void InitForRebel()
    {
        RoleForRebelToSpawnAs = CustomRoles.NotAssigned;

        int value = IRandom.Instance.Next(1, 100);

        if (value <= RebelSpawnChance.GetInt() && CustomRoles.Rebel.IsEnable())
        {
            if (!SelectedRebelRoles().Any()) return;
            var RolesToSelect = SelectedRebelRoles().Shuffle().Shuffle().ToList();
            RoleForRebelToSpawnAs = RolesToSelect.RandomElement();
            Logger.Info("Select Role for Rebel:" + RoleForRebelToSpawnAs.ToString(), "RebelManager");
        }
    }

    public static void ApplyGameOptions(IGameOptions opt, PlayerControl player)
    {
        float vision = IsActive(SystemTypes.Electrical) ? Main.DefaultImpostorVision * 5 : Main.DefaultImpostorVision;
        if (RebelHasImpVision.GetBool())
        {
            opt.SetVision(true);
            opt.SetFloat(FloatOptionNames.CrewLightMod, vision);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
        }
    }

    public static bool CheckWinCondition(CustomWinner winner, PlayerControl pc)
        => winner is not CustomWinner.Crewmate && (CanWinAfterDeath.GetBool() || pc.IsAlive());
}
