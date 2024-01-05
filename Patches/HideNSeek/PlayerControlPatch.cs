using HarmonyLib;
using System.Collections.Generic;
using TOHE.Modules;
using static TOHE.Translator;

namespace TOHE.Patches.HideNSeek;

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

            if (!lowLoad)
            {
                if (Options.LadderDeath.GetBool() && player.IsAlive())
                    FallFromLadder.FixedUpdate(player);

                if (!Main.DoBlockNameChange)
                    Utils.ApplySuffix(__instance);
            }
        }
    }
}
