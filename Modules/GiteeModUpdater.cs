using HarmonyLib;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Globalization;
using static TOHE.Translator;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;

namespace TOHE;

[HarmonyPatch]
public class GiteeModUpdater
{
    //private static readonly string URL_2018k = "http://api.tohre.dev";
    private static readonly string URL_Gitee = "https://gitee.com/LezaiYa/TownofHost-Enhanced/blob/main/info.json";
    //public static readonly string downloadTest = "https://gitee.com/LezaiYa/TownofHost-Enhanced/releases/download/v1.3.0/TOHE.dll";
    public static bool hasUpdate = false;
    public static bool hasOutdate = false;
    public static bool forceUpdate = false;
    public static bool isBroken = false;
    public static bool isChecked = false;
    public static Version latestVersion = null;
    public static string latestTitle = null;
    public static string GiteedownloadUrl = null;
    public static string notice = null;
    public static GenericPopup InfoPopup;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPrefix]
    [HarmonyPriority(2)]
    public static void Start_Prefix(MainMenuManager __instance)
    {
        NewVersionCheck();
        DeleteOldFiles();
        InfoPopup = UnityEngine.Object.Instantiate(Twitch.TwitchManager.Instance.TwitchPopup);
        InfoPopup.name = "InfoPopup";
        InfoPopup.TextAreaTMP.GetComponent<RectTransform>().sizeDelta = new(2.5f, 2f);
        if (!isChecked)
        {
            var done = false;
            done = CheckReleaseFromGitee(Main.BetaBuildURL.Value != "").GetAwaiter().GetResult();
            Logger.Warn("检查更新结果: " + done, "CheckRelease");
            Logger.Info("已更新: " + hasUpdate, "CheckRelease");
            Logger.Info("强制更新: " + forceUpdate, "CheckRelease");
            Logger.Info("Gitee下载链接: " + GiteedownloadUrl, "CheckRelease");
            Logger.Info("最新版本: " + latestVersion, "CheckRelease");
            //ShowPopup($"{done} {hasUpdate} {Main.version} {latestVersion} {forceUpdate} {downloadUrl}", StringNames.Close, true, false);
        }
    }

    public static string Get(string url)
    {
        string result = "";
        HttpClient req = new HttpClient();
        var res = req.GetAsync(url).Result;
        Stream stream = res.Content.ReadAsStreamAsync().Result;
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
    public static async Task<bool> CheckReleaseFromGitee(bool beta = false)
    {
        Logger.Warn("开始从Gitee检查更新", "CheckRelease");
        string url = URL_Gitee + "/releases/latest";
        try
        {
            string result;
            using (HttpClient client = new())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "TOHE Updater");
                using var response = await client.GetAsync(new Uri(url), HttpCompletionOption.ResponseContentRead);
                if (!response.IsSuccessStatusCode || response.Content == null)
                {
                    Logger.Error($"状态码: {response.StatusCode}", "CheckRelease");
                    return false;
                }
                result = await response.Content.ReadAsStringAsync();
            }
            JObject data = JObject.Parse(result);
            if (beta)
            {
                latestTitle = data["name"].ToString();
                GiteedownloadUrl = data["url"].ToString();
                hasUpdate = latestTitle != ThisAssembly.Git.Commit;
            }
            else
            {
                latestVersion = new(data["tag_name"]?.ToString().TrimStart('v'));
                latestTitle = $"Ver. {latestVersion}";
                JArray assets = data["assets"].Cast<JArray>();
                Logger.Info(assets.ToString(), "ModUpdater");
                for (int i = 0; i < assets.Count; i++)
                {
                    if (assets[i]["name"].ToString() == $"TOH-Enhanced.{latestVersion}.zip")
                    {
                        GiteedownloadUrl = assets[i]["browser_Giteedownload_url"].ToString();
                        break;
                    }
                }
                hasUpdate = latestVersion.CompareTo(Main.version) > 0;
                hasOutdate = latestVersion.CompareTo(Main.version) < 0;
            }

            Logger.Info("已更新: " + hasUpdate, "Gitee");
            Logger.Info("已过期: " + hasOutdate, "Gitee");
            Logger.Info("强制更新: " + forceUpdate, "Gitee");
            Logger.Info("Gitee下载链接: " + GiteedownloadUrl, "Gitee");
            Logger.Info("最新版本: " + latestVersion, "Gitee");
            Logger.Info("最新标题: " + latestTitle, "Gitee");

            if (GiteedownloadUrl == null || GiteedownloadUrl == "")
            {
                Logger.Error("获取下载地址失败", "CheckRelease");
                return false;
            }
            isChecked = true;
            isBroken = false;
        }
        catch (Exception ex)
        {
            isBroken = true;
            Logger.Error($"发布检查失败\n{ex}", "CheckRelease", false);
            return false;
        }
        return true;
    }
    public static void StartUpdate(string url, bool Gitee)
    {
        ShowPopup(GetString("updatePleaseWait"), StringNames.Cancel, true, false);
        if (!Gitee)
        {
        _ = DownloadDLL(url);
        }
        else
        {
        _ = DownloadDLLGitee(url);
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
                Logger.Warn("删除旧数据：TOH_DATA", "NewVersionCheck");
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
                Logger.Info($"{filePath} 将被删除", "DeleteOldFiles");
                File.Delete(filePath);
                }
            }
        }
        catch (Exception e)
        {
            Logger.Error($"清除更新残留失败\n{e}", "DeleteOldFiles");
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

            using (HttpClient client = new HttpClient())
            {
                response = await client.GetAsync(url);
            }

            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"文件检索失败，状态码: {response?.StatusCode}");
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

                    while ((length = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, length);

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
            Logger.Error($"更新失败\n{ex}", "DownloadDLL", false);
            ShowPopup(GetString("updateManually"), StringNames.Close, true, true);
            return false;
        }
        return true;
    }
    public static async Task<bool> DownloadDLLGitee(string url)
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

            using (HttpClient client = new HttpClient())
            {
                response = await client.GetAsync(url);
            }

            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"文件检索失败，状态码: {response?.StatusCode}");
            }

            var total = response.Content.Headers.ContentLength ?? 0;
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                // Specify the relative path within the ZIP archive where "TOHE.dll" is located
                var entryPath = "BepInEx/plugins/TOHE.dll";
                var entry = archive.GetEntry(entryPath);

                if (entry == null)
                {
                    throw new Exception($"'{entryPath}' 在 ZIP 压缩包中找不到");
                }

                // Extract "TOHE.dll" to the temporary file
                using (var entryStream = entry.Open())
                using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                {
                    await entryStream.CopyToAsync(fileStream);
                }
            }

            var fileName = Assembly.GetExecutingAssembly().Location;
            File.Move(fileName, fileName + ".bak");
            File.Move(savePath, fileName);
            ShowPopup(GetString("updateRestart"), StringNames.Close, true, true);
        }
        catch (Exception ex)
        {
            Logger.Error($"更新失败\n{ex}", "DownloadDLL", false);
            ShowPopup(GetString("updateManually"), StringNames.Close, true, true);
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
