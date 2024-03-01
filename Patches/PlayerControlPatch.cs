using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOHE.Modules;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.Core.AssignManager;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;
using TOHE.Roles.Core;
using static UnityEngine.RemoteConfigSettingsHelper;

namespace TOHE;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.OnEnable))]
class PlayerControlOnEnablePatch
{
    public static void Postfix(PlayerControl __instance)
    {
        //Shortly after this postfix, playercontrol is started but the amowner is not installed.
        //Need to delay for amowner to work
        _ = new LateTask(() =>
        {
            if (__instance.AmOwner)
            {
                Logger.Info("am owner version check, local player id is " + __instance.PlayerId, "PlayerControlOnEnable");
                RPC.RpcVersionCheck();
                return;
            }

            if (AmongUsClient.Instance.AmHost && __instance.PlayerId != PlayerControl.LocalPlayer.PlayerId)
            {
                Logger.Info("Host send version check, target player id is " + __instance.PlayerId, "PlayerControlOnEnable");
                RPC.RpcVersionCheck();
            }
        }, 0.2f, "Player Spawn LateTask ", false);

        //This late task happens where a playercontrol spawns, it will cause huge logs, so we have to hide it.
        //Its for host and joining client to recognize each other. Client and client recognize should be put in playerjoin latetask
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckProtect))]
class CheckProtectPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost || GameStates.IsHideNSeek) return false;
        Logger.Info("CheckProtect occurs: " + __instance.GetNameWithRole() + "=>" + target.GetNameWithRole(), "CheckProtect");
        var angel = __instance;
        var getAngelRole = angel.GetCustomRole();

        switch (getAngelRole)
        {

            case CustomRoles.Warden:
                return Warden.OnCheckProtect(angel, target);

            case CustomRoles.Minion:
                return Minion.OnCheckProtect(angel, target);

            case CustomRoles.Retributionist:
                return Retributionist.OnCheckProtect(angel, target);

            case CustomRoles.Nemesis:
                return Nemesis.OnCheckProtect(angel, target);

            default:
                break;
        }

        if (angel.Is(CustomRoles.EvilSpirit))
        {
            if (target.Is(CustomRoles.Spiritcaller))
            {
                Spiritcaller.ProtectSpiritcaller();
            }
            else
            {
                Spiritcaller.HauntPlayer(target);
            }
            angel.RpcResetAbilityCooldown();
            return false;
        }

        if (angel.Is(CustomRoles.Sheriff) && angel.Data.IsDead)
        {
                Logger.Info("Blocked protection", "CheckProtect");
                return false; // What is this for? sheriff dosen't become guardian angel lmao
        }
        
        return true;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckMurder))] // Local Side Click Kill Button
class CmdCheckMurderPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        Logger.Info($"{__instance.GetNameWithRole()} => {target.GetNameWithRole()}", "CmdCheckMurder");

        if (AmongUsClient.Instance.AmHost && GameStates.IsModHost)
            __instance.CheckMurder(target);
        else
        {
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.CheckMurder, SendOption.Reliable, -1);
            messageWriter.WriteNetObject(target);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }

        return false;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))] // Upon Receive RPC / Local Host
class CheckMurderPatch
{
    public static Dictionary<byte, float> TimeSinceLastKill = [];
    public static void Update()
    {
        for (byte i = 0; i < 15; i++)
        {
            if (TimeSinceLastKill.ContainsKey(i))
            {
                TimeSinceLastKill[i] += Time.deltaTime;
                if (15f < TimeSinceLastKill[i]) TimeSinceLastKill.Remove(i);
            }
        }
    }
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (GameStates.IsHideNSeek) return true;

        var killer = __instance; // Alternative variable

        var killerRole = __instance.GetCustomRole();
        var targetRole = target.GetCustomRole();

        Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} => {target.GetNameWithRole().RemoveHtmlTags()}", "CheckMurder");

        // Killer is already dead
        if (!killer.IsAlive())
        {
            Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} was cancelled because it is dead", "CheckMurder");
            return false;
        }

        // Is the target in a killable state?
        if (target.Data == null // Check if PlayerData is not null
            // Check target status
            || target.inVent
            || target.inMovingPlat // Moving Platform on Airhip and Zipline on Fungle
            || target.MyPhysics.Animations.IsPlayingEnterVentAnimation()
            || target.MyPhysics.Animations.IsPlayingAnyLadderAnimation()
        )
        {
            Logger.Info("The target is in an unkillable state and the kill is canceled", "CheckMurder");
            return false;
        }
        // Target Is Dead?
        if (!target.IsAlive())
        {
            Logger.Info("The target is in a dead state and the kill is canceled", "CheckMurder");
            return false;
        }
        // Checking during the meeting
        if (MeetingHud.Instance != null)
        {
            Logger.Info("In the meeting, the kill was canceled", "CheckMurder");
            return false;
        }

        var divice = Options.CurrentGameMode == CustomGameMode.FFA ? 3000f : 2000f;
        float minTime = Mathf.Max(0.02f, AmongUsClient.Instance.Ping / divice * 6f); //Ping value is milliseconds (ms), so ÷ 2000
        // No value is stored in TimeSinceLastKill || Stored time is greater than or equal to minTime => Allow kill

        //↓ If not permitted
        if (TimeSinceLastKill.TryGetValue(killer.PlayerId, out var time) && time < minTime)
        {
            Logger.Info("Kill intervals are too short and kills are canceled", "CheckMurder");
            return false;
        }
        TimeSinceLastKill[killer.PlayerId] = 0f;

        //FFA
        if (Options.CurrentGameMode == CustomGameMode.FFA)
        {
            FFAManager.OnPlayerAttack(killer, target);
            return true;
        }

        // Penguin's victim unable to kill
        if (Penguin.AbductVictim != null && killer.PlayerId == Penguin.AbductVictim.PlayerId)
        {
            killer.Notify(GetString("PenguinTargetOnCheckMurder"));
            killer.SetKillCooldown(5);
            return false;
        }

        // added here because it bypasses every shield and just kills the player and antidote, diseased etc.. wont take effect
        if (killer.Is(CustomRoles.KillingMachine))
        {
            killer.RpcMurderPlayerV3(target);
            killer.ResetKillCooldown();
            return false;
        }

        if (killerRole.Is(CustomRoles.Chronomancer))
            Chronomancer.OnCheckMurder(killer);

        killer.ResetKillCooldown();

        // killable decision
        if (killer.PlayerId != target.PlayerId && !killer.CanUseKillButton())
        {
            Logger.Info(killer.GetNameWithRole().RemoveHtmlTags() + " The hitter is not allowed to use the kill button and the kill is canceled", "CheckMurder");
            return false;
        }

        // Replacement process when the actual killer and the KILLER are different
        if (Sniper.IsEnable)
        {
            Sniper.TryGetSniper(target.PlayerId, ref killer);
        }

        if (killer != __instance)
        {
            Logger.Info($"Real Killer = {killer.GetNameWithRole().RemoveHtmlTags()}", "CheckMurder");
        }

        var killerRoleClass = killer.GetRoleClass();
        var targetRoleClass = target.GetRoleClass();

        if (!targetRoleClass.OnCheckMurderAsTarget(killer, target))
        {
            return false;
        }

        if (!killerRoleClass.OnCheckMurderAsKiller(killer, target))
        {
            return false;
        }


        foreach (var targetSubRole in target.GetCustomSubRoles().ToArray())
        {
            switch (targetSubRole)
            {
                case CustomRoles.Diseased:
                    Diseased.CheckMurder(killer);
                    break;

                case CustomRoles.Antidote:
                    Antidote.CheckMurder(killer);
                    break;

                case CustomRoles.Susceptible:
                    Susceptible.CallEnabledAndChange(target);
                    break;

                case CustomRoles.Fragile:
                    if (Fragile.KillFragile(killer, target))
                        return false;
                    break;

                case CustomRoles.Aware:
                    Aware.OnCheckMurder(killerRole, target);
                    break;
            }
        }

        switch (targetRole)
        {
            case CustomRoles.SchrodingersCat:
                if (!SchrodingersCat.OnCheckMurder(killer, target)) return false;
                break;
            case CustomRoles.Shaman:
                if (Main.ShamanTarget != byte.MaxValue && target.IsAlive())
                {
                    target = Utils.GetPlayerById(Main.ShamanTarget);
                    Main.ShamanTarget = byte.MaxValue;
                }
                break;
            case CustomRoles.Solsticer:
                if (Solsticer.OnCheckMurder(killer, target))
                    return false;
                break;
        }

        if (Mastermind.ManipulatedPlayers.ContainsKey(killer.PlayerId))
        {
            return Mastermind.ForceKillForManipulatedPlayer(killer, target);
        }

        killerRole = killer.GetCustomRole();
        targetRole = target.GetCustomRole();

        //Is eaten player can't be killed.
        if (Pelican.IsEaten(target.PlayerId))
            return false;

        // Fake Check
        if (Counterfeiter.IsEnable && Counterfeiter.OnClientMurder(killer))
            return false;

        if (Pursuer.IsEnable && Pursuer.OnClientMurder(killer))
            return false;

        if (Glitch.IsEnable && Glitch.hackedIdList.ContainsKey(killer.PlayerId))
        {
            killer.Notify(string.Format(GetString("HackedByGlitch"), GetString("GlitchKill")));
            return false;
        }

        if (targetRole.Is(CustomRoles.Necromancer) && !Necromancer.OnKillAttempt(killer, target))
            return false;

        else if (targetRole.Is(CustomRoles.Spy) && !Spy.OnKillAttempt(killer, target))
            return false;

        if (Alchemist.IsProtected && targetRole.Is(CustomRoles.Alchemist))
        {
            killer.SetKillCooldown(time: 5f);
            return false;
        }

        // if not suicide
        if (killer.PlayerId != target.PlayerId)
        {
            // Triggered only in non-suicide scenarios
            switch (killerRole)
            {
                //==========On Check Murder==========//
                case CustomRoles.Mercenary:
                    Mercenary.OnCheckMurder(killer);
                    break;
                case CustomRoles.Vampire:
                    if (!Vampire.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Vampiress:
                    if (!Vampiress.OnCheckKill(killer, target)) return false;
                    break;
                case CustomRoles.Pyromaniac:
                    if (!Pyromaniac.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Kamikaze:
                    if (!Kamikaze.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Poisoner:
                    if (!Poisoner.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Witness:
                    killer.SetKillCooldown();
                    if (Main.AllKillers.ContainsKey(target.PlayerId))
                        killer.Notify(GetString("WitnessFoundKiller"));
                    else 
                        killer.Notify(GetString("WitnessFoundInnocent"));
                    return false;
                case CustomRoles.Undertaker:
                    if (!Undertaker.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Warlock:
                    if (!Main.CheckShapeshift[killer.PlayerId] && !Main.isCurseAndKill[killer.PlayerId])
                    { //Warlockが変身時以外にキルしたら、呪われる処理
                        if (target.Is(CustomRoles.Needy) || target.Is(CustomRoles.Lazy) || target.Is(CustomRoles.NiceMini) && Mini.Age < 18) return false;
                        Main.isCursed = true;
                        killer.SetKillCooldown();
                        //RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                        killer.RPCPlayCustomSound("Line");
                        Main.CursedPlayers[killer.PlayerId] = target;
                        Main.WarlockTimer.Add(killer.PlayerId, 0f);
                        Main.isCurseAndKill[killer.PlayerId] = true;
                        //RPC.RpcSyncCurseAndKill();
                        return false;
                    }
                    if (Main.CheckShapeshift[killer.PlayerId])
                    {//呪われてる人がいないくて変身してるときに通常キルになる
                        killer.RpcCheckAndMurder(target);
                        return false;
                    }
                    if (Main.isCurseAndKill[killer.PlayerId]) killer.RpcGuardAndKill(target);
                    return false;
                case CustomRoles.Ninja:
                    if (!Ninja.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Witch:
                    if (!Witch.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.HexMaster:
                    if (!HexMaster.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.PlagueDoctor:
                    if (!PlagueDoctor.OnPDinfect(killer, target)) return false;
                    break;
                case CustomRoles.Glitch:
                    if (!Glitch.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Huntsman:
                    Huntsman.OnCheckMurder(killer, target);
                    break;
                //case CustomRoles.Occultist:
                //    if (!Occultist.OnCheckMurder(killer, target)) return false;
                //    break;
                case CustomRoles.Puppeteer:
                    if (!Puppeteer.OnCheckPuppet(killer, target)) return false;
                    break;
                case CustomRoles.Mastermind:
                    if (!Mastermind.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Necromancer: //必须在击杀发生前处理
                    if (!Necromancer.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Shroud:
                    if (!Shroud.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Gangster:
                    if (Gangster.OnCheckMurder(killer, target))
                        return false;
                    break;
                case CustomRoles.Lightning:
                    if (Lightning.OnCheckMurderAsKiller(killer, target))
                        return false;
                    break;
                case CustomRoles.Greedy:
                    Greedy.OnCheckMurder(killer);
                    break;
                case CustomRoles.QuickShooter:
                    QuickShooter.QuickShooterKill(killer);
                    break;
                case CustomRoles.Juggernaut:
                    Juggernaut.OnCheckMurder(killer);
                    break;
                case CustomRoles.Penguin:
                    if (!Penguin.OnCheckMurderAsKiller(killer, target)) return false;
                    break;
                case CustomRoles.Reverie:
                    Reverie.OnCheckMurder(killer, target);
                    break;
                case CustomRoles.Hangman:
                    if (!Hangman.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Swooper:
                    if (!Swooper.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Wraith:
                    if (!Wraith.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Lurker:
                    Lurker.OnCheckMurder(killer);
                    break;
                case CustomRoles.Crusader:
                    Crusader.OnCheckMurder(killer, target);
                    return false;

                case CustomRoles.PlagueBearer:
                    if (!PlagueBearer.OnCheckMurder(killer, target))
                        return false;
                    break;
                case CustomRoles.Pirate:
                    if (!Pirate.OnCheckMurder(killer, target))
                        return false;
                    break;
                case CustomRoles.Pixie:
                    Pixie.OnCheckMurder(killer, target);
                    return false;

                case CustomRoles.Arsonist:
                    killer.SetKillCooldown(Options.ArsonistDouseTime.GetFloat());
                    if (!Main.isDoused[(killer.PlayerId, target.PlayerId)] && !Main.ArsonistTimer.ContainsKey(killer.PlayerId))
                    {
                        Main.ArsonistTimer.Add(killer.PlayerId, (target, 0f));
                        Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
                        RPC.SetCurrentDousingTarget(killer.PlayerId, target.PlayerId);
                    }
                    return false;
                case CustomRoles.Revolutionist:
                    killer.SetKillCooldown(Options.RevolutionistDrawTime.GetFloat());
                    if (!Main.isDraw[(killer.PlayerId, target.PlayerId)] && !Main.RevolutionistTimer.ContainsKey(killer.PlayerId))
                    {
                        Main.RevolutionistTimer.TryAdd(killer.PlayerId, (target, 0f));
                        Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
                        RPC.SetCurrentDrawTarget(killer.PlayerId, target.PlayerId);
                    }
                    return false;
                case CustomRoles.Innocent:
                    target.RpcMurderPlayerV3(killer);
                    return false;
                case CustomRoles.Pelican:
                    if (Pelican.CanEat(killer, target.PlayerId))
                    {
                        Pelican.EatPlayer(killer, target);
                        if (!Options.DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(killer);
                        killer.SetKillCooldown();
                        killer.RPCPlayCustomSound("Eat");
                        target.RPCPlayCustomSound("Eat");
                    }
                    else
                    {
                        killer.SetKillCooldown();
                        killer.Notify(GetString("Pelican.TargetCannotBeEaten"));
                    }
                    return false;
                case CustomRoles.Hater:
                    if (!Hater.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Gamer:
                    Gamer.CheckGamerMurder(killer, target);
                    return false;
                case CustomRoles.DarkHide:
                    DarkHide.OnCheckMurder(killer, target);
                    break;
                case CustomRoles.Provocateur:
                    if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
                    {
                        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceMini), GetString("CantBoom")));
                        return false;
                    }
                    Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.PissedOff;
                    killer.RpcMurderPlayerV3(target);
                    killer.RpcMurderPlayerV3(killer);
                    killer.SetRealKiller(target);
                    Main.Provoked.TryAdd(killer.PlayerId, target.PlayerId);
                    return false;
                case CustomRoles.Totocalcio:
                    Totocalcio.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Romantic:
                    if (!Romantic.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.VengefulRomantic:
                    if (!VengefulRomantic.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Succubus:
                    Succubus.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.CursedSoul:
                    CursedSoul.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Admirer:
                    Admirer.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Imitator:
                    Imitator.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Infectious:
                    Infectious.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Deputy:
                    Deputy.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Investigator:
                    Investigator.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Jackal:
                    if (Jackal.OnCheckMurder(killer, target))
                        return false;
                    break;
                case CustomRoles.Bandit:
                    if (!Bandit.OnCheckMurder(killer, target))
                    {
                        Bandit.killCooldown[killer.PlayerId] = Bandit.StealCooldown.GetFloat();
                        return false;
                    }
                    else Bandit.killCooldown[killer.PlayerId] = Bandit.KillCooldownOpt.GetFloat();
                    killer.ResetKillCooldown();
                    killer.SyncSettings();
                    break;
                case CustomRoles.Shaman:
                    if (Main.ShamanTargetChoosen == false)
                    {
                        Main.ShamanTarget = target.PlayerId;
                        killer.RpcGuardAndKill(killer);
                        Main.ShamanTargetChoosen = true;
                    }
                    else killer.Notify(GetString("ShamanTargetAlreadySelected"));
                    return false;
                case CustomRoles.Agitater:
                    if (!Agitater.OnCheckMurder(killer, target))
                        return false;
                    break;

                case CustomRoles.Sheriff:
                    if (!Sheriff.OnCheckMurder(killer, target))
                        return false;
                    break;
                case CustomRoles.Jailer:
                    if (!Jailer.OnCheckMurder(killer, target))
                        return false;
                    break;

                case CustomRoles.SwordsMan:
                    if (!SwordsMan.OnCheckMurder(killer))
                        return false;
                    break;
                case CustomRoles.Medic:
                    Medic.OnCheckMurderFormedicaler(killer, target);
                    return false;
                case CustomRoles.Counterfeiter:
                    if (target.Is(CustomRoles.Pestilence)) break;
                    if (target.Is(CustomRoles.SerialKiller)) return true;
                    if (Counterfeiter.CanBeClient(target) && Counterfeiter.CanSeel(killer.PlayerId))
                        Counterfeiter.SeelToClient(killer, target);
                    return false;
                case CustomRoles.Vigilante:
                    if (killer.Is(CustomRoles.Madmate)) break;
                    if (target.GetCustomRole().IsCrewmate())
                    {
                        killer.RpcSetCustomRole(CustomRoles.Madmate);
                        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("VigilanteNotify")));
                        //Utils.NotifyRoles(SpecifySeer: killer);
                        Utils.MarkEveryoneDirtySettings();
                    }
                    break;
                case CustomRoles.Pursuer:
                    if (target.Is(CustomRoles.Pestilence)) break;
                    if (target.Is(CustomRoles.SerialKiller)) return true;
                    if (Pursuer.CanBeClient(target) && Pursuer.CanSeel(killer.PlayerId))
                        Pursuer.SeelToClient(killer, target);
                    return false;
                case CustomRoles.ChiefOfPolice:
                    ChiefOfPolice.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Quizmaster:
                    if (!Quizmaster.OnCheckMurder(killer, target))
                        return false;
                    break;
            }
        }

        if (!killer.RpcCheckAndMurder(target, true))
            return false;

        if (targetRole.Is(CustomRoles.Merchant) && Merchant.OnClientMurder(killer, target))
            return false;

        switch (killerRole)
        {
            case CustomRoles.Virus:
                Virus.OnCheckMurder(killer, target);
                break;

            case CustomRoles.Spiritcaller:
                Spiritcaller.OnCheckMurder(target);
                break;

            case CustomRoles.Ludopath:
                var ran = IRandom.Instance;
                int KillCD = ran.Next(1, Options.LudopathRandomKillCD.GetInt());
                {
                    Main.AllPlayerKillCooldown[killer.PlayerId] = KillCD;
                }
                break;

            case CustomRoles.Werewolf:
                Logger.Info("Werewolf Kill", "Mauled");
                _ = new LateTask(() =>
                {
                    foreach (var player in Main.AllAlivePlayerControls)
                    {
                        if (player == killer) continue;
                        if (player == target) continue;

                        if (player.Is(CustomRoles.Pestilence)) continue;
                        else if ((player.Is(CustomRoles.NiceMini) || player.Is(CustomRoles.EvilMini)) && Mini.Age < 18) continue;

                        if (Vector2.Distance(killer.transform.position, player.transform.position) <= Werewolf.MaulRadius.GetFloat())
                        {
                            Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Mauled;
                            player.SetRealKiller(killer);
                            player.RpcMurderPlayerV3(player);
                        }
                    }
                }, 0.1f, "Werewolf Maul Bug Fix");
                break;

            case CustomRoles.PotionMaster:
                if (!PotionMaster.OnCheckMurder(killer, target))
                    return false;
                break;

            case CustomRoles.Scavenger:
                if (!targetRole.Is(CustomRoles.Pestilence))
                {
                    target.RpcTeleport(ExtendedPlayerControl.GetBlackRoomPosition());
                    target.SetRealKiller(killer);
                    Main.PlayerStates[target.PlayerId].SetDead();
                    target.RpcMurderPlayerV3(target);
                    killer.SetKillCooldown();
                    RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                    NameNotifyManager.Notify(target, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), GetString("KilledByScavenger")));
                    return false;
                }
                else
                {
                    target.RpcMurderPlayerV3(target);
                    target.SetRealKiller(killer);
                    return false;
                }
        }

        foreach (var killerSubRole in killer.GetCustomSubRoles().ToArray())
        {
            switch (killerSubRole)
            {
                case CustomRoles.Mare:
                    if (Mare.IsLightsOut)
                        return false;
                    break;

                case CustomRoles.Unlucky:
                    Unlucky.SuicideRand(killer);
                    if (Unlucky.UnluckCheck[killer.PlayerId]) return false;
                    break;

                case CustomRoles.Tired:
                    Tired.AfterActionTasks(killer);
                    break;

                case CustomRoles.Clumsy:
                    if (!Clumsy.OnCheckMurder(killer))
                        return false;
                    break;

                case CustomRoles.Swift:
                    if (!Swift.OnCheckMurder(killer, target))
                        return false;
                    break;
            }
        }

        //== Kill target ==
        __instance.RpcMurderPlayerV3(target);
        //============

        return false;
    }

    public static bool RpcCheckAndMurder(PlayerControl killer, PlayerControl target, bool check = false)
    {
        if (!AmongUsClient.Instance.AmHost) return false;

        if (target == null) target = killer;

        var killerRole = killer.GetCustomRole();
        var targetRole = target.GetCustomRole();

        if (Jackal.ResetKillCooldownWhenSbGetKilled.GetBool() && !killerRole.Is(CustomRoles.Sidekick) && !killerRole.Is(CustomRoles.Jackal) && !target.Is(CustomRoles.Sidekick) && !target.Is(CustomRoles.Jackal) && !GameStates.IsMeeting)
            Jackal.AfterPlayerDiedTask(killer);

        if (CustomRoles.Benefactor.IsClassEnable() 
            && !Main.PlayerStates.Any(x => x.Value.MainRole == CustomRoles.Benefactor && x.Value.Role.OnCheckMurderAsTarget(killer, target)))
            return false;

        // Romantic partner is protected
        if (Romantic.isPartnerProtected && Romantic.BetPlayer.ContainsValue(target.PlayerId))
            return false;

        if (Medic.IsEnable && Medic.OnCheckMurder(killer, target))
            return false;

        // Impostors can kill Madmate
        if (killer.Is(CustomRoleTypes.Impostor) && !Madmate.ImpCanKillMadmate.GetBool() && target.Is(CustomRoles.Madmate))
            return false;

        if (!Jackal.JackalCanKillSidekick.GetBool())
        {
            // Jackal can kill Sidekick/Recruit
            if (killerRole.Is(CustomRoles.Jackal) && (targetRole.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit)))
                return false;

            // Sidekick/Recruit can kill Jackal
            else if ((killerRole.Is(CustomRoles.Sidekick) || killer.Is(CustomRoles.Recruit)) && targetRole.Is(CustomRoles.Jackal))
                return false;
        }
        
        if (!Jackal.SidekickCanKillSidekick.GetBool())
        {
            // Sidekick can kill Sidekick/Recruit
            if (killer.Is(CustomRoles.Sidekick) && (target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit)))
                return false;

            // Recruit can kill Recruit/Sidekick
            if (killer.Is(CustomRoles.Recruit) && (target.Is(CustomRoles.Recruit) || target.Is(CustomRoles.Sidekick)))
                return false;
        }

        switch (killerRole)
        {
            case CustomRoles.Traitor when target.Is(CustomRoleTypes.Impostor):
            case CustomRoles.Traitor when target.Is(CustomRoles.Traitor):
            case CustomRoles.SerialKiller when target.Is(CustomRoles.SerialKiller):
            case CustomRoles.Juggernaut when target.Is(CustomRoles.Juggernaut):
            case CustomRoles.Werewolf when target.Is(CustomRoles.Werewolf):
            case CustomRoles.Shroud when target.Is(CustomRoles.Shroud):
            case CustomRoles.Jinx when target.Is(CustomRoles.Jinx):
            case CustomRoles.Wraith when target.Is(CustomRoles.Wraith):
            case CustomRoles.HexMaster when target.Is(CustomRoles.HexMaster):
            //case CustomRoles.Occultist when target.Is(CustomRoles.Occultist):
            case CustomRoles.BloodKnight when target.Is(CustomRoles.BloodKnight):
            case CustomRoles.Jackal when target.Is(CustomRoles.Jackal):
            case CustomRoles.Pelican when target.Is(CustomRoles.Pelican):
            case CustomRoles.Poisoner when target.Is(CustomRoles.Poisoner):
            case CustomRoles.Infectious when target.Is(CustomRoles.Infectious):
            case CustomRoles.Virus when target.Is(CustomRoles.Virus):
            case CustomRoles.Parasite when target.Is(CustomRoles.Parasite):
            case CustomRoles.DarkHide when target.Is(CustomRoles.DarkHide):
            case CustomRoles.Pickpocket when target.Is(CustomRoles.Pickpocket):
            case CustomRoles.Spiritcaller when target.Is(CustomRoles.Spiritcaller):
            case CustomRoles.Medusa when target.Is(CustomRoles.Medusa):
            case CustomRoles.PotionMaster when target.Is(CustomRoles.PotionMaster):
            case CustomRoles.Glitch when target.Is(CustomRoles.Glitch):
            case CustomRoles.Succubus when target.Is(CustomRoles.Succubus):
            case CustomRoles.Refugee when target.Is(CustomRoles.Refugee):
            case CustomRoles.Infectious when target.Is(CustomRoles.Infected):
                return false;
        }

        foreach (var killerSubRole in killer.GetCustomSubRoles().ToArray())
        {
            switch (killerSubRole)
            {
                case CustomRoles.Madmate when target.Is(CustomRoleTypes.Impostor) && !Madmate.MadmateCanKillImp.GetBool():
                case CustomRoles.Infected when target.Is(CustomRoles.Infected) && !Infectious.TargetKnowOtherTarget.GetBool():
                case CustomRoles.Infected when target.Is(CustomRoles.Infectious):
                    return false;
            }
        }

        if (target.Is(CustomRoles.Lucky))
        {
            if (!Lucky.OnCheckMurder(killer, target))
                return false;
        }

        // Shield Player
        if (Main.ShieldPlayer != "" && Main.ShieldPlayer == target.GetClient().GetHashedPuid() && Utils.IsAllAlive)
        {
            Main.ShieldPlayer = "";
            killer.RpcGuardAndKill(target);
            killer.SetKillCooldown(forceAnime: true);
            return false;
        }

        // Madmate Spawn Mode Is First Kill
        if (Madmate.MadmateSpawnMode.GetInt() == 1 && Main.MadmateNum < CustomRoles.Madmate.GetCount() && target.CanBeMadmate(inGame:true))
        {
            Main.MadmateNum++;
            target.RpcSetCustomRole(CustomRoles.Madmate);
            ExtendedPlayerControl.RpcSetCustomRole(target.PlayerId, CustomRoles.Madmate);
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("BecomeMadmateCuzMadmateMode")));
            killer.SetKillCooldown();
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);
            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Madmate.ToString(), "Assign " + CustomRoles.Madmate.ToString());
            return false;
        }

        switch (targetRole)
        {
            case CustomRoles.Opportunist when Options.OppoImmuneToAttacksWhenTasksDone.GetBool() && target.AllTasksCompleted():
            case CustomRoles.Guardian when target.AllTasksCompleted():
            case CustomRoles.Monarch when CustomRoles.Knighted.RoleExist():
                return false;
            case CustomRoles.Pestilence: // 🗿🗿
                if (killer != null && killer != target)
                    { Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.PissedOff; ExtendedPlayerControl.RpcMurderPlayerV3(killer, target); }
                    else if (target.GetRealKiller() != null && target.GetRealKiller() != target && killer != null)
                        { Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.PissedOff; ExtendedPlayerControl.RpcMurderPlayerV3(target.GetRealKiller(), target);  }
                return false;
            case CustomRoles.NiceMini:
            case CustomRoles.EvilMini:
                if (Mini.Age < 18)
                {
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mini), GetString("Cantkillkid")));
                    return false;
                }
                break;
            //case CustomRoles.Luckey:
            //    var rd = IRandom.Instance;
            //    if (rd.Next(0, 100) < Options.LuckeyProbability.GetInt())
            //    {
            //        killer.RpcGuardAndKill(target);
            //        return false;
            //    }
            //    break;
            case CustomRoles.Jinx:
                if (Main.JinxSpellCount[target.PlayerId] <= 0) break;
                if (killer.Is(CustomRoles.Pestilence)) break;
                if (killer == target) break;
                killer.RpcGuardAndKill(target);
                target.RpcGuardAndKill(target);
                Main.JinxSpellCount[target.PlayerId] -= 1;
                RPC.SendRPCJinxSpellCount(target.PlayerId);
                if (Jinx.killAttacker.GetBool())
                {
                    killer.SetRealKiller(target);
                    Logger.Info($"{target.GetNameWithRole()} : {Main.JinxSpellCount[target.PlayerId]}回目", "Jinx");
                    Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Jinx;
                    killer.RpcMurderPlayerV3(killer);
                }
                return false;
            //击杀老兵
            case CustomRoles.Veteran:
                if (Main.VeteranInProtect.ContainsKey(target.PlayerId) && killer.PlayerId != target.PlayerId)
                    if (Main.VeteranInProtect[target.PlayerId] + Options.VeteranSkillDuration.GetInt() >= Utils.GetTimeStamp())
                    {
                        if (!killer.Is(CustomRoles.Pestilence))
                        {
                            killer.SetRealKiller(target);
                            target.RpcMurderPlayerV3(killer);
                            Logger.Info($"{target.GetRealName()} 老兵反弹击杀：{killer.GetRealName()}", "Veteran Kill");
                            return false;
                        }
                        if (killer.Is(CustomRoles.Pestilence))
                        {
                            target.SetRealKiller(killer);
                            killer.RpcMurderPlayerV3(target);
                            Logger.Info($"{target.GetRealName()} 老兵反弹击杀：{target.GetRealName()}", "Pestilence Reflect");
                            return false;
                        }
                    }
                break;
            case CustomRoles.TimeMaster:
                if (Main.TimeMasterInProtect.ContainsKey(target.PlayerId) && killer.PlayerId != target.PlayerId)
                    if (Main.TimeMasterInProtect[target.PlayerId] + Options.TimeMasterSkillDuration.GetInt() >= Utils.GetTimeStamp(DateTime.UtcNow))
                    {
                        foreach (var player in Main.AllPlayerControls)
                        {
                            if (!killer.Is(CustomRoles.Pestilence) && Main.TimeMasterBackTrack.TryGetValue(player.PlayerId, out var position))
                            {
                                if (player.CanBeTeleported())
                                {
                                    player.RpcTeleport(position);
                                }
                            }
                        }
                        killer.SetKillCooldown(target: target, forceAnime: true);
                        return false;
                    }
                break;
            case CustomRoles.Masochist:

                killer.SetKillCooldown(target: target, forceAnime: true);
                Main.MasochistKillMax[target.PlayerId]++;
                //    killer.RPCPlayCustomSound("DM");
                target.Notify(string.Format(GetString("MasochistKill"), Main.MasochistKillMax[target.PlayerId]));
                if (Main.MasochistKillMax[target.PlayerId] >= Options.MasochistKillMax.GetInt())
                {
                    if (!CustomWinnerHolder.CheckForConvertedWinner(target.PlayerId))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Masochist);
                        CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
                    }
                }
                return false;
            //return true;
            case CustomRoles.SuperStar:
                if (Main.AllAlivePlayerControls.Any(x =>
                    x.PlayerId != killer.PlayerId &&
                    x.PlayerId != target.PlayerId &&
                    Vector2.Distance(x.transform.position, target.transform.position) < 2f)
                   ) return false;
                break;
            //玩家被击杀事件
            case CustomRoles.Gamer:
                if (!Gamer.CheckMurder(killer, target))
                    return false;
                break;
            //嗜血骑士技能生效中
            case CustomRoles.BloodKnight:
                if (BloodKnight.InProtect(target.PlayerId))
                {
                    killer.RpcGuardAndKill(target);
                    if (!Options.DisableShieldAnimations.GetBool()) target.RpcGuardAndKill();
                    target.Notify(GetString("BKOffsetKill"));
                    return false;
                }
                break;
            case CustomRoles.Wildling:
                if (Wildling.InProtect(target.PlayerId))
                {
                    killer.RpcGuardAndKill(target);
                    if (!Options.DisableShieldAnimations.GetBool()) target.RpcGuardAndKill();
                    target.Notify(GetString("BKOffsetKill"));
                    return false;
                }
                break;
            case CustomRoles.Spiritcaller:
                if (Spiritcaller.InProtect(target))
                {
                    killer.RpcGuardAndKill(target);
                    target.RpcGuardAndKill();
                    return false;
                }
                break;
            //击杀萧暮
            case CustomRoles.Randomizer:
                var Fg = IRandom.Instance;
                int Randomizer = Fg.Next(1, 5);
                if (Randomizer == 1)
                {
                    if (killer.PlayerId != target.PlayerId || (target.GetRealKiller()?.GetCustomRole() is CustomRoles.Swooper or CustomRoles.Wraith) || !killer.Is(CustomRoles.Oblivious) || (killer.Is(CustomRoles.Oblivious) && !Oblivious.ObliviousBaitImmune.GetBool()))
                    {
                        killer.RPCPlayCustomSound("Congrats");
                        target.RPCPlayCustomSound("Congrats");

                        float delay;
                        if (Options.BecomeBaitDelayMax.GetFloat() < Options.BecomeBaitDelayMin.GetFloat())
                        {
                            delay = 0f;
                        }
                        else
                        {
                            delay = IRandom.Instance.Next((int)Options.BecomeBaitDelayMin.GetFloat(), (int)Options.BecomeBaitDelayMax.GetFloat() + 1);
                        }
                        delay = Math.Max(delay, 0.15f);
                        if (delay > 0.15f && Options.BecomeBaitDelayNotify.GetBool())
                        {
                            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Bait), string.Format(GetString("KillBaitNotify"), (int)delay)), delay);
                        }

                        Logger.Info($"{killer.GetNameWithRole()} 击杀了萧暮触发自动报告 => {target.GetNameWithRole()}", "Randomizer");

                        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Randomizer), GetString("YouKillRandomizer1")));

                        _ = new LateTask(() =>
                        {
                            if (GameStates.IsInTask) killer.CmdReportDeadBody(target.Data);
                        }, delay, "Bait Self Report");
                    }
                }
                else if (Randomizer == 2)
                {
                    Logger.Info($"{killer.GetNameWithRole()} 击杀了萧暮触发暂时无法移动 => {target.GetNameWithRole()}", "Randomizer");
                    NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Randomizer), GetString("YouKillRandomizer2")));
                    var tmpSpeed = Main.AllPlayerSpeed[killer.PlayerId];
                    Main.AllPlayerSpeed[killer.PlayerId] = Main.MinSpeed;    //tmpSpeedで後ほど値を戻すので代入しています。
                    ReportDeadBodyPatch.CanReport[killer.PlayerId] = false;
                    killer.MarkDirtySettings();
                    _ = new LateTask(() =>
                    {
                        Main.AllPlayerSpeed[killer.PlayerId] = Main.AllPlayerSpeed[killer.PlayerId] - Main.MinSpeed + tmpSpeed;
                        ReportDeadBodyPatch.CanReport[killer.PlayerId] = true;
                        killer.MarkDirtySettings();
                        RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
                    }, Options.BecomeTrapperBlockMoveTime.GetFloat(), "Trapper BlockMove");
                }
                else if (Randomizer == 3)
                {
                    Logger.Info($"{killer.GetNameWithRole()} 击杀了萧暮触发凶手CD变成600 => {target.GetNameWithRole()}", "Randomizer");
                    NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Randomizer), GetString("YouKillRandomizer3")));
                    Main.AllPlayerKillCooldown[killer.PlayerId] = 600f;
                    killer.SyncSettings();
                }
                else if (Randomizer == 4)
                {
                    Logger.Info($"{killer.GetNameWithRole()} 击杀了萧暮触发随机复仇 => {target.GetNameWithRole()}", "Randomizer");
                    NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Randomizer), GetString("YouKillRandomizer4")));
                    {
                        var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId).ToList();
                        var rp = pcList[IRandom.Instance.Next(0, pcList.Count)];
                        if (!rp.Is(CustomRoles.Pestilence))
                        {
                            Main.PlayerStates[rp.PlayerId].deathReason = PlayerState.DeathReason.Revenge;
                            rp.SetRealKiller(target);
                            rp.RpcMurderPlayerV3(rp);
                        }
                    }
                }
                break;
        }

        //保镖保护
        if (killer.PlayerId != target.PlayerId)
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId).ToArray())
            {
                var pos = target.transform.position;
                var dis = Vector2.Distance(pos, pc.transform.position);
                if (dis > Options.BodyguardProtectRadius.GetFloat()) continue;
                if (pc.Is(CustomRoles.Bodyguard))
                {
                    if (pc.Is(CustomRoles.Madmate) && killer.GetCustomRole().IsImpostorTeam())
                        Logger.Info($"{pc.GetRealName()} 是个叛徒，所以他选择无视杀人现场", "Bodyguard");
                    else
                    {
                        Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Sacrifice;
                        pc.RpcMurderPlayerV3(killer);
                        pc.SetRealKiller(killer);
                        pc.RpcMurderPlayerV3(pc);
                        Logger.Info($"{pc.GetRealName()} 挺身而出与歹徒 {killer.GetRealName()} 同归于尽", "Bodyguard");
                        return false;
                    }
                }
                if (target.Is(CustomRoles.Cyber))
                {
                    if (Main.AllAlivePlayerControls.Any(x =>
                        x.PlayerId != killer.PlayerId &&
                        x.PlayerId != target.PlayerId &&
                        Vector2.Distance(x.transform.position, target.transform.position) < 2f))
                        return false;
                }
            }
        }

        if (Main.ForCrusade.Contains(target.PlayerId))
        {
            foreach (var player in Main.AllAlivePlayerControls)
            {
                if (player.Is(CustomRoles.Crusader))
                {
                    if (!killer.Is(CustomRoles.Pestilence) && !killer.Is(CustomRoles.KillingMachine))
                    {
                        player.RpcMurderPlayerV3(killer);
                        Main.ForCrusade.Remove(target.PlayerId);
                        killer.RpcGuardAndKill(target);
                        return false;
                    }

                    if (killer.Is(CustomRoles.Pestilence))
                    {
                        Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.PissedOff;
                        killer.RpcMurderPlayerV3(player);
                        Main.ForCrusade.Remove(target.PlayerId);
                        target.RpcGuardAndKill(killer);

                        return false;
                    }
                }
            }
        }

        if (PlagueBearer.IsEnable && PlagueBearer.OnCheckMurderPestilence(killer, target))
            return false;

        if (!check) killer.RpcMurderPlayerV3(target);
        if (killer.Is(CustomRoles.Doppelganger)) Doppelganger.OnCheckMurder(killer, target);
        return true;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Exiled))]
class ExilePlayerPatch
{    
    public static void Postfix(PlayerControl __instance)
    {
        try
        {
            GhostRoleAssign.GhostAssignPatch(__instance);
        }
        catch (Exception error)
        {
            Logger.Error($"Error after Ghost assign: {error}", "ExilePlayerPatch.GhostAssign");
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
class MurderPlayerPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] MurderResultFlags resultFlags, ref bool __state)
    {
        Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()} => {target.GetNameWithRole().RemoveHtmlTags()}{(target.IsProtected() ? "(Protected)" : "")}, flags : {resultFlags}", "MurderPlayer Prefix");

        if (RandomSpawn.CustomNetworkTransformPatch.NumOfTP.TryGetValue(__instance.PlayerId, out var num) && num > 2)
        {
            RandomSpawn.CustomNetworkTransformPatch.NumOfTP[__instance.PlayerId] = 3;
        }

        var isProtectedByClient = resultFlags.HasFlag(MurderResultFlags.DecisionByHost) && target.IsProtected();
        var isProtectedByHost = resultFlags.HasFlag(MurderResultFlags.FailedProtected);
        var isFailed = resultFlags.HasFlag(MurderResultFlags.FailedError);
        var isSucceeded = __state = !isProtectedByClient && !isProtectedByHost && !isFailed;
        
        if (isProtectedByClient)
        {
            Logger.Info("The kill will fail because it has DecisonByHost and target is protected", "MurderPlayer Prefix");
        }
        if (isProtectedByHost)
        {
            if (GameStates.IsModHost)
                Logger.Info("Host sent FailedProtected due to role skill / reset kill timer", "MurderPlayer Prefix");
            else
                Logger.Info("Vanilla server canceled murder due to protection", "MurderPlayer Prefix");
        }
        if (isFailed)
        {
            if (GameStates.IsModHost)
                Logger.Info("The kill was cancelled by the host", "MurderPlayer Prefix");
            else
                Logger.Info("The kill was cancelled by the server", "MurderPlayer Prefix");
        }

        if (isSucceeded && AmongUsClient.Instance.AmHost)
        {
            if (target.shapeshifting)
            {
                // During shapeshift animation
                // Delay 1s to account for animation time, plus +0.5s to account for lag with the client
                _ = new LateTask(
                    () =>
                    {
                        if (GameStates.IsInTask)
                        {
                            target.RpcShapeshift(target, false);
                        }
                    },
                    1.5f, "Revert Shapeshift Before Murder");
            }
            else
            {
                if (Main.CheckShapeshift.TryGetValue(target.PlayerId, out var shapeshifting) && shapeshifting)
                {
                    //Shapeshift revert
                    target.RpcShapeshift(target, false);
                }
            }

            if (!target.IsProtected() && !Doppelganger.DoppelVictim.ContainsKey(target.PlayerId) && !Camouflage.ResetSkinAfterDeathPlayers.Contains(target.PlayerId))
            {
                Camouflage.ResetSkinAfterDeathPlayers.Add(target.PlayerId);
                Camouflage.RpcSetSkin(target, ForceRevert: true, RevertToDefault: true);
            }
        }
        return true;
    }
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target/*, [HarmonyArgument(1)] MurderResultFlags resultFlags*/, bool __state)
    {
        if (!__state)
        {
            return;
        }
        if (GameStates.IsHideNSeek) return;
        if (target.AmOwner) RemoveDisableDevicesPatch.UpdateDisableDevices();
        if (!target.Data.IsDead || !AmongUsClient.Instance.AmHost) return;

        if (Main.OverDeadPlayerList.Contains(target.PlayerId)) return;

        PlayerControl killer = __instance;
        bool needUpadteNotifyRoles = true;


        if (PlagueDoctor.IsEnable)
        {
            PlagueDoctor.OnPDdeath(killer, target);
            PlagueDoctor.OnAnyMurder();
        }

        if (Quizmaster.IsEnable)
            Quizmaster.OnPlayerDead(target);

        if (Pelican.IsEnable && target.Is(CustomRoles.Pelican))
            Pelican.OnPelicanDied(target.PlayerId);

        if (Main.GodfatherTarget.Contains(target.PlayerId) && !(killer.GetCustomRole().IsImpostor() || killer.GetCustomRole().IsMadmate() || killer.Is(CustomRoles.Madmate)))
        {
            if (Options.GodfatherChangeOpt.GetValue() == 0) killer.RpcSetCustomRole(CustomRoles.Refugee);
            else killer.RpcSetCustomRole(CustomRoles.Madmate);
        }

        if (Sniper.IsEnable)
        {
            if (Sniper.TryGetSniper(target.PlayerId, ref killer))
            {
                Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Sniped;
            }
        }
        if (killer != __instance)
        {
            Logger.Info($"Real Killer => {killer.GetNameWithRole().RemoveHtmlTags()}", "MurderPlayer");

        }
        if (Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.etc)
        {
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Kill;
        }

        //看看UP是不是被首刀了
        if (Main.FirstDied == "" && target.Is(CustomRoles.Youtuber) && !killer.Is(CustomRoles.KillingMachine))
        {
            CustomSoundsManager.RPCPlayCustomSoundAll("Congrats");
            if (!CustomWinnerHolder.CheckForConvertedWinner(target.PlayerId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Youtuber);
                CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
            }
            //Imagine youtuber is converted
        }

        if (Main.FirstDied == "")
            Main.FirstDied = target.GetClient().GetHashedPuid();


        CustomRoleManager.CheckDeadBody(target);

        if (Main.PlayerStates.TryGetValue(target.PlayerId, out var targetState))
            targetState.Role?.OnTargetDead(killer, target);

        if (Main.PlayerStates.TryGetValue(killer.PlayerId, out var killerState))
            killerState.Role?.OnMurder(killer, target);


        if (target.Is(CustomRoles.Bait))
        {
            Bait.BaitAfterDeathTasks(killer, target);
        }

        if (target.Is(CustomRoles.Burst) && killer.IsAlive() && !killer.Is(CustomRoles.KillingMachine))
        {
            Burst.AfterBurstDeadTasks(killer, target);
        }
        
        if (target.Is(CustomRoles.Trapper) && killer != target && !killer.Is(CustomRoles.KillingMachine))
            killer.TrapperKilled(target);

        if (Main.AllKillers.ContainsKey(killer.PlayerId))
            Main.AllKillers.Remove(killer.PlayerId);

        if (!killer.Is(CustomRoles.Trickster))
            Main.AllKillers.Add(killer.PlayerId, Utils.GetTimeStamp());

        switch (target.GetCustomRole())
        {
            case CustomRoles.Lightning:
                if (killer != target)
                    Lightning.MurderPlayer(killer, target);
                break;
        }
        switch (killer.GetCustomRole())
        {
            case CustomRoles.SwordsMan:
                if (killer != target)
                    SwordsMan.OnMurder(killer);
                break;
            case CustomRoles.BloodKnight:
                BloodKnight.OnMurderPlayer(killer, target);
                break;
            case CustomRoles.Wildling:
                Wildling.OnMurderPlayer(killer, target);
                break;
            case CustomRoles.Butcher:
                Butcher.OnMurderPlayer(killer, target);
                break;
        }

        if (killer.Is(CustomRoles.TicketsStealer) && killer.PlayerId != target.PlayerId)
            killer.Notify(string.Format(GetString("TicketsStealerGetTicket"), ((Main.AllPlayerControls.Count(x => x.GetRealKiller()?.PlayerId == killer.PlayerId) + 1) * Stealer.TicketsPerKill.GetFloat()).ToString("0.0#####")));

        if (killer.Is(CustomRoles.Pickpocket) && killer.PlayerId != target.PlayerId)
            killer.Notify(string.Format(GetString("PickpocketGetVote"), ((Main.AllPlayerControls.Count(x => x.GetRealKiller()?.PlayerId == killer.PlayerId) + 1) * Pickpocket.VotesPerKill.GetFloat()).ToString("0.0#####")));

        if (target.Is(CustomRoles.Avanger))
        {
            Avanger.OnMurderPlayer(target);
        }

        if (target.Is(CustomRoles.Oiiai))
        {
            Oiiai.OnMurderPlayer(killer, target);
        }

        foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Mediumshiper)).ToArray())
            pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mediumshiper), GetString("MediumshiperKnowPlayerDead")));

        if (Executioner.Target.ContainsValue(target.PlayerId))
            Executioner.ChangeRoleByTarget(target);

        if (target.Is(CustomRoles.Executioner) && Executioner.Target.ContainsKey(target.PlayerId))
        {
            Executioner.Target.Remove(target.PlayerId);
            Executioner.SendRPC(target.PlayerId);
        }

        if (Lawyer.Target.ContainsValue(target.PlayerId))
            Lawyer.ChangeRoleByTarget(target);

        if (Mortician.IsEnable) Mortician.OnPlayerDead(target);
        if (Tracefinder.IsEnable) Tracefinder.OnPlayerDead(target);
        if (Vulture.IsEnable) Vulture.OnPlayerDead(target);
        if (SoulCollector.IsEnable) SoulCollector.OnPlayerDead(target);
        if (Medic.IsEnable) Medic.IsDead(target);

        //================GHOST ASSIGN PATCH============
        if (target.Is(CustomRoles.EvilSpirit))
        {
            target.RpcSetRole(RoleTypes.GuardianAngel);
        }
        else
        {
            GhostRoleAssign.GhostAssignPatch(target);
        }

        Utils.AfterPlayerDeathTasks(target);

        Main.PlayerStates[target.PlayerId].SetDead();
        target.SetRealKiller(killer, true);
        Utils.CountAlivePlayers(true);

        Utils.TargetDies(__instance, target);

        if (Options.LowLoadMode.GetBool())
        {
            __instance.MarkDirtySettings();
            target.MarkDirtySettings();

            if (needUpadteNotifyRoles)
            {
                Utils.NotifyRoles(SpecifySeer: killer);
                Utils.NotifyRoles(SpecifySeer: target);
            }
        }
        else
        {
            Utils.SyncAllSettings();

            if (needUpadteNotifyRoles)
            {
                Utils.NotifyRoles(ForceLoop: true);
            }
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcMurderPlayer))]
class RpcMurderPlayerPatch
{
    public static bool Prefix(PlayerControl __instance, PlayerControl target, bool didSucceed)
    {
        if (!AmongUsClient.Instance.AmHost)
            Logger.Error("Client is calling RpcMurderPlayer, are you Hacking?", "RpcMurderPlayerPatch..Prefix");

        MurderResultFlags murderResultFlags = didSucceed ? MurderResultFlags.Succeeded : MurderResultFlags.FailedError;
        if (AmongUsClient.Instance.AmClient)
        {
            __instance.MurderPlayer(target, murderResultFlags);
        }
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable, -1);
        messageWriter.WriteNetObject(target);
        messageWriter.Write((int)murderResultFlags);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);

        return false;
        // There is no need to include DecisionByHost. DecisionByHost will make client check protection locally and cause confusion.
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckShapeshift))]
public static class CheckShapeShiftPatch
{
    public static void RejectShapeshiftAndReset(this PlayerControl player, bool reset = true)
    {
        player.RpcRejectShapeshift();
        if (reset) player.RpcResetAbilityCooldown();
        Logger.Info($"Rejected {player.GetRealName()} shapeshift & " + (reset ? "Reset cooldown" : "Not Reset cooldown"), "RejectShapeshiftAndReset");
    }
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] bool shouldAnimate)
    {
        if (!AmongUsClient.Instance.AmHost || !GameStates.IsModHost) return true;
        if (__instance.PlayerId == target.PlayerId) return true;
        if (!Options.DisableShapeshiftAnimations.GetBool()) return true;

        var shapeshifter = __instance;
        var role = shapeshifter.GetCustomRole();

        // Always show
        if (role is CustomRoles.ShapeshifterTOHE or CustomRoles.Shapeshifter or CustomRoles.ShapeMaster) return true;

        // Check Sniper settings conditions
        if (role is CustomRoles.Sniper && Sniper.ShowShapeshiftAnimations) return true;

        Logger.Info($"{shapeshifter.GetRealName()} => {target.GetRealName()}, shouldAnimate = {shouldAnimate}", "Check ShapeShift");

        if (role.GetVNRole() != CustomRoles.Shapeshifter)
        {
            shapeshifter.RejectShapeshiftAndReset();
            Logger.Info($"Rejected bcz {shapeshifter.GetRealName()} is not shapeshifter in mod roles", "Check ShapeShift");
            return false;
        }

        if (Pelican.IsEaten(shapeshifter.PlayerId))
        {
            shapeshifter.RejectShapeshiftAndReset();
            Logger.Info($"Rejected bcz {shapeshifter.GetRealName()} is eaten by Pelican", "Check ShapeShift");
            return false;
        }

        if (!shapeshifter.IsAlive())
        {
            shapeshifter.RejectShapeshiftAndReset();
            Logger.Info($"Rejected bcz {shapeshifter.GetRealName()} is dead", "Check ShapeShift");
            return false;
        }

        bool shapeshifting = shapeshifter.PlayerId != target.PlayerId;

        var shapeshiftIsHidden = true;
        if (Main.PlayerStates.TryGetValue(shapeshifter.PlayerId, out var playerState))
        {
            shapeshifter.RejectShapeshiftAndReset();
            playerState.Role?.OnShapeshift(shapeshifter, target, false, shapeshiftIsHidden);
            return false;
        }

        switch (role)
        {

            case CustomRoles.BountyHunter:
                Logger.Info("Rejected bcz the ss button is used to display skill timer", "Check ShapeShift");
                shapeshifter.RejectShapeshiftAndReset(false);
                return false;

            case CustomRoles.Penguin:
                Logger.Info("Rejected bcz the ss button is used to display skill timer", "Check ShapeShift");
                shapeshifter.RejectShapeshiftAndReset(false);
                return false;

            case CustomRoles.SerialKiller:
                Logger.Info("Rejected bcz the ss button is used to display skill timer", "Check ShapeShift");
                shapeshifter.RejectShapeshiftAndReset(false);
                return false;

            case CustomRoles.Sniper:
                Sniper.OnShapeshift(shapeshifter, false, shapeshiftIsHidden: shapeshiftIsHidden);
                shapeshifter.RejectShapeshiftAndReset();
                return false;

            case CustomRoles.Warlock:
                shapeshifter.RejectShapeshiftAndReset();
                if (Main.CursedPlayers[shapeshifter.PlayerId] != null)
                {
                    if (Main.CursedPlayers[shapeshifter.PlayerId].IsAlive())
                    {
                        var cp = Main.CursedPlayers[shapeshifter.PlayerId];
                        Vector2 cppos = cp.transform.position;
                        Dictionary<PlayerControl, float> cpdistance = [];
                        float dis;
                        foreach (PlayerControl p in Main.AllAlivePlayerControls)
                        {
                            if (p.PlayerId == cp.PlayerId) continue;
                            if (!Options.WarlockCanKillSelf.GetBool() && p.PlayerId == shapeshifter.PlayerId) continue;
                            if (!Options.WarlockCanKillAllies.GetBool() && p.GetCustomRole().IsImpostor()) continue;
                            if (p.Is(CustomRoles.Glitch)) continue;
                            if (p.Is(CustomRoles.Pestilence)) continue;
                            if (Pelican.IsEaten(p.PlayerId) || Medic.ProtectList.Contains(p.PlayerId)) continue;
                            dis = Vector2.Distance(cppos, p.transform.position);
                            cpdistance.Add(p, dis);
                            Logger.Info($"{p?.Data?.PlayerName}の位置{dis}", "Warlock");
                        }
                        if (cpdistance.Count >= 1)
                        {
                            var min = cpdistance.OrderBy(c => c.Value).FirstOrDefault(); // Retrieve the smallest value
                            PlayerControl targetw = min.Key;
                            if (cp.RpcCheckAndMurder(targetw, true))
                            {
                                targetw.SetRealKiller(shapeshifter);
                                Logger.Info($"{targetw.GetNameWithRole()} was killed", "Warlock");
                                cp.RpcMurderPlayerV3(targetw);//殺す
                                shapeshifter.SetKillCooldown(forceAnime: true);
                                shapeshifter.Notify(GetString("WarlockControlKill"));
                            }
                        }
                        else
                        {
                            shapeshifter.Notify(GetString("WarlockNoTarget"));
                        }
                        Main.isCurseAndKill[shapeshifter.PlayerId] = false;
                    }
                    else
                    {
                        shapeshifter.Notify(GetString("WarlockTargetDead"));
                    }
                    Main.CursedPlayers[shapeshifter.PlayerId] = null;
                }
                else
                {
                    shapeshifter.Notify(GetString("WarlockNoTargetYet"));
                }
                return false;

            case CustomRoles.Undertaker:
                Undertaker.OnShapeshift(shapeshifter, shapeshifting);
                shapeshifter.RejectShapeshiftAndReset();
                shapeshifter.Notify(GetString("RejectShapeshift.AbilityWasUsed"), time: 2f);
                return false;

            case CustomRoles.Fireworker:
                Fireworker.ShapeShiftState(shapeshifter, shapeshifting, shapeshiftIsHidden: shapeshiftIsHidden);
                shapeshifter.RejectShapeshiftAndReset();
                return false;

            case CustomRoles.EvilTracker:
                EvilTracker.OnShapeshift(shapeshifter, target, shapeshifting, shapeshiftIsHidden: shapeshiftIsHidden);
                shapeshifter.RejectShapeshiftAndReset();
                shapeshifter.Notify(GetString("RejectShapeshift.AbilityWasUsed"), time: 2f);
                return false;

            case CustomRoles.Miner:
                shapeshifter.RejectShapeshiftAndReset();
                if (Main.LastEnteredVent.ContainsKey(shapeshifter.PlayerId))
                {
                    var lastVentPosition = Main.LastEnteredVentLocation[shapeshifter.PlayerId];
                    Logger.Info($"Miner - {shapeshifter.GetNameWithRole()}:{lastVentPosition}", "MinerTeleport");
                    shapeshifter.RpcTeleport(lastVentPosition);
                    shapeshifter.RPCPlayCustomSound("Teleport");
                }
                return false;

            case CustomRoles.Ninja:
                Ninja.OnShapeshift(shapeshifter, shapeshifting, shapeshiftIsHidden: shapeshiftIsHidden);
                return false;

            case CustomRoles.Escapist:
                if (Main.EscapistLocation.TryGetValue(shapeshifter.PlayerId, out var position))
                {
                    Main.EscapistLocation.Remove(shapeshifter.PlayerId);
                    Logger.Info($"{shapeshifter.GetNameWithRole()}:{position}", "Escapist Teleport");
                    shapeshifter.RpcTeleport(position);
                    shapeshifter.RPCPlayCustomSound("Teleport");
                }
                else
                {
                    Main.EscapistLocation.Add(shapeshifter.PlayerId, shapeshifter.GetCustomPosition());
                    shapeshifter.Notify(GetString("EscapisMtarkedPosition"));
                }
                shapeshifter.RejectShapeshiftAndReset();
                return false;

            case CustomRoles.QuickShooter:
                QuickShooter.OnShapeshift(shapeshifter, shapeshifting);
                shapeshifter.RejectShapeshiftAndReset();
                return false;

            case CustomRoles.RiftMaker:
                RiftMaker.OnShapeshift(shapeshifter, shapeshifting);
                shapeshifter.RejectShapeshiftAndReset();
                return false;

            case CustomRoles.Twister:
                shapeshifter.RejectShapeshiftAndReset();
                Twister.TwistPlayers(shapeshifter, shapeshiftIsHidden: shapeshiftIsHidden);
                return false;

            case CustomRoles.Pitfall:
                Pitfall.OnShapeshift(shapeshifter);
                shapeshifter.RejectShapeshiftAndReset();
                shapeshifter.Notify(GetString("RejectShapeshift.AbilityWasUsed"), time: 2f);
                return false;

            case CustomRoles.Disperser:
                shapeshifter.RejectShapeshiftAndReset();
                Disperser.DispersePlayers(shapeshifter, shapeshiftIsHidden: shapeshiftIsHidden);
                return false;
        }

        return true;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
class ShapeshiftPatch
{
    public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        var shapeshifter = __instance;

        if (Options.DisableShapeshiftAnimations.GetBool())
        {
            var role = shapeshifter.GetCustomRole();

            Logger.Info($"shapeshifter {__instance?.GetRealName()}:role: {role} => {target?.GetNameWithRole().RemoveHtmlTags()}", "ShapeshiftPatch.DisableShapeshiftAnimations");

            // Check shapeshift
            if (!(
                (role is CustomRoles.ShapeshifterTOHE or CustomRoles.Shapeshifter or CustomRoles.ShapeMaster)
                ||
                (role is CustomRoles.Sniper && Sniper.ShowShapeshiftAnimations)
                ))
                return;
        }

        Logger.Info($"{__instance?.GetNameWithRole().RemoveHtmlTags()} => {target?.GetNameWithRole().RemoveHtmlTags()}", "ShapeshiftPatch");

        var shapeshifting = shapeshifter.PlayerId != target.PlayerId;

        if (Main.CheckShapeshift.TryGetValue(shapeshifter.PlayerId, out var last) && last == shapeshifting)
        {
            Logger.Info($"{__instance?.GetNameWithRole().RemoveHtmlTags()} : Cancel Shapeshift.Prefix", "Shapeshift");
            return;
        }

        Main.CheckShapeshift[shapeshifter.PlayerId] = shapeshifting;
        Main.ShapeshiftTarget[shapeshifter.PlayerId] = target.PlayerId;

        if (!AmongUsClient.Instance.AmHost) return;
        if (GameStates.IsHideNSeek) return;
        if (!shapeshifting) Camouflage.RpcSetSkin(__instance);

        if (!Pelican.IsEaten(shapeshifter.PlayerId))
        {
            var shapeshiftIsHidden = false;
            Main.PlayerStates[shapeshifter.PlayerId].Role?.OnShapeshift(shapeshifter, target, shapeshifting, shapeshiftIsHidden);

            switch (shapeshifter.GetCustomRole())
            {
                case CustomRoles.EvilTracker:
                    EvilTracker.OnShapeshift(shapeshifter, target, shapeshifting);
                    break;
                case CustomRoles.Sniper:
                    Sniper.OnShapeshift(shapeshifter, shapeshifting);
                    break;
                case CustomRoles.Undertaker:
                    Undertaker.OnShapeshift(shapeshifter, shapeshifting);
                    break;
                case CustomRoles.RiftMaker:
                    RiftMaker.OnShapeshift(shapeshifter, shapeshifting);
                    break;
                case CustomRoles.Fireworker:
                    Fireworker.ShapeShiftState(shapeshifter, shapeshifting);
                    break;
                case CustomRoles.Warlock:
                    if (Main.CursedPlayers[shapeshifter.PlayerId] != null)
                    {
                        if (shapeshifting && !Main.CursedPlayers[shapeshifter.PlayerId].Data.IsDead)
                        {
                            var cp = Main.CursedPlayers[shapeshifter.PlayerId];
                            Vector2 cppos = cp.transform.position;
                            Dictionary<PlayerControl, float> cpdistance = [];
                            float dis;
                            foreach (PlayerControl p in Main.AllAlivePlayerControls)
                            {
                                if (p.PlayerId == cp.PlayerId) continue;
                                if (!Options.WarlockCanKillSelf.GetBool() && p.PlayerId == shapeshifter.PlayerId) continue;
                                if (!Options.WarlockCanKillAllies.GetBool() && p.GetCustomRole().IsImpostor()) continue;
                                if (p.Is(CustomRoles.Glitch)) continue;
                                if (p.Is(CustomRoles.Pestilence)) continue;
                                if (Pelican.IsEaten(p.PlayerId) || Medic.ProtectList.Contains(p.PlayerId)) continue;
                                dis = Vector2.Distance(cppos, p.transform.position);
                                cpdistance.Add(p, dis);
                                Logger.Info($"{p?.Data?.PlayerName}の位置{dis}", "Warlock");
                            }
                            if (cpdistance.Count >= 1)
                            {
                                var min = cpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番小さい値を取り出す
                                PlayerControl targetw = min.Key;
                                if (cp.RpcCheckAndMurder(targetw, true))
                                {
                                    targetw.SetRealKiller(shapeshifter);
                                    Logger.Info($"{targetw.GetNameWithRole()}was killed", "Warlock");
                                    cp.RpcMurderPlayerV3(targetw);//殺す
                                    shapeshifter.RpcGuardAndKill(shapeshifter);
                                    shapeshifter.Notify(GetString("WarlockControlKill"));
                                }
                            }
                            else
                            {
                                shapeshifter.Notify(GetString("WarlockNoTarget"));
                            }
                            Main.isCurseAndKill[shapeshifter.PlayerId] = false;
                        }
                        Main.CursedPlayers[shapeshifter.PlayerId] = null;
                    }
                    break;
                case CustomRoles.Escapist:
                    if (shapeshifting)
                    {
                        if (Main.EscapistLocation.TryGetValue(shapeshifter.PlayerId, out var position))
                        {
                            Main.EscapistLocation.Remove(shapeshifter.PlayerId);
                            Logger.Msg($"{shapeshifter.GetNameWithRole()}:{position}", "EscapistTeleport");
                            shapeshifter.RpcTeleport(position);
                            shapeshifter.RPCPlayCustomSound("Teleport");
                        }
                        else
                        {
                            Main.EscapistLocation.Add(shapeshifter.PlayerId, shapeshifter.GetCustomPosition());
                            shapeshifter.Notify(GetString("EscapisMtarkedPosition"));
                        }
                    }
                    break;
                case CustomRoles.Miner:
                    if (shapeshifting && Main.LastEnteredVent.ContainsKey(shapeshifter.PlayerId))
                    {
                        var lastVentPosition = Main.LastEnteredVentLocation[shapeshifter.PlayerId];
                        Logger.Info($"Miner - {shapeshifter.GetNameWithRole()}:{lastVentPosition}", "MinerTeleport");
                        shapeshifter.RpcTeleport(lastVentPosition);
                        shapeshifter.RPCPlayCustomSound("Teleport");
                    }
                    break;
                case CustomRoles.Ninja:
                    Ninja.OnShapeshift(shapeshifter, shapeshifting);
                    break;
                case CustomRoles.QuickShooter:
                    QuickShooter.OnShapeshift(shapeshifter, shapeshifting);
                    break;
                case CustomRoles.Disperser:
                    if (shapeshifting)
                        Disperser.DispersePlayers(shapeshifter);
                    break;
                case CustomRoles.Twister:
                    Twister.TwistPlayers(shapeshifter);
                    break;
                case CustomRoles.Pitfall:
                    if (shapeshifting)
                        Pitfall.OnShapeshift(shapeshifter);
                    break;
            }
        }

        //変身解除のタイミングがずれて名前が直せなかった時のために強制書き換え
        if (!shapeshifting && !shapeshifter.Is(CustomRoles.Glitch))
        {
            _ = new LateTask(() =>
            {
                Utils.NotifyRoles(NoCache: true);
            },
            1.2f, "ShapeShiftNotify");
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
class ReportDeadBodyPatch
{
    public static Dictionary<byte, bool> CanReport;
    public static HashSet<byte> UnreportablePlayers = [];
    public static Dictionary<byte, List<GameData.PlayerInfo>> WaitReport = [];
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo target)
    {
        if (GameStates.IsMeeting || GameStates.IsHideNSeek) return false;

        if (EAC.RpcReportDeadBodyCheck(__instance, target))
        {
            Logger.Fatal("Eac patched the report body rpc", "ReportDeadBodyPatch");
            return false;
        }
        if (Options.DisableMeeting.GetBool()) return false;
        if (Options.CurrentGameMode == CustomGameMode.FFA) return false;

        if (!CanReport[__instance.PlayerId])
        {
            WaitReport[__instance.PlayerId].Add(target);
            Logger.Warn($"{__instance.GetNameWithRole().RemoveHtmlTags()} : Reporting is prohibited and will wait until it becomes possible", "ReportDeadBody");
            return false;
        }

        Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()} => {target?.Object?.GetNameWithRole().RemoveHtmlTags() ?? "null"}", "ReportDeadBody");

        foreach (var kvp in Main.PlayerStates)
        {
            var pc = Utils.GetPlayerById(kvp.Key);
            kvp.Value.LastRoom = pc.GetPlainShipRoom();
        }

        if (!AmongUsClient.Instance.AmHost) return true;

        try
        {
            //通報者が死んでいる場合、本処理で会議がキャンセルされるのでここで止める
            if (__instance.Data.IsDead) return false;

            //=============================================
            //以下、检查是否允许本次会议
            //=============================================

            var killer = target?.Object?.GetRealKiller();
            var killerRole = killer?.GetCustomRole();

            //杀戮机器无法报告或拍灯
            //     if (__instance.Is(CustomRoles.KillingMachine)) return false;

            // if Bait is killed, check the setting condition
            if (!(target != null && target.Object.Is(CustomRoles.Bait) && Bait.BaitCanBeReportedUnderAllConditions.GetBool()))
            {
                // Camouflager
                if (Camouflager.CantPressOnReportButton()) return false;

                // Comms Camouflage
                if (Options.DisableReportWhenCC.GetBool() && Utils.IsActive(SystemTypes.Comms) && Camouflage.IsActive) return false;
            }

            if (Deathpact.CanCallMeeting(__instance)) return false;

            if (target == null) //拍灯事件
            {
                if (__instance.Is(CustomRoles.Jester) && !Options.JesterCanUseButton.GetBool()) return false;
                if (__instance.Is(CustomRoles.Swapper) && !Swapper.CanStartMeeting.GetBool()) return false;
            }
            if (target != null) //拍灯事件
            {
                if (UnreportablePlayers.Contains(target.PlayerId)) return false;

                if (Coroner.UnreportablePlayers.Contains(target.PlayerId)) return false;

                if (!Main.PlayerStates.TryGetValue(__instance.PlayerId, out var playerState))
                    if (playerState != null && playerState.Role != null && playerState.Role.CheckReportDeadBody(__instance, target, killer))
                        return false;

                //Add all the patch bodies here!!!
                //Vulture ate body can not be reported
                if (Vulture.UnreportablePlayers.Contains(target.PlayerId)) return false;
                // 被赌杀的尸体无法被报告 guessed
                if (Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.Gambled) return false;
                // 清道夫的尸体无法被报告 scavenger
                if (killerRole == CustomRoles.Scavenger) return false;

                // Cleaner cleansed body
                if (Cleaner.BodyIsCleansed(target.PlayerId)) return false;

                //Medusa bodies can not be reported
                if (Main.MedusaBodies.Contains(target.PlayerId)) return false;

                

                if (__instance.Is(CustomRoles.Vulture))
                {
                    long now = Utils.GetTimeStamp();
                    if ((Vulture.AbilityLeftInRound[__instance.PlayerId] > 0) && (now - Vulture.LastReport[__instance.PlayerId] > (long)Vulture.VultureReportCD.GetFloat()))
                    {
                        Vulture.LastReport[__instance.PlayerId] = now;

                        Vulture.OnReportDeadBody(__instance, target);
                        __instance.RpcGuardAndKill(__instance);
                        __instance.Notify(GetString("VultureReportBody"));
                        if (Vulture.AbilityLeftInRound[__instance.PlayerId] > 0)
                        {
                            _ = new LateTask(() =>
                            {
                                if (GameStates.IsInTask)
                                {
                                    if (!Options.DisableShieldAnimations.GetBool()) __instance.RpcGuardAndKill(__instance);
                                    __instance.Notify(GetString("VultureCooldownUp"));
                                }
                                return;
                            }, Vulture.VultureReportCD.GetFloat(), "Vulture CD");
                        }

                        Logger.Info($"{__instance.GetRealName()} ate {target.PlayerName} corpse", "Vulture");
                        return false;
                    }
                }

                if (__instance.Is(CustomRoles.Medusa))
                {
                    Main.MedusaBodies.Remove(target.PlayerId);
                    Main.MedusaBodies.Add(target.PlayerId);
                    __instance.Notify(GetString("MedusaStoneBody"));
                    //      __instance.ResetKillCooldown();
                    __instance.SetKillCooldownV3(Medusa.KillCooldownAfterStoneGazing.GetFloat(), forceAnime: true);
                    Logger.Info($"{__instance.GetRealName()} stoned {target.PlayerName} body", "Medusa");
                    return false;
                }

                // 胆小鬼不敢报告
                var tpc = Utils.GetPlayerById(target.PlayerId);
                if (__instance.Is(CustomRoles.Oblivious))
                {
                    if (!tpc.Is(CustomRoles.Bait) || (tpc.Is(CustomRoles.Bait) && Oblivious.ObliviousBaitImmune.GetBool())) /* && (target?.Object != null)*/
                    {
                        return false;
                    }
                }

                var tar = Utils.GetPlayerById(target.PlayerId);
                if (__instance.Is(CustomRoles.Amnesiac))
                {
                    if (tar.GetCustomRole().IsImpostor())
                    {
                        __instance.RpcSetCustomRole(CustomRoles.Refugee);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                    }

                    if (tar.GetCustomRole().IsMadmate() || tar.Is(CustomRoles.Madmate))
                    {
                        __instance.RpcSetCustomRole(CustomRoles.Refugee);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                    }

                    if (tar.GetCustomRole().IsCrewmate() && !tar.Is(CustomRoles.Madmate))
                    {
                        if (tar.IsAmneCrew())
                        {
                            __instance.RpcSetCustomRole(tar.GetCustomRole());
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                            Main.TasklessCrewmate.Add(__instance.PlayerId);
                        }
                        else if (tar.Is(CustomRoles.Sheriff))
                        {
                            Sheriff.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Sheriff);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        else if (tar.Is(CustomRoles.Admirer))
                        {
                            Admirer.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Admirer);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        else if (tar.Is(CustomRoles.Cleanser))
                        {
                            Sheriff.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Cleanser);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                            Main.TasklessCrewmate.Add(__instance.PlayerId);
                        }
                        else if (tar.Is(CustomRoles.CopyCat))
                        {
                            __instance.RpcSetCustomRole(CustomRoles.CopyCat);
                            tar.GetRoleClass()?.Add(tar.PlayerId);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        else if (tar.Is(CustomRoles.Counterfeiter))
                        {
                            Counterfeiter.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Counterfeiter);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        else if (tar.Is(CustomRoles.Crusader))
                        {
                            Crusader.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Crusader);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        else if (tar.Is(CustomRoles.Overseer))
                        {
                            __instance.RpcSetCustomRole(CustomRoles.Overseer);
                            tar.GetRoleClass()?.Add(tar.PlayerId);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        else if (tar.Is(CustomRoles.Jailer))
                        {
                            Jailer.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Jailer);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        else if (tar.Is(CustomRoles.Judge))
                        {
                            Judge.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Judge);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                            Main.TasklessCrewmate.Add(__instance.PlayerId);
                        }
                        else if (tar.Is(CustomRoles.Medic))
                        {
                            Medic.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Medic);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        else if (tar.Is(CustomRoles.Mediumshiper))
                        {
                            Mediumshiper.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Mediumshiper);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                            Main.TasklessCrewmate.Add(__instance.PlayerId);
                        }
                        else if (tar.Is(CustomRoles.Monarch))
                        {
                            __instance.RpcSetCustomRole(CustomRoles.Monarch);
                            tar.GetRoleClass()?.Add(tar.PlayerId);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        else if (tar.Is(CustomRoles.Monitor))
                        {
                            __instance.RpcSetCustomRole(CustomRoles.Monitor);
                            tar.GetRoleClass()?.Add(tar.PlayerId);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                            Main.TasklessCrewmate.Add(__instance.PlayerId);
                        }
                        else if (tar.Is(CustomRoles.Swapper))
                        {
                            __instance.RpcSetCustomRole(CustomRoles.Swapper);
                            tar.GetRoleClass()?.Add(tar.PlayerId);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                            Main.TasklessCrewmate.Add(__instance.PlayerId);
                        }
                        else if (tar.Is(CustomRoles.SabotageMaster))
                        {
                            SabotageMaster.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.SabotageMaster);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                            Main.TasklessCrewmate.Add(__instance.PlayerId);
                        }
                        else if (tar.Is(CustomRoles.SwordsMan))
                        {
                            SwordsMan.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.SwordsMan);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        else
                        {
                            __instance.RpcSetCustomRole(CustomRoles.EngineerTOHE);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                            Main.TasklessCrewmate.Add(__instance.PlayerId);
                        }
                    }

                    if (tar.GetCustomRole().IsAmneNK())
                    {
                        //    Sheriff.Add(__instance.PlayerId);
                        __instance.RpcSetCustomRole(tar.GetCustomRole());
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                    }

                    if (tar.GetCustomRole().IsAmneMaverick())
                    {
                        if (Amnesiac.IncompatibleNeutralMode.GetValue() == 0)
                        {
                            Amnesiac.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Amnesiac);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        if (Amnesiac.IncompatibleNeutralMode.GetValue() == 1)
                        {
                            Pursuer.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Pursuer);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        if (Amnesiac.IncompatibleNeutralMode.GetValue() == 2)
                        {
                            Totocalcio.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Totocalcio);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        if (Amnesiac.IncompatibleNeutralMode.GetValue() == 3)
                        {
                            Maverick.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Maverick);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        if (Amnesiac.IncompatibleNeutralMode.GetValue() == 4)
                        {
                            Imitator.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Imitator);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                    }

                    if (tar.Is(CustomRoles.Jackal))
                    {
                        Sidekick.Add(__instance.PlayerId);
                        __instance.RpcSetCustomRole(CustomRoles.Sidekick);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                    }

                    if (tar.Is(CustomRoles.Juggernaut))
                    {
                        Juggernaut.Add(__instance.PlayerId);
                        __instance.RpcSetCustomRole(CustomRoles.Juggernaut);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                    }

                    if (tar.Is(CustomRoles.BloodKnight))
                    {
                        BloodKnight.Add(__instance.PlayerId);
                        __instance.RpcSetCustomRole(CustomRoles.BloodKnight);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                    }


                    return false;
                }

                if (__instance.Is(CustomRoles.Unlucky) && (target?.Object == null || !target.Object.Is(CustomRoles.Bait)))
                {
                    var Ue = IRandom.Instance;
                    if (Ue.Next(1, 100) <= Unlucky.UnluckyReportSuicideChance.GetInt())
                    {
                        Main.PlayerStates[__instance.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
                        __instance.RpcMurderPlayerV3(__instance);
                        return false;
                    }
                }

                if (playerState != null && playerState.Role != null && !playerState.Role.OnPressReportButton(__instance, target, killer))
                    return false;

                if (target.Object.Is(CustomRoles.Unreportable)) return false;
            }

            if (Options.SyncButtonMode.GetBool() && target == null)
            {
                Logger.Info("最大:" + Options.SyncedButtonCount.GetInt() + ", 現在:" + Options.UsedButtonCount, "ReportDeadBody");
                if (Options.SyncedButtonCount.GetFloat() <= Options.UsedButtonCount)
                {
                    Logger.Info("使用可能ボタン回数が最大数を超えているため、ボタンはキャンセルされました。", "ReportDeadBody");
                    return false;
                }
                else Options.UsedButtonCount++;
                if (Options.SyncedButtonCount.GetFloat() == Options.UsedButtonCount)
                {
                    Logger.Info("使用可能ボタン回数が最大数に達しました。", "ReportDeadBody");
                }
            }

            AfterReportTasks(__instance, target);

        }
        catch (Exception e)
        {
            Logger.Exception(e, "ReportDeadBodyPatch");
            Logger.SendInGame("Error: " + e.ToString());

            // If there is an error in ReportDeadBodyPatch, update the player nicknames anyway
            NameNotifyManager.Notice.Clear();
            Utils.DoNotifyRoles(isForMeeting: true, NoCache: true, CamouflageIsForMeeting: true);
            _ = new LateTask(Utils.SyncAllSettings, 3f, "Sync all settings after report");
        }

        return true;
    }
    public static void AfterReportTasks(PlayerControl player, GameData.PlayerInfo target)
    {
        //=============================================
        // Hereinafter, it is assumed that the button is confirmed to be pressed
        //=============================================

        if (target == null) //Meeting button
        {
            player.GetRoleClass()?.OnPressMeetingButton(player);

            if (Quizmaster.IsEnable)
                Quizmaster.OnButtonPress(player);
        }
        else
        {
            var tpc = Utils.GetPlayerById(target.PlayerId);
            if (tpc != null && !tpc.IsAlive())
            {
                // 侦探报告
                if (player.Is(CustomRoles.Detective) && player.PlayerId != target.PlayerId)
                {
                    string msg;
                    msg = string.Format(GetString("DetectiveNoticeVictim"), tpc.GetRealName(), tpc.GetDisplayRoleAndSubName(tpc, false));
                    if (Options.DetectiveCanknowKiller.GetBool())
                    {
                        var realKiller = tpc.GetRealKiller();
                        if (realKiller == null) msg += "；" + GetString("DetectiveNoticeKillerNotFound");
                        else msg += "；" + string.Format(GetString("DetectiveNoticeKiller"), realKiller.GetDisplayRoleAndSubName(realKiller, false));
                    }
                    Main.DetectiveNotify.Add(player.PlayerId, msg);
                }
                else if (player.Is(CustomRoles.Sleuth) && player.PlayerId != target.PlayerId)
                {
                    string msg;
                    msg = string.Format(GetString("SleuthNoticeVictim"), tpc.GetRealName(), tpc.GetDisplayRoleAndSubName(tpc, false));
                    if (Sleuth.SleuthCanKnowKillerRole.GetBool())
                    {
                        var realKiller = tpc.GetRealKiller();
                        if (realKiller == null) msg += "；" + GetString("SleuthNoticeKillerNotFound");
                        else msg += "；" + string.Format(GetString("SleuthNoticeKiller"), realKiller.GetDisplayRoleAndSubName(realKiller, false));
                    }
                    Sleuth.SleuthNotify.Add(player.PlayerId, msg);
                }
            }

            if (Virus.IsEnable && Main.InfectedBodies.Contains(target.PlayerId))
                Virus.OnKilledBodyReport(player);
        }

        Main.LastVotedPlayerInfo = null;
        Main.ArsonistTimer.Clear();
        Main.GuesserGuessed.Clear();
        Main.VeteranInProtect.Clear();
        Main.GrenadierBlinding.Clear();
        Main.MadGrenadierBlinding.Clear();
        Main.Lighter.Clear();
        Main.AllKillers.Clear();
        Main.GodfatherTarget.Clear();
        Solsticer.patched = false;

        if (Options.BombsClearAfterMeeting.GetBool())
        {
            Main.BombedVents.Clear();
        }

        foreach (var playerStates in Main.PlayerStates.Values.Where(p => p.Role.IsEnable).ToArray())
        {
            playerStates.Role?.OnReportDeadBody(player, target?.Object);
        }

        if (Reverie.IsEnable) Reverie.OnReportDeadBody();
        if (Psychic.IsEnable) Psychic.OnReportDeadBody();
        if (Huntsman.IsEnable) Huntsman.OnReportDeadBody();
        if (Mercenary.IsEnable) Mercenary.OnReportDeadBody();
        if (SoulCollector.IsEnable) SoulCollector.OnReportDeadBody();
        if (Puppeteer.IsEnable) Puppeteer.OnReportDeadBody();
        if (Sniper.IsEnable) Sniper.OnReportDeadBody();
        if (Undertaker.IsEnable) Undertaker.OnReportDeadBody();
        if (Mastermind.IsEnable) Mastermind.OnReportDeadBody();
        if (Vampire.IsEnable) Vampire.OnStartMeeting();
        if (Poisoner.IsEnable) Poisoner.OnStartMeeting();
        if (Vampiress.IsEnable) Vampiress.OnStartMeeting();
        if (Vulture.IsEnable) Vulture.Clear();
        if (Penguin.IsEnable) Penguin.OnReportDeadBody(); 
        if (Pelican.IsEnable) Pelican.OnReportDeadBody();
        if (Bandit.IsEnable) Bandit.OnReportDeadBody();
        if (Cleanser.IsEnable) Cleanser.OnReportDeadBody();
        if (Agitater.IsEnable) Agitater.OnReportDeadBody();
        if (Counterfeiter.IsEnable) Counterfeiter.OnReportDeadBody();
        if (QuickShooter.IsEnable) QuickShooter.OnReportDeadBody();
        if (Eraser.IsEnable) Eraser.OnReportDeadBody();
        if (Divinator.IsEnable) Divinator.OnReportDeadBody();
        if (Retributionist.IsEnable) Retributionist.OnReportDeadBody();
        if (Tracefinder.IsEnable) Tracefinder.OnReportDeadBody();
        if (Judge.IsEnable) Judge.OnReportDeadBody();
        if (Greedy.IsEnable) Greedy.OnReportDeadBody();
        if (Tracker.IsEnable) Tracker.OnReportDeadBody();
        if (Oracle.IsEnable) Oracle.OnReportDeadBody();
        if (Inspector.IsEnable) Inspector.OnReportDeadBody();
        if (PlagueDoctor.IsEnable) PlagueDoctor.OnReportDeadBody();
        if (Doomsayer.IsEnable) Doomsayer.OnReportDeadBody();
        if (Lightning.IsEnable) Lightning.OnReportDeadBody();
        if (Jailer.IsEnable) Jailer.OnReportDeadBody();
        if (Romantic.IsEnable) Romantic.OnReportDeadBody();
        if (Investigator.IsEnable) Investigator.OnReportDeadBody();
        if (Swooper.IsEnable) Swooper.OnReportDeadBody();
        if (Wraith.IsEnable) Wraith.OnReportDeadBody();

        // Alchemist & Bloodlust
        Alchemist.OnReportDeadBody();

        if (Mortician.IsEnable) Mortician.OnReportDeadBody(player, target);
        if (Enigma.IsEnable) Enigma.OnReportDeadBody(player, target);
        if (Mediumshiper.IsEnable) Mediumshiper.OnReportDeadBody(target);
        if (Spiritualist.IsEnable) Spiritualist.OnReportDeadBody(target);
        if (Quizmaster.IsEnable) Quizmaster.OnReportDeadBody(target);

        if (Aware.IsEnable) Aware.OnReportDeadBody();

        foreach (var x in Main.RevolutionistStart.Keys.ToArray())
        {
            var tar = Utils.GetPlayerById(x);
            if (tar == null) continue;
            tar.Data.IsDead = true;
            Main.PlayerStates[tar.PlayerId].deathReason = PlayerState.DeathReason.Sacrifice;
            tar.RpcExileV2();
            Main.PlayerStates[tar.PlayerId].SetDead();
            Logger.Info($"{tar.GetRealName()} 因会议革命失败", "Revolutionist");
        }
        Main.RevolutionistTimer.Clear();
        Main.RevolutionistStart.Clear();
        Main.RevolutionistLastTime.Clear();


        foreach (var pc in Main.AllPlayerControls)
        {
            // Update skins again, since players have different skins
            // And can be easily distinguished from each other
            if (Camouflage.IsCamouflage && Options.KPDCamouflageMode.GetValue() is 2 or 3)
            {
                Camouflage.RpcSetSkin(pc);
            }

            // Check shapeshift and revert skin to default
            if (Main.CheckShapeshift.ContainsKey(pc.PlayerId) && !Doppelganger.DoppelVictim.ContainsKey(pc.PlayerId))
            {
                Camouflage.RpcSetSkin(pc, RevertToDefault: true);
            }
        }

        // Set meeting time
        MeetingTimeManager.OnReportDeadBody();

        // Clear all Notice players
        NameNotifyManager.Notice.Clear();

        // Update Notify Roles for Meeting
        Utils.DoNotifyRoles(isForMeeting: true, NoCache: true, CamouflageIsForMeeting: true);

        // Sync all settings on meeting start
        _ = new LateTask(Utils.SyncAllSettings, 3f, "Sync all settings after report");
    }
    public static async void ChangeLocalNameAndRevert(string name, int time)
    {
        //async Taskじゃ警告出るから仕方ないよね。
        var revertName = PlayerControl.LocalPlayer.name;
        PlayerControl.LocalPlayer.RpcSetNameEx(name);
        await Task.Delay(time);
        PlayerControl.LocalPlayer.RpcSetNameEx(revertName);
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
class FixedUpdateInNormalGamePatch
{
    private static readonly StringBuilder Mark = new(20);
    private static readonly StringBuilder Suffix = new(120);
    private static readonly Dictionary<int, int> BufferTime = [];
    private static int LevelKickBufferTime = 20;

    public static async void Postfix(PlayerControl __instance)
    {
        if (GameStates.IsHideNSeek) return;
        if (!GameStates.IsModHost) return;
        if (__instance == null) return;

        byte id = __instance.PlayerId;
        if (AmongUsClient.Instance.AmHost && GameStates.IsInTask && ReportDeadBodyPatch.CanReport[id] && ReportDeadBodyPatch.WaitReport[id].Count > 0)
        {
            if (Glitch.hackedIdList.ContainsKey(id))
            {
                __instance.Notify(string.Format(GetString("HackedByGlitch"), "Report"));
                Logger.Info("Dead Body Report Blocked (player is hacked by Glitch)", "FixedUpdate.ReportDeadBody");
                ReportDeadBodyPatch.WaitReport[id].Clear();
            }
            else
            {
                var info = ReportDeadBodyPatch.WaitReport[id][0];
                ReportDeadBodyPatch.WaitReport[id].Clear();
                Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()}: The report will be processed now that it is available for reporting", "ReportDeadbody");
                __instance.ReportDeadBody(info);
            }
        }

        try
        {
            await DoPostfix(__instance);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error for {__instance.GetNameWithRole().RemoveHtmlTags()}:  {ex}", "FixedUpdateInNormalGamePatch");
        }
    }

    public static Task DoPostfix(PlayerControl __instance)
    {
        var player = __instance;

        bool lowLoad = false;
        if (Options.LowLoadMode.GetBool())
        {
            if (!BufferTime.TryGetValue(player.PlayerId, out var timerLowLoad))
            {
                BufferTime.TryAdd(player.PlayerId, 10);
                timerLowLoad = 10;
            }

            timerLowLoad--;

            if (timerLowLoad > 0)
            {
                lowLoad = true;
            }
            else
            {
                timerLowLoad = 10;
            }

            BufferTime[player.PlayerId] = timerLowLoad;
        }

        if (!lowLoad)
        {
            Zoom.OnFixedUpdate();
        }

        // Only during the game
        if (GameStates.IsInGame)
        {
            if (Sniper.IsEnable)
                Sniper.OnFixedUpdate(player);

            if (!lowLoad)
            {
                NameNotifyManager.OnFixedUpdate(player);
                TargetArrow.OnFixedUpdate(player);
                LocateArrow.OnFixedUpdate(player);
            }
        }

        if (AmongUsClient.Instance.AmHost)
        {
            if (GameStates.IsLobby)
            {
                bool shouldChangeGamePublic = (ModUpdater.hasUpdate && ModUpdater.forceUpdate) || ModUpdater.isBroken || !Main.AllowPublicRoom || !VersionChecker.IsSupported;
                if (shouldChangeGamePublic && AmongUsClient.Instance.IsGamePublic)
                {
                    AmongUsClient.Instance.ChangeGamePublic(false);
                }

                bool playerInAllowList = false;
                if (Options.ApplyAllowList.GetBool())
                {
                    playerInAllowList = BanManager.CheckAllowList(player.Data.FriendCode);
                }

                if (!playerInAllowList)
                {
                    bool shouldKickLowLevelPlayer = !lowLoad && !player.AmOwner && Options.KickLowLevelPlayer.GetInt() != 0 && player.Data.PlayerLevel != 0 && player.Data.PlayerLevel < Options.KickLowLevelPlayer.GetInt();

                    if (shouldKickLowLevelPlayer)
                    {
                        LevelKickBufferTime--;

                        if (LevelKickBufferTime <= 0)
                        {
                            LevelKickBufferTime = 20;
                            if (!Options.TempBanLowLevelPlayer.GetBool())
                            {
                                AmongUsClient.Instance.KickPlayer(player.GetClientId(), false);
                                string msg = string.Format(GetString("KickBecauseLowLevel"), player.GetRealName().RemoveHtmlTags());
                                Logger.SendInGame(msg);
                                Logger.Info(msg, "Low Level Kick");
                            }
                            else
                            {
                                if (player.GetClient().ProductUserId != "")
                                {
                                    if (!BanManager.TempBanWhiteList.Contains(player.GetClient().GetHashedPuid()))
                                        BanManager.TempBanWhiteList.Add(player.GetClient().GetHashedPuid());
                                }
                                string msg = string.Format(GetString("TempBannedBecauseLowLevel"), player.GetRealName().RemoveHtmlTags());
                                Logger.SendInGame(msg);
                                AmongUsClient.Instance.KickPlayer(player.GetClientId(), true);
                                Logger.Info(msg, "Low Level Temp Ban");
                            }
                        }
                    }
                }

                if (KickPlayerPatch.AttemptedKickPlayerList.Count > 0)
                {
                    foreach (var item in KickPlayerPatch.AttemptedKickPlayerList)
                    {
                        KickPlayerPatch.AttemptedKickPlayerList[item.Key]++;

                        if (item.Value > 11)
                            KickPlayerPatch.AttemptedKickPlayerList.Remove(item.Key);
                    }
                }
            }

            //Mini's count down needs to be done outside if intask if we are counting meeting time
            if (GameStates.IsInGame && player.Is(CustomRoles.NiceMini) || player.Is(CustomRoles.EvilMini))
            {
                if (!player.Data.IsDead)
                    Mini.OnFixedUpdate(player);
            }

            if (GameStates.IsInTask)
            {
                var playerRole = player.GetCustomRole();

                CustomRoleManager.OnFixedUpdate(player);

                if (DoubleTrigger.FirstTriggerTimer.Count > 0)
                    DoubleTrigger.OnFixedUpdate(player);

                // Agitater
                if (Agitater.IsEnable && Agitater.CurrentBombedPlayer == player.PlayerId)
                    Agitater.OnFixedUpdate(player);

                if (PlagueDoctor.IsEnable)
                    PlagueDoctor.OnCheckPlayerPosition(player);

                switch (playerRole)
                {
                    case CustomRoles.Penguin:
                        Penguin.OnFixedUpdate(player);
                        break;

                    case CustomRoles.Vampire:
                        Vampire.OnFixedUpdate(player);
                        break;

                    case CustomRoles.Vampiress:
                        Vampiress.OnFixedUpdate(player);
                        break;

                    case CustomRoles.Poisoner:
                        Poisoner.OnFixedUpdate(player);
                        break;

                    case CustomRoles.Mercenary:
                        Mercenary.OnFixedUpdate(player);
                        break;

                    case CustomRoles.PlagueBearer:
                        PlagueBearer.OnFixedUpdate(player);
                        break;

                    case CustomRoles.Warlock:
                        if (Main.WarlockTimer.TryGetValue(player.PlayerId, out var warlockTimer))
                        {
                            var playerId = player.PlayerId;
                            if (player.IsAlive())
                            {
                                if (warlockTimer >= 1f)
                                {
                                    player.RpcResetAbilityCooldown();
                                    Main.isCursed = false;
                                    player.SyncSettings();
                                    Main.WarlockTimer.Remove(playerId);
                                }
                                else
                                {
                                    warlockTimer += Time.fixedDeltaTime;
                                    Main.WarlockTimer[playerId] = warlockTimer;
                                }
                            }
                            else
                            {
                                Main.WarlockTimer.Remove(playerId);
                            }
                        }
                        break;

                    case CustomRoles.Arsonist:
                        if (Main.ArsonistTimer.TryGetValue(player.PlayerId, out var arsonistTimerData))
                        {
                            var playerId = player.PlayerId;
                            if (!player.IsAlive() || Pelican.IsEaten(playerId))
                            {
                                Main.ArsonistTimer.Remove(playerId);
                                Utils.NotifyRoles(SpecifySeer: player);
                                RPC.ResetCurrentDousingTarget(playerId);
                            }
                            else
                            {
                                var (arTarget, arTime) = arsonistTimerData;

                                if (!arTarget.IsAlive())
                                {
                                    Main.ArsonistTimer.Remove(playerId);
                                }
                                else if (arTime >= Options.ArsonistDouseTime.GetFloat())
                                {
                                    player.SetKillCooldown();
                                    Main.ArsonistTimer.Remove(playerId);
                                    Main.isDoused[(playerId, arTarget.PlayerId)] = true;
                                    player.RpcSetDousedPlayer(arTarget, true);
                                    Utils.NotifyRoles(SpecifySeer: player, SpecifyTarget: arTarget, ForceLoop: true);
                                    RPC.ResetCurrentDousingTarget(playerId);
                                }
                                else
                                {
                                    float range = NormalGameOptionsV07.KillDistances[Mathf.Clamp(player.Is(Reach.IsReach) ? 2 : Main.NormalOptions.KillDistance, 0, 2)] + 0.5f;
                                    float distance = Vector2.Distance(player.GetCustomPosition(), arTarget.GetCustomPosition());

                                    if (distance <= range)
                                    {
                                        Main.ArsonistTimer[playerId] = (arTarget, arTime + Time.fixedDeltaTime);
                                    }
                                    else
                                    {
                                        Main.ArsonistTimer.Remove(playerId);
                                        Utils.NotifyRoles(SpecifySeer: player, SpecifyTarget: arTarget, ForceLoop: true);
                                        RPC.ResetCurrentDousingTarget(playerId);

                                        Logger.Info($"Canceled: {player.GetNameWithRole()}", "Arsonist");
                                    }
                                }
                            }
                        }
                        break;

                    case CustomRoles.Solsticer:
                        Solsticer.OnFixedUpdate(player);
                        break;
                }
                if (player.Is(CustomRoles.Statue) && player.IsAlive())
                    Statue.OnFixedUpdate(player);
            
                // Revolutionist
                #region Revolutionist Timer
                if (Main.RevolutionistTimer.TryGetValue(player.PlayerId, out var revolutionistTimerData))
                {
                    var playerId = player.PlayerId;
                    if (!player.IsAlive() || Pelican.IsEaten(playerId))
                    {
                        Main.RevolutionistTimer.Remove(playerId);
                        Utils.NotifyRoles(SpecifySeer: player);
                        RPC.ResetCurrentDrawTarget(playerId);
                    }
                    else
                    {
                        var (rv_target, rv_time) = revolutionistTimerData;

                        if (!rv_target.IsAlive())
                        {
                            Main.RevolutionistTimer.Remove(playerId);
                        }
                        else if (rv_time >= Options.RevolutionistDrawTime.GetFloat())
                        {
                            var rvTargetId = rv_target.PlayerId;
                            player.SetKillCooldown();
                            Main.RevolutionistTimer.Remove(playerId);
                            Main.isDraw[(playerId, rvTargetId)] = true;
                            player.RpcSetDrawPlayer(rv_target, true);
                            Utils.NotifyRoles(SpecifySeer: player, SpecifyTarget: rv_target, ForceLoop: true);
                            RPC.ResetCurrentDrawTarget(playerId);
                            if (IRandom.Instance.Next(1, 100) <= Options.RevolutionistKillProbability.GetInt())
                            {
                                rv_target.SetRealKiller(player);
                                Main.PlayerStates[rvTargetId].deathReason = PlayerState.DeathReason.Sacrifice;
                                player.RpcMurderPlayerV3(rv_target);
                                Main.PlayerStates[rvTargetId].SetDead();
                                Logger.Info($"Revolutionist: {player.GetNameWithRole()} killed by {rv_target.GetNameWithRole()}", "Revolutionist");
                            }
                        }
                        else
                        {
                            float range = NormalGameOptionsV07.KillDistances[Mathf.Clamp(player.Is(Reach.IsReach) ? 2 : Main.NormalOptions.KillDistance, 0, 2)] + 0.5f;
                            float dis = Vector2.Distance(player.GetCustomPosition(), rv_target.GetCustomPosition());
                            if (dis <= range)
                            {
                                Main.RevolutionistTimer[playerId] = (rv_target, rv_time + Time.fixedDeltaTime);
                            }
                            else
                            {
                                Main.RevolutionistTimer.Remove(playerId);
                                Utils.NotifyRoles(SpecifySeer: player, SpecifyTarget: rv_target, ForceLoop: true);
                                RPC.ResetCurrentDrawTarget(playerId);
                                Logger.Info($"Canceled: {__instance.GetNameWithRole()}", "Revolutionist");
                            }
                        }
                    }
                }
                if (player.IsDrawDone() && player.IsAlive())
                {
                    var playerId = player.PlayerId;
                    if (Main.RevolutionistStart.TryGetValue(playerId, out long startTime))
                    {
                        if (Main.RevolutionistLastTime.TryGetValue(playerId, out long lastTime))
                        {
                            long nowtime = Utils.GetTimeStamp();
                            if (lastTime != nowtime)
                            {
                                Main.RevolutionistLastTime[playerId] = nowtime;
                                lastTime = nowtime;
                            }
                            int time = (int)(lastTime - startTime);
                            int countdown = Options.RevolutionistVentCountDown.GetInt() - time;
                            Main.RevolutionistCountdown.Clear();

                            if (countdown <= 0)
                            {
                                Utils.GetDrawPlayerCount(playerId, out var list);

                                foreach (var pc in list.Where(x => x != null && x.IsAlive()).ToArray())
                                {
                                    pc.Data.IsDead = true;
                                    Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Sacrifice;
                                    pc.RpcMurderPlayerV3(pc);
                                    Main.PlayerStates[pc.PlayerId].SetDead();
                                    Utils.NotifyRoles(SpecifySeer: pc);
                                }
                                player.Data.IsDead = true;
                                Main.PlayerStates[playerId].deathReason = PlayerState.DeathReason.Sacrifice;
                                player.RpcMurderPlayerV3(player);
                                Main.PlayerStates[playerId].SetDead();
                            }
                            else
                            {
                                Main.RevolutionistCountdown.TryAdd(playerId, countdown);
                            }
                        }
                        else
                        {
                            Main.RevolutionistLastTime.TryAdd(playerId, Main.RevolutionistStart[playerId]);
                        }
                    }
                    else
                    {
                        Main.RevolutionistStart.TryAdd(playerId, Utils.GetTimeStamp());
                    }
                }
                #endregion


                if (!lowLoad)
                {
                    if (Main.AllKillers.TryGetValue(player.PlayerId, out var ktime) && ktime + Options.WitnessTime.GetInt() < Utils.GetTimeStamp())
                        Main.AllKillers.Remove(player.PlayerId);

                    playerRole = player.GetCustomRole();

                    CustomRoleManager.OnFixedUpdateLowLoad(player);

                    if (Kamikaze.IsEnable)
                        Kamikaze.MurderKamikazedPlayers(player);

                    if (Alchemist.IsEnable)
                        Alchemist.OnFixedUpdateINV(player);

                    if (Puppeteer.IsEnable)
                        Puppeteer.OnFixedUpdate(player);

                    if (Shroud.IsEnable)
                        Shroud.OnFixedUpdate(player);

                    if (Spiritcaller.IsEnable)
                        Spiritcaller.OnFixedUpdate(player);

                    if (Pitfall.IsEnable)
                        Pitfall.OnFixedUpdate(player);

                    if (Rainbow.isEnabled)
                        Rainbow.OnFixedUpdate();

                    if (Alchemist.BloodlustList.ContainsKey(player.PlayerId))
                        Alchemist.OnFixedUpdate(player);

                    switch (playerRole)
                    {
                        case CustomRoles.RiftMaker:
                            RiftMaker.OnFixedUpdate(player);
                            break;
                        case CustomRoles.Swooper:
                            Swooper.OnFixedUpdate(player);
                            break;

                        case CustomRoles.Wraith:
                            Wraith.OnFixedUpdate(player);
                            break;

                        case CustomRoles.Lightning:
                            Lightning.OnFixedUpdate();
                            break;

                        case CustomRoles.BloodKnight:
                            BloodKnight.OnFixedUpdate(player);
                            break;

                        case CustomRoles.Wildling:
                            Wildling.OnFixedUpdate(player);
                            break;

                        case CustomRoles.Mastermind:
                            Mastermind.OnFixedUpdate();
                            break;

                        case CustomRoles.Pelican:
                            Pelican.OnFixedUpdate();
                            break;

                        case CustomRoles.Spy:
                            Spy.OnFixedUpdate(player);
                            break;

                        case CustomRoles.Glitch:
                            Glitch.UpdateHackCooldown(player);
                            break;

                        case CustomRoles.Veteran:
                            if (Main.VeteranInProtect.TryGetValue(player.PlayerId, out var vtime) && vtime + Options.VeteranSkillDuration.GetInt() < Utils.GetTimeStamp())
                            {
                                Main.VeteranInProtect.Remove(player.PlayerId);

                                if (!Options.DisableShieldAnimations.GetBool())
                                {
                                    player.RpcGuardAndKill();
                                }
                                else
                                {
                                    player.RpcResetAbilityCooldown();
                                }

                                player.Notify(string.Format(GetString("VeteranOffGuard"), Main.VeteranNumOfUsed[player.PlayerId]));
                            }
                            break;

                        case CustomRoles.Grenadier:
                            var stopGrenadierSkill = false;
                            var stopMadGrenadierSkill = false;

                            if (Main.GrenadierBlinding.TryGetValue(player.PlayerId, out var grenadierTime) && grenadierTime + Options.GrenadierSkillDuration.GetInt() < Utils.GetTimeStamp())
                            {
                                Main.GrenadierBlinding.Remove(player.PlayerId);
                                stopGrenadierSkill = true;
                            }

                            if (Main.MadGrenadierBlinding.TryGetValue(player.PlayerId, out var madGrenadierTime) && madGrenadierTime + Options.GrenadierSkillDuration.GetInt() < Utils.GetTimeStamp())
                            {
                                Main.MadGrenadierBlinding.Remove(player.PlayerId);
                                stopMadGrenadierSkill = true;
                            }

                            if (stopGrenadierSkill || stopMadGrenadierSkill)
                            {
                                if (!Options.DisableShieldAnimations.GetBool())
                                {
                                    player.RpcGuardAndKill();
                                }
                                else
                                {
                                    player.RpcResetAbilityCooldown();
                                }
                                player.Notify(GetString("GrenadierSkillStop"));
                                Utils.MarkEveryoneDirtySettings();
                            }
                            break;

                        case CustomRoles.Lighter:
                            if (Main.Lighter.TryGetValue(player.PlayerId, out var ltime) && ltime + Options.LighterSkillDuration.GetInt() < Utils.GetTimeStamp())
                            {
                                Main.Lighter.Remove(player.PlayerId);
                                if (!Options.DisableShieldAnimations.GetBool())
                                {
                                    player.RpcGuardAndKill();
                                }
                                else
                                {
                                    player.RpcResetAbilityCooldown();
                                }
                                player.Notify(GetString("LighterSkillStop"));
                                player.MarkDirtySettings();
                            }
                            break;

                        case CustomRoles.Mario:
                            if (Main.MarioVentCount[player.PlayerId] >= Options.MarioVentNumWin.GetInt())
                            {
                                Main.MarioVentCount[player.PlayerId] = Options.MarioVentNumWin.GetInt();
                                if (!CustomWinnerHolder.CheckForConvertedWinner(player.PlayerId))
                                {
                                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Mario);
                                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                                }
                            }
                            break;

                        case CustomRoles.Vulture:
                            if (Vulture.BodyReportCount[player.PlayerId] >= Vulture.NumberOfReportsToWin.GetInt())
                            {
                                Vulture.BodyReportCount[player.PlayerId] = Vulture.NumberOfReportsToWin.GetInt();
                                if (!CustomWinnerHolder.CheckForConvertedWinner(player.PlayerId))
                                {
                                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Vulture);
                                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                                }
                            }
                            break;
                    }

                    if (Options.LadderDeath.GetBool() && player.IsAlive())
                        FallFromLadder.FixedUpdate(player);

                    if (GameStates.IsInGame && CustomRoles.Lovers.IsEnable())
                        LoversSuicide();


                    //Local Player only
                    if (player.AmOwner)
                    {
                        DisableDevice.FixedUpdate();

                        if (CustomRoles.Monitor.IsClassEnable())
                            Monitor.FixedUpdate();
                    }
                }
            }

            if (!lowLoad)
            {
                if (!Main.DoBlockNameChange)
                    Utils.ApplySuffix(__instance);

                if (GameStates.IsInGame && Main.RefixCooldownDelay <= 0)
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc.Is(CustomRoles.Vampire) || pc.Is(CustomRoles.Warlock) || pc.Is(CustomRoles.Ninja) || pc.Is(CustomRoles.Vampiress))
                            Main.AllPlayerKillCooldown[pc.PlayerId] = Options.DefaultKillCooldown * 2;
                        if (pc.Is(CustomRoles.Poisoner))
                            Main.AllPlayerKillCooldown[pc.PlayerId] = Poisoner.KillCooldown.GetFloat() * 2;
                    }
            }
        }

        //Local Player only
        if (player.AmOwner && GameStates.IsInTask)
        {
            //Kill target override processing
            if (!player.Is(CustomRoleTypes.Impostor) && player.CanUseKillButton() && !player.Data.IsDead)
            {
                var players = __instance.GetPlayersInAbilityRangeSorted(false);
                PlayerControl closest = players.Count <= 0 ? null : players[0];
                HudManager.Instance.KillButton.SetTarget(closest);
            }
        }

        var RoleTextTransform = __instance.cosmetics.nameText.transform.Find("RoleText");
        var RoleText = RoleTextTransform.GetComponent<TMPro.TextMeshPro>();

        if (RoleText != null && __instance != null && !lowLoad)
        {
            if (GameStates.IsLobby)
            {
                if (Main.playerVersion.TryGetValue(__instance.GetClientId(), out var ver))
                {
                    if (Main.ForkId != ver.forkId) // フォークIDが違う場合
                        __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>{ver.forkId}</size>\n{__instance?.name}</color>";
                    else if (Main.version.CompareTo(ver.version) == 0)
                        __instance.cosmetics.nameText.text = ver.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})" ? $"<color=#87cefa>{__instance.name}</color>" : $"<color=#ffff00><size=1.2>{ver.tag}</size>\n{__instance?.name}</color>";
                    else __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>v{ver.version}</size>\n{__instance?.name}</color>";
                }
                else __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName;
            }
            if (GameStates.IsInGame)
            {
                var RoleTextData = Utils.GetRoleAndSubText(PlayerControl.LocalPlayer.PlayerId, __instance.PlayerId);
                RoleText.text = RoleTextData.Item1;
                RoleText.color = RoleTextData.Item2;
                if (Options.CurrentGameMode == CustomGameMode.FFA) RoleText.text = string.Empty;
                if (__instance.AmOwner || Options.CurrentGameMode == CustomGameMode.FFA) RoleText.enabled = true;
                else if (ExtendedPlayerControl.KnowRoleTarget(PlayerControl.LocalPlayer, __instance)) RoleText.enabled = true;
                else RoleText.enabled = false;
                if (!PlayerControl.LocalPlayer.Data.IsDead && PlayerControl.LocalPlayer.IsRevealedPlayer(__instance) && __instance.Is(CustomRoles.Trickster))
                {
                    RoleText.text = Overseer.RandomRole[PlayerControl.LocalPlayer.PlayerId]; // random role for revealed trickster
                    RoleText.text += TaskState.GetTaskState(); // random task count for revealed trickster
                }

                if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay)
                {
                    RoleText.enabled = false; //ゲームが始まっておらずフリープレイでなければロールを非表示
                    if (!__instance.AmOwner) __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName;
                }
                if (Main.VisibleTasksCount) //他プレイヤーでVisibleTasksCountは有効なら
                    RoleText.text += Utils.GetProgressText(__instance); //ロールの横にタスクなど進行状況表示


                var seer = PlayerControl.LocalPlayer;
                var seerRoleClass = seer.GetRoleClass();
                var target = __instance;

                string RealName = target.GetRealName();

                Mark.Clear();
                Suffix.Clear();


                if (target.AmOwner && GameStates.IsInTask)
                {
                    switch (target.GetCustomRole())
                    {
                        case CustomRoles.Arsonist:
                            if (target.IsDouseDone())
                                RealName = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Arsonist), GetString("EnterVentToWin"));
                            break;

                        case CustomRoles.Revolutionist:
                            if (target.IsDrawDone())
                                RealName = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Revolutionist), string.Format(GetString("EnterVentWinCountDown"), Main.RevolutionistCountdown.TryGetValue(seer.PlayerId, out var x) ? x : 10));
                            break;
                    }

                    if (Pelican.IsEaten(seer.PlayerId))
                        RealName = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Pelican), GetString("EatenByPelican"));

                    if (Options.CurrentGameMode == CustomGameMode.FFA)
                        FFAManager.GetNameNotify(target, ref RealName);

                    if (Deathpact.IsInActiveDeathpact(seer))
                        RealName = Deathpact.GetDeathpactString(seer);

                    if (NameNotifyManager.GetNameNotify(target, out var name))
                        RealName = name;
                }

                RealName = RealName.ApplyNameColorData(seer, target, false);
                var seerRole = seer.GetCustomRole();


                Mark.Append(seerRoleClass?.GetMark(seer, target, false));
                Mark.Append(CustomRoleManager.GetMarkOthers(seer, target, false));

                Suffix.Append(CustomRoleManager.GetLowerTextOthers(seer, target));

                Suffix.Append(seerRoleClass?.GetSuffix(seer, target));
                Suffix.Append(CustomRoleManager.GetSuffixOthers(seer, target));

                if (target.GetPlayerTaskState().IsTaskFinished)
                {
                    seerRole = seer.GetCustomRole();

                    if (seerRole.IsImpostor())
                    {
                        if (target.Is(CustomRoles.Snitch) && target.Is(CustomRoles.Madmate))
                            Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "★"));
                    }
                }

                if (PlagueDoctor.IsEnable) 
                    Mark.Append(PlagueDoctor.GetMarkOthers(seer, target));
                
                if (Snitch.IsEnable)
                {
                    Mark.Append(Snitch.GetWarningMark(seer, target));
                    Mark.Append(Snitch.GetWarningArrow(seer, target));
                }

                if (CustomRoles.Solsticer.RoleExist())
                    if (target.AmOwner || target.Is(CustomRoles.Solsticer))
                        Mark.Append(Solsticer.GetWarningArrow(seer, target));

                if (Executioner.IsEnable)
                    Mark.Append(Executioner.TargetMark(seer, target));

                if (Gamer.IsEnable)
                    Mark.Append(Gamer.TargetMark(seer, target));

                if (Totocalcio.IsEnable)
                    Mark.Append(Totocalcio.TargetMark(seer, target));

                if (Romantic.IsEnable)
                    Mark.Append(Romantic.TargetMark(seer, target));

                if (Lawyer.IsEnable)
                    Mark.Append(Lawyer.LawyerMark(seer, target));

                if (EvilTracker.IsEnable && seer.Is(CustomRoles.EvilTracker))
                    Mark.Append(EvilTracker.GetTargetMark(seer, target));

                if (Tracker.IsEnable && seer.Is(CustomRoles.Tracker))
                    Mark.Append(Tracker.GetTargetMark(seer, target));

                if (target.Is(CustomRoles.SuperStar) && Options.EveryOneKnowSuperStar.GetBool())
                    Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.SuperStar), "★"));

                if (target.Is(CustomRoles.Cyber) && Cyber.CyberKnown.GetBool())
                    Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cyber), "★"));

                if (Lightning.IsEnable && Lightning.IsGhost(target))
                    Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lightning), "■"));


                seerRole = seer.GetCustomRole();
                switch (seerRole)
                {
                    case CustomRoles.Lookout:
                        if (seer.IsAlive() && target.IsAlive())
                            Mark.Append(Utils.ColorString(Utils.GetRoleColor(seerRole), " " + target.PlayerId.ToString()) + " ");
                        break;

                    case CustomRoles.PlagueBearer:
                        if (PlagueBearer.IsPlagued(seer.PlayerId, target.PlayerId))
                            Mark.Append($"<color={Utils.GetRoleColorCode(seerRole)}>●</color>");
                        break;

                    case CustomRoles.Arsonist:
                        if (seer.IsDousedPlayer(target))
                            Mark.Append($"<color={Utils.GetRoleColorCode(seerRole)}>▲</color>");

                        else if (Main.currentDousingTarget != byte.MaxValue && Main.currentDousingTarget == target.PlayerId)
                            Mark.Append($"<color={Utils.GetRoleColorCode(seerRole)}>△</color>");
                        break;

                    case CustomRoles.Revolutionist:
                        if (seer.IsDrawPlayer(target))
                            Mark.Append($"<color={Utils.GetRoleColorCode(seerRole)}>●</color>");

                        else if (Main.currentDrawTarget != byte.MaxValue && Main.currentDrawTarget == target.PlayerId)
                            Mark.Append($"<color={Utils.GetRoleColorCode(seerRole)}>○</color>");
                        break;

                    case CustomRoles.Medic:
                        if ((Medic.WhoCanSeeProtect.GetInt() is 0 or 1) && (Medic.InProtect(target.PlayerId) || Medic.TempMarkProtected == target.PlayerId))
                            Mark.Append($"<color={Utils.GetRoleColorCode(seerRole)}>✚</color>");
                        break;

                    case CustomRoles.Puppeteer:
                        Mark.Append(Puppeteer.TargetMark(seer, target));
                        break;

                    case CustomRoles.Shroud:
                        Mark.Append(Shroud.TargetMark(seer, target));
                        break;

                    case CustomRoles.Quizmaster:
                        Mark.Append(Quizmaster.TargetMark(seer, target));
                        break;
                }

                if (Mini.IsEnable && Mini.EveryoneCanKnowMini.GetBool())
                {
                    if (target.Is(CustomRoles.NiceMini))
                        Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mini), Mini.Age != 18 && Mini.UpDateAge.GetBool() ? $"({Mini.Age})" : ""));

                    else if (target.Is(CustomRoles.EvilMini))
                        Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mini), Mini.Age != 18 && Mini.UpDateAge.GetBool() ? $"({Mini.Age})" : ""));
                }

                if (Medic.IsEnable)
                {
                    if ((Medic.WhoCanSeeProtect.GetInt() is 0 or 2) && seer.PlayerId == target.PlayerId && (Medic.InProtect(seer.PlayerId) || Medic.TempMarkProtected == seer.PlayerId))
                        Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Medic)}>✚</color>");

                    if (!seer.IsAlive() && Medic.InProtect(target.PlayerId) && !seer.Is(CustomRoles.Medic))
                        Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Medic)}>✚</color>");
                }

                if (Sniper.IsEnable && target.AmOwner)
                    Mark.Append(Sniper.GetShotNotify(target.PlayerId));

                if (target.Is(CustomRoles.Lovers) && seer.Is(CustomRoles.Lovers))
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♥</color>");
                }
                else if (target.Is(CustomRoles.Lovers) && seer.Data.IsDead)
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♥</color>");
                }
                else if (target.Is(CustomRoles.Ntr) || seer.Is(CustomRoles.Ntr))
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♥</color>");
                }
                else if (target == seer && CustomRolesHelper.RoleExist(CustomRoles.Ntr))
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♥</color>");
                }

                if (Snitch.IsEnable)
                    Suffix.Append(Snitch.GetSnitchArrow(seer, target));

                if (BountyHunter.On)
                    Suffix.Append(BountyHunter.GetTargetArrow(seer, target));

                if (Mortician.IsEnable)
                    Suffix.Append(Mortician.GetTargetArrow(seer, target));

                if (EvilTracker.IsEnable)
                    Suffix.Append(EvilTracker.GetTargetArrow(seer, target));

                if (PlagueDoctor.IsEnable)
                    Suffix.Append(PlagueDoctor.GetLowerTextOthers(seer, target));

                if (Tracker.IsEnable)
                    Suffix.Append(Tracker.GetTrackerArrow(seer, target));


                if (Deathpact.On)
                {
                    Suffix.Append(Deathpact.GetDeathpactPlayerArrow(seer, target));
                    Suffix.Append(Deathpact.GetDeathpactMark(seer, target));
                }

                if (Spiritualist.IsEnable)
                    Suffix.Append(Spiritualist.GetSpiritualistArrow(seer, target));

                if (Options.CurrentGameMode == CustomGameMode.FFA)
                    Suffix.Append(FFAManager.GetPlayerArrow(seer, target));

                if (Tracefinder.IsEnable)
                    Suffix.Append(Tracefinder.GetTargetArrow(seer, target));

                if (Vulture.IsEnable && Vulture.ArrowsPointingToDeadBody.GetBool())
                    Suffix.Append(Vulture.GetTargetArrow(seer, target));

                if (GameStates.IsInTask)
                {
                    if (seer.Is(CustomRoles.AntiAdminer))
                    {
                        //seer.GetCustomRole().GetRoleClass().OnFixedUpdateLowLoad(player);
                        if (target.AmOwner)
                            Suffix.Append(AntiAdminer.GetSuffix());
                    }
                    if (seer.Is(CustomRoles.Monitor))
                    {
                        Monitor.FixedUpdate();
                    }
                    if (player.Is(CustomRoles.TimeMaster))
                    {
                        if (Main.TimeMasterInProtect.TryGetValue(player.PlayerId, out var vtime) && vtime + Options.TimeMasterSkillDuration.GetInt() < Utils.GetTimeStamp())
                        {
                            Main.TimeMasterInProtect.Remove(player.PlayerId);
                            if (!Options.DisableShieldAnimations.GetBool()) player.RpcGuardAndKill();
                            else player.RpcResetAbilityCooldown();
                            player.Notify(GetString("TimeMasterSkillStop"));
                        }
                    }
                }

                /*if(main.AmDebugger.Value && main.BlockKilling.TryGetValue(target.PlayerId, out var isBlocked)) {
                    Mark = isBlocked ? "(true)" : "(false)";}*/

                // Devourer
                if (Devourer.On)
                {
                    bool targetDevoured = Devourer.HideNameOfTheDevoured(target.PlayerId);
                    if (targetDevoured)
                        RealName = GetString("DevouredName");
                }

                // Camouflage
                if ((Utils.IsActive(SystemTypes.Comms) && Camouflage.IsActive) || Camouflager.AbilityActivated)
                    RealName = $"<size=0%>{RealName}</size> ";

                // When MushroomMixup Sabotage Is Active
                //else if (Utils.IsActive(SystemTypes.MushroomMixupSabotage))
                //    RealName = $"<size=0%>{RealName}</size> ";


                string DeathReason = seer.Data.IsDead && seer.KnowDeathReason(target) ? $" ({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId))})" : "";

                target.cosmetics.nameText.text = $"{RealName}{DeathReason}{Mark}";

                if (Suffix.ToString() != "")
                {
                    RoleText.transform.SetLocalY(0.35f);
                    target.cosmetics.nameText.text += "\r\n" + Suffix.ToString();
                }
                else
                {
                    RoleText.transform.SetLocalY(0.2f);
                }
            }
            else
            {
                RoleText.transform.SetLocalY(0.2f);
            }
        }
        return Task.CompletedTask;
    }
    //FIXME: 役職クラス化のタイミングで、このメソッドは移動予定
    public static void LoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (Options.LoverSuicide.GetBool() && Main.isLoversDead == false)
        {
            foreach (var loversPlayer in Main.LoversPlayers.ToArray())
            {
                //生きていて死ぬ予定でなければスキップ
                if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                Main.isLoversDead = true;
                foreach (var partnerPlayer in Main.LoversPlayers.ToArray())
                {
                    //本人ならスキップ
                    if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;

                    //残った恋人を全て殺す(2人以上可)
                    //生きていて死ぬ予定もない場合は心中
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        if (partnerPlayer.Is(CustomRoles.Lovers))
                        {
                            Main.PlayerStates[partnerPlayer.PlayerId].deathReason = PlayerState.DeathReason.FollowingSuicide;
                            if (isExiled)
                                CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                            else
                                partnerPlayer.RpcMurderPlayerV3(partnerPlayer);
                        }
                    }
                }
            }
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
class PlayerStartPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        if (GameStates.IsHideNSeek) return;

        var roleText = UnityEngine.Object.Instantiate(__instance.cosmetics.nameText);
        roleText.transform.SetParent(__instance.cosmetics.nameText.transform);
        roleText.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        roleText.fontSize -= 1.2f;
        roleText.text = "RoleText";
        roleText.gameObject.name = "RoleText";
        roleText.enabled = false;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetColor))]
class SetColorPatch
{
    public static bool IsAntiGlitchDisabled = false;
    public static bool Prefix(PlayerControl __instance, int bodyColor)
    {
        //色変更バグ対策
        if (!AmongUsClient.Instance.AmHost || __instance.CurrentOutfit.ColorId == bodyColor || IsAntiGlitchDisabled) return true;
        return true;
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoExitVent))]
class CoExitVentPatch
{
    public static void Postfix(PlayerPhysics __instance, [HarmonyArgument(0)] int id)
    {
        if (GameStates.IsHideNSeek) return;

        if (Options.CurrentGameMode == CustomGameMode.FFA && FFAManager.FFA_DisableVentingWhenKCDIsUp.GetBool())
        {
            if (__instance.myPlayer != null)
            {
                var now = Utils.GetTimeStamp();
                byte playerId = __instance.myPlayer.PlayerId;
                if (FFAManager.FFAEnterVentTime.ContainsKey(playerId))
                {
                    if (!FFAManager.FFAVentDuration.ContainsKey(playerId)) FFAManager.FFAVentDuration[playerId] = 0f;
                    FFAManager.FFAVentDuration[playerId] = FFAManager.FFAVentDuration[playerId] + (now - FFAManager.FFAEnterVentTime[playerId]);

                    Logger.Warn($"Vent Duration = {FFAManager.FFAVentDuration[playerId]}, vent enter time = {FFAManager.FFAEnterVentTime[playerId]}, vent exit time = {now}, vent time = {now - FFAManager.FFAEnterVentTime[playerId]}", "FFA VENT DURATION");
                    FFAManager.FFAEnterVentTime.Remove(playerId);
                }
            }
        }

        if (Mole.IsEnable)
            Mole.OnExitVent(__instance.myPlayer, id);
    }
}

[HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
class EnterVentPatch
{
    public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc)
    {
        if (GameStates.IsHideNSeek) return;

        pc.GetRoleClass()?.OnEnterVent(pc, __instance);

        Witch.OnEnterVent(pc);
        HexMaster.OnEnterVent(pc);
        //Occultist.OnEnterVent(pc);

     /* if (pc.Is(CustomRoles.Wraith)) // THIS WAS FOR WEREWOLF TESTING PURPOSES, PLEASE IGNORE
        {
            pc?.MyPhysics?.RpcBootFromVent(__instance.Id);            
        } */

     /* else if (pc.Is(CustomRoles.Paranoia))
        {
            if (Main.ParaUsedButtonCount.TryGetValue(pc.PlayerId, out var count) && count < Options.ParanoiaNumOfUseButton.GetInt())
            {
                Main.ParaUsedButtonCount[pc.PlayerId] += 1;
                if (AmongUsClient.Instance.AmHost)
                {
                    _ = new LateTask(() =>
                    {
                        Utils.SendMessage(GetString("SkillUsedLeft") + (Options.ParanoiaNumOfUseButton.GetInt() - Main.ParaUsedButtonCount[pc.PlayerId]).ToString(), pc.PlayerId);
                    }, 4.0f, "Skill Remain Message");
                }
                pc?.MyPhysics?.RpcBootFromVent(__instance.Id);
                pc?.NoCheckStartMeeting(pc?.Data);
            }
        } */

        if (pc.Is(CustomRoles.Mario))
        {
            Main.MarioVentCount.TryAdd(pc.PlayerId, 0);
            Main.MarioVentCount[pc.PlayerId]++;
            Utils.NotifyRoles(SpecifySeer: pc);
            if (AmongUsClient.Instance.AmHost && Main.MarioVentCount[pc.PlayerId] >= Options.MarioVentNumWin.GetInt())
            {
                if (!CustomWinnerHolder.CheckForConvertedWinner(pc.PlayerId))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Mario);
                    CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                }
            }
        }

        if (!AmongUsClient.Instance.AmHost) return;

        Main.LastEnteredVent.Remove(pc.PlayerId);
        Main.LastEnteredVent.Add(pc.PlayerId, __instance);
        Main.LastEnteredVentLocation.Remove(pc.PlayerId);
        Main.LastEnteredVentLocation.Add(pc.PlayerId, pc.GetCustomPosition());

        Swooper.OnEnterVent(pc, __instance);
        Wraith.OnEnterVent(pc, __instance);
        Alchemist.OnEnterVent(pc, __instance.Id);
        Lurker.OnEnterVent(pc);

        if (pc.GetCustomRole() == CustomRoles.Bastion)
        {
            if (Main.BastionNumberOfAbilityUses >= 1)
            {
                Main.BastionNumberOfAbilityUses -= 1;
                if (!Main.BombedVents.Contains(__instance.Id)) Main.BombedVents.Add(__instance.Id);
                pc.Notify(GetString("VentBombSuccess"));
            }
            else
            {
                pc.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
            }
        }


        if (pc.Is(CustomRoles.Veteran) && !Main.VeteranInProtect.ContainsKey(pc.PlayerId))
        {
            Main.VeteranInProtect.Remove(pc.PlayerId);
            Main.VeteranInProtect.Add(pc.PlayerId, Utils.GetTimeStamp(DateTime.Now));
            Main.VeteranNumOfUsed[pc.PlayerId] -= 1;
            if (!Options.DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
            pc.RPCPlayCustomSound("Gunload");
            pc.Notify(GetString("VeteranOnGuard"), Options.VeteranSkillDuration.GetFloat());
        }
        if (pc.Is(CustomRoles.Unlucky))
        {
            Unlucky.SuicideRand(pc);
        }
        if (pc.Is(CustomRoles.Grenadier))
        {
            if (Main.GrenadierNumOfUsed[pc.PlayerId] >= 1)
            {
                if (pc.Is(CustomRoles.Madmate))
                {
                    Main.MadGrenadierBlinding.Remove(pc.PlayerId);
                    Main.MadGrenadierBlinding.Add(pc.PlayerId, Utils.GetTimeStamp());
                    Main.AllPlayerControls.Where(x => x.IsModClient()).Where(x => !x.GetCustomRole().IsImpostorTeam() && !x.Is(CustomRoles.Madmate)).Do(x => x.RPCPlayCustomSound("FlashBang"));
                }
                else
                {
                    Main.GrenadierBlinding.Remove(pc.PlayerId);
                    Main.GrenadierBlinding.Add(pc.PlayerId, Utils.GetTimeStamp());
                    Main.AllPlayerControls.Where(x => x.IsModClient()).Where(x => x.GetCustomRole().IsImpostor() || (x.GetCustomRole().IsNeutral() && Options.GrenadierCanAffectNeutral.GetBool())).Do(x => x.RPCPlayCustomSound("FlashBang"));
                }
                if (!Options.DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
                pc.RPCPlayCustomSound("FlashBang");
                pc.Notify(GetString("GrenadierSkillInUse"), Options.GrenadierSkillDuration.GetFloat());
                Main.GrenadierNumOfUsed[pc.PlayerId] -= 1;
                Utils.MarkEveryoneDirtySettings();
            }
        }
        if (pc.Is(CustomRoles.DovesOfNeace))
        {
            if (Main.DovesOfNeaceNumOfUsed[pc.PlayerId] < 1)
            {
                pc?.MyPhysics?.RpcBootFromVent(__instance.Id);
                pc.Notify(GetString("DovesOfNeaceMaxUsage"));
            }
            else
            {
                Main.DovesOfNeaceNumOfUsed[pc.PlayerId] -= 1;
                if (!Options.DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
                Main.AllAlivePlayerControls.Where(x =>
                pc.Is(CustomRoles.Madmate) ?
                (x.CanUseKillButton() && x.GetCustomRole().IsCrewmate()) :
                (x.CanUseKillButton())
                ).Do(x =>
                {
                    x.RPCPlayCustomSound("Dove");
                    x.ResetKillCooldown();
                    x.SetKillCooldown();
                    if (x.Is(CustomRoles.Mercenary))
                    { Mercenary.OnReportDeadBody(); }
                    x.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DovesOfNeace), GetString("DovesOfNeaceSkillNotify")));
                });
                pc.RPCPlayCustomSound("Dove");
                pc.Notify(string.Format(GetString("DovesOfNeaceOnGuard"), Main.DovesOfNeaceNumOfUsed[pc.PlayerId]));
            }
        }
        if (pc.Is(CustomRoles.Lighter))
        {
            if (Main.LighterNumOfUsed[pc.PlayerId] >= 1)
            {
                Main.Lighter.Remove(pc.PlayerId);
                Main.Lighter.Add(pc.PlayerId, Utils.GetTimeStamp());
                if (!Options.DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
                pc.Notify(GetString("LighterSkillInUse"), Options.LighterSkillDuration.GetFloat());
                Main.LighterNumOfUsed[pc.PlayerId] -= 1;
                pc.MarkDirtySettings();
            }
            else
            {
                pc.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
            }
        }
        if (pc.Is(CustomRoles.TimeMaster))
        {
            if (Main.TimeMasterNumOfUsed[pc.PlayerId] >= 1)
            {
                Main.TimeMasterNumOfUsed[pc.PlayerId] -= 1;
                Main.TimeMasterInProtect.Remove(pc.PlayerId);
                Main.TimeMasterInProtect.Add(pc.PlayerId, Utils.GetTimeStamp());

                if (!pc.IsModClient())
                {
                    pc.RpcGuardAndKill(pc);
                }
                pc.Notify(GetString("TimeMasterOnGuard"), Options.TimeMasterSkillDuration.GetFloat());

                foreach (var player in Main.AllPlayerControls)
                {
                    if (Main.TimeMasterBackTrack.TryGetValue(player.PlayerId, out var position))
                    {
                        if (player.CanBeTeleported() && player.PlayerId != pc.PlayerId)
                        {
                            player.RpcTeleport(position);
                        }
                        else if (pc == player)
                        {
                            player?.MyPhysics?.RpcBootFromVent(Main.LastEnteredVent.TryGetValue(player.PlayerId, out var vent) ? vent.Id : player.PlayerId);
                        }

                        Main.TimeMasterBackTrack.Remove(player.PlayerId);
                    }
                    else
                    {
                        Main.TimeMasterBackTrack.Add(player.PlayerId, player.GetCustomPosition());
                    }
                }
            }
        }
    }
}
[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoEnterVent))]
class CoEnterVentPatch
{
    public static bool Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] int id)
    {
        if (!AmongUsClient.Instance.AmHost || GameStates.IsHideNSeek) return true;
        Logger.Info($" {__instance.myPlayer.GetNameWithRole().RemoveHtmlTags()}, Vent ID: {id}", "CoEnterVent");

        //FFA
        if (Options.CurrentGameMode == CustomGameMode.FFA && FFAManager.FFA_DisableVentingWhenTwoPlayersAlive.GetBool() && Main.AllAlivePlayerControls.Length <= 2)
        {
            var pc = __instance?.myPlayer;
            _ = new LateTask(() =>
            {
                pc?.Notify(GetString("FFA-NoVentingBecauseTwoPlayers"), 7f);
                pc?.MyPhysics?.RpcBootFromVent(id);
            }, 0.5f, "Player No Venting Because Two Players");
            return true;
        }
        //FFA
        if (Options.CurrentGameMode == CustomGameMode.FFA && FFAManager.FFA_DisableVentingWhenKCDIsUp.GetBool())
        {

            var pc = __instance?.myPlayer;
            var now = Utils.GetTimeStamp();
            FFAManager.FFAEnterVentTime[pc.PlayerId] = now;
            if (!FFAManager.FFAVentDuration.ContainsKey(pc.PlayerId)) FFAManager.FFAVentDuration[pc.PlayerId] = 0;
            var canVent = (now - FFAManager.FFALastKill[pc.PlayerId]) <= (Main.AllPlayerKillCooldown[pc.PlayerId] + FFAManager.FFAVentDuration[pc.PlayerId]);
            Logger.Warn($"Enter Time = {now}, last kill time = {FFAManager.FFALastKill[pc.PlayerId]}, {FFAManager.FFAVentDuration[pc.PlayerId]}", "VENT DURATION TESTING");
            Logger.Warn($"can vent {canVent}", "FFA MODE VENTING");
            if (!canVent)
            {
                _ = new LateTask(() =>
                {
                    pc?.Notify(GetString("FFA-NoVentingBecauseKCDIsUP"), 7f);
                    pc?.MyPhysics?.RpcBootFromVent(id);
                }, 0.5f, "Player No Venting Because KCD Is UP");
                return true;
            }

        }

        if (RiftMaker.IsEnable) RiftMaker.OnVent(__instance.myPlayer, id);

        if (Glitch.hackedIdList.ContainsKey(__instance.myPlayer.PlayerId))
        {
            _ = new LateTask(() =>
            {
                __instance.myPlayer?.Notify(string.Format(GetString("HackedByGlitch"), GetString("GlitchVent")));
                __instance.myPlayer?.MyPhysics?.RpcBootFromVent(id);
            }, 0.5f, "Player Boot From Vent By Glith");
            return true;
        }

        if (Main.BombedVents.Contains(id))
        {
            var pc = __instance.myPlayer;
            if (pc.GetCustomRole().IsCrewmate() && !pc.Is(CustomRoles.Bastion) && !pc.IsCrewVenter() && !CopyCat.playerIdList.Contains(pc.PlayerId) && !Main.TasklessCrewmate.Contains(pc.PlayerId)) { }
            else
            {
                _ = new LateTask(() =>
                {
                    foreach (var bastion in Main.AllAlivePlayerControls.Where(bastion => bastion.GetCustomRole() == CustomRoles.Bastion).ToArray())
                    {
                        pc.SetRealKiller(bastion);
                        bastion.Notify(GetString("BastionNotify"));
                        pc.Notify(GetString("EnteredBombedVent"));

                        Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                        pc.RpcMurderPlayerV3(pc);
                        Main.BombedVents.Remove(id);
                    }
                }, 0.5f, "Player bombed by Bastion");
                return true;
            }
        }
        if (AmongUsClient.Instance.IsGameStarted)
        {
            if (__instance.myPlayer.IsDouseDone())
            {
                CustomSoundsManager.RPCPlayCustomSoundAll("Boom");
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (pc != __instance.myPlayer)
                    {
                        //生存者は焼殺
                        pc.SetRealKiller(__instance.myPlayer);
                        Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Torched;
                        pc.RpcMurderPlayerV3(pc);
                        Main.PlayerStates[pc.PlayerId].SetDead();
                    }
                }
                foreach (var pc in Main.AllPlayerControls) pc.KillFlash();
                if (!CustomWinnerHolder.CheckForConvertedWinner(__instance.myPlayer.PlayerId))
                {
                    CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Arsonist); //焼殺で勝利した人も勝利させる
                    CustomWinnerHolder.WinnerIds.Add(__instance.myPlayer.PlayerId);
                }
                return true;
            }
            else if (Options.ArsonistCanIgniteAnytime.GetBool())
            {
                var douseCount = Utils.GetDousedPlayerCount(__instance.myPlayer.PlayerId).Item1;
                if (douseCount >= Options.ArsonistMinPlayersToIgnite.GetInt()) // Don't check for max, since the player would not be able to ignite at all if they somehow get more players doused than the max
                {
                    if (douseCount > Options.ArsonistMaxPlayersToIgnite.GetInt()) Logger.Warn("Arsonist Ignited with more players doused than the maximum amount in the settings", "Arsonist Ignite");
                    foreach (var pc in Main.AllAlivePlayerControls)
                    {
                        if (!__instance.myPlayer.IsDousedPlayer(pc)) continue;
                        pc.KillFlash();
                        pc.SetRealKiller(__instance.myPlayer);
                        Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Torched;
                        pc.RpcMurderPlayerV3(pc);
                        Main.PlayerStates[pc.PlayerId].SetDead();
                    }
                    if (Main.AllAlivePlayerControls.Length == 1)
                    {
                        if (!CustomWinnerHolder.CheckForConvertedWinner(__instance.myPlayer.PlayerId))
                        {
                            CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Arsonist); //焼殺で勝利した人も勝利させる
                            CustomWinnerHolder.WinnerIds.Add(__instance.myPlayer.PlayerId);
                        }
                    }
                    return true;
                }
            }
        }

        if (AmongUsClient.Instance.IsGameStarted && __instance.myPlayer.IsDrawDone())//完成拉拢任务的玩家跳管后
        {
            if (!CustomWinnerHolder.CheckForConvertedWinner(__instance.myPlayer.PlayerId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Revolutionist);//革命者胜利
                Utils.GetDrawPlayerCount(__instance.myPlayer.PlayerId, out var x);
                CustomWinnerHolder.WinnerIds.Add(__instance.myPlayer.PlayerId);
                foreach (var apc in x.ToArray()) CustomWinnerHolder.WinnerIds.Add(apc.PlayerId);//胜利玩家
            }
            return true;
        }

        // Fix Vent Stuck
        if (
            (__instance.myPlayer.Data.Role.Role != RoleTypes.Engineer
                && !__instance.myPlayer.CanUseImpostorVentButton())
            || (__instance.myPlayer.Is(CustomRoles.Mayor) && Mayor.MayorUsedButtonCount.TryGetValue(__instance.myPlayer.PlayerId, out var count) && count >= Mayor.MayorNumOfUseButton.GetInt())
          //|| (__instance.myPlayer.Is(CustomRoles.Paranoia) && Main.ParaUsedButtonCount.TryGetValue(__instance.myPlayer.PlayerId, out var count2) && count2 >= Options.ParanoiaNumOfUseButton.GetInt())
            || (__instance.myPlayer.Is(CustomRoles.Veteran) && Main.VeteranNumOfUsed.TryGetValue(__instance.myPlayer.PlayerId, out var count3) && count3 < 1)
            || (__instance.myPlayer.Is(CustomRoles.DovesOfNeace) && Main.DovesOfNeaceNumOfUsed.TryGetValue(__instance.myPlayer.PlayerId, out var count4) && count4 < 1)
        )
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, -1);
            writer.WritePacked(127);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            _ = new LateTask(() =>
            {
                int clientId = __instance.myPlayer.GetClientId();
                MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, clientId);
                writer2.Write(id);
                AmongUsClient.Instance.FinishRpcImmediately(writer2);
            }, 0.5f, "Fix DesyncImpostor Stuck");
            return false;
        }

        __instance.myPlayer.GetRoleClass()?.OnCoEnterVent(__instance, id);

        if (__instance.myPlayer.Is(CustomRoles.Swooper))
            Swooper.OnCoEnterVent(__instance, id);

        if (__instance.myPlayer.Is(CustomRoles.Wraith))
            Wraith.OnCoEnterVent(__instance, id);
        
        if (__instance.myPlayer.Is(CustomRoles.Alchemist) && Alchemist.PotionID == 8)
            Alchemist.OnCoEnterVent(__instance, id);

        if (__instance.myPlayer.Is(CustomRoles.DovesOfNeace)) __instance.myPlayer.Notify(GetString("DovesOfNeaceMaxUsage"));
        if (__instance.myPlayer.Is(CustomRoles.Veteran)) __instance.myPlayer.Notify(GetString("VeteranMaxUsage"));

        return true;
    }
}

/*[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetName))]
class SetNamePatch
{
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] string name)
    {
    }
}*/
[HarmonyPatch(typeof(GameData), nameof(GameData.CompleteTask))]
class GameDataCompleteTaskPatch
{
    public static void Postfix(PlayerControl pc)
    {
        if (GameStates.IsHideNSeek) return;

        Logger.Info($"Task Complete: {pc.GetNameWithRole().RemoveHtmlTags()}", "CompleteTask");
        Main.PlayerStates[pc.PlayerId].UpdateTask(pc);
        Utils.NotifyRoles(SpecifySeer: pc, ForceLoop: true);
        Utils.NotifyRoles(SpecifyTarget: pc, ForceLoop: true);
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
class PlayerControlCompleteTaskPatch
{
    public static bool Prefix(PlayerControl __instance)
    {
        if (GameStates.IsHideNSeek) return false;

        var player = __instance;

        if (Workhorse.OnAddTask(player))
            return false;

        return true;
    }
    public static void Postfix(PlayerControl __instance, object[] __args)
    {
        if (GameStates.IsHideNSeek) return;

        var pc = __instance;
        Snitch.OnCompleteTask(pc);
        if (pc != null && __args != null && __args.Length > 0)
        {
            int taskIndex = Convert.ToInt32(__args[0]);

            var playerTask = pc.myTasks[taskIndex];
            Benefactor.OnTasKComplete(pc, playerTask);
            Taskinator.OnTasKComplete(pc, playerTask);
        }
        var isTaskFinish = pc.GetPlayerTaskState().IsTaskFinished;
        if (isTaskFinish && pc.Is(CustomRoles.Snitch) && pc.Is(CustomRoles.Madmate))
        {
            foreach (var impostor in Main.AllAlivePlayerControls.Where(pc => pc.Is(CustomRoleTypes.Impostor)).ToArray())
            {
                NameColorManager.Add(impostor.PlayerId, pc.PlayerId, "#ff1919");
            }
            Utils.NotifyRoles(SpecifySeer: pc, ForceLoop: true);
        }
        if ((isTaskFinish &&
            pc.GetCustomRole() is CustomRoles.Doctor or CustomRoles.Sunnyboy) ||
            pc.GetCustomRole() is CustomRoles.SpeedBooster)
        {
            //ライターもしくはスピードブースターもしくはドクターがいる試合のみタスク終了時にCustomSyncAllSettingsを実行する
            Utils.MarkEveryoneDirtySettings();
        }
        if (pc.Is(CustomRoles.Solsticer))
        {
            Solsticer.OnCompleteTask(pc);
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ProtectPlayer))]
class PlayerControlProtectPlayerPatch
{
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()} => {target.GetNameWithRole().RemoveHtmlTags()}", "ProtectPlayer");
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RemoveProtection))]
class PlayerControlRemoveProtectionPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()}", "RemoveProtection");
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MixUpOutfit))]
public static class PlayerControlMixupOutfitPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        if (!__instance.IsAlive())
        {
            return;
        }

        // if player is Desync Impostor and the vanilla sees player as Imposter, the vanilla process does not hide your name, so the other person's name is hidden
        if (PlayerControl.LocalPlayer.Data.Role.IsImpostor &&  // Impostor with vanilla
            !PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor) &&  // Not an Impostor
            Main.ResetCamPlayerList.Contains(PlayerControl.LocalPlayer.PlayerId))  // Desync Impostor
        {
            // Hide names
            __instance.cosmetics.ToggleNameVisible(false);
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckSporeTrigger))]
public static class PlayerControlCheckSporeTriggerPatch
{
    public static bool Prefix()
    {
        if (AmongUsClient.Instance.AmHost)
        {
            return !Options.DisableSporeTriggerOnFungle.GetBool();
        }

        return true;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckUseZipline))]
public static class PlayerControlCheckUseZiplinePatch
{
    public static bool Prefix([HarmonyArgument(2)] bool fromTop)
    {
        if (AmongUsClient.Instance.AmHost && Options.DisableZiplineOnFungle.GetBool())
        {
            if (Options.DisableZiplineFromTop.GetBool() && fromTop) return false;
            if (Options.DisableZiplineFromUnder.GetBool() && !fromTop) return false;
        }

        return true;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
public static class PlayerControlDiePatch
{
    public static void Postfix(PlayerControl __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        __instance.RpcRemovePet();
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
class PlayerControlSetRolePatch
{
    public static bool Prefix(PlayerControl __instance, ref RoleTypes roleType)
    {
        if (GameStates.IsHideNSeek) return true;

        var target = __instance;
        var targetName = __instance.GetNameWithRole().RemoveHtmlTags();
        Logger.Info($" {targetName} => {roleType}", "PlayerControl.RpcSetRole");
        if (!ShipStatus.Instance.enabled) return true;
        if (roleType is RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost)
        {
            var targetIsKiller = target.Is(CustomRoleTypes.Impostor) || Main.ResetCamPlayerList.Contains(target.PlayerId);
            var ghostRoles = new Dictionary<PlayerControl, RoleTypes>();

            foreach (var seer in Main.AllPlayerControls)
            {
                var self = seer.PlayerId == target.PlayerId;
                var seerIsKiller = seer.Is(CustomRoleTypes.Impostor) || Main.ResetCamPlayerList.Contains(seer.PlayerId);

                if (target.IsAnySubRole(x => x.IsGhostRole()) || target.GetCustomRole().IsGhostRole())
                {
                    ghostRoles[seer] = RoleTypes.GuardianAngel;
                }
                else if ((self && targetIsKiller) || (!seerIsKiller && target.Is(CustomRoleTypes.Impostor)))
                {
                    ghostRoles[seer] = RoleTypes.ImpostorGhost;
                }
                else
                {
                    ghostRoles[seer] = RoleTypes.CrewmateGhost;
                }
            }
            if (target.IsAnySubRole(x => x.IsGhostRole()) || target.GetCustomRole().IsGhostRole())
            {
                roleType = RoleTypes.GuardianAngel;
            }
            else if (ghostRoles.All(kvp => kvp.Value == RoleTypes.CrewmateGhost))
            {
                roleType = RoleTypes.CrewmateGhost;
            }
            else if (ghostRoles.All(kvp => kvp.Value == RoleTypes.ImpostorGhost))
            {
                roleType = RoleTypes.ImpostorGhost;
            }
            else
            {
                foreach ((var seer, var role) in ghostRoles)
                {
                    Logger.Info($"Desync {targetName} => {role} for {seer.GetNameWithRole().RemoveHtmlTags()}", "PlayerControl.RpcSetRole");
                    target.RpcSetRoleDesync(role, seer.GetClientId());
                }
                return false;
            }
        }
        return true;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetRole))]
class PlayerControlLocalSetRolePatch
{
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes role)
    {
        if (!AmongUsClient.Instance.AmHost && GameStates.IsNormalGame && !GameStates.IsModHost)
        {
            var modRole = role switch
            {
                RoleTypes.Impostor => CustomRoles.ImpostorTOHE,
                RoleTypes.Shapeshifter => CustomRoles.ShapeshifterTOHE,
                RoleTypes.Crewmate => CustomRoles.CrewmateTOHE,
                RoleTypes.Engineer => CustomRoles.EngineerTOHE,
                RoleTypes.Scientist => CustomRoles.ScientistTOHE,
                _ => CustomRoles.NotAssigned,
            };
            if (modRole != CustomRoles.NotAssigned)
            {
                Main.PlayerStates[__instance.PlayerId].SetMainRole(modRole);
            }
        }
    }
}