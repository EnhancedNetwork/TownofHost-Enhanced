using HarmonyLib;
using System.Linq;
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

        __instance.MapMenu.MapButtons = CollectionExtensions.AddItem(__instance.MapMenu.MapButtons, dleksIconInMenuCopy).ToArray();

        float x = -1f;
        for (int index = 0; index < 6; ++index)
        {
            __instance.MapMenu.MapButtons[index].transform.SetLocalX(x);
            x += 0.34f;
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