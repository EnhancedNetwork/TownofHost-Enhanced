using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine;
using TOHE.Modules;
using TOHE.Patches;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.Core.AssignManager;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using TOHE.Roles.Coven;
using TOHE.Roles.Core;
using static TOHE.Translator;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace TOHE;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckProtect))]
class CheckProtectPatch
{
    public static bool Prefix(PlayerControl __instance, PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost || GameStates.IsHideNSeek) return false;
        Logger.Info($"{ __instance.GetNameWithRole()} => {target.GetNameWithRole()}", "CheckProtect");
        var angel = __instance;

        if (AntiBlackout.SkipTasks)
        {
            Logger.Info("Checking while AntiBlackOut protect, guard protect was canceled", "CheckProtect");
            return false;
        }

        if (!angel.GetRoleClass().OnCheckProtect(angel, target))
            return false;

        if (angel.Is(CustomRoles.EvilSpirit))
        {
            if (target.GetRoleClass() is Spiritcaller sp)
            {
                sp.ProtectSpiritcaller();
            }
            else
            {
                Spiritcaller.HauntPlayer(target);
            }
            angel.RpcResetAbilityCooldown();
            return false;
        }

        angel.RpcSpecificProtectPlayer(target, angel.Data.DefaultOutfit.ColorId);
        return false;
    }

    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        var angel = __instance;
        Utils.NotifyRoles(SpecifySeer: angel);
        Utils.NotifyRoles(SpecifySeer: target);
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
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
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, bool __state = false)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (GameStates.IsHideNSeek) return true;

        var killer = __instance;

        Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} => {target.GetNameWithRole().RemoveHtmlTags()}", "CheckMurder");

        if (CheckForInvalidMurdering(killer, target, true) == false)
        {
            return false;
        }

        killer.ResetKillCooldown();
        Logger.Info($"Kill Cooldown Resets", "CheckMurder");

        // Replacement process when the actual killer and the KILLER are different
        if (Sniper.SnipeIsActive(__instance.PlayerId))
        {
            Logger.Info($"Killer is Sniper", "CheckMurder");

            Sniper.TryGetSniper(target.PlayerId, ref killer);

            Logger.Info($"After Try Get Sniper", "CheckMurder");

            if (killer.PlayerId != __instance.PlayerId)
            {
                Logger.Info($"Real Killer = {killer.GetNameWithRole().RemoveHtmlTags()}", "Sniper.CheckMurder");
            }
        }

        Logger.Info($"Start: CustomRoleManager.OnCheckMurder", "CheckMurder");

        if (CustomRoleManager.OnCheckMurder(ref killer, ref target, ref __state) == false)
        {
            Logger.Info($"Canceled from CustomRoleManager.OnCheckMurder", "CheckMurder");
            return false;
        }

        Logger.Info($"End: CustomRoleManager.OnCheckMurder", "CheckMurder");

        //== Kill target ==
        __instance.RpcMurderPlayer(target);
        //============

        return false;
    }
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, bool __state)
    {
        if (__state)
        {
            Utils.NotifyRoles(SpecifySeer: __instance);
            Utils.NotifyRoles(SpecifySeer: target);
        }
    }
    public static bool CheckForInvalidMurdering(PlayerControl killer, PlayerControl target, bool checkCanUseKillButton = false)
    {
        // Killer is already dead
        if (!killer.IsAlive())
        {
            Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} was cancelled because it is dead", "CheckMurder");
            return false;
        }

        // Is the target in a killable state?
        if (target.Data == null // Check if PlayerData is null
            || target.inVent
            || target.onLadder
            || target.inMovingPlat // Moving Platform on Airhip and Zipline on Fungle
            || target.MyPhysics.Animations.IsPlayingEnterVentAnimation()
            || target.MyPhysics.Animations.IsPlayingAnyLadderAnimation()
        )
        {
            Logger.Info("The target is in an unkillable state and the kill is canceled", "CheckMurder");
            return false;
        }
        // Target Is Dead
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
        // AntiBlackOut protect is active
        if (AntiBlackout.SkipTasks)
        {
            Logger.Info("Checking while AntiBlackOut protect, the kill was canceled", "CheckMurder");
            return false;
        }

        var divice = Options.CurrentGameMode == CustomGameMode.FFA ? 3000f : 1500f;
        float minTime = Mathf.Max(0.02f, AmongUsClient.Instance.Ping / divice * 6f); //Ping value is milliseconds (ms), so ÷ 2000
        // No value is stored in TimeSinceLastKill || Stored time is greater than or equal to minTime => Allow kill

        //↓ If not permitted
        if (TimeSinceLastKill.TryGetValue(killer.PlayerId, out var time) && time < minTime)
        {
            Logger.Info($"Last kill was too shortly before, canceled - Ping: {AmongUsClient.Instance.Ping}, Time: {time}, MinTime: {minTime}", "CheckMurder");
            return false;
        }
        TimeSinceLastKill[killer.PlayerId] = 0f;

        // killable decision
        if (killer.PlayerId != target.PlayerId && !killer.CanUseKillButton() && checkCanUseKillButton)
        {
            Logger.Info(killer.GetNameWithRole().RemoveHtmlTags() + " The hitter is not allowed to use the kill button and the kill is canceled", "CheckMurder");
            return false;
        }

        //FFA
        if (Options.CurrentGameMode == CustomGameMode.FFA)
        {
            FFAManager.OnPlayerAttack(killer, target);
            return false;
        }

        // if player hacked by Glitch
        if (Glitch.HasEnabled && !Glitch.OnCheckMurderOthers(killer, target))
        {
            Logger.Info($"Is hacked by Glitch, it cannot kill ", "Glitch.CheckMurder");
            return false;
        }

        //Is eaten player can't be killed.
        if (Pelican.IsEaten(target.PlayerId))
        {
            Logger.Info("Is eaten player can't be killed", "Pelican.CheckMurder");
            return false;
        }

        // Penguin's victim unable to kill
        List<Penguin> penguins = Utils.GetRoleBasesByType<Penguin>()?.ToList();
        if (Penguin.HasEnabled && penguins != null)
        {
            if (penguins.Any(x => killer.PlayerId == x?.AbductVictim?.PlayerId))
            {
                killer.Notify(GetString("PenguinTargetOnCheckMurder"));
                killer.SetKillCooldown(5);
                return false;
            }
        }

        return true;
    }

    public static bool RpcCheckAndMurder(PlayerControl killer, PlayerControl target, bool check = false)
    {
        if (!AmongUsClient.Instance.AmHost) return false;

        Logger.Info($"check: {check}", "RpcCheckAndMurder");

        if (target == null) target = killer;
        if (killer == null)
        {
            Logger.Info($"Killer: {killer == null} or Target: {target == null} is null", "RpcCheckAndMurder");
            return false;
        }

        var killerRole = killer.GetCustomRole();

        var targetRoleClass = target.GetRoleClass();
        var targetSubRoles = target.GetCustomSubRoles();

        Logger.Info($"Start", "FirstDied.CheckMurder");

        if (target.GetClient().GetHashedPuid() == Main.FirstDiedPrevious && MeetingStates.FirstMeeting)
        {
            killer.SetKillCooldown(5f);
            killer.RpcGuardAndKill(target);
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(killer.GetCustomRole()), GetString("PlayerIsShieldedByGame")));
            Logger.Info($"Canceled from ShieldPersonDiedFirst", "FirstDied");
            return false;
        }

        // Madmate Spawn Mode Is First Kill
        if (Madmate.MadmateSpawnMode.GetInt() == 1 && Main.MadmateNum < CustomRoles.Madmate.GetCount() && target.CanBeMadmate())
        {
            Main.MadmateNum++;
            target.RpcSetCustomRole(CustomRoles.Madmate);
            ExtendedPlayerControl.RpcSetCustomRole(target.PlayerId, CustomRoles.Madmate);
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("BecomeMadmateCuzMadmateMode")));
            killer.SetKillCooldown();
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);
            Logger.Info($"Assign by first try kill: {target?.Data?.PlayerName} = {target.GetCustomRole()} + {CustomRoles.Madmate}", "Madmate");
            return false;
        }

        // Impostors can kill Madmate
        if (killer.Is(Custom_Team.Impostor) && !Madmate.ImpCanKillMadmate.GetBool() && target.Is(CustomRoles.Madmate))
            return false;

        // Coven CAN'T kill Coven/Enchanted
        // if ((killer.Is(Custom_Team.Coven) || killer.Is(CustomRoles.Enchanted)) && (target.Is(Custom_Team.Coven) || target.Is(CustomRoles.Enchanted))) return false;

        Logger.Info($"Start", "OnCheckMurderAsTargetOnOthers");

        // Check murder on others targets
        if (CustomRoleManager.OnCheckMurderAsTargetOnOthers(killer, target) == false)
        {
            return false;
        }

        Logger.Info($"Start", "TargetSubRoles");

        if (targetSubRoles.Any())
            foreach (var targetSubRole in targetSubRoles.ToArray())
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

                    //case CustomRoles.Fragile:
                    //    if (Fragile.KillFragile(killer, target))
                    //        return false;
                    //    break;

                    case CustomRoles.Aware:
                        Aware.OnCheckMurder(killerRole, target);
                        break;

                    case CustomRoles.Lucky:
                        if (!Lucky.OnCheckMurder(killer, target))
                            return false;
                        break;

                    case CustomRoles.Cyber when killer.PlayerId != target.PlayerId:
                        foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId).ToArray())
                        {
                            if (target.Is(CustomRoles.Cyber))
                            {
                                if (Main.AllAlivePlayerControls.Any(x =>
                                    x.PlayerId != killer.PlayerId &&
                                    x.PlayerId != target.PlayerId &&
                                    Utils.GetDistance(x.transform.position, target.transform.position) < 2f))
                                    return false;
                            }
                        }
                        break;
                }
            }

        Logger.Info($"Start", "OnCheckMurderAsTarget");

        // Check Murder as target
        if (targetRoleClass.OnCheckMurderAsTarget(killer, target) == false)
        {
            Logger.Info("Cancels because for target need cancel kill", "OnCheckMurderAsTarget");
            return false;
        }

        if (!check) killer.RpcMurderPlayer(target);
        return true;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
class MurderPlayerPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] MurderResultFlags resultFlags, ref bool __state)
    {
        Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()} => {target.GetNameWithRole().RemoveHtmlTags()}{(target.IsProtected() ? "(Protected)" : "")}, flags : {resultFlags}", "MurderPlayer Prefix");

        if (GameStates.IsLobby)
        {
            Logger.Info("Murder triggered in lobby, so murder canceled", "MurderPlayer Prefix");
            return false;
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

        if (isSucceeded && AmongUsClient.Instance.AmHost && GameStates.IsNormalGame)
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

            if (!target.IsProtected() && !Main.OvverideOutfit.ContainsKey(target.PlayerId) && !Camouflage.ResetSkinAfterDeathPlayers.Contains(target.PlayerId))
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

        if (killer != __instance)
        {
            Logger.Info($"Real Killer => {killer.GetNameWithRole().RemoveHtmlTags()}", "MurderPlayer");

        }
        if (Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.etc)
        {
            target.SetDeathReason(PlayerState.DeathReason.Kill);
        }

        Main.MurderedThisRound.Add(target.PlayerId);

        // Check Youtuber first died
        if (Main.FirstDied == "" && target.Is(CustomRoles.Youtuber) && !killer.Is(CustomRoles.KillingMachine))
        {
            Youtuber.OnMurderPlayer(killer, target);
            return;
            //Imagine youtuber is converted
        }

        if (Main.FirstDied == "")
        {
            Main.FirstDied = target.GetClient().GetHashedPuid();

            if (Options.RemoveShieldOnFirstDead.GetBool() && Main.FirstDiedPrevious != "")
            {
                Main.FirstDiedPrevious = "";
                RPC.SyncAllPlayerNames();
            }

            // Sync protected player from being killed first info for modded clients
            if (PlayerControl.LocalPlayer.IsHost())
            {
                var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncShieldPersonDiedFirst, SendOption.None, -1);
                writer.Write(Main.FirstDied);
                writer.Write(Main.FirstDiedPrevious);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }

        if (Main.AllKillers.ContainsKey(killer.PlayerId))
            Main.AllKillers.Remove(killer.PlayerId);

        killer.SetKillTimer();

        if (!killer.Is(CustomRoles.Trickster))
            Main.AllKillers.Add(killer.PlayerId, Utils.GetTimeStamp());

        Main.PlayerStates[target.PlayerId].SetDead();
        target.SetRealKiller(killer, true);
        Utils.CountAlivePlayers(sendLog: true, checkGameEnd: false);

        // When target death, activate ability for others roles
        AfterPlayerDeathTasks(killer, target, false);

        // Check Kill Flash
        Utils.TargetDies(__instance, target);

        Utils.CountAlivePlayers(checkGameEnd: true);

        if (Options.LowLoadMode.GetBool())
        {
            __instance.MarkDirtySettings();
            target.MarkDirtySettings();
        }
        else
        {
            Utils.SyncAllSettings();
        }

        Utils.NotifyRoles(SpecifySeer: killer);
        Utils.NotifyRoles(SpecifySeer: target);
    }
    public static void AfterPlayerDeathTasks(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        CustomRoleManager.OnMurderPlayer(killer, target, inMeeting);
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcMurderPlayer))]
class RpcMurderPlayerPatch
{
    public static bool Prefix(PlayerControl __instance, PlayerControl target, bool didSucceed)
    {
        if (!AmongUsClient.Instance.AmHost)
            Logger.Error("Client is calling RpcMurderPlayer, are you Hacking?", "RpcMurderPlayerPatch.Prefix");

        if (GameStates.IsLobby)
        {
            Logger.Info("Murder triggered in lobby, so murder canceled", "RpcMurderPlayer.Prefix");
            return false;
        }

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
public static class CheckShapeshiftPatch
{
    private static readonly LogHandler logger = Logger.Handler(nameof(PlayerControl.CheckShapeshift));

    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] bool shouldAnimate)
    {
        if (AmongUsClient.Instance.IsGameOver || !AmongUsClient.Instance.AmHost)
        {
            return false;
        }

        // No called code if is invalid shapeshifting
        if (!CheckInvalidShapeshifting(__instance, target, shouldAnimate))
        {
            __instance.RpcRejectShapeshift();
            return false;
        }

        var shapeshifter = __instance;
        bool resetCooldown = true;

        logger.Info($"Self:{shapeshifter.PlayerId == target.PlayerId} - Is animate:{shouldAnimate} - In Meeting:{GameStates.IsMeeting}");

        var shapeshifterRoleClass = shapeshifter.GetRoleClass();
        if (shapeshifterRoleClass?.OnCheckShapeshift(shapeshifter, target, ref resetCooldown, ref shouldAnimate) == false)
        {
            // role need specific reject shapeshift if player use desync shapeshift
            if (shapeshifterRoleClass.CanDesyncShapeshift)
            {
                shapeshifter.RpcSpecificRejectShapeshift(target, shouldAnimate);

                if (resetCooldown)
                    shapeshifter.RpcResetAbilityCooldown();
            }
            else
            {
                // Global reject shapeshift
                shapeshifter.RejectShapeshiftAndReset(resetCooldown);
            }
            return false;
        }

        shapeshifter.RpcShapeshift(target, shouldAnimate);
        return false;
    }
    private static bool CheckInvalidShapeshifting(PlayerControl instance, PlayerControl target, bool animate)
    {
        logger.Info($"Checking shapeshift {instance.GetNameWithRole()} -> {(target == null || target.Data == null ? "(null)" : target.GetNameWithRole().RemoveHtmlTags())}");

        if (!target || target.Data == null)
        {
            logger.Info("Cancel shapeshifting because target is null");
            return false;
        }
        if (!instance.IsAlive())
        {
            logger.Info("Shapeshifting canceled because shapeshifter is dead");
            return false;
        }
        //if (!instance.Is(CustomRoles.Glitch) && instance.Data.Role.Role != RoleTypes.Shapeshifter && instance.GetCustomRole().GetVNRole() != CustomRoles.Shapeshifter)
        //{
        //    logger.Info("Shapeshifting canceled because the shapeshifter is not a shapeshifter");
        //    return false;
        //}
        if (instance.Data.Disconnected)
        {
            logger.Info("Shapeshifting canceled because shapeshifter is disconnected");
            return false;
        }
        if (target.IsMushroomMixupActive() && animate)
        {
            logger.Info("Shapeshifting canceled because mushroom mixup is active");
            return false;
        }
        if (MeetingHud.Instance && animate)
        {
            logger.Info("Cancel shapeshifting in meeting");
            return false;
        }
        if (AntiBlackout.SkipTasks)
        {
            Logger.Info("Checking while AntiBlackOut protect, shapeshift was canceled", "CheckShapeshift");
            return false;
        }
        if (!(instance.Is(CustomRoles.ShapeshifterTOHE) || instance.Is(CustomRoles.Shapeshifter)) && target.GetClient().GetHashedPuid() == Main.FirstDiedPrevious && MeetingStates.FirstMeeting)
        {
            instance.RpcGuardAndKill(instance);
            instance.Notify(Utils.ColorString(Utils.GetRoleColor(instance.GetCustomRole()), GetString("PlayerIsShieldedByGame")));
            logger.Info($"Cancel shapeshifting because {target.GetRealName()} is protected by the game");
            return false;
        }
        if (Pelican.IsEaten(instance.PlayerId))
        {
            logger.Info($"Cancel shapeshifting because {instance.GetRealName()} is eaten by Pelican");
            return false;
        }

        if (instance == target && Main.UnShapeShifter.Contains(instance.PlayerId))
        {
            if (!instance.IsMushroomMixupActive() && !GameStates.IsMeeting) instance.GetRoleClass().UnShapeShiftButton(instance);
            instance.RpcResetAbilityCooldown(); // Just incase
            logger.Info($"Cancel shapeshifting because {instance.GetRealName()} is using un-shapeshift ability button");
            return false;
        }
        return true;
    }
    public static void RejectShapeshiftAndReset(this PlayerControl player, bool reset = true)
    {
        player.RpcRejectShapeshift();
        if (reset) player.RpcResetAbilityCooldown();
        Logger.Info($"Rejected {player.GetRealName()} shapeshift & " + (reset ? "Reset cooldown" : "Not Reset cooldown"), "RejectShapeshiftAndReset");
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
class ShapeshiftPatch
{
    public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] bool animate)
    {
        Logger.Info($"{__instance?.GetNameWithRole().RemoveHtmlTags()} => {target?.GetNameWithRole().RemoveHtmlTags()}", "ShapeshiftPatch");

        var shapeshifter = __instance;
        var shapeshifting = shapeshifter.PlayerId != target.PlayerId;

        if (Main.CheckShapeshift.TryGetValue(shapeshifter.PlayerId, out var last) && last == shapeshifting)
        {
            Logger.Info($"{__instance?.GetNameWithRole().RemoveHtmlTags()} : Cancel Shapeshift.Prefix", "ShapeshiftPatch");
            return;
        }

        Main.CheckShapeshift[shapeshifter.PlayerId] = shapeshifting;
        Main.ShapeshiftTarget[shapeshifter.PlayerId] = target.PlayerId;

        if (!AmongUsClient.Instance.AmHost) return;
        if (GameStates.IsHideNSeek) return;
        if (!shapeshifting) Camouflage.RpcSetSkin(__instance);

        shapeshifter.GetRoleClass()?.OnShapeshift(shapeshifter, target, animate, shapeshifting);

        if (!shapeshifter.Is(CustomRoles.Glitch) && !Main.MeetingIsStarted)
        {
            var time = animate ? 1.2f : 0.5f;
            //Forced update players name
            if (shapeshifting)
            {
                _ = new LateTask(() =>
                {
                    Utils.NotifyRoles(SpecifyTarget: shapeshifter, NoCache: true);
                },
                time, "ShapeShiftNotify");
            }
            else if (!shapeshifting)
            {
                _ = new LateTask(() =>
                {
                    Utils.NotifyRoles(NoCache: true);
                },
                time, "UnShiftNotify");
            }
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
class ReportDeadBodyPatch
{
    public static Dictionary<byte, bool> CanReport = [];
    public static Dictionary<byte, List<NetworkedPlayerInfo>> WaitReport = [];
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo target)
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

        Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()} => {target?.PlayerName ?? "null (Button Pressed)"}", "ReportDeadBody");

        foreach (var kvp in Main.PlayerStates)
        {
            var pc = Utils.GetPlayerById(kvp.Key);
            kvp.Value.LastRoom = pc.GetPlainShipRoom();
        }

        if (!AmongUsClient.Instance.AmHost) return true;

        try
        {
            // If the player is dead, the meeting is canceled
            if (__instance.Data.IsDead) return false;

            //=============================================
            //Below, check if this meeting is allowed
            //=============================================

            var killer = target?.Object?.GetRealKiller();
            var killerRole = killer?.GetCustomRole();

            if (target == null) //Meeting
            {
                var playerRoleClass = __instance.GetRoleClass();

                if (playerRoleClass.OnCheckStartMeeting(__instance) == false)
                {
                    Logger.Info($"Player has role class: {playerRoleClass} - the start of the meeting has been cancelled", "ReportDeadBody");
                    return false;
                }
            }
            if (target != null) // Report dead body
            {
                // Guessed player cannot report
                if (Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.Gambled) return false;

                // Check report bead body
                foreach (var player in Main.PlayerStates.Values.ToArray())
                {
                    var playerRoleClass = player.RoleClass;
                    if (player == null || playerRoleClass == null) continue;

                    if (playerRoleClass.OnCheckReportDeadBody(__instance, target, killer) == false)
                    {
                        Logger.Info($"Player has role class: {playerRoleClass} - is canceled the report", "ReportDeadBody");
                        return false;
                    }
                }

                // if Bait is killed, check the setting condition
                if (!(target.Object.Is(CustomRoles.Bait) && Bait.BaitCanBeReportedUnderAllConditions.GetBool()))
                {
                    // Comms Camouflage
                    if (Options.DisableReportWhenCC.GetBool() && Utils.IsActive(SystemTypes.Comms) && Camouflage.IsActive) return false;
                }

                //Check unreportable bodies
                if (Main.UnreportableBodies.Contains(target.PlayerId))
                {
                    __instance.Notify(Utils.ColorString(__instance.GetRoleColor(), GetString("BodyCannotBeReported")));
                    return false;
                }

                if (target.Object.Is(CustomRoles.Unreportable)) return false;


                // Oblivious try report body
                var tpc = Utils.GetPlayerById(target.PlayerId);
                if (__instance.Is(CustomRoles.Oblivious))
                {
                    if (!tpc.Is(CustomRoles.Bait) || (tpc.Is(CustomRoles.Bait) && Oblivious.ObliviousBaitImmune.GetBool())) /* && (target?.Object != null)*/
                    {
                        return false;
                    }
                }

                var tar = Utils.GetPlayerById(target.PlayerId);

                if (__instance.Is(CustomRoles.Unlucky) && (target?.Object == null || !target.Object.Is(CustomRoles.Bait)))
                {
                    if (Unlucky.SuicideRand(__instance, Unlucky.StateSuicide.ReportDeadBody))
                        return false;

                }
            }

            if (Options.SyncButtonMode.GetBool() && target == null)
            {
                Logger.Info($"Option: {Options.SyncedButtonCount.GetInt()}, has button count: {Options.UsedButtonCount}", "ReportDeadBody");
                if (Options.SyncedButtonCount.GetFloat() <= Options.UsedButtonCount)
                {
                    Logger.Info("The button has been canceled because the maximum number of available buttons has been exceeded", "ReportDeadBody");
                    return false;
                }
                else Options.UsedButtonCount++;

                if (Options.SyncedButtonCount.GetFloat() == Options.UsedButtonCount)
                {
                    Logger.Info("The maximum number of meeting buttons has been reached", "ReportDeadBody");
                }
            }
        }
        catch (Exception e)
        {
            Utils.ThrowException(e);
        }

        AfterReportTasks(__instance, target);

        // InnerSloth added CheckTaskCompletion() => CheckEndGameViaTasks() in report dead body.
        // This is patched in CheckGameEndPatch
        return true;
    }
    public static void AfterReportTasks(PlayerControl player, NetworkedPlayerInfo target, bool force = false)
    {
        //=============================================
        // Hereinafter, it is assumed that the button is confirmed to be pressed
        //=============================================

        try
        {
            Main.MeetingIsStarted = true;
            Main.LastVotedPlayerInfo = null;
            Main.AllKillers.Clear();
            GuessManager.GuesserGuessed.Clear();
            Main.MurderedThisRound.Clear();

            Logger.Info($"target is null? - {target == null}", "AfterReportTasks");
            Logger.Info($"target.Object is null? - {target?.Object == null}", "AfterReportTasks");
            Logger.Info($"target.PlayerId is - {target?.PlayerId}", "AfterReportTasks");

            foreach (var playerStates in Main.PlayerStates.Values.ToArray())
            {
                try
                {
                    playerStates.RoleClass?.OnReportDeadBody(player, target);
                }
                catch (Exception error)
                {
                    Utils.ThrowException(error);
                    Logger.Error($"Role Class Error: {error}", "RoleClass_OnReportDeadBody");
                    Logger.SendInGame($"Error: {error}");
                }
            }
            Rebirth.OnReportDeadBody();

            // Alchemist & Bloodlust
            Alchemist.OnReportDeadBodyGlobal();

            if (Aware.IsEnable) Aware.OnReportDeadBody();

            Sleuth.OnReportDeadBody(player, target);
            Evader.ReportDeadBody();
        }
        catch (Exception error)
        {
            Utils.ThrowException(error);
            Logger.Error($"Error: {error}", "AfterReportTasks");
            Logger.SendInGame($"Error: {error}");
        }

        foreach (var pc in Main.AllPlayerControls)
        {
            if (!Main.OvverideOutfit.ContainsKey(pc.PlayerId))
            {
                // Update skins again, since players have different skins
                // And can be easily distinguished from each other
                if (Camouflage.IsCamouflage && Options.KPDCamouflageMode.GetValue() is 2 or 3)
                {
                    Camouflage.RpcSetSkin(pc);
                }

                // Check shapeshift and revert skin to default
                if (Main.CheckShapeshift.ContainsKey(pc.PlayerId))
                {
                    Camouflage.RpcSetSkin(pc, RevertToDefault: true);
                }
            }

            if (GameStates.FungleIsActive && (pc.IsMushroomMixupActive() || Utils.IsActive(SystemTypes.MushroomMixupSabotage)))
            {
                pc.FixMixedUpOutfit();
            }

            PhantomRolePatch.OnReportDeadBody(pc, force);

            Logger.Info($"Player {pc?.Data?.PlayerName}: Id {pc.PlayerId} - is alive: {pc.IsAlive()}", "CheckIsAlive");
        }

        // Set meeting time
        MeetingTimeManager.OnReportDeadBody();

        // Clear all Notice players
        NameNotifyManager.Reset();

        // Update Notify Roles for Meeting
        Utils.DoNotifyRoles(isForMeeting: true, NoCache: true, CamouflageIsForMeeting: true);

        // Sync all settings on meeting start
        _ = new LateTask(Utils.SyncAllSettings, 3f, "Sync all settings after report");
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
class FixedUpdateInNormalGamePatch
{
    private static readonly StringBuilder Mark = new(20);
    private static readonly StringBuilder Suffix = new(120);
    private static readonly Dictionary<byte, int> BufferTime = [];
    private static int LevelKickBufferTime = 20;

    public static async void Postfix(PlayerControl __instance)
    {
        if (GameStates.IsHideNSeek) return;
        if (!GameStates.IsModHost) return;
        if (__instance == null) return;

        byte id = __instance.PlayerId;
        if (AmongUsClient.Instance.AmHost && GameStates.IsInTask && ReportDeadBodyPatch.CanReport[id] && ReportDeadBodyPatch.WaitReport[id].Any())
        {
            if (Glitch.HasEnabled && Glitch.OnCheckFixedUpdateReport(id))
            {
                Glitch.CancelReportInFixedUpdate(__instance, id);
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
            Utils.ThrowException(ex);
            Logger.Error($"Error for {__instance.GetNameWithRole().RemoveHtmlTags()}: Error: {ex}", "FixedUpdateInNormalGamePatch");
        }
    }

    public static Task DoPostfix(PlayerControl __instance)
    {
        // FixedUpdate is called 30 times every 1 second
        // If count only one player
        // For example: 15 players will called 450 times every 1 second

        var player = __instance;

        // The code is called once every 1 second (by one player)
        bool lowLoad = false;
        if (Options.LowLoadMode.GetBool())
        {
            if (!BufferTime.TryGetValue(player.PlayerId, out var timerLowLoad))
            {
                BufferTime[player.PlayerId] = 30;
                timerLowLoad = 30;
            }

            timerLowLoad--;

            if (timerLowLoad > 0)
            {
                lowLoad = true;
            }
            else
            {
                timerLowLoad = 30;
            }

            BufferTime[player.PlayerId] = timerLowLoad;
        }

        if (!lowLoad)
        {
            Zoom.OnFixedUpdate();

            //try
            //{
            //    // ChatUpdatePatch doesn't work when host chat is hidden
            //    if (AmongUsClient.Instance.AmHost && player.AmOwner && !DestroyableSingleton<HudManager>.Instance.Chat.isActiveAndEnabled)
            //    {
            //        ChatUpdatePatch.Postfix(ChatUpdatePatch.Instance);
            //    }
            //}
            //catch (Exception er)
            //{
            //    Logger.Error($"Error: {er}", "ChatUpdatePatch");
            //}
        }

        // Only during the game
        if (GameStates.IsInGame)
        {
            Sniper.OnFixedUpdateGlobal(player);


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

                if (KickPlayerPatch.AttemptedKickPlayerList.Any())
                {
                    foreach (var item in KickPlayerPatch.AttemptedKickPlayerList)
                    {
                        KickPlayerPatch.AttemptedKickPlayerList[item.Key]++;

                        if (item.Value > 11)
                            KickPlayerPatch.AttemptedKickPlayerList.Remove(item.Key);
                    }
                }
            }

            DoubleTrigger.OnFixedUpdate(player);
            KillTimerManager.FixedUpdate(player);

            //Mini's count down needs to be done outside if intask if we are counting meeting time
            if (GameStates.IsInGame && player.GetRoleClass() is Mini min)
            {
                if (!player.Data.IsDead)
                    min.OnFixedUpdates(player);
            }

            if (!GameStates.IsLobby && !GameStates.IsInTask && !GameStates.IsMeeting && player.Is(CustomRoles.Spurt) && !Mathf.Approximately(Main.AllPlayerSpeed[player.PlayerId], Spurt.StartingSpeed[player.PlayerId])) // fix ludicrous bug
            {
                Main.AllPlayerSpeed[player.PlayerId] = Spurt.StartingSpeed[player.PlayerId];
                player.MarkDirtySettings();
            }


            if (GameStates.IsInTask && !AntiBlackout.SkipTasks)
            {
                CustomRoleManager.OnFixedUpdate(player, lowLoad, Utils.GetTimeStamp());

                player.OnFixedAddonUpdate(lowLoad);

                if (Main.LateOutfits.TryGetValue(player.PlayerId, out var Method) && !player.CheckCamoflague())
                {
                    Method();
                    Main.LateOutfits.Remove(player.PlayerId);
                    Logger.Info($"Reset {player.GetRealName()}'s outfit", "LateOutfits..OnFixedUpdate");
                }

                if (!lowLoad)
                {
                    if (Options.LadderDeath.GetBool() && player.IsAlive())
                        FallFromLadder.FixedUpdate(player);

                    //Local Player only
                    if (player.AmOwner)
                    {
                        DisableDevice.FixedUpdate();

                        if (CustomRoles.Lovers.IsEnable())
                            LoversSuicide();

                        if (Rainbow.IsEnabled && Main.IntroDestroyed)
                            Rainbow.OnFixedUpdate();

                        if (Main.UnShapeShifter.Any(x => Utils.GetPlayerById(x) != null && Utils.GetPlayerById(x).CurrentOutfitType != PlayerOutfitType.Shapeshifted)
                            && !player.IsMushroomMixupActive() && Main.GameIsLoaded)
                        {
                            foreach (var UnShapeshifterId in Main.UnShapeShifter)
                            {
                                var UnShapeshifter = Utils.GetPlayerById(UnShapeshifterId);
                                if (UnShapeshifter == null)
                                {
                                    Main.UnShapeShifter.Remove(UnShapeshifterId);
                                    continue;
                                }
                                if (UnShapeshifter.CurrentOutfitType == PlayerOutfitType.Shapeshifted) continue;

                                var randomPlayer = Main.AllPlayerControls.FirstOrDefault(x => x != UnShapeshifter);
                                UnShapeshifter.RpcShapeshift(randomPlayer, false);
                                UnShapeshifter.RpcRejectShapeshift();
                                UnShapeshifter.ResetPlayerOutfit(setNamePlate: true);
                                Utils.NotifyRoles(SpecifyTarget: UnShapeshifter);
                                Logger.Info($"Revert to shapeshifting state for: {player.GetRealName()}", "UnShapeShifer_FixedUpdate");
                            }
                        }
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
                        if (pc.Is(CustomRoles.Vampire) || pc.Is(CustomRoles.Warlock) || pc.Is(CustomRoles.Ninja))
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
            if (!player.Is(Custom_Team.Impostor) && player.CanUseKillButton() && !player.Data.IsDead)
            {
                var players = __instance.GetPlayersInAbilityRangeSorted(false);
                PlayerControl closest = !players.Any() ? null : players[0];
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
                    if (Main.ForkId != ver.forkId)
                        __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>{ver.forkId}</size>\n{__instance?.name}</color>";
                    else if (Main.version.CompareTo(ver.version) == 0)
                        __instance.cosmetics.nameText.text = ver.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})" ? $"<color=#87cefa>{__instance.name}</color>" : $"<color=#ffff00><size=1.2>{ver.tag}</size>\n{__instance?.name}</color>";
                    else __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>v{ver.version}</size>\n{__instance?.name}</color>";
                }
                else if (Main.BAUPlayers.TryGetValue(__instance.Data, out var puid)) // Set name color for BAU users
                {
                    if (puid == __instance.Data.Puid)
                    {
                        __instance.cosmetics.nameText.text = $"<color=#0dff00>{__instance.name}</color>";
                    }
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

                if (!PlayerControl.LocalPlayer.Data.IsDead && Overseer.IsRevealedPlayer(PlayerControl.LocalPlayer, __instance) && __instance.Is(CustomRoles.Trickster))
                {
                    RoleText.text = Overseer.GetRandomRole(PlayerControl.LocalPlayer.PlayerId); // random role for revealed trickster
                    RoleText.text += TaskState.GetTaskState(); // random task count for revealed trickster
                }
                if (!PlayerControl.LocalPlayer.Data.IsDead && Overseer.IsRevealedPlayer(PlayerControl.LocalPlayer, __instance) && Illusionist.IsCovIllusioned(__instance.PlayerId))
                {
                    RoleText.text = Overseer.GetRandomRole(PlayerControl.LocalPlayer.PlayerId);
                    RoleText.text += TaskState.GetTaskState();
                }
                if (!PlayerControl.LocalPlayer.Data.IsDead && Overseer.IsRevealedPlayer(PlayerControl.LocalPlayer, __instance) && Illusionist.IsNonCovIllusioned(__instance.PlayerId))
                {
                    var randomRole = CustomRolesHelper.AllRoles.Where(role => role.IsEnable() && !role.IsAdditionRole() && role.IsCoven()).ToList().RandomElement();
                    RoleText.text = Utils.ColorString(Utils.GetRoleColor(randomRole), GetString(randomRole.ToString()));
                    if (randomRole is CustomRoles.CovenLeader or CustomRoles.Jinx or CustomRoles.Illusionist or CustomRoles.VoodooMaster) // Roles with Ability Uses
                    {
                        RoleText.text += randomRole.GetStaticRoleClass().GetProgressText(PlayerControl.LocalPlayer.PlayerId, false);
                    }
                }


                if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay)
                {
                    RoleText.enabled = false;
                    if (!__instance.AmOwner) __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName;
                }

                if (Main.VisibleTasksCount)
                    RoleText.text += Utils.GetProgressText(__instance);


                var seer = PlayerControl.LocalPlayer;
                var seerRoleClass = seer.GetRoleClass();
                var target = __instance;

                if (seer != target && seer != DollMaster.DollMasterTarget)
                    target = DollMaster.SwapPlayerInfo(target); // If a player is possessed by the Dollmaster swap each other's controllers.

                string RealName = target.GetRealName();

                Mark.Clear();
                Suffix.Clear();


                if (target.AmOwner && GameStates.IsInTask)
                {
                    if (Options.CurrentGameMode == CustomGameMode.FFA)
                        FFAManager.GetNameNotify(target, ref RealName);

                    if (Pelican.IsEaten(seer.PlayerId))
                        RealName = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Pelican), GetString("EatenByPelican"));

                    if (Deathpact.IsInActiveDeathpact(seer))
                        RealName = Deathpact.GetDeathpactString(seer);

                    if (NameNotifyManager.GetNameNotify(target, out var name))
                        RealName = name;
                }

                RealName = RealName.ApplyNameColorData(seer, target, false);
                var seerRole = seer.GetCustomRole();

                // Add protected player icon from ShieldPersonDiedFirst
                if (target.GetClient().GetHashedPuid() == Main.FirstDiedPrevious && MeetingStates.FirstMeeting)
                {
                    if (Options.ShowShieldedPlayerToAll.GetBool())
                    {
                        RealName = "<color=#4fa1ff><u></color>" + RealName + "</u>";
                        Mark.Append("<color=#4fa1ff>✚</color>");
                    }
                    else if (seer == target)
                    {
                        RealName = "<color=#4fa1ff><u></color>" + RealName + "</u>";
                        Mark.Append("<color=#4fa1ff>✚</color>");
                    }
                }

                Mark.Append(seerRoleClass?.GetMark(seer, target, false));
                Mark.Append(CustomRoleManager.GetMarkOthers(seer, target, false));

                Suffix.Append(CustomRoleManager.GetLowerTextOthers(seer, target, false, false));

                Suffix.Append(seerRoleClass?.GetSuffix(seer, target, false));
                Suffix.Append(CustomRoleManager.GetSuffixOthers(seer, target, false));

                Suffix.Append(Radar.GetPlayerArrow(seer, target, isForMeeting: false));

                if (seerRole.IsImpostor() && target.GetPlayerTaskState().IsTaskFinished)
                {
                    if (target.Is(CustomRoles.Snitch) && target.Is(CustomRoles.Madmate))
                        Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "★"));
                }

                if (target.Is(CustomRoles.Cyber) && Cyber.CyberKnown.GetBool())
                    Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cyber), "★"));

                if (target.Is(CustomRoles.Lovers) && seer.Is(CustomRoles.Lovers))
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♥</color>");
                }
                else if (target.Is(CustomRoles.Lovers) && seer.Data.IsDead)
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♥</color>");
                }


                if (Options.CurrentGameMode == CustomGameMode.FFA)
                    Suffix.Append(FFAManager.GetPlayerArrow(seer, target));

                /*if(main.AmDebugger.Value && main.BlockKilling.TryGetValue(target.PlayerId, out var isBlocked)) {
                    Mark = isBlocked ? "(true)" : "(false)";}*/

                // Devourer
                if (CustomRoles.Devourer.HasEnabled())
                {
                    bool targetDevoured = Devourer.HideNameOfTheDevoured(target.PlayerId);
                    if (targetDevoured)
                        RealName = GetString("DevouredName");
                }

                // Dollmaster, Prevent seeing self in mushroom cloud
                if (CustomRoles.DollMaster.HasEnabled() && seerRole != CustomRoles.DollMaster)
                {
                    if (DollMaster.IsDoll(seer.PlayerId))
                        RealName = "<size=10000%><color=#000000>■</color></size>";
                }

                // Camouflage
                if ((Camouflage.IsActive && Utils.IsActive(SystemTypes.Comms)) || Camouflager.AbilityActivated)
                    RealName = $"<size=0%>{RealName}</size> ";

                string DeathReason = seer.Data.IsDead && seer.KnowDeathReason(target)
                    ? $"\n<size=1.7>『{Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId))}』</size>" : string.Empty;

                // code from EHR (Endless Host Roles by: Gurge44)
                var currentText = target.cosmetics.nameText.text;
                var changeTo = $"{RealName}{DeathReason}{Mark}\r\n{Suffix}";
                bool needUpdate = currentText != changeTo;

                if (needUpdate)
                {
                    target.cosmetics.nameText.text = changeTo;

                    float offset = 0.2f;
                    float colorBlind = -0.2f;

                    if (NameNotifyManager.Notice.TryGetValue(seer.PlayerId, out var notify) && notify.Text.Contains('\n'))
                    {
                        int count = notify.Text.Count(x => x == '\n');
                        for (int i = 0; i < count; i++)
                        {
                            offset += 0.1f;
                            colorBlind -= 0.1f;
                        }
                    }

                    if (Suffix.ToString() != string.Empty)
                    {
                        // If the name is on two lines, the job title text needs to be moved up.
                        offset += 0.15f;
                        colorBlind -= 0.2f;
                    }

                    if (!seer.IsAlive() && !target.IsAlive()) { offset += 0.1f; colorBlind -= 0.1f; }

                    RoleText.transform.SetLocalY(offset);
                    target.cosmetics.colorBlindText.transform.SetLocalY(colorBlind);
                }
            }
            else
            {
                RoleText.transform.SetLocalY(0.2f);
                __instance.cosmetics.colorBlindText.transform.SetLocalY(-0.32f);
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
                if (loversPlayer.IsAlive() && loversPlayer.PlayerId != deathId) continue;

                Main.isLoversDead = true;
                foreach (var partnerPlayer in Main.LoversPlayers.ToArray())
                {
                    if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;

                    if (partnerPlayer.PlayerId != deathId && partnerPlayer.IsAlive())
                    {
                        if (partnerPlayer.Is(CustomRoles.Lovers))
                        {
                            partnerPlayer.SetDeathReason(PlayerState.DeathReason.FollowingSuicide);

                            if (isExiled)
                            {
                                if (Main.PlayersDiedInMeeting.Contains(deathId))
                                {
                                    partnerPlayer.Data.IsDead = true;
                                    partnerPlayer.RpcExileV2();
                                    Main.PlayerStates[partnerPlayer.PlayerId].SetDead();
                                    if (MeetingHud.Instance?.state is MeetingHud.VoteStates.Discussion or MeetingHud.VoteStates.NotVoted or MeetingHud.VoteStates.Voted)
                                    {
                                        MeetingHud.Instance?.CheckForEndVoting();
                                    }
                                    MurderPlayerPatch.AfterPlayerDeathTasks(partnerPlayer, partnerPlayer, true);
                                    _ = new LateTask(() => HudManager.Instance?.SetHudActive(false), 0.3f, "SetHudActive in LoversSuicide", shoudLog: false);
                                }
                                else
                                {
                                    CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                                }
                            }
                            else
                            {
                                partnerPlayer.RpcMurderPlayer(partnerPlayer);
                            }
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
        roleText.fontSize = 1.3f;
        roleText.text = "RoleText";
        roleText.gameObject.name = "RoleText";
        roleText.enabled = false;
    }
}
// Player press vent button
[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoEnterVent))]
class CoEnterVentPatch
{
    public static bool Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] int id)
    {
        if (!AmongUsClient.Instance.AmHost || GameStates.IsHideNSeek) return true;
        Logger.Info($" {__instance.myPlayer.GetNameWithRole().RemoveHtmlTags()}, Vent ID: {id}", "CoEnterVent");

        //FFA
        if (Options.CurrentGameMode == CustomGameMode.FFA && FFAManager.CheckCoEnterVent(__instance, id))
        {
            return true;
        }

        if (KillTimerManager.AllKillTimers.TryGetValue(__instance.myPlayer.PlayerId, out var timer))
        {
            KillTimerManager.AllKillTimers[__instance.myPlayer.PlayerId] = timer + 0.5f;
        }

        // Check others enter to vent
        if (CustomRoleManager.OthersCoEnterVent(__instance, id))
        {
            return true;
        }

        var playerRoleClass = __instance.myPlayer.GetRoleClass();

        // Prevent vanilla players from enter vents if their current role does not allow it
        if (!__instance.myPlayer.CanUseVents() || (playerRoleClass != null && playerRoleClass.CheckBootFromVent(__instance, id))
        )
        {
            _ = new LateTask(() => __instance?.RpcBootFromVent(id), 0.5f, "Prevent Enter Vents");
            return true;
        }

        playerRoleClass?.OnCoEnterVent(__instance, id);
        return true;
    }
    public static void Postfix()
    {
        _ = new LateTask(() => VentSystemDeterioratePatch.ForceUpadate = false, 1f, "Set Force Upadate As False", shoudLog: false);
    }
}
// Player entered in vent
[HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
class EnterVentPatch
{
    public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc)
    {
        if (GameStates.IsHideNSeek) return;

        Main.LastEnteredVent.Remove(pc.PlayerId);
        Main.LastEnteredVent.Add(pc.PlayerId, __instance);
        Main.LastEnteredVentLocation.Remove(pc.PlayerId);
        Main.LastEnteredVentLocation.Add(pc.PlayerId, pc.GetCustomPosition());

        if (!AmongUsClient.Instance.AmHost) return;

        pc.GetRoleClass()?.OnEnterVent(pc, __instance);

        if (pc.Is(CustomRoles.Unlucky))
        {
            Unlucky.SuicideRand(pc, Unlucky.StateSuicide.EnterVent);
        }
    }
}
[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoExitVent))]
class CoExitVentPatch
{
    public static void Postfix(PlayerPhysics __instance, [HarmonyArgument(0)] int id)
    {
        if (GameStates.IsHideNSeek) return;
        Logger.Info($" {__instance.myPlayer.GetNameWithRole().RemoveHtmlTags()}, Vent ID: {id}", "CoExitVent");

        var player = __instance.myPlayer;
        if (Options.CurrentGameMode == CustomGameMode.FFA && FFAManager.FFA_DisableVentingWhenKCDIsUp.GetBool())
        {
            FFAManager.CoExitVent(player);
        }

        if (!AmongUsClient.Instance.AmHost) return;

        player.GetRoleClass()?.OnExitVent(player, id);

        _ = new LateTask(() => { player?.RpcSetVentInteraction(); }, 0.8f, $"Set vent interaction after exit vent {player?.PlayerId}", shoudLog: false);
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
class PlayerControlCompleteTaskPatch
{
    public static bool Prefix(PlayerControl __instance, uint idx)
    {
        if (GameStates.IsHideNSeek) return true;

        var player = __instance;

        Logger.Info($"Task Complete: {player.GetNameWithRole()}", "CompleteTask.Prefix");
        var taskState = player.GetPlayerTaskState();
        taskState.Update(player);

        var ret = true;

        if (AmongUsClient.Instance.AmHost)
        {
            var roleClass = player.GetRoleClass();
            // Check task complete for role
            if (roleClass != null)
            {
                ret = roleClass.OnTaskComplete(player, taskState.CompletedTasksCount, taskState.AllTasksCount);
            }

            // Check others complete task
            var playerTask = player.myTasks.ToArray().FirstOrDefault(task => task.Id == idx);

            if (playerTask != null)
                CustomRoleManager.OthersCompleteThisTask(player, playerTask);

            var playerSubRoles = player.GetCustomSubRoles();

            // Add-Ons
            if (playerSubRoles.Any())
            {
                foreach (var subRole in playerSubRoles)
                {
                    switch (subRole)
                    {
                        case CustomRoles.Unlucky when player.IsAlive():
                            Unlucky.SuicideRand(player, Unlucky.StateSuicide.CompleteTask);
                            break;

                        case CustomRoles.Tired when player.IsAlive():
                            Tired.AfterActionTasks(player);
                            break;

                        case CustomRoles.Bloodthirst when player.IsAlive():
                            Bloodthirst.OnTaskComplete(player);
                            break;

                        case CustomRoles.Ghoul when taskState.CompletedTasksCount >= taskState.AllTasksCount:
                            Ghoul.OnTaskComplete(player);
                            break;

                        case CustomRoles.Madmate when taskState.IsTaskFinished && player.Is(CustomRoles.Snitch):
                            foreach (var impostor in Main.AllAlivePlayerControls.Where(pc => pc.Is(Custom_Team.Impostor)).ToArray())
                            {
                                NameColorManager.Add(impostor.PlayerId, player.PlayerId, "#ff1919");
                            }
                            break;
                    }
                }
            }
        }

        // Add task for Workhorse
        ret &= Workhorse.OnAddTask(player);

        Utils.NotifyRoles(SpecifySeer: player, ForceLoop: true);
        Utils.NotifyRoles(SpecifyTarget: player, ForceLoop: true);

        return ret;
    }
    public static void Postfix()
    {
        if (GameStates.IsHideNSeek) return;

        // Temporarily placed until the treatment of attribute classes is determined
        GameData.Instance.RecomputeTaskCounts();
        Logger.Info($"TotalTaskCounts = {GameData.Instance.CompletedTasks}/{GameData.Instance.TotalTasks}", "CompleteTask.Postfix");
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckName))]
class PlayerControlCheckNamePatch
{
    public static void Postfix(PlayerControl __instance, ref string playerName)
    {
        if (!AmongUsClient.Instance.AmHost || !GameStates.IsLobby) return;

        // Set name after check vanilla code
        // The original "playerName" sometimes have randomized nickname
        // So CheckName sets the original nickname but only saved it on "Data.PlayerName"
        playerName = __instance.Data.PlayerName ?? playerName;

        if (BanManager.CheckDenyNamePlayer(__instance, playerName)) return;

        if (!Main.AllClientRealNames.ContainsKey(__instance.OwnerId))
        {
            Main.AllClientRealNames.Add(__instance.OwnerId, playerName);
        }

        // Standard nickname
        var name = playerName;
        if (Options.FormatNameMode.GetInt() == 2 && __instance.Data.ClientId != AmongUsClient.Instance.ClientId)
            name = Main.Get_TName_Snacks;
        else
        {
            name = name.RemoveHtmlTags().Replace(@"\", string.Empty).Replace("/", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty);
            if (name.Length > 10) name = name[..10];
            if (Options.DisableEmojiName.GetBool()) name = Regex.Replace(name, @"\p{Cs}", string.Empty);
            if (Regex.Replace(Regex.Replace(name, @"\s", string.Empty), @"[\x01-\x1F,\x7F]", string.Empty).Length < 1) name = Main.Get_TName_Snacks;
        }
        Main.AllPlayerNames.Remove(__instance.PlayerId);
        Main.AllPlayerNames.TryAdd(__instance.PlayerId, name);

        Logger.Info($"PlayerId: {__instance.PlayerId} - playerName: {playerName} => {name}", "Name player");

        RPC.SyncAllPlayerNames();
        if (__instance != null && !name.Equals(playerName))
        {
            Logger.Warn($"Standard nickname: {playerName} => {name}", "Name Format");
            __instance.RpcSetName(name);
        }

        _ = new LateTask(() =>
        {
            if (__instance != null && !__instance.Data.Disconnected && !__instance.IsModded())
            {
                var sender = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RequestRetryVersionCheck, SendOption.Reliable, __instance.OwnerId);
                AmongUsClient.Instance.FinishRpcImmediately(sender);
            }
        }, 0.6f, "Retry Version Check", false);
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetColor))]
class RpcSetColorPatch
{
    public static void Postfix(PlayerControl __instance, byte bodyColor)
    {
        if (Main.IntroDestroyed || __instance == null) return;

        Logger.Info($"PlayerId: {__instance.PlayerId} - playerColor: {bodyColor}", "RpcSetColor");
        if (bodyColor == 255) return;

        Main.PlayerColors.Remove(__instance.PlayerId);
        Main.PlayerColors[__instance.PlayerId] = Palette.PlayerColors[bodyColor];
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckName))]
class CmdCheckNameVersionCheckPatch
{
    public static void Postfix()
    {
        RPC.RpcVersionCheck();
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
        if (!PlayerControl.LocalPlayer.Is(Custom_Team.Impostor) &&  // Not an Impostor
            PlayerControl.LocalPlayer.HasDesyncRole())  // Desync Impostor
        {
            // Hide names
            __instance.cosmetics.ToggleNameVisible(false);
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixMixedUpOutfit))]
public static class PlayerControlFixMixedUpOutfitPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        if (!__instance.IsAlive())
        {
            return;
        }

        // Show names
        __instance.cosmetics.ToggleNameVisible(true);
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
        if (!AmongUsClient.Instance.AmHost || __instance == null) return;
        // Skip Tasks while Anti Blackout but not for real exiled
        if (AntiBlackout.SkipTasks && AntiBlackout.ExilePlayerId != __instance.PlayerId) return;

        __instance.RpcRemovePet();
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
class PlayerControlSetRolePatch
{
    private static readonly Dictionary<PlayerControl, RoleTypes> GhostRoles = [];
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] ref RoleTypes roleType, [HarmonyArgument(1)] ref bool canOverrideRole)
    {
        // Skip first assign
        if (RpcSetRoleReplacer.BlockSetRole || GameStates.IsHideNSeek) return true;

        canOverrideRole = true;
        if (GameStates.IsHideNSeek || __instance == null) return true;
        if (!ShipStatus.Instance.enabled || !AmongUsClient.Instance.AmHost) return true;

        var target = __instance;
        var targetName = __instance.GetNameWithRole().RemoveHtmlTags();

        // Ghost assign
        if (roleType is RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost)
        {
            try
            {
                GhostRoleAssign.GhostAssignPatch(__instance); // Sets customrole ghost if succeed
                __instance.SyncSettings();
            }
            catch (Exception error)
            {
                Logger.Warn($"Error After RpcSetRole: {error}", "RpcSetRole.Prefix.GhostAssignPatch");
            }

            var targetIsKiller = target.Is(Custom_Team.Impostor) || target.HasDesyncRole();
            GhostRoles.Clear();

            foreach (var seer in Main.AllPlayerControls)
            {
                var self = seer.PlayerId == target.PlayerId;
                var seerIsKiller = seer.Is(Custom_Team.Impostor) || seer.HasDesyncRole();

                if (target.HasGhostRole())
                {
                    GhostRoles[seer] = RoleTypes.GuardianAngel;
                }
                else if ((self && targetIsKiller) || (!seerIsKiller && target.Is(Custom_Team.Impostor)))
                {
                    GhostRoles[seer] = RoleTypes.ImpostorGhost;
                }
                else
                {
                    GhostRoles[seer] = RoleTypes.CrewmateGhost;
                }
            }
            // If all players see player as Guardian Angel
            if (GhostRoles.All(kvp => kvp.Value == RoleTypes.GuardianAngel))
            {
                roleType = RoleTypes.GuardianAngel;
                __instance.RpcSetRoleDesync(RoleTypes.GuardianAngel, __instance.GetClientId());
                foreach (var seer in Main.AllPlayerControls)
                {
                    if (seer.PlayerId == __instance.PlayerId) continue;
                    __instance.RpcSetRoleDesync(RoleTypes.CrewmateGhost, seer.GetClientId());
                }
                GhostRoleAssign.CreateGAMessage(__instance);
                return false;
            }
            // If all players see player as Crewmate Ghost
            else if (GhostRoles.All(kvp => kvp.Value == RoleTypes.CrewmateGhost))
            {
                roleType = RoleTypes.CrewmateGhost;
                return true;
            }
            // If all players see player as Impostor Ghost
            else if (GhostRoles.All(kvp => kvp.Value == RoleTypes.ImpostorGhost))
            {
                roleType = RoleTypes.ImpostorGhost;
                return true;
            }
            else
            {
                foreach ((var seer, var role) in GhostRoles)
                {
                    if (seer == null || target == null) continue;
                    Logger.Info($"Desync {targetName} => {role} for {seer.GetNameWithRole().RemoveHtmlTags()}", "PlayerControl.RpcSetRole");
                    target.RpcSetRoleDesync(role, seer.GetClientId());
                }
                return false;
            }
        }

        return true;
    }
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes roleType, bool __runOriginal)
    {
        if (!AmongUsClient.Instance.AmHost || __instance == null) return;

        try
        {
            if (__runOriginal)
            {
                Logger.Info($" {__instance.GetRealName()} => {roleType}", "PlayerControl.RpcSetRole");
            }
        }
        catch (Exception e)
        {
            Logger.Error($"Role Type:{roleType} - Run Original:{__runOriginal} - Error: {e}", "RpcSetRole.Postfix");
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CoSetRole))]
class PlayerControlLocalSetRolePatch
{
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes role)
    {
        if (!AmongUsClient.Instance.AmHost && GameStates.IsNormalGame && !GameStates.IsModHost)
        {
            var modRole = role switch
            {
                RoleTypes.Crewmate => CustomRoles.CrewmateTOHE,
                RoleTypes.Impostor => CustomRoles.ImpostorTOHE,
                RoleTypes.Scientist => CustomRoles.ScientistTOHE,
                RoleTypes.Engineer => CustomRoles.EngineerTOHE,
                RoleTypes.Shapeshifter => CustomRoles.ShapeshifterTOHE,
                RoleTypes.Noisemaker => CustomRoles.NoisemakerTOHE,
                RoleTypes.Phantom => CustomRoles.PhantomTOHE,
                RoleTypes.Tracker => CustomRoles.TrackerTOHE,
                _ => CustomRoles.NotAssigned,
            };
            if (modRole != CustomRoles.NotAssigned)
            {
                Main.PlayerStates[__instance.PlayerId].SetMainRole(modRole);
            }
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.AssertWithTimeout))]
class AssertWithTimeoutPatch
{
    // Completely disable the trash put by Innersloth
    public static bool Prefix()
    {
        return false;
    }
}
