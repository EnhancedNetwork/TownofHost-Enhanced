using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Patches
{

    [HarmonyPatch(typeof(GameSettingMenu))]
    internal class GameSettingsMenuPatch
    {
        private static readonly Vector3 ButtonPositionLeft = new(-3.9f, -0.4f, 0f);
        private static readonly Vector3 ButtonPositionRight = new(-2.4f, -0.4f, 0f);

        private static readonly Vector3 ButtonSize = new(0.45f, 0.6f, 1f);

        private static GameOptionsMenu TemplateGameOptionsMenu;
        private static PassiveButton TemplateGameSettingsButton;

        static Dictionary<TabGroup, PassiveButton> ModSettingsButtons = [];
        static Dictionary<TabGroup, GameOptionsMenu> ModSettingsTabs = [];

        private static void SetDefaultButton(GameSettingMenu __instance)
        {
            __instance.GamePresetsButton.gameObject.SetActive(false);

            var gameSettingButton = __instance.GameSettingsButton;
            gameSettingButton.transform.localPosition = new(-3f, -0.5f, 0f);

            var textLabel = gameSettingButton.GetComponentInChildren<TextMeshPro>();
            textLabel.DestroyTranslator();
            textLabel.text = Translator.GetString("TabVanilla.GameSettings");

            gameSettingButton.transform.localPosition = ButtonPositionRight;
            gameSettingButton.transform.localScale = ButtonSize;

            __instance.RoleSettingsButton.gameObject.SetActive(false);

            __instance.DefaultButtonSelected = gameSettingButton;
            __instance.ControllerSelectable = new();
            __instance.ControllerSelectable.Add(gameSettingButton);
        }
        [HarmonyPatch(nameof(GameSettingMenu.OnEnable)), HarmonyPrefix]
        private static bool OnEnablePrefix(GameSettingMenu __instance)
        {
            TemplateGameSettingsButton ??= GameObject.Instantiate(__instance.GamePresetsButton, __instance.GameSettingsTab.transform.parent);
            TemplateGameSettingsButton.transform.localScale = new Vector3(0.4f, 0.7f, 1f);
            var TLabel = TemplateGameSettingsButton.transform.Find("FontPlacer/Text_TMP").GetComponent<TextMeshPro>();
            TLabel.alignment = TextAlignmentOptions.Center;
            TLabel.transform.localScale = new Vector3(1.2f, TLabel.transform.localScale.y, TLabel.transform.localScale.z);
            TemplateGameSettingsButton.gameObject.SetActive(false);

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

        [HarmonyPatch(nameof(GameSettingMenu.Start)), HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static void StartPostFix(GameSettingMenu __instance)
        {
            foreach (var tab in Enum.GetValues<TabGroup>().TakeWhile(x => (int)x > 20))
            {
                var button = GameObject.Instantiate(TemplateGameSettingsButton, __instance.GameSettingsButton.transform.parent);
                button.gameObject.SetActive(true);
                button.name = "Button_" + tab; 

                var label = button.GetComponentInChildren<TextMeshPro>();

                label.DestroyTranslator();

                string tabcolor = tab switch
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
                // Set color
                //button.HeldButtonSprite.color = new Color(255, 192, 203);
                // ボタンテキストの名前変更
                label.text = $"<color={tabcolor}>{Translator.GetString("TabGroup." + tab)}</color>";
                // ボタンテキストの色変更
                //button.activeTextColor = button.inactiveTextColor = Color.black;
                // ボタンテキストの選択中の色変更
                //button.selectedTextColor = Color.blue;

                //var activeButton = Utils.LoadSprite($"TownOfHost_Y.Resources.Tab_Active_{tab}.png", 100f);
                //// 各種スプライトをオリジナルのものに変更
                //button.inactiveSprites.GetComponent<SpriteRenderer>().color = tabcolor;
                //button.activeSprites.GetComponent<SpriteRenderer>().color = tabcolor;
                //button.selectedSprites.GetComponent<SpriteRenderer>().color = tabcolor;

                // Y座標オフセット
                Vector3 offset = new(0.0f, 0.5f * (((int)tab + 1) / 2), 0.0f);
                // ボタンの座標設定
                button.transform.localPosition = ((((int)tab + 1) % 2 == 0) ? ButtonPositionRight : ButtonPositionRight) - offset;
                // ボタンのサイズ設定
                button.transform.localScale = new Vector3(1f, 1f, 1f);

                // ボタンがクリックされた時の設定
                var buttonComponent = button.GetComponent<PassiveButton>();
                buttonComponent.OnClick = new();
                // ボタンがクリックされるとタブをそのものに変更する
                buttonComponent.OnClick.AddListener(
                    (Action)(() => __instance.ChangeTab((int)tab + 3, false)));

                // ボタン登録
                ModSettingsButtons.Add(tab, button);

            }

        }

        [HarmonyPatch(nameof(GameSettingMenu.ChangeTab)), HarmonyPrefix]
        public static bool ChangeTabPrefix(GameSettingMenu __instance, ref int tabNum, [HarmonyArgument(1)] bool previewOnly)
        {
            //GameOptionsMenu settingsTab;
            PassiveButton button;

            if ((previewOnly && Controller.currentTouchType == Controller.TouchType.Joystick) || !previewOnly)
            {
                /*foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
                {
                    if (ModSettingsTabs.TryGetValue(tab, out settingsTab) &&
                        settingsTab != null)
                    {
                        settingsTab.gameObject.SetActive(false);
                    }
                }*/ // To be decided
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

            if ((previewOnly && Controller.currentTouchType == Controller.TouchType.Joystick) || !previewOnly)
            {
                __instance.PresetsTab.gameObject.SetActive(false);
                __instance.GameSettingsTab.gameObject.SetActive(false);
                __instance.RoleSettingsTab.gameObject.SetActive(false);
                __instance.GamePresetsButton.SelectButton(false);
                __instance.GameSettingsButton.SelectButton(false);
                __instance.RoleSettingsButton.SelectButton(false);

               /* if (ModSettingsTabs.TryGetValue((TabGroup)(tabNum - 3), out settingsTab) && settingsTab != null)
                {
                    settingsTab.gameObject.SetActive(true);
                    __instance.MenuDescriptionText.DestroyTranslator();
                    switch ((TabGroup)(tabNum - 3))
                    {
                        case TabGroup.SystemSettings:
                        case TabGroup.ModSettings:
                        case TabGroup.ModifierSettings:
                            __instance.MenuDescriptionText.text = GetString("TabMenuDescription_General");
                            break;
                    }
                }*/ // To be decided
            }

            if (previewOnly)
            {
                __instance.ToggleLeftSideDarkener(false);
                __instance.ToggleRightSideDarkener(true);
                return false;
            }
            __instance.ToggleLeftSideDarkener(true);
            __instance.ToggleRightSideDarkener(false);

            if (ModSettingsButtons.TryGetValue((TabGroup)(tabNum - 3), out button) &&
                button != null)
            {
                button.SelectButton(true);
            }

            return false;
        }
    }
}
