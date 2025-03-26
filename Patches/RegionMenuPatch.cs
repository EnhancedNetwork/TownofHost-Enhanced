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
            // 每列7个按钮
            const int buttonsPerColumn = 7;

            // 计算总列数
            int columnCount = (allButtons.Count + buttonsPerColumn - 1) / buttonsPerColumn;

            // 从左上角开始的起始位置
            Vector3 startPosition = new Vector3(0, -buttonSpacing, 0);

            // 为每个按钮设置位置
            for (int i = 0; i < allButtons.Count; i++)
            {
                int col = i / buttonsPerColumn;
                int row = i % buttonsPerColumn;
                allButtons[i].transform.localPosition = startPosition + new Vector3(col * columnSpacing, -row * buttonSpacing, 0f);
            }
        }
        else
        {
            const int buttonsInFirstColumn = 5;

            // 计算第二列的按钮数量
            int buttonsInSecondColumn = allButtons.Count - buttonsInFirstColumn;

            // 确保至少有一个按钮在第一列
            if (buttonsInFirstColumn <= 0 || buttonsInSecondColumn <= 0)
            {
                // 如果按钮总数少于等于5，就直接一列显示
                for (int i = 0; i < allButtons.Count; i++)
                {
                    allButtons[i].transform.localPosition = new Vector3(0, -i * buttonSpacing, 0);
                }
                return;
            }

            // 从左上角开始的起始位置
            Vector3 startPosition = new Vector3(0, -buttonSpacing, 0);

            // 为第一列的按钮设置位置（0到4）
            for (int i = 0; i < buttonsInFirstColumn; i++)
            {
                allButtons[i].transform.localPosition = startPosition + new Vector3(0, -i * buttonSpacing, 0);
            }

            // 计算第二列按钮的起始y位置，以确保最后一个按钮与第一列最后一个按钮对齐
            float secondColumnStartY = 0;
            if (buttonsInSecondColumn > 1)
            {
                // 计算第二列起始Y位置，使最后一个按钮与第一列最后一个按钮对齐
                secondColumnStartY = (buttonsInFirstColumn - buttonsInSecondColumn) * buttonSpacing;
            }

            // 为第二列的按钮设置位置
            for (int i = 0; i < buttonsInSecondColumn; i++)
            {
                int buttonIndex = buttonsInFirstColumn + i;
                allButtons[buttonIndex].transform.localPosition = startPosition + new Vector3(columnSpacing, secondColumnStartY - i * buttonSpacing, 0);
            }
        }
    }
}
