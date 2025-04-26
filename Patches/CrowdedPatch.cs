using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using TMPro;
using UnityEngine;

// https://github.com/CrowdedMods/CrowdedMod/blob/master/src/CrowdedMod
// Niko adjusted mono behavior patches to fit into non-reactor mods

namespace TOHE.Patches.Crowded;

internal static class Crowded
{
    private static CreateOptionsPicker instance;
    public static int MaxPlayers => GameStates.IsVanillaServer ? 15 : 127;
    public static int MaxImpostors => MaxPlayers / 2;

    [HarmonyPatch(typeof(CreateOptionsPicker), nameof(CreateOptionsPicker.Awake))]
    public static class CreateOptionsPicker_Awake
    {
        public static void Prefix(CreateOptionsPicker __instance)
        {
            instance = __instance;
        }
        public static void Postfix(CreateOptionsPicker __instance)
        {
            if (__instance.mode != SettingsMode.Host) return;

            {
                var firstButtonRenderer = __instance.MaxPlayerButtons[0];
                firstButtonRenderer.GetComponentInChildren<TextMeshPro>().text = "-";
                firstButtonRenderer.enabled = false;

                var firstButtonButton = firstButtonRenderer.GetComponent<PassiveButton>();
                firstButtonButton.OnClick.RemoveAllListeners();
                firstButtonButton.OnClick.AddListener((Action)(() =>
                {
                    for (var i = 1; i < 11; i++)
                    {
                        var playerButton = __instance.MaxPlayerButtons[i];

                        var tmp = playerButton.GetComponentInChildren<TextMeshPro>();
                        var newValue = Mathf.Max(byte.Parse(tmp.text) - 10, byte.Parse(playerButton.name) - 2);
                        tmp.text = newValue.ToString();
                    }

                    __instance.UpdateMaxPlayersButtons(__instance.GetTargetOptions());
                }));
                UnityEngine.Object.Destroy(firstButtonRenderer);

                var lastButtonRenderer = __instance.MaxPlayerButtons[^1];
                lastButtonRenderer.GetComponentInChildren<TextMeshPro>().text = "+";
                lastButtonRenderer.enabled = false;

                var lastButtonButton = lastButtonRenderer.GetComponent<PassiveButton>();
                lastButtonButton.OnClick.RemoveAllListeners();
                lastButtonButton.OnClick.AddListener((Action)(() =>
                {
                    for (var i = 1; i < 11; i++)
                    {
                        var playerButton = __instance.MaxPlayerButtons[i];

                        var tmp = playerButton.GetComponentInChildren<TextMeshPro>();
                        var newValue = Mathf.Min(byte.Parse(tmp.text) + 10,
                            MaxPlayers - 14 + byte.Parse(playerButton.name));
                        tmp.text = newValue.ToString();
                    }

                    __instance.UpdateMaxPlayersButtons(__instance.GetTargetOptions());
                }));
                UnityEngine.Object.Destroy(lastButtonRenderer);

                for (var i = 1; i < 11; i++)
                {
                    var playerButton = __instance.MaxPlayerButtons[i].GetComponent<PassiveButton>();
                    var text = playerButton.GetComponentInChildren<TextMeshPro>();

                    playerButton.OnClick.RemoveAllListeners();
                    playerButton.OnClick.AddListener((Action)(() =>
                    {
                        var maxPlayers = byte.Parse(text.text);
                        var maxImp = Mathf.Min(__instance.GetTargetOptions().NumImpostors, maxPlayers / 2);
                        __instance.GetTargetOptions().SetInt(Int32OptionNames.NumImpostors, maxImp);
                        __instance.ImpostorButtons[1].TextMesh.text = maxImp.ToString();
                        __instance.SetMaxPlayersButtons(maxPlayers);
                    }));
                }

                foreach (var button in __instance.MaxPlayerButtons)
                {
                    button.enabled = button.GetComponentInChildren<TextMeshPro>().text == __instance.GetTargetOptions().MaxPlayers.ToString();
                }
            }

            {
                var secondButton = __instance.ImpostorButtons[1];
                secondButton.SpriteRenderer.enabled = false;
                UnityEngine.Object.Destroy(secondButton.transform.FindChild("ConsoleHighlight").gameObject);
                UnityEngine.Object.Destroy(secondButton.PassiveButton);
                UnityEngine.Object.Destroy(secondButton.BoxCollider);

                var secondButtonText = secondButton.TextMesh;
                secondButtonText.text = __instance.GetTargetOptions().NumImpostors.ToString();

                var firstButton = __instance.ImpostorButtons[0];
                firstButton.SpriteRenderer.enabled = false;
                firstButton.TextMesh.text = "-";

                var firstPassiveButton = firstButton.PassiveButton;
                firstPassiveButton.OnClick.RemoveAllListeners();
                firstPassiveButton.OnClick.AddListener((Action)(() =>
                {
                    var newVal = Mathf.Clamp(
                        byte.Parse(secondButtonText.text) - 1,
                        1,
                        __instance.GetTargetOptions().MaxPlayers / 2
                    );
                    __instance.SetImpostorButtons(newVal);
                    secondButtonText.text = newVal.ToString();
                }));

                var thirdButton = __instance.ImpostorButtons[2];
                thirdButton.SpriteRenderer.enabled = false;
                thirdButton.TextMesh.text = "+";

                var thirdPassiveButton = thirdButton.PassiveButton;
                thirdPassiveButton.OnClick.RemoveAllListeners();
                thirdPassiveButton.OnClick.AddListener((Action)(() =>
                {
                    var newVal = Mathf.Clamp(
                        byte.Parse(secondButtonText.text) + 1,
                        1,
                        __instance.GetTargetOptions().MaxPlayers / 2
                    );
                    __instance.SetImpostorButtons(newVal);
                    secondButtonText.text = newVal.ToString();
                }));
            }
        }
    }

    [HarmonyPatch(typeof(CreateOptionsPicker), nameof(CreateOptionsPicker.Refresh))]
    public static class CreateOptionsPicker_Refresh
    {
        public static bool Prefix(CreateOptionsPicker __instance)
        {
            IGameOptions targetOptions = __instance.GetTargetOptions();
            __instance.UpdateImpostorsButtons(targetOptions.NumImpostors);
            __instance.UpdateMaxPlayersButtons(targetOptions);
            __instance.UpdateLanguageButton((uint)targetOptions.Keywords);
            __instance.MapMenu.UpdateMapButtons((int)targetOptions.MapId);
            __instance.GameModeText.text = DestroyableSingleton<TranslationController>.Instance.GetString(GameModesHelpers.ModeToName[GameOptionsManager.Instance.CurrentGameOptions.GameMode]);
            return false;

            // Skip maxplayers => max impostors array check here
            // Overwrite to 3 bug
        }
    }

    [HarmonyPatch(typeof(ServerManager), nameof(ServerManager.SetRegion))]
    public static class ServerManager_SetRegion
    {
        // I dont find a effect way to patch CreateOptionsPicker LOL
        public static void Postfix(ServerManager __instance)
        {
            if (GameStates.IsVanillaServer)
            {
                if (GameOptionsManager.Instance.GameHostOptions != null)
                {
                    if (GameOptionsManager.Instance.GameHostOptions.MaxPlayers > 15)
                    {
                        GameOptionsManager.Instance.GameHostOptions.SetInt(Int32OptionNames.MaxPlayers, 15);
                    }

                    if (GameOptionsManager.Instance.GameHostOptions.NumImpostors > 3)
                    {
                        GameOptionsManager.Instance.GameHostOptions.SetInt(Int32OptionNames.NumImpostors, 3);
                    }
                }
                if (instance)
                {
                    for (var i = 1; i < 11; i++)
                    {
                        var playerButton = instance.MaxPlayerButtons[i];

                        var tmp = playerButton.GetComponentInChildren<TextMeshPro>();
                        var newValue = Mathf.Min(byte.Parse(tmp.text) + 10,
                            MaxPlayers - 14 + byte.Parse(playerButton.name));
                        tmp.text = newValue.ToString();
                    }

                    instance.UpdateMaxPlayersButtons(instance.GetTargetOptions());
                }
            }
        }
    }

    [HarmonyPatch(typeof(CreateOptionsPicker), nameof(CreateOptionsPicker.UpdateMaxPlayersButtons))]
    public static class CreateOptionsPicker_UpdateMaxPlayersButtons
    {
        public static bool Prefix(CreateOptionsPicker __instance, [HarmonyArgument(0)] IGameOptions opts)
        {
            if (__instance.mode != SettingsMode.Host) return true;
            if (__instance.CrewArea)
            {
                __instance.CrewArea.SetCrewSize(opts.MaxPlayers, opts.NumImpostors);
            }

            var selectedAsString = opts.MaxPlayers.ToString();
            for (var i = 1; i < __instance.MaxPlayerButtons.Count - 1; i++)
            {
                __instance.MaxPlayerButtons[i].enabled = __instance.MaxPlayerButtons[i].GetComponentInChildren<TextMeshPro>().text == selectedAsString;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(NormalGameOptionsV09), nameof(NormalGameOptionsV09.AreInvalid))]
    public static class NormalGameOptions_AreInvalid
    {
        public static bool Prefix(NormalGameOptionsV09 __instance, ref bool __result)
        {
            __result = __instance.NumImpostors < 0 || __instance.KillDistance < 0 || __instance.KillCooldown < 0 || __instance.PlayerSpeedMod <= 0;

            if (GameStates.IsVanillaServer)
            {
                if (__instance.MaxPlayers > 15)
                {
                    __result = true;
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(CreateOptionsPicker), nameof(CreateOptionsPicker.UpdateImpostorsButtons))]
    public static class CreateOptionsPicker_UpdateImpostorsButtons
    {
        public static bool Prefix(CreateOptionsPicker __instance)
        {
            if (__instance.mode == SettingsMode.Host) return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(CreateOptionsPicker), nameof(CreateOptionsPicker.SetImpostorButtons))]
    public static class CreateOptionsPicker_SetImpostorButtons
    {
        public static bool Prefix(CreateOptionsPicker __instance, int numImpostors)
        {
            if (__instance.mode != SettingsMode.Host) return true;
            IGameOptions targetOptions = __instance.GetTargetOptions();
            targetOptions.SetInt(Int32OptionNames.NumImpostors, numImpostors);
            __instance.SetTargetOptions(targetOptions);
            __instance.UpdateImpostorsButtons(numImpostors);

            return false;
        }
    }

    [HarmonyPatch(typeof(CreateOptionsPicker), nameof(CreateOptionsPicker.SetMaxPlayersButtons))]
    public static class CreateOptionsPicker_SetMaxPlayersButtons
    {
        public static bool Prefix(CreateOptionsPicker __instance, int maxPlayers)
        {
            if (DestroyableSingleton<FindAGameManager>.InstanceExists || __instance.mode != SettingsMode.Host)
            {
                return true;
            }

            IGameOptions targetOptions = __instance.GetTargetOptions();
            targetOptions.SetInt(Int32OptionNames.MaxPlayers, maxPlayers);
            __instance.SetTargetOptions(targetOptions);
            __instance.UpdateMaxPlayersButtons(targetOptions);

            return false;
        }
    }

    [HarmonyPatch(typeof(SecurityLogger), nameof(SecurityLogger.Awake))]
    public static class SecurityLoggerPatch
    {
        public static void Postfix(ref SecurityLogger __instance)
        {
            __instance.Timers = new Il2CppStructArray<float>(127);
        }
    }

    [HarmonyPatch(typeof(PlayerTab), nameof(PlayerTab.Update))]
    public static class PlayerTabIsSelectedItemEquippedPatch
    {
        public static void Postfix(PlayerTab __instance)
        {
            if (GameOptionsManager.Instance.CurrentGameOptions.MaxPlayers > 15)
            {
                __instance.currentColorIsEquipped = false;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerTab), nameof(PlayerTab.UpdateAvailableColors))]
    public static class PlayerTabUpdateAvailableColorsPatch
    {
        public static bool Prefix(PlayerTab __instance)
        {
            if (GameOptionsManager.Instance.CurrentGameOptions.MaxPlayers <= 15)
            {
                return true;
            }

            __instance.AvailableColors.Clear();
            for (var i = 0; i < Palette.PlayerColors.Count; i++)
            {
                if (!PlayerControl.LocalPlayer || PlayerControl.LocalPlayer.CurrentOutfit.ColorId != i)
                {
                    __instance.AvailableColors.Add(i);
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    public static class MeetingHudStartPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            __instance.gameObject.AddComponent<MeetingHudPagingBehaviour>().meetingHud = __instance;
        }
    }

    [HarmonyPatch(typeof(ShapeshifterMinigame), nameof(ShapeshifterMinigame.Begin))]
    public static class ShapeshifterMinigameBeginPatch
    {
        public static void Postfix(ShapeshifterMinigame __instance)
        {
            __instance.gameObject.AddComponent<ShapeShifterPagingBehaviour>().shapeshifterMinigame = __instance;
        }
    }

    [HarmonyPatch(typeof(VitalsMinigame), nameof(VitalsMinigame.Begin))]
    public static class VitalsMinigameBeginPatch
    {
        public static void Postfix(VitalsMinigame __instance)
        {
            __instance.gameObject.AddComponent<VitalsPagingBehaviour>().vitalsMinigame = __instance;
        }
    }

    [HarmonyPatch(typeof(PSManager), nameof(PSManager.CreateGame))]
    [HarmonyPatch(typeof(CreateGameOptions), nameof(CreateGameOptions.ContinueStart))]
    public static class BeforeHostGamePatch
    {
        public static void Prefix()
        {
            Logger.Info("Host Game is being called!", "CrowdedPatch");

            if (GameStates.IsVanillaServer && !GameStates.IsLocalGame)
            {
                if (GameOptionsManager.Instance.GameHostOptions != null)
                {
                    if (GameOptionsManager.Instance.GameHostOptions.MaxPlayers > 15)
                    {
                        GameOptionsManager.Instance.GameHostOptions.SetInt(Int32OptionNames.MaxPlayers, 15);
                    }

                    if (GameOptionsManager.Instance.GameHostOptions.NumImpostors > 3)
                    {
                        GameOptionsManager.Instance.GameHostOptions.SetInt(Int32OptionNames.NumImpostors, 3);
                    }
                }
            }
        }
    }
}

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public class AbstractPagingBehaviour : MonoBehaviour
{
    public AbstractPagingBehaviour(IntPtr ptr) : base(ptr)
    {
    }

    public const string PAGE_INDEX_GAME_OBJECT_NAME = "CrowdedMod_PageIndex";

    private int _page;

    public virtual int MaxPerPage => 15;
    // public virtual IEnumerable<T> Targets { get; }

    public virtual int PageIndex
    {
        get => _page;
        set
        {
            _page = value;
            OnPageChanged();
        }
    }

    public virtual int MaxPageIndex => throw new NotImplementedException();
    // public virtual int MaxPages => Targets.Count() / MaxPerPage;

    public virtual void OnPageChanged() => throw new NotImplementedException();

    public virtual void Start() => OnPageChanged();

    public virtual void Update()
    {
        bool chatIsOpen = HudManager.Instance.Chat.IsOpenOrOpening;
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.LeftArrow) || (!chatIsOpen && Input.mouseScrollDelta.y > 0f))
            Cycle(false);
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.RightArrow) || (!chatIsOpen && Input.mouseScrollDelta.y < 0f))
            Cycle(true);
    }

    /// <summary>
    /// Loops around if you go over the limits.<br/>
    /// Attempting to go up a page while on the first page will take you to the last page and vice versa.
    /// </summary>
    public virtual void Cycle(bool increment)
    {
        var change = increment ? 1 : -1;
        PageIndex = Mathf.Clamp(PageIndex + change, 0, MaxPageIndex);
    }
}

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public class MeetingHudPagingBehaviour : AbstractPagingBehaviour
{
    public MeetingHudPagingBehaviour(IntPtr ptr) : base(ptr)
    {
    }

    internal MeetingHud meetingHud = null!;

    [HideFromIl2Cpp]
    public IEnumerable<PlayerVoteArea> Targets => meetingHud.playerStates.OrderBy(p => p.AmDead);
    public override int MaxPageIndex
    {
        get
        {
            if (maxPageIndex == -1)
            {
                maxPageIndex = (Targets.Count() - 1) / MaxPerPage;
            }
            return maxPageIndex;
        }
    }

    private int maxPageIndex = -1;

    public override void Start()
    {
        maxPageIndex = -1;
        OnPageChanged();
    }

    public override void Update()
    {
        base.Update();

        if (meetingHud.state is MeetingHud.VoteStates.Animating or MeetingHud.VoteStates.Proceeding || meetingHud.TimerText.text.Contains($" ({PageIndex + 1}/{MaxPageIndex + 1})"))
            return; // TimerText does not update there                                                 ^ Sometimes the timer text is spammed with the page counter for some weird reason so this is just a band-aid fix for it

        meetingHud.TimerText.text += $" ({PageIndex + 1}/{MaxPageIndex + 1})";
    }

    public override void OnPageChanged()
    {
        var i = 0;

        foreach (var button in Targets)
        {
            if (i >= PageIndex * MaxPerPage && i < (PageIndex + 1) * MaxPerPage)
            {
                button.gameObject.SetActive(true);

                var relativeIndex = i % MaxPerPage;
                var row = relativeIndex / 3;
                var col = relativeIndex % 3;
                var buttonTransform = button.transform;
                buttonTransform.localPosition = meetingHud.VoteOrigin +
                                          new Vector3(
                                              meetingHud.VoteButtonOffsets.x * col,
                                              meetingHud.VoteButtonOffsets.y * row,
                                              buttonTransform.localPosition.z
                                          );
            }
            else
            {
                button.gameObject.SetActive(false);
            }
            i++;
        }
    }
}

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public class ShapeShifterPagingBehaviour : AbstractPagingBehaviour
{
    public ShapeShifterPagingBehaviour(IntPtr ptr) : base(ptr)
    {
    }

    public ShapeshifterMinigame shapeshifterMinigame = null!;
    [HideFromIl2Cpp]
    public IEnumerable<ShapeshifterPanel> Targets => shapeshifterMinigame.potentialVictims.ToArray();

    public override int MaxPageIndex => (Targets.Count() - 1) / MaxPerPage;
    private TextMeshPro PageText = null!;

    public override void Start()
    {
        PageText = Instantiate(HudManager.Instance.KillButton.cooldownTimerText, shapeshifterMinigame.transform);
        PageText.name = PAGE_INDEX_GAME_OBJECT_NAME;
        PageText.enableWordWrapping = false;
        PageText.gameObject.SetActive(true);
        PageText.transform.localPosition = new Vector3(4.1f, -2.36f, -1f);
        PageText.transform.localScale *= 0.5f;
        OnPageChanged();
    }

    public override void OnPageChanged()
    {
        PageText.text = $"({PageIndex + 1}/{MaxPageIndex + 1})";
        var i = 0;

        foreach (var panel in Targets)
        {
            if (i >= PageIndex * MaxPerPage && i < (PageIndex + 1) * MaxPerPage)
            {
                panel.gameObject.SetActive(true);

                var relativeIndex = i % MaxPerPage;
                var row = relativeIndex / 3;
                var col = relativeIndex % 3;
                var buttonTransform = panel.transform;
                buttonTransform.localPosition = new Vector3(
                                                    shapeshifterMinigame.XStart + shapeshifterMinigame.XOffset * col,
                                                    shapeshifterMinigame.YStart + shapeshifterMinigame.YOffset * row,
                                                    buttonTransform.localPosition.z
                                                );
            }
            else
            {
                panel.gameObject.SetActive(false);
            }

            i++;
        }
    }
}

[Obfuscation(Exclude = true, ApplyToMembers = true)]

public class VitalsPagingBehaviour : AbstractPagingBehaviour
{
    public VitalsPagingBehaviour(IntPtr ptr) : base(ptr) { }

    public VitalsMinigame vitalsMinigame = null!;

    [HideFromIl2Cpp]
    public IEnumerable<VitalsPanel> Targets => vitalsMinigame.vitals.ToArray();
    public override int MaxPageIndex => (Targets.Count() - 1) / MaxPerPage;
    private TextMeshPro PageText = null!;

    public override void Start()
    {
        PageText = Instantiate(HudManager.Instance.KillButton.cooldownTimerText, vitalsMinigame.transform);
        PageText.name = PAGE_INDEX_GAME_OBJECT_NAME;
        PageText.enableWordWrapping = false;
        PageText.gameObject.SetActive(true);
        PageText.transform.localPosition = new Vector3(2.7f, -2f, -1f);
        PageText.transform.localScale *= 0.5f;
        OnPageChanged();
    }

    public override void OnPageChanged()
    {
        if (PlayerTask.PlayerHasTaskOfType<HudOverrideTask>(PlayerControl.LocalPlayer))
            return;

        PageText.text = $"({PageIndex + 1}/{MaxPageIndex + 1})";
        var i = 0;

        foreach (var panel in Targets)
        {
            if (i >= PageIndex * MaxPerPage && i < (PageIndex + 1) * MaxPerPage)
            {
                panel.gameObject.SetActive(true);
                var relativeIndex = i % MaxPerPage;
                var row = relativeIndex / 3;
                var col = relativeIndex % 3;
                var panelTransform = panel.transform;
                panelTransform.localPosition = new Vector3(
                                                    vitalsMinigame.XStart + vitalsMinigame.XOffset * col,
                                                    vitalsMinigame.YStart + vitalsMinigame.YOffset * row,
                                                    panelTransform.localPosition.z
                                                );
            }
            else
                panel.gameObject.SetActive(false);

            i++;
        }
    }
}
