using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TOHE.Translator;
using Object = UnityEngine.Object;

namespace TOHE;

[HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
[HarmonyPriority(Priority.First)]
class GameSettingMenuStartPatch
{
    public static void Postfix(GameSettingMenu __instance)
    {
        // Need for Hide&Seek because tabs are disabled by default
        __instance.Tabs.SetActive(true);
    }
}
[HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Close))]
class GameSettingMenuClosePatch
{
    public static void Postfix()
    {
        // if custom game mode is HideNSeekTOHE in normal game, set standart
        if (GameStates.IsNormalGame && Options.CurrentGameMode == CustomGameMode.HidenSeekTOHE)
        {
            // Select standart custom game mode
            Options.GameMode.SetValue(0);
        }
    }
}
[HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.InitializeOptions))]
public static class GameSettingMenuInitializeOptionsPatch
{
    public static void Prefix(GameSettingMenu __instance)
    {
        // Unlocks map/impostor amount changing in online (for testing on your custom servers)
        // Changed to be able to change the map in online mode without having to re-establish the room.
        __instance.HideForOnline = new Il2CppReferenceArray<Transform>(0);
    }

    // Add Dleks to map selection
    public static void Postfix([HarmonyArgument(0)] Il2CppReferenceArray<Transform> items)
    {
        items
            .FirstOrDefault(
                i => i.gameObject.activeSelf && i.name.Equals("MapName", StringComparison.OrdinalIgnoreCase))?
            .GetComponent<KeyValueOption>()?
            .Values?
            // using .Insert will convert managed values and break the struct resulting in crashes
            .System_Collections_IList_Insert((int)MapNames.Dleks, new Il2CppSystem.Collections.Generic.KeyValuePair<string, int>(Constants.MapNames[(int)MapNames.Dleks], (int)MapNames.Dleks));
    }
}
[HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Start))]
[HarmonyPriority(799)]
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
                if (gameSettings == null) return;

                GameObject.Find("Tint")?.SetActive(false);

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
            }
            else if (GameStates.IsHideNSeek)
            {
                // Select custom game mode for Hide & Seek
                Options.GameMode.SetValue(2);

                gameSettingMenu = Object.FindObjectOfType<GameSettingMenu>();
                if (gameSettingMenu == null) return;

                gameSettingMenu.RegularGameSettings.gameObject.SetActive(true);
                gameSettingMenu.RolesSettings.gameObject.SetActive(true);

                GameObject.Find("Tint")?.SetActive(false);

                template = Object.FindObjectsOfType<StringOption>().FirstOrDefault();
                if (template == null) return;

                gameSettings = GameObject.Find("Game Settings");
                if (gameSettings == null) return;

                gameSettingMenu.RegularGameSettings.gameObject.SetActive(false);
                gameSettingMenu.RolesSettings.gameObject.SetActive(false);

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

            List<GameObject> menus = new() { gameSettingMenu.RegularGameSettings, gameSettingMenu.RolesSettings.gameObject };
            List<SpriteRenderer> highlights = new() { gameSettingMenu.GameSettingsHightlight, gameSettingMenu.RolesSettingsHightlight };
            List<GameObject> tabs = new() { gameTab, roleTab };

            // No add roleTab in Hide & Seek
            if (GameStates.IsHideNSeek)
            {
                menus = new() { gameSettingMenu.RegularGameSettings };
                highlights = new() { gameSettingMenu.GameSettingsHightlight };
                tabs = new() { gameTab };
            }

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

                if (!modeForSmallScreen)
                {
                    tohSettingsTransform.Find("BackPanel").transform.localScale =
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
                    tohSettingsTransform.Find("BackPanel").transform.localScale =
                    tohSettingsTransform.Find("Bottom Gradient").transform.localScale = new Vector3(1.2f, 1f, 1f);
                    tohSettingsTransform.Find("Background").transform.localScale = new Vector3(1.3f, 1f, 1f);
                    tohSettingsTransform.Find("UI_Scrollbar").transform.localPosition += new Vector3(0.35f, 0f, 0f);
                    tohSettingsTransform.Find("UI_ScrollbarTrack").transform.localPosition += new Vector3(0.35f, 0f, 0f);
                    tohSettingsTransform.Find("GameGroup/SliderInner").transform.localPosition += new Vector3(-0.15f, 0f, 0f);
                }

                var tohMenu = tohSettingsTransform.Find("GameGroup/SliderInner").GetComponent<GameOptionsMenu>();
                foreach (var optionBehaviour in tohMenu.GetComponentsInChildren<OptionBehaviour>())
                {
                    // Discard OptionBehaviour
                    Object.Destroy(optionBehaviour.gameObject);
                }

                List<OptionBehaviour> scOptions = new();
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
                tohMenu.Children = scOptions.ToArray();
                tohSettings.gameObject.SetActive(false);
                menus.Add(tohSettings.gameObject);

                var tohTab = Object.Instantiate(roleTab, roleTab.transform.parent);
                tohTab.transform.Find("Hat Button").Find("Icon").GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite($"TOHE.Resources.Images.TabIcon_{tab}.png", 100f);
                tabs.Add(tohTab);
                var tohTabHighlight = tohTab.transform.Find("Hat Button").Find("Tab Background").GetComponent<SpriteRenderer>();
                highlights.Add(tohTabHighlight);
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
                            gameSettingMenu.RegularGameSettings.SetActive(false);
                            gameSettingMenu.RolesSettings.gameObject.SetActive(false);
                            gameSettingMenu.HideNSeekSettings.gameObject.SetActive(false);
                            gameSettingMenu.GameSettingsHightlight.enabled = false;
                            gameSettingMenu.RolesSettingsHightlight.enabled = false;

                            if (copiedIndex == 0)
                            {
                                gameSettingMenu.HideNSeekSettings.gameObject.SetActive(true);
                                gameSettingMenu.GameSettingsHightlight.enabled = true;
                            }
                        }
                        for (var j = 0; j < menusCount; j++)
                        {
                            if (GameStates.IsHideNSeek && j == 0 && copiedIndex == 0) continue;
                            menus[j].SetActive(j == copiedIndex);
                            highlights[j].enabled = j == copiedIndex;
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

[HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
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
                TabGroup.OtherRoles => "#76b8e0",
                _ => "#ffffff",
            };
            if (__instance.transform.parent.parent.name != tab + "Tab") continue;
            __instance.transform.Find("../../GameGroup/Text").GetComponent<TextMeshPro>().SetText($"<color={tabcolor}>" + GetString("TabGroup." + tab) + "</color>");

            _timer += Time.deltaTime;
            if (_timer < 0.1f) return;
            _timer = 0f;

            float numItems = __instance.Children.Length;
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

[HarmonyPatch(typeof(StringOption), nameof(StringOption.OnEnable))]
public class StringOptionEnablePatch
{
    public static bool Prefix(StringOption __instance)
    {
        var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
        if (option == null) return true;

        __instance.OnValueChanged = new Action<OptionBehaviour>((o) => { });
        __instance.TitleText.text = option.GetName();
        __instance.Value = __instance.oldValue = option.CurrentValue;
        __instance.ValueText.text = option.GetString();

        return false;
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
                case GameModes.Normal when option.CurrentValue == gameModeCount - 1:
                // To prevent the Host from selecting CustomGameMode.Standard/FFA
                case GameModes.HideNSeek when option.CurrentValue == gameModeCount:
                    return false;
                default:
                    break;
            }
        }

        option.SetValue(option.CurrentValue + (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 5 : 1));
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
                case GameModes.Normal when option.CurrentValue == 0:
                // To prevent the Host from selecting CustomGameMode.Standard/FFA
                case GameModes.HideNSeek when option.CurrentValue == Options.gameModes.Length - 1:
                    return false;
                default:
                    break;
            }
        }

        option.SetValue(option.CurrentValue - (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 5 : 1));
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
[HarmonyPatch(typeof(RolesSettingsMenu), nameof(RolesSettingsMenu.Start))]
public static class RolesSettingsMenuPatch
{
    public static void Postfix(RolesSettingsMenu __instance)
    {
        if (GameStates.IsHideNSeek) return;

        foreach (var ob in __instance.Children.ToArray())
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
[HarmonyPatch(typeof(NormalGameOptionsV07), nameof(NormalGameOptionsV07.SetRecommendations))]
public static class SetRecommendationsPatch
{
    public static bool Prefix(NormalGameOptionsV07 __instance, int numPlayers, bool isOnline)
    {
        numPlayers = Mathf.Clamp(numPlayers, 4, 15);
        __instance.PlayerSpeedMod = __instance.MapId == 4 ? 1.5f : 1.25f;
        __instance.CrewLightMod = 1.0f;
        __instance.ImpostorLightMod = 1.75f;
        __instance.KillCooldown = 27.5f;
        __instance.NumCommonTasks = 2;
        __instance.NumLongTasks = 1;
        __instance.NumShortTasks = 2;
        __instance.NumEmergencyMeetings = 3;
        if (!isOnline)
            __instance.NumImpostors = NormalGameOptionsV07.RecommendedImpostors[numPlayers];
        __instance.KillDistance = 0;
        __instance.DiscussionTime = 0;
        __instance.VotingTime = 120;
        __instance.IsDefaults = true;
        __instance.ConfirmImpostor = false;
        __instance.VisualTasks = false;

        __instance.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
        __instance.roleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
        __instance.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);
        __instance.roleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
        __instance.roleOptions.SetRoleRecommended(RoleTypes.Shapeshifter);
        __instance.roleOptions.SetRoleRecommended(RoleTypes.Scientist);
        __instance.roleOptions.SetRoleRecommended(RoleTypes.GuardianAngel);
        __instance.roleOptions.SetRoleRecommended(RoleTypes.Engineer);

        if (Options.CurrentGameMode == CustomGameMode.FFA) //FFA
        {
            __instance.CrewLightMod = __instance.ImpostorLightMod = 1.25f;
            __instance.NumImpostors = 3;
            __instance.NumCommonTasks = 0;
            __instance.NumLongTasks = 0;
            __instance.NumShortTasks = 0;
            __instance.KillCooldown = 0f;
        }

        return false;
    }
}