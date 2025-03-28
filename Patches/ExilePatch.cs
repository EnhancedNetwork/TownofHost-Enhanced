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
        public static void Prefix()
        {
            CheckAndDoRandomSpawn();
        }
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
        public static void Prefix()
        {
            CheckAndDoRandomSpawn();
        }
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
    private static void CheckAndDoRandomSpawn()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (RandomSpawn.IsRandomSpawn() || Options.CurrentGameMode == CustomGameMode.FFA)
        {
            RandomSpawn.SpawnMap spawnMap = Utils.GetActiveMapName() switch
            {
                MapNames.Skeld => new RandomSpawn.SkeldSpawnMap(),
                MapNames.MiraHQ => new RandomSpawn.MiraHQSpawnMap(),
                MapNames.Polus => new RandomSpawn.PolusSpawnMap(),
                MapNames.Dleks => new RandomSpawn.DleksSpawnMap(),
                MapNames.Fungle => new RandomSpawn.FungleSpawnMap(),
                _ => null,
            };
            if (spawnMap != null) Main.AllPlayerControls.Do(spawnMap.RandomTeleport);
        }
    }
    private static void WrapUpPostfix(NetworkedPlayerInfo exiled)
    {
        if (AntiBlackout.BlackOutIsActive) exiled = AntiBlackout_LastExiled;

        // Still not springing up in Airship
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
            exiled.IsDead = true;
            exiled.PlayerId.SetDeathReason(PlayerState.DeathReason.Vote);

            var exiledRoleClass = exiled.PlayerId.GetRoleClassById();
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

            // Check for remove Pet
            player.RpcRemovePet();

            // Set UnShift after meeting
            player.DoUnShiftState();
        }

        Main.MeetingIsStarted = false;
        Main.MeetingsPassed++;

        Utils.CountAlivePlayers(sendLog: true, checkGameEnd: Options.CurrentGameMode == CustomGameMode.Standard);
    }

    private static void WrapUpFinalizer(NetworkedPlayerInfo exiled)
    {
        // Even if an exception occurs in WrapUpPostfix, this is the only part that will be executed reliably
        if (AmongUsClient.Instance.AmHost)
        {
            _ = new LateTask(() =>
            {
                if (GameStates.IsEnded) return;

                exiled = AntiBlackout_LastExiled;
                AntiBlackout.SendGameData();
                AntiBlackout.SetRealPlayerRoles();

                if (AntiBlackout.BlackOutIsActive && // State in which the expulsion target is overwritten (need not be executed if the expulsion target is not overwritten)
                    exiled != null && // Exiled is not null
                    exiled.Object != null) //exiled.Object is not null
                {
                    exiled.Object.RpcExileV2();
                }
            }, Options.CurrentGameMode is CustomGameMode.Standard ? 0.5f : 0.8f, "Restore IsDead Task");

            _ = new LateTask(AntiBlackout.ResetAfterMeeting, 0.6f, "ResetAfterMeeting");

            _ = new LateTask(() =>
            {
                if (GameStates.IsEnded) return;

                Main.AfterMeetingDeathPlayers.Do(x =>
                {
                    var player = x.Key.GetPlayer();
                    var state = Main.PlayerStates[x.Key];

                    Logger.Info($"{player?.GetNameWithRole().RemoveHtmlTags()} died with {x.Value}", "AfterMeetingDeath");

                    state.deathReason = x.Value;
                    state.SetDead();
                    player?.RpcExileV2();

                    if (x.Value == PlayerState.DeathReason.Suicide)
                        player?.SetRealKiller(player, true);

                    MurderPlayerPatch.AfterPlayerDeathTasks(player, player, true);
                });

                Main.AfterMeetingDeathPlayers.Clear();

                Utils.AfterMeetingTasks();
                Utils.SyncAllSettings();
                Utils.CheckAndSetVentInteractions();

                if (Main.CurrentServerIsVanilla && Options.BypassRateLimitAC.GetBool())
                {
                    Main.Instance.StartCoroutine(Utils.NotifyEveryoneAsync(speed: 5));
                }
                else
                {
                    Utils.NotifyRoles();
                }

                Main.LastMeetingEnded = Utils.TimeStamp;
            }, 1f, "AfterMeetingDeathPlayers Task");
        }

        //This should happen shortly after the Exile Controller wrap up finished for clients
        //For Certain Laggy clients 0.8f delay is still not enough. The finish time can differ
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
