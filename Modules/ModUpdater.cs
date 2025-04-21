using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using TOHE.Modules;
using UnityEngine;
using UnityEngine.Networking;
using static TOHE.Translator;
using IEnumerator = System.Collections.IEnumerator;

namespace TOHE;

[HarmonyPatch]
public class ModUpdater
{
    //private static readonly string URL_2018k = "http://api.tohre.dev";
    private static readonly string URL_Github = "https://api.github.com/repos/0xDrMoe/TownofHost-Enhanced";
    //public static readonly string downloadTest = "https://github.com/Pietrodjaowjao/TOHEN-Contributions/releases/download/v123123123/TOHE.dll";
    public static bool hasUpdate = false;
    private static bool firstNotify = true;
    public static bool forceUpdate = false;
    public static bool isBroken = false;
    public static bool isChecked = false;
    public static bool isFail = false;
    public static DateTime? latestVersion = null;
    public static string latestTitleModName = null;
    public static string latestTitle = null;
    public static string downloadUrl = null;
    public static string notice = null;
    public static GenericPopup InfoPopup;
    public static GenericPopup InfoPopupV2;
    public static PassiveButton updateButton;
    private static CancellationTokenSource downloadCancellationTokenSource = new();

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
        CheckCustomRegions();
        NewVersionCheck();
        DeleteOldFiles();
        InfoPopup = UnityEngine.Object.Instantiate(Twitch.TwitchManager.Instance.TwitchPopup);
        InfoPopup.name = "InfoPopup";
        InfoPopup.TextAreaTMP.GetComponent<RectTransform>().sizeDelta = new(2.5f, 2f);

        InfoPopupV2 = UnityEngine.Object.Instantiate(Twitch.TwitchManager.Instance.TwitchPopup);
        InfoPopupV2.name = "InfoPopupV2";

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

    const string RegionConfigPath = "./BepInEx/config/at.duikbo.regioninstall.cfg";
    const string MiniRegionInstallPath = "./BepInEx/plugins/Mini.RegionInstall.dll";
    const string RegionConfigResource = "TOHE.Resources.at.duikbo.regioninstall.cfg";
    const string MiniRegionInstallResource = "TOHE.Resources.Mini.RegionInstall.dll";
    private static void CheckCustomRegions()
    {
        var regions = ServerManager.Instance.AvailableRegions;
        var hasCustomRegions = false;
        var forceUpdate = false;

        foreach (var region in regions)
        {
            if (region.Name.Contains("Niko233", StringComparison.OrdinalIgnoreCase))
            {
                hasCustomRegions = true;
                break;
            }
        }

        foreach (var region in regions)
        {
            if (region.Name.Contains("Niko233(NA_US)", StringComparison.OrdinalIgnoreCase) || region.Name.Contains("NikoCat233", StringComparison.OrdinalIgnoreCase) || !region.Name.Contains("Niko233(EU)", StringComparison.OrdinalIgnoreCase))
            {
                forceUpdate = true;
                break;
            }
        }

        if (!hasCustomRegions || forceUpdate)
        {
            Logger.Info("No custom regions found.", "CheckCustomRegions");
            MoveFile();
            return;
        }

        if (!File.Exists(RegionConfigPath) || !File.Exists(MiniRegionInstallPath))
        {
            Logger.Info("Updating Region file due to it is missing.", "CheckCustomRegions");
            MoveFile();
            return;
        }

        static void MoveFile()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using Stream resourceStream = assembly.GetManifestResourceStream(RegionConfigResource);
            if (resourceStream == null)
            {
                Logger.Error($"Resource {RegionConfigResource} not found in assembly.", "MoveRegionConfig");
                return;
            }

            using FileStream fileStream = new(RegionConfigPath, FileMode.Create, FileAccess.Write);
            resourceStream.CopyTo(fileStream);
            Logger.Info($"Resource {RegionConfigResource} has been moved to {RegionConfigPath}.", "MoveRegionConfig");

            if (!File.Exists(MiniRegionInstallPath))
            {
                using Stream miniResourceStream = assembly.GetManifestResourceStream(MiniRegionInstallResource);
                if (miniResourceStream == null)
                {
                    Logger.Error($"Resource {MiniRegionInstallResource} not found in assembly.", "MoveRegionConfig");
                    return;
                }

                using FileStream miniFileStream = new(MiniRegionInstallPath, FileMode.Create, FileAccess.Write);
                miniResourceStream.CopyTo(miniFileStream);
                Logger.Info($"Resource {MiniRegionInstallResource} has been moved to {MiniRegionInstallPath}.", "MoveRegionConfig");
            }
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
    public static void ShowAvailableUpdate()
    {
        if (firstNotify && hasUpdate)
        {
            firstNotify = false;

            if (!string.IsNullOrEmpty(latestTitleModName))
                ShowPopupWithTwoButtons(string.Format(GetString("NewUpdateAvailable"), latestTitleModName), GetString("update"), onClickOnFirstButton: () => StartUpdate(downloadUrl));
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
            //ÞÄÀÕÅûÕåàÕ«╣
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

        latestTitleModName = data["name"].ToString();

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

            JArray assets = data["assets"].CastFast<JArray>();
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
        Task.Run(() => DownloadDLLAsync(url));
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
                Logger.Warn("Deleting old data´╝ÜTOH_DATA", "NewVersionCheck");
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
        lock (downloadLock)
        {
            downloadCancellationTokenSource?.Cancel();

            cachedfileStream?.Dispose();
            cachedfileStream = null;
        }
    }
    public static IEnumerator DeleteFilesAfterCancel()
    {
        ShowPopupAsync(GetString("deletingFiles"), StringNames.None, false);
        yield return new WaitForSeconds(2f);
        InfoPopup.Close();
        yield return new WaitForSeconds(0.3f);
        DeleteOldFiles();
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

    private static async Task DownloadDLLAsync(string url)
    {
        var savePath = "BepInEx/plugins/TOHE.dll.temp";

        // Delete the temporary file if it exists
        DeleteOldFiles();

        try
        {
            downloadCancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = downloadCancellationTokenSource.Token;

            using (HttpClient client = new())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Connection.Add("Keep-Alive");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"File retrieval failed with status code: {response.StatusCode}", "DownloadDLL", false);
                    ShowPopupAsync(GetString("updateManually"), StringNames.Close, true, InfoPopup.Close);
                    return;
                }

                var total = response.Content.Headers.ContentLength ?? -1L;
                using var stream = await response.Content.ReadAsStreamAsync(token);
                using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);

                cachedfileStream = fileStream;
                byte[] buffer = new byte[1024];
                long readLength = 0;
                int length;

                while ((length = await stream.ReadAsync(buffer, token)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, length), token);

                    readLength += length;
                    double progress = total > 0 ? Math.Round((double)readLength / total * 100, 2, MidpointRounding.ToZero) : 0;

                    lock (downloadLock)
                    {
                        DownloadCallBack(total, readLength, progress);
                    }
                }

                await fileStream.DisposeAsync();
            }

            var fileName = Assembly.GetExecutingAssembly().Location;
            File.Move(fileName, fileName + ".bak");
            File.Move(savePath, fileName);
            ShowPopupAsync(GetString("updateRestart"), StringNames.Close, true, Application.Quit);
        }
        catch (OperationCanceledException)
        {
            Logger.Warn("Download operation was canceled.", "DownloadDLL");
            Main.Instance.StartCoroutine(DeleteFilesAfterCancel());
        }
        catch (Exception ex)
        {
            Logger.Error($"An error occurred during the download: {ex.Message}", "DownloadDLL", false);
            ShowPopupAsync(GetString("updateManually"), StringNames.Close, true, InfoPopup.Close);
        }
        finally
        {
            cachedfileStream?.Dispose();
            cachedfileStream = null;

            downloadCancellationTokenSource?.Dispose();
            downloadCancellationTokenSource = null;
        }
    }
    private static void DownloadCallBack(long total, long downloaded, double progress)
    {
        ShowPopupAsync($"{GetString("updateInProgress")}\n{downloaded / (1024f * 1024f):F2}/{total / (1024f * 1024f):F2} MB ({progress}%)", StringNames.Cancel, true, StopDownload);
    }
    private static void ShowPopupAsync(string message, StringNames buttonText, bool showButton = false, Action onClick = null)
    {
        Dispatcher.Dispatch(() =>
        {
            ShowPopup(message, buttonText, showButton, onClick);
        });
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
    private static void ShowPopupWithTwoButtons(string message, string firstButtonText, string secondButtonText = "", Action onClickOnFirstButton = null, Action onClickOnSecondButton = null)
    {
        if (InfoPopupV2 != null)
        {
            var templateExitGame = InfoPopupV2.transform.FindChild("ExitGame");
            if (templateExitGame == null) return;

            var background = InfoPopupV2.transform.FindChild("Background");
            if (background == null) return;
            background.localScale *= 2f;

            InfoPopupV2.Show(message);
            templateExitGame.gameObject.SetActive(false);
            var firstButton = UnityEngine.Object.Instantiate(templateExitGame, InfoPopupV2.transform);
            var secondButton = UnityEngine.Object.Instantiate(templateExitGame, InfoPopupV2.transform);
            if (firstButton != null)
            {
                firstButton.gameObject.SetActive(true);
                firstButton.name = "FirstButtom";
                var firstButtonTransform = firstButton.transform;
                firstButton.transform.localPosition = new Vector3(firstButtonTransform.localPosition.x - 1f, firstButtonTransform.localPosition.y - 0.7f, firstButtonTransform.localPosition.z);
                firstButton.transform.localScale *= 1.2f;
                var firstButtonGetChild = firstButton.GetChild(0);
                firstButtonGetChild.GetComponent<TextTranslatorTMP>().TargetText = StringNames.Cancel;
                firstButtonGetChild.GetComponent<TextTranslatorTMP>().ResetText();
                firstButtonGetChild.GetComponent<TextTranslatorTMP>().DestroyTranslator();
                firstButtonGetChild.GetComponent<TextMeshPro>().text = firstButtonText;
                firstButtonGetChild.GetComponent<TMP_Text>().text = firstButtonText;
                firstButton.GetComponent<PassiveButton>().OnClick = new();
                if (onClickOnFirstButton != null)
                    firstButton.GetComponent<PassiveButton>().OnClick.AddListener((UnityEngine.Events.UnityAction)(() => { onClickOnFirstButton(); InfoPopupV2.Close(); }));
                else firstButton.GetComponent<PassiveButton>().OnClick.AddListener((UnityEngine.Events.UnityAction)(() => InfoPopupV2.Close()));
            }
            if (secondButton != null)
            {
                secondButton.gameObject.SetActive(true);
                secondButton.name = "SecondButtom";
                var secondButtonTransform = secondButton.transform;
                secondButton.transform.localPosition = new Vector3(secondButtonTransform.localPosition.x + 1f, secondButtonTransform.localPosition.y - 0.7f, secondButtonTransform.localPosition.z);
                secondButton.transform.localScale *= 1.2f;
                var secondButtonGetChild = secondButton.GetChild(0);
                secondButtonGetChild.GetComponent<TextTranslatorTMP>().TargetText = StringNames.Cancel;
                secondButtonGetChild.GetComponent<TextTranslatorTMP>().ResetText();
                if (!string.IsNullOrEmpty(secondButtonText))
                {
                    secondButtonGetChild.GetComponent<TextTranslatorTMP>().DestroyTranslator();
                    secondButtonGetChild.GetComponent<TextMeshPro>().text = secondButtonText;
                    secondButtonGetChild.GetComponent<TMP_Text>().text = secondButtonText;
                }
                secondButton.GetComponent<PassiveButton>().OnClick = new();
                if (onClickOnSecondButton != null)
                    secondButton.GetComponent<PassiveButton>().OnClick.AddListener((UnityEngine.Events.UnityAction)(() => { onClickOnSecondButton(); InfoPopupV2.Close(); }));
                else secondButton.GetComponent<PassiveButton>().OnClick.AddListener((UnityEngine.Events.UnityAction)(() => InfoPopupV2.Close()));
            }
        }
    }
}
