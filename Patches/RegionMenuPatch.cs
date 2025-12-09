using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using System;
using TMPro;
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
        int maxPerColumn = 6;
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

        const int buttonsPerColumn = 12;
        int columnCount = (allButtons.Count + buttonsPerColumn - 1) / buttonsPerColumn;

        Vector3 startPosition = new Vector3(0, -buttonSpacing, 0);

        for (int i = 0; i < allButtons.Count; i++)
        {
            int col = i / buttonsPerColumn;
            int row = i % buttonsPerColumn;
            allButtons[i].transform.localPosition = startPosition + new Vector3(col * columnSpacing, -row * buttonSpacing, 0f);
        }
    }
}

[HarmonyPatch(typeof(FindAGameManager))]
public static class FindAGameManagerPatch
{
    // 10+ Listing ported from BetterAmongUs, coded by D1GQ
    // HostName and gamecode coded by Pietro
    public static Scroller Scroller;

    [HarmonyPatch(nameof(FindAGameManager.HandleList))]
    [HarmonyPostfix]
    public static void HandleList_Postfix(FindAGameManager __instance)
    {
        foreach (var container in __instance.gameContainers)
        {
            var child = container.transform.Find("Container");
            var tmproObject = child.Find("TrueHostName_TMP");
            TMP_Text tmpro;
            if (tmproObject == null)
            {
                tmproObject = new GameObject("TrueHostName_TMP").transform;
                tmproObject.transform.SetParent(child.transform, true);
                tmproObject.transform.localPosition = new(8.5f, -2.21f, -1f);
                tmpro = tmproObject.gameObject.AddComponent<TextMeshPro>();
            }
            else
            {
                tmpro = tmproObject.GetComponent<TextMeshPro>();
            }
            tmpro.fontSize = 2.5f;
            tmpro.text = @$"<font=""LiberationSans SDF"" material=""LiberationSans SDF RadialMenu Material"">{container.gameListing.TrueHostName}<br>{GameCode.IntToGameNameV2(container.gameListing.GameId)}";
        }
    }

    [HarmonyPatch(nameof(FindAGameManager.Start))]
    [HarmonyPrefix]
    internal static void Start_Prefix(FindAGameManager __instance)
    {
        var aspectPosition = __instance.serverDropdown.transform.parent.GetComponent<AspectPosition>();
        if (aspectPosition != null)
        {
            aspectPosition.Alignment = AspectPosition.EdgeAlignments.Top;
            aspectPosition.anchorPoint = Vector3.zero;
            aspectPosition.DistanceFromEdge = new Vector3(-1.2f, 0.3f, 0f);
            aspectPosition.AdjustPosition();
        }

        __instance.modeText.transform.localPosition -= new Vector3(0.4f, 0f, 0f);

        GameContainer gameContainer = __instance.gameContainers[4];
        GameObject gameObject = new GameObject("GameListScroller");
        gameObject.transform.SetParent(gameContainer.transform.parent);
        Scroller = gameObject.AddComponent<Scroller>();
        Scroller.Inner = gameObject.transform;
        Scroller.MouseMustBeOverToScroll = true;
        BoxCollider2D boxCollider2D = gameContainer.transform.parent.gameObject.AddComponent<BoxCollider2D>();
        boxCollider2D.size = new Vector2(100f, 100f);
        Scroller.ClickMask = boxCollider2D;
        Scroller.ScrollWheelSpeed = 0.3f;
        Scroller.SetYBoundsMin(0f);
        Scroller.SetYBoundsMax(3.5f);
        Scroller.allowY = true;
        foreach (GameContainer gameContainer2 in __instance.gameContainers)
        {
            gameContainer2.transform.SetParent(gameObject.transform);
            Vector3 position = gameContainer2.transform.position;
            gameContainer2.transform.position = new Vector3(position.x, position.y, 25f);
        }
        List<GameContainer> list = __instance.gameContainers.ToList<GameContainer>();
        for (int i = 0; i < 5; i++)
        {
            GameContainer gameContainer3 = UnityEngine.Object.Instantiate<GameContainer>(gameContainer, gameObject.transform);
            Vector3 position2 = gameContainer3.transform.position;
            gameContainer3.transform.position = new Vector3(position2.x, position2.y - 0.75f * (float)(i + 1), 25f);
            list.Add(gameContainer3);
        }
        __instance.gameContainers = list.ToArray();
        SpriteRenderer spriteRenderer = CreateBlackSquareSprite();
        spriteRenderer.transform.SetParent(gameObject.transform.parent);
        spriteRenderer.transform.localPosition = new Vector3(0f, 3f, 1f);
        spriteRenderer.transform.localScale = new Vector3(1500f, 200f, 100f);
    }

    [HarmonyPatch(nameof(FindAGameManager.RefreshList))]
    [HarmonyPostfix]
    internal static void RefreshList_Postfix()
    {
        Scroller scroller = Scroller;
        if (scroller != null)
        {
            scroller.ScrollRelative(new Vector2(0f, -100f));
        }
    }

    private static SpriteRenderer CreateBlackSquareSprite()
    {
        GameObject gameObject = new GameObject("CutOffTop");
        SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        Texture2D texture2D = new Texture2D(100, 100);
        Color[] array = texture2D.GetPixels();
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = Color.black;
        }
        texture2D.SetPixels(array);
        texture2D.Apply();
        Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
        spriteRenderer.sprite = sprite;
        gameObject.transform.localScale = new Vector3(100f, 100f, 1f);
        return spriteRenderer;
    }
}

[HarmonyPatch(typeof(EnterCodeManager), nameof(EnterCodeManager.FindGameResult))]
public static class EnterCodeManagerPatch
{
    public static IRegionInfo tempRegion;
    public static int tempGameId;
    [HarmonyPostfix]
    public static void FindGameResult_Postfix(HttpMatchmakerManager.FindGameByCodeResponse response)
    {
        if (response == null)
        {
            return;
        }
        foreach (var error in response.Errors.ToArray())
        {
            Logger.Error($"{error.Reason}", "FindGameResult");
        }
        if (ServerManager.InstanceExists)
        {
            IRegionInfo region;
            if (response.Region != StringNames.NoTranslation)
            {
                region = ServerManager.DefaultRegions.FirstOrDefault(r => r.TranslateName == response.Region);

                if (region != null)
                {
                    tempRegion = region;
                    tempGameId = response.Game.GameId;
                    Logger.Info($"Caching Official Region {tempRegion.Name} for {tempGameId}", "EnterCodeManager.FindGameResult");
                    return;
                }
            }

            region = ServerManager.Instance.AvailableRegions.FirstOrDefault(r => r.Name == response.UntranslatedRegion);
            if (region != null)
            {
                tempRegion = region;
                tempGameId = response.Game.GameId;
                Logger.Info($"Caching Region {tempRegion.Name} for {tempGameId}", "EnterCodeManager.FindGameResult");
            }
        }
    }
}
