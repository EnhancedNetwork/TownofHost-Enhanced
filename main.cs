using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TOHE.Roles.Neutral;
using UnityEngine;

[assembly: AssemblyFileVersion(TOHE.Main.PluginVersion)]
[assembly: AssemblyInformationalVersion(TOHE.Main.PluginVersion)]
[assembly: AssemblyVersion(TOHE.Main.PluginVersion)]
namespace TOHE;

[BepInPlugin(PluginGuid, "TOHE", PluginVersion)]
[BepInIncompatibility("jp.ykundesu.supernewroles")]
[BepInProcess("Among Us.exe")]
public class Main : BasePlugin
{
    // == プログラム設定 / Program Config ==
    public const string OriginalForkId = "OriginalTOH";

    public static readonly string ModName = "TOHE";
    public static readonly string ForkId = "TOHE";
    public static readonly string ModColor = "#ffc0cb";
    public static readonly bool AllowPublicRoom = true;

    public static HashAuth DebugKeyAuth { get; private set; }
    public const string DebugKeyHash = "c0fd562955ba56af3ae20d7ec9e64c664f0facecef4b3e366e109306adeae29d";
    public const string DebugKeySalt = "59687b";

    public static ConfigEntry<string> DebugKeyInput { get; private set; }
    public static readonly string MainMenuText = " ";

    public const string PluginGuid = "com.0xdrmoe.townofhostenhanced";
    public const string PluginVersion = "1.2.0";
    public const string PluginDisplayVersion = "1.2.0 dev 8";
    public static readonly string SupportedVersionAU = "2023.10.24";
    public const bool Canary = false; // Unused variable?

    public static readonly bool ShowGitHubButton = true;
    public static readonly bool ShowKofiButton = true;
    public static readonly string GitHubInviteUrl = "https://github.com/0xDrMoe/TownofHost-Enhanced";
    public static readonly string kofiInviteUrl = "https://ko-fi.com/TOHEN";

    public static readonly bool ShowDiscordButton = true;
    public static readonly string DiscordInviteUrl = "https://discord.gg/tohe";

    public static readonly bool ShowWebsiteButton = true;
    public static readonly string WebsiteInviteUrl = "https://tohre.dev";

    public static readonly bool ShowPatreonButton = true;
    public static readonly string PatreonInviteUrl = "https://www.patreon.com/TOHRE";

    public Harmony Harmony { get; } = new Harmony(PluginGuid);
    public static Version version = Version.Parse(PluginVersion);
    public static BepInEx.Logging.ManualLogSource Logger;
    public static bool hasArgumentException = false;
    public static string ExceptionMessage;
    public static bool ExceptionMessageIsShown = false;
    public static bool AlreadyShowMsgBox = false;
    public static string credentialsText;
    public static NormalGameOptionsV07 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;
    //Client Options
    public static ConfigEntry<string> HideName { get; private set; }
    public static ConfigEntry<string> HideColor { get; private set; }
    public static ConfigEntry<int> MessageWait { get; private set; }
    public static ConfigEntry<bool> UnlockFPS { get; private set; }
    public static ConfigEntry<bool> ShowFPS { get; private set; }
    public static ConfigEntry<bool> AutoMuteUs { get; private set; }
    public static ConfigEntry<bool> EnableGM { get; private set; }
    public static ConfigEntry<bool> AutoStart { get; private set; }
    public static ConfigEntry<bool> ForceOwnLanguage { get; private set; }
    public static ConfigEntry<bool> ForceOwnLanguageRoleName { get; private set; }
    public static ConfigEntry<bool> EnableCustomButton { get; private set; }
    public static ConfigEntry<bool> EnableCustomSoundEffect { get; private set; }
    public static ConfigEntry<bool> ShowTextOverlay { get; private set; }
    public static ConfigEntry<bool> ModeForSmallScreen { get; private set; }
    public static ConfigEntry<bool> EnableRoleSummary { get; private set; }
    public static ConfigEntry<bool> SwitchVanilla { get; private set; }
    public static ConfigEntry<bool> VersionCheat { get; private set; }
    public static ConfigEntry<bool> GodMode { get; private set; }

    public static Dictionary<byte, PlayerVersion> playerVersion = new();
    //Preset Name Options
    public static ConfigEntry<string> Preset1 { get; private set; }
    public static ConfigEntry<string> Preset2 { get; private set; }
    public static ConfigEntry<string> Preset3 { get; private set; }
    public static ConfigEntry<string> Preset4 { get; private set; }
    public static ConfigEntry<string> Preset5 { get; private set; }
    //Other Configs
    public static ConfigEntry<string> WebhookURL { get; private set; }
    public static ConfigEntry<string> BetaBuildURL { get; private set; }
    public static ConfigEntry<float> LastKillCooldown { get; private set; }
    public static ConfigEntry<float> LastShapeshifterCooldown { get; private set; }
    public static OptionBackupData RealOptionsData;
    public static Dictionary<byte, PlayerState> PlayerStates = new();
    public static Dictionary<byte, string> AllPlayerNames = new();
    public static Dictionary<byte, CustomRoles> AllPlayerCustomRoles;
    public static Dictionary<(byte, byte), string> LastNotifyNames;
    public static Dictionary<byte, Color32> PlayerColors = new();
    public static Dictionary<byte, PlayerState.DeathReason> AfterMeetingDeathPlayers = new();
    public static Dictionary<CustomRoles, string> roleColors;
    public static bool IsFixedCooldown => CustomRoles.Vampire.IsEnable() || CustomRoles.Poisoner.IsEnable() || CustomRoles.Vampiress.IsEnable();
    public static float RefixCooldownDelay = 0f;
    public static GameData.PlayerInfo LastVotedPlayerInfo;
    public static string LastVotedPlayer;
    public static List<byte> ResetCamPlayerList = new();
    public static List<byte> winnerList = new();
    public static List<byte> ForCrusade = new();
    public static List<byte> KillGhoul = new();
    public static List<string> winnerNameList = new();
    public static List<int> clientIdList = new();
    public static List<(string, byte, string)> MessagesToSend = new();
    public static bool isChatCommand = false;
    public static List<PlayerControl> LoversPlayers = new();
    public static bool isLoversDead = true;
    public static Dictionary<byte, float> AllPlayerKillCooldown = new();
    public static Dictionary<byte, float> EvilMiniKillcooldown = new();
    public static Dictionary<byte, long> NiceMiniTime = new();
    public static float EvilMiniKillcooldownf = new();
    public static Dictionary<byte, Vent> LastEnteredVent = new();
    public static Dictionary<byte, Vector2> LastEnteredVentLocation = new();
    public static Dictionary<byte, Vector2> TimeMasterBackTrack = new();
    public static Dictionary<byte, int> MasochistKillMax = new();
    public static Dictionary<byte, int> BerserkerKillMax = new();
    public static Dictionary<byte, int> TimeMasterNum = new();
    public static Dictionary<byte, long> TimeMasterInProtect = new();
    //public static Dictionary<byte, long> FlashbangInProtect = new();
    public static List<byte> CyberStarDead = new();
    public static List<byte> CyberDead = new();
    public static List<int> BombedVents = new();
    public static List<byte> WorkaholicAlive = new();
    public static List<byte> BurstBodies = new();
    public static List<byte> BaitAlive = new();
    public static List<byte> TasklessCrewmate = new();
    public static List<byte> BoobyTrapBody = new();
    public static List<byte> BoobyTrapKiller = new();
    //public static List<byte> KilledDiseased = new();
    public static Dictionary<byte, int> KilledDiseased = new();
    public static Dictionary<byte, int> KilledAntidote = new();
    //public static List<byte> ForFlashbang = new();
    public static Dictionary<byte, byte> KillerOfBoobyTrapBody = new();
    public static Dictionary<byte, string> DetectiveNotify = new();
    public static Dictionary<byte, string> SleuthNotify = new();
    public static Dictionary<byte, string> VirusNotify = new();
    public static List<byte> OverDeadPlayerList = new();
    public static bool DoBlockNameChange = false;
    public static int updateTime;
    public static bool newLobby = false;
    public static Dictionary<int, int> SayStartTimes = new();
    public static Dictionary<int, int> SayBanwordsTimes = new();
    public static Dictionary<byte, float> AllPlayerSpeed = new();
    public const float MinSpeed = 0.0001f;
    public static List<byte> CleanerBodies = new();
    public static List<byte> MedusaBodies = new();
    public static List<byte> InfectedBodies = new();
    public static List<byte> BrakarVoteFor = new();
    public static Dictionary<byte, (byte, float)> BitPlayers = new();
    public static Dictionary<byte, float> WarlockTimer = new();
    public static Dictionary<byte, float> AssassinTimer = new();
    public static Dictionary<byte, PlayerControl> CursedPlayers = new();
    public static Dictionary<byte, bool> isCurseAndKill = new();
    public static Dictionary<byte, int> MafiaRevenged = new();
    public static Dictionary<byte, int> RetributionistRevenged = new();
    public static Dictionary<byte, int> GuesserGuessed = new();
    public static Dictionary<byte, int> CapitalismAddTask = new();
    public static Dictionary<byte, int> CapitalismAssignTask = new();
    public static Dictionary<(byte, byte), bool> isDoused = new();
    public static Dictionary<(byte, byte), bool> isDraw = new();
    public static Dictionary<(byte, byte), bool> isRevealed = new();
    public static Dictionary<byte, (PlayerControl, float)> ArsonistTimer = new();
    public static Dictionary<byte, (PlayerControl, float)> RevolutionistTimer = new();
    public static Dictionary<byte, long> RevolutionistStart = new();
    public static Dictionary<byte, long> RevolutionistLastTime = new();
    public static Dictionary<byte, int> RevolutionistCountdown = new();
    public static Dictionary<byte, byte> SpeedBoostTarget = new();
    public static Dictionary<byte, int> MayorUsedButtonCount = new();
    public static Dictionary<byte, int> ParaUsedButtonCount = new();
    public static Dictionary<byte, int> MarioVentCount = new();
    public static Dictionary<byte, long> VeteranInProtect = new();
    public static Dictionary<byte, float> VeteranNumOfUsed = new();
    public static Dictionary<byte, long> GrenadierBlinding = new();
    public static Dictionary<byte, long> MadGrenadierBlinding = new();
    public static float BastionNumberOfAbilityUses = 0;
    public static Dictionary<byte, float> GrenadierNumOfUsed = new();
    public static Dictionary<byte, long> Lighter = new();
    public static Dictionary<byte, float> LighterNumOfUsed = new();
    public static Dictionary<byte, long> AllKillers = new();
    public static Dictionary<byte, float> TimeMasterNumOfUsed = new();
    public static Dictionary<byte, int> CursedWolfSpellCount = new();
    public static Dictionary<byte, int> JinxSpellCount = new();
    public static int AliveImpostorCount;
    public static bool isCursed;
    public static Dictionary<byte, bool> CheckShapeshift = new();
    public static Dictionary<byte, byte> ShapeshiftTarget = new();
    public static Dictionary<(byte, byte), string> targetArrows = new();
    public static Dictionary<byte, Vector2> EscapeeLocation = new();
    public static Dictionary<byte, Vector2> TimeMasterLocation = new();
    public static bool VisibleTasksCount = false;
    public static string nickName = "";
    public static bool introDestroyed = false;
    public static int DiscussionTime;
    public static int VotingTime;
    public static byte currentDousingTarget = byte.MaxValue;
    public static byte currentDrawTarget = byte.MaxValue;
    public static float DefaultCrewmateVision;
    public static float DefaultImpostorVision;
    public static bool IsInitialRelease = DateTime.Now.Month == 1 && DateTime.Now.Day is 17;
    public static bool IsAprilFools = DateTime.Now.Month == 4 && DateTime.Now.Day is 1;
    public static bool ResetOptions = true;
    public static byte FirstDied = byte.MaxValue;
    public static byte ShieldPlayer = byte.MaxValue;
    public static int MadmateNum = 0;
    public static int BardCreations = 0;
    public static int MeetingsPassed = 0;
    public static Dictionary<byte, byte> Provoked = new();
    public static Dictionary<byte, float> DovesOfNeaceNumOfUsed = new();
    public static bool SwapSend;

    public static Dictionary<byte, CustomRoles> DevRole = new();
    public static List<byte> GodfatherTarget = new();
    public static Dictionary<byte, int> CrewpostorTasksDone = new();
    public static Dictionary<byte, List<string>> AwareInteracted = new();
    public static byte ShamanTarget = byte.MaxValue;
    public static bool ShamanTargetChoosen = false;



    public static IEnumerable<PlayerControl> AllPlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null);
    public static IEnumerable<PlayerControl> AllAlivePlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.IsAlive() && !p.Data.Disconnected && !Pelican.IsEaten(p.PlayerId));

    public static List<PlayerControl> AllPlayerControlsList => (List<PlayerControl>)PlayerControl.AllPlayerControls.ToArray().Where(p => p != null);
    public static List<PlayerControl> AllAlivePlayerControlsList => (List<PlayerControl>)PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.IsAlive() && !p.Data.Disconnected && !Pelican.IsEaten(p.PlayerId));

    public static Main Instance;

    //一些很新的东东

    public static string OverrideWelcomeMsg = "";
    public static int HostClientId;

    public static Dictionary<byte,List<int>> GuessNumber = new();

    public static List<string> TName_Snacks_CN = new() { "冰激凌", "奶茶", "巧克力", "蛋糕", "甜甜圈", "可乐", "柠檬水", "冰糖葫芦", "果冻", "糖果", "牛奶", "抹茶", "烧仙草", "菠萝包", "布丁", "椰子冻", "曲奇", "红豆土司", "三彩团子", "艾草团子", "泡芙", "可丽饼", "桃酥", "麻薯", "鸡蛋仔", "马卡龙", "雪梅娘", "炒酸奶", "蛋挞", "松饼", "西米露", "奶冻", "奶酥", "可颂", "奶糖" };
    public static List<string> TName_Snacks_EN = new() { "Ice cream", "Milk tea", "Chocolate", "Cake", "Donut", "Coke", "Lemonade", "Candied haws", "Jelly", "Candy", "Milk", "Matcha", "Burning Grass Jelly", "Pineapple Bun", "Pudding", "Coconut Jelly", "Cookies", "Red Bean Toast", "Three Color Dumplings", "Wormwood Dumplings", "Puffs", "Can be Crepe", "Peach Crisp", "Mochi", "Egg Waffle", "Macaron", "Snow Plum Niang", "Fried Yogurt", "Egg Tart", "Muffin", "Sago Dew", "panna cotta", "soufflé", "croissant", "toffee" };
    public static string Get_TName_Snacks => TranslationController.Instance.currentLanguage.languageID is SupportedLangs.SChinese or SupportedLangs.TChinese ?
        TName_Snacks_CN[IRandom.Instance.Next(0, TName_Snacks_CN.Count)] :
        TName_Snacks_EN[IRandom.Instance.Next(0, TName_Snacks_EN.Count)];

    public override void Load()
    {
        Instance = this;

        //Client Options
        HideName = Config.Bind("Client Options", "Hide Game Code Name", "TOHE");
        HideColor = Config.Bind("Client Options", "Hide Game Code Color", $"{ModColor}");
        DebugKeyInput = Config.Bind("Authentication", "Debug Key", "");
        AutoStart = Config.Bind("Client Options", "AutoStart", false);
        UnlockFPS = Config.Bind("Client Options", "UnlockFPS", false);
        ShowFPS = Config.Bind("Client Options", "ShowFPS", false);
        EnableGM = Config.Bind("Client Options", "EnableGM", false);
        AutoStart = Config.Bind("Client Options", "AutoStart", false);
        ForceOwnLanguage = Config.Bind("Client Options", "ForceOwnLanguage", false);
        ForceOwnLanguageRoleName = Config.Bind("Client Options", "ForceOwnLanguageRoleName", false);
        EnableCustomButton = Config.Bind("Client Options", "EnableCustomButton", false);
        EnableCustomSoundEffect = Config.Bind("Client Options", "EnableCustomSoundEffect", true);
        ShowTextOverlay = Config.Bind("Client Options", "ShowTextOverlay", false);
        ModeForSmallScreen = Config.Bind("Client Options", "ModeForSmallScreen", false);
        EnableRoleSummary = Config.Bind("Client Options", "EnableRoleSummary", false); // Reverted to false due to it now being a setting to revert the summary change
        SwitchVanilla = Config.Bind("Client Options", "SwitchVanilla", false);
        VersionCheat = Config.Bind("Client Options", "VersionCheat", false);
        GodMode = Config.Bind("Client Options", "GodMode", false);
        AutoMuteUs = Config.Bind("Client Options", "AutoMuteUs", false); // The AutoMuteUs bot fails to match the host's name.

        Logger = BepInEx.Logging.Logger.CreateLogSource("TOHE");
        TOHE.Logger.Enable();
        TOHE.Logger.Disable("NotifyRoles");
        TOHE.Logger.Disable("SwitchSystem");
        TOHE.Logger.Disable("ModNews");
        if (!DebugModeManager.AmDebugger)
        {
            TOHE.Logger.Disable("2018k");
            TOHE.Logger.Disable("Github");
            TOHE.Logger.Disable("CustomRpcSender");
            //TOHE.Logger.Disable("ReceiveRPC");
            TOHE.Logger.Disable("SendRPC");
            TOHE.Logger.Disable("SetRole");
            TOHE.Logger.Disable("Info.Role");
            TOHE.Logger.Disable("TaskState.Init");
            //TOHE.Logger.Disable("Vote");
            TOHE.Logger.Disable("RpcSetNamePrivate");
            //TOHE.Logger.Disable("SendChat");
            TOHE.Logger.Disable("SetName");
            //TOHE.Logger.Disable("AssignRoles");
            //TOHE.Logger.Disable("RepairSystem");
            //TOHE.Logger.Disable("MurderPlayer");
            //TOHE.Logger.Disable("CheckMurder");
            TOHE.Logger.Disable("PlayerControl.RpcSetRole");
            TOHE.Logger.Disable("SyncCustomSettings");
        }
        //TOHE.Logger.isDetail = true;

        // 認証関連-初期化
        DebugKeyAuth = new HashAuth(DebugKeyHash, DebugKeySalt);

        // 認証関連-認証
        DebugModeManager.Auth(DebugKeyAuth, DebugKeyInput.Value);

        Preset1 = Config.Bind("Preset Name Options", "Preset1", "Preset_1");
        Preset2 = Config.Bind("Preset Name Options", "Preset2", "Preset_2");
        Preset3 = Config.Bind("Preset Name Options", "Preset3", "Preset_3");
        Preset4 = Config.Bind("Preset Name Options", "Preset4", "Preset_4");
        Preset5 = Config.Bind("Preset Name Options", "Preset5", "Preset_5");
        WebhookURL = Config.Bind("Other", "WebhookURL", "none");
        BetaBuildURL = Config.Bind("Other", "BetaBuildURL", "");
        MessageWait = Config.Bind("Other", "MessageWait", 1);
        LastKillCooldown = Config.Bind("Other", "LastKillCooldown", (float)30);
        LastShapeshifterCooldown = Config.Bind("Other", "LastShapeshifterCooldown", (float)30);

        hasArgumentException = false;
        ExceptionMessage = "";
        try
        {
            roleColors = new Dictionary<CustomRoles, string>()
            {
                //バニラ役職
                {CustomRoles.Crewmate, "#8cffff"},
                {CustomRoles.Engineer, "#8cffff"},
                {CustomRoles.Scientist, "#8cffff"},
                {CustomRoles.GuardianAngel, "#ffffff"},
                // Special impostor roles
                {CustomRoles.EvilMini, "#FF1919" },
                // Probability roles
                {CustomRoles.Mini, "#dddddd" },
                // Vanilla Remakes
                {CustomRoles.CrewmateTOHE, "#8cffff"},
                {CustomRoles.EngineerTOHE, "#FF6A00"},
                {CustomRoles.ScientistTOHE, "#8ee98e"},
                {CustomRoles.GuardianAngelTOHE, "#77e6d1"},
                //特殊クルー役職
                {CustomRoles.Luckey, "#b8d7a3" },
                {CustomRoles.President, "#00ffac"},
                {CustomRoles.Needy, "#a4dffe"},
                {CustomRoles.SabotageMaster, "#3333ff"},
                {CustomRoles.Snitch, "#b8fb4f"},
                {CustomRoles.Marshall, "#5573aa"},
                {CustomRoles.Mayor, "#204d42"},
                {CustomRoles.Bastion, "#696969"},
                {CustomRoles.Paranoia, "#c993f5"},
                {CustomRoles.Psychic, "#6F698C"},
                {CustomRoles.Cleanser,"#98FF98" },
                {CustomRoles.Sheriff, "#ffb347"},
                {CustomRoles.Vigilante, "#9900CC"},
                {CustomRoles.CopyCat, "#ffb2ab"},
                {CustomRoles.SuperStar, "#f6f657"},
                {CustomRoles.CyberStar, "#ee4a55" },
                {CustomRoles.SpeedBooster, "#00ffff"},
                {CustomRoles.Doctor, "#80ffdd"},
                {CustomRoles.Dictator, "#df9b00"},
                {CustomRoles.Detective, "#7160e8" },
                {CustomRoles.NiceGuesser, "#f0e68c"},
                {CustomRoles.SwordsMan, "#7a7a7a"},
                {CustomRoles.Transporter, "#42D1FF"},
                {CustomRoles.TimeManager, "#6495ed"},
                {CustomRoles.Veteran, "#a77738"},
                {CustomRoles.Bodyguard, "#185abd"},
                {CustomRoles.Counterfeiter, "#BE29EC"},
                {CustomRoles.Witness, "#e70052"},
                {CustomRoles.Grenadier, "#3c4a16"},
                {CustomRoles.Lighter, "#eee5be"},
                {CustomRoles.TaskManager, "#00ffa5" },
                {CustomRoles.Medic, "#00ff97"},
                {CustomRoles.Divinator, "#882c83"},
                {CustomRoles.Glitch, "#39FF14"},
                {CustomRoles.Judge, "#f8d85a"},
                {CustomRoles.Lookout, "#2a52be"},
                {CustomRoles.Mortician, "#333c49"},
                {CustomRoles.Mediumshiper, "#a200ff"},
                {CustomRoles.Observer, "#a8e0fa"},
                {CustomRoles.DovesOfNeace, "#007FFF"},
                {CustomRoles.Monarch, "#FFA500"},
                {CustomRoles.Bloodhound, "#8B0000"},
                {CustomRoles.Tracker, "#3CB371"},
                {CustomRoles.Merchant, "#D27D2D"},
                {CustomRoles.Retributionist, "#228B22"},
                {CustomRoles.Alchemist, "#a058bf"},
                {CustomRoles.Deputy, "#df9026"},
                {CustomRoles.Investigator, "#007FFF"},
                {CustomRoles.Jailer,"#aa900d"},
                {CustomRoles.Guardian, "#2E8B57"},
                {CustomRoles.Addict, "#008000"},
                {CustomRoles.Tracefinder, "#0066CC"},
                {CustomRoles.Oracle, "#6666FF"},
                {CustomRoles.Spiritualist, "#669999"},
                {CustomRoles.Chameleon, "#01C834"},
                {CustomRoles.ParityCop, "#0D57AF"},
                {CustomRoles.Admirer, "#ee43c3"},
                {CustomRoles.TimeMaster, "#44baff"},
                {CustomRoles.Crusader, "#C65C39"},
                {CustomRoles.Reverie, "#00BFFF"},
                {CustomRoles.Monitor, "#7223DA"},
                {CustomRoles.Swapper, "#66E666"},
                {CustomRoles.ChiefOfPolice,"#f8cd46"},
                {CustomRoles.NiceMini, "#edc240" },
                {CustomRoles.Spy, "#34495E"},
                //第三陣営役職
                {CustomRoles.Arsonist, "#ff6633"},
                {CustomRoles.Pyromaniac, "#fc8a4c"},
                {CustomRoles.Agitater, "#F4A460"},
                {CustomRoles.Bandit, "#8B008B"},
                {CustomRoles.PlagueBearer,"#e5f6b4"},
                {CustomRoles.Pestilence,"#343136"},
                {CustomRoles.Jester, "#ec62a5"},
                {CustomRoles.Terrorist, "#00e600"},
                {CustomRoles.Executioner, "#c0c0c0"},
                {CustomRoles.Lawyer, "#008080"},
                {CustomRoles.God, "#f96464"},
                {CustomRoles.Opportunist, "#4dff4d"},
                {CustomRoles.Shaman, "#50c878"},
                {CustomRoles.Mario, "#ff6201"},
                {CustomRoles.Jackal, "#00b4eb"},
                {CustomRoles.Sidekick, "#00b4eb"},
                {CustomRoles.Innocent, "#8f815e"},
                {CustomRoles.Pelican, "#34c84b"},
                {CustomRoles.Revolutionist, "#ba4d06"},
                {CustomRoles.FFF, "#414b66"},
                {CustomRoles.Konan, "#4d4dff"},
                {CustomRoles.Gamer, "#68bc71"},
                {CustomRoles.DarkHide, "#483d8b"},
                {CustomRoles.Workaholic, "#008b8b"},
                {CustomRoles.Collector, "#9d8892"},
                {CustomRoles.Provocateur, "#74ba43"},
                {CustomRoles.Sunnyboy, "#ff9902"},
                {CustomRoles.Poisoner, "#478800"},
                {CustomRoles.Huntsman, "#ad8739"},
                {CustomRoles.Necromancer, "#9C87AB"},
                {CustomRoles.NWitch, "#BF5FFF"},
                {CustomRoles.Totocalcio, "#ff9409"},
                {CustomRoles.Romantic, "#FF1493"},
                {CustomRoles.VengefulRomantic, "#8B0000"},
                {CustomRoles.RuthlessRomantic, "#D2691E"},
                {CustomRoles.Succubus, "#cf6acd"},
                {CustomRoles.HexMaster, "#ff00ff"},
                //{CustomRoles.Occultist, "#375d91"},
                {CustomRoles.Wraith, "#4B0082"},
                {CustomRoles.NSerialKiller, "#233fcc"},
                {CustomRoles.BloodKnight, "#630000"},
                {CustomRoles.Juggernaut, "#A41342"},
                {CustomRoles.Parasite, "#ff1919"},
                {CustomRoles.Crewpostor, "#ff1919"},
                {CustomRoles.Refugee, "#ff1919"},
                {CustomRoles.Infectious, "#7B8968"},
                {CustomRoles.Virus, "#2E8B57"},
                {CustomRoles.Farseer, "#BA55D3"},
                {CustomRoles.Pursuer, "#617218"},
                {CustomRoles.Phantom, "#662962"},
                {CustomRoles.Jinx, "#ed2f91"},
                {CustomRoles.Maverick, "#781717"},
                {CustomRoles.CursedSoul, "#531269"},
                {CustomRoles.PotionMaster, "#663399"},
        //        {CustomRoles.Sorcerer, "#663399"},
                {CustomRoles.Pickpocket, "#47008B"},
                {CustomRoles.Traitor, "#BA2E05"},
                {CustomRoles.Vulture, "#556B2F"},
                {CustomRoles.Medusa, "#9900CC"},
                {CustomRoles.Spiritcaller, "#003366"},
                {CustomRoles.EvilSpirit, "#003366"},
                {CustomRoles.Convict, "#ff1919"},
                {CustomRoles.Amnesiac, "#7FBFFF"},
                {CustomRoles.Doomsayer, "#14f786"},
                {CustomRoles.Masochist, "#684405"},
                {CustomRoles.Pirate, "#EDC240"},
                {CustomRoles.Shroud, "#6697FF"},
                {CustomRoles.Werewolf, "#191970"},
                {CustomRoles.Seeker, "#ffaa00"},
                {CustomRoles.SoulCollector, "#A675A1"},
                {CustomRoles.Imitator, "#B3D94C"},
                {CustomRoles.Doppelganger,"#f6f4a3" },
            
                // GM
                {CustomRoles.GM, "#ff5b70"},
                //サブ役職
                {CustomRoles.NotAssigned, "#ffffff"},
                {CustomRoles.LastImpostor, "#ff1919"},
                {CustomRoles.Lovers, "#ff9ace"},
                {CustomRoles.Ntr, "#00a4ff"},
                {CustomRoles.Madmate, "#ff1919"},
                {CustomRoles.Watcher, "#800080"},
                {CustomRoles.Flashman, "#ff8400"},
                {CustomRoles.Torch, "#eee5be"},
                {CustomRoles.Seer, "#61b26c"},
                {CustomRoles.Brakar, "#1447af"},
                {CustomRoles.Oblivious, "#424242"},
                {CustomRoles.Bewilder, "#c894f5"},
                //{CustomRoles.Sunglasses, "#E7C12B"},
                {CustomRoles.Workhorse, "#00ffff"},
                {CustomRoles.Fool, "#e6e7ff"},
                {CustomRoles.Avanger, "#ffab1c"},
                {CustomRoles.Youtuber, "#fb749b"},
                {CustomRoles.Egoist, "#5600ff"},
                {CustomRoles.TicketsStealer, "#ff1919"},
                {CustomRoles.DualPersonality, "#3a648f"},
                {CustomRoles.Mimic, "#ff1919"},
                {CustomRoles.Guesser, "#f8cd46"},
                {CustomRoles.Necroview, "#663399"},
                {CustomRoles.Reach, "#74ba43"},
                {CustomRoles.Charmed, "#cf6acd"},
                {CustomRoles.Cleansed,"#98FF98"},
                {CustomRoles.Bait, "#00f7ff"},
                {CustomRoles.Trapper, "#5a8fd0"},
                {CustomRoles.Infected, "#7B8968"},
                {CustomRoles.Onbound, "#BAAAE9"},
                {CustomRoles.Rebound, "#56b5ff"},
                {CustomRoles.Knighted, "#FFA500"},
                {CustomRoles.Contagious, "#2E8B57"},
                {CustomRoles.Unreportable, "#FF6347"},
                {CustomRoles.Rogue, "#696969"},
                {CustomRoles.Lucky, "#b8d7a3"},
                {CustomRoles.Unlucky, "#d7a3a3"},
                {CustomRoles.DoubleShot, "#19fa8d"},
     //           {CustomRoles.Reflective, "#FFD700"},
                {CustomRoles.Rascal, "#990000"},
                {CustomRoles.Soulless, "#531269"},
                {CustomRoles.Gravestone, "#2EA8E7"},
                {CustomRoles.Lazy, "#a4dffe"},
                {CustomRoles.Autopsy, "#80ffdd"},
                {CustomRoles.Loyal, "#B71556"},
                {CustomRoles.Visionary, "#ff1919"},
                {CustomRoles.Recruit, "#00b4eb"},
                {CustomRoles.Admired, "#ee43c3"},
                //{CustomRoles.Glow, "#E2F147"},
                {CustomRoles.Diseased, "#AAAAAA"},
                {CustomRoles.Antidote,"#FF9876"},
                {CustomRoles.VoidBallot,"#FF3399"},
                {CustomRoles.Aware,"#4B0082"},
                {CustomRoles.Stubborn,"#FF5733"},
                {CustomRoles.Fragile,"#D3D3D3"},
                {CustomRoles.Burst, "#B619B9"},
                {CustomRoles.Bloodlust, "#691a2e"},
                {CustomRoles.Overclocked, "#C4AD2C"},
                {CustomRoles.Swift, "#ff1919"},
                {CustomRoles.Mare, "#ff1919"},
                {CustomRoles.Ghoul, "#B22222"},
                {CustomRoles.Sleuth, "#803333" },
                {CustomRoles.Clumsy, "#ff1919"},
                {CustomRoles.Circumvent, "#ff1919"},
                {CustomRoles.Nimble, "#FFFAA6"},
                {CustomRoles.Repairman, "#3333ff"},

                {CustomRoles.Cyber, "#F46F4E" },

             //   {CustomRoles.QuickFix, "#3333ff"},

            };
            foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
            {
                switch (role.GetCustomRoleTypes())
                {
                    case CustomRoleTypes.Impostor:
                        roleColors.TryAdd(role, "#ff1919");
                        break;
                    default:
                        break;
                }
            }
        }
        catch (ArgumentException ex)
        {
            TOHE.Logger.Error("错误：字典出现重复项", "LoadDictionary");
            TOHE.Logger.Exception(ex, "LoadDictionary");
            hasArgumentException = true;
            ExceptionMessage = ex.Message;
            ExceptionMessageIsShown = false;
        }

        CustomWinnerHolder.Reset();
        ServerAddManager.Init();
        Translator.Init();
        BanManager.Init();
        TemplateManager.Init();
        //SpamManager.Init();
        DevManager.Init();
        Cloud.Init();

        IRandom.SetInstance(new NetRandomWrapper());

        TOHE.Logger.Info($" {Application.version}", "Among Us Version");

        var handler = TOHE.Logger.Handler("GitVersion");
        handler.Info($"{nameof(ThisAssembly.Git.BaseTag)}: {ThisAssembly.Git.BaseTag}");
        handler.Info($"{nameof(ThisAssembly.Git.Commit)}: {ThisAssembly.Git.Commit}");
        handler.Info($"{nameof(ThisAssembly.Git.Commits)}: {ThisAssembly.Git.Commits}");
        handler.Info($"{nameof(ThisAssembly.Git.IsDirty)}: {ThisAssembly.Git.IsDirty}");
        handler.Info($"{nameof(ThisAssembly.Git.Sha)}: {ThisAssembly.Git.Sha}");
        handler.Info($"{nameof(ThisAssembly.Git.Tag)}: {ThisAssembly.Git.Tag}");

        ClassInjector.RegisterTypeInIl2Cpp<ErrorText>();

        Harmony.PatchAll();

        if (!DebugModeManager.AmDebugger) ConsoleManager.DetachConsole();
        else ConsoleManager.CreateConsole();

        TOHE.Logger.Msg("========= TOHE loaded! =========", "Plugin Load");
    }
}
public enum CustomRoles
{
    //Default
    Crewmate = 0,
    //Impostor(Vanilla)
    Impostor,
    Shapeshifter,
    // Vanilla Remakes
    ImpostorTOHE,
    ShapeshifterTOHE,
    //Impostor
    BountyHunter,
    Undertaker,
    FireWorks,
    Mafia,
    Godfather,
    SerialKiller,
    ShapeMaster,
    Wildling,
    EvilGuesser,
    Minimalism,
    Zombie,
    Sniper,
    Vampire,
    Vampiress,
    Witch,
    Warlock,
    Assassin,
    Vindicator,
    Hacker,
    Miner,
    Escapee,
    Inhibitor,
    Puppeteer,
    Mastermind,
    TimeThief,
    EvilTracker,
    AntiAdminer,
    Sans,
    Bomber,
    Nuker,
    BoobyTrap,
    Scavenger,
    Capitalism,
    Gangster,
    Cleaner,
    BallLightning,
    Greedier,
    CursedWolf,
    ImperiusCurse,
    QuickShooter,
    Eraser,
    OverKiller,
    Hangman,
    Bard,
    Trickster,
    Swooper,
    Crewpostor,
    Parasite,
    Disperser,
    Camouflager,
    Saboteur,
    Councillor,
    Dazzler,
    Deathpact,
    Devourer,
    EvilDiviner,
    Morphling,
    Twister,
    Lurker,
    Convict,
    Berserker,
    Visionary,
    Refugee,
    Underdog,
    Ludopath,
    Chronomancer,
    Pitfall,
    EvilMini,
    Blackmailer,
    // Flashbang,
    //Crewmate(Vanilla)
    Engineer,
    GuardianAngel,
    Scientist,
    // Vanilla Remakes
    CrewmateTOHE,
    EngineerTOHE,
    GuardianAngelTOHE,
    ScientistTOHE,
    //Crewmate
    Luckey,
    President,
    Needy,
    SuperStar,
    CyberStar,
    Cleanser,
    Mayor,
    Bastion,
    Paranoia,
    Psychic,
    SabotageMaster,
    Sheriff,
    Vigilante,
    Snitch,
    Jailer,
    Marshall,
    SpeedBooster,
    Dictator,
    Doctor,
    Detective,
    SwordsMan,
    NiceGuesser,
    Transporter,
    TimeManager,
    Veteran,
    Bodyguard,
    Counterfeiter,
    Witness,
    Grenadier,
    Lighter,
    TaskManager,
    Medic,
    Divinator,
    Glitch,
    Judge,
    Mortician,
    Mediumshiper,
    Observer,
    DovesOfNeace,
    Monarch,
    CopyCat,
    Farseer,
    Bloodhound,
    Tracker,
    Merchant,
    Retributionist,
    Alchemist,
    Deputy,
    Investigator,
    Guardian,
    Addict,
    Tracefinder,
    Oracle,
    Spiritualist,
    Chameleon,
    ParityCop,
    Admirer,
    TimeMaster,
    Crusader,
    Reverie,
    Lookout,
    Monitor,
    Swapper,
    ChiefOfPolice,
    NiceMini,
    Spy,

    //Neutral
    Arsonist,
    Doppelganger,
    Pyromaniac,
    Agitater,
    Bandit,
    Seeker,
    SoulCollector,
    HexMaster,
    Jester,
    God,
    Opportunist,
    Shaman,
    Mario,
    Terrorist,
    Executioner,
    Lawyer,
    Jackal,
    Poisoner,
    NWitch,
    Innocent,
    Pelican,
    Revolutionist,
    NSerialKiller,
    Juggernaut,
    Infectious,
    FFF,
    Konan,
    Gamer,
    DarkHide,
    Workaholic,
    Collector,
    Provocateur,
    Sunnyboy,
    BloodKnight,
    Wraith,
    Totocalcio,
    Romantic,
    VengefulRomantic,
    RuthlessRomantic,
    Succubus,
    Virus,
    Pursuer,
    Phantom,
    Jinx,
    Maverick,
    CursedSoul,
    Pirate,
    PotionMaster,
    Pickpocket,
    Traitor,
    Vulture,
    PlagueBearer,
    Pestilence,
    Medusa,
    Sidekick,
    Spiritcaller,
    Amnesiac,
    Doomsayer,
    Masochist,
    Shroud,
    Werewolf,
    Necromancer,
    Huntsman,
    //Occultist,
    Imitator,
   //two-way camp
    Mini,
   // Sorcerer,
   // Flux,

    //GM
    GM,

    // Sub-role after 500
    NotAssigned = 500,
    LastImpostor,
    Lovers,
    Ntr,
    Madmate,
    Watcher,
    Flashman,
    Torch,
    Seer,
    Brakar,
    Oblivious,
    Bewilder,
    //Sunglasses,
    Workhorse,
    Fool,
    Avanger,
    Youtuber,
    Egoist,
    TicketsStealer,
    DualPersonality,
    Mimic,
    Guesser,
    Necroview,
    Reach,
    Charmed,
    Cleansed,
    Bait,
    Trapper,
    Infected,
    Onbound,
    Rebound,
    Knighted,
    Contagious,
    Unreportable,
    Rogue,
    Lucky,
    Unlucky,
    DoubleShot,
   // Reflective,
    Rascal,
    Soulless,
    Gravestone,
    Lazy,
    Autopsy,
    Loyal,
    EvilSpirit,
    Recruit,
    Admired,
    //Glow,
    Diseased,
    Antidote,
    VoidBallot,
    Aware,
    Fragile,
    Swift,
    Ghoul,
    Mare,
    Burst,
    Bloodlust,
    Sleuth,
    Clumsy,
    Nimble,
    Circumvent,
    Repairman,
    Cyber,
    Stubborn,
    Overclocked
    // QuickFix
}
//WinData
public enum CustomWinner
{
    Draw = -1,
    Default = -2,
    None = -3,
    Error = -4,
    Neutrals = -5,
    Impostor = CustomRoles.Impostor,
    Crewmate = CustomRoles.Crewmate,
    Jester = CustomRoles.Jester,
    Terrorist = CustomRoles.Terrorist,
    Lovers = CustomRoles.Lovers,
    Executioner = CustomRoles.Executioner,
    Arsonist = CustomRoles.Arsonist,
    Pyromaniac = CustomRoles.Pyromaniac,
    Agitater = CustomRoles.Agitater,
    Revolutionist = CustomRoles.Revolutionist,
    Jackal = CustomRoles.Jackal,
    Sidekick = CustomRoles.Sidekick,
    God = CustomRoles.God,
    Mario = CustomRoles.Mario,
    Innocent = CustomRoles.Innocent,
    Pelican = CustomRoles.Pelican,
    Youtuber = CustomRoles.Youtuber,
    Egoist = CustomRoles.Egoist,
    Gamer = CustomRoles.Gamer,
    DarkHide = CustomRoles.DarkHide,
    Workaholic = CustomRoles.Workaholic,
    Collector = CustomRoles.Collector,
    BloodKnight = CustomRoles.BloodKnight,
    Poisoner = CustomRoles.Poisoner,
    HexMaster = CustomRoles.HexMaster,
    //Occultist = CustomRoles.Occultist,
    Succubus = CustomRoles.Succubus,
    Wraith = CustomRoles.Wraith,
    Bandit = CustomRoles.Bandit,
    Pirate = CustomRoles.Pirate,
    SerialKiller = CustomRoles.NSerialKiller,
    Werewolf = CustomRoles.Werewolf,
    Necromancer = CustomRoles.Necromancer,
    Huntsman = CustomRoles.Huntsman,
    Witch = CustomRoles.NWitch,
    Juggernaut = CustomRoles.Juggernaut,
    Infectious = CustomRoles.Infectious,
    Virus = CustomRoles.Virus,
    Rogue = CustomRoles.Rogue,
    Phantom = CustomRoles.Phantom,
    Jinx = CustomRoles.Jinx,
    CursedSoul = CustomRoles.CursedSoul,
    PotionMaster = CustomRoles.PotionMaster,
    Pickpocket = CustomRoles.Pickpocket,
    Traitor = CustomRoles.Traitor,
    Vulture = CustomRoles.Vulture,
    Pestilence = CustomRoles.Pestilence,
    Medusa = CustomRoles.Medusa,
    Spiritcaller = CustomRoles.Spiritcaller,
    Glitch = CustomRoles.Glitch,
    Plaguebearer = CustomRoles.PlagueBearer,
    Masochist = CustomRoles.Masochist,
    Doomsayer = CustomRoles.Doomsayer,
    Shroud = CustomRoles.Shroud,
    Seeker = CustomRoles.Seeker,
    SoulCollector = CustomRoles.SoulCollector,
    RuthlessRomantic = CustomRoles.RuthlessRomantic,
    NiceMini = CustomRoles.Mini,
    Doppelganger = CustomRoles.Doppelganger,
}
public enum AdditionalWinners
{
    None = -1,
    Lovers = CustomRoles.Lovers,
    Opportunist = CustomRoles.Opportunist,
    Executioner = CustomRoles.Executioner,
    Lawyer = CustomRoles.Lawyer,
    FFF = CustomRoles.FFF,
    Provocateur = CustomRoles.Provocateur,
    Sunnyboy = CustomRoles.Sunnyboy,
    Witch = CustomRoles.NWitch,
    Totocalcio = CustomRoles.Totocalcio,
    Romantic = CustomRoles.Romantic,
    VengefulRomantic = CustomRoles.VengefulRomantic,
    RuthlessRomantic = CustomRoles.RuthlessRomantic,
    Jackal = CustomRoles.Jackal,
    Sidekick = CustomRoles.Sidekick,
    Pursuer = CustomRoles.Pursuer,
    Phantom = CustomRoles.Phantom,
    Maverick = CustomRoles.Maverick,
    Shaman = CustomRoles.Shaman,
 //   NiceMini = CustomRoles.NiceMini,
 //   Baker = CustomRoles.Baker,
}
public enum SuffixModes
{
    None = 0,
    TOHE,
    Streaming,
    Recording,
    RoomHost,
    OriginalName,
    DoNotKillMe,
    NoAndroidPlz,
    AutoHost
}
public enum VoteMode
{
    Default,
    Suicide,
    SelfVote,
    Skip
}
public enum TieMode
{
    Default,
    All,
    Random
}
