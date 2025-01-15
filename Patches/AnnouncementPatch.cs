using AmongUs.Data;
using AmongUs.Data.Player;
using Assets.InnerNet;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using LibCpp2IL;
using System;
using System.Collections;
using System.IO;
using System.Text.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace TOHE;

// code credit https://github.com/Yumenopai/TownOfHost_Y
[HarmonyPatch]
public class ModNews
{
    public int Number;
    public int BeforeNumber;
    public string Title;
    public string SubTitle;
    public string ShortTitle;
    public string Text;
    public string Date;

    public Announcement ToAnnouncement()
    {
        var result = new Announcement
        {
            Number = Number,
            Title = Title,
            SubTitle = SubTitle,
            ShortTitle = ShortTitle,
            Text = Text,
            Language = (uint)DataManager.Settings.Language.CurrentLanguage,
            Date = Date,
            Id = "ModNews"
        };

        return result;
    }
    public static List<ModNews> AllModNews = [];
    public static string ModNewsURL = "https://raw.githubusercontent.com/EnhancedNetwork/TownofHost-Enhanced/refs/heads/main/Resources/Announcements/modNews-";
    static bool downloaded = false;
    public ModNews(int Number, string Title, string SubTitle, string ShortTitle, string Text, string Date)
    {
        this.Number = Number;
        this.Title = Title;
        this.SubTitle = SubTitle;
        this.ShortTitle = ShortTitle;
        this.Text = Text;
        this.Date = Date;
        AllModNews.Add(this);
    }

    [HarmonyPatch(typeof(AnnouncementPopUp), nameof(AnnouncementPopUp.Init)), HarmonyPostfix]
    public static void Initialize_Postfix(ref Il2CppSystem.Collections.IEnumerator __result)
    {
        static IEnumerator FetchBlacklist()
        {
            if (downloaded)
            {
                yield break;
            }
            downloaded = true;
            ModNewsURL += TranslationController.Instance.currentLanguage.languageID switch
            {
                SupportedLangs.German => "de_DE.json",
                SupportedLangs.Latam => "es_419.json",
                SupportedLangs.Spanish => "es_ES.json",
                SupportedLangs.Filipino => "fil_PH.json",
                SupportedLangs.French => "fr_FR.json",
                SupportedLangs.Italian => "it_IT.json",
                SupportedLangs.Japanese => "ja_JP.json",
                SupportedLangs.Korean => "ko_KR.json",
                SupportedLangs.Dutch => "nl_NL.json",
                SupportedLangs.Brazilian => "pt_BR.json",
                SupportedLangs.Russian => "ru_RU.json",
                SupportedLangs.SChinese => "zh_CN.json",
                SupportedLangs.TChinese => "zh_TW.json",
                _ => "en_US.json", //English and any other unsupported language
            };
            var request = UnityWebRequest.Get(ModNewsURL);
            yield return request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                downloaded = false;
                Logger.Error("ModNews Error Fetch:" + request.responseCode.ToString(), "ModNews");
                LoadModNewsFromResources();
                yield break;
            }

            try
            {
                using var jsonDocument = JsonDocument.Parse(request.downloadHandler.text);
                var newsArray = jsonDocument.RootElement.GetProperty("News");

                foreach (var newsElement in newsArray.EnumerateArray())
                {
                    var number = int.Parse(newsElement.GetProperty("Number").GetString());
                    var title = newsElement.GetProperty("Title").GetString();
                    var subTitle = newsElement.GetProperty("Subtitle").GetString();
                    var shortTitle = newsElement.GetProperty("Short").GetString();
                    var body = newsElement.GetProperty("Body").EnumerateArray().ToStringEnumerable().ToString();
                    var dateString = newsElement.GetProperty("Date").GetString();
                    // Create ModNews object
                    ModNews _ = new(number, title, subTitle, shortTitle, body, dateString);
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "ModNews");
                Logger.Error("Failed to load mod info from github, load from local instead", "ModNews");
                // Use local Mod news instead
                LoadModNewsFromResources();
            }
        }
        __result = Effects.Sequence(FetchBlacklist().WrapToIl2Cpp(), __result);
    }

    private static void LoadModNewsFromResources()
    {
        string filename = TranslationController.Instance.currentLanguage.languageID switch
        {
            SupportedLangs.German => "de_DE.json",
            SupportedLangs.Latam => "es_419.json",
            SupportedLangs.Spanish => "es_ES.json",
            SupportedLangs.Filipino => "fil_PH.json",
            SupportedLangs.French => "fr_FR.json",
            SupportedLangs.Italian => "it_IT.json",
            SupportedLangs.Japanese => "ja_JP.json",
            SupportedLangs.Korean => "ko_KR.json",
            SupportedLangs.Dutch => "nl_NL.json",
            SupportedLangs.Brazilian => "pt_BR.json",
            SupportedLangs.Russian => "ru_RU.json",
            SupportedLangs.SChinese => "zh_CN.json",
            SupportedLangs.TChinese => "zh_TW.json",
            _ => "en_US.json", //English and any other unsupported language
        };

        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        using Stream resourceStream = assembly.GetManifestResourceStream("TOHE.Resources.Announcements.modNews-" + filename);
        using StreamReader reader = new(resourceStream);
        using var jsonDocument = JsonDocument.Parse(reader.ReadToEnd());
        var newsArray = jsonDocument.RootElement.GetProperty("News");

        foreach (var newsElement in newsArray.EnumerateArray())
        {
            var number = int.Parse(newsElement.GetProperty("Number").GetString());
            var title = newsElement.GetProperty("Title").GetString();
            var subTitle = newsElement.GetProperty("Subtitle").GetString();
            var shortTitle = newsElement.GetProperty("Short").GetString();
            var body = newsElement.GetProperty("Body").EnumerateArray().ToStringEnumerable().ToString();
            var dateString = newsElement.GetProperty("Date").GetString();
            // Create ModNews object
            ModNews _ = new(number, title, subTitle, shortTitle, body, dateString);
        }
    }

    [HarmonyPatch(typeof(PlayerAnnouncementData), nameof(PlayerAnnouncementData.SetAnnouncements)), HarmonyPrefix]
    public static bool SetModAnnouncements_Prefix(PlayerAnnouncementData __instance, [HarmonyArgument(0)] ref Il2CppReferenceArray<Announcement> aRange)
    {
        Logger.Info("AllModNews:" + AllModNews.Count, "ModNews");
        AllModNews.Sort((a1, a2) => { return DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)); });

        List<Announcement> FinalAllNews = [];
        AllModNews.Do(n => FinalAllNews.Add(n.ToAnnouncement()));
        foreach (var news in aRange)
        {
            if (!AllModNews.Any(x => x.Number == news.Number))
                FinalAllNews.Add(news);
        }
        FinalAllNews.Sort((a1, a2) => { return DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)); });

        aRange = new(FinalAllNews.Count);
        for (int i = 0; i < FinalAllNews.Count; i++)
            aRange[i] = FinalAllNews[i];

        return true;
    }


    [HarmonyPatch(typeof(AnnouncementPanel), nameof(AnnouncementPanel.SetUp)), HarmonyPostfix]
    public static void SetUpPanel_Postfix(AnnouncementPanel __instance, [HarmonyArgument(0)] Announcement announcement)
    {
        if (announcement.Number < 100000) return;
        var obj = new GameObject("ModLabel");
        //obj.layer = -1;
        obj.transform.SetParent(__instance.transform);
        obj.transform.localPosition = new Vector3(-0.8f, 0.13f, 0.5f);
        obj.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = Utils.LoadSprite($"TOHE.Resources.Images.CreditsButton.png", 250f);
        renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
    }
}


//    [HarmonyPatch(typeof(PlayerAnnouncementData), nameof(PlayerAnnouncementData.SetAnnouncements)), HarmonyPrefix]
//    public static bool SetModAnnouncements_Prefix(PlayerAnnouncementData __instance, [HarmonyArgument(0)] ref Il2CppReferenceArray<Announcement> aRange)
//    {
//        if (AllModNews.Count == 0)
//        {
//            Init();
//            AllModNews.Sort((a1, a2) => { return DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)); });
//        }

//        List<Announcement> FinalAllNews = [];
//        AllModNews.Do(n => FinalAllNews.Add(n.ToAnnouncement()));
//        foreach (var news in aRange.ToArray())
//        {
//            if (!AllModNews.Any(x => x.Number == news.Number))
//                FinalAllNews.Add(news);
//        }
//        FinalAllNews.Sort((a1, a2) => { return DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)); });

//        aRange = new(FinalAllNews.Count);
//        for (int i = 0; i < FinalAllNews.Count; i++)
//            aRange[i] = FinalAllNews[i];

//        return true;
//    }
//}
