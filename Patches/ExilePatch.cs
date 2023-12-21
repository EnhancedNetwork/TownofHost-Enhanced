using AmongUs.Data;
using HarmonyLib;
using System.Linq;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;

namespace TOHE;

class ExileControllerWrapUpPatch
{
    public static GameData.PlayerInfo AntiBlackout_LastExiled;
    [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
    class BaseExileControllerPatch
    {
        public static void Postfix(ExileController __instance)
        {
            try
            {
                WrapUpPostfix(__instance.exiled);
            }
            finally
            {
                WrapUpFinalizer(__instance.exiled);
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
                WrapUpPostfix(__instance.exiled);
            }
            finally
            {
                WrapUpFinalizer(__instance.exiled);
            }
        }
    }
    static void WrapUpPostfix(GameData.PlayerInfo exiled)
    {
        if (AntiBlackout.ImpostorOverrideExiledPlayer || AntiBlackout.NeutralOverrideExiledPlayer) exiled = AntiBlackout_LastExiled;

        bool DecidedWinner = false;
        if (!AmongUsClient.Instance.AmHost) return;
        AntiBlackout.RestoreIsDead(doSend: false);

        Logger.Info($"{!Collector.CollectorWin(false)}", "!Collector.CollectorWin(false)");
        Logger.Info($"{exiled != null}", "exiled != null");
        if (!Collector.CollectorWin(false) && exiled != null)
        {
            // Deal with the darkening bug for the spirit world
            if (!(AntiBlackout.ImpostorOverrideExiledPlayer || AntiBlackout.NeutralOverrideExiledPlayer) && Main.ResetCamPlayerList.Contains(exiled.PlayerId))
                exiled.Object?.ResetPlayerCam(1f);

            exiled.IsDead = true;
            Main.PlayerStates[exiled.PlayerId].deathReason = PlayerState.DeathReason.Vote;
            
            var role = exiled.GetCustomRole();

            //判断冤罪师胜利
            var pcList = Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Innocent) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == exiled.PlayerId).ToArray();
            if (pcList.Any())
            {
                if (!Options.InnocentCanWinByImp.GetBool() && role.IsImpostor())
                {
                    Logger.Info("Exeiled Winner Check for impostor", "Innocent");
                }
                else
                {
                    bool isInnocentWinConverted = false;
                    foreach (var Innocent in pcList)
                    {
                        if (CustomWinnerHolder.CheckForConvertedWinner(Innocent.PlayerId))
                        {
                            isInnocentWinConverted = true;
                            break;
                        }
                    }
                    if (!isInnocentWinConverted)
                    {
                        if (DecidedWinner)
                        {
                            CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Innocent);
                        }
                        else
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Innocent);
                        }

                        pcList.Do(x => CustomWinnerHolder.WinnerIds.Add(x.PlayerId));
                    }
                    DecidedWinner = true;
                }
            }
            //Jester win
            if (Options.MeetingsNeededForJesterWin.GetInt() <= Main.MeetingsPassed)
            {           
                if (role.Is(CustomRoles.Jester) && AmongUsClient.Instance.AmHost)
                {
                    if (!CustomWinnerHolder.CheckForConvertedWinner(exiled.PlayerId))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jester);
                        CustomWinnerHolder.WinnerIds.Add(exiled.PlayerId);
                    }

                    foreach (var executioner in Executioner.playerIdList)
                    {
                        //var GetValue = Executioner.Target.TryGetValue(executioner, out var targetId);
                        if (Executioner.Target.TryGetValue(executioner, out var targetId) && exiled.PlayerId == targetId)
                        {
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Executioner);
                            CustomWinnerHolder.WinnerIds.Add(executioner);
                        }
                    }
                    DecidedWinner = true;
                }
            }

            //Executioner check win
            if (Executioner.CheckExileTarget(exiled, DecidedWinner))
            {
                DecidedWinner = true;
            }

            //Terrorist check win
            if (role.Is(CustomRoles.Terrorist))
            {
                Utils.CheckTerroristWin(exiled);
            }

            //Devourer check win
            if (role.Is(CustomRoles.Devourer))
            {
                Devourer.OnDevourerDied(exiled.PlayerId);
            }

            //Lawyer check win
            if (Lawyer.CheckExileTarget(exiled, DecidedWinner))
            {
                DecidedWinner = false;
            }

            if (role.Is(CustomRoles.Devourer))
            {
                Devourer.OnDevourerDied(exiled.PlayerId);
            }

            if (Lawyer.CheckExileTarget(exiled, DecidedWinner))
            {
                DecidedWinner = false;
            }

            Pixie.CheckExileTarget(exiled);

            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Terrorist) Main.PlayerStates[exiled.PlayerId].SetDead();

            Instigator.OnPlayerExile(exiled);
        }
        if (AmongUsClient.Instance.AmHost && Main.IsFixedCooldown)
        {
            Main.RefixCooldownDelay = Options.DefaultKillCooldown - 3f;
        }

        if (Witch.IsEnable) 
            Witch.RemoveSpelledPlayer();

        if (HexMaster.IsEnable)
            HexMaster.RemoveHexedPlayer();

        if (Swapper.Vote.Any() && Swapper.VoteTwo.Any())
        {
            foreach (var swapper in Main.AllAlivePlayerControls)
            {
                if (swapper.Is(CustomRoles.Swapper))
                {
                    Swapper.Swappermax[swapper.PlayerId]--;
                    Swapper.Vote.Clear();
                    Swapper.VoteTwo.Clear();
                    Main.SwapSend = false;
                }
            }
        }
        
        foreach (var player in Main.AllPlayerControls)
        {
            //PlayerControl player = allPlayerControls[item];
            CustomRoles playerRole = player.GetCustomRole(); // Only roles (no add-ons)

            switch (playerRole)
            {
                case CustomRoles.Mayor when Options.MayorHasPortableButton.GetBool():
                    player.RpcResetAbilityCooldown();
                    break;

                case CustomRoles.Warlock:
                    Main.CursedPlayers[player.PlayerId] = null;
                    Main.isCurseAndKill[player.PlayerId] = false;
                    break;
            }

            if (Infectious.IsEnable)
            {
                if (playerRole.Is(CustomRoles.Infectious) && !player.IsAlive())
                {
                    Infectious.MurderInfectedPlayers();
                }
            }

            if (Shroud.IsEnable)
            {
                Shroud.MurderShroudedPlayers(player);
            }

            player.RpcRemovePet();

            player.ResetKillCooldown();
            player.RpcResetAbilityCooldown();
        }

        Main.MeetingIsStarted = false;
        Main.MeetingsPassed++;

        if (Options.RandomSpawn.GetBool() || Options.CurrentGameMode == CustomGameMode.FFA)
        {
            RandomSpawn.SpawnMap map;
            switch (Main.NormalOptions.MapId)
            {
                case 0:
                    map = new RandomSpawn.SkeldSpawnMap();
                    Main.AllPlayerControls.Do(map.RandomTeleport);
                    break;
                case 1:
                    map = new RandomSpawn.MiraHQSpawnMap();
                    Main.AllPlayerControls.Do(map.RandomTeleport);
                    break;
                case 2:
                    map = new RandomSpawn.PolusSpawnMap();
                    Main.AllPlayerControls.Do(map.RandomTeleport);
                    break;
                case 3:
                    map = new RandomSpawn.DleksSpawnMap();
                    Main.AllPlayerControls.Do(map.RandomTeleport);
                    break;
                case 5:
                    map = new RandomSpawn.FungleSpawnMap();
                    Main.AllPlayerControls.Do(map.RandomTeleport);
                    break;
            }
        }

        FallFromLadder.Reset();
        Utils.CountAlivePlayers(true);
        Utils.AfterMeetingTasks();
        Utils.SyncAllSettings();
        Utils.NotifyRoles(ForceLoop: true);
    }

    static void WrapUpFinalizer(GameData.PlayerInfo exiled)
    {
        // Even if an exception occurs in WrapUpPostfix, this is the only part that will be executed reliably.
        if (AmongUsClient.Instance.AmHost)
        {
            _ = new LateTask(() =>
            {
                exiled = AntiBlackout_LastExiled;
                AntiBlackout.SendGameData();
                if ((AntiBlackout.ImpostorOverrideExiledPlayer || AntiBlackout.NeutralOverrideExiledPlayer) && // State in which the expulsion target is overwritten (need not be executed if the expulsion target is not overwritten)
                    exiled != null && // exiled is not null
                    exiled.Object != null) //exiled.Object is not null
                {
                    exiled.Object.RpcExileV2();
                }
            }, 0.5f, "Restore IsDead Task");

            _ = new LateTask(() =>
            {
                Main.AfterMeetingDeathPlayers.Do(x =>
                {
                    var player = Utils.GetPlayerById(x.Key);
                    var state = Main.PlayerStates[x.Key];
                    
                    Logger.Info($"{player.GetNameWithRole()} died with {x.Value}", "AfterMeetingDeath");

                    state.deathReason = x.Value;
                    state.SetDead();
                    player?.RpcExileV2();

                    if (x.Value == PlayerState.DeathReason.Suicide)
                        player?.SetRealKiller(player, true);

                    if (Main.ResetCamPlayerList.Contains(x.Key))
                        player?.ResetPlayerCam(1f);

                    Utils.AfterPlayerDeathTasks(player);
                });
                Main.AfterMeetingDeathPlayers.Clear();

            }, 0.5f, "AfterMeetingDeathPlayers Task");
        }

        GameStates.AlreadyDied |= !Utils.IsAllAlive;
        RemoveDisableDevicesPatch.UpdateDisableDevices();
        SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);
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
