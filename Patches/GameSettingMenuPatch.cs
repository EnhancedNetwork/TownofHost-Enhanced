using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TOHE.Translator;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using static UnityEngine.RemoteConfigSettingsHelper;
using TOHE;

namespace TOHE.Patches
{
    [HarmonyPatch(typeof(GameSettingMenu))]
    internal class GameSettingMenuPatch
    {

        private static void SetDefaultButton(GameSettingMenu __instance)
        {
            __instance.GamePresetsButton.gameObject.SetActive(false);

            var gameSettingButton = __instance.GameSettingsButton;
            __instance.GameSettingsButton.transform.localScale = new Vector3(0.4f, 0.7f, 1f);
            __instance.GameSettingsButton.transform.localPosition = new Vector3(-2.5f, -0.5f, -2.0f);
            var GLabel = gameSettingButton.transform.Find("FontPlacer/Text_TMP").GetComponent<TextMeshPro>();
            GLabel.alignment = TextAlignmentOptions.Center;
            GLabel.transform.localScale = new Vector3(1.2f, GLabel.transform.localScale.y, GLabel.transform.localScale.z);
            SetButtonColor(ref gameSettingButton, ref GLabel, new Color32(72, 86, 217, 255));

            var textLabel = gameSettingButton.GetComponentInChildren<TextMeshPro>();
            textLabel.DestroyTranslator();
            textLabel.fontStyle = FontStyles.UpperCase;
            textLabel.text = GetString("TabVanilla.GameSettings");

            __instance.DefaultButtonSelected = gameSettingButton;
            __instance.ControllerSelectable = new();
            __instance.ControllerSelectable.Add(gameSettingButton);
        }

        [HarmonyPatch(nameof(GameSettingMenu.OnEnable)), HarmonyPrefix]
        public static bool EnablePrefix(GameSettingMenu __instance)
        {
            if (TemplateGameSettingsButton == null)
            {
                TemplateGameSettingsButton = Object.Instantiate(__instance.GamePresetsButton, __instance.GamePresetsButton.transform.parent);
                TemplateGameSettingsButton.transform.localScale = new Vector3(0.4f, 0.7f, 1f);
                var TLabel = TemplateGameSettingsButton.transform.Find("FontPlacer/Text_TMP").GetComponent<TextMeshPro>();
                TLabel.alignment = TextAlignmentOptions.Center;
                TLabel.transform.localScale = new Vector3(1.2f, TLabel.transform.localScale.y, TLabel.transform.localScale.z);
                TemplateGameSettingsButton.gameObject.SetActive(false);
            }
            if (TemplateGameOptionsMenu == null)
            {
                TemplateGameOptionsMenu = Object.Instantiate(__instance.GameSettingsTab, __instance.GameSettingsTab.transform.parent);
                TemplateGameOptionsMenu.transform.localPosition = new Vector3(-2.97f, TemplateGameOptionsMenu.transform.localPosition.y - 0.82f, TemplateGameOptionsMenu.transform.localPosition.z);
                TemplateGameOptionsMenu.gameObject.SetActive(false);
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




        [HarmonyPatch(nameof(GameSettingMenu.Start)), HarmonyPrefix]
        public static void StartPrefix(GameSettingMenu __instance)
        {

            // Unlocks map/impostor amount changing in online (for testing on your custom servers)
            // Changed to be able to change the map in online mode without having to re-establish the room.
            __instance.GameSettingsTab.HideForOnline = new Il2CppReferenceArray<Transform>(0);
        }

        private static readonly Vector3 ButtonPositionLeft = new(-3.9f, -1.7f, -2.0f);
        private static readonly Vector3 ButtonPositionRight = new(-2.5f, -1.1f, -2.0f);

        static Dictionary<TabGroup, PassiveButton> ModSettingsButtons = [];
        static Dictionary<TabGroup, GameOptionsMenu> ModSettingsTabs = [];
        private static GameOptionsMenu TemplateGameOptionsMenu;
        private static PassiveButton TemplateGameSettingsButton;
        public static OptionItem Presetitem;
        public static Action UpdatePreset;
        public static StringOption PresetBehaviour;

        [HarmonyPatch(nameof(GameSettingMenu.Start)), HarmonyPostfix]
        public static void StartPostfix(GameSettingMenu __instance)
        {
            Transform ParentLeftPanel = GameObject.Find("LeftPanel").transform;

            ModGameOptionsMenu.OptionList = new();
            ModGameOptionsMenu.BehaviourList = new();
            ModGameOptionsMenu.CategoryHeaderList = new();

            ModSettingsTabs = [];
            ModSettingsButtons = [];

            var gamepreset = __instance.GamePresetsButton;
            gamepreset.gameObject.SetActive(false);


            var TempMinus = GameObject.Find("MinusButton").gameObject;
            var GMinus = GameObject.Instantiate(gamepreset, ParentLeftPanel);
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
                    (Action)(() => { Presetitem.CurrentValue = Presetitem.CurrentValue - 1; 
                        GameSettingMenuPatch.UpdatePreset?.Invoke();
                        if (PresetBehaviour != null)
                        {
                            PresetBehaviour.Value = Presetitem.CurrentValue;
                            PresetBehaviour.OnValueChanged?.Invoke(PresetBehaviour);
                        }
                        GameOptionsMenuPatch.UpdateSettings();
                    }));
            Minus.activeTextColor = new Color(255f, 255f, 255f);
            Minus.inactiveTextColor = new Color(255f, 255f, 255f);
            Minus.disabledTextColor = new Color(255f, 255f, 255f);
            Minus.selectedTextColor = new Color(255f, 255f, 255f);

            Minus.transform.localPosition = new Vector3(-4.55f, -0.51f, -2.0f);
            Minus.inactiveSprites.GetComponent<SpriteRenderer>().sprite = TempMinus.GetComponentInChildren<SpriteRenderer>().sprite;
            Minus.activeSprites.GetComponent<SpriteRenderer>().sprite = TempMinus.GetComponentInChildren<SpriteRenderer>().sprite;
            Minus.selectedSprites.GetComponent<SpriteRenderer>().sprite = TempMinus.GetComponentInChildren<SpriteRenderer>().sprite;

            Minus.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color32(55, 54, 54, 255);
            Minus.activeSprites.GetComponent<SpriteRenderer>().color = new Color32(61, 59, 59, 255);
            Minus.selectedSprites.GetComponent<SpriteRenderer>().color = new Color32(55, 54, 54, 255);



            var PlusFab = GameObject.Instantiate(GMinus, ParentLeftPanel);
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
                    (Action)(() => { Presetitem.CurrentValue = Presetitem.CurrentValue + 1; 
                        GameSettingMenuPatch.UpdatePreset?.Invoke();
                        if (PresetBehaviour != null)
                        {
                            PresetBehaviour.Value = Presetitem.CurrentValue;
                            PresetBehaviour.OnValueChanged?.Invoke(PresetBehaviour);
                        }
                        GameOptionsMenuPatch.UpdateSettings();
                    }));
            plus.activeTextColor = new Color(255f, 255f, 255f);
            plus.inactiveTextColor = new Color(255f, 255f, 255f);
            plus.disabledTextColor = new Color(255f, 255f, 255f);
            plus.selectedTextColor = new Color(255f, 255f, 255f);


            plus.transform.localPosition = new Vector3(-3.54f, -0.51f, -2.0f);



            var labeltag = GameObject.Find("PrivacyLabel");
            var preset = Object.Instantiate(labeltag, ParentLeftPanel);
            preset.transform.localPosition = new Vector3(-4.1f, -0.55f, -2.0f);
            preset.transform.localScale = new Vector3(1f, 1f, 1f);

            Color clr = new(-1, -1, -1);
            // sprite.set_color_Injected(ref clr);
            var PLabel = preset.GetComponentInChildren<TextMeshPro>();
                PLabel.DestroyTranslator();
                PLabel.text = GetString($"Preset_{OptionItem.CurrentPreset + 1}");
                PLabel.font = PLuLabel.font;
                PLabel.fontSizeMax = 3.45f; PLabel.fontSizeMin = 3.45f;
            UpdatePreset = () => { PLabel.text = Presetitem.GetString(); };
            
            plus.transform.parent = preset.transform;
            Minus.transform.parent = preset.transform;



            var rolesettings = __instance.RoleSettingsButton;
            __instance.RoleSettingsButton.transform.localScale = new Vector3(0.4f, 0.7f, 1f);
            __instance.RoleSettingsButton.transform.localPosition = new Vector3(-3.9f, -1.1f, -2.0f);
            var RLabel = rolesettings.transform.Find("FontPlacer/Text_TMP").GetComponent<TextMeshPro>();
            RLabel.alignment = TextAlignmentOptions.Center; RLabel.transform.localScale = new Vector3(1.2f, RLabel.transform.localScale.y, RLabel.transform.localScale.z);
            SetButtonColor(ref rolesettings, ref RLabel, new Color32(128, 31, 219, 255));


            var customTabs = Enum.GetValues<TabGroup>().Take(3); // only support for 3 tabs

            var currentoffset = 0f;
            foreach (var tab in customTabs)
            {
                var button = Object.Instantiate(TemplateGameSettingsButton, ParentLeftPanel);
                button.gameObject.SetActive(true);
                button.name = "Button_" + tab;
                var lable = button.GetComponentInChildren<TextMeshPro>();
                lable.DestroyTranslator();
                string htmlcolor = tab switch
                {
                    TabGroup.SystemSettings => Main.ModColor,
                    TabGroup.ModSettings => "#59ef83",
                    TabGroup.ModifierSettings => "#EF59AF",
                    _ => "#ffffff",
                };
                lable.fontStyle = FontStyles.UpperCase;
                _ = ColorUtility.TryParseHtmlString(htmlcolor, out Color tabColor);
                lable.text = $"<color={htmlcolor}>{GetString("TabGroup." + tab)}</color>";
                SetButtonColor(ref button, ref lable, tabColor);

                Vector3 offset = new(0.0f, currentoffset, 0.0f);
                button.transform.localPosition = ((((int)tab) % 2 == 0) ? ButtonPositionLeft : ButtonPositionRight) + offset;

                if ((int)tab % 2 == 0){
                    currentoffset -= 0.6f;
                }

                var buttonComponent = button.GetComponent<PassiveButton>();
                buttonComponent.OnClick = new();
                buttonComponent.OnClick.AddListener(
                    (Action)(() => __instance.ChangeTab((int)tab + 3, false)));

                ModSettingsButtons.Add(tab, button);
            }

            foreach (var tab in customTabs)
            {
                var setTab = Object.Instantiate(TemplateGameOptionsMenu, ParentLeftPanel);
                setTab.name = "tab_" + tab;
                setTab.gameObject.SetActive(false);

                ModSettingsTabs.Add(tab, setTab);
            }

            foreach (var tab in customTabs)
            {
                if (ModSettingsButtons.TryGetValue(tab, out var button))
                {
                    __instance.ControllerSelectable.Add(button);
                }
            }

        }
        static void SetButtonColor(ref PassiveButton btn, ref TextMeshPro Tob, Color32 col)
        {
            Color clr = col;
            btn.inactiveSprites.GetComponent<SpriteRenderer>().color = clr;
            btn.activeSprites.GetComponent<SpriteRenderer>().color = clr.ShadeColor(-0.5f);
            btn.selectedSprites.GetComponent<SpriteRenderer>().color = clr.ShadeColor(-0.5f);
            Color textC = clr.ShadeColor(-0.5f);

            btn.activeTextColor = textC.ShadeColor(-2f);
            btn.disabledTextColor = textC;
            btn.inactiveTextColor = textC;
            btn.selectedTextColor = textC.ShadeColor(-2f);

            Tob.color = textC;
            Tob.SetFaceColor(textC);


        }

        [HarmonyPatch(nameof(GameSettingMenu.ChangeTab)), HarmonyPrefix]
        public static bool ChangeTabPrefix(GameSettingMenu __instance, ref int tabNum, [HarmonyArgument(1)] bool previewOnly)
        {
            ModGameOptionsMenu.TabIndex = tabNum;

            GameOptionsMenu settingsTab;
            PassiveButton button;

            if (tabNum == 0) tabNum = 1;

            if ((previewOnly && Controller.currentTouchType == Controller.TouchType.Joystick) || !previewOnly)
            {
                foreach (var tab in EnumHelper.GetAllValues<TabGroup>().Take(3))
                {
                    if (ModSettingsTabs.TryGetValue(tab, out settingsTab) &&
                        settingsTab != null)
                    {
                        settingsTab.gameObject.SetActive(false);
                    }
                }
                foreach (var tab in EnumHelper.GetAllValues<TabGroup>().Take(3))
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
        [HarmonyPatch(nameof(GameSettingMenu.ChangeTab)), HarmonyPostfix]
        public static void ChangeTabpostfix(GameSettingMenu __instance, int tabNum)
        {
             string strang = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameSettingsDescription);
            if (__instance.MenuDescriptionText.text != strang && __instance.GameSettingsTab.isActiveAndEnabled)
            {
                __instance.GameSettingsButton.SelectButton(true);
                __instance.MenuDescriptionText.DestroyTranslator();
                __instance.MenuDescriptionText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameSettingsDescription);
            }
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
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
public class RpcSyncSettingsPatch
{
    public static void Postfix()
    {
        OptionItem.SyncAllOptions();
    }
}

