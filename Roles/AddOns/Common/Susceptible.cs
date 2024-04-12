using System;
using TOHE.Roles._Ghosts_.Crewmate;
using TOHE.Roles._Ghosts_.Impostor;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;

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
            switch (randomReason)
            {
                case PlayerState.DeathReason.Eaten:
                    if (!Pelican.HasEnabled)
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Spell:
                    if (!Witch.HasEnabled)
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Hex:
                    if (CustomRoles.HexMaster.IsEnable())
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Curse:
                    if (!CustomRoles.CursedWolf.RoleExist())
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Jinx:
                    if (!Jinx.HasEnabled)
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;


                case PlayerState.DeathReason.Shattered:
                    if (!CustomRoles.Lovers.RoleExist())
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;


                case PlayerState.DeathReason.Bite:
                    if (!Vampire.HasEnabled)
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Poison:
                    if (!Poisoner.HasEnabled)
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Bombed:
                    if (!Bomber.HasEnabled && !Burst.IsEnable && !Trapster.HasEnabled && !Fireworker.HasEnabled)
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Misfire:
                    if (!ChiefOfPolice.IsEnable)
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Torched:
                    if (!CustomRoles.Arsonist.RoleExist())
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Sniped:
                    if (!Sniper.HasEnabled)
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Revenge:
                    if (!CustomRoles.Avanger.RoleExist() && !CustomRoles.Retributionist.RoleExist() && !CustomRoles.Nemesis.RoleExist())
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Gambled:
                    if (!CustomRoles.EvilGuesser.RoleExist() && !CustomRoles.NiceGuesser.RoleExist() && !Options.GuesserMode.GetBool())
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Quantization:
                    if (!Lightning.HasEnabled)
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Overtired:
                    if (!CustomRoles.Workaholic.RoleExist())
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Ashamed:
                    if (!CustomRoles.Workaholic.RoleExist())
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.PissedOff:
                    if (!CustomRoles.Pestilence.RoleExist() && !CustomRoles.Provocateur.RoleExist())
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Dismembered:
                    if (!CustomRoles.Butcher.RoleExist())
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.LossOfHead:
                    if (!Hangman.HasEnabled)
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Trialed:
                    if (!Judge.HasEnabled && !Councillor.HasEnabled)
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Infected:
                    if (!Infectious.HasEnabled)
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Hack:
                    if (!Glitch.HasEnabled)
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Pirate:
                    if (!Pirate.HasEnabled)
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Shrouded:
                    if (!Shroud.HasEnabled)
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case PlayerState.DeathReason.Mauled:
                    if (!Werewolf.HasEnabled)
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;
                case PlayerState.DeathReason.Slice:
                    if (!Hawk.HasEnabled)
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;
                case PlayerState.DeathReason.BloodLet:
                    if (!Bloodmoon.HasEnabled)
                    {
                        Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                default:
                    while (Main.PlayerStates[victim.PlayerId].deathReason != randomReason)
                        Main.PlayerStates[victim.PlayerId].deathReason = randomReason;
                    break;
            }
        }
        else
        {
            while (Main.PlayerStates[victim.PlayerId].deathReason != randomReason)
                Main.PlayerStates[victim.PlayerId].deathReason = randomReason;
        }
    }
}
