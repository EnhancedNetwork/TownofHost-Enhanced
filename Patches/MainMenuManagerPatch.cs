using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static TOHE.Credentials;
using static TOHE.Translator;
using Object = UnityEngine.Object;

namespace TOHE;

[HarmonyPatch(typeof(MainMenuManager))]
public static class MainMenuManagerPatch
{
    private static PassiveButton template;
    private static PassiveButton gitHubButton;
    private static PassiveButton kofiButton;
    private static PassiveButton discordButton;
    private static PassiveButton websiteButton;
    //private static PassiveButton patreonButton;

    public static PassiveButton updateButton;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.LateUpdate)), HarmonyPostfix]
    public static void Postfix(MainMenuManager __instance)
    {
        if (__instance == null) return;
        __instance.playButton.transform.gameObject.SetActive(Options.IsLoaded);
        if (TitleLogoPatch.LoadingHint != null)
            TitleLogoPatch.LoadingHint.SetActive(!Options.IsLoaded);
        var PlayOnlineButton = __instance.PlayOnlineButton;
        if (PlayOnlineButton != null)
        {
            if (RunLoginPatch.isAllowedOnline && !Main.hasAccess)
            {
                var PlayLocalButton = __instance.playLocalButton;
                if (PlayLocalButton != null) PlayLocalButton.gameObject.SetActive(false);

                PlayOnlineButton.gameObject.SetActive(false);
                DisconnectPopup.Instance.ShowCustom(GetString("NoAccess"));
            }
        }
    }

    [HarmonyPatch(nameof(MainMenuManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.Normal)]
    public static void StartPostfix(MainMenuManager __instance)
    {
        if (template == null) template = __instance.quitButton;

        // FPS
        Application.targetFrameRate = Main.UnlockFPS.Value ? 165 : 60;

        __instance.screenTint.gameObject.transform.localPosition += new Vector3(1000f, 0f);
        __instance.screenTint.enabled = false;
        __instance.rightPanelMask.SetActive(true);
        // The background texture (large sprite asset)
        __instance.mainMenuUI.FindChild<SpriteRenderer>("BackgroundTexture").transform.gameObject.SetActive(false);
        // The glint on the Among Us Menu
        __instance.mainMenuUI.FindChild<SpriteRenderer>("WindowShine").transform.gameObject.SetActive(false);
        __instance.mainMenuUI.FindChild<Transform>("ScreenCover").gameObject.SetActive(false);

        GameObject leftPanel = __instance.mainMenuUI.FindChild<Transform>("LeftPanel").gameObject;
        GameObject rightPanel = __instance.mainMenuUI.FindChild<Transform>("RightPanel").gameObject;
        rightPanel.gameObject.GetComponent<SpriteRenderer>().enabled = false;
        GameObject maskedBlackScreen = rightPanel.FindChild<Transform>("MaskedBlackScreen").gameObject;
        maskedBlackScreen.GetComponent<SpriteRenderer>().enabled = false;
        //maskedBlackScreen.transform.localPosition = new Vector3(-3.345f, -2.05f); //= new Vector3(0f, 0f);
        maskedBlackScreen.transform.localScale = new Vector3(7.35f, 4.5f, 4f);

        __instance.mainMenuUI.gameObject.transform.position += new Vector3(-0.2f, 0f);

        leftPanel.gameObject.GetComponent<SpriteRenderer>().enabled = false;
        leftPanel.gameObject.FindChild<SpriteRenderer>("Divider").enabled = false;
        leftPanel.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.name == "Shine").ToList().ForEach(r => r.enabled = false);

        GameObject splashArt = new("SplashArt");
        splashArt.transform.position = new Vector3(0, 0f, 600f); //= new Vector3(0, 0.40f, 600f);
        var spriteRenderer = splashArt.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = Utils.LoadSprite("TOHE.Resources.Background.TOH-RE-Background-New.png", 150f);


        //__instance.playLocalButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(0.1647f, 0f, 0.7765f);
        //__instance.PlayOnlineButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(0.1647f, 0f, 0.7765f);
        //__instance.playLocalButton.transform.position = new Vector3(2.095f, -0.25f, 520f);
        //__instance.PlayOnlineButton.transform.position = new Vector3(0f, -0.25f, 0f);


    /*    __instance.playButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0.35f);
        __instance.playButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color(0.929f, 0.255f, 0.773f);
        Color originalColorPlayButton = __instance.playButton.inactiveSprites.GetComponent<SpriteRenderer>().color;
        __instance.playButton.inactiveSprites.GetComponent<SpriteRenderer>().color = originalColorPlayButton * 0.5f;
        __instance.playButton.activeTextColor = Color.white;
        __instance.playButton.inactiveTextColor = Color.white;
        __instance.playButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0.35f);

        __instance.inventoryButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0.35f);
        __instance.inventoryButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color(0.929f, 0.255f, 0.773f);
        Color originalColorInventoryButton = __instance.inventoryButton.inactiveSprites.GetComponent<SpriteRenderer>().color;
        __instance.inventoryButton.inactiveSprites.GetComponent<SpriteRenderer>().color = originalColorInventoryButton * 0.5f;
        __instance.inventoryButton.activeTextColor = Color.white;
        __instance.inventoryButton.inactiveTextColor = Color.white;
        __instance.inventoryButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0.35f);

        __instance.shopButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0.35f);
        __instance.shopButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color(0.929f, 0.255f, 0.773f);
        Color originalColorShopButton = __instance.shopButton.inactiveSprites.GetComponent<SpriteRenderer>().color;
        __instance.shopButton.inactiveSprites.GetComponent<SpriteRenderer>().color = originalColorShopButton * 0.5f;
        __instance.shopButton.activeTextColor = Color.white;
        __instance.shopButton.inactiveTextColor = Color.white;
        __instance.shopButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0.35f);



        __instance.newsButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(0.95f, 0f, 1f);
        __instance.newsButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0.85f);
        Color originalColorNewsButton = __instance.newsButton.inactiveSprites.GetComponent<SpriteRenderer>().color;
        __instance.newsButton.inactiveSprites.GetComponent<SpriteRenderer>().color = originalColorNewsButton * 0.6f;
        __instance.newsButton.activeTextColor = Color.white;
        __instance.newsButton.inactiveTextColor = Color.white;

        __instance.myAccountButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(0.95f, 0f, 1f);
        __instance.myAccountButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0.85f);
        Color originalColorMyAccount = __instance.myAccountButton.inactiveSprites.GetComponent<SpriteRenderer>().color;
        __instance.myAccountButton.inactiveSprites.GetComponent<SpriteRenderer>().color = originalColorMyAccount * 0.6f;
        __instance.myAccountButton.activeTextColor = Color.white;
        __instance.myAccountButton.inactiveTextColor = Color.white;
        __instance.accountButtons.transform.position += new Vector3(0f, 0f, -1f);

        __instance.settingsButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(0.95f, 0f, 1f);
        __instance.settingsButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0.85f);
        Color originalColorSettingsButton = __instance.settingsButton.inactiveSprites.GetComponent<SpriteRenderer>().color;
        __instance.settingsButton.inactiveSprites.GetComponent<SpriteRenderer>().color = originalColorSettingsButton * 0.6f;
        __instance.settingsButton.activeTextColor = Color.white;
        __instance.settingsButton.inactiveTextColor = Color.white;



        //__instance.creditsButton.gameObject.SetActive(false);
        //__instance.quitButton.gameObject.SetActive(false);

        __instance.quitButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(0.95f, 0f, 1f);
        __instance.quitButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0.85f);
        Color originalColorQuitButton = __instance.quitButton.inactiveSprites.GetComponent<SpriteRenderer>().color;
        __instance.quitButton.inactiveSprites.GetComponent<SpriteRenderer>().color = originalColorQuitButton * 0.6f;
        __instance.quitButton.activeTextColor = Color.white;
        __instance.quitButton.inactiveTextColor = Color.white;

        __instance.creditsButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(0.95f, 0f, 1f);
        __instance.creditsButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0.85f);
        Color originalColorCreditsButton = __instance.creditsButton.inactiveSprites.GetComponent<SpriteRenderer>().color;
        __instance.creditsButton.inactiveSprites.GetComponent<SpriteRenderer>().color = originalColorCreditsButton * 0.6f;
        __instance.creditsButton.activeTextColor = Color.white;
        __instance.creditsButton.inactiveTextColor = Color.white; */



        if (template == null) return;


        // ko-fi Button
        if (kofiButton == null)
        {
            kofiButton = CreateButton(
                "kofiButton",
                new(-1.8f, -1.1f, 1f),
                new(0, 255, 255, byte.MaxValue),
                new(75, 255, 255, byte.MaxValue),
                () => Application.OpenURL(Main.kofiInviteUrl),
                GetString("kofi")); //"Kofi"
        }
        kofiButton.gameObject.SetActive(Main.ShowKofiButton);

        // update Button
        if (updateButton == null)
        {
            updateButton = CreateButton(
                "updateButton",
                new(3.68f, -2.68f, 1f),
                new(255, 165, 0, byte.MaxValue),
                new(255, 200, 0, byte.MaxValue),
                () => ModUpdater.StartUpdate(ModUpdater.downloadUrl, true),
                GetString("update")); //"Update"
            updateButton.transform.localScale = Vector3.one;
        }
        updateButton.gameObject.SetActive(ModUpdater.hasUpdate);

        // GitHub Button
        if (gitHubButton == null)
        {
            gitHubButton = CreateButton(
                "GitHubButton",
                new(-1.8f, -1.5f, 1f),
                new(153, 153, 153, byte.MaxValue),
                new(209, 209, 209, byte.MaxValue),
                () => Application.OpenURL(Main.GitHubInviteUrl),
                GetString("GitHub")); //"GitHub"
        }
        gitHubButton.gameObject.SetActive(Main.ShowGitHubButton);

        // Discord Button
        if (discordButton == null)
        {
            discordButton = CreateButton(
                "DiscordButton",
                new(-1.8f, -1.9f, 1f),
                new(88, 101, 242, byte.MaxValue),
                new(148, 161, byte.MaxValue, byte.MaxValue),
                () => Application.OpenURL(Main.DiscordInviteUrl),
                GetString("Discord")); //"Discord"
        }
        discordButton.gameObject.SetActive(Main.ShowDiscordButton);

        // Website Button
        if (websiteButton == null)
        {
            websiteButton = CreateButton(
                "WebsiteButton",
                new(-1.8f, -2.3f, 1f),
                new(251, 81, 44, byte.MaxValue),
                new(211, 77, 48, byte.MaxValue),
                () => Application.OpenURL(Main.WebsiteInviteUrl),
                GetString("Website")); //"Website"
        }
        websiteButton.gameObject.SetActive(Main.ShowWebsiteButton);

        var howToPlayButton = __instance.howToPlayButton;
        var freeplayButton = howToPlayButton.transform.parent.Find("FreePlayButton");

        if (freeplayButton != null) freeplayButton.gameObject.SetActive(false);

        howToPlayButton.transform.SetLocalX(0);

    }

    private static PassiveButton CreateButton(string name, Vector3 localPosition, Color32 normalColor, Color32 hoverColor, Action action, string label, Vector2? scale = null)
    {
        var button = Object.Instantiate(template, Credentials.ToheLogo.transform);
        button.name = name;
        Object.Destroy(button.GetComponent<AspectPosition>());
        button.transform.localPosition = localPosition;

        button.OnClick = new();
        button.OnClick.AddListener(action);

        var buttonText = button.transform.Find("FontPlacer/Text_TMP").GetComponent<TMP_Text>();
        buttonText.DestroyTranslator();
        buttonText.fontSize = buttonText.fontSizeMax = buttonText.fontSizeMin = 3.5f;
        buttonText.enableWordWrapping = false;
        buttonText.text = label;
        var normalSprite = button.inactiveSprites.GetComponent<SpriteRenderer>();
        var hoverSprite = button.activeSprites.GetComponent<SpriteRenderer>();
        normalSprite.color = normalColor;
        hoverSprite.color = hoverColor;

        var container = buttonText.transform.parent;
        Object.Destroy(container.GetComponent<AspectPosition>());
        Object.Destroy(buttonText.GetComponent<AspectPosition>());
        container.SetLocalX(0f);
        buttonText.transform.SetLocalX(0f);
        buttonText.horizontalAlignment = HorizontalAlignmentOptions.Center;

        var buttonCollider = button.GetComponent<BoxCollider2D>();
        if (scale.HasValue)
        {
            normalSprite.size = hoverSprite.size = buttonCollider.size = scale.Value;
        }

        buttonCollider.offset = new(0f, 0f);

        return button;
    }
    public static void Modify(this PassiveButton passiveButton, Action action)
    {
        if (passiveButton == null) return;
        passiveButton.OnClick = new Button.ButtonClickedEvent();
        passiveButton.OnClick.AddListener(action);
    }
    public static T FindChild<T>(this MonoBehaviour obj, string name) where T : Object
    {
        string name2 = name;
        return obj.GetComponentsInChildren<T>().First((T c) => c.name == name2);
    }
    public static T FindChild<T>(this GameObject obj, string name) where T : Object
    {
        string name2 = name;
        return obj.GetComponentsInChildren<T>().First((T c) => c.name == name2);
    }
    public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> action)
    {
        //if (source == null) throw new ArgumentNullException("source");
        if (source == null) throw new ArgumentNullException(nameof(source));

        IEnumerator<TSource> enumerator = source.GetEnumerator();
        while (enumerator.MoveNext())
        {
            action(enumerator.Current);
        }

        enumerator.Dispose();
    }

    [HarmonyPatch(nameof(MainMenuManager.OpenGameModeMenu))]
    [HarmonyPatch(nameof(MainMenuManager.OpenAccountMenu))]
    [HarmonyPatch(nameof(MainMenuManager.OpenCredits))]
    [HarmonyPostfix]
    public static void OpenMenuPostfix()
    {
        if (ToheLogo != null) ToheLogo.gameObject.SetActive(false);
    }
    [HarmonyPatch(nameof(MainMenuManager.ResetScreen)), HarmonyPostfix]
    public static void ResetScreenPostfix()
    {
        if (ToheLogo != null) ToheLogo.gameObject.SetActive(true);
    }
}