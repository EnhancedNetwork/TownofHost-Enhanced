using System.Collections.Generic;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;
public static class Tricky
{
    private static readonly int Id = 19900;
    private static OptionItem EnabledDeathReasons;
    private static Dictionary<byte, PlayerState.DeathReason> randomReason = [];

    public static void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Tricky, canSetNum: true, tab: TabGroup.Addons);
        EnabledDeathReasons = BooleanOptionItem.Create(Id + 11, "OnlyEnabledDeathReasons", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tricky]);
    }
    public static void Init()
    {
        randomReason = [];
    }
    private static void ChangeRandomDeath(byte killerId)
    {
        PlayerState.DeathReason[] deathReasons = EnumHelper.GetAllValues<PlayerState.DeathReason>();
        var random = IRandom.Instance;
        int randomIndex = random.Next(deathReasons.Length);
        randomReason[killerId] = deathReasons[randomIndex];
    }

    private static void CallEnabledAndChange(PlayerControl victim, PlayerControl killer)
    {
        if (victim == null || killer == null) return;
        ChangeRandomDeath(killer.PlayerId);
        if (EnabledDeathReasons.GetBool())
        {
            Logger.Info($"{victim.GetNameWithRole().RemoveHtmlTags()} had the death reason {randomReason[killer.PlayerId]}", "Tricky");
            switch (randomReason[killer.PlayerId])
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
                    if (!CustomRoles.Bomber.RoleExist() && !CustomRoles.Burst.RoleExist() && !CustomRoles.BoobyTrap.RoleExist() && !Fireworker.IsEnable)
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

                default:
                    while (Main.PlayerStates[victim.PlayerId].deathReason != randomReason[killer.PlayerId])
                        Main.PlayerStates[victim.PlayerId].deathReason = randomReason[killer.PlayerId];
                    break;
            }
        }
        else
        {
            while (Main.PlayerStates[victim.PlayerId].deathReason != randomReason[killer.PlayerId])
                Main.PlayerStates[victim.PlayerId].deathReason = randomReason[killer.PlayerId];
        }

    }
    public static void AfterPlayerDeathTasks(PlayerControl target)
    {
        if (target == null) return;
        _ = new LateTask(() =>
        {
            var killer = target.GetRealKiller();
            if (killer == null || !killer.Is(CustomRoles.Tricky)) return;
            CallEnabledAndChange(target, killer);
            Main.PlayerStates[target.PlayerId].deathReason = randomReason[killer.PlayerId];
            Main.PlayerStates[target.PlayerId].SetDead();
        }, 0.3f, "Tricky random death reason");
    }
}