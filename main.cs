using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
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
    public const string PluginVersion = "2024.1.21.1501";
    public const string PluginDisplayVersion = "1.5.0 Canary 1";
    public static readonly string SupportedVersionAU = "2023.10.24"; // also 2023.11.28

    /******************* Change one of the three variables to true before making a release. *******************/
    public const bool Canary = true; 
    public const bool fullRelease = false;
    public const bool devRelease = false;

    public static bool hasAccess = true;

    public static readonly bool ShowUpdateButton = true;

    public static readonly bool ShowGitHubButton = true;
    public static readonly string GitHubInviteUrl = "https://github.com/0xDrMoe/TownofHost-Enhanced";

    public static readonly bool ShowDiscordButton = true;
    public static readonly string DiscordInviteUrl = "https://discord.gg/tohe";

    public static readonly bool ShowWebsiteButton = true;
    public static readonly string WebsiteInviteUrl = "https://tohre.dev";
    
    public static readonly bool ShowKofiButton = true;
    public static readonly string kofiInviteUrl = "https://ko-fi.com/TOHEN";

    public Harmony Harmony { get; } = new Harmony(PluginGuid);
    public static Version version = Version.Parse(PluginVersion);
    public static BepInEx.Logging.ManualLogSource Logger;
    public static bool hasArgumentException = false;
    public static string ExceptionMessage;
    public static bool ExceptionMessageIsShown = false;
    public static bool AlreadyShowMsgBox = false;
    public static string credentialsText;
    public static NormalGameOptionsV07 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;
    public static HideNSeekGameOptionsV07 HideNSeekOptions => GameOptionsManager.Instance.currentHideNSeekGameOptions;
    //Client Options
    public static ConfigEntry<string> HideName { get; private set; }
    public static ConfigEntry<string> HideColor { get; private set; }
    public static ConfigEntry<int> MessageWait { get; private set; }
    public static ConfigEntry<bool> UnlockFPS { get; private set; }
    public static ConfigEntry<bool> ShowFPS { get; private set; }
    public static ConfigEntry<bool> AutoMuteUs { get; private set; }
    public static ConfigEntry<bool> HorseMode { get; private set; }
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
    public static bool IsHostVersionCheating = false;
    public static ConfigEntry<bool> GodMode { get; private set; }
    public static ConfigEntry<bool> AutoRehost { get; private set; }

    public static Dictionary<int, PlayerVersion> playerVersion = [];
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
    public static Dictionary<byte, PlayerState> PlayerStates = [];
    public static Dictionary<byte, string> AllPlayerNames = [];
    public static Dictionary<byte, CustomRoles> AllPlayerCustomRoles;
    public static Dictionary<(byte, byte), string> LastNotifyNames;
    public static Dictionary<byte, Color32> PlayerColors = [];
    public static Dictionary<byte, PlayerState.DeathReason> AfterMeetingDeathPlayers = [];
    public static Dictionary<CustomRoles, string> roleColors;
    const string LANGUAGE_FOLDER_NAME = "Language";
    public static bool IsFixedCooldown => CustomRoles.Vampire.IsEnable() || CustomRoles.Poisoner.IsEnable() || CustomRoles.Vampiress.IsEnable();
    public static float RefixCooldownDelay = 0f;
    public static GameData.PlayerInfo LastVotedPlayerInfo;
    public static string LastVotedPlayer;
    public static HashSet<byte> ResetCamPlayerList = [];
    public static HashSet<byte> winnerList = [];
    public static HashSet<byte> ForCrusade = [];
    public static HashSet<byte> KillGhoul = [];
    public static HashSet<string> winnerNameList = [];
    public static HashSet<int> clientIdList = [];
    public static List<(string, byte, string)> MessagesToSend = [];
    public static bool isChatCommand = false;
    public static bool MeetingIsStarted = false;
    public static HashSet<PlayerControl> LoversPlayers = [];
    public static bool isLoversDead = true;
    public static Dictionary<byte, float> AllPlayerKillCooldown = [];
    public static Dictionary<byte, Vent> LastEnteredVent = [];
    public static Dictionary<byte, Vector2> LastEnteredVentLocation = [];
    public static Dictionary<byte, Vector2> TimeMasterBackTrack = [];
    public static Dictionary<byte, int> MasochistKillMax = [];
    public static Dictionary<byte, int> BerserkerKillMax = [];
    public static Dictionary<byte, int> TimeMasterNum = [];
    public static Dictionary<byte, long> TimeMasterInProtect = [];
    //public static Dictionary<byte, long> FlashbangInProtect = [];
    public static List<byte> CyberStarDead = [];
    public static List<byte> CyberDead = [];
    public static List<int> BombedVents = [];
    public static List<byte> WorkaholicAlive = [];
    public static List<byte> BurstBodies = [];
    public static List<byte> BaitAlive = [];
    public static List<byte> TasklessCrewmate = [];
    public static List<byte> BoobyTrapBody = [];
    public static List<byte> BoobyTrapKiller = [];
    //public static List<byte> KilledDiseased = [];
    public static Dictionary<byte, int> KilledDiseased = [];
    public static Dictionary<byte, int> KilledAntidote = [];
    //public static List<byte> ForFlashbang = [];
    public static Dictionary<byte, byte> KillerOfBoobyTrapBody = [];
    public static Dictionary<byte, string> DetectiveNotify = [];
    public static Dictionary<byte, string> SleuthNotify = [];
    public static Dictionary<byte, string> VirusNotify = [];
    public static List<byte> OverDeadPlayerList = [];
    public static bool DoBlockNameChange = false;
    public static int updateTime;
    public static bool newLobby = false;
    public static Dictionary<int, int> SayStartTimes = [];
    public static Dictionary<int, int> SayBanwordsTimes = [];
    public static Dictionary<byte, float> AllPlayerSpeed = [];
    public const float MinSpeed = 0.0001f;
    public static List<byte> CleanerBodies = [];
    public static List<byte> MedusaBodies = [];
    public static List<byte> InfectedBodies = [];
    public static List<byte> BrakarVoteFor = [];
    public static Dictionary<byte, (byte, float)> BitPlayers = [];
    public static Dictionary<byte, float> WarlockTimer = [];
    public static Dictionary<byte, float> AssassinTimer = [];
    public static Dictionary<byte, PlayerControl> CursedPlayers = [];
    public static Dictionary<byte, bool> isCurseAndKill = [];
    public static Dictionary<byte, int> MafiaRevenged = [];
    public static Dictionary<byte, int> RetributionistRevenged = [];
    public static Dictionary<byte, int> GuesserGuessed = [];
    public static Dictionary<byte, int> CapitalismAddTask = [];
    public static Dictionary<byte, int> CapitalismAssignTask = [];
    public static Dictionary<(byte, byte), bool> isDoused = [];
    public static Dictionary<(byte, byte), bool> isDraw = [];
    public static Dictionary<(byte, byte), bool> isRevealed = [];
    public static Dictionary<byte, (PlayerControl, float)> ArsonistTimer = [];
    public static Dictionary<byte, (PlayerControl, float)> RevolutionistTimer = [];
    public static Dictionary<byte, long> RevolutionistStart = [];
    public static Dictionary<byte, long> RevolutionistLastTime = [];
    public static Dictionary<byte, int> RevolutionistCountdown = [];
    public static Dictionary<byte, byte> SpeedBoostTarget = [];
    public static Dictionary<byte, int> MayorUsedButtonCount = [];
    public static Dictionary<byte, int> ParaUsedButtonCount = [];
    public static Dictionary<byte, int> MarioVentCount = [];
    public static Dictionary<byte, long> VeteranInProtect = [];
    public static Dictionary<byte, float> VeteranNumOfUsed = [];
    public static Dictionary<byte, long> GrenadierBlinding = [];
    public static Dictionary<byte, long> MadGrenadierBlinding = [];
    public static float BastionNumberOfAbilityUses = 0;
    public static Dictionary<byte, float> GrenadierNumOfUsed = [];
    public static Dictionary<byte, long> Lighter = [];
    public static Dictionary<byte, float> LighterNumOfUsed = [];
    public static Dictionary<byte, long> AllKillers = [];
    public static Dictionary<byte, float> TimeMasterNumOfUsed = [];
    public static Dictionary<byte, int> CursedWolfSpellCount = [];
    public static Dictionary<byte, int> JinxSpellCount = [];
    public static int AliveImpostorCount;
    public static bool isCursed;
    public static Dictionary<byte, bool> CheckShapeshift = [];
    public static Dictionary<byte, byte> ShapeshiftTarget = [];
    public static Dictionary<(byte, byte), string> targetArrows = [];
    public static Dictionary<byte, Vector2> EscapeeLocation = [];
    public static Dictionary<byte, Vector2> TimeMasterLocation = [];
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
    public static string FirstDied = ""; //Store with hash puid so things can pass through different round
    public static string ShieldPlayer = "";
    public static int MadmateNum = 0;
    public static int BardCreations = 0;
    public static int MeetingsPassed = 0;
    public static Dictionary<byte, byte> Provoked = [];
    public static Dictionary<byte, float> DovesOfNeaceNumOfUsed = [];
    //public static bool SwapSend;

    public static Dictionary<byte, CustomRoles> DevRole = [];
    public static List<byte> GodfatherTarget = [];
    public static Dictionary<byte, int> CrewpostorTasksDone = [];
    public static Dictionary<byte, List<string>> AwareInteracted = [];
    public static byte ShamanTarget = byte.MaxValue;
    public static bool ShamanTargetChoosen = false;
    
    public static Dictionary<byte, CustomRoles> ErasedRoleStorage = [];
    public static Dictionary<string, int> PlayerQuitTimes = [];

    //public static IEnumerable<PlayerControl> AllPlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null);
    //public static IEnumerable<PlayerControl> AllAlivePlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.IsAlive() && !p.Data.Disconnected && !Pelican.IsEaten(p.PlayerId));

    //public static List<PlayerControl> AllPlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null).ToList();
    //public static List<PlayerControl> AllAlivePlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.IsAlive() && !p.Data.Disconnected && !Pelican.IsEaten(p.PlayerId)).ToList();

    // Seems this better (if use foreach)
    public static PlayerControl[] AllPlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null).ToArray();
    public static PlayerControl[] AllAlivePlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.IsAlive() && !p.Data.Disconnected && !Pelican.IsEaten(p.PlayerId)).ToArray();

    public static Main Instance;

    public static string OverrideWelcomeMsg = "";
    public static int HostClientId;
    public static Dictionary<byte,List<int>> GuessNumber = [];

    public static List<string> TName_Snacks_CN = ["冰激凌", "奶茶", "巧克力", "蛋糕", "甜甜圈", "可乐", "柠檬水", "冰糖葫芦", "果冻", "糖果", "牛奶", "抹茶", "烧仙草", "菠萝包", "布丁", "椰子冻", "曲奇", "红豆土司", "三彩团子", "艾草团子", "泡芙", "可丽饼", "桃酥", "麻薯", "鸡蛋仔", "马卡龙", "雪梅娘", "炒酸奶", "蛋挞", "松饼", "西米露", "奶冻", "奶酥", "可颂", "奶糖"];
    public static List<string> TName_Snacks_EN = ["Ice cream", "Milk tea", "Chocolate", "Cake", "Donut", "Coke", "Lemonade", "Candied haws", "Jelly", "Candy", "Milk", "Matcha", "Burning Grass Jelly", "Pineapple Bun", "Pudding", "Coconut Jelly", "Cookies", "Red Bean Toast", "Three Color Dumplings", "Wormwood Dumplings", "Puffs", "Can be Crepe", "Peach Crisp", "Mochi", "Egg Waffle", "Macaron", "Snow Plum Niang", "Fried Yogurt", "Egg Tart", "Muffin", "Sago Dew", "panna cotta", "soufflé", "croissant", "toffee"];
    public static string Get_TName_Snacks => TranslationController.Instance.currentLanguage.languageID is SupportedLangs.SChinese or SupportedLangs.TChinese ?
        TName_Snacks_CN[IRandom.Instance.Next(0, TName_Snacks_CN.Count)] :
        TName_Snacks_EN[IRandom.Instance.Next(0, TName_Snacks_EN.Count)];

    private static void CreateTemplateRoleColorFile()
    {
        var sb = new StringBuilder();
        foreach (var title in roleColors) sb.Append($"{title.Key}:\n");
        File.WriteAllText(@$"./{LANGUAGE_FOLDER_NAME}/templateRoleColor.dat", sb.ToString());
    }
    public static void LoadCustomRoleColor()
    {
        const string filename = "RoleColor.dat";
        string path = @$"./{LANGUAGE_FOLDER_NAME}/{filename}";
        if (File.Exists(path))
        {
            TOHE.Logger.Info($"Load custom Role Color file：{filename}", "LoadCustomRoleColor");
            using StreamReader sr = new(path, Encoding.GetEncoding("UTF-8"));
            string text;
            string[] tmp = [];
            while ((text = sr.ReadLine()) != null)
            {
                tmp = text.Split(":");
                if (tmp.Length > 1 && tmp[1] != "")
                {
                    try
                    {
                        if (Enum.TryParse<CustomRoles>(tmp[0], out CustomRoles role))
                        {
                            var color = tmp[1].Trim().TrimStart('#');
                            if (Utils.CheckColorHex(color))
                            { 
                                roleColors[role] = "#"+color;
                            }
                            else TOHE.Logger.Error($"Invalid Hexcolor #{color}", "LoadCustomRoleColor");
                        }
                    }
                    catch (KeyNotFoundException)
                    {
                        TOHE.Logger.Warn($"Invalid Key：{tmp[0]}", "LoadCustomTranslation");
                    }
                }
            }
        }
        else
        {
            TOHE.Logger.Error($"File not found：{filename}", "LoadCustomTranslation");
        }
    }
    public static void LoadRoleColors()
    {
        try
        {
            roleColors = [];
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = "TOHE.Resources.roleColor.json";
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using StreamReader reader = new(stream);
                    
                    string jsonData = reader.ReadToEnd();
                    Dictionary<string, string> jsonDict = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData);
                    foreach (var kvp in jsonDict)
                    {
                        if (Enum.TryParse(kvp.Key, out CustomRoles role))
                        {
                            roleColors[role] = kvp.Value;
                        }
                        else
                        {
                            // Handle invalid or unrecognized enum keys
                            TOHE.Logger.Error($"Invalid enum key: {kvp.Key}", "Reading Role Colors");
                        }
                    }
                }
                else
                {
                    TOHE.Logger.Error($"Embedded resource not found.", "Reading Role Colors");
                }
            }

            foreach (var role in EnumHelper.GetAllValues<CustomRoles>())
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
            if (!Directory.Exists(LANGUAGE_FOLDER_NAME)) Directory.CreateDirectory(LANGUAGE_FOLDER_NAME);
            CreateTemplateRoleColorFile();
            if (File.Exists(@$"./{LANGUAGE_FOLDER_NAME}/RoleColor.dat"))
            {
                UpdateCustomTranslation();
                LoadCustomRoleColor(); 
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
    }
    static void UpdateCustomTranslation()
    {
        string path = @$"./{LANGUAGE_FOLDER_NAME}/RoleColor.dat";
        if (File.Exists(path))
        {
            TOHE.Logger.Info("Updating Custom Role Colors", "UpdateRoleColors");
            try
            {
                List<string> roleList = [];
                using (StreamReader reader = new(path, Encoding.GetEncoding("UTF-8")))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Split the line by ':' to get the first part
                        string[] parts = line.Split(':');

                        // Check if there is at least one part before ':'
                        if (parts.Length >= 1)
                        {
                            // Trim any leading or trailing spaces and add it to the list
                            string role = parts[0].Trim();
                            roleList.Add(role);
                        }
                    }
                }
                var sb = new StringBuilder();
                foreach (var templateRole in roleColors.Keys)
                {
                    if (!roleList.Contains(templateRole.ToString())) sb.Append($"{templateRole}:\n");
                }
                using FileStream fileStream = new(path, FileMode.Append, FileAccess.Write);
                using StreamWriter writer = new(fileStream);
                writer.WriteLine(sb.ToString());

            }
            catch (Exception e)
            {
                TOHE.Logger.Error("An error occurred: " + e.Message, "UpdateRoleColors");
            }
        }
    }

    public static void ExportCustomRoleColors()
    {
        var sb = new StringBuilder();
        foreach (var kvp in roleColors)
        {
            sb.Append($"{kvp.Key.ToString()}:{kvp.Value}\n");
        }
        File.WriteAllText(@$"./{LANGUAGE_FOLDER_NAME}/export_RoleColor.dat", sb.ToString());
    }

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
        HorseMode = Config.Bind("Client Options", "HorseMode", false);
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
        AutoRehost = Config.Bind("Client Options", "AutoRehost", false);

        Logger = BepInEx.Logging.Logger.CreateLogSource("TOHE");
        TOHE.Logger.Enable();
        //TOHE.Logger.Disable("NotifyRoles");
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

        LoadRoleColors(); //loads all the role colors from default and then tries to load custom colors if any.

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
    /*******************************************************
     * Please add all the new roles in alphabetical order *
     ******************************************************/
    //Default
    Crewmate = 0,
    //Impostor(Vanilla)
    Impostor,
    Shapeshifter,
    // Vanilla Remakes
    ImpostorTOHE,
    ShapeshifterTOHE,

    //Impostor
    Sans, //arrogance
    Hacker, //anonymous
    AntiAdminer,
    Bard,
    Berserker,
    Blackmailer,
    Bomber,
    BountyHunter,
    OverKiller, //butcher
    Camouflager,
    Capitalism, //capitalist
    Chronomancer,
    Cleaner,
    EvilDiviner, //Consigliere
    Convict,
    Councillor,
    Crewpostor,
    CursedWolf,
    Dazzler,
    Deathpact,
    Devourer,
    Disperser,
    Eraser,
    Escapee, //escapist
    EvilGuesser,
    EvilMini,
    EvilTracker,
    FireWorks,
    Gangster,
    Godfather,
    Greedier, //greedy
    Hangman,
    Inhibitor,
    Instigator,
    Kamikaze,
    Minimalism, //killing machine
    BallLightning, //Lightning
    Ludopath,
    Lurker,
    Mastermind,
    SerialKiller, //mercenary
    Miner,
    Morphling,
    Mafia, //nemesis
    Assassin, //ninja
    Nuker,
    Parasite,
    Pitfall,
    Puppeteer,
    QuickShooter,
    Refugee,
    RiftMaker,
    Saboteur,
    Scavenger,
    ShapeMaster,
    Sniper,
    Witch, //spellcaster
    ImperiusCurse, //soulcatcher
    Swooper,
    TimeThief,
    BoobyTrap, //trapster
    Trickster,
    Twister,
    Underdog,
    Undertaker,
    Vampire,
    Vampiress,
    Vindicator,
    Visionary,
    Warlock,
    Wildling,
    Zombie,
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
    Addict,
    Admirer,
    Alchemist,
    Bastion,
    Benefactor,
    Bodyguard,
    Captain,
    CyberStar, //celebrity
    Chameleon,
    Cleanser,
    CopyCat,
    Bloodhound, //coroner
    Crusader,
    Detective,
    Counterfeiter, //deceiver
    Deputy,
    Dictator,
    Doctor,
    Enigma,
    Divinator, //FortuneTeller
    Guardian,
    GuessMaster,
    Grenadier,
    ParityCop, //inspector
    Investigator,
    Jailer,
    Judge,
    Keeper,
    SwordsMan, //knight
    Needy, //Lazy guy
    Lighter,
   // Luckey,
    Lookout,
    Marshall,
    Mayor,
    SabotageMaster, //Mechanic
    Medic,
    Mediumshiper, //medium
    Merchant,
    Mole,
    Monarch,
    Mortician,
    NiceGuesser,
    NiceMini,
    Observer,
    Oracle,
    Farseer, //overseer
    DovesOfNeace, //pacifist
    Paranoia, //paranoid
    ChiefOfPolice, //police commisioner
    President,
    Psychic,
    Randomizer,
    Reverie,
    Retributionist,
    Sheriff,
    Snitch,
    SpeedBooster,
    Spiritualist,
    Spy,
    SuperStar,
    Swapper,
    TaskManager,
    Monitor, //telecommunications
    Tracefinder,
    Tracker,
    Transporter,
    TimeManager,
    TimeMaster,
    Veteran,
    Vigilante,
    Witness,

    //Neutral
    Agitater,
    Amnesiac,
    Arsonist,
    Bandit,
    BloodKnight,
    Collector,
    Succubus, //cultist
    CursedSoul,
    Gamer, //demon
    Doomsayer,
    Doppelganger,
    Executioner,
    Totocalcio, //follower
    Glitch,
    God,
    FFF, //hater
    HexMaster,
    Huntsman,
    Imitator,
    Infectious,
    Innocent,
    Jackal,
    Jester,
    Jinx,
    Juggernaut,
    Konan,
    Lawyer,
    Masochist,
    Maverick,
    Medusa,
    Necromancer,
    Opportunist,
    //Occultist,
    Pelican,
    Pestilence,
    Phantom,
    Pickpocket,
    Pirate,
    Pixie,
    PlagueBearer,
    PotionMaster,
    Poisoner,
    Provocateur,
    Pursuer,
    Pyromaniac,
    Quizmaster,
    Revolutionist,
    Romantic,
    RuthlessRomantic,
    Seeker,
    NSerialKiller, //serial killer
    Shaman,
    Shroud,
    Sidekick,
    Solsticer,
    SoulCollector,
    Spiritcaller,
    DarkHide, //stalker
    Sunnyboy,
    Taskinator,
    Terrorist,
    Traitor,
    Mario,//vector
    VengefulRomantic,
    Virus,
    Vulture,
    Werewolf,
    NWitch, //witch
    Workaholic,
    Wraith,

   //two-way camp
    Mini,

    // Sorcerer,
    // Flux,

    //FFA
    Killer,

    //GM
    GM,

    // Sub-role after 500
    NotAssigned = 500,
    Admired,
    Antidote,
    Autopsy,
    Avanger, //avenger
    Aware,
    Bait,
    Bewilder,
    Bloodlust,
    Burst,
    Charmed,
    Circumvent,
    Cleansed,
    Clumsy,
    Contagious,
    Cyber,
    Unreportable, //disregarded
    Diseased,
    DoubleShot,
    Egoist,
    EvilSpirit,
    Flash,
    Fool,
    Fragile,
    Ghoul,
    Gravestone,
    Guesser,
    Hurried,
    Infected,
    Influenced,
    Knighted,
    LastImpostor,
    Lazy,
    Lovers,
    Loyal,
    Lucky,
    Madmate,
    Mare,
    Mimic,
    Mundane,
    Necroview,
    Ntr, //neptune
    Nimble,
    Oblivious,
    Oiiai,
    Onbound,
    Overclocked,
    Rascal,
    Reach,
    Rebound,
    Recruit,
    Repairman,
    Rogue,
    DualPersonality, //Schizophrenic
    Seer,
    Silent,
    Sleuth,
    Soulless,
    TicketsStealer, //stealer
    Stubborn,
    Susceptible,
    Swift,
    Brakar, //tiebreaker
    Torch,
    Trapper,
    Tired,
    Unlucky,
    VoidBallot,
    Watcher,
    //Sunglasses,
    Workhorse,
    Youtuber

   // Reflective,
    //Glow,

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
    Quizmaster = CustomRoles.Quizmaster,
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
    Solsticer = CustomRoles.Solsticer,
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
    Taskinator = CustomRoles.Taskinator,
    Pixie = CustomRoles.Pixie,
    Quizmaster = CustomRoles.Quizmaster,
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
