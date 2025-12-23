using static TOHE.Translator;

namespace TOHE;

public static class SpamManager
{
    //private static readonly string BANEDWORDS_FILE_PATH = "./TOHE-DATA/BanWords.txt";
    //public static List<string> BanWords = new();
    //public static void Init()
    //{
    //    CreateIfNotExists();
    //    BanWords = ReturnAllNewLinesInFile(BANEDWORDS_FILE_PATH);
    //}
    //public static void CreateIfNotExists()
    //{
    //    if (!File.Exists(BANEDWORDS_FILE_PATH))
    //    {
    //        try
    //        {
    //            if (!Directory.Exists(@"TOHE-DATA")) Directory.CreateDirectory(@"TOHE-DATA");
    //            if (File.Exists(@"./BanWords.txt")) File.Move(@"./BanWords.txt", BANEDWORDS_FILE_PATH);
    //            else
    //            {
    //                string fileName;
    //                string[] name = CultureInfo.CurrentCulture.Name.Split("-");
    //                if (name.Count() >= 2)
    //                    fileName = name[0] switch
    //                    {
    //                        "zh" => "SChinese",
    //                        "ru" => "Russian",
    //                        _ => "English"
    //                    };
    //                else fileName = "English";
    //                Logger.Warn($"创建新的 BanWords 文件：{fileName}", "SpamManager");
    //                File.WriteAllText(BANEDWORDS_FILE_PATH, GetResourcesTxt($"TOHE.Resources.Config.BanWords.{fileName}.txt"));
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Logger.Exception(ex, "SpamManager");
    //        }
    //    }
    //}
    //private static string GetResourcesTxt(string path)
    //{
    //    var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
    //    stream.Position = 0;
    //    using StreamReader reader = new(stream, Encoding.UTF8);
    //    return reader.ReadToEnd();
    //}
    //public static List<string> ReturnAllNewLinesInFile(string filename)
    //{
    //    if (!File.Exists(filename)) return new List<string>();
    //    using StreamReader sr = new(filename, Encoding.GetEncoding("UTF-8"));
    //    string text;
    //    List<string> sendList = new();
    //    while ((text = sr.ReadLine()) != null)
    //        if (text.Length > 1 && text != "") sendList.Add(text.Replace("\\n", "\n").ToLower());
    //    return sendList;
    //}
    public static bool CheckSpam(PlayerControl player, string text)
    {
        if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId) return false;
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

        //bool banned = BanWords.Any(text.Contains);

        //if (!banned) return false;

        //if (Options.AutoWarnStopWords.GetBool()) msg = string.Format(GetString("Message.WarnWhoSayBanWord"), name);
        //if (Options.AutoKickStopWords.GetBool())
        //{
        //    if (!Main.SayBanwordsTimes.ContainsKey(player.GetClientId())) Main.SayBanwordsTimes.Add(player.GetClientId(), 0);
        //    Main.SayBanwordsTimes[player.GetClientId()]++;
        //    msg = string.Format(GetString("Message.WarnWhoSayBanWordTimes"), name, Main.SayBanwordsTimes[player.GetClientId()]);
        //    if (Main.SayBanwordsTimes[player.GetClientId()] > Options.AutoKickStopWordsTimes.GetInt())
        //    {
        //        msg = string.Format(GetString("Message.KickWhoSayBanWordAfterWarn"), name, Main.SayBanwordsTimes[player.GetClientId()]);
        //        kick = true;
        //    }
        //}

        if (msg != "")
        {
            if (kick || !GameStates.IsInGame) Utils.SendMessage(msg);
            else
            {
                foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.IsAlive() == player.IsAlive()).ToArray())
                    Utils.SendMessage(msg, pc.PlayerId);
            }
        }
        if (kick) AmongUsClient.Instance.KickPlayer(player.GetClientId(), Options.AutoKickStartAsBan.GetBool());
        return true;
    }
    private static bool ContainsStart(string text)
    {
        text = text.Trim().ToLower();

        var stNum = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i..].Equals("k")) stNum++;
            if (text[i..].Equals("开")) stNum++;
        }

        if (stNum >= 3) return true;

        switch (text)
        {
            case "Start":
            case "start":
            case "/Start":
            case "/Start/":
            case "Start/":
            case "/start":
            case "/start/":
            case "start/":
            case "plsstart":
            case "pls start":
            case "please start":
            case "pleasestart":
            case "Plsstart":
            case "Pls start":
            case "Please start":
            case "Pleasestart":
            case "plsStart":
            case "pls Start":
            case "please Start":
            case "pleaseStart":
            case "PlsStart":
            case "Pls Start":
            case "Please Start":
            case "PleaseStart":
            case "sTart":
            case "stArt":
            case "staRt":
            case "starT":
            case "s t a r t":
            case "S t a r t":
            case "started":
            case "Started":
            case "s t a r t e d":
            case "S t a r t e d":
            case "Го":
            case "гО":
            case "го":
            case "Гоу":
            case "гоу":
            case "Старт":
            case "старт":
            case "/Старт":
            case "/Старт/":
            case "Старт/":
            case "/старт":
            case "/старт/":
            case "старт/":
            case "пжстарт":
            case "пж старт":
            case "пжСтарт":
            case "пж Старт":
            case "Пжстарт":
            case "Пж старт":
            case "ПжСтарт":
            case "Пж Старт":
            case "сТарт":
            case "стАрт":
            case "стаРт":
            case "старТ":
            case "с т а р т":
            case "С т а р т":
            case "начни":
            case "Начни":
            case "начинай":
            case "начинай уже":
            case "Начинай":
            case "Начинай уже":
            case "Начинай Уже":
            case "н а ч и н а й":
            case "Н а ч и н а й":
            case "пж го":
            case "пжго":
            case "Пж Го":
            case "Пж го":
            case "пж Го":
            case "ПжГо":
            case "Пжго":
            case "пжГо":
            case "ГоПж":
            case "гоПж":
            case "Гопж":
            case "开":
            case "快开":
            case "开始":
            case "开啊":
            case "开阿":
            case "kai":
            case "kaishi":
                return true;
        }

        if (text.Length > 30) return false;

        if (text.Contains("start")) return true;
        if (text.Contains("Start")) return true;
        if (text.Contains("STart")) return true;
        if (text.Contains("s t a r t")) return true;
        if (text.Contains("begin")) return true;
        if (text.Contains('了')) return false;
        if (text.Contains('没')) return false;
        if (text.Contains('吗')) return false;
        if (text.Contains('哈')) return false;
        if (text.Contains('还')) return false;
        if (text.Contains('现')) return false;
        if (text.Contains('不')) return false;
        if (text.Contains('可')) return false;
        if (text.Contains('刚')) return false;
        if (text.Contains('的')) return false;
        if (text.Contains('打')) return false;
        if (text.Contains('门')) return false;
        if (text.Contains('关')) return false;
        if (text.Contains('怎')) return false;
        if (text.Contains('要')) return false;
        if (text.Contains('摆')) return false;
        if (text.Contains('啦')) return false;
        if (text.Contains('咯')) return false;
        if (text.Contains('嘞')) return false;
        if (text.Contains('勒')) return false;
        if (text.Contains('心')) return false;
        if (text.Contains('呢')) return false;
        if (text.Contains('门')) return false;
        if (text.Contains('总')) return false;
        if (text.Contains('哥')) return false;
        if (text.Contains('姐')) return false;
        if (text.Contains('《')) return false;
        if (text.Contains('?')) return false;
        if (text.Contains('？')) return false;
        return text.Contains('开') /*|| text.Contains("kai")*/;
    }
}
