using AmongUs.Data;
using System;
using TOHE.Roles.Core;
using TOHE.Roles.Neutral;

namespace TOHE;

class ExileControllerWrapUpPatch
{
    public static NetworkedPlayerInfo AntiBlackout_LastExiled;
    [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
    class BaseExileControllerPatch
    {
        public static void Postfix(ExileController __instance)
        {
            try
            {
                WrapUpPostfix(__instance.initData.networkedPlayer);
            }
            catch (Exception error)
            {
                Logger.Error($"Error after exiled: {error}", "WrapUp");
            }
            finally
            {
                WrapUpFinalizer(__instance.initData.networkedPlayer);
            }
        }
    }

    [HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.WrapUpAndSpawn))]
    class AirshipExileControllerPatch
    {
        public static void Postfix(AirshipExileController __instance)
        {
            try
            {
                WrapUpPostfix(__instance.initData.networkedPlayer);
            }
            catch (Exception error)
            {
                Logger.Error($"Error after exiled: {error}", "WrapUpAndSpawn");
            }
            finally
            {
                WrapUpFinalizer(__instance.initData.networkedPlayer);
            }
        }
    }
    static void WrapUpPostfix(NetworkedPlayerInfo exiled)
    {
        if (AntiBlackout.BlackOutIsActive) exiled = AntiBlackout_LastExiled;

        // Still not springing up in airships
        if (!GameStates.AirshipIsActive)
        {
            foreach (var state in Main.PlayerStates.Values)
            {
                state.HasSpawned = true;
            }
        }

        bool DecidedWinner = false;
        if (!AmongUsClient.Instance.AmHost) return;
        AntiBlackout.RestoreIsDead(doSend: false);

        List<Collector> collectorCL = Utils.GetRoleBasesByType<Collector>()?.ToList();

        if (collectorCL != null) Logger.Info($"{!collectorCL.Any(x => x.CollectorWin(false))}", "!Collector.CollectorWin(false)");
        Logger.Info($"{exiled != null}", "exiled != null");
        bool CLThingy = collectorCL == null || !collectorCL.Any(x => x.CollectorWin(false));

        if (CLThingy && exiled != null)
        {
            // Reset player cam for exiled desync impostor
           if (exiled.Object.HasDesyncRole())
            {
                //exiled.Object?.ResetPlayerCam(1f);
              //  exiled.Object.FixDesyncImpostorRolesBYPASS();
           }

            exiled.IsDead = true;
            exiled.PlayerId.SetDeathReason(PlayerState.DeathReason.Vote);

            var exiledPC = Utils.GetPlayerById(exiled.PlayerId);
            var exiledRoleClass = exiledPC.GetRoleClass();
           
            var emptyString = string.Empty;

            exiledRoleClass?.CheckExile(exiled, ref DecidedWinner, isMeetingHud: false, name: ref emptyString);

            CustomRoleManager.AllEnabledRoles.Do(roleClass => roleClass.CheckExileTarget(exiled, ref DecidedWinner, isMeetingHud: false, name: ref emptyString));

            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Terrorist) Main.PlayerStates[exiled.PlayerId].SetDead();
        }
        
        if (AmongUsClient.Instance.AmHost && Main.IsFixedCooldown)
        {
            Main.RefixCooldownDelay = Options.DefaultKillCooldown - 3f;
        }

        
        foreach (var player in Main.AllPlayerControls)
        {
            player.GetRoleClass()?.OnPlayerExiled(player, exiled);

            // Check Anti BlackOut
           // if (player.GetCustomRole().IsImpostor() 
           //     && !player.IsAlive() // if player is dead impostor
           //     && AntiBlackout.BlackOutIsActive) // if Anti BlackOut is activated
          //  {
          //      player.ResetPlayerCam(1f);
          //  }

            // Check for remove pet
            player.RpcRemovePet();

            // Reset Kill/Ability cooldown
            player.ResetKillCooldown();
            player.RpcResetAbilityCooldown();

            //player.FixDesyncImpostorRoles(); // Fix Impostor For Desync roles
        }
        exiled?.Object?.FixDesyncImpostorRoles(true);

        Main.MeetingIsStarted = false;
        Main.MeetingsPassed++;

        FallFromLadder.Reset();
        Utils.CountAlivePlayers(sendLog: true, checkGameEnd: Options.CurrentGameMode is CustomGameMode.Standard);
        Utils.AfterMeetingTasks();
        Utils.SyncAllSettings();
        Utils.NotifyRoles(NoCache: true);

        if (RandomSpawn.IsRandomSpawn() || Options.CurrentGameMode == CustomGameMode.FFA)
        {
            _ = new LateTask(() =>
            {
                RandomSpawn.SpawnMap map = Utils.GetActiveMapId() switch
                {
                    0 => new RandomSpawn.SkeldSpawnMap(),
                    1 => new RandomSpawn.MiraHQSpawnMap(),
                    2 => new RandomSpawn.PolusSpawnMap(),
                    3 => new RandomSpawn.DleksSpawnMap(),
                    5 => new RandomSpawn.FungleSpawnMap(),
                    _ => null,
                };
                if (map != null) Main.AllPlayerControls.Do(map.RandomTeleport);

            }, 0.8f, "Random Spawn After Meeting");
        }
    }

    static void WrapUpFinalizer(NetworkedPlayerInfo exiled)
    {
        // Even if an exception occurs in WrapUpPostfix, this is the only part that will be executed reliably.
        if (AmongUsClient.Instance.AmHost)
        {
            _ = new LateTask(() =>
            {
                exiled = AntiBlackout_LastExiled;
                AntiBlackout.SendGameData();
                if (AntiBlackout.BlackOutIsActive && // State in which the expulsion target is overwritten (need not be executed if the expulsion target is not overwritten)
                    exiled != null && // exiled is not null
                    exiled.Object != null) //exiled.Object is not null
                {
                    exiled.Object.RpcExileV2();
                }
            }, 0.8f, "Restore IsDead Task");

            _ = new LateTask(() =>
            {
                Main.AfterMeetingDeathPlayers.Do(x =>
                {
                    var player = Utils.GetPlayerById(x.Key);
                    var state = Main.PlayerStates[x.Key];
                    
                    Logger.Info($"{player.GetNameWithRole().RemoveHtmlTags()} died with {x.Value}", "AfterMeetingDeath");

                    state.deathReason = x.Value;
                    state.SetDead();
                    player?.RpcExileV2();

                    if (x.Value == PlayerState.DeathReason.Suicide)
                        player?.SetRealKiller(player, true);

                    // Reset player cam for dead desync impostor
                   // if (player.HasDesyncRole())
                    //{
                    //    player?.ResetPlayerCam(1f);
                   // }

                    MurderPlayerPatch.AfterPlayerDeathTasks(player, player, true);
                });
                Main.AfterMeetingDeathPlayers.Clear();

            }, 0.8f, "AfterMeetingDeathPlayers Task");
        }
        //This should happen shortly after the Exile Controller wrap up finished for clients
        //For Certain Laggy clients 0.8f delay is still not enough. The finish time can differ.
        //If the delay is too long, it will influence other normal players' view

        GameStates.AlreadyDied |= !Utils.IsAllAlive;
        RemoveDisableDevicesPatch.UpdateDisableDevices();
        SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);

        _ = new LateTask(() =>
        {
            if (!AmongUsClient.Instance.IsGameOver)
                DestroyableSingleton<HudManager>.Instance.SetHudActive(true);
        }, 0.8f, "Set Hud Active");

        Logger.Info("Start of Task Phase", "Phase");
    }

    [HarmonyPatch(typeof(PbExileController), nameof(PbExileController.PlayerSpin))]
    class PolusExileHatFixPatch
    {
        public static void Prefix(PbExileController __instance)
        {
            __instance.Player.cosmetics.hat.transform.localPosition = new(-0.2f, 0.6f, 1.1f);
        }
    }
}
