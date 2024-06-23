using AmongUs.GameOptions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using TMPro;
using UnityEngine;
using static TOHE.Translator;
using Object = UnityEngine.Object;

namespace TOHE;

//[HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
//[HarmonyPriority(Priority.First)]
//class GameSettingMenuStartPatch
//{
//    public static void Postfix(GameSettingMenu __instance)
//    {
//        // Need for Hide&Seek because tabs are disabled by default
//        // I dont know what this means..
//        __instance.GameSettingsTab.gameObject.SetActive(true);
//    }
////}
//[HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Close))]
//class GameSettingMenuClosePatch
//{
//    public static void Postfix()
//    {
//        // if custom game mode is HideNSeekTOHE in normal game, set standart
//        if (GameStates.IsNormalGame && Options.CurrentGameMode == CustomGameMode.HidenSeekTOHE)
//        {
//            // Select standart custom game mode
//            Options.GameMode.SetValue(0);
//        }
//    }
//}


[HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
public static class GameSettingMenuInitializeOptionsPatch
{
    public static void Prefix(GameSettingMenu __instance)
    {

        // Unlocks map/impostor amount changing in online (for testing on your custom servers)
        // Changed to be able to change the map in online mode without having to re-establish the room.
        __instance.GameSettingsTab.HideForOnline = new Il2CppReferenceArray<Transform>(0);
    }
    // Add Dleks to map selection
    public static void Postfix(GameSettingMenu __instance)
    {
        var gamepreset = __instance.GamePresetsButton;

        var gamesettings = __instance.GameSettingsButton;
        __instance.GameSettingsButton.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        __instance.GameSettingsButton.transform.localPosition = new Vector3(gamesettings.transform.localPosition.x, gamepreset.transform.localPosition.y + 0.1f, gamesettings.transform.localPosition.z);

        var rolesettings = __instance.RoleSettingsButton;
        __instance.RoleSettingsButton.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        __instance.RoleSettingsButton.transform.localPosition = new Vector3(rolesettings.transform.localPosition.x, gamesettings.transform.localPosition.y - 0.4f, rolesettings.transform.localPosition.z);
        //rolesettings.OnClick.RemoveAllListeners();
        // button.OnClick.AddListener( () => {}); // add rolemenu method

        //button 1
        GameObject template = gamepreset.gameObject;
        GameObject targetBox = Object.Instantiate(template, gamepreset.transform);
        targetBox.name = "System Settings";
        targetBox.transform.localScale = new Vector3(0.59f, 0.59f, 1f);
        targetBox.transform.localPosition = new Vector3(targetBox.transform.localPosition.x + 2.95f, rolesettings.transform.localPosition.y - 0.1f, targetBox.transform.localPosition.z);

        _ = new LateTask(() =>
        {
            targetBox.transform.parent = null;
            // gamepreset.transform.localScale = new Vector3(0f, 0f, 0f);
            gamepreset.gameObject.SetActive(false);
            targetBox.transform.parent = __instance.transform.Find("LeftPanel");
        }, 0.05f, "Remove GamePreset // Set Button 1"); // remove GamePresets

        var SystemButton = targetBox.GetComponent<PassiveButton>();
        SystemButton.OnClick.RemoveAllListeners();
        SystemButton.OnClick.AddListener((Action)(() =>
            Logger.Info("Activated System Settings", "System Settings TEST")
        )); 
        var label = SystemButton.transform.Find("FontPlacer/Text_TMP").GetComponent<TextMeshPro>();
        _ = new LateTask(() => { label.text = GetString("TabGroup.SystemSettings"); }, 0.05f, "Set Button1 Text"); 


        //button 2
        GameObject template2 = targetBox.gameObject;
        GameObject targetBox2 = Object.Instantiate(template2, targetBox.transform);
        targetBox2.name = "Mod Settings";
        targetBox2.transform.localScale = new Vector3(1f, 1f, 1f);
        targetBox2.transform.localPosition = new Vector3(targetBox2.transform.localPosition.x, targetBox.transform.localPosition.y, targetBox2.transform.localPosition.z);

        var ModConfButton = targetBox2.GetComponent<PassiveButton>();
        ModConfButton.OnClick.RemoveAllListeners();
        ModConfButton.OnClick.AddListener((Action)(() =>
            Logger.Info("Activated Mod Settings", "Mop Settings TEST")
        )); 
        var label2 = ModConfButton.transform.Find("FontPlacer/Text_TMP").GetComponent<TextMeshPro>(); 
        _ = new LateTask(() => { label2.text = GetString("TabGroup.ModSettings"); }, 0.05f, "Set Button2 Text"); 


        //button 3
        GameObject template3 = targetBox2.gameObject;
        GameObject targetBox3 = Object.Instantiate(template3, targetBox2.transform);
        targetBox3.name = "Game Modifiers";
        targetBox3.transform.localScale = new Vector3(1f, 1f, 1f);
        targetBox3.transform.localPosition = new Vector3(targetBox3.transform.localPosition.x, targetBox2.transform.localPosition.y, targetBox3.transform.localPosition.z);

        var GameModifButton = targetBox3.GetComponent<PassiveButton>();
        GameModifButton.OnClick.RemoveAllListeners();
        GameModifButton.OnClick.AddListener((Action)(() => 
            Logger.Info("Activated game Modifier", "Game Modifiers TEST")
        )); 
        var label3 = GameModifButton.transform.Find("FontPlacer/Text_TMP").GetComponent<TextMeshPro>(); 
        _ = new LateTask(() => { label3.text = GetString("TabGroup.ModifierSettings"); }, 0.05f, "Set Button3 Text"); 
    }
}

[HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.ChangeTab))]
public class TabChange
{
    public static void Prefix(ref int tabNum, [HarmonyArgument(1)] bool previewOnly)
    {
        if (tabNum == 0)
        { // Disables preset menu in any instances
            tabNum = 1;
        }
    }
    public static void Postfix(GameSettingMenu __instance, [HarmonyArgument(0)] int tabNum)
    {

        if (tabNum == 1 && __instance.GameSettingsTab.isActiveAndEnabled)
        {
            _ = new LateTask(() => __instance.MenuDescriptionText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameSettingsDescription), 0.05f, "Fix Menu Description Text");
            return;
        }

    }



}
[HarmonyPatch(typeof(RolesSettingsMenu), nameof(RolesSettingsMenu.Start))]
public static class RolesSettingsMenuAwakePatch
{
    public static void Postfix(RolesSettingsMenu __instance)
    {
        //Transform mainAreaTransform = __instance.transform.Find("MainArea");
        //RolesSettingsMenu roleTabMenu = mainAreaTransform.Find("ROLES TAB").GetComponent<RolesSettingsMenu>();
        //Logger.Info($"{roleTabMenu == null}", "Check");
        //if (roleTabMenu == null) return;

        //__instance.
        //roleTabMenu.

        //var toheRoleSettings = Object.Instantiate(roleTabMenu, roleTabMenu.transform.parent);

        //toheRoleSettings.name = "TEST ADSDSF";
        //toheRoleSettings.enabled = true;
        //toheRoleSettings.
    }
}
[HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Awake))]
//[HarmonyPriority(799)]
public static class GameOptionsMenuStartPatch
{
    public static void Postfix(GameOptionsMenu __instance)
    {
        try
        {
            var modeForSmallScreen = Main.ModeForSmallScreen.Value;

            StringOption template = Object.FindObjectOfType<StringOption>();
            GameObject gameSettings = GameObject.Find("Game Settings");
            GameSettingMenu gameSettingMenu = Object.FindObjectOfType<GameSettingMenu>();

            if (GameStates.IsNormalGame)
            {
                if (Options.CurrentGameMode == CustomGameMode.HidenSeekTOHE)
                {
                    // Select standart custom game mode for normal game
                    Options.GameMode.SetValue(0);
                }

                template = Object.FindObjectOfType<StringOption>();
                if (template == null) return;

                gameSettings = GameObject.Find("Game Settings");
                if (gameSettings == null) return;

                gameSettingMenu = Object.FindObjectOfType<GameSettingMenu>();
                if (gameSettingMenu == null) return;

                GameObject.Find("Tint")?.SetActive(false);

                _ = new LateTask(() =>
                {
                    var children = __instance.Children.ToArray();
                    foreach (var ob in children)
                    {
                        switch (ob.Title)
                        {
                            case StringNames.GameVotingTime:
                                ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 600);
                                break;
                            case StringNames.GameShortTasks:
                            case StringNames.GameLongTasks:
                            case StringNames.GameCommonTasks:
                                ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 99);
                                break;
                            case StringNames.GameKillCooldown:
                                ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                                break;
                            default:
                                break;
                        }
                    }
                }, 2f, "StringNames options", shoudLog: false);
            }
            else if (GameStates.IsHideNSeek)
            {
                // Select custom game mode for Hide & Seek
                Options.GameMode.SetValue(2);

                gameSettingMenu = Object.FindObjectOfType<GameSettingMenu>();
                if (gameSettingMenu == null) return;

                gameSettingMenu.GameSettingsTab.gameObject.SetActive(true);
                gameSettingMenu.RoleSettingsTab.gameObject.SetActive(true);

                GameObject.Find("Tint")?.SetActive(false);

                template = Object.FindObjectsOfType<StringOption>().FirstOrDefault();
                if (template == null) return;

                gameSettings = GameObject.Find("Game Settings");
                if (gameSettings == null) return;

                gameSettingMenu.GameSettingsTab.gameObject.SetActive(false);
                gameSettingMenu.RoleSettingsTab.gameObject.SetActive(false);

                var children = __instance.Children.ToArray();
                foreach (var ob in children)
                {
                    switch (ob.Title)
                    {
                        case StringNames.EscapeTime:
                            ob.Cast<NumberOption>().ValidRange = new FloatRange(10, 600);
                            break;
                        case StringNames.FinalEscapeTime:
                            ob.Cast<NumberOption>().ValidRange = new FloatRange(10, 300);
                            break;
                        case StringNames.GameShortTasks:
                        case StringNames.GameLongTasks:
                        case StringNames.GameCommonTasks:
                            ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 10);
                            break;
                        case StringNames.GameNumImpostors:
                            ob.Cast<NumberOption>().ValidRange = new FloatRange(1, 3);
                            break;
                        default:
                            break;
                    }
                }
            }

            gameSettings.transform.Find("GameGroup").GetComponent<Scroller>().ScrollWheelSpeed = 1.2f;

            var roleTab = GameObject.Find("RoleTab");
            var gameTab = GameObject.Find("GameTab");

            List<GameObject> menus = [gameSettingMenu.GameSettingsTab.gameObject, gameSettingMenu.RoleSettingsTab.gameObject];
            //List<SpriteRenderer> highlights = [gameSettingMenu.GameSettingsHightlight, gameSettingMenu];
            List<GameObject> tabs = [gameTab, roleTab];

            // No add roleTab in Hide & Seek
            if (GameStates.IsHideNSeek)
            {
                menus = [gameSettingMenu.GameSettingsTab.gameObject];
                //highlights = [gameSettingMenu.GameSettingsHightlight];
                tabs = [gameTab];
            }

            float delay = 0f;

            foreach (var tab in EnumHelper.GetAllValues<TabGroup>().Where(tab => GameStates.IsNormalGame || (GameStates.IsHideNSeek && (tab is TabGroup.SystemSettings or TabGroup.GameSettings or TabGroup.TaskSettings))).ToArray())
            {
                var obj = gameSettings.transform.parent.Find(tab + "Tab");
                if (obj != null)
                {
                    obj.transform.Find("../../GameGroup/Text").GetComponent<TextMeshPro>().SetText(GetString("TabGroup." + tab));
                    continue;
                }

                var tohSettings = Object.Instantiate(gameSettings, gameSettings.transform.parent);
                tohSettings.name = tab + "Tab";

                var tohSettingsTransform = tohSettings.transform;
                var backPanel = tohSettingsTransform.Find("BackPanel");

                if (!modeForSmallScreen)
                {
                    backPanel.transform.localScale =
                    tohSettingsTransform.Find("Bottom Gradient").transform.localScale = new Vector3(1.6f, 1f, 1f);
                    tohSettingsTransform.Find("Bottom Gradient").transform.localPosition += new Vector3(0.2f, 0f, 0f);
                    tohSettingsTransform.Find("BackPanel").transform.localPosition += new Vector3(0.2f, 0f, 0f);
                    tohSettingsTransform.Find("Background").transform.localScale = new Vector3(1.8f, 1f, 1f);
                    tohSettingsTransform.Find("UI_Scrollbar").transform.localPosition += new Vector3(1.4f, 0f, 0f);
                    tohSettingsTransform.Find("UI_ScrollbarTrack").transform.localPosition += new Vector3(1.4f, 0f, 0f);
                    tohSettingsTransform.Find("GameGroup/SliderInner").transform.localPosition += new Vector3(-0.3f, 0f, 0f);
                }
                else
                {
                    backPanel.transform.localScale =
                    tohSettingsTransform.Find("Bottom Gradient").transform.localScale = new Vector3(1.2f, 1f, 1f);
                    tohSettingsTransform.Find("Background").transform.localScale = new Vector3(1.3f, 1f, 1f);
                    tohSettingsTransform.Find("UI_Scrollbar").transform.localPosition += new Vector3(0.35f, 0f, 0f);
                    tohSettingsTransform.Find("UI_ScrollbarTrack").transform.localPosition += new Vector3(0.35f, 0f, 0f);
                    tohSettingsTransform.Find("GameGroup/SliderInner").transform.localPosition += new Vector3(-0.15f, 0f, 0f);
                }

                var tohMenu = tohSettingsTransform.Find("GameGroup/SliderInner").GetComponent<GameOptionsMenu>();
                List<OptionBehaviour> scOptions = [];

                _ = new LateTask(() =>
                {
                    tohMenu.GetComponentsInChildren<OptionBehaviour>().Do(x => Object.Destroy(x.gameObject));

                    foreach (var option in OptionItem.AllOptions.Where(opt => opt.Tab == tab).ToArray())
                    {

                        if (option.OptionBehaviour == null)
                        {
                            float yoffset = option.IsText ? 300f : 0f;
                            float xoffset = option.IsText ? 300f : 0.3f;
                            var stringOption = Object.Instantiate(template, tohMenu.transform);
                            scOptions.Add(stringOption);
                            stringOption.OnValueChanged = new Action<OptionBehaviour>((o) => { });
                            stringOption.TitleText.text = option.Name;
                            stringOption.Value = stringOption.oldValue = option.CurrentValue;
                            stringOption.ValueText.text = option.GetString();
                            stringOption.name = option.Name;

                            var stringOptionTransform = stringOption.transform;
                            if (!modeForSmallScreen)
                            {
                                stringOptionTransform.Find("Background").localScale = new Vector3(1.6f, 1f, 1f);
                                stringOptionTransform.Find("Plus_TMP").localPosition += new Vector3(option.IsText ? 300f : 1.4f, yoffset, 0f);
                                stringOptionTransform.Find("Minus_TMP").localPosition += new Vector3(option.IsText ? 300f : 1.0f, yoffset, 0f);
                                stringOptionTransform.Find("Value_TMP").localPosition += new Vector3(option.IsText ? 300f : 1.2f, yoffset, 0f);
                                stringOptionTransform.Find("Value_TMP").GetComponent<RectTransform>().sizeDelta = new Vector2(1.6f, 0.26f);
                                stringOptionTransform.Find("Title_TMP").localPosition += new Vector3(option.IsText ? 0.25f : 0.1f, option.IsText ? -0.1f : 0f, 0f);
                                stringOptionTransform.Find("Title_TMP").GetComponent<RectTransform>().sizeDelta = new Vector2(5.5f, 0.37f);
                            }
                            else
                            {
                                stringOptionTransform.Find("Background").localScale = new Vector3(1.2f, 1f, 1f);
                                stringOptionTransform.Find("Plus_TMP").localPosition += new Vector3(xoffset, yoffset, 0f);
                                stringOptionTransform.Find("Minus_TMP").localPosition += new Vector3(xoffset, yoffset, 0f);
                                stringOptionTransform.Find("Value_TMP").localPosition += new Vector3(xoffset, yoffset, 0f);
                                stringOptionTransform.Find("Title_TMP").localPosition += new Vector3(option.IsText ? 0.3f : 0.15f, option.IsText ? -0.1f : 0f, 0f);
                                stringOptionTransform.Find("Title_TMP").GetComponent<RectTransform>().sizeDelta = new Vector2(3.5f, 0.37f);
                            }

                            option.OptionBehaviour = stringOption;
                        }
                        option.OptionBehaviour.gameObject.SetActive(true);
                    }
                }, delay, "Settings", shoudLog: false);

                delay += 0.1f;
                
                // FIX THIS SH
                //tohMenu.Children = scOptions.ToArray();
                tohSettings.gameObject.SetActive(false);
                menus.Add(tohSettings.gameObject);

                var tohTab = Object.Instantiate(roleTab, roleTab.transform.parent);
                var hatButton = tohTab.transform.Find("Hat Button");

                hatButton.Find("Icon").GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite($"TOHE.Resources.Images.TabIcon_{tab}.png", 100f);
                tabs.Add(tohTab);
                var tohTabHighlight = hatButton.Find("Tab Background").GetComponent<SpriteRenderer>();
                //highlights.Add(tohTabHighlight);
            }

            // hide roleTab in Hide & Seek
            if (GameStates.IsHideNSeek)
            {
                roleTab.active = false;
            }

            var tabsCount = tabs.Count;
            var menusCount = menus.Count;
            var tabsCountDividedBy323 = tabsCount / 3.23f;

            for (var i = 0; i < tabsCount; i++)
            {
                var tab = tabs[i];
                var transform = tab.transform;

                var xValue = modeForSmallScreen ? 0.6f * (i - 1) - tabsCountDividedBy323 : 0.65f * (i - 1) - tabsCountDividedBy323;
                transform.localPosition = new(xValue, transform.localPosition.y, transform.localPosition.z);

                var button = tab.GetComponentInChildren<PassiveButton>();
                if (button != null)
                {
                    var copiedIndex = i;
                    button.OnClick ??= new UnityEngine.UI.Button.ButtonClickedEvent();
                    void value()
                    {
                        if (GameStates.IsHideNSeek)
                        {
                            //gameSettingMenu.GameSettingsTab.SetActive(false);
                            gameSettingMenu.RoleSettingsTab.gameObject.SetActive(false);
                            //gameSettingMenu.GameSettingsTab.HideNSeekSettings.gameObject.SetActive(false);
                            //gameSettingMenu.GameSettingsTab.GameSettingsHightlight.enabled = false;
                            //gameSettingMenu.RoleSettingsTabHightlight.enabled = false;

                            if (copiedIndex == 0)
                            {
                                //gameSettingMenu.HideNSeekSettings.gameObject.SetActive(true);
                                //gameSettingMenu.GameSettingsHightlight.enabled = true;
                            }
                        }
                        for (var j = 0; j < menusCount; j++)
                        {
                            if (GameStates.IsHideNSeek && j == 0 && copiedIndex == 0) continue;
                            menus[j].SetActive(j == copiedIndex);
                            //highlights[j].enabled = j == copiedIndex;
                        }
                    }
                    button.OnClick.AddListener((Action)value);
                }
            }
        }
        catch
        {
        }
    }
}

//[HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Update))]
public class GameOptionsMenuUpdatePatch
{
    private static float _timer = 1f;

    public static void Postfix(GameOptionsMenu __instance)
    {
        if (__instance.transform.parent.parent.name == "Game Settings") return;

        if (GameStates.IsHideNSeek)
        {
            Main.HideNSeekOptions.NumImpostors = Options.NumImpostorsHnS.GetInt();
        }

        foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
        {
            string tabcolor = tab switch
            {
                TabGroup.SystemSettings => Main.ModColor,
                TabGroup.GameSettings => "#59ef83",
                TabGroup.TaskSettings => "#EF59AF",
                TabGroup.ImpostorRoles => "#f74631",
                TabGroup.CrewmateRoles => "#8cffff",
                TabGroup.NeutralRoles => "#7f8c8d",
                TabGroup.Addons => "#ff9ace",
                _ => "#ffffff",
            };
            if (__instance.transform.parent.parent.name != tab + "Tab") continue;
            __instance.transform.Find("../../GameGroup/Text").GetComponent<TextMeshPro>().SetText($"<color={tabcolor}>" + GetString("TabGroup." + tab) + "</color>");

            _timer += Time.deltaTime;
            if (_timer < 0.1f) return;
            _timer = 0f;

            float numItems = __instance.Children.Count;
            var offset = 2.7f;

            foreach (var option in OptionItem.AllOptions.Where(opt => tab == opt.Tab && opt.OptionBehaviour != null && opt.OptionBehaviour.gameObject != null).ToArray())
            {
                var enabled = true;
                var parent = option.Parent;

                enabled = AmongUsClient.Instance.AmHost &&
                    !option.IsHiddenOn(Options.CurrentGameMode);

                var opt = option.OptionBehaviour.transform.Find("Background").GetComponent<SpriteRenderer>();
                opt.size = new(5.0f, 0.45f);
                while (parent != null && enabled)
                {
                    enabled = parent.GetBool() && !parent.IsHiddenOn(Options.CurrentGameMode);
                    parent = parent.Parent;
                    opt.color = new(0f, 1f, 0f);
                    opt.size = new(4.8f, 0.45f);

                    if (!Main.ModeForSmallScreen.Value)
                    {
                        opt.transform.localPosition = new Vector3(0.11f, 0f);
                        option.OptionBehaviour.transform.Find("Title_TMP").transform.localPosition = new Vector3(-1.08f, 0f);
                        option.OptionBehaviour.transform.Find("Title_TMP").GetComponent<RectTransform>().sizeDelta = new Vector2(5.1f, 0.28f);
                        if (option.Parent?.Parent != null)
                        {
                            opt.color = new(0f, 0f, 1f);
                            opt.size = new(4.6f, 0.45f);
                            opt.transform.localPosition = new Vector3(0.24f, 0f);
                            option.OptionBehaviour.transform.Find("Title_TMP").transform.localPosition = new Vector3(-0.88f, 0f);
                            option.OptionBehaviour.transform.Find("Title_TMP").GetComponent<RectTransform>().sizeDelta = new Vector2(4.9f, 0.28f);
                            if (option.Parent?.Parent?.Parent != null)
                            {
                                opt.color = new(1f, 0f, 0f);
                                opt.size = new(4.4f, 0.45f);
                                opt.transform.localPosition = new Vector3(0.37f, 0f);
                                option.OptionBehaviour.transform.Find("Title_TMP").transform.localPosition = new Vector3(-0.68f, 0f);
                                option.OptionBehaviour.transform.Find("Title_TMP").GetComponent<RectTransform>().sizeDelta = new Vector2(4.7f, 0.28f);
                            }
                        }
                    }
                    else
                    {
                        option.OptionBehaviour.transform.Find("Title_TMP").transform.localPosition = new Vector3(-0.95f, 0f);
                        option.OptionBehaviour.transform.Find("Title_TMP").GetComponent<RectTransform>().sizeDelta = new Vector2(3.4f, 0.37f);
                        if (option.Parent?.Parent != null)
                        {
                            opt.color = new(0f, 0f, 1f);
                            opt.size = new(4.6f, 0.45f);
                            opt.transform.localPosition = new Vector3(0.24f, 0f);
                            option.OptionBehaviour.transform.Find("Title_TMP").transform.localPosition = new Vector3(-0.7f, 0f);
                            option.OptionBehaviour.transform.Find("Title_TMP").GetComponent<RectTransform>().sizeDelta = new Vector2(3.3f, 0.37f);
                            if (option.Parent?.Parent?.Parent != null)
                            {
                                opt.color = new(1f, 0f, 0f);
                                opt.size = new(4.4f, 0.45f);
                                opt.transform.localPosition = new Vector3(0.37f, 0f);
                                option.OptionBehaviour.transform.Find("Title_TMP").transform.localPosition = new Vector3(-0.55f, 0f);
                                option.OptionBehaviour.transform.Find("Title_TMP").GetComponent<RectTransform>().sizeDelta = new Vector2(3.2f, 0.37f);
                            }
                        }
                    }
                }

                if (option.IsText)
                {
                    opt.color = new(0, 0, 0);
                    opt.transform.localPosition = new(300f, 300f, 300f);
                }

                option.OptionBehaviour.gameObject.SetActive(enabled);
                if (enabled)
                {
                    offset -= option.IsHeader ? 0.7f : 0.5f;
                    option.OptionBehaviour.transform.localPosition = new Vector3(
                        option.OptionBehaviour.transform.localPosition.x,
                        offset,
                        option.OptionBehaviour.transform.localPosition.z);

                    if (option.IsHeader)
                    {
                        if (!Main.ModeForSmallScreen.Value)
                            numItems += 0.3f;
                        else
                            numItems += 0.5f;
                    }
                }
                else
                {
                    numItems--;
                }
            }
            __instance.GetComponentInParent<Scroller>().ContentYBounds.max = (-offset) - 1.5f;
        }
    }
}

[HarmonyPatch(typeof(StringOption), nameof(StringOption.SetUpFromData))]
public class StringOptionEnablePatch
{
    public static void Postfix(StringOption __instance)
    {
        var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
        if (option == null) return;

        __instance.OnValueChanged = new Action<OptionBehaviour>((o) => { });
        if (option.IsVanillaText)
        {
            __instance.TitleText.text = option.GetNameVanilla();
        }
        else
        {
            __instance.TitleText.text = option.GetName();
        }
        __instance.Value = __instance.oldValue = option.CurrentValue;
        __instance.ValueText.text = option.GetString();
    }
}

[HarmonyPatch(typeof(StringOption), nameof(StringOption.Increase))]
public class StringOptionIncreasePatch
{
    public static bool Prefix(StringOption __instance)
    {
        var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
        if (option == null) return true;

        if (option.Name == "GameMode")
        {
            var gameModeCount = Options.gameModes.Length - 1;
            switch (GameOptionsManager.Instance.CurrentGameOptions.GameMode)
            {
                // To prevent the Host from selecting CustomGameMode.HidenSeekTOHE
                case GameModes.NormalFools when option.CurrentValue == 0:
                case GameModes.Normal when option.CurrentValue == gameModeCount - 1:
                // To prevent the Host from selecting CustomGameMode.Standard/FFA
                case GameModes.SeekFools when option.CurrentValue == gameModeCount:
                case GameModes.HideNSeek when option.CurrentValue == gameModeCount:
                    return false;
                default:
                    break;
            }
        }

        option.SetValue(option.CurrentValue + (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 5 : 1));

        if (option.Name == "Preset")
        {
            if (GameStates.IsHideNSeek)
            {
                // Set Hide & Seek game mode
                Options.GameMode.SetValue(2);
            }
            else if (Options.CurrentGameMode == CustomGameMode.HidenSeekTOHE)
            {
                // Set standart game mode
                Options.GameMode.SetValue(0);
            }
        }
        return false;
    }
}

[HarmonyPatch(typeof(StringOption), nameof(StringOption.Decrease))]
public class StringOptionDecreasePatch
{
    public static bool Prefix(StringOption __instance)
    {
        var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
        if (option == null) return true;

        if (option.Name == "GameMode")
        {
            switch (GameOptionsManager.Instance.CurrentGameOptions.GameMode)
            {
                // To prevent the Host from selecting CustomGameMode.HidenSeekTOHE
                case GameModes.NormalFools when option.CurrentValue == 0:
                case GameModes.Normal when option.CurrentValue == 0:
                // To prevent the Host from selecting CustomGameMode.Standard/FFA
                case GameModes.SeekFools when option.CurrentValue == Options.gameModes.Length - 1:
                case GameModes.HideNSeek when option.CurrentValue == Options.gameModes.Length - 1:
                    return false;
                default:
                    break;
            }
        }

        option.SetValue(option.CurrentValue - (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 5 : 1));

        if (option.Name == "Preset")
        {
            if (GameStates.IsHideNSeek)
            {
                // Set Hide & Seek game mode
                Options.GameMode.SetValue(2);
            }
            else if (Options.CurrentGameMode == CustomGameMode.HidenSeekTOHE)
            {
                // Set standart game mode
                Options.GameMode.SetValue(0);
            }
        }
        return false;
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
[HarmonyPatch(typeof(RolesSettingsMenu), nameof(RolesSettingsMenu.ChangeTab))]
public static class RolesSettingsMenu_ChangeTabPatch
{
    public static void Postfix(RolesSettingsMenu __instance)
    {
        if (GameStates.IsHideNSeek) return;

        foreach (var ob in __instance.advancedSettingChildren.ToArray())
        {
            switch (ob.Title)
            {
                case StringNames.EngineerCooldown:
                    ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                    break;
                case StringNames.ShapeshifterCooldown:
                    ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                    break;
                default:
                    break;
            }
        }
    }
}
[HarmonyPatch(typeof(RolesSettingsMenu), nameof(RolesSettingsMenu.SetQuotaTab))]
public static class RolesSettingsMenu_ChanceTabPatch
{
    public static bool Prefix(RolesSettingsMenu __instance)
    {
        if (GameStates.IsHideNSeek) return true;

        float num = 0.662f;
        float num2 = -1.928f;
        __instance.roleTabs = new List<PassiveButton>().ToIl2Cpp();

        List<RoleRulesCategory> list = GameManager.Instance.GameSettingsList.AllRoles.ToManaged().FindAll((RoleRulesCategory cat) => cat.Role.TeamType == RoleTeamTypes.Crewmate);
        List<RoleRulesCategory> list2 = GameManager.Instance.GameSettingsList.AllRoles.ToManaged().FindAll((RoleRulesCategory cat) => cat.Role.TeamType == RoleTeamTypes.Impostor);

        // Impostor Tab
        AddRoleTabCustom(__instance, Custom_RoleType.ImpostorVanilla, ref num2);

        // Neutral Tab
        AddRoleTabCustom(__instance, Custom_RoleType.NeutralBenign, ref num2);


        CategoryHeaderEditRole categoryHeaderEditRole = Object.Instantiate<CategoryHeaderEditRole>(__instance.categoryHeaderEditRoleOrigin, Vector3.zero, Quaternion.identity, __instance.RoleChancesSettings.transform);
        categoryHeaderEditRole.SetHeader(StringNames.CrewmateRolesHeader, 20);
        categoryHeaderEditRole.transform.localPosition = new Vector3(4.986f, num, -2f);
        num -= 0.522f;
        int num3 = 0;
        for (int k = 0; k < list.Count; k++)
        {
            __instance.CreateQuotaOption(list[k], ref num, num3);
            num3++;
        }
        num -= 0.22f;

        CategoryHeaderEditRole categoryHeaderEditRole2 = Object.Instantiate<CategoryHeaderEditRole>(__instance.categoryHeaderEditRoleOrigin, Vector3.zero, Quaternion.identity, __instance.RoleChancesSettings.transform);
        categoryHeaderEditRole2.SetHeader(StringNames.ImpostorRolesHeader, 20);
        categoryHeaderEditRole2.transform.localPosition = new Vector3(4.986f, num, -2f);

        num -= 0.522f;
        for (int l = 0; l < list2.Count; l++)
        {
            __instance.CreateQuotaOption(list2[l], ref num, num3);
            num3++;
        }
        return false;
    }

    private static RoleSettingsTabButton AddRoleTabCustom(RolesSettingsMenu thiz, Custom_RoleType roleType, ref float tabXPos)
    {
        RoleSettingsTabButton tab = null;
        VanillaLikeRoleTypes realRoleType = VanillaLikeRoleTypes.Crewmate;
        switch (roleType)
        {
            case Custom_RoleType.CrewmateVanilla:
                tab = Object.Instantiate(thiz.roleSettingsTabButtonOrigin, Vector3.zero, Quaternion.identity, thiz.tabParent);
                realRoleType = VanillaLikeRoleTypes.Crewmate;
                break;
            case Custom_RoleType.ImpostorVanilla:
                tab = Object.Instantiate(thiz.roleSettingsTabButtonOriginImpostor, Vector3.zero, Quaternion.identity, thiz.tabParent);
                realRoleType = VanillaLikeRoleTypes.Impostor;
                break;
            case Custom_RoleType.NeutralBenign:
                tab = Object.Instantiate(thiz.roleSettingsTabButtonOrigin, Vector3.zero, Quaternion.identity, thiz.tabParent);
                RoleBehaviour impRole = RoleManager.Instance.AllRoles.Where(r => r.Role == RoleTypes.Shapeshifter).FirstOrDefault();
                tab.icon.sprite = impRole.RoleIconWhite;
                SetTabColor(tab, "#7f8c8d");
                realRoleType = VanillaLikeRoleTypes.Neutral;
                break;
            default:
                throw new InvalidOperationException("To prevent issues, you should only create this with: Custom_RoleType.NeutralBenign, Custom_RoleType.ImpostorVanilla, Custom_RoleType.CrewmateVanilla");
        }
        tab.transform.localPosition = new Vector3(tabXPos, 2.27f, -2f);

        tab.button.OnClick.AddListener(new Action(() =>
        {
            LoadRoleOptions(realRoleType, tab.Button);
        }));
        tabXPos += 0.762f;
        thiz.roleTabs.Add(tab.Button);
        return tab;
    }

    private static void LoadRoleOptions(VanillaLikeRoleTypes type, PassiveButton btn)
    {
        Logger.Info(type.ToString(), "LoadRoleOptions");
    }

    private static void SetTabColor(RoleSettingsTabButton tab, string hex)
    {
        if (tab == null) return;

        Color color = Color.blue;
        hex = hex.TrimStart('#');

        if (hex.Length != 6)
        {
            throw new InvalidOperationException("Hex color should be 6 characters long excluding the '#' symbol.");
        }

        float r = Convert.ToInt32(hex.Substring(0, 2), 16) / 255f;
        float g = Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
        float b = Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;

        color = new Color(r, g, b);
        if (tab.button.inactiveSprites.GetComponent<SpriteRenderer>() != null && tab.button.activeSprites.GetComponent<SpriteRenderer>() != null)
        {
            SpriteRenderer inactiveSprite = tab.button.inactiveSprites.GetComponent<SpriteRenderer>();
            SpriteRenderer activeSprite = tab.button.inactiveSprites.GetComponent<SpriteRenderer>();
            inactiveSprite.color = GetInactiveColor(color);
            activeSprite.color = color;
        }
    }

    private static Color GetInactiveColor(this Color color, float shadowFactor = 0.5f)
    {
        shadowFactor = Mathf.Max(1.0f, shadowFactor);

        Color shadowColor = new(
            color.r / shadowFactor,
            color.g / shadowFactor,
            color.b / shadowFactor,
            color.a
        );

        return shadowColor;
    }

    enum VanillaLikeRoleTypes : int
    {
        Crewmate = 0,
        Impostor = 1,
        Neutral = 2,
    }
}