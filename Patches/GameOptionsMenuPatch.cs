using BepInEx.Unity.IL2CPP.Utils.Collections;
using System;
using TMPro;
using TOHE.Patches;
using UnityEngine;
using static TOHE.Translator;
using Object = UnityEngine.Object;

namespace TOHE;

// Thanks: https://github.com/Yumenopai/TownOfHost_Y/blob/main/Patches/GameOptionsMenuPatch.cs
public static class ModGameOptionsMenu
{
    public static int TabIndex = 0;
    public static Il2CppSystem.Collections.Generic.Dictionary<OptionBehaviour, int> OptionList = new();
    public static Il2CppSystem.Collections.Generic.Dictionary<int, OptionBehaviour> BehaviourList = new();
    public static Il2CppSystem.Collections.Generic.Dictionary<int, CategoryHeaderMasked> CategoryHeaderList = new();
}
[HarmonyPatch(typeof(GameOptionsMenu))]
public static class GameOptionsMenuPatch
{
    public static GameOptionsMenu Instance;
    [HarmonyPatch(nameof(GameOptionsMenu.Initialize)), HarmonyPrefix]
    private static bool InitializePrefix(GameOptionsMenu __instance)
    {
        Instance ??= __instance;
        if (ModGameOptionsMenu.TabIndex < 3) return true;

        if (__instance.Children == null || __instance.Children.Count == 0)
        {
            __instance.MapPicker.gameObject.SetActive(false);
            __instance.Children = new Il2CppSystem.Collections.Generic.List<OptionBehaviour>();
            __instance.CreateSettings();
            __instance.cachedData = GameOptionsManager.Instance.CurrentGameOptions;
            for (int i = 0; i < __instance.Children.Count; i++)
            {
                OptionBehaviour optionBehaviour = __instance.Children[i];
                optionBehaviour.OnValueChanged = new Action<OptionBehaviour>(__instance.ValueChanged);
            }
            __instance.InitializeControllerNavigation();
        }

        return false;
    }
    // Thanks: https://github.com/Gurge44/EndlessHostRoles
    [HarmonyPatch(nameof(GameOptionsMenu.Initialize)), HarmonyPostfix]
    private static void InitializePostfix()
    {
        var optionMenu = GameObject.Find("PlayerOptionsMenu(Clone)");
        optionMenu?.transform.FindChild("Background")?.gameObject.SetActive(false);

        _ = new LateTask(() =>
        {
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

            var menuDescriptionText = GameSettingMenu.Instance.MenuDescriptionText;
            menuDescriptionText.m_marginWidth = 2.5f;
        }, 0.2f, "Set Menu", shoudLog: false);
    }

    [HarmonyPatch(nameof(GameOptionsMenu.CreateSettings)), HarmonyPrefix]
    private static bool CreateSettingsPrefix(GameOptionsMenu __instance)
    {
        Instance ??= __instance;
        // When is vanilla tab, run vanilla code
        if (ModGameOptionsMenu.TabIndex < 3) return true;

        __instance.scrollBar.SetYBoundsMax(CalculateScrollBarYBoundsMax());
        __instance.StartCoroutine(CoRoutine().WrapToIl2Cpp());
        return false;

        System.Collections.IEnumerator CoRoutine()
        {
            var modTab = (TabGroup)(ModGameOptionsMenu.TabIndex - 3);


            float num = 2.0f;
            const float posX = 0.952f;
            const float posZ = -2.0f;
            for (int index = 0; index < OptionItem.AllOptions.Count; index++)
            {
                var option = OptionItem.AllOptions[index];
                if (option.Tab != modTab) continue;

                var enabled = !option.IsHiddenOn(Options.CurrentGameMode) && option.Parent?.GetBool() is null or true;

                if (option is TextOptionItem)
                {
                    CategoryHeaderMasked categoryHeaderMasked = Object.Instantiate(__instance.categoryHeaderOrigin, Vector3.zero, Quaternion.identity, __instance.settingsContainer);
                    categoryHeaderMasked.SetHeader(StringNames.RolesCategory, 20);
                    categoryHeaderMasked.Title.text = option.GetName();
                    categoryHeaderMasked.transform.localScale = Vector3.one * 0.68f;
                    categoryHeaderMasked.transform.localPosition = new(-0.913f, num, posZ);
                    var chmText = categoryHeaderMasked.transform.FindChild("HeaderText").GetComponent<TextMeshPro>();
                    chmText.fontStyle = FontStyles.Bold;
                    chmText.outlineWidth = 0.17f;
                    categoryHeaderMasked.gameObject.SetActive(enabled);
                    ModGameOptionsMenu.CategoryHeaderList.TryAdd(index, categoryHeaderMasked);

                    if (enabled) num -= 0.63f;
                }
                else if (option.IsHeader && enabled) num -= 0.3f;

                if (option is TextOptionItem) continue;

                var baseGameSetting = GetSetting(option);
                if (baseGameSetting == null) continue;


                OptionBehaviour optionBehaviour;

                switch (baseGameSetting.Type)
                {
                    case OptionTypes.Checkbox:
                        {
                            optionBehaviour = Object.Instantiate(__instance.checkboxOrigin, Vector3.zero, Quaternion.identity, __instance.settingsContainer);
                            optionBehaviour.transform.localPosition = new(posX, num, posZ);

                            OptionBehaviourSetSizeAndPosition(optionBehaviour, option, baseGameSetting.Type);

                            optionBehaviour.SetClickMask(__instance.ButtonClickMask);
                            optionBehaviour.SetUpFromData(baseGameSetting, 20);
                            ModGameOptionsMenu.OptionList.TryAdd(optionBehaviour, index);
                            break;
                        }
                    case OptionTypes.String:
                        {
                            optionBehaviour = Object.Instantiate(__instance.stringOptionOrigin, Vector3.zero, Quaternion.identity, __instance.settingsContainer);
                            optionBehaviour.transform.localPosition = new(posX, num, posZ);

                            OptionBehaviourSetSizeAndPosition(optionBehaviour, option, baseGameSetting.Type);

                            if (option.Name == "Preset" && !ModGameOptionsMenu.OptionList.ContainsValue(index))
                            {
                                GameSettingMenuPatch.PresetBehaviour = optionBehaviour as StringOption;
                            }

                            optionBehaviour.SetClickMask(__instance.ButtonClickMask);
                            optionBehaviour.SetUpFromData(baseGameSetting, 20);
                            ModGameOptionsMenu.OptionList.TryAdd(optionBehaviour, index);
                            break;
                        }
                    case OptionTypes.Float:
                    case OptionTypes.Int:
                        {
                            optionBehaviour = Object.Instantiate(__instance.numberOptionOrigin, Vector3.zero, Quaternion.identity, __instance.settingsContainer);
                            optionBehaviour.transform.localPosition = new(posX, num, posZ);

                            OptionBehaviourSetSizeAndPosition(optionBehaviour, option, baseGameSetting.Type);

                            optionBehaviour.SetClickMask(__instance.ButtonClickMask);
                            optionBehaviour.SetUpFromData(baseGameSetting, 20);
                            ModGameOptionsMenu.OptionList.TryAdd(optionBehaviour, index);
                            break;
                        }
                    default:
                        continue;
                }

                optionBehaviour.transform.localPosition = new(0.952f, num, -2f);
                optionBehaviour.SetClickMask(__instance.ButtonClickMask);
                optionBehaviour.SetUpFromData(baseGameSetting, 20);
                ModGameOptionsMenu.OptionList.TryAdd(optionBehaviour, index);
                ModGameOptionsMenu.BehaviourList.TryAdd(index, optionBehaviour);
                optionBehaviour.gameObject.SetActive(enabled);
                optionBehaviour.OnValueChanged = new Action<OptionBehaviour>(__instance.ValueChanged);
                __instance.Children.Add(optionBehaviour);

                if (enabled) num -= 0.45f;

                if (index % 50 == 0) yield return null;
            }

            yield return null;

            __instance.ControllerSelectable.Clear();
            foreach (var x in __instance.scrollBar.GetComponentsInChildren<UiElement>())
            {
                __instance.ControllerSelectable.Add(x);
            }
        }

        float CalculateScrollBarYBoundsMax()
        {
            float num = 2.0f;
            foreach (var option in OptionItem.AllOptions)
            {
                if (option.Tab != (TabGroup)(ModGameOptionsMenu.TabIndex - 3)) continue;

                var enabled = !option.IsHiddenOn(Options.CurrentGameMode) && option.Parent?.GetBool() is null or true;

                if (option is TextOptionItem) num -= 0.63f;
                else if (enabled)
                {
                    if (option.IsHeader) num -= 0.3f;
                    num -= 0.45f;
                }
            }

            return -num - 1.65f;
        }
    }

    private static void OptionBehaviourSetSizeAndPosition(OptionBehaviour optionBehaviour, OptionItem option, OptionTypes type)
    {
        Vector3 positionOffset = new(0f, 0f, 0f);
        Vector3 scaleOffset = new(0f, 0f, 0f);
        Color color = new(0.8f, 0.8f, 0.8f);
        float sizeDelta_x = 5.7f;

        if (option.Parent?.Parent?.Parent != null)
        {
            scaleOffset = new(-0.18f, 0, 0);
            positionOffset = new(0.3f, 0f, 0f);
            sizeDelta_x = 5.1f;
        }
        else if (option.Parent?.Parent != null)
        {
            scaleOffset = new(-0.12f, 0, 0);
            positionOffset = new(0.2f, 0f, 0f);
            sizeDelta_x = 5.3f;
        }
        else if (option.Parent != null)
        {
            scaleOffset = new(-0.05f, 0, 0);
            positionOffset = new(0.1f, 0f, 0f);
            sizeDelta_x = 5.5f;
        }

        var labelBackground = optionBehaviour.transform.FindChild("LabelBackground");
        if (option.Tab == TabGroup.SystemSettings)
        {
            labelBackground.GetComponent<SpriteRenderer>().color = Palette.White;
        }
        else if (option.Tab == TabGroup.ModSettings)
        {
            labelBackground.GetComponent<SpriteRenderer>().color = Palette.AcceptedGreen;
        }
        else if (option.Tab == TabGroup.CovenRoles)
        {
            labelBackground.GetComponent<SpriteRenderer>().color = Palette.Purple;
        }
        else if (option.Tab == TabGroup.ImpostorRoles)
        {
            labelBackground.GetComponent<SpriteRenderer>().color = Palette.ImpostorRed;
        }
        else if (option.Tab == TabGroup.CrewmateRoles)
        {
            labelBackground.GetComponent<SpriteRenderer>().color = Palette.CrewmateBlue;
        }
        else if (option.Tab == TabGroup.NeutralRoles)
        {
            labelBackground.GetComponent<SpriteRenderer>().color = Palette.DisabledGrey;
        }
        else
        {
            labelBackground.GetComponent<SpriteRenderer>().color = Palette.Brown;
        }
        labelBackground.localScale += new Vector3(1f, -0.2f, 0f) + scaleOffset;
        labelBackground.localPosition += new Vector3(-0.6f, 0f, 0f) + positionOffset;

        var titleText = optionBehaviour.transform.FindChild("Title Text");
        titleText.localPosition += new Vector3(-0.7f, 0f, 0f) + positionOffset;
        titleText.GetComponent<RectTransform>().sizeDelta = new(sizeDelta_x, 0.37f);
        var textMeshPro = titleText.GetComponent<TextMeshPro>();
        textMeshPro.alignment = TextAlignmentOptions.MidlineLeft;
        textMeshPro.fontStyle = FontStyles.Bold;
        textMeshPro.outlineWidth = 0.17f;

        switch (type)
        {
            case OptionTypes.Checkbox:
                optionBehaviour.transform.FindChild("Toggle").localPosition = new(1.46f, -0.042f);
                break;

            case OptionTypes.String:
                optionBehaviour.transform.FindChild("PlusButton").localPosition += new Vector3(option.IsText ? 500f : 1.7f, option.IsText ? 500f : 0f, option.IsText ? 500f : 0f);
                optionBehaviour.transform.FindChild("MinusButton").localPosition += new Vector3(option.IsText ? 500f : 0.9f, option.IsText ? 500f : 0f, option.IsText ? 500f : 0f);

                var valueTMP = optionBehaviour.transform.FindChild("Value_TMP (1)");
                valueTMP.localPosition += new Vector3(1.3f, 0f, 0f);
                valueTMP.GetComponent<RectTransform>().sizeDelta = new(2.3f, 0.4f);
                goto default;

            case OptionTypes.Float:
            case OptionTypes.Int:
                optionBehaviour.transform.FindChild("PlusButton").localPosition += new Vector3(option.IsText ? 500f : 1.7f, option.IsText ? 500f : 0f, option.IsText ? 500f : 0f);
                optionBehaviour.transform.FindChild("MinusButton").localPosition += new Vector3(option.IsText ? 500f : 0.9f, option.IsText ? 500f : 0f, option.IsText ? 500f : 0f);
                optionBehaviour.transform.FindChild("Value_TMP").localPosition += new Vector3(1.3f, 0f, 0f);
                goto default;

            default:
                var valueBox = optionBehaviour.transform.FindChild("ValueBox");
                valueBox.localScale += new Vector3(0.2f, 0f, 0f);
                valueBox.localPosition += new Vector3(1.3f, 0f, 0f);
                break;
        }
    }
    public static void ReOpenSettings(int index = 4)
    {
        //Close setting menu
        GameSettingMenu.Instance.Close();

        // Auto Click "Edit" Button
        _ = new LateTask(() =>
        {
            if (!GameStates.IsLobby) return;
            var hostButtons = GameObject.Find("Host Buttons");
            if (hostButtons == null) return;
            hostButtons.transform.FindChild("Edit").GetComponent<PassiveButton>().ReceiveClickDown();
        }, 0.1f, "Click Edit Button");


        if (index < 3)
            return;

        // Change tab to Original Tab
        _ = new LateTask(() =>
        {
            if (!GameStates.IsLobby || GameSettingMenu.Instance == null) return;
            GameSettingMenu.Instance.ChangeTab(index, Controller.currentTouchType == Controller.TouchType.Joystick);
        }, 0.28f, "Change Tab");

    }
    [HarmonyPatch(nameof(GameOptionsMenu.ValueChanged)), HarmonyPrefix]
    private static bool ValueChangedPrefix(GameOptionsMenu __instance, OptionBehaviour option)
    {
        if (__instance == null || ModGameOptionsMenu.TabIndex < 3) return true;

        if (ModGameOptionsMenu.OptionList.TryGetValue(option, out var index))
        {
            var item = OptionItem.AllOptions[index];
            if (item != null && item.Children.Count > 0) ReCreateSettings(__instance);
        }
        return false;
    }
    public static void ReCreateSettings(GameOptionsMenu __instance)
    {
        if (ModGameOptionsMenu.TabIndex < 3) return;
        var modTab = (TabGroup)(ModGameOptionsMenu.TabIndex - 3);

        float num = 2.0f;
        for (int index = 0; index < OptionItem.AllOptions.Count; index++)
        {
            var option = OptionItem.AllOptions[index];
            if (option.Tab != modTab) continue;

            var enabled = !option.IsHiddenOn(Options.CurrentGameMode) && option.Parent?.GetBool() is null or true;

            if (ModGameOptionsMenu.CategoryHeaderList.TryGetValue(index, out var categoryHeaderMasked))
            {
                categoryHeaderMasked.transform.localPosition = new(-0.903f, num, -2f);
                categoryHeaderMasked.gameObject.SetActive(enabled);
                if (enabled) num -= 0.63f;
            }
            else if (option.IsHeader && enabled) num -= 0.3f;

            if (ModGameOptionsMenu.BehaviourList.TryGetValue(index, out var optionBehaviour))
            {
                optionBehaviour.transform.localPosition = new(0.952f, num, -2f);
                optionBehaviour.gameObject.SetActive(enabled);
                if (enabled) num -= 0.45f;
            }
        }

        __instance.ControllerSelectable.Clear();
        foreach (var x in __instance.scrollBar.GetComponentsInChildren<UiElement>())
            __instance.ControllerSelectable.Add(x);
        __instance.scrollBar.SetYBoundsMax(-num - 1.65f);
    }
    private static BaseGameSetting GetSetting(OptionItem item)
    {
        static t CreateAndInvoke<t>(Func<t> func) where t : BaseGameSetting
        {
            return func.Invoke();
        }

        // Redundant casts are here for clarity
        // C# dosen't support intra switch statement methods 😭

        BaseGameSetting baseGameSetting = item switch
        {
            BooleanOptionItem => CreateAndInvoke(() =>
            {
                var x = ScriptableObject.CreateInstance<CheckboxGameSetting>();
                x.Type = OptionTypes.Checkbox;

                return x;
            }),
            IntegerOptionItem integerOptionItem => CreateAndInvoke(() =>
            {
                var x = ScriptableObject.CreateInstance<IntGameSetting>();
                x.Type = OptionTypes.Int;
                x.Value = integerOptionItem.GetInt();
                x.Increment = integerOptionItem.Rule.Step;
                x.ValidRange = new(integerOptionItem.Rule.MinValue, integerOptionItem.Rule.MaxValue);
                x.ZeroIsInfinity = false;
                x.SuffixType = NumberSuffixes.Multiplier;
                x.FormatString = string.Empty;

                return x;
            }),
            FloatOptionItem floatOptionItem => CreateAndInvoke(() =>
            {
                var x = ScriptableObject.CreateInstance<FloatGameSetting>();
                x.Type = OptionTypes.Float;
                x.Value = floatOptionItem.GetFloat();
                x.Increment = floatOptionItem.Rule.Step;
                x.ValidRange = new(floatOptionItem.Rule.MinValue, floatOptionItem.Rule.MaxValue);
                x.ZeroIsInfinity = false;
                x.SuffixType = NumberSuffixes.Multiplier;
                x.FormatString = string.Empty;

                return x;
            }),
            StringOptionItem stringOptionItem => CreateAndInvoke(() =>
            {
                var x = ScriptableObject.CreateInstance<StringGameSetting>();
                x.Type = OptionTypes.String;
                x.Values = new StringNames[stringOptionItem.Selections.Length];
                x.Index = stringOptionItem.GetInt();

                return x;
            }),
            PresetOptionItem presetOptionItem => CreateAndInvoke(() =>
            {
                var x = ScriptableObject.CreateInstance<StringGameSetting>();
                x.Type = OptionTypes.String;
                x.Values = new StringNames[presetOptionItem.ValuePresets];
                x.Index = presetOptionItem.GetInt();

                return x;
            }),
            _ => null
        };

        if (baseGameSetting != null)
        {
            baseGameSetting.Title = StringNames.Accept;
        }

        return baseGameSetting;
    }
}

[HarmonyPatch(typeof(ToggleOption))]
public static class ToggleOptionPatch
{
    [HarmonyPatch(nameof(ToggleOption.Initialize)), HarmonyPrefix]
    private static bool InitializePrefix(ToggleOption __instance)
    {
        if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index))
        {
            var item = OptionItem.AllOptions[index];
            //Logger.Info($"{item.Name}, {index}", "ToggleOption.Initialize.TryGetValue");
            __instance.TitleText.text = item.GetName();
            __instance.CheckMark.enabled = item.GetBool();
            return false;
        }
        return true;
    }
    [HarmonyPatch(nameof(ToggleOption.UpdateValue)), HarmonyPrefix]
    private static bool UpdateValuePrefix(ToggleOption __instance)
    {
        if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index))
        {
            var item = OptionItem.AllOptions[index];
            //Logger.Info($"{item.Name}, {index}", "ToggleOption.UpdateValue.TryGetValue");
            item.SetValue(__instance.GetBool() ? 1 : 0);
            NotificationPopperPatch.AddSettingsChangeMessage(index, item, false);
            return false;
        }
        return true;
    }
}
[HarmonyPatch(typeof(NumberOption))]
public static class NumberOptionPatch
{
    private static int IncrementMultiplier
    {
        get
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return 5;
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) return 10;
            return 1;
        }
    }
    [HarmonyPatch(nameof(NumberOption.Initialize)), HarmonyPrefix]
    private static bool InitializePrefix(NumberOption __instance)
    {
        switch (__instance.Title)
        {
            case StringNames.GameVotingTime:
                __instance.ValidRange = new(0, 600);
                __instance.Value = (float)Math.Round(__instance.Value, 2);
                break;
            case StringNames.GameShortTasks:
            case StringNames.GameLongTasks:
            case StringNames.GameCommonTasks:
                __instance.ValidRange = new(0, 90);
                __instance.Value = (float)Math.Round(__instance.Value, 2);
                break;
            case StringNames.GameKillCooldown:
                __instance.ValidRange = new(0, 180);
                __instance.Increment = 0.5f;
                __instance.Value = (float)Math.Round(__instance.Value, 2);
                break;
            case StringNames.GamePlayerSpeed:
            case StringNames.GameCrewLight:
            case StringNames.GameImpostorLight:
                __instance.Increment = 0.05f;
                __instance.Value = (float)Math.Round(__instance.Value, 2);
                break;
            case StringNames.GameNumImpostors:
                __instance.ValidRange = new(0f, GameOptionsManager.Instance.CurrentGameOptions.MaxPlayers / 2);
                break;
        }

        if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index))
        {
            var item = OptionItem.AllOptions[index];
            __instance.TitleText.text = item.GetName();
            return false;
        }

        return true;
    }
    [HarmonyPatch(nameof(NumberOption.UpdateValue)), HarmonyPrefix]
    private static bool UpdateValuePrefix(NumberOption __instance)
    {
        if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index))
        {
            var item = OptionItem.AllOptions[index];
            //Logger.Info($"{item.Name}, {index}", "NumberOption.UpdateValue.TryGetValue");

            if (item is IntegerOptionItem integerOptionItem)
            {
                integerOptionItem.SetValue(integerOptionItem.Rule.GetNearestIndex(__instance.GetInt()));
            }
            else if (item is FloatOptionItem floatOptionItem)
            {
                floatOptionItem.SetValue(floatOptionItem.Rule.GetNearestIndex(__instance.GetFloat()));
            }
            NotificationPopperPatch.AddSettingsChangeMessage(index, item, false);
            return false;
        }
        return true;
    }
    [HarmonyPatch(nameof(NumberOption.FixedUpdate)), HarmonyPrefix]
    private static bool FixedUpdatePrefix(NumberOption __instance)
    {
        if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index))
        {
            __instance.MinusBtn.SetInteractable(true);
            __instance.PlusBtn.SetInteractable(true);

            if (__instance.oldValue != __instance.Value)
            {
                __instance.oldValue = __instance.Value;
                __instance.ValueText.text = GetValueString(__instance, __instance.Value, OptionItem.AllOptions[index]);
            }
            return false;
        }
        return true;
    }
    public static string GetValueString(NumberOption __instance, float value, OptionItem item)
    {
        if (__instance.ZeroIsInfinity && Mathf.Abs(value) < 0.0001f) return "<b>∞</b>";
        return item == null ? value.ToString(__instance.FormatString) : item.GetString();
    }
    [HarmonyPatch(nameof(NumberOption.Increase)), HarmonyPrefix]
    public static bool IncreasePrefix(NumberOption __instance)
    {
        // This is for mod options. Vanilla options's button should be disabled at this moment
        if (__instance.Value >= __instance.ValidRange.max)
        {
            __instance.Value = __instance.ValidRange.min;
            __instance.UpdateValue();
            __instance.OnValueChanged.Invoke(__instance);
            __instance.AdjustButtonsActiveState();
            return false;
        }

        var increment = IncrementMultiplier * __instance.Increment;
        if (__instance.Value + increment < __instance.ValidRange.max)
        {
            __instance.Value += increment;
            __instance.UpdateValue();
            __instance.OnValueChanged.Invoke(__instance);
            __instance.AdjustButtonsActiveState();
            return false;
        }

        return true;
    }
    [HarmonyPatch(nameof(NumberOption.Decrease)), HarmonyPrefix]
    public static bool DecreasePrefix(NumberOption __instance)
    {
        // This is for mod options. Vanilla options's button should be disabled at this moment
        if (__instance.Value <= __instance.ValidRange.min)
        {
            __instance.Value = __instance.ValidRange.max;
            __instance.UpdateValue();
            __instance.OnValueChanged.Invoke(__instance);
            __instance.AdjustButtonsActiveState();
            return false;
        }

        var increment = IncrementMultiplier * __instance.Increment;
        if (__instance.Value - increment > __instance.ValidRange.min)
        {
            __instance.Value -= increment;
            __instance.UpdateValue();
            __instance.OnValueChanged.Invoke(__instance);
            __instance.AdjustButtonsActiveState();
            return false;
        }

        return true;
    }
}
[HarmonyPatch(typeof(StringOption))]
public static class StringOptionPatch
{
    [HarmonyPatch(nameof(StringOption.Initialize)), HarmonyPrefix]
    private static bool InitializePrefix(StringOption __instance)
    {
        if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index))
        {
            var item = OptionItem.AllOptions[index];
            var name = item.GetName();
            var name1 = name;
            var language = DestroyableSingleton<TranslationController>.Instance.currentLanguage.languageID;
            //Logger.Info($" Language: {language}", "StringOption.Initialize");

            if (EnumHelper.GetAllValues<CustomRoles>().Find(x => GetString($"{x}") == name1.RemoveHtmlTags(), out var role))
            {
                name = $"<size=3.5>{name}</size>";
                __instance.TitleText.fontWeight = FontWeight.Black;
                __instance.TitleText.outlineWidth = language switch
                {
                    SupportedLangs.Russian or SupportedLangs.Japanese or SupportedLangs.SChinese or SupportedLangs.TChinese => 0.15f,
                    _ => 0.35f,
                };

                SetupHelpIcon(role, __instance);
            }
            __instance.TitleText.text = name;
            return false;
        }
        return true;
    }

    //Credit For SetupHelpIcon to EHR https://github.com/Gurge44/EndlessHostRoles/blob/main/Patches/GameOptionsMenuPatch.cs
    private static void SetupHelpIcon(CustomRoles role, StringOption __instance)
    {
        var template = __instance.transform.FindChild("MinusButton");
        var icon = Object.Instantiate(template, template.parent, true);
        icon.gameObject.SetActive(true);
        icon.name = $"{role}HelpIcon";
        var text = icon.GetComponentInChildren<TextMeshPro>();
        text.text = "?";
        text.color = Color.white;
        _ = ColorUtility.TryParseHtmlString("#117055", out var clr);
        _ = ColorUtility.TryParseHtmlString("#33d6a3", out var clr2);
        icon.FindChild("ButtonSprite").GetComponent<SpriteRenderer>().color = clr;
        var GameOptionsButton = icon.GetComponent<GameOptionButton>();
        GameOptionsButton.OnClick = new();
        GameOptionsButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
        {

            if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index))
            {
                var item = OptionItem.AllOptions[index];
                var name = item.GetName();
                if (Enum.GetValues<CustomRoles>().Find(x => GetString($"{x}") == name.RemoveHtmlTags(), out var role))
                {
                    var roleName = role.IsVanilla() ? role + "TOHE" : role.ToString();
                    var str = GetString($"{roleName}InfoLong");
                    int size = str.Length > 500 ? str.Length > 550 ? 65 : 70 : 100;
                    var infoLong = str[(str.IndexOf('\n') + 1)..str.Length];
                    var ColorRole = Utils.ColorString(Utils.GetRoleColor(role), GetString(role.ToString()));
                    var info = $"<size={size}%>{ColorRole}: {infoLong}</size>";
                    GameSettingMenu.Instance.MenuDescriptionText.text = info;
                }
            }
        }));
        GameOptionsButton.interactableColor = clr;
        GameOptionsButton.interactableHoveredColor = clr2;
        icon.localPosition += new Vector3(-0.8f, 0f, 0f);
        icon.SetAsLastSibling();

    }

    [HarmonyPatch(nameof(StringOption.UpdateValue)), HarmonyPrefix]
    private static bool UpdateValuePrefix(StringOption __instance)
    {
        if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index))
        {
            var item = OptionItem.AllOptions[index];
            //Logger.Info($"{item.Name}, {index}", "StringOption.UpdateValue.TryAdd");

            item.SetValue(__instance.GetInt());

            if (item is PresetOptionItem || (item is StringOptionItem && item.Name == "GameMode"))
            {
                if (Options.CurrentGameMode == CustomGameMode.HidenSeekTOHE && !GameStates.IsHideNSeek) //Hide & Seek
                {
                    Options.GameMode.SetValue(0);
                }
                else if (Options.CurrentGameMode == CustomGameMode.HidenSeekTOHE && GameStates.IsHideNSeek)
                {
                    Options.GameMode.SetValue(Options.GetGameModeInt(CustomGameMode.HidenSeekTOHE));
                }
                GameOptionsMenuPatch.ReOpenSettings(item.Name != "GameMode" ? 1 : 4);
            }

            NotificationPopperPatch.AddSettingsChangeMessage(index, item, false);
            return false;
        }
        return true;
    }
    [HarmonyPatch(nameof(StringOption.FixedUpdate)), HarmonyPrefix]
    private static bool FixedUpdatePrefix(StringOption __instance)
    {
        if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out var index))
        {
            var item = OptionItem.AllOptions[index];
            __instance.MinusBtn.SetInteractable(true);
            __instance.PlusBtn.SetInteractable(true);

            if (item is StringOptionItem stringOptionItem)
            {
                if (__instance.oldValue != __instance.Value)
                {
                    __instance.oldValue = __instance.Value;
                    __instance.ValueText.text = stringOptionItem.GetString();
                }
            }
            else if (item is PresetOptionItem presetOptionItem)
            {
                if (__instance.oldValue != __instance.Value)
                {
                    __instance.oldValue = __instance.Value;
                    __instance.ValueText.text = presetOptionItem.GetString();
                }
            }
            return false;
        }
        return true;
    }
    [HarmonyPatch(nameof(StringOption.Increase)), HarmonyPrefix]
    public static bool IncreasePrefix(StringOption __instance)
    {
        if (__instance.Value == __instance.Values.Length - 1)
        {
            __instance.Value = 0;
            __instance.UpdateValue();
            __instance.OnValueChanged?.Invoke(__instance);
            return false;
        }
        return true;
    }
    [HarmonyPatch(nameof(StringOption.Decrease)), HarmonyPrefix]
    public static bool DecreasePrefix(StringOption __instance)
    {
        if (__instance.Value == 0)
        {
            __instance.Value = __instance.Values.Length - 1;
            __instance.UpdateValue();
            __instance.OnValueChanged?.Invoke(__instance);
            return false;
        }
        return true;
    }
}
