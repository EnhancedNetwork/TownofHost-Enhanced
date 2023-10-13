using AmongUs.Data;
using HarmonyLib;
using System.Linq;

using TOHE.Roles.Impostor;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Neutral;
using TOHE.Roles.Double;

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
            if (Main.AllPlayerControls.Any(x => x.Is(CustomRoles.Innocent) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == exiled.PlayerId))
            {
                if (!Options.InnocentCanWinByImp.GetBool() && role.IsImpostor())
                {
                    Logger.Info("Exeiled Winner Check", "Innocent");
                }
                else
                {
                    if (DecidedWinner) CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Innocent);
                    else CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Innocent);
                    Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Innocent) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == exiled.PlayerId)
                        .Do(x => CustomWinnerHolder.WinnerIds.Add(x.PlayerId));
                    DecidedWinner = true;
                }
            }
            if (Options.MeetingsNeededForJesterWin.GetInt() <= Main.MeetingsPassed)
            {           
                if (role == CustomRoles.Jester && AmongUsClient.Instance.AmHost)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jester);
                    CustomWinnerHolder.WinnerIds.Add(exiled.PlayerId);
                    foreach (var executioner in Executioner.playerIdList)
                    {
                        var GetValue = Executioner.Target.TryGetValue(executioner, out var targetId);
                        if (GetValue && exiled.PlayerId == targetId)
                        {
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Executioner);
                            CustomWinnerHolder.WinnerIds.Add(executioner);
                        }
                    }
                    DecidedWinner = true;
                }
            }            
            if (Executioner.CheckExileTarget(exiled, DecidedWinner)) DecidedWinner = true;

            if (role == CustomRoles.Terrorist) Utils.CheckTerroristWin(exiled);

            if (role == CustomRoles.Devourer) Devourer.OnDevourerDied(exiled.PlayerId);

            if (Lawyer.CheckExileTarget(exiled, DecidedWinner)) DecidedWinner = false;

            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Terrorist) Main.PlayerStates[exiled.PlayerId].SetDead();
        }
        if (AmongUsClient.Instance.AmHost && Main.IsFixedCooldown)
            Main.RefixCooldownDelay = Options.DefaultKillCooldown - 3f;

        Witch.RemoveSpelledPlayer();
        HexMaster.RemoveHexedPlayer();
        //Occultist.RemoveCursedPlayer();

        if (Swapper.Vote.Count > 0 && Swapper.VoteTwo.Count > 0)
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

        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.Is(CustomRoles.EvilMini) && Mini.Age != 18)
            {
                Main.AllPlayerKillCooldown[pc.PlayerId] = Mini.MinorCD.GetFloat() + 2f;
                Main.EvilMiniKillcooldown[pc.PlayerId] = Mini.MinorCD.GetFloat() + 2f;
                Main.EvilMiniKillcooldownf = Mini.MinorCD.GetFloat();
                pc.MarkDirtySettings();
                pc.SetKillCooldown();
            }
            else if (pc.Is(CustomRoles.EvilMini) && Mini.Age == 18)
            {
                Main.AllPlayerKillCooldown[pc.PlayerId] = Mini.MajorCD.GetFloat();
                pc.MarkDirtySettings();
                pc.SetKillCooldown();
            }
        }
        
        foreach (var pc in Main.AllPlayerControls)
        {
            pc.ResetKillCooldown();
            if (Options.MayorHasPortableButton.GetBool() && pc.Is(CustomRoles.Mayor))
                pc.RpcResetAbilityCooldown();
            if (pc.Is(CustomRoles.Warlock))
            {
                Main.CursedPlayers[pc.PlayerId] = null;
                Main.isCurseAndKill[pc.PlayerId] = false;
                //RPC.RpcSyncCurseAndKill();
            }
            if (pc.GetCustomRole() is
                CustomRoles.Paranoia or
                CustomRoles.Veteran or
                CustomRoles.Greedier or
                CustomRoles.DovesOfNeace or
                CustomRoles.QuickShooter or
                CustomRoles.Addict or
                CustomRoles.ShapeshifterTOHE or
                CustomRoles.Wildling or
                CustomRoles.Twister or
                CustomRoles.Deathpact or
                CustomRoles.Dazzler or
                CustomRoles.Devourer or
                CustomRoles.Nuker or
                CustomRoles.Assassin or
                CustomRoles.Camouflager or
                CustomRoles.Disperser or
                CustomRoles.Escapee or
                CustomRoles.Hacker or
                CustomRoles.Hangman or
                CustomRoles.ImperiusCurse or
                CustomRoles.Miner or
                CustomRoles.Morphling or
                CustomRoles.Sniper or
                CustomRoles.Warlock or
                CustomRoles.Workaholic or
                CustomRoles.Chameleon or
                CustomRoles.Engineer or
                CustomRoles.Grenadier or
                CustomRoles.Scientist or
                CustomRoles.Lighter or
                CustomRoles.Pitfall or
                CustomRoles.Bastion or
                CustomRoles.ScientistTOHE or
                CustomRoles.Tracefinder or
                CustomRoles.Doctor or
                CustomRoles.Alchemist or
                CustomRoles.Bomber or
                CustomRoles.Undertaker
                ) pc.RpcResetAbilityCooldown();


            if (Infectious.IsEnable)
            {
                if (pc.Is(CustomRoles.Infectious) && !pc.IsAlive())
                {
                    Infectious.MurderInfectedPlayers();
                }
            }

            if (Shroud.IsEnable)
            {
                Shroud.MurderShroudedPlayers(pc);
            }

            Main.MeetingsPassed++;

            pc.RpcRemovePet();

            if (Options.RandomSpawn.GetBool())
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
                }
            }

            FallFromLadder.Reset();
            Utils.CountAlivePlayers(true);
            Utils.AfterMeetingTasks();
            Utils.SyncAllSettings();
            Utils.NotifyRoles();
        }
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
                    if (Executioner.Target.ContainsValue(x.Key))
                        Executioner.ChangeRoleByTarget(player);
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
