using AmongUs.Data;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static TOHE.Translator;

namespace TOHE;

public static class TemplateManager
{
    private static readonly string TEMPLATE_FILE_PATH = "./TOHE-DATA/template.txt";
    private static readonly Dictionary<string, Func<string>> _replaceDictionaryNormalOptions = new()
    {
        ["RoomCode"] = () => InnerNet.GameCode.IntToGameName(AmongUsClient.Instance.GameId),
        ["HostName"] = () => DataManager.Player.Customization.Name,
        ["AmongUsVersion"] = () => UnityEngine.Application.version,
        ["InternalVersion"] = () => Main.PluginVersion,
        ["ModVersion"] = () => Main.PluginDisplayVersion,
        ["Map"] = () => Constants.MapNames[Main.NormalOptions.MapId],
        ["NumEmergencyMeetings"] = () => Main.NormalOptions.NumEmergencyMeetings.ToString(),
        ["EmergencyCooldown"] = () => Main.NormalOptions.EmergencyCooldown.ToString(),
        ["DiscussionTime"] = () => Main.NormalOptions.DiscussionTime.ToString(),
        ["VotingTime"] = () => Main.NormalOptions.VotingTime.ToString(),
        ["PlayerSpeedMod"] = () => Main.NormalOptions.PlayerSpeedMod.ToString(),
        ["CrewLightMod"] = () => Main.NormalOptions.CrewLightMod.ToString(),
        ["ImpostorLightMod"] = () => Main.NormalOptions.ImpostorLightMod.ToString(),
        ["KillCooldown"] = () => Main.NormalOptions.KillCooldown.ToString(),
        ["NumCommonTasks"] = () => Main.NormalOptions.NumCommonTasks.ToString(),
        ["NumLongTasks"] = () => Main.NormalOptions.NumLongTasks.ToString(),
        ["NumShortTasks"] = () => Main.NormalOptions.NumShortTasks.ToString(),
        ["Date"] = () => DateTime.Now.ToShortDateString(),
        ["Time"] = () => DateTime.Now.ToShortTimeString(),
        ["PlayerName"] = () => ""
        
    };

    private static readonly Dictionary<string, Func<string>> _replaceDictionaryHideNSeekOptions = new()
    {
        ["RoomCode"] = () => InnerNet.GameCode.IntToGameNameV2(AmongUsClient.Instance.GameId),
        ["HostName"] = () => DataManager.Player.Customization.Name,
        ["AmongUsVersion"] = () => UnityEngine.Application.version,
        ["InternalVersion"] = () => Main.PluginVersion,
        ["ModVersion"] = () => Main.PluginDisplayVersion,
        ["Map"] = () => Constants.MapNames[Main.NormalOptions.MapId],
        ["PlayerSpeedMod"] = () => Main.HideNSeekOptions.PlayerSpeedMod.ToString(),
        ["Date"] = () => DateTime.Now.ToShortDateString(),
        ["Time"] = () => DateTime.Now.ToShortTimeString(),
        ["PlayerName"] = () => ""

    };

    public static void Init()
    {
        CreateIfNotExists();
    }

    public static void CreateIfNotExists()
    {

        try
        {
            string fileName;
            string name = CultureInfo.CurrentCulture.Name;
            if (name.Length >= 2)
                fileName = name switch // https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c
                {
                    var lang when lang.StartsWith("ru") => "Russian",
                    "zh-Hans" or "zh" or "zh-CN" or "zn-SG" => "SChinese",
                    "zh-Hant" or "zh-HK" or "zh-MO" or "zh-TW" => "TChinese",
                    "pt-BR" => "Brazilian",
                    "es-419" => "Latam",
                    var lang when lang.StartsWith("es") => "Spanish",
                    var lang when lang.StartsWith("fr") => "French",
                    "ja" or "ja-JP" => "Japanese",
                    var lang when lang.StartsWith("nl") => "Dutch",
                    var lang when lang.StartsWith("it") => "Italian",
                    _ => "English"
                };
            else fileName = "English";
            if (!Directory.Exists(@"TOHE-DATA")) Directory.CreateDirectory(@"TOHE-DATA");
            var defaultTemplateMsg = GetResourcesTxt($"TOHE.Resources.Config.template.{fileName}.txt");
            if (!File.Exists(@"./TOHE-DATA/Default_Teamplate.txt")) //default template
            {
                Logger.Warn("Creating Default_Template.txt", "TemplateManager");
                using FileStream fs = File.Create(@"./TOHE-DATA/Default_Teamplate.txt");
            }
            File.WriteAllText(@"./TOHE-DATA/Default_Teamplate.txt", defaultTemplateMsg); //overwriting default template
            if (!File.Exists(TEMPLATE_FILE_PATH))
            {
                if (File.Exists(@"./template.txt")) File.Move(@"./template.txt", TEMPLATE_FILE_PATH);
                else
                {
                    Logger.Warn($"Creating a new Template file from: {fileName}", "TemplateManager");
                    File.WriteAllText(TEMPLATE_FILE_PATH, defaultTemplateMsg);
                }
            }
            else
            {
                var text = File.ReadAllText(TEMPLATE_FILE_PATH, Encoding.GetEncoding("UTF-8"));
                File.WriteAllText(TEMPLATE_FILE_PATH, text.Replace("5PNwUaN5", "hkk2p9ggv4"));
            }
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "TemplateManager");
        }
    }

    private static string GetResourcesTxt(string path)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
        stream.Position = 0;
        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public static void SendTemplate(string str = "", byte playerId = 0xff, bool noErr = false)
    {
        CreateIfNotExists();
        using StreamReader sr = new(TEMPLATE_FILE_PATH, Encoding.GetEncoding("UTF-8"));
        string text;
        string[] tmp = Array.Empty<string>();
        List<string> sendList = new();
        HashSet<string> tags = new();
        Func<string> playerName = () => "";
        if (playerId != 0xff)
        {
            playerName = () => Main.AllPlayerNames[playerId];   
        }

        if (GameStates.IsNormalGame)
            _replaceDictionaryNormalOptions["PlayerName"] = playerName;
        else if (GameStates.IsHideNSeek)
            _replaceDictionaryHideNSeekOptions["PlayerName"] = playerName;

        while ((text = sr.ReadLine()) != null)
        {
            tmp = text.Split(":");
            if (tmp.Length > 1 && tmp[1] != "")
            {
                tags.Add(tmp[0]);
                if (tmp[0].ToLower() == str.ToLower()) sendList.Add(tmp.Skip(1).Join(delimiter: ":").Replace("\\n", "\n"));
            }
        }
        if (sendList.Count == 0 && !noErr)
        {
            if (playerId == 0xff)
                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, string.Format(GetString("Message.TemplateNotFoundHost"), str, tags.Join(delimiter: ", ")));
            else Utils.SendMessage(string.Format(GetString("Message.TemplateNotFoundClient"), str), playerId);
        }
        else foreach (string x in sendList.ToArray()) Utils.SendMessage(ApplyReplaceDictionary(x), playerId);
    }

    private static string ApplyReplaceDictionary(string text)
    {
        if (GameStates.IsNormalGame)
        {
            foreach (var kvp in _replaceDictionaryNormalOptions)
            {
                text = Regex.Replace(text, "{{" + kvp.Key + "}}", kvp.Value.Invoke() ?? "", RegexOptions.IgnoreCase);
            }
        }
        else if (GameStates.IsHideNSeek)
        {
            foreach (var kvp in _replaceDictionaryHideNSeekOptions)
            {
                text = Regex.Replace(text, "{{" + kvp.Key + "}}", kvp.Value.Invoke() ?? "", RegexOptions.IgnoreCase);
            }
        }
        return text;
    }
}