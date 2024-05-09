using System;

namespace TOHE.Roles.AddOns.Common;

public class Susceptible
{
    private const int Id = 27100;
    public static OptionItem CanBeOnCrew;
    public static OptionItem CanBeOnImp;
    public static OptionItem CanBeOnNeutral;
    private static OptionItem EnabledDeathReasons;

    public static PlayerState.DeathReason randomReason;

    public static void SetupCustomOptions()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Susceptible, canSetNum: true, tab: TabGroup.Addons);
        EnabledDeathReasons = BooleanOptionItem.Create(Id + 11, "OnlyEnabledDeathReasons", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Susceptible]);
        CanBeOnImp = BooleanOptionItem.Create(Id + 12, "ImpCanBeSusceptible", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Susceptible]);
        CanBeOnCrew = BooleanOptionItem.Create(Id + 13, "CrewCanBeSusceptible", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Susceptible]);
        CanBeOnNeutral = BooleanOptionItem.Create(Id + 14, "NeutralCanBeSusceptible", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Susceptible]);
    }

    private static void ChangeRandomDeath()
    {
        PlayerState.DeathReason[] deathReasons = EnumHelper.GetAllValues<PlayerState.DeathReason>();
        Random random = new();
        int randomIndex = random.Next(deathReasons.Length);
        randomReason = deathReasons[randomIndex];
    }

    public static void CallEnabledAndChange(PlayerControl victim)
    {
        ChangeRandomDeath();
        if (EnabledDeathReasons.GetBool())
        {
            Logger.Info($"{victim.GetNameWithRole().RemoveHtmlTags()} had the death reason {randomReason}", "Susceptible");
            Main.PlayerStates[victim.PlayerId].deathReason = randomReason.DeathReasonIsEnable() ? randomReason : Main.PlayerStates[victim.PlayerId].deathReason;
            
        }
        else
        {
            Main.PlayerStates[victim.PlayerId].deathReason = randomReason.DeathReasonIsEnable(true) ? randomReason : Main.PlayerStates[victim.PlayerId].deathReason;
        }
    }
}
