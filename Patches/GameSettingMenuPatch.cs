using System;
using TMPro;
using UnityEngine;
using static TOHE.Translator;
using Object = UnityEngine.Object;

namespace TOHE;

// Thanks: https://github.com/Yumenopai/TownOfHost_Y/blob/main/Patches/GameSettingMenuPatch.cs
[HarmonyPatch(typeof(GameSettingMenu))]
public class GameSettingMenuPatch
{
    private static readonly Vector3 ButtonPositionLeft = new(-3.9f, -0.4f, 0f);
    private static readonly Vector3 ButtonPositionRight = new(-2.4f, -0.4f, 0f);

    private static readonly Vector3 ButtonSize = new(0.45f, 0.6f, 1f);

    private static GameOptionsMenu TemplateGameOptionsMenu;
    private static PassiveButton TemplateGameSettingsButton;

    static Dictionary<TabGroup, PassiveButton> ModSettingsButtons = [];
    static Dictionary<TabGroup, GameOptionsMenu> ModSettingsTabs = [];
    public static GameSettingMenu Instance;

    [HarmonyPatch(nameof(GameSettingMenu.Start)), HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void StartPostfix(GameSettingMenu __instance)
    {
        Instance = __instance;

        TabGroup[] ExludeList = Options.CurrentGameMode switch
        {
            CustomGameMode.HidenSeekTOHO => Enum.GetValues<TabGroup>().Skip(3).ToArray(),
            CustomGameMode.FFA => Enum.GetValues<TabGroup>().Skip(2).ToArray(),
            CustomGameMode.UltimateTeam => Enum.GetValues<TabGroup>().Skip(2).ToArray(),
            CustomGameMode.TrickorTreat => Enum.GetValues<TabGroup>().Skip(2).ToArray(),
            CustomGameMode.CandR => Enum.GetValues<TabGroup>().Skip(3).ToArray(),
            _ => []
        };

        // https://gyazo.com/a8f6ec93e44eca8e6febb7d2e91c3750 So much empy space, I think this definetevly calls for HnS roles 😼

        ModSettingsButtons = [];
        foreach (var tab in EnumHelper.GetAllValues<TabGroup>().Except(ExludeList))
        {
            var button = Object.Instantiate(TemplateGameSettingsButton, __instance.GameSettingsButton.transform.parent);
            button.gameObject.SetActive(true);
            button.name = "Button_" + tab;
            var label = button.GetComponentInChildren<TextMeshPro>();
            label.DestroyTranslator();
            string htmlcolor = tab switch
            {
                TabGroup.SystemSettings => Main.ModColor,
                TabGroup.ModSettings => "#59ef83",
                TabGroup.ImpostorRoles => "#f74631",
                TabGroup.CrewmateRoles => "#8cffff",
                TabGroup.NeutralRoles => "#7f8c8d",
                TabGroup.CovenRoles => "#ac42f2",
                TabGroup.Addons => "#ffff00",
                _ => "#ffffff",
            };
            label.fontStyle = FontStyles.UpperCase;
            label.text = $"<color={htmlcolor}>{GetString("TabGroup." + tab)}</color>";

            _ = ColorUtility.TryParseHtmlString(htmlcolor, out Color tabColor);
            button.inactiveSprites.GetComponent<SpriteRenderer>().color = tabColor;
            button.activeSprites.GetComponent<SpriteRenderer>().color = tabColor;
            button.selectedSprites.GetComponent<SpriteRenderer>().color = tabColor;

            Vector3 offset = new(0.0f, 0.5f * (((int)tab + 1) / 2), 0.0f);
            button.transform.localPosition = ((((int)tab + 1) % 2 == 0) ? ButtonPositionLeft : ButtonPositionRight) - offset;
            button.transform.localScale = ButtonSize;

            var buttonComponent = button.GetComponent<PassiveButton>();
            buttonComponent.OnClick = new();
            buttonComponent.OnClick.AddListener(
                (UnityEngine.Events.UnityAction)(() => __instance.ChangeTab((int)tab + 3, false)));

            ModSettingsButtons.Add(tab, button);
        }

        ModSettingsTabs = [];
        foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
        {
            var setTab = Object.Instantiate(TemplateGameOptionsMenu, __instance.GameSettingsTab.transform.parent);
            setTab.name = "tab_" + tab;
            setTab.gameObject.SetActive(false);

            ModSettingsTabs.Add(tab, setTab);
        }

        foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
        {
            if (ModSettingsButtons.TryGetValue(tab, out var button))
            {
                __instance.ControllerSelectable.Add(button);
            }
        }

        HiddenBySearch.Do(x => x.SetHidden(false));
        HiddenBySearch.Clear();

        SetupAdittionalButtons(__instance);
    }
    private static void SetDefaultButton(GameSettingMenu __instance)
    {
        __instance.GamePresetsButton.gameObject.SetActive(false);

        var gameSettingButton = __instance.GameSettingsButton;
        gameSettingButton.transform.localPosition = new(-3f, -0.5f, 0f);

        var textLabel = gameSettingButton.GetComponentInChildren<TextMeshPro>();
        textLabel.DestroyTranslator();
        textLabel.fontStyle = FontStyles.UpperCase;
        textLabel.text = GetString("TabVanilla.GameSettings");

        var optionMenu = GameObject.Find("PlayerOptionsMenu(Clone)");
        var menuDescription = optionMenu?.transform.FindChild("What Is This?");

        var infoImage = menuDescription.transform.FindChild("InfoImage");
        infoImage.transform.localPosition = new(-4.65f, 0.16f, -1f);
        infoImage.transform.localScale = new(0.2202f, 0.2202f, 0.3202f);

        var infoText = menuDescription.transform.FindChild("InfoText");
        infoText.transform.localPosition = new(-3.5f, 0.83f, -2f);
        infoText.transform.localScale = new(1f, 1f, 1f);

        var cubeObject = menuDescription.transform.FindChild("Cube");
        cubeObject.transform.localPosition = new(-3.2f, 0.55f, -0.1f);
        cubeObject.transform.localScale = new(0.61f, 0.64f, 1f);

        __instance.MenuDescriptionText.m_marginWidth = 2.5f;

        gameSettingButton.transform.localPosition = ButtonPositionLeft;
        gameSettingButton.transform.localScale = ButtonSize;

        __instance.RoleSettingsButton.gameObject.SetActive(false);

        __instance.DefaultButtonSelected = gameSettingButton;
        __instance.ControllerSelectable = new();
        __instance.ControllerSelectable.Add(gameSettingButton);
    }
    public static StringOption PresetBehaviour;
    public static FreeChatInputField InputField;
    public static List<OptionItem> HiddenBySearch = [];
    public static Action _SearchForOptions;

    private static void SetupAdittionalButtons(GameSettingMenu __instance)
    {
        if (__instance == null) return;
        
        var ParentLeftPanel = __instance.GamePresetsButton.transform.parent;

        var labeltag = GameObject.Find("ModeValue");
        var preset = Object.Instantiate(labeltag, ParentLeftPanel);
        preset.transform.localPosition = new Vector3(-3.33f, -0.45f, -2f);

        preset.transform.localScale = new Vector3(0.65f, 0.63f, 1f);
        var SpriteRenderer = preset.GetComponentInChildren<SpriteRenderer>();
        SpriteRenderer.color = Color.white;
        SpriteRenderer.sprite = Utils.LoadSprite("TOHE.Resources.Images.PresetBox.png", 55f);

        Color clr = new(-1, -1, -1);
        var PLabel = preset.GetComponentInChildren<TextMeshPro>();
        PLabel.DestroyTranslator();
        PLabel.text = GetString($"Preset_{OptionItem.CurrentPreset + 1}");
        float size = DestroyableSingleton<TranslationController>.Instance.currentLanguage.languageID switch
        {
            SupportedLangs.Russian => 1.45f,
            _ => 2.45f,
        };
        (PLabel.fontSizeMax, PLabel.fontSizeMin) = (size, size);

        var TempMinus = GameObject.Find("MinusButton").gameObject;
        var GMinus = Object.Instantiate(__instance.GamePresetsButton.gameObject, preset.transform);
        GMinus.gameObject.SetActive(true);
        GMinus.transform.localScale = new Vector3(0.08f, 0.4f, 1f);


        var MLabel = GMinus.transform.Find("FontPlacer/Text_TMP").GetComponent<TextMeshPro>();
        MLabel.alignment = TextAlignmentOptions.Center;
        MLabel.DestroyTranslator();
        MLabel.text = "-";
        MLabel.transform.localPosition = new Vector3(MLabel.transform.localPosition.x, MLabel.transform.localPosition.y + 0.26f, MLabel.transform.localPosition.z);
        MLabel.color = new Color(255f, 255f, 255f);
        MLabel.SetFaceColor(new Color(255f, 255f, 255f));
        MLabel.transform.localScale = new Vector3(12f, 4f, 1f);


        var Minus = GMinus.GetComponent<PassiveButton>();
        Minus.OnClick.RemoveAllListeners();
        Minus.OnClick.AddListener(
                (UnityEngine.Events.UnityAction)(() =>
                {
                    if (PresetBehaviour == null) __instance.ChangeTab(3, false);
                    PresetBehaviour.Decrease();
                }));
        Minus.activeTextColor = new Color(255f, 255f, 255f);
        Minus.inactiveTextColor = new Color(255f, 255f, 255f);
        Minus.disabledTextColor = new Color(255f, 255f, 255f);
        Minus.selectedTextColor = new Color(255f, 255f, 255f);

        Minus.transform.localPosition = new Vector3(-2f, -3.37f, -4f);
        Minus.inactiveSprites.GetComponent<SpriteRenderer>().sprite = TempMinus.GetComponentInChildren<SpriteRenderer>().sprite;
        Minus.activeSprites.GetComponent<SpriteRenderer>().sprite = TempMinus.GetComponentInChildren<SpriteRenderer>().sprite;
        Minus.selectedSprites.GetComponent<SpriteRenderer>().sprite = TempMinus.GetComponentInChildren<SpriteRenderer>().sprite;

        Minus.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color32(55, 59, 60, 255);
        Minus.activeSprites.GetComponent<SpriteRenderer>().color = new Color32(61, 62, 63, 255);
        Minus.selectedSprites.GetComponent<SpriteRenderer>().color = new Color32(55, 59, 60, 255);



        var PlusFab = Object.Instantiate(GMinus, preset.transform);
        var PLuLabel = PlusFab.transform.Find("FontPlacer/Text_TMP").GetComponent<TextMeshPro>();
        PLuLabel.alignment = TextAlignmentOptions.Center;
        PLuLabel.DestroyTranslator();
        PLuLabel.text = "+";
        PLuLabel.color = new Color(255f, 255f, 255f);
        PLuLabel.transform.localPosition = new Vector3(PLuLabel.transform.localPosition.x, PLuLabel.transform.localPosition.y + 0.26f, PLuLabel.transform.localPosition.z);
        PLuLabel.transform.localScale = new Vector3(12f, 4f, 1f);

        var plus = PlusFab.GetComponent<PassiveButton>();
        plus.OnClick.RemoveAllListeners();
        plus.OnClick.AddListener(
                (UnityEngine.Events.UnityAction)(() =>
                {
                    if (PresetBehaviour == null) __instance.ChangeTab(3, false);
                    PresetBehaviour.Increase();
                }));
        plus.activeTextColor = new Color(255f, 255f, 255f);
        plus.inactiveTextColor = new Color(255f, 255f, 255f);
        plus.disabledTextColor = new Color(255f, 255f, 255f);
        plus.selectedTextColor = new Color(255f, 255f, 255f);

        plus.transform.localPosition = new Vector3(-0.4f, -3.37f, -4f);

        var GameSettingsLabel = __instance.GameSettingsButton.transform.parent.parent.FindChild("GameSettingsLabel").GetComponent<TextMeshPro>();
        GameSettingsLabel.DestroyTranslator();
        GameSettingsLabel.text = GetString($"{Options.CurrentGameMode}");

        var FreeChatField = DestroyableSingleton<ChatController>.Instance.freeChatField;
        var TextField = Object.Instantiate(FreeChatField, ParentLeftPanel.parent);
        TextField.transform.localScale = new Vector3(0.3f, 0.59f, 1);
        TextField.transform.localPosition = new Vector3(-2.07f, -2.57f, -5f);
        TextField.textArea.outputText.transform.localScale = new Vector3(3.5f, 2f, 1f);
        TextField.textArea.outputText.font = PLuLabel.font;
        TextField.name = "InputField";

        InputField = TextField;


        var button = TextField.transform.FindChild("ChatSendButton");

        Object.Destroy(button.FindChild("Normal").FindChild("Icon").GetComponent<SpriteRenderer>());
        Object.Destroy(button.FindChild("Hover").FindChild("Icon").GetComponent<SpriteRenderer>());
        Object.Destroy(button.FindChild("Disabled").FindChild("Icon").GetComponent<SpriteRenderer>());
        Object.Destroy(button.transform.FindChild("Text").GetComponent<TextMeshPro>());

        button.FindChild("Normal").FindChild("Background").GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite("TOHE.Resources.Images.SearchIconActive.png", 100f);
        button.FindChild("Hover").FindChild("Background").GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite("TOHE.Resources.Images.SearchIconHover.png", 100f);
        button.FindChild("Disabled").FindChild("Background").GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite("TOHE.Resources.Images.SearchIcon.png", 100f);

        if (DestroyableSingleton<TranslationController>.Instance.currentLanguage.languageID == SupportedLangs.Russian)
        {
            Vector3 FixedScale = new(0.7f, 1f, 1f);
            button.FindChild("Normal").FindChild("Background").transform.localScale = FixedScale;
            button.FindChild("Hover").FindChild("Background").transform.localScale = FixedScale;
            button.FindChild("Disabled").FindChild("Background").transform.localScale = FixedScale;
        }

        PassiveButton passiveButton = button.GetComponent<PassiveButton>();

        passiveButton.OnClick = new();
        passiveButton.OnClick.AddListener(
                (UnityEngine.Events.UnityAction)(() =>
                {
                    SearchForOptions(TextField);
                }));

        _SearchForOptions = (() =>
        {
            if (TextField.textArea.text == string.Empty)
                return;

            passiveButton.ReceiveClickDown();
        });

        static void SearchForOptions(FreeChatInputField textField)
        {
            if (ModGameOptionsMenu.TabIndex < 3) return;

            HiddenBySearch.Do(x => x.SetHidden(false));
            string text = textField.textArea.text.Trim().ToLower();
            var Result = OptionItem.AllOptions.Where(x => x.Parent == null && !x.IsHiddenOn(Options.CurrentGameMode)
            && !GetString($"{x.Name}").ToLower().Contains(text) && x.Tab == (TabGroup)(ModGameOptionsMenu.TabIndex - 3)).ToList();
            HiddenBySearch = Result;
            var SearchWinners = OptionItem.AllOptions.Where(x => x.Parent == null && !x.IsHiddenOn(Options.CurrentGameMode) && x.Tab == (TabGroup)(ModGameOptionsMenu.TabIndex - 3) && !Result.Contains(x)).ToList();
            if (!SearchWinners.Any() || !ModSettingsTabs.TryGetValue((TabGroup)(ModGameOptionsMenu.TabIndex - 3), out var settingsTab) || settingsTab == null)
            {
                HiddenBySearch.Clear();
                Logger.SendInGame(GetString("SearchNoResult")); // okay so showpopup nor this will overlay the menu, but I use this just for the sound lol
                return;
            }

            Result.Do(x => x.SetHidden(true));

            GameOptionsMenuPatch.ReCreateSettings(settingsTab);
            textField.Clear();
        }
    }

    [HarmonyPatch(nameof(GameSettingMenu.ChangeTab)), HarmonyPrefix]
    public static bool ChangeTabPrefix(GameSettingMenu __instance, ref int tabNum, [HarmonyArgument(1)] bool previewOnly)
    {
        if (HiddenBySearch.Any())
        {
            HiddenBySearch.Do(x => x.SetHidden(false));
            if (ModSettingsTabs.TryGetValue((TabGroup)(ModGameOptionsMenu.TabIndex - 3), out var GameSettingsTab) && GameSettingsTab != null)
                GameOptionsMenuPatch.ReCreateSettings(GameSettingsTab);

            HiddenBySearch.Clear();
        }

        if (!previewOnly || tabNum != 1) ModGameOptionsMenu.TabIndex = tabNum;

        GameOptionsMenu settingsTab;
        PassiveButton button;

        if ((previewOnly && Controller.currentTouchType == Controller.TouchType.Joystick) || !previewOnly)
        {
            foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
            {
                if (ModSettingsTabs.TryGetValue(tab, out settingsTab) &&
                    settingsTab != null)
                {
                    settingsTab.gameObject.SetActive(false);
                }
            }
            foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
            {
                if (ModSettingsButtons.TryGetValue(tab, out button) &&
                    button != null)
                {
                    button.SelectButton(false);
                }
            }
        }

        if (tabNum < 3) return true;

        var tabGroupId = (TabGroup)(tabNum - 3);
        if ((previewOnly && Controller.currentTouchType == Controller.TouchType.Joystick) || !previewOnly)
        {
            __instance.PresetsTab.gameObject.SetActive(false);
            __instance.GameSettingsTab.gameObject.SetActive(false);
            __instance.RoleSettingsTab.gameObject.SetActive(false);
            __instance.GamePresetsButton.SelectButton(false);
            __instance.GameSettingsButton.SelectButton(false);
            __instance.RoleSettingsButton.SelectButton(false);

            if (ModSettingsTabs.TryGetValue(tabGroupId, out settingsTab) && settingsTab != null)
            {
                settingsTab.gameObject.SetActive(true);
                __instance.MenuDescriptionText.DestroyTranslator();
                switch (tabGroupId)
                {
                    case TabGroup.SystemSettings:
                    case TabGroup.ModSettings:
                        __instance.MenuDescriptionText.text = GetString("TabMenuDescription_General");
                        break;
                    case TabGroup.ImpostorRoles:
                    case TabGroup.CrewmateRoles:
                    case TabGroup.NeutralRoles:
                    case TabGroup.CovenRoles:
                    case TabGroup.Addons:
                        __instance.MenuDescriptionText.text = GetString("TabMenuDescription_Roles&AddOns");
                        break;
                }
            }
        }

        if (previewOnly)
        {
            __instance.ToggleLeftSideDarkener(false);
            __instance.ToggleRightSideDarkener(true);
            return false;
        }
        __instance.ToggleLeftSideDarkener(true);
        __instance.ToggleRightSideDarkener(false);

        if (ModSettingsButtons.TryGetValue(tabGroupId, out button) &&
            button != null)
        {
            button.SelectButton(true);
        }

        return false;
    }

    [HarmonyPatch(nameof(GameSettingMenu.OnEnable)), HarmonyPrefix]
    private static bool OnEnablePrefix(GameSettingMenu __instance)
    {

        if (TemplateGameOptionsMenu == null)
        {
            TemplateGameOptionsMenu = Object.Instantiate(__instance.GameSettingsTab, __instance.GameSettingsTab.transform.parent);
            TemplateGameOptionsMenu.gameObject.SetActive(false);
        }
        if (TemplateGameSettingsButton == null)
        {
            TemplateGameSettingsButton = Object.Instantiate(__instance.GameSettingsButton, __instance.GameSettingsButton.transform.parent);
            TemplateGameSettingsButton.gameObject.SetActive(false);
        }
        ModGameOptionsMenu.OptionList = new();
        ModGameOptionsMenu.BehaviourList = new();
        ModGameOptionsMenu.CategoryHeaderList = new();

        SetDefaultButton(__instance);

        ControllerManager.Instance.OpenOverlayMenu(__instance.name, __instance.BackButton, __instance.DefaultButtonSelected, __instance.ControllerSelectable, false);
        DestroyableSingleton<HudManager>.Instance.menuNavigationPrompts.SetActive(false);
        if (Controller.currentTouchType != Controller.TouchType.Joystick)
        {
            __instance.ChangeTab(1, Controller.currentTouchType == Controller.TouchType.Joystick);
        }
        __instance.StartCoroutine(__instance.CoSelectDefault());

        return false;
    }
    [HarmonyPatch(nameof(GameSettingMenu.Close)), HarmonyPostfix]
    private static void ClosePostfix(GameSettingMenu __instance)
    {
        foreach (var button in ModSettingsButtons.Values)
            Object.Destroy(button);
        foreach (var tab in ModSettingsTabs.Values)
            Object.Destroy(tab);
        ModSettingsButtons = [];
        ModSettingsTabs = [];
    }
}
[HarmonyPatch(typeof(FreeChatInputField), nameof(FreeChatInputField.UpdateCharCount))]
public static class FixInputChatField
{
    public static bool Prefix(FreeChatInputField __instance)
    {
        if (GameSettingMenuPatch.InputField != null && __instance == GameSettingMenuPatch.InputField)
        {
            Vector2 size = __instance.Background.size;
            size.y = Math.Max(0.62f, __instance.textArea.TextHeight + 0.2f);
            __instance.Background.size = size;
            return false;
        }
        return true;
    }
}
[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
public static class FixDarkThemeForSearchBar
{
    public static void Postfix()
    {
        if (!GameSettingMenu.Instance || !Main.DarkTheme.Value) return;
        var field = GameSettingMenuPatch.InputField;
        if (field != null)
        {
            field.background.color = new Color32(40, 40, 40, byte.MaxValue);
            field.textArea.compoText.Color(Color.white);
            field.textArea.outputText.color = Color.white;
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
public class RpcSyncSettingsPatch
{
    public static void Postfix()
    {
        OptionItem.SyncAllOptions();
    }
}
