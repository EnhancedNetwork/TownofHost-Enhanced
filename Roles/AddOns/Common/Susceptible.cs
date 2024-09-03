using System;

namespace TOHE.Roles.AddOns.Common;

public class Susceptible : IAddon
{
    private const int Id = 27100;
    public AddonTypes Type => AddonTypes.Mixed;
    private static OptionItem EnabledDeathReasons;

    public static PlayerState.DeathReason randomReason;

    public void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Susceptible, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
        EnabledDeathReasons = BooleanOptionItem.Create(Id + 11, "OnlyEnabledDeathReasons", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Susceptible]);
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
