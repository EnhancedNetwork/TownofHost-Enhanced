using AmongUs.Data;
using AmongUs.GameOptions;
using InnerNet;
using System;
using TMPro;
using TOHE.Patches;
using UnityEngine;
using static TOHE.Translator;
using Object = UnityEngine.Object;

namespace TOHE;

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
public static class GameStartManagerMinPlayersPatch
{
    public static void Prefix(GameStartManager __instance)
    {
        __instance.MinPlayers = 1;
    }
}
public class GameStartManagerPatch
{
    public static float timer = 600f;
    public static long joinedTime = Utils.GetTimeStamp();
    private static Vector3 GameStartTextlocalPosition;
    private static TextMeshPro warningText;
    private static TextMeshPro timerText;
    private static PassiveButton cancelButton;
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
    public class GameStartManagerStartPatch
    {
        public static TextMeshPro HideName;
        public static void Postfix(GameStartManager __instance)
        {
            GameStartManagerUpdatePatch.AlredyBegin = false;
            __instance.GameRoomNameCode.text = GameCode.IntToGameName(AmongUsClient.Instance.GameId);
            // Reset lobby countdown timer
            timer = 600f;
            joinedTime = Utils.GetTimeStamp();

            HideName = Object.Instantiate(__instance.GameRoomNameCode, __instance.GameRoomNameCode.transform);
            HideName.text = ColorUtility.TryParseHtmlString(Main.HideColor.Value, out _)
                    ? $"<color={Main.HideColor.Value}>{Main.HideName.Value}</color>"
                    : $"<color={Main.ModColor}>{Main.HideName.Value}</color>";

            warningText = Object.Instantiate(__instance.GameStartText, __instance.transform.parent);
            warningText.name = "WarningText";
            warningText.transform.localPosition = new(0f, __instance.transform.localPosition.y + 3f, -1f);
            warningText.gameObject.SetActive(false);

            if (AmongUsClient.Instance.AmHost)
            {
                timerText = Object.Instantiate(__instance.PlayerCounter, __instance.StartButton.transform.parent);
            }
            else
            {
                timerText = Object.Instantiate(__instance.PlayerCounter, __instance.StartButtonClient.transform.parent);
            }
            timerText.fontSize = 6.2f;
            timerText.autoSizeTextContainer = true;
            timerText.name = "Timer";
            timerText.DestroyChildren();
            timerText.DestroySubMeshObjects();
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.outlineColor = Color.black;
            timerText.outlineWidth = 0.40f;
            timerText.hideFlags = HideFlags.None;
            //timerText.transform.localPosition += new Vector3(-0.5f, -2.6f, 0f);
            timerText.transform.localPosition += new Vector3(-0.55f, -0.25f, 0f);
            timerText.transform.localScale = new(0.7f, 0.7f, 1f);
            timerText.gameObject.SetActive(AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame && GameStates.IsVanillaServer);

            cancelButton = Object.Instantiate(__instance.StartButton, __instance.transform);
            cancelButton.name = "CancelButton";
            var cancelLabel = cancelButton.buttonText;
            cancelLabel.DestroyTranslator();
            cancelLabel.text = GetString("Cancel");
            //cancelButton.transform.localScale = new(0.5f, 0.5f, 1f);
            var cancelButtonInactiveRenderer = cancelButton.inactiveSprites.GetComponent<SpriteRenderer>();
            cancelButtonInactiveRenderer.color = new(0.8f, 0f, 0f, 1f);
            var cancelButtonActiveRenderer = cancelButton.activeSprites.GetComponent<SpriteRenderer>();
            cancelButtonActiveRenderer.color = Color.red;
            var cancelButtonInactiveShine = cancelButton.inactiveSprites.transform.Find("Shine");
            if (cancelButtonInactiveShine)
            {
                cancelButtonInactiveShine.gameObject.SetActive(false);
            }
            cancelButton.activeTextColor = cancelButton.inactiveTextColor = Color.white;
            //cancelButton.transform.localPosition = new(2f, 0.13f, 0f);
            GameStartTextlocalPosition = __instance.GameStartText.transform.localPosition;
            cancelButton.OnClick = new();
            cancelButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                __instance.ResetStartState();
            }));
            cancelButton.gameObject.SetActive(false);

            if (!AmongUsClient.Instance.AmHost) return;

            if (GameStates.IsNormalGame)
            {
                Main.NormalOptions.ConfirmImpostor = false;
                Main.NormalOptions.SetBool(BoolOptionNames.ConfirmImpostor, false);

                if (Main.NormalOptions.KillCooldown == 0f)
                    Main.NormalOptions.KillCooldown = Main.LastKillCooldown.Value;

                AURoleOptions.SetOpt(Main.NormalOptions.CastFast<IGameOptions>());
                if (AURoleOptions.ShapeshifterCooldown == 0f)
                    AURoleOptions.ShapeshifterCooldown = Main.LastShapeshifterCooldown.Value;

                if (AURoleOptions.GuardianAngelCooldown == 0f)
                    AURoleOptions.GuardianAngelCooldown = Main.LastGuardianAngelCooldown.Value;
            }
        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    public static class GameStartManagerUpdatePatch
    {
        public static bool AlredyBegin = false;
        private static bool update = false;
        private static string currentText = "";
        public static float exitTimer = -1f;
        private static float minWait, maxWait;
        private static int minPlayer;
        public static void Prefix(GameStartManager __instance)
        {
            if (__instance == null || LobbyBehaviour.Instance == null) return;
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

            update = GameData.Instance?.PlayerCount != __instance.LastPlayerCount;
            if (!AmongUsClient.Instance.AmHost || !GameData.Instance || AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame) return; // Not host or no instance or LocalGame

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
                                BeginGameAutoStart(Options.ImmediateStartTimer.GetInt());
                                return;
                            }
                        }

                        if ((GameData.Instance.PlayerCount >= minPlayer && timer <= minWait) || timer <= maxWait)
                        {
                            BeginGameAutoStart(Options.AutoStartTimer.GetInt());
                            return;
                        }

                        if (joinedTime + Options.StartWhenTimePassed.GetInt() < Utils.GetTimeStamp())
                        {
                            BeginGameAutoStart(Options.AutoStartTimer.GetInt());
                            return;
                        }
                    }
                }
            }
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
                cancelButton.gameObject.SetActive(__instance.startState == GameStartManager.StartingStates.Countdown);
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
            if (warningMessage == "")
            {
                warningText.gameObject.SetActive(false);
            }
            else
            {
                warningText.text = warningMessage;
                warningText.gameObject.SetActive(true);
            }

            if (AmongUsClient.Instance.AmHost)
            {
                __instance.GameStartText.transform.localPosition = new Vector3(__instance.GameStartText.transform.localPosition.x, 2f, __instance.GameStartText.transform.localPosition.z);
            }
            else
            {
                __instance.GameStartText.transform.localPosition = GameStartTextlocalPosition;
            }

            __instance.RulesPresetText.text = GetString($"Preset_{OptionItem.CurrentPreset + 1}");

            // Lobby timer
            if (!GameData.Instance || AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame || !GameStates.IsVanillaServer) return;

            if (update) currentText = __instance.PlayerCounter.text;

            timer = Mathf.Max(0f, timer -= Time.deltaTime);
            int minutes = (int)timer / 60;
            int seconds = (int)timer % 60;
            string countDown = $"{minutes:00}:{seconds:00}";
            if (timer <= 60) countDown = Utils.ColorString((int)timer % 2 == 0 ? Color.yellow : Color.red, countDown);
            timerText.text = countDown;
        }
        private static void BeginGameAutoStart(float countdown)
        {
            if (AlredyBegin) return;
            AlredyBegin = true;

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

                GameStartManagerBeginGamePatch.DoTasksForBeginGame();

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
}
[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
public class GameStartManagerBeginGamePatch
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

        DoTasksForBeginGame();

        __instance.ReallyBegin(false);
        return false;
    }
    public static void DoTasksForBeginGame()
    {
        if (Options.NoGameEnd.GetBool())
            Logger.SendInGame(string.Format(GetString("Warning.NoGameEndIsEnabled"), GetString("NoGameEnd")));

        if (Options.RandomMapsMode.GetBool())
        {
            var mapId = SelectRandomMap();

            if (GameStates.IsNormalGame)
            {
                Main.NormalOptions.MapId = mapId;
            }
            else if (GameStates.IsHideNSeek)
            {
                Main.HideNSeekOptions.MapId = mapId;
            }

            if (mapId == 3) // Dleks map
                CreateOptionsPickerPatch.SetDleks = true;
            else
                CreateOptionsPickerPatch.SetDleks = false;
        }
        else if (CreateOptionsPickerPatch.SetDleks)
        {
            if (GameStates.IsNormalGame)
                Main.NormalOptions.MapId = 3;

            else if (GameStates.IsHideNSeek)
                Main.HideNSeekOptions.MapId = 3;
        }

        //if (GameStates.IsNormalGame && Options.IsActiveDleks)
        //{
        //    Logger.SendInGame(GetString("Warning.BrokenVentsInDleksSendInGame"));
        //    Utils.SendMessage(GetString("Warning.BrokenVentsInDleksMessage"), title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceMini), GetString("WarningTitle")));
        //}

        IGameOptions opt = GameStates.IsNormalGame
            ? Main.NormalOptions.CastFast<IGameOptions>()
            : Main.HideNSeekOptions.CastFast<IGameOptions>();

        if (GameStates.IsNormalGame)
        {
            Options.DefaultKillCooldown = Main.NormalOptions.KillCooldown;
            Main.LastKillCooldown.Value = Main.NormalOptions.KillCooldown;
            Main.NormalOptions.KillCooldown = 0f;

            AURoleOptions.SetOpt(opt);
            Main.LastShapeshifterCooldown.Value = AURoleOptions.ShapeshifterCooldown;
            AURoleOptions.ShapeshifterCooldown = 0f;
            AURoleOptions.ImpostorsCanSeeProtect = false;

            Main.LastGuardianAngelCooldown.Value = Options.DefaultAngelCooldown.GetFloat();
            AURoleOptions.GuardianAngelCooldown = 0f;
        }

        PlayerControl.LocalPlayer.RpcSyncSettings(GameOptionsManager.Instance.gameOptionsFactory.ToBytes(opt, AprilFoolsMode.IsAprilFoolsModeToggledOn));
        RPC.RpcVersionCheck();
    }
    private static byte SelectRandomMap()
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
    public static void Prefix(GameStartManager __instance)
    {
        if (GameStates.IsCountDown)
        {
            GameStartManagerPatch.GameStartManagerUpdatePatch.AlredyBegin = false;

            SoundManager.Instance.StopSound(__instance.gameStartSound);

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
