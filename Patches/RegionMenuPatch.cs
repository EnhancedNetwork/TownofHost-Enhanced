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
        if (FindAGameManager.Instance != null && FindAGameManager.Instance.isActiveAndEnabled)
        {
            // 每列5个按钮
            const int buttonsPerColumn = 7;
            // 按钮间的垂直间距，可根据实际需求调整
            float buttonSpacing = 0.6f;
            // 列之间的水平间距，可根据实际需求调整
            float columnSpacing = 4.25f;

            // 获取所有服务器按钮
            List<ServerListButton> allButtons = __instance.GetComponentsInChildren<ServerListButton>().ToList();

            if (allButtons.Count == 0)
                return;

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
    }
}
