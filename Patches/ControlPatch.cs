using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHE.Modules;
using TOHE.Patches;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(ControllerManager), nameof(ControllerManager.Update))]
internal class ControllerManagerUpdatePatch
{
    private static readonly (int, int)[] resolutions = [(480, 270), (640, 360), (800, 450), (1280, 720), (1600, 900), (1920, 1080)];
    private static int resolutionIndex = 0;

    public static List<string> addDes = [];
    public static int addonIndex = -1;

    public static void Postfix(/*ControllerManager __instance*/)
    {
        try
        {
            if (!RehostManager.IsAutoRehostDone && GetKeysDown(KeyCode.LeftShift, KeyCode.C))
            {
                Logger.Info("User canceled Auto Rehost!", "ControllerManager");
                RehostManager.IsAutoRehostDone = true;
            }

            if (EndGameManagerPatch.IsRestarting && GetKeysDown(KeyCode.LeftShift, KeyCode.C))
            {
                Logger.Info("User canceled Auto Play Again!", "ControllerManager");
                EndGameManagerPatch.IsRestarting = false;
            }
            //切换自定义设置的页面
            if (GameStates.IsLobby)
            {
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    OptionShower.Next();
                }
                for (var i = 0; i < 9; i++)
                {
                    if (ORGetKeysDown(KeyCode.Alpha1 + i, KeyCode.Keypad1 + i) && OptionShower.pages.Count >= i + 1)
                        OptionShower.currentPage = i;
                }
            }
            //捕捉全屏快捷键
            //if (GetKeysDown(KeyCode.LeftAlt, KeyCode.Return))
            //{
            //    _ = new LateTask(SetResolutionManager.Postfix, 0.01f, "Fix Button Position");
            //}
            //职业介绍
            if (Input.GetKeyDown(KeyCode.F1) && GameStates.InGame && Options.CurrentGameMode == CustomGameMode.Standard)
            {
                try
                {
                    var role = PlayerControl.LocalPlayer.GetCustomRole();
                    var lp = PlayerControl.LocalPlayer;
                    var sb = new StringBuilder();
                    sb.Append(GetString(role.ToString()) + Utils.GetRoleMode(role) + lp.GetRoleInfo(true));
                    if (Options.CustomRoleSpawnChances.TryGetValue(role, out var opt))
                        Utils.ShowChildrenSettings(Options.CustomRoleSpawnChances[role], ref sb, command: true);
                    HudManager.Instance.ShowPopUp(sb.ToString());
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "ControllerManagerUpdatePatch");
                    throw;
                }
            }
            //附加职业介绍
            if (Input.GetKeyDown(KeyCode.F2) && GameStates.InGame && Options.CurrentGameMode == CustomGameMode.Standard)
            {
                try
                {
                    var role = PlayerControl.LocalPlayer.GetCustomRole();
                    var lp = PlayerControl.LocalPlayer;
                    if (Main.PlayerStates[lp.PlayerId].SubRoles.Count == 0) return;

                    addDes = [];
                    foreach (var subRole in Main.PlayerStates[lp.PlayerId].SubRoles.Where(x => x is not CustomRoles.Charmed).ToArray())
                    {
                        addDes.Add(GetString($"{subRole}") + Utils.GetRoleMode(subRole) + GetString($"{subRole}InfoLong"));
                    }
                    if (CustomRolesHelper.RoleExist(CustomRoles.Ntr) && (role is not CustomRoles.GM and not CustomRoles.Ntr))
                    {
                        addDes.Add(GetString($"Lovers") + Utils.GetRoleMode(CustomRoles.Lovers) + GetString($"LoversInfoLong"));
                    }

                    addonIndex++;
                    if (addonIndex >= addDes.Count) addonIndex = 0;
                    HudManager.Instance.ShowPopUp(addDes[addonIndex]);
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "ControllerManagerUpdatePatch");
                    throw;
                }
            }
            //更改分辨率
            if (GetKeysDown(KeyCode.F11, KeyCode.LeftAlt))
            {
                resolutionIndex++;
                if (resolutionIndex >= resolutions.Length) resolutionIndex = 0;
                ResolutionManager.SetResolution(resolutions[resolutionIndex].Item1, resolutions[resolutionIndex].Item2, false);
                //SetResolutionManager.Postfix();
            }
            //重新加载自定义翻译
            if (GetKeysDown(KeyCode.F5, KeyCode.T))
            {
                Logger.Info("Reloaded Custom Translation File Colors", "KeyCommand");
                LoadLangs();
                Logger.SendInGame("Reloaded Custom Translation File");
            }
            if (GetKeysDown(KeyCode.F5, KeyCode.X))
            {
                Logger.Info("Exported Custom Translation and Role File", "KeyCommand");
                ExportCustomTranslation();
                Main.ExportCustomRoleColors();
                Logger.SendInGame("Exported Custom Translation and Role File");
            }
            //日志文件转储
            if (GetKeysDown(KeyCode.F1, KeyCode.LeftControl))
            {
                Logger.Info("输出日志", "KeyCommand");
                Utils.DumpLog();
            }
            //将当前设置复制为文本
            if (GetKeysDown(KeyCode.LeftAlt, KeyCode.C) && !Input.GetKey(KeyCode.LeftShift) && !GameStates.IsNotJoined)
            {
                Utils.CopyCurrentSettings();
            }
            //打开游戏目录
            if (GetKeysDown(KeyCode.F10))
            {
                System.Diagnostics.Process.Start(Environment.CurrentDirectory);
            }

            if (GetKeysDown(KeyCode.Return, KeyCode.C, KeyCode.LeftShift))
            {
                HudManager.Instance.Chat.SetVisible(true);
            }
            //获取现在的坐标
            if (Input.GetKeyDown(KeyCode.P))
            {
                Logger.Info(PlayerControl.LocalPlayer.GetTruePosition().ToString(), "GetLocalPlayerPos GetTruePosition()");
                Logger.Info(PlayerControl.LocalPlayer.transform.position.ToString(), "GetLocalPlayerPos transform.position");
            }


            //-- 下面是主机专用的命令--//
            if (!AmongUsClient.Instance.AmHost) return;
            // 强制显示聊天框
            //强制结束游戏
            if (GetKeysDown(KeyCode.Return, KeyCode.L, KeyCode.LeftShift) && GameStates.IsInGame)
            {
                NameNotifyManager.Notice.Clear();
                Utils.DoNotifyRoles(ForceLoop: true);
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
                GameManager.Instance.LogicFlow.CheckEndCriteria();
                if (GameStates.IsHideNSeek)
                {
                    GameEndCheckerForNormal.StartEndGame(GameOverReason.ImpostorDisconnect);
                }
            }
            // Forse start/end Meeting
            if (GetKeysDown(KeyCode.Return, KeyCode.M, KeyCode.LeftShift) && GameStates.IsInGame)
            {
                if (GameStates.IsHideNSeek) return;

                if (GameStates.IsMeeting)
                {
                    foreach (var pva in MeetingHud.Instance.playerStates)
                    {
                        if (pva == null) continue;

                        if (pva.VotedFor < 253)
                            MeetingHud.Instance.RpcClearVote(pva.TargetPlayerId);
                    }
                    List<MeetingHud.VoterState> statesList = [];
                    MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), null, true);
                    MeetingHud.Instance.RpcClose();
                }
                else
                {
                    PlayerControl.LocalPlayer.NoCheckStartMeeting(null, force: true);
                }
            }
            //立即开始        
            if (Input.GetKeyDown(KeyCode.LeftShift) && GameStates.IsCountDown && !HudManager.Instance.Chat.IsOpenOrOpening)
            {
                var invalidColor = Main.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId).ToArray();
                if (invalidColor.Length > 0)
                {
                    GameStartManager.Instance.ResetStartState(); //Hope this works
                    Logger.SendInGame(GetString("Error.InvalidColorPreventStart"));
                    Logger.Info("Invalid Color Detected on force start!", "KeyCommand");
                }
                else
                {
                    Logger.Info("倒计时修改为0", "KeyCommand");
                    GameStartManager.Instance.countDownTimer = 0;
                }
            }

            //倒计时取消
            if (Input.GetKeyDown(KeyCode.C) && GameStates.IsCountDown)
            {
                Logger.Info("重置倒计时", "KeyCommand");
                GameStartManager.Instance.ResetStartState();
                Logger.SendInGame(GetString("CancelStartCountDown"));
            }
            //显示当前有效设置的说明
            if (GetKeysDown(KeyCode.N, KeyCode.LeftShift, KeyCode.LeftControl))
            {
                Main.isChatCommand = true;
                Utils.ShowActiveSettingsHelp();
            }
            //显示当前有效设置
            if (GetKeysDown(KeyCode.N, KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift))
            {
                Main.isChatCommand = true;
                Utils.ShowActiveSettings();
            }
            // Reset All TOHE Setting To Default
            if (GameStates.IsLobby && GetKeysDown(KeyCode.Delete, KeyCode.LeftControl))
            {
                OptionItem.AllOptions.ToArray().Where(x => x.Id > 0).Do(x => x.SetValueNoRpc(x.DefaultValue));
                Logger.SendInGame(GetString("RestTOHESetting"));
                if (!(!AmongUsClient.Instance.AmHost || PlayerControl.AllPlayerControls.Count <= 1 || (AmongUsClient.Instance.AmHost == false && PlayerControl.LocalPlayer == null)))
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RestTOHESetting, SendOption.Reliable, -1);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }
                OptionShower.GetText();
            }
            //放逐自己
            if (GetKeysDown(KeyCode.Return, KeyCode.E, KeyCode.LeftShift) && GameStates.IsInGame)
            {
                PlayerControl.LocalPlayer.Data.IsDead = true;
                Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].deathReason = PlayerState.DeathReason.etc;
                Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].SetDead();
                PlayerControl.LocalPlayer.RpcExileV2();

                Utils.SendMessage(GetString("HostKillSelfByCommand"), title: $"<color=#ff0000>{GetString("DefaultSystemMessageTitle")}</color>");
            }

            if (GetKeysDown(KeyCode.Return, KeyCode.G, KeyCode.LeftShift) && GameStates.IsInGame && PlayerControl.LocalPlayer.FriendCode.GetDevUser().DeBug)
            {
                HudManager.Instance.StartCoroutine(HudManager.Instance.CoFadeFullScreen(Color.clear, Color.black));
                HudManager.Instance.StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoShowIntro());
            }

            //切换日志是否也在游戏中输出
            if (GetKeysDown(KeyCode.F2, KeyCode.LeftControl))
            {
                Logger.isAlsoInGame = !Logger.isAlsoInGame;
                Logger.SendInGame($"游戏中输出日志：{Logger.isAlsoInGame}");
            }

            //--下面是调试模式的命令--//
            if (!DebugModeManager.IsDebugMode) return;

            //杀戮闪烁
            if (GetKeysDown(KeyCode.Return, KeyCode.F, KeyCode.LeftShift))
            {
                Utils.FlashColor(new(1f, 0f, 0f, 0.3f));
                if (Constants.ShouldPlaySfx()) RPC.PlaySound(PlayerControl.LocalPlayer.PlayerId, Sounds.KillSound);
            }

            //实名投票
            if (GetKeysDown(KeyCode.Return, KeyCode.V, KeyCode.LeftShift) && GameStates.IsMeeting && !GameStates.IsOnlineGame)
            {
                MeetingHud.Instance.RpcClearVote(AmongUsClient.Instance.ClientId);
            }

            //打开飞艇所有的门
            if (GetKeysDown(KeyCode.Return, KeyCode.D, KeyCode.LeftShift) && GameStates.IsInGame)
            {
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 79);
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 80);
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 81);
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 82);
            }

            //将击杀冷却设定为0秒
            if (GetKeysDown(KeyCode.Return, KeyCode.K, KeyCode.LeftShift) && GameStates.IsInGame)
            {
                PlayerControl.LocalPlayer.Data.Object.SetKillTimer(0f);
            }

            //完成你的所有任务
            if (GetKeysDown(KeyCode.Return, KeyCode.T, KeyCode.LeftShift) && GameStates.IsInGame)
            {
                foreach (var task in PlayerControl.LocalPlayer.myTasks.ToArray())
                    PlayerControl.LocalPlayer.RpcCompleteTask(task.Id);
            }

            //同步设置
            if (Input.GetKeyDown(KeyCode.Y))
            {
                RPC.SyncCustomSettingsRPC();
                Logger.SendInGame(GetString("SyncCustomSettingsRPC"));
            }

            //入门测试
            if (Input.GetKeyDown(KeyCode.G))
            {
                HudManager.Instance.StartCoroutine(HudManager.Instance.CoFadeFullScreen(Color.clear, Color.black));
                HudManager.Instance.StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoShowIntro());
            }
            //任务数显示切换
            if (Input.GetKeyDown(KeyCode.Equals))
            {
                Main.VisibleTasksCount = !Main.VisibleTasksCount;
                DestroyableSingleton<HudManager>.Instance.Notifier.AddItem("VisibleTaskCountが" + Main.VisibleTasksCount.ToString() + "に変更されました。");
            }

            //マスゲーム用コード
            if (Input.GetKeyDown(KeyCode.C) && !GameStates.IsLobby)
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (!pc.AmOwner) pc.MyPhysics.RpcEnterVent(2);
                }
            }
            if (Input.GetKeyDown(KeyCode.V) && !GameStates.IsLobby)
            {
                Vector2 pos = PlayerControl.LocalPlayer.NetTransform.transform.position;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (!pc.AmOwner)
                    {
                        pc.NetTransform.RpcSnapTo(pos);
                        pos.x += 0.5f;
                    }
                }
            }
            /*if (Input.GetKeyDown(KeyCode.L))
              {
                  Logger.Info($"{Utils.IsActive(SystemTypes.Reactor)}", "Check SystemType.Reactor");
                  Logger.Info($"{Utils.IsActive(SystemTypes.LifeSupp)}", "Check SystemTypes.LifeSupp");
                  Logger.Info($"{Utils.IsActive(SystemTypes.Laboratory)}", "Check SystemTypes.Laboratory");
                  Logger.Info($"{Utils.IsActive(SystemTypes.HeliSabotage)}", "Check SystemTypes.HeliSabotage");
                  Logger.Info($"{Utils.IsActive(SystemTypes.Comms)}", "Check SystemTypes.Comms");
                  Logger.Info($"{Utils.IsActive(SystemTypes.Electrical)}", "Check SystemTypes.Electrical");
                  Logger.Info($"{Utils.IsActive(SystemTypes.MushroomMixupSabotage)}", "Check SystemTypes.MushroomMixupSabotage");
              }*/
            if (Input.GetKeyDown(KeyCode.B))
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (!pc.AmOwner) pc.MyPhysics.RpcExitVent(2);
                }
            }
            if (Input.GetKeyDown(KeyCode.N))
            {
                VentilationSystem.Update(VentilationSystem.Operation.StartCleaning, 0);
            }
            //マスゲーム用コード終わり
        }
        catch (Exception error)
        {
            Logger.Warn($"Error when using keyboard shortcuts: {error}", "ControllerManagerUpdatePatch.Postfix");
        }
    }

    private static bool GetKeysDown(params KeyCode[] keys)
    {
        if (keys.Any(k => Input.GetKeyDown(k)) && keys.All(k => Input.GetKey(k)))
        {
            Logger.Info($"Shortcut Key：{keys.First(k => Input.GetKeyDown(k))} in [{string.Join(",", keys)}]", "GetKeysDown");
            return true;
        }
        return false;
    }

    private static bool ORGetKeysDown(params KeyCode[] keys) => keys.Any(k => Input.GetKeyDown(k));
}

[HarmonyPatch(typeof(ConsoleJoystick), nameof(ConsoleJoystick.HandleHUD))]
internal class ConsoleJoystickHandleHUDPatch
{
    public static void Postfix()
    {
        HandleHUDPatch.Postfix(ConsoleJoystick.player);
    }
}
[HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.HandleHud))]
internal class KeyboardJoystickHandleHUDPatch
{
    public static void Postfix()
    {
        HandleHUDPatch.Postfix(KeyboardJoystick.player);
    }
}

internal class HandleHUDPatch
{
    public static void Postfix(Rewired.Player player)
    {
        if (!GameStates.IsInGame) return;
        if (GameStates.IsHideNSeek) return;
        if (player.GetButtonDown(8) && // 8:キルボタンのactionId
        PlayerControl.LocalPlayer.Data?.Role?.IsImpostor == false &&
        PlayerControl.LocalPlayer.CanUseKillButton())
        {
            DestroyableSingleton<HudManager>.Instance.KillButton.DoClick();
        }
        if (player.GetButtonDown(50) && // 50:インポスターのベントボタンのactionId
        PlayerControl.LocalPlayer.Data?.Role?.IsImpostor == false &&
        PlayerControl.LocalPlayer.CanUseImpostorVentButton())
        {
            DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.DoClick();
        }
    }
}