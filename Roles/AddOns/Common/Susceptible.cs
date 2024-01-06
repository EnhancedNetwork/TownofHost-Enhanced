using System;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;

namespace TOHE.Roles.AddOns.Common
{
    public class Susceptible
    {
        private static readonly int Id = 27000;
        public static OptionItem CanBeOnCrew;
        public static OptionItem CanBeOnImp;
        public static OptionItem CanBeOnNeutral;
        public static OptionItem EnabledDeathReasons;
       
        private static PlayerState.DeathReason randomReason;


        public static void SetupCustomOptions()
        {
            Options.SetupAdtRoleOptions(Id, CustomRoles.Susceptible, canSetNum: true, tab: TabGroup.Addons);
            EnabledDeathReasons = BooleanOptionItem.Create(Id + 11, "OnlyEnabledDeathReasons", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Susceptible]);
            CanBeOnImp = BooleanOptionItem.Create(Id + 12, "ImpCanBeSusceptible", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Susceptible]);
            CanBeOnCrew = BooleanOptionItem.Create(Id + 13, "CrewCanBeSusceptible", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Susceptible]);
            CanBeOnNeutral = BooleanOptionItem.Create(Id + 14, "NeutralCanBeSusceptible", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Susceptible]);
        }

         public static void ChangeRandomDeath()
        {
            PlayerState.DeathReason[] deathReasons = (PlayerState.DeathReason[])Enum.GetValues(typeof(PlayerState.DeathReason));
            Random random = new Random();
            int randomIndex = random.Next(deathReasons.Length);
            randomReason = deathReasons[randomIndex];
        }

        public static void CallEnabledAndChange(PlayerControl victim)
        {
            ChangeRandomDeath();

            if (EnabledDeathReasons.GetBool())
            {
                Logger.Info($"{victim.GetNameWithRole()} had the deathreason {randomReason}", "Susceptible");
                switch (randomReason) 
                { 
                    case PlayerState.DeathReason.Eaten:
                        if (!Pelican.IsEnable)
                        {
                            Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                        } 
                        else 
                        {
                            goto default;
                        }
                    break;

                    case PlayerState.DeathReason.Spell:
                        if (!Witch.IsEnable)
                        {
                            Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                        }
                        else
                        {
                            goto default;
                        }
                    break;

                    case PlayerState.DeathReason.Hex:
                        if (!HexMaster.IsEnable)
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
                        if (!Jinx.IsEnable)
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
                        if (!Vampire.IsEnable)
                        {
                            Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                        }
                        else
                        {
                            goto default;
                        }
                    break;

                    case PlayerState.DeathReason.Poison:
                        if (!Poisoner.IsEnable)
                        {
                            Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                        }
                        else
                        {
                            goto default;
                        }
                    break;

                    case PlayerState.DeathReason.Bombed:
                        if (!CustomRoles.Bomber.RoleExist() && !CustomRoles.Burst.RoleExist() && !CustomRoles.BoobyTrap.RoleExist() && !FireWorks.IsEnable)
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
                        if (!Sniper.IsEnable)
                        {
                            Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                        }
                        else
                        {
                            goto default;
                        }
                    break;
                    
                    case PlayerState.DeathReason.Revenge:
                        if (!CustomRoles.Avanger.RoleExist() && !CustomRoles.Retributionist.RoleExist() && !CustomRoles.Mafia.RoleExist())
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
                        if (!BallLightning.IsEnable)
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
                        if (!CustomRoles.OverKiller.RoleExist())
                        {
                            Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                        }
                        else
                        {
                            goto default;
                        }
                    break;

                    case PlayerState.DeathReason.LossOfHead:
                        if (!Hangman.IsEnable)
                        {
                            Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                        }
                        else
                        {
                            goto default;
                        }
                    break;

                    case PlayerState.DeathReason.Trialed:
                        if (!Judge.IsEnable && !Councillor.IsEnable)
                        {
                            Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                        }
                        else
                        {
                            goto default;
                        }
                    break;

                    case PlayerState.DeathReason.Infected:
                        if (!Infectious.IsEnable)
                        {
                            Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                        }
                        else
                        {
                            goto default;
                        }
                    break;

                    case PlayerState.DeathReason.Hack:
                        if (!Glitch.IsEnable)
                        {
                            Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                        }
                        else
                        {
                            goto default;
                        }
                    break;

                    case PlayerState.DeathReason.Pirate:
                        if (!Pirate.IsEnable)
                        {
                            Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                        }
                        else
                        {
                            goto default;
                        }
                    break;

                    case PlayerState.DeathReason.Shrouded:
                        if (!Shroud.IsEnable)
                        {
                            Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                        }
                        else
                        {
                            goto default;
                        }
                    break;

                    case PlayerState.DeathReason.Mauled:
                        if (!Werewolf.IsEnable)
                        {
                            Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                        }
                        else
                        {
                            goto default;
                        }
                    break;

                    case PlayerState.DeathReason.Targeted:
                        if (!Kamikaze.IsEnable)
                        {
                            Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                        }
                        else
                        {
                            goto default;
                        }
                    break;

                    default:
                        while(Main.PlayerStates[victim.PlayerId].deathReason != randomReason)
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
}
