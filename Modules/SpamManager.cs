using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using static TOHE.Translator;

namespace TOHE;

public static class SpamManager
{
    private static readonly string BANEDWORDS_FILE_PATH = "./TOHE-DATA/BanWords.txt";
    public static List<string> BanWords = new();
    public static void Init()
    {
        CreateIfNotExists();
        BanWords = ReturnAllNewLinesInFile(BANEDWORDS_FILE_PATH);
    }
    public static void CreateIfNotExists()
    {
        if (!File.Exists(BANEDWORDS_FILE_PATH))
        {
            try
            {
                if (!Directory.Exists(@"TOHE-DATA")) Directory.CreateDirectory(@"TOHE-DATA");
                if (File.Exists(@"./BanWords.txt")) File.Move(@"./BanWords.txt", BANEDWORDS_FILE_PATH);
                else
                {
                    string fileName;
                    string[] name = CultureInfo.CurrentCulture.Name.Split("-");
                    if (name.Count() >= 2)
                        fileName = name[0] switch
                        {
                            "zh-Hans" or "zh" or "zh-CN" or "zn-SG" => "SChinese",
                            "zh-Hant" or "zh-HK" or "zh-MO" or "zh-TW" => "TChinese",
                            "ru" => "Russian",
                            _ => "English"
                        };
                    else fileName = "English";
                    Logger.Warn($"创建新的 BanWords 文件：{fileName}", "SpamManager");
                    File.WriteAllText(BANEDWORDS_FILE_PATH, GetResourcesTxt($"TOHE.Resources.Config.BanWords.{fileName}.txt"));
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "SpamManager");
            }
        }
    }
    private static string GetResourcesTxt(string path)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
        stream.Position = 0;
        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
    public static List<string> ReturnAllNewLinesInFile(string filename)
    {
        if (!File.Exists(filename)) return new List<string>();
        using StreamReader sr = new(filename, Encoding.GetEncoding("UTF-8"));
        string text;
        List<string> sendList = new();
        while ((text = sr.ReadLine()) != null)
            if (text.Length > 1 && text != "") sendList.Add(text.Replace("\\n", "\n").ToLower());
        return sendList;
    }
    public static bool CheckSpam(PlayerControl player, string text)
    {
        if (player.AmOwner || !AmongUsClient.Instance.AmHost) return false;
        string name = player.GetRealName();
        bool kick = false;
        string msg = "";

        if (Options.AutoKickStart.GetBool())
        {
            if (ContainsStart(text) && GameStates.IsLobby)
            {
                msg = string.Format(GetString("Message.KickWhoSayStart"), name);
                if (Options.AutoKickStart.GetBool())
                {
                    if (!Main.SayStartTimes.ContainsKey(player.GetClientId())) Main.SayStartTimes.Add(player.GetClientId(), 0);
                    Main.SayStartTimes[player.GetClientId()]++;
                    msg = string.Format(GetString("Message.WarnWhoSayStart"), name, Main.SayStartTimes[player.GetClientId()]);
                    if (Main.SayStartTimes[player.GetClientId()] > Options.AutoKickStartTimes.GetInt())
                    {
                        msg = string.Format(GetString("Message.KickStartAfterWarn"), name, Main.SayStartTimes[player.GetClientId()]);
                        kick = true;
                    }
                }
                if (msg != "") Utils.SendMessage(msg);
                if (kick) AmongUsClient.Instance.KickPlayer(player.GetClientId(), Options.AutoKickStartAsBan.GetBool());
                return true;
            }
        }

        bool banned = BanWords.Any(text.Contains);

        if (!banned) return false;

        if (Options.AutoWarnStopWords.GetBool()) msg = string.Format(GetString("Message.WarnWhoSayBanWord"), name);
        if (Options.AutoKickStopWords.GetBool())
        {
            if (!Main.SayBanwordsTimes.ContainsKey(player.GetClientId())) Main.SayBanwordsTimes.Add(player.GetClientId(), 0);
            Main.SayBanwordsTimes[player.GetClientId()]++;
            msg = string.Format(GetString("Message.WarnWhoSayBanWordTimes"), name, Main.SayBanwordsTimes[player.GetClientId()]);
            if (Main.SayBanwordsTimes[player.GetClientId()] > Options.AutoKickStopWordsTimes.GetInt())
            {
                msg = string.Format(GetString("Message.KickWhoSayBanWordAfterWarn"), name, Main.SayBanwordsTimes[player.GetClientId()]);
                kick = true;
            }
        }

        if (msg != "")
        {
            if (kick || !GameStates.IsInGame) Utils.SendMessage(msg);
            else
            {
                foreach (var pc in Main.AllPlayerControls.Where(x => x.IsAlive() == player.IsAlive()))
                    Utils.SendMessage(msg, pc.PlayerId);
            }
        }
        if (kick) AmongUsClient.Instance.KickPlayer(player.GetClientId(), Options.AutoKickStartAsBan.GetBool());
        return true;
    }
    private static bool ContainsStart(string text)
    {
        text = text.Trim().ToLower();

        int stNum = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i..].Equals("k")) stNum++;
            if (text[i..].Equals("开")) stNum++;
        }
        if (stNum >= 3) return true;

        if (text == "Start") return true;
        if (text == "start") return true;
        if (text == "/Start") return true;
        if (text == "/Start/") return true;
        if (text == "Start/") return true;
        if (text == "/start") return true;
        if (text == "/start/") return true;
        if (text == "start/") return true;

        if (text == "plsstart") return true;
        if (text == "pls start") return true;
        if (text == "please start") return true;
        if (text == "pleasestart") return true;

        if (text == "Plsstart") return true;
        if (text == "Pls start") return true;
        if (text == "Please start") return true;
        if (text == "Pleasestart") return true;

        if (text == "plsStart") return true;
        if (text == "pls Start") return true;
        if (text == "please Start") return true;
        if (text == "pleaseStart") return true;

        if (text == "PlsStart") return true;
        if (text == "Pls Start") return true;
        if (text == "Please Start") return true;
        if (text == "PleaseStart") return true;

        if (text == "sTart") return true;
        if (text == "stArt") return true;
        if (text == "staRt") return true;
        if (text == "starT") return true;

        if (text == "s t a r t") return true;
        if (text == "S t a r t") return true;

        if (text == "started") return true;
        if (text == "Started") return true;
        if (text == "s t a r t e d") return true;
        if (text == "S t a r t e d") return true;

        if (text == "Começa") return true;
        if (text == "começa") return true;
        if (text == "/Começar") return true;
        if (text == "/Começar/") return true;
        if (text == "Começar/") return true;
        if (text == "/começar") return true;
        if (text == "/começar/") return true;
        if (text == "começar/") return true;

        if (text == "começa por favor") return true;
        if (text == "COMEÇA POR FAVOR") return true;
        if (text == "cmc pfv") return true;
        if (text == "CMC PFV") return true;

        if (text == "cmc logo") return true;
        if (text == "CMC LOGO") return true;
        if (text == "começa logo") return true;
        if (text == "COMEÇA LOGO") return true;

        if (text == "CMC MANO") return true;
        if (text == "CMC MN") return true;
        if (text == "cmc mano") return true;
        if (text == "cmc mn") return true;

        if (text == "Iniciar") return true;
        if (text == "Iniciar Por Favor") return true;
        if (text == "iniciar por favor") return true;
        if (text == "iNiciar") return true;

        if (text == "inIciar") return true;
        if (text == "iniCiar") return true;
        if (text == "iniciAr") return true;

        if (text == "C O M E Ç A") return true;
        if (text == "I N I C I A R") return true;
        
        if (text == "Го") return true;
        if (text == "гО") return true;
        if (text == "го") return true;
        if (text == "Гоу") return true;
        if (text == "гоу") return true;
        if (text == "Старт") return true;
        if (text == "старт") return true;
        if (text == "/Старт") return true;
        if (text == "/Старт/") return true;
        if (text == "Старт/") return true;
        if (text == "/старт") return true;
        if (text == "/старт/") return true;
        if (text == "старт/") return true;

        if (text == "пжстарт") return true;
        if (text == "пж старт") return true;
        if (text == "пжСтарт") return true;
        if (text == "пж Старт") return true;
        if (text == "Пжстарт") return true;
        if (text == "Пж старт") return true;
        if (text == "ПжСтарт") return true;
        if (text == "Пж Старт") return true;

        if (text == "сТарт") return true;
        if (text == "стАрт") return true;
        if (text == "стаРт") return true;
        if (text == "старТ") return true;

        if (text == "с т а р т") return true;
        if (text == "С т а р т") return true;

        if (text == "начни") return true;
        if (text == "Начни") return true;
        if (text == "начинай") return true;
        if (text == "начинай уже") return true;
        if (text == "Начинай") return true;
        if (text == "Начинай уже") return true;
        if (text == "Начинай Уже") return true;
        if (text == "н а ч и н а й") return true;
        if (text == "Н а ч и н а й") return true;

        if (text == "пж го") return true;
        if (text == "пжго") return true;
        if (text == "Пж Го") return true;
        if (text == "Пж го") return true;
        if (text == "пж Го") return true;
        if (text == "ПжГо") return true;
        if (text == "Пжго") return true;
        if (text == "пжГо") return true;
        if (text == "ГоПж") return true;
        if (text == "гоПж") return true;
        if (text == "Гопж") return true;

        if (text == "开") return true;
        if (text == "快开") return true;
        if (text == "开始") return true;
        if (text == "开啊") return true;
        if (text == "开阿") return true;
        if (text == "kai") return true;
        if (text == "kaishi") return true;

        if (text.Length >= 3) return false;

        if (text.Contains("start")) return true;
        if (text.Contains("s t a r t")) return true;
        if (text.Contains("begin")) return true;
        return text.Contains("开") || text.Contains("kai");
    }
}
