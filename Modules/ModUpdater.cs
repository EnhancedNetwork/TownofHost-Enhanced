using HarmonyLib;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch]
public class ModUpdater
{
    //private static readonly string URL_2018k = "http://api.2018k.cn";
    //private static readonly string URL_Github = "https://api.github.com/repos/Loonie-Toons/TOHE-Restored";
    public static bool hasUpdate = false;
    public static bool forceUpdate = false;
    public static bool isBroken = false;
    public static bool isChecked = false;
    public static Version latestVersion = null;
    public static string latestTitle = null;
    public static string downloadUrl = null;
    public static string md5 = null;
    public static string notice = null;
    public static GenericPopup InfoPopup;
    public static int visit = 0;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPrefix]
    [HarmonyPriority(2)]
    public static void Start_Prefix(MainMenuManager __instance)
    {
        /*NewVersionCheck();
        DeleteOldFiles();
        InfoPopup = UnityEngine.Object.Instantiate(Twitch.TwitchManager.Instance.TwitchPopup);
        InfoPopup.name = "InfoPopup";
        InfoPopup.TextAreaTMP.GetComponent<RectTransform>().sizeDelta = new(2.5f, 2f);
        if (!isChecked)
        {
            var done = false;
            if (CultureInfo.CurrentCulture.Name == "zh-CN")
            {
                done = CheckRelease().GetAwaiter().GetResult();
            }
            else
            {
                done = CheckReleaseFromGithub(Main.BetaBuildURL.Value != "").GetAwaiter().GetResult();
                done = CheckRelease(done).GetAwaiter().GetResult();
            }
            Logger.Warn("检查更新结果: " + done, "CheckRelease");
            Logger.Info("hasupdate: " + hasUpdate, "CheckRelease");
            Logger.Info("forceupdate: " + forceUpdate, "CheckRelease");
            Logger.Info("downloadUrl: " + downloadUrl, "CheckRelease");
            Logger.Info("latestVersionl: " + latestVersion, "CheckRelease");
        }*/
        
    }

    public static string UrlSetId(string url) => url + "?id=6C5A46D1420E476ABD560271FC8040D7";
    public static string UrlSetCheck(string url) => url + "/checkVersion";
    public static string UrlSetInfo(string url) => url + "/getExample";
    public static string UrlSetToday(string url) => url + "/today";

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

    /*public static Task<bool> CheckRelease(bool onlyInfo = false)
    {
        Logger.Warn("开始从2018k检查更新", "CheckRelease");
        string url = UrlSetId(UrlSetCheck(URL_2018k)) + "&version=" + Main.PluginVersion;
        try
        {
            string res = Get(url);
            string[] info = res.Split("|");
            if (!onlyInfo)
            {
                hasUpdate = false;
                forceUpdate = info[1] == "false";
                latestVersion = new(info[4]);
                latestTitle = "Ver. " + info[4];

                string[] num = info[4].Split(".");
                string[] inum = Main.PluginVersion.Split(".");
                if (num.Length > inum.Length) inum.AddItem("0");
                for (int i = 0; i < num.Length; i++)
                {
                    int c = int.Parse(num[i]);
                    int m = int.Parse(inum[i]);
                    if (c > m) hasUpdate = true;
                    if (c != m) break;
                }
            }
            

#if DEBUG
            if (!hasUpdate && Main.PluginVersion == info[4] && !onlyInfo) hasUpdate = true;
#endif

            
            if (downloadUrl == null || downloadUrl == "")
            {
                Logger.Error("获取下载地址失败", "CheckRelease");
                return Task.FromResult(false);
            }

            isChecked = true;
            isBroken = false;
        }
        catch (Exception ex)
        {
            if (CultureInfo.CurrentCulture.Name == "zh-CN")
            {
                isChecked = false;
                isBroken = true;
            }
            else if (!onlyInfo)
            {
                isChecked = true;
                isBroken = false;
                Logger.Error($"检查更新时发生错误\n{ex}", "CheckRelease", false);
            }
            Logger.Error($"检查更新时发生错误，已忽略\n{ex}", "CheckRelease", false);
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }
    public static async Task<bool> CheckReleaseFromGithub(bool beta = false)
    {
        Logger.Warn("开始从Github检查更新", "CheckRelease");
        string url = beta ? Main.BetaBuildURL.Value : URL_Github + "/releases/latest";
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
                downloadUrl = data["url"].ToString();
                hasUpdate = latestTitle != ThisAssembly.Git.Commit;
            }
            else
            {
                latestVersion = new(data["tag_name"]?.ToString().TrimStart('v'));
                latestTitle = $"Ver. {latestVersion}";
                JArray assets = data["assets"].Cast<JArray>();
                for (int i = 0; i < assets.Count; i++)
                {
                    if (assets[i]["name"].ToString() == "TOHE_Steam.dll" && Constants.GetPlatformType() == Platforms.StandaloneSteamPC)
                    {
                        downloadUrl = assets[i]["browser_download_url"].ToString();
                        break;
                    }
                    if (assets[i]["name"].ToString() == "TOHE_Epic.dll" && Constants.GetPlatformType() == Platforms.StandaloneEpicPC)
                    {
                        downloadUrl = assets[i]["browser_download_url"].ToString();
                        break;
                    }
                    if (assets[i]["name"].ToString() == "TOHE.dll")
                        downloadUrl = assets[i]["browser_download_url"].ToString();
                }
                hasUpdate = latestVersion.CompareTo(Main.version) > 0;
            }

            Logger.Info("hasupdate: " + hasUpdate, "Github");
            Logger.Info("forceupdate: " + forceUpdate, "Github");
            Logger.Info("downloadUrl: " + downloadUrl, "Github");
            Logger.Info("latestVersionl: " + latestVersion, "Github");
            Logger.Info("latestTitle: " + latestTitle, "Github");

            if (downloadUrl == null || downloadUrl == "")
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
    }*/
    public static void StartUpdate(string url)
    {
        ShowPopup(GetString("updatePleaseWait"), StringNames.Cancel, true, false);
        _ = DownloadDLL(url);
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
    /*public static bool BackOldDLL()
    {
        try
        {
            foreach (var path in Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.dll"))
            {
                Logger.Info($"{Path.GetFileName(path)} 已删除", "BackOldDLL");
                File.Delete(path);
            }
            File.Move(Assembly.GetExecutingAssembly().Location + ".bak", Assembly.GetExecutingAssembly().Location);
        }
        catch
        {
            Logger.Error("回退老版本失败", "BackOldDLL");
            return false;
        }
        return true;
    }
    public static void DeleteOldFiles()
    {
        try
        {
            foreach (var path in Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.*"))
            {
                if (path.EndsWith(Path.GetFileName(Assembly.GetExecutingAssembly().Location))) continue;
                if (path.EndsWith("TOHE.dll")) continue;
                Logger.Info($"{Path.GetFileName(path)} 已删除", "DeleteOldFiles");
                File.Delete(path);
            }
        }
        catch (Exception e)
        {
            Logger.Error($"清除更新残留失败\n{e}", "DeleteOldFiles");
        }
        return;
    }*/
    private static readonly object downloadLock = new();
    public static async Task<bool> DownloadDLL(string url)
    {
        try
        {
            var savePath = "BepInEx/plugins/TOHE.dll.temp";
            File.Delete(savePath);

#nullable enable
            HttpResponseMessage? response = null;
#nullable disable
            var downloadCallBack = DownloadCallBack;
            using (HttpClient client = new HttpClient())
                response = await client.GetAsync(url);
            if (response == null)
                throw new Exception("文件获取失败");
            var total = response.Content.Headers.ContentLength ?? 0;
            var stream = await response.Content.ReadAsStreamAsync();
            var file = new FileInfo(savePath);
            using (var fileStream = file.Create())
            using (stream)
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
                        // 写入到文件
                        fileStream.Write(buffer, 0, length);

                        //更新进度
                        readLength += length;
                        double? progress = Math.Round((double)readLength / total * 100, 2, MidpointRounding.ToZero);
                        lock (downloadLock)
                        {
                            //下载完毕立刻关闭释放文件流
                            if (total == readLength && progress == 100)
                            {
                                fileStream.Close();
                                fileStream.Dispose();
                            }
                            downloadCallBack?.Invoke(total, readLength, progress ?? 0);
                        }
                    }
                }
            }

            if (GetMD5HashFromFile(savePath) != md5)
            {
                File.Delete(savePath);
                ShowPopup(GetString("downloadFailed"), StringNames.Okay, true, false);
                //MainMenuManagerPatch.UpdateButton.gameObject.SetActive(hasUpdate);
                //MainMenuManagerPatch.UpdateButton.transform.Find("FontPlacer/Text_TMP").GetComponent<TMPro.TMP_Text>().SetText($"{GetString("updateButton")}\n{latestTitle}");
                //MainMenuManagerPatch.updateButton.SetActive(true);
                //MainMenuManagerPatch.updateButton.transform.position = MainMenuManagerPatch.template.transform.position + new Vector3(0.25f, 0.75f);
            }
            else
            {
                var fileName = Assembly.GetExecutingAssembly().Location;
                File.Move(fileName, fileName + ".bak");
                File.Move("BepInEx/plugins/TOHE.dll.temp", fileName);
                ShowPopup(GetString("updateRestart"), StringNames.ExitGame, true, true);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"更新失败\n{ex}", "DownloadDLL", false);
            ShowPopup(GetString("updateManually"), StringNames.ExitGame, true, true);
            return false;
        }
        return true;
    }
    public static string GetMD5HashFromFile(string fileName)
    {
        try
        {
            FileStream file = new(fileName, FileMode.Open);
            MD5 md5 = MD5.Create();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();

            StringBuilder sb = new();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }
        catch (Exception ex)
        {
            throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
        }
    }
    private static void DownloadCallBack(long total, long downloaded, double progress)
    {
        ShowPopup($"{GetString("updateInProgress")}\n{downloaded}/{total}({progress}%)", StringNames.Cancel, true, false);
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
                button.GetComponent<PassiveButton>().OnClick = new();
                if (buttonIsExit) button.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => Application.Quit()));
                else button.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => InfoPopup.Close()));
            }
        }
    }
}