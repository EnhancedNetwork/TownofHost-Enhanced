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

namespace TOHE.Patches
{
    [HarmonyPatch(typeof(GameSettingMenu))]
    internal class GameSettingMenuPatch
    {

        [HarmonyPatch(nameof(GameSettingMenu.OnEnable)), HarmonyPostfix]
        public static void EnablePrefix(GameSettingMenu __instance)
        {
            TemplateButton ??= Object.Instantiate(__instance.GamePresetsButton, __instance.GamePresetsButton.transform.parent);
            TemplateButton.transform.localScale = new Vector3(0.4f, 0.7f, 1f);
            var TLabel = TemplateButton.transform.Find("FontPlacer/Text_TMP").GetComponent<TextMeshPro>();
            TLabel.alignment = TextAlignmentOptions.Center;
            TLabel.transform.localScale = new Vector3(1.2f, TLabel.transform.localScale.y, TLabel.transform.localScale.z);
            TemplateButton.gameObject.SetActive(false);
        }




        [HarmonyPatch(nameof(GameSettingMenu.Start)), HarmonyPrefix]
        public static void StartPrefix(GameSettingMenu __instance)
        {

            // Unlocks map/impostor amount changing in online (for testing on your custom servers)
            // Changed to be able to change the map in online mode without having to re-establish the room.
            __instance.GameSettingsTab.HideForOnline = new Il2CppReferenceArray<Transform>(0);
        }

        private static Dictionary<TabGroup, PassiveButton> ModButtons = [];
        private static PassiveButton TemplateButton;

        [HarmonyPatch(nameof(GameSettingMenu.Start)), HarmonyPostfix]
        public static void StartPostfix(GameSettingMenu __instance)
        {
            Transform ParentLeftPanel = GameObject.Find("LeftPanel").transform;

            var gamepreset = __instance.GamePresetsButton;
            gamepreset.gameObject.SetActive(false);

            var gamesettings = __instance.GameSettingsButton;
            __instance.GameSettingsButton.transform.localScale = new Vector3(0.4f, 0.7f, 1f);
            __instance.GameSettingsButton.transform.localPosition = new Vector3(-2.5f, -0.5f, -2.0f);
            var GLabel = gamesettings.transform.Find("FontPlacer/Text_TMP").GetComponent<TextMeshPro>();
            GLabel.alignment = TextAlignmentOptions.Center;
            GLabel.transform.localScale = new Vector3(1.2f, GLabel.transform.localScale.y, GLabel.transform.localScale.z);
            SetButtonColor(ref gamesettings, ref GLabel, new Color32(72, 86, 217, 255));

            Logger.Info($"{gamesettings.transform.localPosition}", "GameSettings POS");

            var TempMinus = GameObject.Find("MinusButton").gameObject;
            var GMinus = GameObject.Instantiate(gamepreset, ParentLeftPanel);
            GMinus.gameObject.SetActive(true);
            GMinus.transform.localScale = new Vector3(0.08f, 0.4f, 1f);

            var MLabel = GMinus.transform.Find("FontPlacer/Text_TMP").GetComponent<TextMeshPro>();
            MLabel.alignment = TextAlignmentOptions.Center;
            _ = new LateTask(() => {
                MLabel.text = "-";
                MLabel.transform.localPosition = new Vector3(MLabel.transform.localPosition.x, MLabel.transform.localPosition.y + 0.26f, MLabel.transform.localPosition.z);
                MLabel.color = new Color(255f, 255f, 255f);
                MLabel.SetFaceColor(new Color(255f, 255f, 255f));
                MLabel.transform.localScale = new Vector3(12f, 4f, 1f);
            }, 0.05f, shoudLog: false);

            var Minus = GMinus.GetComponent<PassiveButton>();
            Minus.OnClick.RemoveAllListeners();
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
           // PlusFab.transform.localScale = new Vector3(1f, 1f, 1f);
            _ = new LateTask(() => {
                PLuLabel.text = "+";
                PLuLabel.color = new Color(255f, 255f, 255f);
                PLuLabel.transform.localPosition = new Vector3(PLuLabel.transform.localPosition.x, PLuLabel.transform.localPosition.y + 0.26f, PLuLabel.transform.localPosition.z);
                PLuLabel.transform.localScale = new Vector3(12f, 4f, 1f);
            }, 0.05f, shoudLog: false);
            var plus = PlusFab.GetComponent<PassiveButton>();
            plus.OnClick.RemoveAllListeners();
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
            _ = new LateTask(() => {
                PLabel.text = GetString($"Preset_{OptionItem.CurrentPreset + 1}");
                PLabel.font = PLuLabel.font;
                PLabel.fontSizeMax = 3.45f; PLabel.fontSizeMin = 3.45f;
            }, 0.05f, shoudLog: false);
            plus.transform.parent = preset.transform;
            Minus.transform.parent = preset.transform;



            var rolesettings = __instance.RoleSettingsButton;
            __instance.RoleSettingsButton.transform.localScale = new Vector3(0.4f, 0.7f, 1f);
            __instance.RoleSettingsButton.transform.localPosition = new Vector3(-3.9f, -1.1f, -2.0f);
            var RLabel = rolesettings.transform.Find("FontPlacer/Text_TMP").GetComponent<TextMeshPro>();
            RLabel.alignment = TextAlignmentOptions.Center; RLabel.transform.localScale = new Vector3(1.2f, RLabel.transform.localScale.y, RLabel.transform.localScale.z);
            SetButtonColor(ref rolesettings, ref RLabel, new Color32(128, 31, 219, 255));


            // Gonna automate below buttons later ig 

            //button 1
            GameObject template = rolesettings.gameObject;
            GameObject targetBox = Object.Instantiate(TemplateButton.gameObject, ParentLeftPanel);
            targetBox.name = "System Settings";
            targetBox.transform.localPosition = new Vector3(-2.5f, -1.1f, -2.0f);
            targetBox.gameObject.SetActive(true);


            var SystemButton = targetBox.GetComponent<PassiveButton>();
            SystemButton.OnClick.RemoveAllListeners();
            SystemButton.OnClick.AddListener(
                (Action)(() => __instance.ChangeTab((int)TabGroup.SystemSettings + 3, false)));

            var label = SystemButton.transform.Find("FontPlacer/Text_TMP").GetComponent<TextMeshPro>();
            _ = new LateTask(() => { label.text = GetString("TabGroup.SystemSettings"); }, 0.05f, "Set Button1 Text");
            SetButtonColor(ref SystemButton, ref label, new Color32(199, 109, 124, 255));
            ModButtons.Add(TabGroup.SystemSettings, SystemButton);

            //button 2
            GameObject template2 = targetBox.gameObject;
            GameObject targetBox2 = Object.Instantiate(TemplateButton.gameObject, ParentLeftPanel);
            targetBox2.name = "Mod Settings";
            targetBox2.transform.localPosition = new Vector3(-3.9f, -1.7f, -2.0f);
            targetBox2.gameObject.SetActive(true);

            var ModConfButton = targetBox2.GetComponent<PassiveButton>();
            ModConfButton.OnClick.RemoveAllListeners();
            ModConfButton.OnClick.AddListener(
                (Action)(() => __instance.ChangeTab((int)TabGroup.ModSettings + 3, false)));

            var label2 = ModConfButton.transform.Find("FontPlacer/Text_TMP").GetComponent<TextMeshPro>();
            _ = new LateTask(() => { label2.text = GetString("TabGroup.ModSettings"); }, 0.05f, "Set Button2 Text");
            SetButtonColor(ref ModConfButton, ref label2, new Color32(89, 239, 131, 255));
            ModButtons.Add(TabGroup.ModSettings, ModConfButton);

            //button 3
            GameObject template3 = targetBox2.gameObject;
            GameObject targetBox3 = Object.Instantiate(TemplateButton.gameObject, ParentLeftPanel);
            targetBox3.name = "Game Modifiers";
            targetBox3.transform.localPosition = new Vector3(-2.5f, -1.7f, -2.0f);
            targetBox3.gameObject.SetActive(true);

            var GameModifButton = targetBox3.GetComponent<PassiveButton>();
            GameModifButton.OnClick.RemoveAllListeners();
            GameModifButton.OnClick.AddListener(
                (Action)(() => __instance.ChangeTab((int)TabGroup.ModifierSettings + 3, false)));

            var label3 = GameModifButton.transform.Find("FontPlacer/Text_TMP").GetComponent<TextMeshPro>();
            _ = new LateTask(() => { label3.text = GetString("TabGroup.ModifierSettings"); }, 0.05f, "Set Button3 Text");
            SetButtonColor(ref GameModifButton, ref label3, new Color32(239, 89, 175, 255));
            ModButtons.Add(TabGroup.ModifierSettings, GameModifButton);




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
        }


        [HarmonyPatch(nameof(GameSettingMenu.ChangeTab)), HarmonyPrefix]

        public static bool ChangePrefix(GameSettingMenu __instance, ref int tabNum, [HarmonyArgument(1)] bool previewOnly)
        {

            if (tabNum == 0) tabNum = 1;

            PassiveButton button;

            if ((previewOnly && Controller.currentTouchType == Controller.TouchType.Joystick) || !previewOnly)
            {
                foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
                {
                    if (ModButtons.TryGetValue(tab, out button) &&
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

            }

            if (previewOnly)
            {
                __instance.ToggleLeftSideDarkener(false);
                __instance.ToggleRightSideDarkener(true);
                return false;
            }
            __instance.ToggleLeftSideDarkener(true);
            __instance.ToggleRightSideDarkener(false);

            if (ModButtons.TryGetValue((TabGroup)(tabNum - 3), out button) &&
                button != null)
            {
                button.SelectButton(true);
            }

            return false;
        }
        [HarmonyPatch(nameof(GameSettingMenu.ChangeTab)), HarmonyPostfix]
        public static void ChangePostfix(GameSettingMenu __instance, [HarmonyArgument(0)] int tabNum)
        {

            if (tabNum == 1 && __instance.GameSettingsTab.isActiveAndEnabled)
            {

                _ = new LateTask(() => {
                    __instance.MenuDescriptionText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameSettingsDescription);
                    __instance.GameSettingsButton.SelectButton(true);
                }, 0.05f, "Fix Menu Description Text");
                return;
            }

        }



    }
    }

