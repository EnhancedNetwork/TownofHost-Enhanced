using AmongUs.GameOptions;
using TOHE.Roles.Core.AssignManager;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE;

public static class NarcManager //I dont think it should be a RoleBase class,although idk why,btw thanks TOR for the inspiration
{
    //===========================SETUP================================\\
    public static CustomRoles RoleForNarcToSpawnAs;
    public static bool IsNarcAssigned() => RoleForNarcToSpawnAs != CustomRoles.NotAssigned;
    //==================================================================\\

    public static void InitForNarc()
    {
        RoleForNarcToSpawnAs = CustomRoles.NotAssigned;

        int value = IRandom.Instance.Next(1, 100);

        if (value <= NarcSpawnChance.GetInt() && CustomRoles.Narc.IsEnable())
        {
            List<CustomRoles> RolesEnabled = CustomRolesHelper.AllRoles
                                        .Where(r => r.IsEnable() && (r.IsImpostor() || r.IsMadmate()) && !r.IsVanilla())
                                        .ToList();
            var RolesToSelect = new List<CustomRoles>();
            foreach (var improle in RolesEnabled)
            {
                if (RoleAssign.SetRoles.ContainsValue(improle)) continue;
                if (improle.IsMadmate() && !MadmateCanBeNarc.GetBool()) continue;
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
        => !Main.AllAlivePlayerControls.Any(x => (x.Is(CustomRoles.Madmate) && x.Is(CountTypes.Impostor)) 
                                            || (x.GetCustomRole().IsImpostorTeamV3() && !x.IsPlayerCrewmateTeam())) 
                                        || ImpsCanKillEachOther.GetBool();
    public static bool CantUseSabotage(PlayerControl pc) => pc.Is(CustomRoles.Narc) && !NarcCanUseSabotage.GetBool();

    public static bool KnowRoleOfTarget(PlayerControl seer, PlayerControl target)
    {
        return (seer.GetCustomRole() is CustomRoles.Sheriff or CustomRoles.ChiefOfPolice && seer.IsPlayerCrewmateTeam() && target.Is(CustomRoles.Narc)) || 
            (seer.Is(CustomRoles.Narc) && target.GetCustomRole() is CustomRoles.Sheriff or CustomRoles.ChiefOfPolice && target.IsPlayerCrewmateTeam());
    }

    public static string NarcAndPoliceSeeColor(PlayerControl seer, PlayerControl target)
    {
        var color = "";
        if (seer.Is(CustomRoles.Narc) && target.GetCustomRole() is CustomRoles.ChiefOfPolice or CustomRoles.Sheriff && target.IsPlayerCrewmateTeam())
            color = Main.roleColors[target.GetCustomRole()];
        if (seer.GetCustomRole() is CustomRoles.ChiefOfPolice or CustomRoles.Sheriff && seer.IsPlayerCrewmateTeam() && target.Is(CustomRoles.Narc))
            color = Main.roleColors[CustomRoles.Narc];

        return color;
    }

    public static bool CheckMurderOnNarcPresence(PlayerControl killer, PlayerControl target)
    {
        if (killer.CheckImpCanSeeAllies(CheckAsSeer: true) 
            && target.CheckImpCanSeeAllies(CheckAsTarget: true) 
            && !ImpsCanKillEachOther.GetBool())
            return false;

        if ((killer.Is(CustomRoles.Sheriff) && killer.IsPlayerCrewmateTeam() && target.Is(CustomRoles.Narc)) || 
            (killer.Is(CustomRoles.Narc) && target.GetCustomRole() is CustomRoles.Sheriff or CustomRoles.ChiefOfPolice && target.IsPlayerCrewmateTeam()))
            return false;

        return true;
    }

    public static bool CheckBlockGuesses(PlayerControl guesser, PlayerControl target, bool isUI = false)
    {
        if (guesser.Is(CustomRoles.Narc) && target.IsPlayerCrewmateTeam())
        {
            if (target.Is(CustomRoles.Sheriff))
            {
                guesser.ShowInfoMessage(isUI, GetString("GuessSheriff"));
                return true;
            }
            if (target.Is(CustomRoles.ChiefOfPolice))
            {
                guesser.ShowInfoMessage(isUI, GetString("GuessCoP"));
                return true;
            }
        }

        if (guesser.GetCustomRole() is CustomRoles.ChiefOfPolice or CustomRoles.Sheriff && guesser.IsPlayerCrewmateTeam() && target.Is(CustomRoles.Narc))
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessNarc"));
            return true;
        }

        return false;
    }
}