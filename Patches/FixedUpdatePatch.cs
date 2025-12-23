using System;
using InnerNet;
using TMPro;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Patches;

// Credit: EHR
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.FixedUpdate))]
public static class FixedUpdatePatch
{
    private static int NonLowLoadPlayerIndex;
    public static void Postfix()
    {
        try
        {
            KickNotJoinedPlayers();

            var amongUsClient = AmongUsClient.Instance;

            var shipStatus = ShipStatus.Instance;

            if (shipStatus)
            {
                ShipStatusFixedUpdate();
                ShipStatusUpdateVents(shipStatus);
            }

#if ANDROID

            if (GameStartManager.InstanceExists)
                GameStartManagerPatch.GameStartManagerUpdatePatch.DoPostfix(GameStartManager.Instance);
#endif

            if (HudManager.InstanceExists)
            {
                HudManager hudManager = HudManager.Instance;

                if (hudManager)
                {
                    UpdateHud(hudManager);
                    Zoom.DoZoom();
                    // HudSpritePatch.Postfix(hudManager);
                }
            }

            // try
            // {
            //     foreach (byte key in EAC.TimeSinceLastTaskCompletion.Keys.ToArray())
            //         EAC.TimeSinceLastTaskCompletion[key] += Time.fixedDeltaTime;
            // }
            // catch (Exception e) { Utils.ThrowException(e); }

            if (!PlayerControl.LocalPlayer) return;

            if (amongUsClient.IsGameStarted)
                Utils.CountAlivePlayers();

            try
            {
                if (HudManager.InstanceExists && GameStates.IsInTask && !ExileController.Instance && !AntiBlackout.SkipTasks && PlayerControl.LocalPlayer.CanUseKillButton())
                {
                    List<PlayerControl> players = PlayerControl.LocalPlayer.GetPlayersInAbilityRangeSorted(_ => true);
                    PlayerControl closest = players.Count == 0 ? null : players[0];

                    KillButton killButton = HudManager.Instance.KillButton;

                    if (killButton.currentTarget && killButton.currentTarget != closest)
                        killButton.currentTarget.ToggleHighlight(false, RoleTeamTypes.Impostor);

                    killButton.currentTarget = closest;

                    if (killButton.currentTarget)
                    {
                        killButton.currentTarget.ToggleHighlight(true, RoleTeamTypes.Impostor);
                        killButton.SetEnabled();
                    }
                    else
                        killButton.SetDisabled();
                }
            }
            catch { }

            // try
            // {
            //     if (CopyCat.Instances.Count > 0) CopyCat.Instances.RemoveAll(x => x.CopyCatPC == null);
            // }
            // catch { }

            bool lobby = GameStates.IsLobby;

            if (lobby || (Main.IntroDestroyed && GameStates.InGame && !GameStates.IsMeeting && !ExileController.Instance && !AntiBlackout.SkipTasks))
            {
                NonLowLoadPlayerIndex++;

                int count = PlayerControl.AllPlayerControls.Count;

                if (NonLowLoadPlayerIndex >= count)
                    NonLowLoadPlayerIndex = Math.Min(0, -(30 - count));

                CustomGameMode currentGameMode = Options.CurrentGameMode;

                for (var index = 0; index < count; index++)
                {
                    try
                    {
                        PlayerControl pc = PlayerControl.AllPlayerControls[index];

                        if (!pc || pc.PlayerId >= 254) continue;

                        FixedUpdateInNormalGamePatch.Postfix(pc, NonLowLoadPlayerIndex != index);

                        if (lobby) continue;

                        // CheckInvalidMovementPatch.Postfix(pc);
                    }
                    catch (Exception e) { Utils.ThrowException(e); }
                }
            }
        }
        catch (Exception e) { Utils.ThrowException(e); }
    }

    private static float Timer;
    public static void KickNotJoinedPlayers()
    {
        try
        {
            if (GameStates.IsLocalGame || !GameStates.IsLobby || !Options.KickNotJoinedPlayersRegularly.GetBool() || Main.AllPlayerControls.Length < 7) return;

            Timer += Time.fixedDeltaTime;
            if (Timer < 25f) return;
            Timer = 0f;

            AmongUsClient.Instance.KickNotJoinedPlayers();
        }
        catch (Exception e) { Utils.ThrowException(e); }
    }

    public static void ShipStatusFixedUpdate()
    {
        //Above here, all of us will execute
        if (!AmongUsClient.Instance.AmHost) return;

        //Below here, only the host performs
        if (Main.IsFixedCooldown && Main.RefixCooldownDelay >= 0)
        {
            Main.RefixCooldownDelay -= Time.fixedDeltaTime;
        }
        else if (!float.IsNaN(Main.RefixCooldownDelay))
        {
            Utils.MarkEveryoneDirtySettings();
            Main.RefixCooldownDelay = float.NaN;
            Logger.Info("Refix Cooldown", "CoolDown");
        }
    }

    public static Dictionary<byte, int> ClosestVent = [];
    public static Dictionary<byte, bool> CanUseClosestVent = [];

    private static int Count;

    public static void ShipStatusUpdateVents(ShipStatus instance)
    {
        try
        {
            if (!AmongUsClient.Instance.AmHost || !GameStates.InGame || !Main.IntroDestroyed || GameStates.IsMeeting || ExileController.Instance || AntiBlackout.SkipTasks) return;

            if (IntroCutsceneDestroyPatch.IntroDestroyTS + 5 > Utils.TimeStamp) return;

            if (Count++ < 40) return;
            Count = 0;

            var ventilationSystem = instance.Systems[SystemTypes.Ventilation].CastFast<VentilationSystem>();
            if (ventilationSystem == null) return;

            foreach (PlayerControl pc in Main.AllAlivePlayerControls)
            {
                try
                {
                    Vent closestVent = pc.GetClosestVent();
                    if (closestVent == null) continue;

                    int ventId = closestVent.Id;
                    bool canUseVent = pc.CanUseVent(ventId);

                    if (!ClosestVent.TryGetValue(pc.PlayerId, out int lastVentId) || !CanUseClosestVent.TryGetValue(pc.PlayerId, out bool lastCanUseVent))
                    {
                        ClosestVent[pc.PlayerId] = ventId;
                        CanUseClosestVent[pc.PlayerId] = canUseVent;
                        continue;
                    }

                    if (ventId != lastVentId || canUseVent != lastCanUseVent)
                        VentilationSystemDeterioratePatch.SerializeV2(ventilationSystem, pc);

                    ClosestVent[pc.PlayerId] = ventId;
                    CanUseClosestVent[pc.PlayerId] = canUseVent;
                }
                catch (Exception e) { Utils.ThrowException(e); }
            }
        }
        catch (Exception e) { Utils.ThrowException(e); }
    }

    public static TextMeshPro LowerInfoText;
    public static GameObject TempLowerInfoText;
    public static void UpdateHud(HudManager __instance)
    {
        if (!GameStates.IsModHost || __instance == null) return;

        var player = PlayerControl.LocalPlayer;
        if (player == null) return;
        //Õúüµè£Òüæ
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if ((!AmongUsClient.Instance.IsGameStarted || !GameStates.IsOnlineGame)
                && player.CanMove)
            {
                player.Collider.offset = new Vector2(0f, 127f);
            }
        }
        //Õúüµè£Òüæ×ğúÚÖñ
        if (player.Collider.offset.y == 127f)
        {
            if (!Input.GetKey(KeyCode.LeftControl) || (AmongUsClient.Instance.IsGameStarted && GameStates.IsOnlineGame))
            {
                player.Collider.offset = new Vector2(0f, -0.3636f);
            }
        }

        if (!AmongUsClient.Instance.IsGameStarted || GameStates.IsHideNSeek) return;

        Utils.CountAlivePlayers(sendLog: false, checkGameEnd: false);

        if (SetHudActivePatch.IsActive)
        {
            if (Options.CurrentGameMode == CustomGameMode.FFA)
            {
                if (LowerInfoText == null)
                {
                    TempLowerInfoText = new GameObject("CountdownText");
                    TempLowerInfoText.transform.position = new Vector3(0f, -2f, 1f);
                    LowerInfoText = TempLowerInfoText.AddComponent<TextMeshPro>();
                    //LowerInfoText.text = string.Format(GetString("CountdownText"));
                    LowerInfoText.alignment = TextAlignmentOptions.Center;
                    //LowerInfoText = Object.Instantiate(__instance.KillButton.buttonLabelText);
                    LowerInfoText.transform.parent = __instance.transform;
                    LowerInfoText.transform.localPosition = new Vector3(0, -2f, 0);
                    LowerInfoText.overflowMode = TextOverflowModes.Overflow;
                    LowerInfoText.enableWordWrapping = false;
                    LowerInfoText.color = Color.white;
                    LowerInfoText.outlineColor = Color.black;
                    LowerInfoText.outlineWidth = 20000000f;
                    LowerInfoText.fontSize = 2f;
                }
                LowerInfoText.text = FFAManager.GetHudText();
            }
            if (player.IsAlive())
            {
                // Set default
                __instance.KillButton?.OverrideText(GetString("KillButtonText"));
                __instance.ReportButton?.OverrideText(GetString("ReportButtonText"));
                __instance.SabotageButton?.OverrideText(GetString("SabotageButtonText"));

                player.GetRoleClass()?.SetAbilityButtonText(__instance, player.PlayerId);

                // Set lower info text for modded players
                if (LowerInfoText == null)
                {
                    LowerInfoText = UnityEngine.Object.Instantiate(__instance.KillButton.cooldownTimerText, __instance.transform, true);
                    LowerInfoText.alignment = TextAlignmentOptions.Center;
                    LowerInfoText.transform.localPosition = new(0, -2f, 0);
                    LowerInfoText.overflowMode = TextOverflowModes.Overflow;
                    LowerInfoText.enableWordWrapping = false;
                    LowerInfoText.color = Color.white;
                    LowerInfoText.fontSize = LowerInfoText.fontSizeMax = LowerInfoText.fontSizeMin = 2.8f;
                }
                switch (Options.CurrentGameMode)
                {
                    case CustomGameMode.Standard:
                        var roleClass = player.GetRoleClass();
                        LowerInfoText.text = roleClass?.GetLowerText(player, player, isForMeeting: Main.MeetingIsStarted, isForHud: true) ?? string.Empty;

                        LowerInfoText.text += "\n" + Spurt.GetSuffix(player, true, isformeeting: Main.MeetingIsStarted);
                        break;
                }

                LowerInfoText.enabled = LowerInfoText.text != "" && LowerInfoText.text != string.Empty;

                if ((!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay) || GameStates.IsMeeting)
                {
                    LowerInfoText.enabled = false;
                }

                if (player.CanUseKillButton())
                {
                    __instance.KillButton.ToggleVisible(player.IsAlive() && GameStates.IsInTask);
                    player.Data.Role.CanUseKillButton = true;
                }
                else
                {
                    __instance.KillButton.SetDisabled();
                    __instance.KillButton.ToggleVisible(false);
                }

                __instance.ImpostorVentButton.ToggleVisible(player.CanUseImpostorVentButton());
                player.Data.Role.CanVent = player.CanUseVents();

                // Sometimes sabotage button was visible for non-host modded clients
                if (!AmongUsClient.Instance.AmHost && !player.CanUseSabotage())
                    __instance.SabotageButton.Hide();
            }
            else
            {
                __instance.ReportButton.Hide();
                __instance.ImpostorVentButton.Hide();
                __instance.KillButton.Hide();
                __instance.AbilityButton.Show();
                __instance.AbilityButton.OverrideText(GetString(StringNames.HauntAbilityName));
            }
        }


        if (Input.GetKeyDown(KeyCode.Y) && AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay)
        {
            __instance.ToggleMapVisible(new MapOptions()
            {
                Mode = MapOptions.Modes.Sabotage,
                AllowMovementWhileMapOpen = true
            });
            if (player.AmOwner)
            {
                player.MyPhysics.inputHandler.enabled = true;
                ConsoleJoystick.SetMode_Task();
            }
        }

        if (AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame) RepairSender.enabled = false;
        if (Input.GetKeyDown(KeyCode.RightShift) && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
        {
            RepairSender.enabled = !RepairSender.enabled;
            RepairSender.Reset();
        }
        if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0)) RepairSender.Input(0);
            if (Input.GetKeyDown(KeyCode.Alpha1)) RepairSender.Input(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) RepairSender.Input(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) RepairSender.Input(3);
            if (Input.GetKeyDown(KeyCode.Alpha4)) RepairSender.Input(4);
            if (Input.GetKeyDown(KeyCode.Alpha5)) RepairSender.Input(5);
            if (Input.GetKeyDown(KeyCode.Alpha6)) RepairSender.Input(6);
            if (Input.GetKeyDown(KeyCode.Alpha7)) RepairSender.Input(7);
            if (Input.GetKeyDown(KeyCode.Alpha8)) RepairSender.Input(8);
            if (Input.GetKeyDown(KeyCode.Alpha9)) RepairSender.Input(9);
            if (Input.GetKeyDown(KeyCode.Return)) RepairSender.InputEnter();
        }
    }
}