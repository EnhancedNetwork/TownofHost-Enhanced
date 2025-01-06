using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using UnityEngine;
using static TOHE.Translator;
using UnityEngine.Networking;
using IEnumerator = System.Collections.IEnumerator;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace TOHE;

[HarmonyPatch]
public class ModUpdater
{
    //private static readonly string URL_2018k = "http://api.tohre.dev";
    private static readonly string URL_Github = "https://api.github.com/repos/0xDrMoe/TownofHost-Enhanced";
    //public static readonly string downloadTest = "https://github.com/Pietrodjaowjao/TOHEN-Contributions/releases/download/v123123123/TOHE.dll";
    public static bool hasUpdate = false;
    //public static bool isNewer = false;
    public static bool forceUpdate = false;
    public static bool isBroken = false;
    public static bool isChecked = false;
    public static bool isFail = false;
    public static DateTime? latestVersion = null;
    public static string latestTitle = null;
    public static string downloadUrl = null;
    public static string notice = null;
    public static GenericPopup InfoPopup;
    public static PassiveButton updateButton;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.VeryLow)]
    public static void Start_Postfix(/*MainMenuManager __instance*/)
    {
        ResetUpdateButton();
        if (isChecked) return;
        //If we are not using it for now, just freaking disable it.

        Main.Instance.StartCoroutine(PrefixCoroutine());
    }

    public static IEnumerator PrefixCoroutine()
    {
        NewVersionCheck();
        DeleteOldFiles();
        InfoPopup = UnityEngine.Object.Instantiate(Twitch.TwitchManager.Instance.TwitchPopup);
        InfoPopup.name = "InfoPopup";
        InfoPopup.TextAreaTMP.GetComponent<RectTransform>().sizeDelta = new(2.5f, 2f);

        if (!isChecked)
        {
            yield return CheckReleaseFromGithub(Main.BetaBuildURL.Value != "");
            Logger.Warn("Check for updated results: " + !isFail, "CheckRelease");
            Logger.Info("hasupdate: " + hasUpdate, "CheckRelease");
            Logger.Info("forceupdate: " + forceUpdate, "CheckRelease");
            Logger.Info("downloadUrl: " + downloadUrl, "CheckRelease");
            Logger.Info("latestVersionl: " + latestVersion, "CheckRelease");
            ResetUpdateButton();
        }
    }
    public static void ResetUpdateButton()
    {
        if (updateButton == null)
        {
            updateButton = MainMenuManagerPatch.CreateButton(
                "updateButton",
                new(3.68f, -2.68f, 1f),
                new(255, 165, 0, byte.MaxValue),
                new(255, 200, 0, byte.MaxValue),
                (UnityEngine.Events.UnityAction)(() => StartUpdate(downloadUrl)),
                GetString("update"));
            updateButton.transform.localScale = Vector3.one;
        }
        updateButton.gameObject.SetActive(hasUpdate);
    }
    public static string Get(string url)
    {
        string result = "";
        HttpClient req = new();
        var res = req.GetAsync(url).Result;
        Stream stream = res.Content.ReadAsStream();
        try
        {
            //获取内容
            using StreamReader reader = new(stream);
            result = reader.ReadToEnd();
        }
        finally
        {
            stream.Close();
        }
        return result;
    }
    public static IEnumerator CheckReleaseFromGithub(bool beta = false)
    {
        Logger.Warn("Start checking for updates from Github", "CheckRelease");
        string url = URL_Github + "/releases/latest";

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = 15;
        request.SetRequestHeader("Connection", "Keep-Alive");
        request.SetRequestHeader("User-Agent", "Mozilla/5.0");
        request.chunkedTransfer = false;

        yield return request.SendWebRequest();
        yield return new WaitForSeconds(0.5f);

        if (request.result != UnityWebRequest.Result.Success)
        {
            Logger.Error($"Request failed: {request.error}: {request.result}", "CheckRelease");
            isFail = true;
            yield break;
        }

        string result = request.downloadHandler.text;

        JObject data = JsonConvert.DeserializeObject<JObject>(result);

        if (beta)
        {
            latestTitle = data["name"].ToString();
            downloadUrl = data["url"].ToString();
            hasUpdate = latestTitle != ThisAssembly.Git.Commit;
        }
        else
        {
            string publishedAt = data["published_at"]?.ToString();
            latestVersion = DateTime.TryParse(publishedAt, out DateTime parsedDate) ? parsedDate : DateTime.MinValue;
            latestTitle = $"Day: {latestVersion?.Day} Month: {latestVersion?.Month} Year: {latestVersion?.Year}";

            JArray assets = data["assets"].TryCast<JArray>();
            for (int i = 0; i < assets.Count; i++)
            {
                string assetName = assets[i]["name"].ToString();
                if (assetName.ToLower() == "tohe.dll")
                {
                    downloadUrl = assets[i]["browser_download_url"].ToString();
                    Logger.Info($"Github downloadUrl is set to {downloadUrl}", "CheckRelease");
                }
            }

            DateTime pluginTimestamp = DateTime.ParseExact(Main.PluginVersion.Substring(5, 4), "MMdd", System.Globalization.CultureInfo.InvariantCulture);
            int year = int.Parse(Main.PluginVersion.Substring(0, 4));
            pluginTimestamp = pluginTimestamp.AddYears(year - pluginTimestamp.Year);
            Logger.Info($"Day: {pluginTimestamp.Day} Month: {pluginTimestamp.Month} Year: {pluginTimestamp.Year}", "PluginVersion");
            hasUpdate = latestVersion?.Date > pluginTimestamp.Date;
        }

        Logger.Info("hasupdate: " + hasUpdate, "Github");
        Logger.Info("downloadUrl: " + downloadUrl, "Github");
        Logger.Info("latestVersion: " + latestVersion, "Github");
        Logger.Info("latestTitle: " + latestTitle, "Github");

        if (hasUpdate && (downloadUrl == null || downloadUrl == ""))
        {
            Logger.Error("Failed to get download address", "CheckRelease");
            isFail = true;
            yield break;
        }

        isChecked = true;
        isBroken = false;

        // Manually dispose of the UnityWebRequest instance
        request.Dispose();

        isFail = false;
        yield break;
    }
    public static void StartUpdate(string url)
    {
        ShowPopup(GetString("updatePleaseWait"), StringNames.Cancel, false);
        Main.Instance.StartCoroutine(DownloadDLL(url));
        return;
    }
    public static bool NewVersionCheck()
    {
        try
        {
            var fileName = Assembly.GetExecutingAssembly().Location;
            if (Directory.Exists("TOH_DATA") && File.Exists(@"./TOHE-DATA/BanWords.txt"))
            {
                DirectoryInfo di = new("TOH_DATA");
                di.Delete(true);
                Logger.Warn("Deleting old data：TOH_DATA", "NewVersionCheck");
            }
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "NewVersionCheck");
            return false;
        }
        return true;
    }
    public static void StopDownload()
    {
        cachedfileStream.Dispose();
        Main.Instance.StopAllCoroutines();
        Main.Instance.StartCoroutine(DeleteFilesAfterCancel());
    }
    public static IEnumerator DeleteFilesAfterCancel()
    {
        ShowPopup(GetString("deletingFiles"), StringNames.None, false);
        yield return new WaitForSeconds(2f);
        InfoPopup.Close();
        yield return new WaitForSeconds(0.3f);
        DeleteOldFiles();
        Application.targetFrameRate = Main.UnlockFPS.Value ? 165 : 60;
        yield break;
    }
    public static void DeleteOldFiles()
    {
        string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string searchPattern = "TOHE.dll*";
        string[] files = Directory.GetFiles(path, searchPattern);
        try
        {
            foreach (string filePath in files)
            {
                if (Path.GetFileName(filePath).EndsWith(".bak") || Path.GetFileName(filePath).EndsWith(".temp"))
                {
                    Logger.Info($"{filePath} will be deleted", "DeleteOldFiles");
                    File.Delete(filePath);
                }
            }
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to clear update residue\n{e}", "DeleteOldFiles");
        }
    }
    private static readonly object downloadLock = new();
    private static FileStream cachedfileStream;

    private static IEnumerator DownloadDLL(string url)
    {
        var savePath = "BepInEx/plugins/TOHE.dll.temp";
        Application.targetFrameRate = -1;

        // Delete the temporary file if it exists
        DeleteOldFiles();

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = 10;
        request.SetRequestHeader("Connection", "Keep-Alive");
        request.SetRequestHeader("User-Agent", "Mozilla/5.0");
        request.chunkedTransfer = false;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Logger.Error($"File retrieval failed with status code: {request.responseCode}", "DownloadDLL", false);
            ShowPopup(GetString("updateManually"), StringNames.Close, true, InfoPopup.Close);
            Application.targetFrameRate = Main.UnlockFPS.Value ? 165 : 60;
            yield break;
        }

        var total = request.downloadedBytes;
        using (var stream = new MemoryStream(request.downloadHandler.data))
        {
            using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                cachedfileStream = fileStream;
                byte[] buffer = new byte[1024];
                long readLength = 0;
                int length;

                while ((length = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    yield return null;

                    fileStream.Write(buffer, 0, length);

                    readLength += length;
                    double? progress = Math.Round((double)readLength / total * 100, 2, MidpointRounding.ToZero);

                    lock (downloadLock)
                    {
                        DownloadCallBack(total, readLength, progress ?? 0); // Call back with progress info
                    }
                }
            }
        }

        var fileName = Assembly.GetExecutingAssembly().Location;
        File.Move(fileName, fileName + ".bak");
        File.Move(savePath, fileName);
        ShowPopup(GetString("updateRestart"), StringNames.Close, true, Application.Quit);
    }
    private static void DownloadCallBack(ulong total, long downloaded, double progress)
    {
        ShowPopup($"{GetString("updateInProgress")}\n{downloaded / (1024f * 1024f):F2}/{total / (1024f * 1024f):F2} MB ({progress}%)", StringNames.Cancel, true, StopDownload);
    }
    private static void ShowPopup(string message, StringNames buttonText, bool showButton = false, Action onClick = null)
    {
        if (InfoPopup != null)
        {
            InfoPopup.Show(message);
            var button = InfoPopup.transform.FindChild("ExitGame");
            if (button != null)
            {
                button.gameObject.SetActive(showButton);
                button.GetChild(0).GetComponent<TextTranslatorTMP>().TargetText = buttonText;
                button.GetChild(0).GetComponent<TextTranslatorTMP>().ResetText();
                button.GetComponent<PassiveButton>().OnClick = new();
                if (onClick != null) button.GetComponent<PassiveButton>().OnClick.AddListener(onClick);
                else button.GetComponent<PassiveButton>().OnClick.AddListener((UnityEngine.Events.UnityAction)(() => InfoPopup.Close()));
            }
        }
    }
}
