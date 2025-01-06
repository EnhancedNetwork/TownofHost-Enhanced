using System.Text;
using TMPro;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
class PingTrackerUpdatePatch
{
    public static PingTracker Instance;
    private static int DelayUpdate = 0;
    private static readonly StringBuilder sb = new();

    private static bool Prefix(PingTracker __instance)
    {
        try
        {
            Instance ??= __instance;

            DelayUpdate--;

            if (DelayUpdate > 0 && sb.Length > 0)
            {
                ChangeText(__instance);
                __instance.aspectPosition.DistanceFromEdge = GetPingPosition();
                __instance.text.text = sb.ToString();
                return false;
            }

            DelayUpdate = 500;

            ChangeText(__instance);
            sb.Clear();

            sb.Append(Main.credentialsText);

            var ping = AmongUsClient.Instance.Ping;
            string pingcolor = "#ff4500";
            if (ping < 30) pingcolor = "#44dfcc";
            else if (ping < 100) pingcolor = "#7bc690";
            else if (ping < 200) pingcolor = "#f3920e";
            else if (ping < 400) pingcolor = "#ff146e";
            sb.Append($"\r\n<color={pingcolor}>Ping: {ping} ms</color>\r\n<color=#a54aff>Server: <color=#f34c50>{Utils.GetRegionName()}</color>");

            if (!GameStates.IsModHost)
            {
                //CheckIsModHost = true;
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

            __instance.aspectPosition.DistanceFromEdge = GetPingPosition();
            __instance.text.text = sb.ToString();

            return false;
        }
        catch
        {
            DelayUpdate = 0;
            sb.Clear();

            return false;
        }
    }
    private static Vector3 GetPingPosition()
    {
        var settingButtonTransformPosition = DestroyableSingleton<HudManager>.Instance.SettingsButton.transform.localPosition;
        var offset_x = settingButtonTransformPosition.x - 1.58f;
        var offset_y = settingButtonTransformPosition.y + 3.2f;
        Vector3 position;
        if (!Main.ShowTextOverlay.Value)
        {
            offset_y += 0.1f;
        }
        if (AmongUsClient.Instance.IsGameStarted)
        {
            if (DestroyableSingleton<HudManager>.Instance && !HudManager.Instance.Chat.isActiveAndEnabled)
            {
                offset_x += 0.7f; // Additional offsets for chat button if present
            }
            else
            {
                offset_x += 0.1f;
            }

            position = new Vector3(offset_x, offset_y, 0f);
        }
        else
        {
            position = new Vector3(offset_x, offset_y, 0f);
        }

        return position;
    }
    private static void ChangeText(PingTracker __instance)
    {
        __instance.text.alignment = TextAlignmentOptions.Right;
        __instance.text.outlineColor = Color.black;

        if (Main.ShowTextOverlay.Value || Main.ShowFPS.Value)
        {
            var language = DestroyableSingleton<TranslationController>.Instance.currentLanguage.languageID;
            __instance.text.outlineWidth = language switch
            {
                SupportedLangs.Russian or SupportedLangs.Japanese or SupportedLangs.SChinese or SupportedLangs.TChinese => 0.25f,
                _ => 0.40f,
            };
        }
        else
        {
            __instance.text.outlineWidth = 0.40f;
        }
    }
}
[HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
class VersionShowerStartPatch
{
    static TextMeshPro SpecialEventText;
    private static void Postfix(VersionShower __instance)
    {
        Main.credentialsText = $"<size=70%><size=85%><color={Main.ModColor}>{Main.ModName}</color> v{Main.PluginDisplayVersion}</size>";
        var buildtype = "";

#if RELEASE
            Main.credentialsText += $"\r\n<color=#a54aff>By <color=#f34c50>The Enhanced Network</color>";
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
            Main.credentialsText = $"<color=#00bfff>Town Of Host</color> v11.45.14";

        var credentials = Object.Instantiate(__instance.text);
        credentials.text = Main.credentialsText;
        credentials.alignment = TextAlignmentOptions.Right;
        credentials.transform.position = new Vector3(1f, 2.67f, -2f);
        credentials.fontSize = credentials.fontSizeMax = credentials.fontSizeMin = 2f;

        ErrorText.Create(__instance.text);
        if (Main.hasArgumentException && ErrorText.Instance != null)
        {
            ErrorText.Instance.AddError(ErrorCode.Main_DictionaryError);
        }

        VersionChecker.Check();

        if (SpecialEventText == null && MainMenuManagerStartPatch.ToheLogo != null)
        {
            SpecialEventText = Object.Instantiate(__instance.text, MainMenuManagerStartPatch.ToheLogo.transform);
            SpecialEventText.name = "SpecialEventText";
            SpecialEventText.text = "";
            SpecialEventText.color = Color.white;
            SpecialEventText.fontSizeMin = 3f;
            SpecialEventText.alignment = TextAlignmentOptions.Center;
            SpecialEventText.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        }
        if (SpecialEventText != null)
        {
            SpecialEventText.enabled = MainMenuManagerStartPatch.amongUsLogo != null;
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
[HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
class ModManagerLateUpdatePatch
{
    public static void Prefix(ModManager __instance)
    {
        __instance.ShowModStamp();

        LateTask.Update(Time.deltaTime);
        CheckMurderPatch.Update();
    }
    public static void Postfix(ModManager __instance)
    {
        var offset_y = HudManager.InstanceExists ? 1.8f : 0.9f;
        __instance.ModStamp.transform.position = AspectPosition.ComputeWorldPosition(
            __instance.localCamera, AspectPosition.EdgeAlignments.RightTop,
            new Vector3(0.4f, offset_y, __instance.localCamera.nearClipPlane + 0.1f));
    }
}