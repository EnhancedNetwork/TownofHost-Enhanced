using System;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TOHE;

// Thanks: https://github.com/Yumenopai/TownOfHost_Y/blob/main/Patches/GameSettingMenuPatch.cs
[HarmonyPatch(typeof(GameSettingMenu))]
public class GameSettingMenuPatch
{
    // ゲーム設定メニュータブ
    public enum GameSettingMenuTab
    {
        GamePresets = 0,
        GameSettings,
        RoleSettings,
        Mod_MainSettings,
        Mod_ImpostorRoles,
        Mod_MadmateRoles,
        Mod_CrewmateRoles,
        Mod_NeutralRoles,
        Mod_UnitRoles,
        Mod_AddOns,

        MaxCount,
    }

    // ボタンに表示する名前
    public static string[] buttonName = new string[]{
        "Game Settings",
        "TOH_Y Settings",
        "Impostor Roles",
        "Madmate Roles",
        "Crewmate Roles",
        "Neutral Roles",
        "Unit Roles",
        "Add-Ons"
    };

    // 左側配置ボタン座標
    private static Vector3 buttonPosition_Left = new(-3.9f, -0.4f, 0f);
    // 右側配置ボタン座標
    private static Vector3 buttonPosition_Right = new(-2.4f, -0.4f, 0f);
    // ボタンサイズ
    private static Vector3 buttonSize = new(0.45f, 0.6f, 1f);

    private static GameOptionsMenu templateGameOptionsMenu;
    private static PassiveButton templateGameSettingsButton;

    // MOD設定用ボタン格納変数
    static Dictionary<TabGroup, PassiveButton> ModSettingsButtons = new();
    // MOD設定メニュー用タブ格納変数
    static Dictionary<TabGroup, GameOptionsMenu> ModSettingsTabs = new();

    // ゲーム設定メニュー 初期関数
    [HarmonyPatch(nameof(GameSettingMenu.Start)), HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void StartPostfix(GameSettingMenu __instance)
    {
        /******** ボタン作成 ********/

        // 各グループ毎にボタンを作成する
        ModSettingsButtons = new();
        foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
        {
            // ゲーム設定ボタンを元にコピー
            var button = Object.Instantiate(templateGameSettingsButton, __instance.GameSettingsButton.transform.parent);
            button.gameObject.SetActive(true);
            // 名前は「button_ + ボタン名」
            button.name = "Button_" + buttonName[(int)tab + 1]; // buttonName[0]はバニラ設定用の名前なので+1
            // ボタンテキスト
            var label = button.GetComponentInChildren<TextMeshPro>();
            // ボタンテキストの翻訳破棄
            label.DestroyTranslator();
            // Temp color tab
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
            Vector3 offset = new (0.0f, 0.5f * (((int)tab + 1) / 2), 0.0f);
            // ボタンの座標設定
            button.transform.localPosition = ((((int)tab + 1) % 2 == 0) ? buttonPosition_Left : buttonPosition_Right) - offset;
            // ボタンのサイズ設定
            button.transform.localScale = buttonSize;

            // ボタンがクリックされた時の設定
            var buttonComponent = button.GetComponent<PassiveButton>();
            buttonComponent.OnClick = new();
            // ボタンがクリックされるとタブをそのものに変更する
            buttonComponent.OnClick.AddListener(
                (Action)(() => __instance.ChangeTab((int)tab + 3, false)));

            // ボタン登録
            ModSettingsButtons.Add(tab, button);
        }/******** ボタン作成 ここまで ********/

        /******** タブ作成 ********/
        //// ストリングオプションのテンプレート作成
        //var templateStringOption = GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainArea/GAME SETTINGS TAB/Scroller/SliderInner/GameOption_String(Clone)").GetComponent<StringOption>();
        //if (templateStringOption == null) return;

        ModGameOptionsMenu.OptionList = new();
        ModGameOptionsMenu.BehaviourList = new();
        ModGameOptionsMenu.CategoryHeaderList = new();

        // 各グループ毎にタブを作成する/基盤作成
        ModSettingsTabs = new();
        foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
        {
            // ゲーム設定タブからコピー
            var setTab = Object.Instantiate(templateGameOptionsMenu, __instance.GameSettingsTab.transform.parent);
            // 名前はゲーム設定タブEnumから取得
            setTab.name = ((GameSettingMenuTab)tab + 3).ToString();
            //// 中身を削除
            //setTab.GetComponentsInChildren<OptionBehaviour>().Do(x => Object.Destroy(x.gameObject));
            //setTab.GetComponentsInChildren<CategoryHeaderMasked>().Do(x => Object.Destroy(x.gameObject));
            setTab.gameObject.SetActive(false);

            // 設定タブを追加
            ModSettingsTabs.Add(tab, setTab);
        }

        foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
        {
            if (ModSettingsButtons.TryGetValue(tab, out var button))
            {
                __instance.ControllerSelectable.Add(button);
            }
        }

        //⇒GamOptionsMenuPatchで処理
        //// 各グループ毎にタブを作成する/中身追加
        //foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
        //{
        //    // オプションをまとめて格納する
        //    Il2CppSystem.Collections.Generic.List<OptionBehaviour> scOptions = new();

        //    // オプションを全てまわす
        //    foreach (var option in OptionItem.AllOptions)
        //    {
        //        // オプションを出すタブでないなら次
        //        if (option.Tab != tab) continue;

        //        // ビヘイビアがまだ設定されていないなら
        //        if (option.OptionBehaviour == null)
        //        {
        //            // ストリングオプションをコピー
        //            var stringOption = Object.Instantiate(templateStringOption, GameObject.Find($"{ModSettingsTabs[tab].name}/Scroller/SliderInner").transform);
        //            // オプションListに追加
        //            scOptions.Add(stringOption);
        //            stringOption.OnValueChanged = new System.Action<OptionBehaviour>((o) => { });
        //            stringOption.TitleText.text = option.Name;
        //            stringOption.Value = stringOption.oldValue = option.CurrentValue;
        //            stringOption.ValueText.text = option.GetString();
        //            stringOption.name = option.Name;
        //            stringOption.transform.FindChild("LabelBackground").localScale = new Vector3(1.6f, 1f, 1f);
        //            stringOption.transform.FindChild("LabelBackground").SetLocalX(-2.2695f);
        //            stringOption.transform.FindChild("PlusButton (1)").localPosition += new Vector3(option.IsFixValue ? 100f : 1.1434f, option.IsFixValue ? 100f : 0f, option.IsFixValue ? 100f : 0f);
        //            stringOption.transform.FindChild("MinusButton (1)").localPosition += new Vector3(option.IsFixValue ? 100f : 0.3463f, option.IsFixValue ? 100f : 0f, option.IsFixValue ? 100f : 0f);
        //            stringOption.transform.FindChild("Value_TMP (1)").localPosition += new Vector3(0.7322f, 0f, 0f);
        //            stringOption.transform.FindChild("ValueBox").localScale += new Vector3(0.2f, 0f, 0f);
        //            stringOption.transform.FindChild("ValueBox").localPosition += new Vector3(0.7322f, 0f, 0f);
        //            stringOption.transform.FindChild("Title Text").localPosition += new Vector3(-1.096f, 0f, 0f);
        //            stringOption.transform.FindChild("Title Text").GetComponent<RectTransform>().sizeDelta = new Vector2(6.5f, 0.37f);
        //            stringOption.transform.FindChild("Title Text").GetComponent<TMPro.TextMeshPro>().alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        //            stringOption.SetClickMask(ModSettingsTabs[tab].ButtonClickMask);

        //            // ビヘイビアに作成したストリングオプションを設定
        //            option.OptionBehaviour = stringOption;
        //        }
        //        // ビヘイビアのobjectを表示
        //        option.OptionBehaviour.gameObject.SetActive(true);
        //    }
        //    // タブの子にオプションリストを設定
        //    ModSettingsTabs[tab].Children = scOptions;
        //    // 選択されるときに表示するため、初期値はfalse
        //    ModSettingsTabs[tab].gameObject.SetActive(false);
        //    // 有効にする
        //    ModSettingsTabs[tab].enabled = true;
        //}
    }
    private static void SetDefaultButton(GameSettingMenu __instance)
    {
        /******** デフォルトボタン設定 ********/
        // プリセット設定 非表示
        __instance.GamePresetsButton.gameObject.SetActive(false);

        /**** ゲーム設定ボタンを変更 ****/
        var gameSettingButton = __instance.GameSettingsButton;
        // 座標指定
        gameSettingButton.transform.localPosition = new(-3f, -0.5f, 0f);
        // ボタンテキスト
        var textLabel = gameSettingButton.GetComponentInChildren<TextMeshPro>();
        // 翻訳破棄
        textLabel.DestroyTranslator();
        // バニラ設定ボタンの名前を設定
        textLabel.text = Translator.GetString("TabVanilla.GameSettings");
        // ボタンテキストの色変更
        //gameSettingButton.activeTextColor = gameSettingButton.inactiveTextColor = Color.black;
        // ボタンテキストの選択中の色変更
        //gameSettingButton.selectedTextColor = Color.blue;

        //var vanillaActiveButton = Utils.LoadSprite($"TownOfHost_Y.Resources.Tab_Active_VanillaGameSettings.png", 100f);
        //// 各種スプライトをオリジナルのものに変更
        //gameSettingButton.inactiveSprites.GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite($"TownOfHost_Y.Resources.Tab_Small_VanillaGameSettings.png", 100f);
        //gameSettingButton.activeSprites.GetComponent<SpriteRenderer>().sprite = vanillaActiveButton;
        //gameSettingButton.selectedSprites.GetComponent<SpriteRenderer>().sprite = vanillaActiveButton;
        // ボタンの座標設定
        gameSettingButton.transform.localPosition = buttonPosition_Left;
        // ボタンのサイズ設定
        gameSettingButton.transform.localScale = buttonSize;
        /**** ゲーム設定ボタンを変更 ここまで ****/

        // バニラ役職設定 非表示
        __instance.RoleSettingsButton.gameObject.SetActive(false);
        /******** デフォルトボタン設定 ここまで ********/

        __instance.DefaultButtonSelected = gameSettingButton;
        __instance.ControllerSelectable = new();
        __instance.ControllerSelectable.Add(gameSettingButton);
    }

    [HarmonyPatch(nameof(GameSettingMenu.ChangeTab)), HarmonyPrefix]
    public static bool ChangeTabPrefix(GameSettingMenu __instance, ref int tabNum, [HarmonyArgument(1)] bool previewOnly)
    {
        //// プリセットタブは表示させないため、ゲーム設定タブを設定する
        //if (tabNum == (int)GameSettingMenuTab.GamePresets) {
        //    tabNum = (int)GameSettingMenuTab.GameSettings;

        //    // What Is this?のテキスト文を変更
        //    // __instance.MenuDescriptionText.text = "test";
        //}

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

        if ((previewOnly && Controller.currentTouchType == Controller.TouchType.Joystick) || !previewOnly)
        {
            __instance.PresetsTab.gameObject.SetActive(false);
            __instance.GameSettingsTab.gameObject.SetActive(false);
            __instance.RoleSettingsTab.gameObject.SetActive(false);
            __instance.GamePresetsButton.SelectButton(false);
            __instance.GameSettingsButton.SelectButton(false);
            __instance.RoleSettingsButton.SelectButton(false);

            if (ModSettingsTabs.TryGetValue((TabGroup)(tabNum - 3), out settingsTab) &&
                settingsTab != null)
            {
                settingsTab.gameObject.SetActive(true);
                __instance.MenuDescriptionText.DestroyTranslator();
                //switch ((TabGroup)(tabNum - 3))
                //{
                //    case TabGroup.ModMainSettings:
                //        __instance.MenuDescriptionText.text = "MOD機能の設定ができる。";
                //        break;
                //    case TabGroup.ImpostorRoles:
                //        __instance.MenuDescriptionText.text = "MODインポスターロールの設定ができる。";
                //        break;
                //    case TabGroup.MadmateRoles:
                //        __instance.MenuDescriptionText.text = "MODマッドメイトロールの設定ができる。";
                //        break;
                //    case TabGroup.CrewmateRoles:
                //        __instance.MenuDescriptionText.text = "MODクルーメイトロールの設定ができる。";
                //        break;
                //    case TabGroup.NeutralRoles:
                //        __instance.MenuDescriptionText.text = "MODニュートラルロールの設定ができる。";
                //        break;
                //    case TabGroup.UnitRoles:
                //        __instance.MenuDescriptionText.text = "MODユニットロールの設定ができる。";
                //        break;
                //    case TabGroup.Addons:
                //        __instance.MenuDescriptionText.text = "MODロール属性の設定ができる。";
                //        break;
                //}
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
        //if (ModSettingsTabs.TryGetValue((TabGroup)(tabNum - 3), out settingsTab) &&
        //    settingsTab != null)
        //{
        //    settingsTab.OpenMenu();
        //}
        if (ModSettingsButtons.TryGetValue((TabGroup)(tabNum - 3), out button) &&
            button != null)
        {
            button.SelectButton(true);
        }

        return false;
    }

    [HarmonyPatch(nameof(GameSettingMenu.OnEnable)), HarmonyPrefix]
    private static bool OnEnablePrefix(GameSettingMenu __instance)
    {
        if (templateGameOptionsMenu == null)
        {
            templateGameOptionsMenu = Object.Instantiate(__instance.GameSettingsTab, __instance.GameSettingsTab.transform.parent);
            templateGameOptionsMenu.gameObject.SetActive(false);
        }
        if (templateGameSettingsButton == null)
        {
            templateGameSettingsButton = Object.Instantiate(__instance.GameSettingsButton, __instance.GameSettingsButton.transform.parent);
            templateGameSettingsButton.gameObject.SetActive(false);
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
            UnityEngine.Object.Destroy(button);
        foreach (var tab in ModSettingsTabs.Values)
            UnityEngine.Object.Destroy(tab);
        ModSettingsButtons = new();
        ModSettingsTabs = new();
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

    //[HarmonyPatch(typeof(NormalGameOptionsV08), nameof(NormalGameOptionsV08.SetRecommendations))]
    //public static class SetRecommendationsPatch
    //{
    //    public static bool Prefix(NormalGameOptionsV08 __instance, int numPlayers, bool isOnline)
    //    {
    //        numPlayers = Mathf.Clamp(numPlayers, 4, 15);
    //        __instance.PlayerSpeedMod = __instance.MapId == 4 ? 1.25f : 1f; //AirShipなら1.25、それ以外は1
    //        __instance.CrewLightMod = 0.5f;
    //        __instance.ImpostorLightMod = 1.75f;
    //        __instance.KillCooldown = 25f;
    //        __instance.NumCommonTasks = 2;
    //        __instance.NumLongTasks = 4;
    //        __instance.NumShortTasks = 6;
    //        __instance.NumEmergencyMeetings = 1;
    //        if (!isOnline)
    //            __instance.NumImpostors = NormalGameOptionsV08.RecommendedImpostors[numPlayers];
    //        __instance.KillDistance = 0;
    //        __instance.DiscussionTime = 0;
    //        __instance.VotingTime = 150;
    //        __instance.IsDefaults = true;
    //        __instance.ConfirmImpostor = false;
    //        __instance.VisualTasks = false;

    //        __instance.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
    //        __instance.roleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
    //        __instance.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);
    //        __instance.roleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
    //        __instance.roleOptions.SetRoleRecommended(RoleTypes.Shapeshifter);
    //        __instance.roleOptions.SetRoleRecommended(RoleTypes.Scientist);
    //        __instance.roleOptions.SetRoleRecommended(RoleTypes.GuardianAngel);
    //        __instance.roleOptions.SetRoleRecommended(RoleTypes.Engineer);

    //        if (Options.CurrentGameMode == CustomGameMode.HideAndSeek) //HideAndSeek
    //        {
    //            __instance.PlayerSpeedMod = 1.75f;
    //            __instance.CrewLightMod = 5f;
    //            __instance.ImpostorLightMod = 0.25f;
    //            __instance.NumImpostors = 1;
    //            __instance.NumCommonTasks = 0;
    //            __instance.NumLongTasks = 0;
    //            __instance.NumShortTasks = 10;
    //            __instance.KillCooldown = 10f;
    //        }
    //        if (Options.IsStandardHAS) //StandardHAS
    //        {
    //            __instance.PlayerSpeedMod = 1.75f;
    //            __instance.CrewLightMod = 5f;
    //            __instance.ImpostorLightMod = 0.25f;
    //            __instance.NumImpostors = 1;
    //            __instance.NumCommonTasks = 0;
    //            __instance.NumLongTasks = 0;
    //            __instance.NumShortTasks = 10;
    //            __instance.KillCooldown = 10f;
    //        }
    //        if (Options.IsCCMode)
    //        {
    //            __instance.PlayerSpeedMod = 1.5f;
    //            __instance.CrewLightMod = 0.5f;
    //            __instance.ImpostorLightMod = 0.75f;
    //            __instance.NumImpostors = 1;
    //            __instance.NumCommonTasks = 0;
    //            __instance.NumLongTasks = 0;
    //            __instance.NumShortTasks = 1;
    //            __instance.KillCooldown = 20f;
    //            __instance.NumEmergencyMeetings = 1;
    //            __instance.EmergencyCooldown = 30;
    //            __instance.KillDistance = 0;
    //            __instance.DiscussionTime = 0;
    //            __instance.VotingTime = 60;
    //        }
    //        //if (Options.IsONMode)
    //        //{
    //        //    __instance.NumCommonTasks = 1;
    //        //    __instance.NumLongTasks = 0;
    //        //    __instance.NumShortTasks = 1;
    //        //    __instance.KillCooldown = 20f;
    //        //    __instance.NumEmergencyMeetings = 0;
    //        //    __instance.KillDistance = 0;
    //        //    __instance.DiscussionTime = 0;
    //        //    __instance.VotingTime = 300;
    //        //}

    //        return false;
    //    }
    //}
//}