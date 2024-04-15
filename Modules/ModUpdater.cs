using HarmonyLib;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using static TOHE.Translator;
using Newtonsoft.Json.Linq;
using System.Globalization;

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
    public static DateTime? latestVersion = null;
    public static string latestTitle = null;
    public static string downloadUrl = null;
    public static string notice = null;
    public static GenericPopup InfoPopup;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPrefix]
    [HarmonyPriority(2)]
    public static void Start_Prefix(/*MainMenuManager __instance*/)
    {
        if (isChecked) return;
        //If we are not using it for now, just freaking disable it.

        NewVersionCheck();
        DeleteOldFiles();
        InfoPopup = UnityEngine.Object.Instantiate(Twitch.TwitchManager.Instance.TwitchPopup);
        InfoPopup.name = "InfoPopup";
        InfoPopup.TextAreaTMP.GetComponent<RectTransform>().sizeDelta = new(2.5f, 2f);
        if (!isChecked)
        {
            bool done = CheckReleaseFromGithub(Main.BetaBuildURL.Value != "");
            Logger.Warn("Check for updated results: " + done, "CheckRelease");
            Logger.Info("hasupdate: " + hasUpdate, "CheckRelease");
            Logger.Info("forceupdate: " + forceUpdate, "CheckRelease");
            Logger.Info("downloadUrl: " + downloadUrl, "CheckRelease");
            Logger.Info("latestVersionl: " + latestVersion, "CheckRelease");
            //ShowPopup($"{done} {hasUpdate} {Main.version} {latestVersion} {forceUpdate} {downloadUrl}", StringNames.Close, true, false);
        }
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
    public static bool CheckReleaseFromGithub(bool beta = false)
    {
        Logger.Warn("Start checking for updates from Github", "CheckRelease");
        string url = URL_Github + "/releases/latest";
        try
        {
            string result;
            using var httpClientHandler = new HttpClientHandler();
            httpClientHandler.UseProxy = true;
            
            using (HttpClient client = new(httpClientHandler))
            {
                client.DefaultRequestHeaders.Add("User-Agent", "TOHE Updater");
                var response = client.GetAsync(new Uri(url), HttpCompletionOption.ResponseContentRead).Result;

                if (!response.IsSuccessStatusCode || response.Content == null)
                {
                    Logger.Error($"Status Code: {response.StatusCode}", "CheckRelease");
                    return false;
                }

                result = response.Content.ReadAsStringAsync().Result;
            }
            JObject data = JObject.Parse(result);
            if (beta)
            {
                latestTitle = data["name"].ToString();
                downloadUrl = data["url"].ToString();
                hasUpdate = latestTitle != ThisAssembly.Git.Commit;
            }
            else
            {
                string publishedAt = data["published_at"]?.ToString();
                DateTime? latestVersion = DateTime.TryParse(publishedAt, out DateTime parsedDate) ? parsedDate : (DateTime?)null;
                latestTitle = $"Day: {latestVersion?.Day} Month: {latestVersion?.Month} Year: {latestVersion?.Year}";

                JArray assets = data["assets"].Cast<JArray>();
                for (int i = 0; i < assets.Count; i++)
                {
                    string assetName = assets[i]["name"].ToString();

                    if (assetName.ToLower() == "tohe.dll")
                    {
                        downloadUrl = assets[i]["browser_download_url"].ToString();
                        Logger.Info($"Github downloadUrl is set to {downloadUrl}", "CheckRelease");
                    }
                }

                DateTime pluginTimestamp = DateTime.ParseExact(Main.PluginVersion.Substring(5, 4), "MMdd", CultureInfo.InvariantCulture);
                int year = int.Parse(Main.PluginVersion.Substring(0, 4));
                pluginTimestamp = pluginTimestamp.AddYears(year - pluginTimestamp.Year);
                Logger.Info($"Day: {pluginTimestamp.Day} Month: {pluginTimestamp.Month} Year: {pluginTimestamp.Year}", "PluginVersion");
                hasUpdate = latestVersion?.Date > pluginTimestamp.Date;
            }

            Logger.Info("hasupdate: " + hasUpdate, "Github");
            Logger.Info("forceupdate: " + forceUpdate, "Github");
            Logger.Info("downloadUrl: " + downloadUrl, "Github");
            Logger.Info("latestVersionl: " + latestVersion, "Github");
            Logger.Info("latestTitle: " + latestTitle, "Github");

            if (hasUpdate && (downloadUrl == null || downloadUrl == ""))
            {
                Logger.Error("Failed to get download address", "CheckRelease");
                return false;
            }

            isChecked = true;
            isBroken = false;
        }
        catch (Exception ex)
        {
            isBroken = true;
            Logger.Error($"Publishing Check Failure\n{ex}", "CheckRelease", false);
            return false;
        }
        return true;
    }
    public static void StartUpdate(string url, bool github)
    {
        ShowPopup(GetString("updatePleaseWait"), StringNames.Cancel, true, false);
        if (!github)
        {
            _ = DownloadDLL(url);
        }
        else
        {
            _ = DownloadDLLGithub(url);
        }
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
    public static async Task<bool> DownloadDLL(string url)
    {
        try
        {
            var savePath = "BepInEx/plugins/TOHE.dll.temp";

            // Delete the temporary file if it exists
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }

            HttpResponseMessage response;
            var downloadCallBack = DownloadCallBack;

            using (HttpClient client = new())
            {
                response = await client.GetAsync(url);
            }

            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"File retrieval failed with status code: {response?.StatusCode}");
            }

            var total = response.Content.Headers.ContentLength ?? 0;
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                if (downloadCallBack == null)
                {
                    await stream.CopyToAsync(fileStream);
                }
                else
                {
                    byte[] buffer = new byte[1024];
                    long readLength = 0;
                    int length;

                    while ((length = await stream.ReadAsync(buffer)) != 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, length));

                        readLength += length;
                        double? progress = Math.Round((double)readLength / total * 100, 2, MidpointRounding.ToZero);
                    }
                }
            }

            var fileName = Assembly.GetExecutingAssembly().Location;
            File.Move(fileName, fileName + ".bak");
            File.Move(savePath, fileName);
            ShowPopup(GetString("updateRestart"), StringNames.Close, true, true);
        }
        catch (Exception ex)
        {
            Logger.Error($"Update failed\n{ex}", "DownloadDLL", false);
            ShowPopup(GetString("updateManually"), StringNames.Close, true, false);
            return false;
        }
        return true;
    }
    public static async Task<bool> DownloadDLLGithub(string url)
    {
        try
        {
            Logger.Info($"DownLoading Github dll from '${url}'", "DownloadDLLGithub");
            if (url == null || url == "") throw new Exception($"url is empty, cannot update!");

            var savePath = "BepInEx/plugins/TOHE.dll.temp";

            // Delete the temporary file if it exists
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }

            HttpResponseMessage response;

            using (var httpClientHandler = new HttpClientHandler())
            {
                // Use System Proxy
                httpClientHandler.UseProxy = true;

                using HttpClient client = new(httpClientHandler);
                response = await client.GetAsync(url);
            }

            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"File retrieval failed with status code: {response?.StatusCode}");
            }

            using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await response.Content.CopyToAsync(fileStream);
            }

            var fileName = Assembly.GetExecutingAssembly().Location;
            File.Move(fileName, fileName + ".bak");
            File.Move(savePath, fileName);
            ShowPopup(GetString("updateRestart"), StringNames.Close, true, true);
        }
        catch (Exception ex)
        {
            Logger.Error($"Update failed\n{ex}", "DownloadDLL", false);
            ShowPopup(GetString("updateManually"), StringNames.Close, true, false);
            return false;
        }
        return true;
    }
    private static void DownloadCallBack(long total, long downloaded, double progress)
    {
    }
    private static void ShowPopup(string message, StringNames buttonText, bool showButton = false, bool buttonIsExit = true)
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
                if (buttonIsExit) button.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => Application.Quit()));
                else button.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => InfoPopup.Close()));
            }
        }
    }
}
