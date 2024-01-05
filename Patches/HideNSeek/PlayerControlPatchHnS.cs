using HarmonyLib;
using System.Collections.Generic;
using TOHE.Modules;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Patches.HideNSeek;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
class CheckMurderInHidenSeekPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (GameStates.IsNormalGame) return true;

        var killer = __instance; // Alternative variable

        Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} => {target.GetNameWithRole().RemoveHtmlTags()}", "CheckMurder H&S");

        // Killer is already dead
        if (!killer.IsAlive())
        {
            Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} was cancelled because it is dead", "CheckMurder H&S");
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
            Logger.Info("The target is in an unkillable state and the kill is canceled", "CheckMurder H&S");
            return false;
        }
        // Target Is Dead?
        if (!target.IsAlive())
        {
            Logger.Info("The target is in a dead state and the kill is canceled", "CheckMurder H&S");
            return false;
        }
        // Checking during the meeting
        if (MeetingHud.Instance != null)
        {
            Logger.Info("In the meeting, the kill was canceled", "CheckMurder H&S");
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
class MurderPlayerInHidenSeekPatch
{
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] MurderResultFlags resultFlags)
    {
        if (GameStates.IsNormalGame) return;
        if (!target.Data.IsDead || !AmongUsClient.Instance.AmHost) return;

        PlayerControl killer = __instance;

        Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} => {target.GetNameWithRole().RemoveHtmlTags()}", "MurderPlayer H&S");

        if (killer != __instance)
        {
            Logger.Info($"Real Killer => {killer.GetNameWithRole().RemoveHtmlTags()}", "MurderPlayer H&S");

        }
        if (Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.etc)
        {
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Kill;
        }

        Main.PlayerStates[target.PlayerId].SetDead();
        target.SetRealKiller(killer, true);

        if (Options.LowLoadMode.GetBool())
        {
            killer.MarkDirtySettings();
            target.MarkDirtySettings();
        }
        else
        {
            Utils.SyncAllSettings();
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
class FixedUpdateInHidenSeekPatch
{
    private static int LevelKickBufferTime = 20;
    private static Dictionary<int, int> BufferTime = new();

    public static void Postfix(PlayerControl __instance)
    {
        if (GameStates.IsNormalGame) return;

        var player = __instance;

        if (!GameStates.IsModHost) return;

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

        if (AmongUsClient.Instance.AmHost) return;

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

            if (!lowLoad)
            {
                if (!Main.DoBlockNameChange)
                    Utils.ApplySuffix(__instance);
            }
        }
        else
        {
            if (!lowLoad)
            {
                if (Options.LadderDeath.GetBool() && player.IsAlive())
                    FallFromLadder.FixedUpdate(player);
            }
        }
    }
}
