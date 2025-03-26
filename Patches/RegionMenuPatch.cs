using Il2CppSystem;
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
    [HarmonyPatch(typeof(ServerDropdown), nameof(ServerDropdown.FillServerOptions))]
    [HarmonyPostfix]
    public static void AdjustButtonPositions(ServerDropdown __instance)
    {
        List<ServerListButton> allButtons = __instance.GetComponentsInChildren<ServerListButton>().ToList();

        if (allButtons.Count == 0)
            return;

        float buttonSpacing = 0.6f;
        float columnSpacing = 4.25f;

        if (SceneManager.GetActiveScene().name == "FindAGame")
        {
            const int buttonsPerColumn = 7;

            int columnCount = (allButtons.Count + buttonsPerColumn - 1) / buttonsPerColumn;

            Vector3 startPosition = new Vector3(0, -buttonSpacing, 0);

            for (int i = 0; i < allButtons.Count; i++)
            {
                int col = i / buttonsPerColumn;
                int row = i % buttonsPerColumn;
                allButtons[i].transform.localPosition = startPosition + new Vector3(col * columnSpacing, -row * buttonSpacing, 0f);
            }
        }
        else
        {
            // Total 2 colums
            const int buttonsInFirstColumn = 5;

            int buttonsInSecondColumn = allButtons.Count - buttonsInFirstColumn;

            // if we have less than 5 buttons to show, skip following codes.
            if (buttonsInFirstColumn <= 0 || buttonsInSecondColumn <= 0)
            {
                for (int i = 0; i < allButtons.Count; i++)
                {
                    allButtons[i].transform.localPosition = new Vector3(0, -i * buttonSpacing, 0);
                }
                return;
            }

            Vector3 startPosition = new Vector3(0, -buttonSpacing, 0);

            for (int i = 0; i < buttonsInFirstColumn; i++)
            {
                allButtons[i].transform.localPosition = startPosition + new Vector3(0, -i * buttonSpacing, 0);
            }

            float secondColumnStartY = 0;
            if (buttonsInSecondColumn > 1)
            {
                // Last button in second column should be at the same height as the last button in the first column
                secondColumnStartY = -(buttonsInFirstColumn - buttonsInSecondColumn) * buttonSpacing;
            }

            for (int i = 0; i < buttonsInSecondColumn; i++)
            {
                int buttonIndex = buttonsInFirstColumn + i;
                allButtons[buttonIndex].transform.localPosition = startPosition + new Vector3(columnSpacing, secondColumnStartY - i * buttonSpacing, 0);
            }
        }
    }
}
