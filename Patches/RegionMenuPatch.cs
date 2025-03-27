using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TOHE.Patches;

[HarmonyPatch(typeof(RegionMenu))]
public static class RegionMenuPatch
{
    [HarmonyPatch(nameof(RegionMenu.OnEnable))]
    [HarmonyPostfix]
    public static void AdjustButtonPositions_Postfix(RegionMenu __instance)
    {
        const int maxColumns = 4;
        int buttonsPerColumn = 6;
        float buttonSpacing = 0.6f;
        float buttonSpacingSide = 2.25f;

        List<UiElement> buttons = __instance.controllerSelectable.ToArray().ToList();

        int columnCount = (buttons.Count + buttonsPerColumn - 1) / buttonsPerColumn;

        while (columnCount > maxColumns)
        {
            buttonsPerColumn++;
            columnCount = (buttons.Count + buttonsPerColumn - 1) / buttonsPerColumn;
        }

        float totalWidth = (columnCount - 1) * buttonSpacingSide;
        float totalHeight = (buttonsPerColumn - 1) * buttonSpacing;

        Vector3 startPosition = new Vector3(-totalWidth / 2, totalHeight / 2, 0f);

        for (int i = 0; i < buttons.Count; i++)
        {
            int col = i / buttonsPerColumn;
            int row = i % buttonsPerColumn;
            buttons[i].transform.localPosition = startPosition + new Vector3(col * buttonSpacingSide, -row * buttonSpacing, 0f);
        }
    }
}

[HarmonyPatch]
public static class ServerDropDownPatch
{
    // From BetterAmongUs, code by D1GQ
    [HarmonyPatch(typeof(ServerDropdown), nameof(ServerDropdown.FillServerOptions))]
    [HarmonyPrefix]
    internal static bool FillServerOptions_Prefix(ServerDropdown __instance)
    {
        if (SceneManager.GetActiveScene().name == "FindAGame") return true;

        __instance.background.size = new Vector2(5, 1);

        int num = 0;
        int column = 0;
        const int maxPerColumn = 6;
        const float columnWidth = 4.15f;
        const float buttonSpacing = 0.5f;

        // Get all available regions except current one
        var regions = DestroyableSingleton<ServerManager>.Instance.AvailableRegions.OrderBy(ServerManager.DefaultRegions.Contains).ToList();

        // Calculate total columns needed
        int totalColumns = Mathf.Max(1, Mathf.CeilToInt(regions.Count / (float)maxPerColumn));
        int rowsInLastColumn = regions.Count % maxPerColumn;
        int maxRows = (regions.Count > maxPerColumn) ? maxPerColumn : regions.Count;

        foreach (IRegionInfo regionInfo in regions)
        {
            if (DestroyableSingleton<ServerManager>.Instance.CurrentRegion.Name == regionInfo.Name)
            {
                __instance.defaultButtonSelected = __instance.firstOption;
                __instance.firstOption.ChangeButtonText(DestroyableSingleton<TranslationController>.Instance.GetStringWithDefault(regionInfo.TranslateName, regionInfo.Name, new Il2CppReferenceArray<Il2CppSystem.Object>(0)));
                continue;
            }

            IRegionInfo region = regionInfo;
            ServerListButton serverListButton = __instance.ButtonPool.Get<ServerListButton>();

            // Calculate position based on column and row
            float xPos = (column - (totalColumns - 1) / 2f) * columnWidth;
            float yPos = __instance.y_posButton - buttonSpacing * (num % maxPerColumn);

            serverListButton.transform.localPosition = new Vector3(xPos, yPos, -1f);
            serverListButton.transform.localScale = Vector3.one;
            serverListButton.Text.text = DestroyableSingleton<TranslationController>.Instance.GetStringWithDefault(
                regionInfo.TranslateName,
                regionInfo.Name,
                new Il2CppReferenceArray<Il2CppSystem.Object>(0));
            serverListButton.Text.ForceMeshUpdate(false, false);
            serverListButton.Button.OnClick.RemoveAllListeners();
            serverListButton.Button.OnClick.AddListener((Action)(() => __instance.ChooseOption(region)));
            __instance.controllerSelectable.Add(serverListButton.Button);

            // Move to next column if current column is full
            num++;
            if (num % maxPerColumn == 0)
            {
                column++;
            }
        }

        // Calculate background dimensions
        float backgroundHeight = 1.2f + buttonSpacing * (maxRows - 1);
        float backgroundWidth = (totalColumns > 1) ?
            (columnWidth * (totalColumns - 1) + __instance.background.size.x) :
            __instance.background.size.x;

        __instance.background.transform.localPosition = new Vector3(
            0f,
            __instance.initialYPos - (backgroundHeight - 1.2f) / 2f,
            0f);
        __instance.background.size = new Vector2(backgroundWidth, backgroundHeight);

        return false;
    }

    [HarmonyPatch(typeof(ServerDropdown), nameof(ServerDropdown.FillServerOptions))]
    [HarmonyPostfix]
    // Used for find a game manager  
    internal static void FillServerOptions_Postfix(ServerDropdown __instance)
    {
        if (SceneManager.GetActiveScene().name != "FindAGame") return;
        float buttonSpacing = 0.6f;
        float columnSpacing = 4.15f;

        List<ServerListButton> allButtons = __instance.GetComponentsInChildren<ServerListButton>().OrderByDescending(b => b.transform.localPosition.y).ToList();
        if (allButtons.Count == 0)
            return;

        const int buttonsPerColumn = 7;
        int columnCount = (allButtons.Count + buttonsPerColumn - 1) / buttonsPerColumn;

        Vector3 startPosition = new Vector3(0, -buttonSpacing, 0);

        for (int i = 0; i < allButtons.Count; i++)
        {
            int col = i / buttonsPerColumn;
            int row = i % buttonsPerColumn;
            allButtons[i].transform.localPosition = startPosition + new Vector3(col * columnSpacing, -row * buttonSpacing, 0f);
        }

        int maxRows  = Math.Min(buttonsPerColumn, allButtons.Count);
        float backgroundHeight = 1.2f + buttonSpacing * (maxRows - 1);
        float backgroundWidth = (columnCount > 1) ?
            (columnSpacing * (columnCount - 1) + 5) : 5;

        // Adjust background
        __instance.background.transform.localPosition = new Vector3(
            0f,
            __instance.initialYPos - (backgroundHeight - 1.2f) / 2f,
            0f);
        __instance.background.size = new Vector2(backgroundWidth, backgroundHeight);
        __instance.background.transform.localPosition += new Vector3(4f, 0, 0);
    }
}
