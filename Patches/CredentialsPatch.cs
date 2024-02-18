using HarmonyLib;
using System.Linq;
using System.Text;
using TMPro;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

//[HarmonyPatch]
public static class Credentials
{
    public static SpriteRenderer ToheLogo { get; private set; }

    [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
    class PingTrackerUpdatePatch
    {
        private static int DelayUpdate = 0;
        private static string TempPing;
        private static bool CheckIsModHost = true;
        private static readonly StringBuilder sb = new();

        private static bool Prefix(PingTracker __instance)
        {
            DelayUpdate--;

            if (DelayUpdate > 0 && sb.Length > 0)
            {
                __instance.text.alignment = TextAlignmentOptions.TopRight;

                if (CheckIsModHost && GameStates.IsModHost)
                {
                    sb.Remove(2, $"\r\n{Utils.ColorString(Color.red, GetString("Warning.NoModHost"))}".Length);
                    CheckIsModHost = false;
                }

                __instance.text.text = TempPing;

                return false;
            }

            DelayUpdate = 500;

            __instance.text.alignment = TextAlignmentOptions.TopRight;

            sb.Clear();

            sb.Append(Main.credentialsText);

            var ping = AmongUsClient.Instance.Ping;
            string pingcolor = "#ff4500";
            if (ping < 30) pingcolor = "#44dfcc";
            else if (ping < 100) pingcolor = "#7bc690";
            else if (ping < 200) pingcolor = "#f3920e";
            else if (ping < 400) pingcolor = "#ff146e";
            sb.Append($"\r\n<color={pingcolor}>Ping: {ping} ms</color>");

            if (!GameStates.IsModHost)
            {
                CheckIsModHost = true;
                sb.Append($"\r\n{Utils.ColorString(Color.red, GetString("Warning.NoModHost"))}");
            }

            if (Main.ShowFPS.Value)
            {
                var FPSGame = 1.0f / Time.deltaTime;
                Color fpscolor = Color.green;

                if (FPSGame < 20f) fpscolor = Color.red;
                else if (FPSGame < 40f) fpscolor = Color.yellow;

                sb.Append($"\r\n{Utils.ColorString(fpscolor, Utils.ColorString(Color.cyan, GetString("FPSGame")) + ((int)FPSGame).ToString())}");
            }

            if (Main.ShowTextOverlay.Value)
            {
                var sbOverlay = new StringBuilder();
                if (Options.LowLoadMode.GetBool()) sbOverlay.Append($"\r\n{Utils.ColorString(Color.green, GetString("Overlay.LowLoadMode"))}");
                if (Options.NoGameEnd.GetBool()) sbOverlay.Append($"\r\n{Utils.ColorString(Color.red, GetString("Overlay.NoGameEnd"))}");
                if (Options.GuesserMode.GetBool()) sbOverlay.Append($"\r\n{Utils.ColorString(Color.yellow, GetString("Overlay.GuesserMode"))}");
                if (Options.AllowConsole.GetBool() && PlayerControl.LocalPlayer.FriendCode.GetDevUser().DeBug) sbOverlay.Append($"\r\n{Utils.ColorString(Color.red, GetString("Overlay.AllowConsole"))}");
                if (DebugModeManager.IsDebugMode) sbOverlay.Append($"\r\n{Utils.ColorString(Color.green, GetString("Overlay.DebugMode"))}");

                if (sbOverlay.Length > 0)
                    sb.Append(sbOverlay);
            }

            var offset_x = 1.2f; //Offset from right edge
            if (HudManager.InstanceExists && HudManager._instance.Chat.chatButton.active) offset_x += 0.8f; // Additional offsets for chat button if present
            if (FriendsListManager.InstanceExists && FriendsListManager._instance.FriendsListButton.Button.active) offset_x += 0.8f; // Additional offsets if friend list button is present
            __instance.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(offset_x, 0f, 0f);

            TempPing = sb.ToString();
            __instance.text.text = TempPing;

            return false;
        }
    }
    [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
    class VersionShowerStartPatch
    {
        static TextMeshPro SpecialEventText;
        private static void Postfix(VersionShower __instance)
        {
            Main.credentialsText = $"\r\n<color={Main.ModColor}>{Main.ModName}</color> v{Main.PluginDisplayVersion}";
            var buildtype = "";

#if RELEASE
            Main.credentialsText += $"\r\n<color=#a54aff>By <color=#f34c50>Enhanced Network</color>";
            buildtype = "Release";
#endif

#if CANARY
            Main.credentialsText += $"\r\n<color=#ffc0cb>Canary:</color><color=#f34c50>{ThisAssembly.Git.Branch}</color>(<color=#ffc0cb>{ThisAssembly.Git.Commit}</color>)";
            Main.credentialsText += $"\r\n<color=#a54aff>By <color=#f34c50>The Enhanced Network</color>";
            buildtype = "Canary";
#endif

#if DEBUG
            Main.credentialsText += $"\r\n<color=#ffc0cb>Debug:</color><color=#f34c50>{ThisAssembly.Git.Branch}</color>(<color=#ffc0cb>{ThisAssembly.Git.Commit}</color>)";
            Main.credentialsText += $"\r\n<color=#a54aff>By <color=#f34c50>The Enhanced Network</color>";
            buildtype = "Debug";
#endif
            Logger.Info($"v{Main.PluginVersion}, {buildtype}:{ThisAssembly.Git.Branch}:({ThisAssembly.Git.Commit}), link [{ThisAssembly.Git.RepositoryUrl}], dirty: [{ThisAssembly.Git.IsDirty}]", "TOHE version");

            if (Main.IsAprilFools)
                Main.credentialsText = $"\r\n<color=#00bfff>Town Of Host</color> v11.45.14";

            var credentials = Object.Instantiate(__instance.text);
            credentials.text = Main.credentialsText;
            credentials.alignment = TextAlignmentOptions.Right;
            credentials.transform.position = new Vector3(1f, 2.79f, -2f);
            credentials.fontSize = credentials.fontSizeMax = credentials.fontSizeMin = 2f;

            ErrorText.Create(__instance.text);
            if (Main.hasArgumentException && ErrorText.Instance != null)
            {
                ErrorText.Instance.AddError(ErrorCode.Main_DictionaryError);
            }

            VersionChecker.Check();

            if (SpecialEventText == null && ToheLogo != null)
            {
                SpecialEventText = Object.Instantiate(__instance.text, ToheLogo.transform);
                SpecialEventText.name = "SpecialEventText";
                SpecialEventText.text = "";
                SpecialEventText.color = Color.white;
                SpecialEventText.fontSizeMin = 3f;
                SpecialEventText.alignment = TextAlignmentOptions.Center;
                SpecialEventText.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            }
            if (SpecialEventText != null)
            {
                SpecialEventText.enabled = TitleLogoPatch.amongUsLogo != null;
            }
            if (Main.IsInitialRelease)
            {
                SpecialEventText.text = $"Happy Birthday to {Main.ModName}!";
                if (ColorUtility.TryParseHtmlString(Main.ModColor, out var col))
                {
                    SpecialEventText.color = col;
                }
            }
        }
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public class TitleLogoPatch
    {
        public static GameObject amongUsLogo;
        public static GameObject Ambience;
        public static GameObject LoadingHint;

        private static void Postfix(MainMenuManager __instance)
        {
            amongUsLogo = GameObject.Find("LOGO-AU");

            var rightpanel = __instance.gameModeButtons.transform.parent;
            var logoObject = new GameObject("titleLogo_TOHE");
            var logoTransform = logoObject.transform;
            ToheLogo = logoObject.AddComponent<SpriteRenderer>();
            logoTransform.parent = rightpanel;
            logoTransform.localPosition = new(-0.16f, 0f, 1f); //new(0f, 0.3f, 1f); new(0f, 0.15f, 1f);
            logoTransform.localScale *= 1.2f;

            if (!Options.IsLoaded)
            {
                LoadingHint = new GameObject("LoadingHint");
                LoadingHint.transform.position = Vector3.down;
                var LoadingHintText = LoadingHint.AddComponent<TextMeshPro>();
                LoadingHintText.text = GetString("Loading");
                LoadingHintText.alignment = TextAlignmentOptions.Center;
                LoadingHintText.fontSize = 2f;
                LoadingHintText.transform.position = amongUsLogo.transform.position;
                LoadingHintText.transform.position += new Vector3 (-0.25f, -0.9f, 0f);
                LoadingHintText.color = new Color32(17, 255, 1, byte.MaxValue);
                __instance.playButton.transform.gameObject.SetActive(false);
            }
            if ((Ambience = GameObject.Find("Ambience")) != null)
            {
                // Show playButton when mod is fully loaded
                if (Options.IsLoaded && LoadingHint != null) __instance.playButton.transform.gameObject.SetActive(true);

                Ambience.SetActive(false);
                //var CustomBG = new GameObject("CustomBG");
                //CustomBG.transform.position = new Vector3(2.095f, -0.25f, 520f);
                //var bgRenderer = CustomBG.AddComponent<SpriteRenderer>();
                //bgRenderer.sprite = Utils.LoadSprite("TOHE.Resources.Background.TOH-Background-Old.jpg", 245f);
            }
        }
    }
    [HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
    class ModManagerLateUpdatePatch
    {
        public static void Prefix(ModManager __instance)
        {
            __instance.ShowModStamp();

            LateTask.Update(Time.deltaTime);
            CheckMurderPatch.Update();

            if (Utils.GetTimeStamp() > EAC.LastClientInfoCheck + 20)
            {
                EAC.LastClientInfoCheck = Utils.GetTimeStamp();
                
                if (PlayerControl.LocalPlayer && GameStates.IsOnlineGame)
                    foreach (var pc in Main.AllPlayerControls.ToArray())
                        EAC.ClientInfoCheck(pc.GetClient(), false);
            }
        }
        public static void Postfix(ModManager __instance)
        {
            var offset_y = HudManager.InstanceExists ? 1.6f : 0.9f;
            __instance.ModStamp.transform.position = AspectPosition.ComputeWorldPosition(
                __instance.localCamera, AspectPosition.EdgeAlignments.RightTop,
                new Vector3(0.4f, offset_y, __instance.localCamera.nearClipPlane + 0.1f));
        }
    }
}