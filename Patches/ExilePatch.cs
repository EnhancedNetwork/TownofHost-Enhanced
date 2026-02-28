using AmongUs.Data;
using AmongUs.GameOptions;
using Hazel;
using System;
using System.Diagnostics;
using TOHE.Roles.Core;
using TOHE.Roles.Neutral;

namespace TOHE;

class ExileControllerWrapUpPatch
{
    public static Stopwatch Stopwatch;
    public static NetworkedPlayerInfo AntiBlackout_LastExiled;
    [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
    class ExileControllerBeginPatch
    {
        // This patch is to show exile string for modded players
        public static void Postfix(ExileController __instance, [HarmonyArgument(0)] ExileController.InitProperties init)
        {
            if (Options.CurrentGameMode is CustomGameMode.Standard && init != null && init.outfit != null)
                __instance.completeString = CheckForEndVotingPatch.TempExileMsg;
            // TempExileMsg for client is sent in RpcClose
        }
    }

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
    class BaseExileControllerPatch
    {
        public static void Prefix()
        {
            CheckAndDoRandomSpawn();
            CheckForEndVotingPatch.TempExiledPlayer = null;
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

    [HarmonyPatch(typeof(AirshipExileController._WrapUpAndSpawn_d__11), nameof(AirshipExileController._WrapUpAndSpawn_d__11.MoveNext))]
    class AirshipExileControllerPatch
    {
        public static void Postfix(AirshipExileController._WrapUpAndSpawn_d__11 __instance, ref bool __result)
        {
            var instance = __instance.__4__this;
            if (!__result)
            {
                Logger.Info("AirshipExileController WrapUpAndSpawn Postfix", "AirshipExileControllerPatch");
                try
                {
                    WrapUpPostfix(instance.initData.networkedPlayer);
                }
                catch (Exception error)
                {
                    Logger.Error($"Error after exiled: {error}", "WrapUpAndSpawn");
                }
                finally
                {
                    WrapUpFinalizer(instance.initData.networkedPlayer);
                }
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
            if (spawnMap != null) Main.EnumeratePlayerControls().Do(spawnMap.RandomTeleport);
        }
    }
    private static void WrapUpPostfix(NetworkedPlayerInfo exiled)
    {
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

        List<Collector> collectorCL = Utils.GetRoleBasesByType<Collector>()?.ToList();

        if (collectorCL != null) Logger.Info($"{!collectorCL.Any(x => x.CollectorWin(false))}", "!Collector.CollectorWin(false)");
        Logger.Info($"{exiled != null}", "exiled != null");
        bool CLThingy = collectorCL == null || !collectorCL.Any(x => x.CollectorWin(false));

        if (CLThingy && exiled)
        {
            // exiled.IsDead = true;
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


        foreach (var player in Main.EnumeratePlayerControls())
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
            Stopwatch = Stopwatch.StartNew();

            LateTask.New(() =>
            {
                if (GameStates.IsEnded) return;
                AntiBlackout.RevertToActualRoleTypes();
            }, 2f, "Revert AntiBlackout Measures");
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

    public static void AfterMeetingTasks()
    {
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default || GameStates.IsEnded){ 
            Stopwatch.Reset();
            return;
        }

        bool hasValue = false;
        CustomRpcSender sender = CustomRpcSender.Create("Exile AfterMeetingDeathPlayers", SendOption.Reliable);
        Main.AfterMeetingDeathPlayers.Keys.ToValidPlayers().Do(x => hasValue |= sender.RpcExileV2(x));
        sender.SendMessage(dispose: !hasValue);

        foreach ((byte id, PlayerState.DeathReason deathReason) in Main.AfterMeetingDeathPlayers)
        {
            var player = id.GetPlayer();
            var state = Main.PlayerStates[id];

            Logger.Info($"{player?.GetNameWithRole().RemoveHtmlTags()} died with {deathReason}", "AfterMeetingDeath");

            if (deathReason == PlayerState.DeathReason.Suicide)
                player?.SetRealKiller(player, true);

            state.deathReason = deathReason;
            state.SetDead();

            if (!player) continue;

            if (deathReason == PlayerState.DeathReason.Suicide)
                player.SetRealKiller(player, true);

            MurderPlayerPatch.AfterPlayerDeathTasks(player, player, true);
        }

        Main.AfterMeetingDeathPlayers.Clear();

        Utils.AfterMeetingTasks();
        Utils.SyncAllSettings();
        Utils.CheckAndSetVentInteractions();

        Main.Instance.StartCoroutine(Utils.NotifyEveryoneAsync());

        _ = new LateTask(() =>
        {
            foreach (var player in Main.EnumerateAlivePlayerControls())
            {
                if (player.GetRoleClass() is not DefaultSetup)
                {
                    if (player.GetRoleClass().ThisRoleBase.GetRoleTypesDirect() is RoleTypes.Impostor or RoleTypes.Phantom or RoleTypes.Shapeshifter or RoleTypes.Viper)
                    {
                        player.ResetKillCooldown();
                        if (Main.AllPlayerKillCooldown.TryGetValue(player.PlayerId, out var killTimer) && (killTimer - 2f) > 0f)
                        {
                            player.SetKillCooldown(killTimer - 2f);
                        }
                    }
                }
            }
        }, 1f, $"Fix Kill Cooldown Task after meeting");

        Main.LastMeetingEnded = Utils.TimeStamp;
        Stopwatch.Reset();
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
