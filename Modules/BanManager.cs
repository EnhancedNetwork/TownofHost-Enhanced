using HarmonyLib;
using InnerNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static TOHE.Translator;

namespace TOHE;

public static class BanManager
{
    private static readonly string DENY_NAME_LIST_PATH = @"./TOHE-DATA/DenyName.txt";
    private static readonly string BAN_LIST_PATH = @"./TOHE-DATA/BanList.txt";
    private static readonly string MODERATOR_LIST_PATH = @"./TOHE-DATA/Moderators.txt";
    private static readonly string VIP_LIST_PATH = @"./TOHE-DATA/VIP-List.txt";
    private static readonly string WHITE_LIST_LIST_PATH = @"./TOHE-DATA/WhiteList.txt";
    private static List<string> EACList = new(); // Don't make it read-only
    public static List<string> TempBanWhiteList = new(); //To prevent writing to ban list
    public static void Init()
    {
        try
        {
            Directory.CreateDirectory("TOHE-DATA");

            if (!File.Exists(BAN_LIST_PATH))
            {
                Logger.Warn("Create a new BanList.txt file", "BanManager");
                File.Create(BAN_LIST_PATH).Close();
            }
            if (!File.Exists(DENY_NAME_LIST_PATH))
            {
                Logger.Warn("Create a new DenyName.txt file", "BanManager");
                File.Create(DENY_NAME_LIST_PATH).Close();
                File.WriteAllText(DENY_NAME_LIST_PATH, GetResourcesTxt("TOHE.Resources.Config.DenyName.txt"));
            }
            if (!File.Exists(MODERATOR_LIST_PATH))
            {
                Logger.Warn("Creating a new Moderators.txt file", "BanManager");
                File.Create(MODERATOR_LIST_PATH).Close();
            }
            if (!File.Exists(VIP_LIST_PATH))
            {
                Logger.Warn("Creating a new VIP-List.txt file", "BanManager");
                File.Create(VIP_LIST_PATH).Close();
            }
            if (!File.Exists(WHITE_LIST_LIST_PATH))
            {
                Logger.Warn("Creating a new WhiteList.txt file", "BanManager");
                File.Create(WHITE_LIST_LIST_PATH).Close();
            }

            // Read EAC List
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TOHE.Resources.Config.EACList.txt");
            stream.Position = 0;
            using StreamReader sr = new(stream, Encoding.UTF8);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line == "" || line.StartsWith("#")) continue;
                EACList.Add(line);
            }

        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "BanManager");
        }
    }
    private static string GetResourcesTxt(string path)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
        stream.Position = 0;
        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
    public static string GetHashedPuid(this ClientData player)
    {
        if (player == null) return "";
        string puid = player.ProductUserId;
        using (SHA256 sha256 = SHA256.Create())
        using (MD5 md5 = MD5.Create())
        {
            // get sha-256 hash
            byte[] sha256Bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(puid));
            string sha256Hash = BitConverter.ToString(sha256Bytes).Replace("-", "").ToLower();

            // pick front 5 and last 4
            return string.Concat(sha256Hash.AsSpan(0, 5), sha256Hash.AsSpan(sha256Hash.Length - 4));
        }
    }
    public static void AddBanPlayer(InnerNet.ClientData player)
    {
        if (!AmongUsClient.Instance.AmHost || player == null) return;
        if (!CheckBanList(player?.FriendCode, player?.GetHashedPuid()) && !TempBanWhiteList.Contains(player?.GetHashedPuid()))
        {
            if (player?.GetHashedPuid() != "" && player?.GetHashedPuid() != null && player?.GetHashedPuid() != "e3b0cb855")
            {
                File.AppendAllText(BAN_LIST_PATH, $"{player.FriendCode},{player?.GetHashedPuid()},{player.PlayerName.RemoveHtmlTags()}\n");
                Logger.SendInGame(string.Format(GetString("Message.AddedPlayerToBanList"), player.PlayerName));
            }
            else Logger.Info($"Failed to add player {player?.PlayerName.RemoveHtmlTags()}/{player?.FriendCode}/{player?.GetHashedPuid()} to ban list!", "AddBanPlayer");
        }
    }
    
    public static void CheckDenyNamePlayer(InnerNet.ClientData player)
    {
        if (!AmongUsClient.Instance.AmHost || !Options.ApplyDenyNameList.GetBool()) return;
        try
        {
            Directory.CreateDirectory("TOHE-DATA");
            if (!File.Exists(DENY_NAME_LIST_PATH)) File.Create(DENY_NAME_LIST_PATH).Close();
            using StreamReader sr = new(DENY_NAME_LIST_PATH);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line == "") continue;
                if (line.Contains("Amogus"))
                {
                    AmongUsClient.Instance.KickPlayer(player.Id, false);
                    Logger.SendInGame(string.Format(GetString("Message.KickedByDenyName"), player.PlayerName, line));
                    Logger.Info($"{player.PlayerName}は名前が「{line}」に一致したためキックされました。", "Kick");
                    return;
                }
                if (line.Contains("Amogus V"))
                {
                    AmongUsClient.Instance.KickPlayer(player.Id, false);
                    Logger.SendInGame(string.Format(GetString("Message.KickedByDenyName"), player.PlayerName, line));
                    Logger.Info($"{player.PlayerName}は名前が「{line}」に一致したためキックされました。", "Kick");
                    return;
                }

                if (Regex.IsMatch(player.PlayerName, line))
                {
                    AmongUsClient.Instance.KickPlayer(player.Id, false);
                    Logger.SendInGame(string.Format(GetString("Message.KickedByDenyName"), player.PlayerName, line));
                    Logger.Info($"{player.PlayerName}は名前が「{line}」に一致したためキックされました。", "Kick");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "CheckDenyNamePlayer");
        }
    }
    public static void CheckBanPlayer(InnerNet.ClientData player)
    {
        if (!AmongUsClient.Instance.AmHost || !Options.ApplyBanList.GetBool()) return;
        if (CheckBanList(player?.FriendCode, player?.GetHashedPuid()))
        {
            AmongUsClient.Instance.KickPlayer(player.Id, true);
            Logger.SendInGame(string.Format(GetString("Message.BanedByBanList"), player.PlayerName));
            Logger.Info($"{player.PlayerName}は過去にBAN済みのためBANされました。", "BAN");
            return;
        }
        if (CheckEACList(player?.FriendCode, player?.GetHashedPuid()))
        {
            AmongUsClient.Instance.KickPlayer(player.Id, true);
            Logger.SendInGame(string.Format(GetString("Message.BanedByEACList"), player.PlayerName));
            Logger.Info($"{player.PlayerName}存在于EAC封禁名单", "BAN");
            return;
        }
        if (TempBanWhiteList.Contains(player?.GetHashedPuid()))
        {
            AmongUsClient.Instance.KickPlayer(player.Id, true);
            //This should not happen
            Logger.Info($"{player.PlayerName} was in temp ban list", "BAN");
            return;
        }
    }
    public static bool CheckBanList(string code, string hashedpuid = "")
    {
        bool OnlyCheckPuid = false;
        if (code == "" && hashedpuid != "") OnlyCheckPuid = true;
        else if (code == "") return false;
        try
        {
            Directory.CreateDirectory("TOHE-DATA");
            if (!File.Exists(BAN_LIST_PATH)) File.Create(BAN_LIST_PATH).Close();
            using StreamReader sr = new(BAN_LIST_PATH);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line == "") continue;
                if (!OnlyCheckPuid)
                    if (line.Contains(code)) return true;
                if (line.Contains(hashedpuid)) return true;
            }
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "CheckBanList");
        }
        return false;
    }
    public static bool CheckEACList(string code, string hashedPuid)
    {
        bool OnlyCheckPuid = false;
        if (code == "" && hashedPuid == "") OnlyCheckPuid = true;
        else if (code == "") return false;
        return (EACList.Any(x => x.Contains(code) && !OnlyCheckPuid) || EACList.Any(x => x.Contains(hashedPuid) && hashedPuid != ""));
    }
}
[HarmonyPatch(typeof(BanMenu), nameof(BanMenu.Select))]
class BanMenuSelectPatch
{
    public static void Postfix(BanMenu __instance, int clientId)
    {
        InnerNet.ClientData recentClient = AmongUsClient.Instance.GetRecentClient(clientId);
        if (recentClient == null) return;
        if (!BanManager.CheckBanList(recentClient?.FriendCode, recentClient?.GetHashedPuid())) __instance.BanButton.GetComponent<ButtonRolloverHandler>().SetEnabledColors();
    }
}