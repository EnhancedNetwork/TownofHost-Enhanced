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

    [HarmonyPatch(nameof(GameSettingMenu.Start)), HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void StartPostfix(GameSettingMenu __instance)
    {
        TabGroup[] ExludeList = Options.CurrentGameMode switch
        {
            CustomGameMode.HidenSeekTOHE => Enum.GetValues<TabGroup>().Skip(3).ToArray(),
            CustomGameMode.FFA => Enum.GetValues<TabGroup>().Skip(2).ToArray(),
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
                TabGroup.ModifierSettings => "#EF59AF",
                TabGroup.ImpostorRoles => "#f74631",
                TabGroup.CrewmateRoles => "#8cffff",
                TabGroup.NeutralRoles => "#7f8c8d",
                TabGroup.Addons => "#ff9ace",
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
                (Action)(() => __instance.ChangeTab((int)tab + 3, false)));

            ModSettingsButtons.Add(tab, button);
        }

        ModGameOptionsMenu.OptionList = new();
        ModGameOptionsMenu.BehaviourList = new();
        ModGameOptionsMenu.CategoryHeaderList = new();

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
        //gameSettingButton.activeTextColor = gameSettingButton.inactiveTextColor = Color.black;
        //gameSettingButton.selectedTextColor = Color.blue;

        //var vanillaActiveButton = Utils.LoadSprite($"TownOfHost_Y.Resources.Tab_Active_VanillaGameSettings.png", 100f);
        //gameSettingButton.inactiveSprites.GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite($"TownOfHost_Y.Resources.Tab_Small_VanillaGameSettings.png", 100f);
        //gameSettingButton.activeSprites.GetComponent<SpriteRenderer>().sprite = vanillaActiveButton;
        //gameSettingButton.selectedSprites.GetComponent<SpriteRenderer>().sprite = vanillaActiveButton;

        gameSettingButton.transform.localPosition = ButtonPositionLeft;
        gameSettingButton.transform.localScale = ButtonSize;

        __instance.RoleSettingsButton.gameObject.SetActive(false);

        __instance.DefaultButtonSelected = gameSettingButton;
        __instance.ControllerSelectable = new();
        __instance.ControllerSelectable.Add(gameSettingButton);
    }

    [HarmonyPatch(nameof(GameSettingMenu.ChangeTab)), HarmonyPrefix]
    public static bool ChangeTabPrefix(GameSettingMenu __instance, ref int tabNum, [HarmonyArgument(1)] bool previewOnly)
    {
        ModGameOptionsMenu.TabIndex = tabNum;

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
                    case TabGroup.ModifierSettings:
                        __instance.MenuDescriptionText.text = GetString("TabMenuDescription_General");
                        break;
                    case TabGroup.ImpostorRoles:
                    case TabGroup.CrewmateRoles:
                    case TabGroup.NeutralRoles:
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

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
public class RpcSyncSettingsPatch
{
    public static void Postfix()
    {
        OptionItem.SyncAllOptions();
    }
}