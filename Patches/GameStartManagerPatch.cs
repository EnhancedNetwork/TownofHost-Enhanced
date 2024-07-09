using AmongUs.Data;
using AmongUs.GameOptions;
using InnerNet;
using System;
using TMPro;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Translator;
using Object = UnityEngine.Object;

namespace TOHE;

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
public static class GameStartManagerUpdatePatch
{
    public static void Prefix(GameStartManager __instance)
    {
        __instance.MinPlayers = 1;
    }
}
//タイマーとコード隠し
public class GameStartManagerPatch
{
    public static float timer = 600f;
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
    public class GameStartManagerStartPatch
    {
        public static TextMeshPro HideName;
        public static TextMeshPro GameCountdown;
        public static void Postfix(GameStartManager __instance)
        {
            var temp = __instance.PlayerCounter;
            if (AmongUsClient.Instance.AmHost)
            {
                // Host have start button can be pressed
                GameCountdown = Object.Instantiate(temp, __instance.StartButton.transform);
            }
            else
            {
                // Others players have start button cannot be pressed
                GameCountdown = Object.Instantiate(temp, __instance.StartButtonClient.transform);
            }
            var gameCountdownTransformPosition = GameCountdown.transform.localPosition;
            GameCountdown.transform.localPosition = new Vector3(gameCountdownTransformPosition.x - 0.8f, gameCountdownTransformPosition.y - 0.6f, gameCountdownTransformPosition.z);
            GameCountdown.text = "";

            if (AmongUsClient.Instance.AmHost)
            {
                __instance.GameStartTextParent.GetComponent<SpriteRenderer>().sprite = null;
                __instance.StartButton.ChangeButtonText(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.StartLabel));
                __instance.GameStartText.transform.localPosition = new Vector3(__instance.GameStartText.transform.localPosition.x, 2f, __instance.GameStartText.transform.localPosition.z);
            }
            __instance.GameRoomNameCode.text = GameCode.IntToGameName(AmongUsClient.Instance.GameId);
            // Reset lobby countdown timer
            timer = 600f;

            HideName = Object.Instantiate(__instance.GameRoomNameCode, __instance.GameRoomNameCode.transform);
            HideName.text = ColorUtility.TryParseHtmlString(Main.HideColor.Value, out _)
                    ? $"<color={Main.HideColor.Value}>{Main.HideName.Value}</color>"
                    : $"<color={Main.ModColor}>{Main.HideName.Value}</color>";

            if (!AmongUsClient.Instance.AmHost) return;

            // Make Public Button
            if (ModUpdater.isBroken || (ModUpdater.hasUpdate && ModUpdater.forceUpdate) || !Main.AllowPublicRoom || !VersionChecker.IsSupported)
            {
                //__instance.HostPublicButton.activeTextColor = Palette.DisabledClear;
                //__instance.hj.color = Palette.DisabledClear;
            }

            if (GameStates.IsNormalGame)
            {
                Main.NormalOptions.ConfirmImpostor = false;
                Main.NormalOptions.SetBool(BoolOptionNames.ConfirmImpostor, false);

                if (Main.NormalOptions.KillCooldown == 0f)
                    Main.NormalOptions.KillCooldown = Main.LastKillCooldown.Value;

                AURoleOptions.SetOpt(Main.NormalOptions.Cast<IGameOptions>());
                if (AURoleOptions.ShapeshifterCooldown == 0f)
                    AURoleOptions.ShapeshifterCooldown = Main.LastShapeshifterCooldown.Value;

                if (AURoleOptions.GuardianAngelCooldown == 0f)
                    AURoleOptions.GuardianAngelCooldown = Options.DefaultAngelCooldown.GetFloat();
            }
        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    public class GameStartManagerUpdatePatch
    {
        public static float exitTimer = -1f;
        private static float minWait, maxWait;
        private static int minPlayer;
        public static bool Prefix(GameStartManager __instance)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                VanillaUpdate(__instance);
            }

            minWait = Options.MinWaitAutoStart.GetFloat();
            maxWait = Options.MaxWaitAutoStart.GetFloat();
            minPlayer = Options.PlayerAutoStart.GetInt();
            minWait = 600f - minWait * 60f;
            maxWait *= 60f;
            // Lobby code
            if (DataManager.Settings.Gameplay.StreamerMode)
            {
                __instance.GameRoomNameCode.color = new(255, 255, 255, 0);
                GameStartManagerStartPatch.HideName.enabled = true;
            }
            else
            {
                __instance.GameRoomNameCode.color = new(255, 255, 255, 255);
                GameStartManagerStartPatch.HideName.enabled = false;
            }

            if (AmongUsClient.Instance.AmHost && AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame) return false;
            if (!AmongUsClient.Instance.AmHost || !GameData.Instance) return true; // Not host or no instance or LocalGame

            if (Main.AutoStart.Value)
            {
                Main.updateTime++;
                if (Main.updateTime >= 50)
                {
                    Main.updateTime = 0;
                    if (!GameStates.IsCountDown)
                    {
                        if (Options.ImmediateAutoStart.GetBool())
                        {
                            if ((GameData.Instance.PlayerCount >= Options.StartWhenPlayersReach.GetInt() && Options.StartWhenPlayersReach.GetInt() > 1) ||
                                (timer <= Options.StartWhenTimerLowerThan.GetInt() && Options.StartWhenTimerLowerThan.GetInt() > 0))
                            {
                                BeginAutoStart(Options.ImmediateStartTimer.GetInt());
                                return false;
                            }
                        }

                        if ((GameData.Instance.PlayerCount >= minPlayer && timer <= minWait) || timer <= maxWait)
                        {
                            BeginAutoStart(Options.AutoStartTimer.GetInt());
                            return false;
                        }
                    }
                }
            }
            return false;
        }
        public static void Postfix(GameStartManager __instance)
        {
            if (!AmongUsClient.Instance) return;

            string warningMessage = "";
            if (AmongUsClient.Instance.AmHost)
            {
                bool canStartGame = true;
                List<string> mismatchedPlayerNameList = [];
                foreach (var client in AmongUsClient.Instance.allClients.ToArray())
                {
                    if (client.Character == null) continue;
                    var dummyComponent = client.Character.GetComponent<DummyBehaviour>();
                    if (dummyComponent != null && dummyComponent.enabled)
                        continue;
                    if (!MatchVersions(client.Id, true))
                    {
                        canStartGame = false;
                        mismatchedPlayerNameList.Add(Utils.ColorString(Palette.PlayerColors[client.ColorId], client.Character.Data.PlayerName));
                    }
                }
                if (!canStartGame)
                {
                    __instance.StartButton.gameObject.SetActive(false);
                    warningMessage = Utils.ColorString(Color.red, string.Format(GetString("Warning.MismatchedVersion"), string.Join(" ", mismatchedPlayerNameList), $"<color={Main.ModColor}>{Main.ModName}</color>"));
                }
            }
            else
            {
                if (MatchVersions(AmongUsClient.Instance.HostId, true) || Main.VersionCheat.Value || Main.IsHostVersionCheating)
                    exitTimer = 0;
                else
                {
                    exitTimer += Time.deltaTime;
                    if (exitTimer >= 5)
                    {
                        exitTimer = 0;
                        AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                        SceneChanger.ChangeScene("MainMenu");
                    }
                    if (exitTimer != 0)
                        warningMessage = Utils.ColorString(Color.red, string.Format(GetString("Warning.AutoExitAtMismatchedVersion"), $"<color={Main.ModColor}>{Main.ModName}</color>", Math.Round(5 - exitTimer).ToString()));
                }
            }

            __instance.RulesPresetText.text = GetString($"Preset_{OptionItem.CurrentPreset + 1}");

            // Lobby timer
            if (!GameData.Instance || AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame || !GameStates.IsVanillaServer) return;

            timer = Mathf.Max(0f, timer -= Time.deltaTime);
            int minutes = (int)timer / 60;
            int seconds = (int)timer % 60;
            string suffix = $" ({minutes:00}:{seconds:00})";
            if (timer <= 60) suffix = Utils.ColorString(Color.red, suffix);

            GameStartManagerStartPatch.GameCountdown.text = suffix;
            GameStartManagerStartPatch.GameCountdown.fontSize = 3f;
            GameStartManagerStartPatch.GameCountdown.autoSizeTextContainer = false;
        }

        private static void VanillaUpdate(GameStartManager thiz)
        {
            if (thiz == null || !GameData.Instance || !GameManager.Instance)
            {
                return;
            }
            thiz.UpdateMapImage((MapNames)GameManager.Instance.LogicOptions.MapId);
            thiz.CheckSettingsDiffs();
            thiz.StartButton.gameObject.SetActive(true);
            thiz.RulesPresetText.text = DestroyableSingleton<TranslationController>.Instance.GetString(GameOptionsManager.Instance.CurrentGameOptions.GetRulesPresetTitle());
            if (GameCode.IntToGameName(AmongUsClient.Instance.GameId) == null)
            {
                thiz.privatePublicPanelText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.LocalButton);
            }
            else if (AmongUsClient.Instance.IsGamePublic)
            {
                thiz.privatePublicPanelText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.PublicHeader);
            }
            else
            {
                thiz.privatePublicPanelText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.PrivateHeader);
            }
            thiz.HostPrivateButton.gameObject.SetActive(!AmongUsClient.Instance.IsGamePublic);
            thiz.HostPublicButton.gameObject.SetActive(AmongUsClient.Instance.IsGamePublic);
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
            {
                ClipboardHelper.PutClipboardString(GameCode.IntToGameName(AmongUsClient.Instance.GameId));
            }
            if (GameData.Instance.PlayerCount != thiz.LastPlayerCount)
            {
                thiz.LastPlayerCount = GameData.Instance.PlayerCount;
                string text = "<color=#FF0000FF>";
                if (thiz.LastPlayerCount > thiz.MinPlayers)
                {
                    text = "<color=#00FF00FF>";
                }
                if (thiz.LastPlayerCount == thiz.MinPlayers)
                {
                    text = "<color=#FFFF00FF>";
                }
                if (AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame)
                {
                    thiz.PlayerCounter.text = string.Format("{0}{1}/{2}", text, thiz.LastPlayerCount, 15);
                }
                else
                {
                    thiz.PlayerCounter.text = string.Format("{0}{1}/{2}", text, thiz.LastPlayerCount, GameManager.Instance.LogicOptions.MaxPlayers);
                }
                thiz.StartButton.SetButtonEnableState(thiz.LastPlayerCount >= thiz.MinPlayers);
                ActionMapGlyphDisplay startButtonGlyph = thiz.StartButtonGlyph;
                startButtonGlyph?.SetColor((thiz.LastPlayerCount >= thiz.MinPlayers) ? Palette.EnabledColor : Palette.DisabledClear);
                if (DestroyableSingleton<DiscordManager>.InstanceExists)
                {
                    if (AmongUsClient.Instance.AmHost && AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame)
                    {
                        DestroyableSingleton<DiscordManager>.Instance.SetInLobbyHost(thiz.LastPlayerCount, GameManager.Instance.LogicOptions.MaxPlayers, AmongUsClient.Instance.GameId);
                    }
                    else
                    {
                        DestroyableSingleton<DiscordManager>.Instance.SetInLobbyClient(thiz.LastPlayerCount, GameManager.Instance.LogicOptions.MaxPlayers, AmongUsClient.Instance.GameId);
                    }
                }
            }
            if (AmongUsClient.Instance.AmHost)
            {
                if (thiz.startState == GameStartManager.StartingStates.Countdown)
                {
                    thiz.StartButton.ChangeButtonText(GetString("Cancel"));
                    int num = Mathf.CeilToInt(thiz.countDownTimer);
                    thiz.countDownTimer -= Time.deltaTime;
                    int num2 = Mathf.CeilToInt(thiz.countDownTimer);
                    if (!thiz.GameStartTextParent.activeSelf)
                    {
                        SoundManager.Instance.PlaySound(thiz.gameStartSound, false);
                    }
                    thiz.GameStartTextParent.SetActive(true);
                    thiz.GameStartText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameStarting, num2);
                    if (num != num2)
                    {
                        PlayerControl.LocalPlayer.RpcSetStartCounter(num2);
                    }
                    if (num2 <= 0)
                    {
                        thiz.FinallyBegin();
                    }
                }
                else
                {
                    thiz.StartButton.ChangeButtonText(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.StartLabel));
                    thiz.GameStartTextParent.SetActive(false);
                    thiz.GameStartText.text = string.Empty;
                }
            }
            if (thiz.LobbyInfoPane.gameObject.activeSelf && DestroyableSingleton<HudManager>.Instance.Chat.IsOpenOrOpening)
            {
                thiz.LobbyInfoPane.DeactivatePane();
            }
            thiz.LobbyInfoPane.gameObject.SetActive(!DestroyableSingleton<HudManager>.Instance.Chat.IsOpenOrOpening);
        }

        private static void BeginAutoStart(float countdown)
        {
            _ = new LateTask(() =>
            {
                var invalidColor = Main.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId).ToArray();

                if (invalidColor.Any())
                {
                    invalidColor.Do(p => AmongUsClient.Instance.KickPlayer(p.GetClientId(), false));

                    Logger.SendInGame(GetString("Error.InvalidColorPreventStart"));
                    var msg = GetString("Error.InvalidColor");
                    msg += "\n" + string.Join(",", invalidColor.Select(p => $"{p.GetRealName()}"));
                    Utils.SendMessage(msg);
                }

                if (Options.RandomMapsMode.GetBool())
                {
                    if (GameStates.IsNormalGame)
                        Main.NormalOptions.MapId = GameStartRandomMap.SelectRandomMap();

                    else if (GameStates.IsHideNSeek)
                        Main.HideNSeekOptions.MapId = GameStartRandomMap.SelectRandomMap();
                }

                //if (GameStates.IsNormalGame && Options.IsActiveDleks)
                //{
                //    Logger.SendInGame(GetString("Warning.BrokenVentsInDleksSendInGame"));
                //    Utils.SendMessage(GetString("Warning.BrokenVentsInDleksMessage"), title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceMini), GetString("WarningTitle")));
                //}

                RPC.RpcVersionCheck();

                GameStartManager.Instance.startState = GameStartManager.StartingStates.Countdown;
                GameStartManager.Instance.countDownTimer = (countdown == 0 ? 0.2f : countdown);
                GameStartManager.Instance.StartButton.gameObject.SetActive(false);
            }, 0.8f, "Auto Start");
        }
        private static bool MatchVersions(int clientId, bool acceptVanilla = false)
        {
            if (!Main.playerVersion.TryGetValue(clientId, out var version)) return acceptVanilla;
            return Main.ForkId == version.forkId
                && Main.version.CompareTo(version.version) == 0
                && version.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})";
        }
    }
    [HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.SetText))]
    public static class HiddenTextPatch
    {
        private static void Postfix(TextBoxTMP __instance)
        {
            if (__instance.name == "GameIdText") __instance.outputText.text = new string('*', __instance.text.Length);
        }
    }
}

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
public class GameStartRandomMap
{
    public static bool Prefix(GameStartManager __instance)
    {
        var invalidColor = Main.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId).ToArray();
        if (invalidColor.Any())
        {
            Logger.SendInGame(GetString("Error.InvalidColorPreventStart"));
            var msg = GetString("Error.InvalidColor");
            msg += "\n" + string.Join(",", invalidColor.Select(p => $"{p.GetRealName()}"));
            Utils.SendMessage(msg);
            return false;
        }

        if (Options.RandomMapsMode.GetBool())
        {
            if (GameStates.IsNormalGame)
                Main.NormalOptions.MapId = SelectRandomMap();

            else if (GameStates.IsHideNSeek)
                Main.HideNSeekOptions.MapId = SelectRandomMap();
        }

        //if (GameStates.IsNormalGame && Options.IsActiveDleks)
        //{
        //    Logger.SendInGame(GetString("Warning.BrokenVentsInDleksSendInGame"));
        //    Utils.SendMessage(GetString("Warning.BrokenVentsInDleksMessage"), title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceMini), GetString("WarningTitle")));
        //}

        IGameOptions opt = GameStates.IsNormalGame
            ? Main.NormalOptions.Cast<IGameOptions>()
            : Main.HideNSeekOptions.Cast<IGameOptions>();

        if (GameStates.IsNormalGame)
        {
            var startStateIsCountdown = __instance.startState == GameStartManager.StartingStates.Countdown;
            
            if (startStateIsCountdown)
            {
                Main.NormalOptions.KillCooldown = Options.DefaultKillCooldown;
            }
            else
            {
                Options.DefaultKillCooldown = Main.NormalOptions.KillCooldown;
                Main.LastKillCooldown.Value = Main.NormalOptions.KillCooldown;
                Main.NormalOptions.KillCooldown = 0f;
            }

            AURoleOptions.SetOpt(opt);

            if (startStateIsCountdown)
            {
                AURoleOptions.ShapeshifterCooldown = Main.LastShapeshifterCooldown.Value;
            }
            else
            {
                Main.LastShapeshifterCooldown.Value = AURoleOptions.ShapeshifterCooldown;
                AURoleOptions.ShapeshifterCooldown = 0f;
                AURoleOptions.ImpostorsCanSeeProtect = false;
            }
        }

        PlayerControl.LocalPlayer.RpcSyncSettings(GameOptionsManager.Instance.gameOptionsFactory.ToBytes(opt, AprilFoolsMode.IsAprilFoolsModeToggledOn));
        RPC.RpcVersionCheck();

        __instance.ReallyBegin(false);
        return false;
    }
    public static byte SelectRandomMap()
    {
        var rand = IRandom.Instance;
        List<byte> randomMaps = [];
        /*
            The Skeld    = 0
            MIRA HQ      = 1
            Polus        = 2
            Dleks        = 3
            The Airship  = 4
            The Fungle   = 5
        */

        if (Options.UseMoreRandomMapSelection.GetBool())
        {
            if (rand.Next(1, 100) <= Options.SkeldChance.GetInt()) randomMaps.Add(0);
            if (rand.Next(1, 100) <= Options.MiraChance.GetInt()) randomMaps.Add(1);
            if (rand.Next(1, 100) <= Options.PolusChance.GetInt()) randomMaps.Add(2);
            if (rand.Next(1, 100) <= Options.DleksChance.GetInt()) randomMaps.Add(3);
            if (rand.Next(1, 100) <= Options.AirshipChance.GetInt()) randomMaps.Add(4);
            if (rand.Next(1, 100) <= Options.FungleChance.GetInt()) randomMaps.Add(5);
        }
        else
        {
            var tempRand = rand.Next(1, 100);

            if (tempRand <= Options.SkeldChance.GetInt()) randomMaps.Add(0);
            if (tempRand <= Options.MiraChance.GetInt()) randomMaps.Add(1);
            if (tempRand <= Options.PolusChance.GetInt()) randomMaps.Add(2);
            if (tempRand <= Options.DleksChance.GetInt()) randomMaps.Add(3);
            if (tempRand <= Options.AirshipChance.GetInt()) randomMaps.Add(4);
            if (tempRand <= Options.FungleChance.GetInt()) randomMaps.Add(5);
        }

        if (randomMaps.Any())
        {
            var mapsId = randomMaps.RandomElement();

            Logger.Info($"{mapsId}", "Chance Select MapId");
            return mapsId;
        }
        else
        {
            if (Options.SkeldChance.GetInt() > 0) randomMaps.Add(0);
            if (Options.MiraChance.GetInt() > 0) randomMaps.Add(1);
            if (Options.PolusChance.GetInt() > 0) randomMaps.Add(2);
            if (Options.DleksChance.GetInt() > 0) randomMaps.Add(3);
            if (Options.AirshipChance.GetInt() > 0) randomMaps.Add(4);
            if (Options.FungleChance.GetInt() > 0) randomMaps.Add(5);

            var mapsId = randomMaps.RandomElement();

            Logger.Info($"{mapsId}", "Random Select MapId");
            return mapsId;
        }
    }
}
[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.ResetStartState))]
class ResetStartStatePatch
{
    public static void Prefix()
    {
        if (GameStates.IsCountDown)
        {
            if (GameStates.IsNormalGame)
                Main.NormalOptions.KillCooldown = Options.DefaultKillCooldown;

            PlayerControl.LocalPlayer.RpcSyncSettings(GameOptionsManager.Instance.gameOptionsFactory.ToBytes(GameOptionsManager.Instance.CurrentGameOptions, AprilFoolsMode.IsAprilFoolsModeToggledOn));
        }
    }
}
[HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.GetAdjustedNumImpostors))]
class UnrestrictedNumImpostorsPatch
{
    public static bool Prefix(ref int __result)
    {
        __result = GameOptionsManager.Instance.CurrentGameOptions.NumImpostors;
        return false;
    }
}

public class GameStartManagerBeginPatch
{
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.ReallyBegin))]
    public class GameStartManagerStartPatch
    {
        public static bool Prefix(GameStartManager __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return true;

            if (__instance.startState == GameStartManager.StartingStates.Countdown)
            {
                __instance.ResetStartState();
                return false;
            }

            __instance.startState = GameStartManager.StartingStates.Countdown;
            __instance.GameSizePopup.SetActive(false);
            DataManager.Player.Onboarding.AlwaysShowMinPlayerWarning = false;
            DataManager.Player.Onboarding.ViewedMinPlayerWarning = true;
            DataManager.Player.Save();
            __instance.StartButton.gameObject.SetActive(false);
            __instance.StartButtonClient.gameObject.SetActive(false);
            __instance.GameStartTextParent.SetActive(false);
            __instance.countDownTimer = 5.0001f;
            __instance.startState = GameStartManager.StartingStates.Countdown;
            AmongUsClient.Instance.KickNotJoinedPlayers();
            return false;
        }
    }
}