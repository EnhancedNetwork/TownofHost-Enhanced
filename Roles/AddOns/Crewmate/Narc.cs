using AmongUs.GameOptions;
using TOHE.Roles.Core.AssignManager;
using TOHE.Roles.Crewmate;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE;

public static class NarcManager
{
    //===========================SETUP================================\\
    public static CustomRoles RoleForNarcToSpawnAs;
    public static bool IsNarcAssigned() => RoleForNarcToSpawnAs != CustomRoles.NotAssigned;
    //==================================================================\\
    private static OptionItem NarcSpawnChance;
    private static OptionItem NarcCanUseSabotage;
    private static OptionItem NarcHasCrewVision;
    //public static OptionItem MadmateCanBeNarc;
    public static OptionItem ImpsCanKillEachOther;

    public static void SetUpOptionsForNarc(int id = 31400, CustomRoles role = CustomRoles.Narc, CustomGameMode customGameMode = CustomGameMode.Standard, TabGroup tab = TabGroup.Addons)
    {
        var spawnOption = StringOptionItem.Create(id, role.ToString(), EnumHelper.GetAllNames<RatesZeroOne>(), 0, tab, false).SetColor(Utils.GetRoleColor(role))
            .SetHeader(true)
            .SetGameMode(customGameMode) as StringOptionItem;

        NarcSpawnChance = IntegerOptionItem.Create(id + 2, "AdditionRolesSpawnRate", new(0, 100, 5), 65, tab, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(customGameMode);

        NarcCanUseSabotage = BooleanOptionItem.Create(id + 3, "NarcCanUseSabotage", true, tab, false)
            .SetParent(spawnOption)
            .SetGameMode(customGameMode);

        NarcHasCrewVision = BooleanOptionItem.Create(id + 4, "NarcHasCrewVision", false, tab, false)
            .SetParent(spawnOption)
            .SetGameMode(customGameMode);

        //MadmateCanBeNarc = BooleanOptionItem.Create(id + 5, "MadmateCanBeNarc", false, tab, false)
        //    .SetParent(spawnOption)
        //    .SetGameMode(customGameMode);

        ImpsCanKillEachOther = BooleanOptionItem.Create(id + 6, "ImpsCanKillEachOther", false, tab, false)
            .SetParent(spawnOption)
            .SetGameMode(customGameMode);


        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, 1, 1), 1, tab, false)
            .SetParent(spawnOption)
            .SetHidden(true)
            .SetGameMode(customGameMode);

        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }

    public static void InitForNarc()
    {
        RoleForNarcToSpawnAs = CustomRoles.NotAssigned;

        int value = IRandom.Instance.Next(1, 100);

        if (value <= NarcSpawnChance.GetInt() && CustomRoles.Narc.IsEnable())
        {
            List<CustomRoles> RolesEnabled = CustomRolesHelper.AllRoles
                                        .Where(r => r.IsEnable() && (r.IsImpostor() /*|| r.IsMadmate()*/) && !r.IsVanilla() && !r.IsGhostRole())
                                        .ToList();
            var RolesToSelect = new List<CustomRoles>();
            foreach (var improle in RolesEnabled)
            {
                if (RoleAssign.SetRoles.ContainsValue(improle)) continue;
                //if (improle.IsMadmate() && !MadmateCanBeNarc.GetBool()) continue;
                if (improle is CustomRoles.PhantomTOHE) continue;
                RolesToSelect.Add(improle);
            }

            if (!RolesToSelect.Any()) return;
            RolesToSelect = RolesToSelect.Shuffle().Shuffle().ToList();
            RoleForNarcToSpawnAs = RolesToSelect.RandomElement();
        }
    }


    public static void ApplyGameOptions(IGameOptions opt, PlayerControl player)
    {
        float vision = Utils.IsActive(SystemTypes.Electrical) ? Main.DefaultCrewmateVision / 5 : Main.DefaultCrewmateVision;
        if (NarcHasCrewVision.GetBool())
        {
            opt.SetVision(true);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, vision);
        }
    }

    public static bool NarcCanUseKillButton(PlayerControl pc)
        => !Main.AllAlivePlayerControls.Any(x => x.GetCustomRole().IsImpostorTeamV3() && !x.IsPlayerCrewmateTeam()) || ImpsCanKillEachOther.GetBool();
    public static bool CantUseSabotage(PlayerControl pc) => pc.Is(CustomRoles.Narc) && !NarcCanUseSabotage.GetBool();

    public static bool IsPolice(this PlayerControl player)
        => (player.Is(CustomRoles.Sheriff) || player.Is(CustomRoles.ChiefOfPolice))
            && player.IsPlayerCrewmateTeam() && !CopyCat.playerIdList.Contains(player.PlayerId);

    public static bool KnowRoleOfTarget(PlayerControl seer, PlayerControl target)
    {
        return (seer.IsPolice() && target.Is(CustomRoles.Narc)) ||
            (seer.Is(CustomRoles.Narc) && target.IsPolice());
    }

    public static string NarcAndPoliceSeeColor(PlayerControl seer, PlayerControl target)
    {
        var color = "";
        if (seer.Is(CustomRoles.Narc) && target.IsPolice())
            color = Main.roleColors[target.GetCustomRole()];
        if (seer.IsPolice() && target.Is(CustomRoles.Narc))
            color = Main.roleColors[CustomRoles.Narc];

        return color;
    }

    public static bool CheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (killer.CheckImpCanSeeAllies(CheckAsSeer: true)
            && target.CheckImpCanSeeAllies(CheckAsTarget: true)
            && !ImpsCanKillEachOther.GetBool())
            return false;

        if ((killer.IsPolice() && target.Is(CustomRoles.Narc)) ||
            (killer.Is(CustomRoles.Narc) && target.IsPolice()))
            return false;

        return true;
    }

    public static bool CheckBlockGuesses(PlayerControl guesser, PlayerControl target, bool isUI = false)
    {
        if (guesser.Is(CustomRoles.Narc) && target.IsPlayerCrewmateTeam())
        {
            if (target.Is(CustomRoles.Sheriff))
            {
                guesser.ShowInfoMessage(isUI, CopyCat.playerIdList.Contains(target.PlayerId) ? GetString("GuessImmune") : GetString("GuessSheriff"));
                return true;
            }
            if (target.Is(CustomRoles.ChiefOfPolice))
            {
                guesser.ShowInfoMessage(isUI, CopyCat.playerIdList.Contains(target.PlayerId) ? GetString("GuessImmune") : GetString("GuessCoP"));
                return true;
            }
        }

        if (guesser.IsPolice() && target.Is(CustomRoles.Narc))
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessNarc"));
            return true;
        }

        return false;
    }
}
