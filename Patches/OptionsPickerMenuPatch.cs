using AmongUs.GameOptions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace TOHE.Patches;

// Thanks: https://github.com/SubmergedAmongUs/Submerged/blob/4a5a6b47cbed526670ae4b7eae76acd7c42e35de/Submerged/UI/Patches/MapSelectButtonPatches.cs#L49
[HarmonyPatch(typeof(CreateOptionsPicker), nameof(CreateOptionsPicker.Awake))]
class CreateOptionsPickerPatch
{
    public static void Postfix(CreateOptionsPicker __instance)
    {
        Transform mapPickerTransform = __instance.transform.Find("MapPicker");
        MapPickerMenu mapPickerMenu = mapPickerTransform.Find("Map Picker Menu").GetComponent<MapPickerMenu>();

        MapFilterButton airhipIconInMenu = __instance.MapMenu.MapButtons[3];
        MapFilterButton fungleIconInMenu = __instance.MapMenu.MapButtons[4];
        MapFilterButton skeldIconInMenu = __instance.MapMenu.MapButtons[0];
        MapFilterButton dleksIconInMenuCopy = Object.Instantiate(airhipIconInMenu, airhipIconInMenu.transform.parent);

        Transform skeldMenuButton = mapPickerMenu.transform.Find("Skeld");
        Transform polusMenuButton = mapPickerMenu.transform.Find("Polus");
        Transform airshipMenuButton = mapPickerMenu.transform.Find("Airship");
        Transform fungleMenuButton = mapPickerMenu.transform.Find("Fungle");
        Transform dleksMenuButtonCopy = Object.Instantiate(airshipMenuButton, airshipMenuButton.parent);

        // Set mapid for Dleks button
        PassiveButton dleksButton = dleksMenuButtonCopy.GetComponent<PassiveButton>();
        dleksButton.OnClick.m_PersistentCalls.m_Calls._items[0].arguments.intArgument = (int)MapNames.Dleks;

        SpriteRenderer dleksImage = dleksMenuButtonCopy.Find("Image").GetComponent<SpriteRenderer>();
        dleksImage.sprite = skeldMenuButton.Find("Image").GetComponent<SpriteRenderer>().sprite;

        dleksIconInMenuCopy.name = "Dleks";
        dleksIconInMenuCopy.transform.localPosition = new Vector3(0.8f, airhipIconInMenu.transform.localPosition.y, airhipIconInMenu.transform.localPosition.z);
        dleksIconInMenuCopy.MapId = MapNames.Dleks;
        dleksIconInMenuCopy.Button = dleksButton;
        dleksIconInMenuCopy.ButtonCheck = dleksMenuButtonCopy.Find("selectedCheck").GetComponent<SpriteRenderer>();
        dleksIconInMenuCopy.ButtonImage = dleksImage;
        dleksIconInMenuCopy.ButtonOutline = dleksImage.transform.parent.GetComponent<SpriteRenderer>();
        dleksIconInMenuCopy.Icon.sprite = skeldIconInMenu.Icon.sprite;

        dleksMenuButtonCopy.name = "Dleks";
        dleksMenuButtonCopy.position = new Vector3(dleksMenuButtonCopy.position.x, 2f * dleksMenuButtonCopy.position.y - polusMenuButton.transform.position.y, dleksMenuButtonCopy.position.z);
        fungleMenuButton.position = new Vector3(fungleMenuButton.position.x, dleksMenuButtonCopy.transform.position.y - 0.6f, fungleMenuButton.position.z);

        __instance.MapMenu.MapButtons = HarmonyLib.CollectionExtensions.AddItem(__instance.MapMenu.MapButtons, dleksIconInMenuCopy).ToArray();

        float xPos = -1f;
        for (int index = 0; index < 6; ++index)
        {
            __instance.MapMenu.MapButtons[index].transform.SetLocalX(xPos);
            xPos += 0.34f;
        }

        if (__instance.mode == SettingsMode.Host)
        {
            mapPickerMenu.transform.localPosition = new Vector3(mapPickerMenu.transform.localPosition.x, 0.85f, mapPickerMenu.transform.localPosition.z);

            mapPickerTransform.localScale = new Vector3(0.86f, 0.85f, 1f);
            mapPickerTransform.transform.localPosition = new Vector3(mapPickerTransform.transform.localPosition.x + 0.05f, mapPickerTransform.transform.localPosition.y + 0.03f, mapPickerTransform.transform.localPosition.z);
        }

        SwapIconOrButtomsPositions(airhipIconInMenu, dleksIconInMenuCopy);
        SwapIconOrButtomsPositions(fungleIconInMenu, airhipIconInMenu);

        SwapIconOrButtomsPositions(airshipMenuButton, dleksMenuButtonCopy);

        // set flipped dleks map Icon/button
        __instance.MapMenu.MapButtons[5].SetFlipped(true);

        mapPickerMenu.transform.Find("Backdrop").localScale *= 5;
    }
    private static void SwapIconOrButtomsPositions(Component one, Component two)
    {
        Transform transform1 = one.transform;
        Transform transform2 = two.transform;
        Vector3 position1 = two.transform.position;
        Vector3 position2 = one.transform.position;
        transform1.position = position1;
        Vector3 vector3 = position2;
        transform2.position = vector3;
    }
}

//Thanks: https://github.com/TheOtherRolesAU/TheOtherRoles/blob/8c55511599d73ba2c166491f7d6d4351d04f7ce8/TheOtherRoles/Patches/CreateOptionsPickerPatch.cs#L10
[HarmonyPatch(typeof(GameModeMenu))]
class GameModeMenuPatch
{
    [HarmonyPatch(typeof(GameModeMenu), nameof(GameModeMenu.OnEnable))]
    public static bool Prefix(GameModeMenu __instance)
    {
        uint gameMode = (uint)__instance.Parent.GetTargetOptions().GameMode;
        float num = (Mathf.CeilToInt(3f / 10f) / 2f - 0.5f) * -2.5f;
        __instance.controllerSelectable.Clear();
        int num2 = 0;
        __instance.ButtonPool.poolSize = 3;
        for (int i = 0; i <= 3; i++)
        {
            GameModes entry = (GameModes)i;
            if (entry != GameModes.None)
            {
                ChatLanguageButton chatLanguageButton = __instance.ButtonPool.Get<ChatLanguageButton>();
                chatLanguageButton.transform.localPosition = new Vector3(num + (float)(num2 / 10) * 2.5f, 2f - (float)(num2 % 10) * 0.5f, 0f);
                if (i <= 2)
                    chatLanguageButton.Text.text = DestroyableSingleton<TranslationController>.Instance.GetString(GameModesHelpers.ModeToName[entry], new Il2CppReferenceArray<Il2CppSystem.Object>(0));
                else
                {
                    chatLanguageButton.Text.text = i == 3 ? "FFA" : "Unknown";
                }
                chatLanguageButton.Button.OnClick.RemoveAllListeners();
                chatLanguageButton.Button.OnClick.AddListener((System.Action)delegate {
                    __instance.ChooseOption(entry);
                });

                bool isCurrentMode = i <= 2 && Options.CurrentGameMode == CustomGameMode.Standard ? (long)entry == (long)((ulong)gameMode) : (i == 3 && Options.CurrentGameMode == CustomGameMode.FFA || i == 4 && Options.CurrentGameMode == CustomGameMode.HidenSeekTOHE);
                chatLanguageButton.SetSelected(isCurrentMode);
                __instance.controllerSelectable.Add(chatLanguageButton.Button);
                if (isCurrentMode)
                {
                    __instance.defaultButtonSelected = chatLanguageButton.Button;
                }
                num2++;
            }
        }
        ControllerManager.Instance.OpenOverlayMenu(__instance.name, __instance.BackButton, __instance.defaultButtonSelected, __instance.controllerSelectable, false);
        return false;
    }

    [HarmonyPatch(typeof(CreateOptionsPicker), nameof(CreateOptionsPicker.SetGameMode))]
    public static bool Prefix(CreateOptionsPicker __instance, ref GameModes mode)
    {
        if (mode <= GameModes.HideNSeek)
        {
            Options.CurrentGameMode = CustomGameMode.Standard;
            return true;
        }

        __instance.SetGameMode(GameModes.Normal);
        int gm = (int)mode - 1;
        if (gm == (int)CustomGameMode.FFA)
        {
            __instance.GameModeText.text = "FFA";
            Options.CurrentGameMode = CustomGameMode.FFA;
        }
        return false;
    }
}