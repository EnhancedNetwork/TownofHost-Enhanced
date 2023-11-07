using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TOHE;

public class DevUser
{
    public string Code { get; set; }
    public string Color { get; set; }
    public string Tag { get; set; }
    public bool IsUp { get; set; }
    public bool IsDev { get; set; }
    public bool IsMod { get; set; }
    public bool DeBug { get; set; }
    public bool ColorCmd { get; set; }
    public string UpName { get; set; }
    public DevUser(string code = "", string color = "null", string tag = "null", bool isUp = false, bool isDev = false, bool isMod = false, bool deBug = false, bool colorCmd = false, string upName = "未认证用户")
    {
        Code = code;
        Color = color;
        Tag = tag;
        IsUp = isUp;
        IsDev = isDev;
        IsMod = isMod;
        DeBug = deBug;
        ColorCmd = colorCmd;
        UpName = upName;
    }

    public bool HasTag() => Tag != "null";
    //public string GetTag() => Color == "null" ? $"<size=1.2>{Tag}</size>\r\n" : $"<color={Color}><size=1.2>{(Tag == "#Dev" ? Translator.GetString("Developer") : Tag)}</size></color>\r\n";
    public string GetTag()
    {
        string tagColorFilePath = @$"./TOHE-DATA/Tags/SPONSOR_TAGS/{Code}.txt";

        if (Color == "null" || Color == string.Empty) return $"<size=1.2>{Tag}</size>\r\n";
        var startColor = Color.TrimStart('#');

        if (File.Exists(tagColorFilePath))
        {
            var ColorCode = File.ReadAllText(tagColorFilePath);
            if (Utils.CheckColorHex(ColorCode)) startColor = ColorCode;
        }
        string t1;
        t1 = Tag == "#Dev" ? Translator.GetString("Developer") : Tag;
        return $"<size=1.2><color=#{startColor}>{t1}</color></size>\r\n";
    }
    //public string GetTag() 
    //{
    //    string tagColorFilePath = @$"./TOHE-DATA/Tags/SPONSOR_TAGS/{Code}.txt";

    //    if (Color == "null" || Color == string.Empty) return $"<size=1.2>{Tag}</size>\r\n";
    //    var startColor = "FFFF00";
    //    var endColor = "FFFF00";
    //    var startColor1 = startColor;
    //    var endColor1 = endColor;
    //    if (Color.Split(",").Length == 1)
    //    {
    //        startColor1 = Color.Split(",")[0].TrimStart('#');
    //        endColor1 = startColor1;
    //    }
    //    else if (Color.Split(",").Length == 2)
    //    {
    //         startColor1 = Color.Split(",")[0].TrimStart('#');
    //         endColor1 = Color.Split(",")[1].TrimStart('#');
    //    }
    //    if (File.Exists(tagColorFilePath))
    //    {
    //        var ColorCode = File.ReadAllText(tagColorFilePath);
    //        if (ColorCode.Split(" ").Length == 2)
    //        {
    //            startColor = ColorCode.Split(" ")[0];
    //            endColor = ColorCode.Split(" ")[1];
    //        }
    //        else
    //        {
    //            startColor = startColor1;
    //            endColor = endColor1;
    //        }
    //    }
    //    else
    //    {
    //        startColor = startColor1;
    //        endColor = endColor1;
    //    }
    //    if (!Utils.CheckGradientCode($"{startColor} {endColor}"))
    //    {
    //        startColor = "FFFF00";
    //        endColor = "FFFF00";
    //    }
    //    var t1 = "";
    //    t1 = Tag == "#Dev" ? Translator.GetString("Developer") : Tag;
    //    return $"<size=1.2>{Utils.GradientColorText(startColor,endColor, t1)}</size>\r\n";
    //}
}

public static class DevManager
{
    private readonly static DevUser DefaultDevUser = new();
    private readonly static List<DevUser> DevUserList = new();
    public static void Init()
    {
        // Dev
        DevUserList.Add(new(code: "actorour#0029", color: "#ffc0cb", tag: "Original Developer", isUp: true, isDev: true, deBug: true, colorCmd: true, upName: "KARPED1EM"));
        DevUserList.Add(new(code: "pinklaze#1776", color: "#30548e", tag: "#Dev", isUp: true, isDev: true, deBug: true, colorCmd: false, upName: "NCSIMON"));
        DevUserList.Add(new(code: "keepchirpy#6354", color: "#1FF3C6", tag: "Переводчик", isUp: true, isDev: true, deBug: true, colorCmd: true, upName: "TommyXL")); //Tommy-XL
        DevUserList.Add(new(code: "taskunsold#2701", color: "null", tag: "<color=#426798>Tem</color><color=#f6e509>mie</color>", isUp: false, isDev: true, deBug: false, colorCmd: false, upName: null)); //Tem
        DevUserList.Add(new(code: "timedapper#9496", color: "#48FFFF", tag: "#Dev", isUp: false, isDev: true, deBug: true, colorCmd: false, upName: null)); //阿龍
        DevUserList.Add(new(code: "sofaagile#3120", color: "null", tag: "null", isUp: false, isDev: true, deBug: true, colorCmd: false, upName: null)); //天寸
        DevUserList.Add(new(code: "keyscreech#2151", color: "null", tag: "<color=#D3A4FF>美術</color><color=#5A5AAD>NotKomi</color>", isUp: false, isDev: true, deBug: false, upName: null)); //Endrmen40409
        DevUserList.Add(new(code: "openlanded#9533", color: "#9e2424", tag: "discord.gg/tohe", isUp: true, isDev: true, deBug: true, colorCmd: true, upName: "ryuk"));
        DevUserList.Add(new(code: "icingposh#6469", color: "#9e2424", tag: "discord.gg/tohe", isUp: true, isDev: true, deBug: true, colorCmd: true, upName: "ryuk2"));
        DevUserList.Add(new(code: "straymovie#6453", color: "#F6B05E", tag: "Project Lead", isUp: true, isDev: true, deBug: true, colorCmd: true, upName: "Moe")); //Moe
        DevUserList.Add(new(code: "singlesign#1823", color: "#ffb6cd", tag: "Princess", isUp: true, isDev: true, deBug: true, colorCmd: true, upName: "Lauryn")); //Lauryn

        DevUserList.Add(new(code: "happypride#3747", color: "#00ff1d", tag: "绿色游戏", isUp: true, isDev: true, deBug: true, colorCmd: true, upName: null)); //NikoCat233
        DevUserList.Add(new(code: "bestanswer#3360", color: "#00ff1d", tag: "绿色游戏", isUp: true, isDev: true, deBug: true, colorCmd: true, upName: null)); //NikoCat233's alt

        // pt-BR Translators
        DevUserList.Add(new(code: "modelpad#5195", color: "null", tag: "Tradutor", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Reginaldoo")); // and content creator
        DevUserList.Add(new(code: "lotelfin#1641", color: "null", tag: "Tradutor", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Dopzy")); // and content creator
        DevUserList.Add(new(code: "maltyhoney#2924", color: "null", tag: "Tradutor", isUp: false, isDev: false, deBug: false, colorCmd: false, upName: "TonyStark"));
        DevUserList.Add(new(code: "mimerecord#9638", color: "null", tag: "Tradutor", isUp: false, isDev: false, deBug: false, colorCmd: false, upName: "Arc"));
        // SChinese translation
        DevUserList.Add(new(code: "cloakhazy#9133", color: "#87CEFA", tag: "我是崽子吖awa", isUp: true, isDev: true, deBug: true, colorCmd: true, upName: "LezaiYa")); //乐崽吖
        DevUserList.Add(new(code: "drawncod#3642", color: "#00FFFF", tag: "简中翻译人员", isUp: false, isDev: false, deBug: false, colorCmd: false, upName: "crewcyan")); //船员小青
        DevUserList.Add(new(code: "grubmotive#0072", color: "#4169E1", tag: "跟班诅咒中", isUp: false, isDev: false, deBug: false, colorCmd: false, upName: "ninyouyigehao")); //您有一个好
        //TChinese translation
        DevUserList.Add(new(code: "richgaff#1771", color: "#F76C05", tag: "TChinese Translator", isUp: false, isDev: false, deBug: false, colorCmd: false, upName: null)); //FlyFlyTurtle
        DevUserList.Add(new(code: "eastbutton#6692", color: "#27E878", tag: "繁體中文翻譯", isUp: false, isDev: false, deBug: false, colorCmd: false, upName: null)); //柚子(Pomelo)
        // French Translators
        DevUserList.Add(new(code: "bigecho#5256", color: "#0131b4", tag: "Traducteur", isUp: false, isDev: false, deBug: false, colorCmd: false, upName: "KevOut")); //kevoutings
        DevUserList.Add(new(code: "fivedogged#3140", color: "#0B1FB8", tag: "Translator", isUp: false, isDev: false, deBug: false, colorCmd: false, upName: "Sansationnelle")); //shapmaster
        // Japanese Translators
        DevUserList.Add(new(code: "deepmusic#4504", color: "#FCF4A3", tag: "Translator", isUp: false, isDev: false, deBug: false, colorCmd: false, upName: "Sunnyboi")); //misheru.u
        // Latam Translators
        DevUserList.Add(new(code: "magicyear#5568", color: "#1F75FE", tag: "Traductor", isUp: false, isDev: false, deBug: false, colorCmd: false, upName: "CreepPower")); //creeppower
       // Spanish Translators
        DevUserList.Add(new(code: "swiftlord#0872", color: "#E64236", tag: "Translator", isUp: false, isDev: false, deBug: false, colorCmd: false, upName: "Dawson")); //butwhatabout

        // Youtubers and Dev (BRASIL)
        DevUserList.Add(new(code: "tinedpun#6584", color: "#0000ff", tag: "Desenvolvedor", isUp: true, isDev: true, isMod: false, deBug: true, colorCmd: true, upName: "Dechis"));
        DevUserList.Add(new(code: "fireretro#9325", color: "#0000ff", tag: "Desenvolvedor", isUp: true, isDev: true, isMod: false, deBug: true, colorCmd: true, upName: "Pietro"));
        DevUserList.Add(new(code: "tonalpins#1855", color: "#ffc0cb", tag: "YouTuber", isUp: true, isDev: true, isMod: true, deBug: true, colorCmd: true, upName: "Ícaro Cup"));
        DevUserList.Add(new(code: "calmpall#0468", color: "#ffc0cb", tag: "YouTuber", isUp: true, isDev: true, isMod: true, deBug: true, colorCmd: true, upName: "Joanzinn"));
        DevUserList.Add(new(code: "stoolfoggy#3070", color: "#ffc0cb", tag: "YouTuber", isUp: true, isDev: true, isMod: true, deBug: true, colorCmd: true, upName: "GG6868"));
        DevUserList.Add(new(code: "elatedpity#2202", color: "#ffc0cb", tag: "YouTuber", isUp: true, isDev: true, isMod: true, deBug: true, colorCmd: true, upName: "vtr"));
        DevUserList.Add(new(code: "grassyprey#9593", color: "#ffc0cb", tag: "YouTuber", isUp: true, isDev: true, isMod: true, deBug: true, colorCmd: true, upName: "LucasCarr"));
        DevUserList.Add(new(code: "fluelesser#2433", color: "#ffc0cb", tag: "YouTuber", isUp: true, isDev: true, isMod: true, deBug: true, colorCmd: true, upName: "ofelepe"));
        DevUserList.Add(new(code: "drivesolar#9523", color: "#ffc0cb", tag: "YouTuber", isUp: true, isDev: true, isMod: true, deBug: true, colorCmd: true, upName: "Sariguê 7"));
        DevUserList.Add(new(code: "traderaser#4755", color: "#ffc0cb", tag: "YouTuber", isUp: true, isDev: true, isMod: true, deBug: true, colorCmd: true, upName: "gabrielzin"));
        DevUserList.Add(new(code: "spryvowel#8882", color: "#ffc0cb", tag: "YouTuber", isUp: true, isDev: true, isMod: true, deBug: true, colorCmd: true, upName: "Felx"));
        DevUserList.Add(new(code: "muzzlefawn#7540", color: "#ffc0cb", tag: "YouTuber", isUp: true, isDev: true, isMod: true, deBug: true, colorCmd: true, upName: "CouthMalfy"));
        DevUserList.Add(new(code: "famedfun#8888", color: "#ffc0cb", tag: "YouTuber", isUp: true, isDev: true, isMod: false, deBug: true, colorCmd: true, upName: "PH Gaming"));
        DevUserList.Add(new(code: "stormsame#0836", color: "#ffc0cb", tag: "YouTuber", isUp: true, isDev: true, isMod: true, deBug: true, colorCmd: true, upName: "KaleuCarr"));
        
        // China Up
        DevUserList.Add(new(code: "truantwarm#9165", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "萧暮不姓萧"));
        DevUserList.Add(new(code: "drilldinky#1386", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "爱玩AU的河豚"));
        DevUserList.Add(new(code: "farardour#6818", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "-提米SaMa-"));
        DevUserList.Add(new(code: "vealused#8192", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "lag丶xy"));
        DevUserList.Add(new(code: "storyeager#0815", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "航娜丽莎"));
        DevUserList.Add(new(code: "versegame#3885", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "柴唔cw"));
        DevUserList.Add(new(code: "closegrub#6217", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "警长不会玩"));
        DevUserList.Add(new(code: "frownnatty#7935", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "鬼灵official"));
        DevUserList.Add(new(code: "veryscarf#5368", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "小武同学102"));
        DevUserList.Add(new(code: "sparklybee#0275", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "--红包SaMa--"));
        DevUserList.Add(new(code: "endingyon#3175", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "游侠开摆"));
        DevUserList.Add(new(code: "firmine#0232", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "YH永恒_"));
        DevUserList.Add(new(code: "storkfey#3570", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Calypso"));
        DevUserList.Add(new(code: "fellowsand#1003", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "C-Faust"));
        DevUserList.Add(new(code: "jetsafe#8512", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Hoream是好人"));
        DevUserList.Add(new(code: "primether#5348", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "AnonWorks"));
        DevUserList.Add(new(code: "spoonkey#0792", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "没好康的"));
        DevUserList.Add(new(code: "busethical#4134", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "茄-au"));
        DevUserList.Add(new(code: "doggedsize#7892", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "TronAndRey"));
        DevUserList.Add(new(code: "lotelfin#1641", color: "null", tag: "null", isUp: true, isDev: false, colorCmd: false, deBug: false, upName: "Dopzy"));
        DevUserList.Add(new(code: "marlymoor#2246", color: "null", tag: "null", isUp: true, isDev: false, colorCmd: false, deBug: false, upName: "落Yan"));

        DevUserList.Add(new(code: "unlikecity#4086", color: "#eD2F91", tag: "Ward", isUp: true, isDev: false, deBug: false, colorCmd: true, upName: "Ward"));
        DevUserList.Add(new(code: "iconicdrop#2727", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: true, upName: "jackler"));

        DevUserList.Add(new(code: "neatnet#5851", color: "#FFFF00", tag: "The 200IQ guy", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "The 200IQ guy"));
        DevUserList.Add(new(code: "contenthue#0404", color: "#FFFF00", tag: "The 200IQ guy", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "The 200IQ guy"));
        DevUserList.Add(new(code: "heavyclod#2286", color: "#FFFF00", tag: "小叨.exe已停止运行", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "小叨院长"));
        DevUserList.Add(new(code: "storeroan#0331", color: "#FF0066", tag: "Night_瓜", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Night_瓜"));
        DevUserList.Add(new(code: "teamelder#5856", color: "#1379bf", tag: "屑Slok（没信誉的鸽子）", isUp: true, isDev: false, colorCmd: false, deBug: false, upName: "Slok7565"));
        DevUserList.Add(new(code: "freepit#9942", color: "#00FFFF", tag: "古明地白糖", isUp: true, isDev: false, colorCmd: false, deBug: false, upName: "古明地白糖"));
        DevUserList.Add(new(code: "radarright#2509", color: "null", tag: "null", isUp: false, isDev: false, deBug: true, colorCmd: false, upName: null));

        // Sponsor
        DevUserList.Add(new(code: "recentduct#6068", color: "#FF00FF", tag: "高冷男模法师", isUp: false, isDev: false, colorCmd: false, deBug: true, upName: null));
        DevUserList.Add(new(code: "canneddrum#2370", color: "#fffcbe", tag: "我是喜唉awa", isUp: false, isDev: false, colorCmd: false, deBug: false, upName: null));
        DevUserList.Add(new(code: "dovefitted#5329", color: "#1379bf", tag: "不要首刀我", isUp: false, isDev: false, colorCmd: false, deBug: false, upName: null));
        DevUserList.Add(new(code: "luckylogo#7352", color: "#f30000", tag: "林@林", isUp: false, isDev: false, colorCmd: false, deBug: false, upName: null));
        DevUserList.Add(new(code: "axefitful#8788", color: "#8e8171", tag: "寄才是真理", isUp: false, isDev: false, colorCmd: false, deBug: false, upName: null));
        DevUserList.Add(new(code: "raftzonal#8893", color: "#8e8171", tag: "寄才是真理", isUp: false, isDev: false, colorCmd: false, deBug: false, upName: null));
        DevUserList.Add(new(code: "twainrobin#8089", color: "#0000FF", tag: "啊哈修maker", isUp: false, isDev: false, colorCmd: false, deBug: false, upName: null));
        DevUserList.Add(new(code: "mallcasual#6075", color: "#f89ccb", tag: "波奇酱", isUp: false, isDev: false, colorCmd: false, deBug: false, upName: null));
        DevUserList.Add(new(code: "beamelfin#9478", color: "#6495ED", tag: "Amaster-1111", isUp: false, isDev: false, colorCmd: false, deBug: false, upName: null));
        DevUserList.Add(new(code: "lordcosy#8966", color: "#FFD6EC", tag: "HostTOHE", isUp: false, isDev: false, colorCmd: false, deBug: false, upName: null)); //K
        DevUserList.Add(new(code: "honestsofa#2870", color: "#D381D9", tag: "Discord: SolarFlare#0700", isUp: true, isDev: false, colorCmd: false, deBug: false, upName: "SolarFlare")); //SolarFlare
        
        // Patreons
        DevUserList.Add(new(code: "firmshame#7569", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Yankee"));
        DevUserList.Add(new(code: "ghostapt#7243", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "MasterKy"));
        DevUserList.Add(new(code: "moonmodest#5153", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Allie"));
        DevUserList.Add(new(code: "woolrusty#4204", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "jo"));
        DevUserList.Add(new(code: "funnyshe#2647", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Stabby"));
        DevUserList.Add(new(code: "fluffycord#2605", color: "#ff00ff", tag: "discord.gg/maul", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "sarhadactyl"));
        DevUserList.Add(new(code: "cannylink#0564", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "SpicyPoops"));
        DevUserList.Add(new(code: "examfishy#9080", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "killer5362"));
        DevUserList.Add(new(code: "dusksole#6956", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Bandz"));
        DevUserList.Add(new(code: "wontsave#5153", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "wayne"));
        DevUserList.Add(new(code: "sightcalm#1943", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Scarlet")); //lavender_loves
        DevUserList.Add(new(code: "cangirlish#9017", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Matt")); //mattbr
        DevUserList.Add(new(code: "strongpurr#1431", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "fieryflower")); //fieryflower
        DevUserList.Add(new(code: "kindlyplum#7250", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "loom")); //loom.nz
        DevUserList.Add(new(code: "goldenoil#1511", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "clayford")); //clayfordg
        DevUserList.Add(new(code: "goldstubby#6891", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "cuse")); //cuse85
        DevUserList.Add(new(code: "occultdisc#1148", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "bran")); //reapxr76
        DevUserList.Add(new(code: "puddletwin#4866", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "hax")); //haxdoo
        DevUserList.Add(new(code: "sleepyrose#3739", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Sociopath")); //sanefield
        DevUserList.Add(new(code: "sensualpit#1329", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "doll")); //babycult
        DevUserList.Add(new(code: "lovelycat#8421", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "jamiek197")); //jamiek197
        DevUserList.Add(new(code: "thenfrozen#1719", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "misstoh")); //misstoh
        DevUserList.Add(new(code: "rarespite#3723", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "buddah2400")); //buddah2400
        DevUserList.Add(new(code: "famousdove#2275", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "mina")); //mina_xo
        DevUserList.Add(new(code: "alertfive#4882", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "EnemyFighter")); //misterenemyfighter
        DevUserList.Add(new(code: "primebulb#9031", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "CultAnxi")); //CultAnxi
        DevUserList.Add(new(code: "peakbass#6507", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Vulcan")); //.vulcan.is.a.star.
        // DevUserList.Add(new(code: "rollingegg#7687", color: "#fe7d6e", tag: "Ruler of Jiggly Peach Cakes", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "DarlingXX"));
        DevUserList.Add(new(code: "riskyhunt#8928", color: "#DC143C", tag: "I♥PANDAS", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Panda"));
    }
    public static bool IsDevUser(this string code) => DevUserList.Any(x => x.Code == code);
    public static DevUser GetDevUser(this string code) => code.IsDevUser() ? DevUserList.Find(x => x.Code == code) : DefaultDevUser;
}
