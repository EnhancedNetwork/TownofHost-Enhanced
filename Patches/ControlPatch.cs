using System;
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

    private static int addonInfoIndex = -1;
    private static int addonSettingsIndex = -1;

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

            //Show Role info
            if (Input.GetKeyDown(KeyCode.F1) && GameStates.IsInGame && Options.CurrentGameMode == CustomGameMode.Standard)
            {
                try
                {
                    var role = PlayerControl.LocalPlayer.GetCustomRole();
                    var lp = PlayerControl.LocalPlayer;
                    var sb = new StringBuilder();
                    sb.Append(GetString(role.ToString()) + Utils.GetRoleMode(role) + lp.GetRoleInfo(true));
                    HudManager.Instance.ShowPopUp(sb.ToString() + "<size=0%>tohe</size>");
                }
                catch (Exception ex)
                {
                    Utils.ThrowException(ex);
                    throw;
                }
            }
            // Show Add-ons info
            if (Input.GetKeyDown(KeyCode.F2) && GameStates.IsInGame && Options.CurrentGameMode == CustomGameMode.Standard)
            {
                try
                {
                    var lp = PlayerControl.LocalPlayer;
                    if (Main.PlayerStates[lp.PlayerId].SubRoles.Count == 0) return;

                    List<string> addDes = [];
                    foreach (var subRole in Main.PlayerStates[lp.PlayerId].SubRoles.Where(x => x is not CustomRoles.Charmed).ToArray())
                    {
                        addDes.Add(GetString($"{subRole}") + Utils.GetRoleMode(subRole) + GetString($"{subRole}InfoLong"));
                    }

                    addonInfoIndex++;
                    if (addonInfoIndex >= addDes.Count) addonInfoIndex = 0;
                    HudManager.Instance.ShowPopUp(addDes[addonInfoIndex] + "<size=0%>tohe</size>");
                }
                catch (Exception ex)
                {
                    Utils.ThrowException(ex);
                    throw;
                }
            }
            if (Input.GetKeyDown(KeyCode.F3) && GameStates.IsInGame && Options.CurrentGameMode == CustomGameMode.Standard)
            {
                try
                {
                    var lp = PlayerControl.LocalPlayer;
                    var role = lp.GetCustomRole();
                    var sb = new StringBuilder();
                    if (Options.CustomRoleSpawnChances.TryGetValue(role, out var soi))
                        Utils.ShowChildrenSettings(soi, ref sb, command: false);
                    HudManager.Instance.ShowPopUp(sb.ToString().Trim());
                }
                catch (Exception ex)
                {
                    Utils.ThrowException(ex);
                }
            }
            if (Input.GetKeyDown(KeyCode.F4) && GameStates.IsInGame && Options.CurrentGameMode == CustomGameMode.Standard)
            {
                try
                {
                    var lp = PlayerControl.LocalPlayer;
                    if (Main.PlayerStates[lp.PlayerId].SubRoles.Count == 0) return;

                    var sb = new StringBuilder();
                    List<string> addSett = [];

                    foreach (var subRole in Main.PlayerStates[lp.PlayerId].SubRoles.Where(x => x is not CustomRoles.Charmed).ToArray())
                    {
                        if (Options.CustomRoleSpawnChances.TryGetValue(subRole, out var soi))
                            Utils.ShowChildrenSettings(soi, ref sb, command: false);

                        addSett.Add(sb.ToString());
                    }

                    addonSettingsIndex++;
                    if (addonSettingsIndex >= addSett.Count) addonSettingsIndex = 0;
                    HudManager.Instance.ShowPopUp(addSett[addonSettingsIndex] + "<size=0%>tohe</size>");
                }
                catch (Exception ex)
                {
                    Utils.ThrowException(ex);
                }
            }
            //Changing the resolution
            if (GetKeysDown(KeyCode.F11, KeyCode.LeftAlt))
            {
                resolutionIndex++;
                if (resolutionIndex >= resolutions.Length) resolutionIndex = 0;
                ResolutionManager.SetResolution(resolutions[resolutionIndex].Item1, resolutions[resolutionIndex].Item2, false);
                //SetResolutionManager.Postfix();
            }
            // Reloaded File Colors
            if (GetKeysDown(KeyCode.F5, KeyCode.T))
            {
                Logger.Info("Reloaded Custom Translation File Colors", "KeyCommand");
                LoadLangs();
                Logger.SendInGame("Reloaded Custom Translation File");
            }
            // Exported Custom Translation
            if (GetKeysDown(KeyCode.F5, KeyCode.X))
            {
                Logger.Info("Exported Custom Translation and Role File", "KeyCommand");
                ExportCustomTranslation();
                Main.ExportCustomRoleColors();
                Logger.SendInGame("Exported Custom Translation and Role File");
            }
            // Send Logs
            if (GetKeysDown(KeyCode.F1, KeyCode.LeftControl))
            {
                Logger.Info("Send logs", "KeyCommand");
                Utils.DumpLog();
            }
            //Copy current Settings
            if (GetKeysDown(KeyCode.LeftAlt, KeyCode.C) && !Input.GetKey(KeyCode.LeftShift) && !GameStates.IsNotJoined)
            {
                Utils.CopyCurrentSettings();
            }

            // Show Chat
            if (GetKeysDown(KeyCode.Return, KeyCode.C, KeyCode.LeftShift))
            {
                HudManager.Instance.Chat.SetVisible(true);
            }

            if (DebugModeManager.IsDebugMode && GetKeysDown(KeyCode.F8) && HudManager.Instance && !GameStates.IsMeeting && (GameStates.IsInGame || GameStates.IsLobby))
            {
                HudManager.Instance.gameObject.SetActive(!HudManager.Instance.gameObject.active);
            }

            // Get Position
            if (Input.GetKeyDown(KeyCode.P) && PlayerControl.LocalPlayer != null)
            {
                Logger.Info(PlayerControl.LocalPlayer.GetTruePosition().ToString(), "GetLocalPlayerPos GetTruePosition()");
                Logger.Info(PlayerControl.LocalPlayer.transform.position.ToString(), "GetLocalPlayerPos transform.position");
            }

            // ############################################################################################################
            // ================================================= Only Host ================================================
            // ############################################################################################################
            if (!AmongUsClient.Instance.AmHost) return;

            // Forse end game
            if (GetKeysDown(KeyCode.Return, KeyCode.L, KeyCode.LeftShift) && GameStates.IsInGame)
            {
                NameNotifyManager.Notice.Clear();
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
                GameManager.Instance.LogicFlow.CheckEndCriteria();
                GameEndCheckerForNormal.GameIsEnded = true;
                if (GameStates.IsHideNSeek)
                {
                    GameEndCheckerForNormal.StartEndGame(GameOverReason.ImpostorDisconnect);
                }
            }

            //Search Bar In Menu "Press Enter" alternative function
            if (GetKeysDown(KeyCode.Return) && GameSettingMenuPatch.Instance != null && GameSettingMenuPatch.Instance.isActiveAndEnabled == true)
            {
                GameSettingMenuPatch._SearchForOptions?.Invoke();
            }

            // Force start/end meeting
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
                    if (Utils.GetTimeStamp() - Main.LastMeetingEnded < 2) return;
                    PlayerControl.LocalPlayer.NoCheckStartMeeting(null, force: true);
                }
            }
            // Forse start game       
            if (Input.GetKeyDown(KeyCode.LeftShift) && GameStates.IsCountDown && !HudManager.Instance.Chat.IsOpenOrOpening)
            {
                var invalidColor = Main.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId).ToArray();
                if (invalidColor.Any())
                {
                    GameStartManager.Instance.ResetStartState(); //Hope this works
                    Logger.SendInGame(GetString("Error.InvalidColorPreventStart"));
                    Logger.Info("Invalid Color Detected on force start!", "KeyCommand");
                }
                else
                {
                    Logger.Info("Countdown timer changed to 0", "KeyCommand");
                    GameStartManager.Instance.countDownTimer = 0;
                }
            }

            // Cancel Start count down
            if (Input.GetKeyDown(KeyCode.C) && GameStates.IsCountDown && GameStates.IsLobby)
            {
                Logger.Info("Reset Countdown", "KeyCommand");
                GameStartManager.Instance.ResetStartState();
                Logger.SendInGame(GetString("CancelStartCountDown"));
            }
            if (GetKeysDown(KeyCode.N, KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift))
            {
                Main.isChatCommand = true;
                Utils.ShowActiveSettings();
            }

            // Reset All TOHE Setting To Default
            if (GameStates.IsLobby && GetKeysDown(KeyCode.LeftControl, KeyCode.LeftShift, KeyCode.Return, KeyCode.Delete))
            {
                OptionItem.AllOptions.ToArray().Where(x => x.Id > 0).Do(x => x.SetValueNoRpc(x.DefaultValue));
                Logger.SendInGame(GetString("RestTOHOSetting"));
            }

            // Host kill self
            if (GetKeysDown(KeyCode.LeftControl, KeyCode.LeftShift, KeyCode.E, KeyCode.Return) && GameStates.IsInGame)
            {
                PlayerControl.LocalPlayer.Data.IsDead = true;
                PlayerControl.LocalPlayer.SetDeathReason(PlayerState.DeathReason.etc);
                PlayerControl.LocalPlayer.SetRealKiller(PlayerControl.LocalPlayer);
                Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].SetDead();
                PlayerControl.LocalPlayer.RpcExileV2();
                MurderPlayerPatch.AfterPlayerDeathTasks(PlayerControl.LocalPlayer, PlayerControl.LocalPlayer, GameStates.IsMeeting);

                Utils.SendMessage(GetString("HostKillSelfByCommand"), title: $"<color=#ff0000>{GetString("DefaultSystemMessageTitle")}</color>");
            }

            // Show Intro
            if (GetKeysDown(KeyCode.Return, KeyCode.G, KeyCode.LeftShift) && GameStates.IsInGame && PlayerControl.LocalPlayer.FriendCode.GetDevUser().DeBug)
            {
                HudManager.Instance.StartCoroutine(HudManager.Instance.CoFadeFullScreen(Color.clear, Color.black));
                HudManager.Instance.StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoShowIntro());
            }

            // Whether the toggle log is also output in the game
            if (GetKeysDown(KeyCode.F2, KeyCode.LeftControl))
            {
                Logger.isAlsoInGame = !Logger.isAlsoInGame;
                Logger.SendInGame($"In-game output log：{Logger.isAlsoInGame}");
            }

            // ############################################################################################################
            // ========================================== Only Host and in Debug ==========================================
            // ############################################################################################################
            if (!DebugModeManager.IsDebugMode) return;

            if (GetKeysDown(KeyCode.E, KeyCode.F, KeyCode.LeftControl))
            {
                CriticalErrorManager.SetCriticalError("Test AntiBlackout", true);
            }

            // Kill Flash
            if (GetKeysDown(KeyCode.Return, KeyCode.F, KeyCode.LeftShift))
            {
                Utils.FlashColor(new(1f, 0f, 0f, 0.3f));
                if (Constants.ShouldPlaySfx()) RPC.PlaySound(PlayerControl.LocalPlayer.PlayerId, Sounds.KillSound);
            }

            // Clear self vote only in local game
            if (GetKeysDown(KeyCode.Return, KeyCode.V, KeyCode.LeftShift) && GameStates.IsMeeting && !GameStates.IsOnlineGame)
            {
                MeetingHud.Instance.RpcClearVote(AmongUsClient.Instance.ClientId);
            }

            // Open all the doors in Airship map
            if (GetKeysDown(KeyCode.Return, KeyCode.D, KeyCode.LeftShift) && GameStates.IsInGame)
            {
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 79);
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 80);
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 81);
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 82);
            }

            // Set kill cooldown to 0 seconds
            if (GetKeysDown(KeyCode.Return, KeyCode.K, KeyCode.LeftShift) && GameStates.IsInGame)
            {
                PlayerControl.LocalPlayer.SetKillTimer(0f);
            }

            // Complete all your tasks
            if (GetKeysDown(KeyCode.Return, KeyCode.T, KeyCode.LeftShift) && GameStates.IsInGame)
            {
                foreach (var task in PlayerControl.LocalPlayer.myTasks.ToArray())
                    PlayerControl.LocalPlayer.RpcCompleteTask(task.Id);
            }

            // Force sync custom settings
            if (Input.GetKeyDown(KeyCode.Y))
            {
                RPC.SyncCustomSettingsRPC();
                Logger.SendInGame(GetString("SyncCustomSettingsRPC"));
            }

            // Task number display toggle
            if (Input.GetKeyDown(KeyCode.Equals))
            {
                Main.VisibleTasksCount = !Main.VisibleTasksCount;
                DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage($"VisibleTaskCount has been changed to {Main.VisibleTasksCount}");
            }

            // All Players enter Vent
            if (Input.GetKeyDown(KeyCode.C) && !GameStates.IsLobby)
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (!pc.AmOwner) pc.MyPhysics.RpcEnterVent(2);
                }
            }

            // All Players exit Vent
            if (Input.GetKeyDown(KeyCode.B))
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (!pc.AmOwner) pc.MyPhysics.RpcExitVent(2);
                }
            }

            // Teleport all Players to the Host
            if (GetKeysDown(KeyCode.LeftShift, KeyCode.V, KeyCode.Return) && !GameStates.IsLobby && PlayerControl.LocalPlayer.FriendCode.GetDevUser().DeBug)
            {
                Vector2 pos = PlayerControl.LocalPlayer.NetTransform.transform.position;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (!pc.AmOwner)
                    {
                        pc.RpcTeleport(pos);
                        pos.x += 0.5f;
                    }
                }
            }

            // Clear Vent
            if (Input.GetKeyDown(KeyCode.N))
            {
                VentilationSystem.Update(VentilationSystem.Operation.StartCleaning, 0);
            }
        }
        catch (Exception error)
        {
            Utils.ThrowException(error);
        }
    }

    private static bool GetKeysDown(params KeyCode[] keys)
    {
        if (keys.Any(Input.GetKeyDown) && keys.All(Input.GetKey))
        {
            Logger.Info($"Shortcut Key：{keys.First(Input.GetKeyDown)} in [{string.Join(",", keys)}]", "GetKeysDown");
            return true;
        }
        return false;
    }

    private static bool ORGetKeysDown(params KeyCode[] keys) => keys.Any(Input.GetKeyDown);
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
