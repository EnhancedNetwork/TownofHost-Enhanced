using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Il2CppInterop.Runtime.Injection;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
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

    public const string PluginGuid = "com.0xdrmoe.townofhostenhanced";
    public const string PluginVersion = "2024.0724.200.12000"; // YEAR.MMDD.VERSION.CANARYDEV
    public const string PluginDisplayVersion = "2.0.0 Canary 12";
    public const string SupportedVersionAU = "2024.6.18";

    /******************* Change one of the three variables to true before making a release. *******************/
    public static readonly bool Canary = true; // ACTIVE - Latest: V2.0.0 Canary 12
    public static readonly bool fullRelease = false; // INACTIVE - Latest: V1.6.0
    public static readonly bool devRelease = false; // INACTIVE - Latest: V2.0.0 Dev 25

    public static bool hasAccess = true;

    public static readonly bool ShowUpdateButton = true;

    public static readonly bool ShowGitHubButton = true;
    public static readonly string GitHubInviteUrl = "https://github.com/0xDrMoe/TownofHost-Enhanced";

    public static readonly bool ShowDiscordButton = true;
    public static readonly string DiscordInviteUrl = "https://discord.gg/tohe";

    public static readonly bool ShowWebsiteButton = true;
    public static readonly string WebsiteInviteUrl = "https://tohre.dev";
    
    public static readonly bool ShowKofiButton = true;
    public static readonly string kofiInviteUrl = "https://ko-fi.com/TOHE";

    public Harmony Harmony { get; } = new Harmony(PluginGuid);
    public static Version version = Version.Parse(PluginVersion);
    public static BepInEx.Logging.ManualLogSource Logger;
    public static bool hasArgumentException = false;
    public static string ExceptionMessage;
    public static bool ExceptionMessageIsShown = false;
    public static bool AlreadyShowMsgBox = false;
    public static string credentialsText;
    public Coroutines coroutines;
    public static NormalGameOptionsV08 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;
    public static HideNSeekGameOptionsV08 HideNSeekOptions => GameOptionsManager.Instance.currentHideNSeekGameOptions;
    //Client Options
    public static ConfigEntry<string> HideName { get; private set; }
    public static ConfigEntry<string> HideColor { get; private set; }
    public static ConfigEntry<int> MessageWait { get; private set; }

    public static ConfigEntry<bool> UnlockFPS { get; private set; }
    public static ConfigEntry<bool> ShowFPS { get; private set; }
    public static ConfigEntry<bool> EnableGM { get; private set; }
    public static ConfigEntry<bool> AutoStart { get; private set; }
    public static ConfigEntry<bool> DarkTheme { get; private set; }
    public static ConfigEntry<bool> DisableLobbyMusic { get; private set; }
    public static ConfigEntry<bool> ShowTextOverlay { get; private set; }
    public static ConfigEntry<bool> HorseMode { get; private set; }
    public static ConfigEntry<bool> ForceOwnLanguage { get; private set; }
    public static ConfigEntry<bool> ForceOwnLanguageRoleName { get; private set; }
    public static ConfigEntry<bool> EnableCustomButton { get; private set; }
    public static ConfigEntry<bool> EnableCustomSoundEffect { get; private set; }
    public static ConfigEntry<bool> EnableCustomDecorations { get; private set; }
    public static ConfigEntry<bool> SwitchVanilla { get; private set; }

    // Debug
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
    public static ConfigEntry<float> PlayerSpawnTimeOutCooldown { get; private set; }

    public static OptionBackupData RealOptionsData;
    
    public static Dictionary<byte, PlayerState> PlayerStates = [];
    public static readonly Dictionary<byte, string> AllPlayerNames = [];
    public static readonly Dictionary<int, string> AllClientRealNames = [];
    public static readonly Dictionary<byte, CustomRoles> AllPlayerCustomRoles = [];
    public static readonly Dictionary<(byte, byte), string> LastNotifyNames = [];
    public static readonly Dictionary<byte, Action> LateOutfits = [];
    public static readonly Dictionary<byte, Color32> PlayerColors = [];
    public static readonly Dictionary<byte, PlayerState.DeathReason> AfterMeetingDeathPlayers = [];
    public static readonly Dictionary<CustomRoles, string> roleColors = [];
    const string LANGUAGE_FOLDER_NAME = "Language";
    
    public static bool IsFixedCooldown => CustomRoles.Vampire.IsEnable() || CustomRoles.Poisoner.IsEnable();
    public static float RefixCooldownDelay = 0f;
    public static NetworkedPlayerInfo LastVotedPlayerInfo;
    public static string LastVotedPlayer;
    public static readonly HashSet<byte> ResetCamPlayerList = [];
    public static readonly HashSet<byte> winnerList = [];
    public static readonly HashSet<string> winnerNameList = [];
    public static readonly HashSet<int> clientIdList = [];
    public static readonly List<(string, byte, string)> MessagesToSend = [];
    public static readonly Dictionary<string, int> PlayerQuitTimes = [];
    public static bool isChatCommand = false;
    public static bool MeetingIsStarted = false;

    public static readonly HashSet<byte> TasklessCrewmate = [];
    public static readonly HashSet<byte> OverDeadPlayerList = [];
    public static readonly HashSet<byte> UnreportableBodies = [];
    public static readonly Dictionary<byte, float> AllPlayerKillCooldown = [];
    public static readonly Dictionary<byte, Vent> LastEnteredVent = [];
    public static readonly Dictionary<byte, Vector2> LastEnteredVentLocation = [];
    public static readonly Dictionary<int, int> SayStartTimes = [];
    public static readonly Dictionary<int, int> SayBanwordsTimes = [];
    public static readonly Dictionary<byte, float> AllPlayerSpeed = [];
    public static readonly Dictionary<byte, int> GuesserGuessed = [];
    public static readonly Dictionary<byte, long> AllKillers = [];
    public static readonly Dictionary<byte, bool> CheckShapeshift = [];
    public static readonly Dictionary<byte, byte> ShapeshiftTarget = [];

    public static bool isLoversDead = true;
    public static readonly HashSet<PlayerControl> LoversPlayers = [];

    public static bool DoBlockNameChange = false;
    public static int updateTime;
    public const float MinSpeed = 0.0001f;
    public static int AliveImpostorCount;
    public static bool VisibleTasksCount = false;
    public static bool AssignRolesIsStarted = false;
    public static string HostRealName = "";
    public static bool introDestroyed = false;
    public static int DiscussionTime;
    public static int VotingTime;
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

    public static PlayerControl[] AllPlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null).ToArray();
    public static PlayerControl[] AllAlivePlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.IsAlive() && !p.Data.Disconnected && !Pelican.IsEaten(p.PlayerId)).ToArray();

    public static Main Instance;

    public static string OverrideWelcomeMsg = "";
    public static int HostClientId;
    public static Dictionary<byte, List<int>> GuessNumber = [];

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

    public void StartCoroutine(System.Collections.IEnumerator coroutine)
    {
        if (coroutine == null)
        {
            return;
        }
        coroutines.StartCoroutine(coroutine.WrapToIl2Cpp());
    }

    public void StopCoroutine(System.Collections.IEnumerator coroutine)
    {
        if (coroutine == null)
        {
            return;
        }
        coroutines.StopCoroutine(coroutine.WrapToIl2Cpp());
    }

    public void StopAllCoroutines()
    {
        coroutines.StopAllCoroutines();
    }

    public static void LoadRoleColors()
    {
        try
        {
            roleColors.Clear();
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
                switch (role.GetCustomRoleTeam())
                {
                    case Custom_Team.Impostor:
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
    public static void LoadRoleClasses()
    {
        TOHE.Logger.Info("Loading All RoleClasses...", "LoadRoleClasses");
        try
        {
            var RoleTypes = Assembly.GetAssembly(typeof(RoleBase))!
                .GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(RoleBase)));

            CustomRolesHelper.DuplicatedRoles = new Dictionary<CustomRoles, Type>
            {
                { CustomRoles.NiceMini, typeof(Mini) },
                { CustomRoles.EvilMini, typeof(Mini) }
            };


            foreach (var role in CustomRolesHelper.AllRoles.Where(x => x < CustomRoles.NotAssigned))
            {
                if (!CustomRolesHelper.DuplicatedRoles.TryGetValue(role, out Type roleType))
                {
                    roleType = RoleTypes.FirstOrDefault(x => x.Name.Equals(role.ToString(), StringComparison.OrdinalIgnoreCase)) ?? typeof(DefaultSetup);
                }

                CustomRoleManager.RoleClass.Add(role, (RoleBase)Activator.CreateInstance(roleType));
            }

            TOHE.Logger.Info("RoleClasses Loaded Successfully", "LoadRoleClasses");
        }
        catch (Exception err)
        {
            TOHE.Logger.Error($"Error at LoadRoleClasses: {err}", "LoadRoleClasses");
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

        UnlockFPS = Config.Bind("Client Options", "UnlockFPS", false);
        ShowFPS = Config.Bind("Client Options", "ShowFPS", false);
        EnableGM = Config.Bind("Client Options", "EnableGM", false);
        AutoStart = Config.Bind("Client Options", "AutoStart", false);
        DarkTheme = Config.Bind("Client Options", "DarkTheme", false);
        DisableLobbyMusic = Config.Bind("Client Options", "DisableLobbyMusic", false);
        ShowTextOverlay = Config.Bind("Client Options", "ShowTextOverlay", false);
        HorseMode = Config.Bind("Client Options", "HorseMode", false);
        ForceOwnLanguage = Config.Bind("Client Options", "ForceOwnLanguage", false);
        ForceOwnLanguageRoleName = Config.Bind("Client Options", "ForceOwnLanguageRoleName", false);
        EnableCustomButton = Config.Bind("Client Options", "EnableCustomButton", true);
        EnableCustomSoundEffect = Config.Bind("Client Options", "EnableCustomSoundEffect", true);
        EnableCustomDecorations = Config.Bind("Client Options", "EnableCustomDecorations", true);
        SwitchVanilla = Config.Bind("Client Options", "SwitchVanilla", false);

        // Debug
        VersionCheat = Config.Bind("Client Options", "VersionCheat", false);
        GodMode = Config.Bind("Client Options", "GodMode", false);
        AutoRehost = Config.Bind("Client Options", "AutoRehost", false);

        Logger = BepInEx.Logging.Logger.CreateLogSource("TOHE");
        coroutines = AddComponent<Coroutines>();
        TOHE.Logger.Enable();
        //TOHE.Logger.Disable("NotifyRoles");
        TOHE.Logger.Disable("SwitchSystem");
        TOHE.Logger.Disable("ModNews");
        TOHE.Logger.Disable("CustomRpcSender");
        if (!DebugModeManager.AmDebugger)
        {
            TOHE.Logger.Disable("2018k");
            TOHE.Logger.Disable("Github");
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
            //TOHE.Logger.Disable("DoNotifyRoles");
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
        PlayerSpawnTimeOutCooldown = Config.Bind("Other", "PlayerSpawnTimeOutCooldown", (float)3);

        hasArgumentException = false;
        ExceptionMessage = "";

        LoadRoleClasses();
        LoadRoleColors(); //loads all the role colors from default and then tries to load custom colors if any.

        CustomWinnerHolder.Reset();
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

    // Crewmate(Vanilla)
    Crewmate = 0,
    Engineer,
    GuardianAngel,
    Noisemaker,
    Scientist,
    Tracker,

    // Impostor(Vanilla)
    Impostor,
    Phantom,
    Shapeshifter,

    // Crewmate Vanilla Remakes
    CrewmateTOHE,
    EngineerTOHE,
    GuardianAngelTOHE,
    NoisemakerTOHE,
    ScientistTOHE,
    TrackerTOHE,

    // Impostor Vanilla Remakes
    ImpostorTOHE,
    PhantomTOHE,
    ShapeshifterTOHE,

    // Impostor Ghost
    Bloodmoon,
    Minion,

    //Impostor
    Anonymous,
    AntiAdminer,
    Arrogance,
    Bard,
    Berserker,
    Blackmailer,
    Bomber,
    BountyHunter,
    Butcher,
    Camouflager,
    Chronomancer,
    Cleaner,
    Consigliere,
    Councillor,
    Crewpostor,
    CursedWolf,
    Dazzler,
    Deathpact,
    Devourer,
    Disperser,
    DollMaster,
    Eraser,
    Escapist,
    EvilGuesser,
    EvilHacker,
    EvilMini,
    EvilTracker,
    Fireworker,
    Gangster,
    Godfather,
    Greedy,
    Hangman,
    Inhibitor,
    Instigator,
    Kamikaze,
    KillingMachine,
    Lightning,
    Ludopath,
    Lurker,
    Mastermind,
    Mercenary,
    Miner,
    Morphling,
    Nemesis,
    Ninja,
    Parasite,
    Penguin,
    Pitfall,
    Puppeteer,
    QuickShooter,
    Refugee,
    RiftMaker,
    Saboteur,
    Scavenger,
    ShapeMaster,
    Sniper,
    SoulCatcher,
    Stealth,
    Swooper,
    TimeThief,
    Trapster,
    Trickster,
    Twister,
    Underdog,
    Undertaker,
    Vampire,
    Vindicator,
    Visionary,
    Warlock,
    Wildling,
    Witch,
    Zombie,

    //Crewmate Ghost
    Ghastly,
    Hawk,
    Warden,

    //Crewmate
    Addict,
    Admirer,
    Alchemist,
    Bastion,
    Benefactor,
    Bodyguard,
    Captain,
    Celebrity, 
    Chameleon,
    ChiefOfPolice, //police commisioner ///// UNUSED
    Cleanser,
    CopyCat,
    Coroner, 
    Crusader,
    Deceiver, 
    Deputy,
    Detective,
    Dictator,
    Doctor,
    Enigma,
    FortuneTeller, 
    Grenadier,
    Guardian,
    GuessMaster,
    Inspector, 
    Investigator,
    Jailer,
    Judge,
    Keeper,
    Knight, 
    LazyGuy,
    Lighter,
    Lookout,
    Marshall,
    Mayor,
    Mechanic, 
    Medic,
    Medium,
    Merchant,
    Mole,
    Monarch,
    Mortician,
    NiceGuesser,
    NiceMini,
    Observer,
    Oracle,
    Overseer, 
    Pacifist, 
    President,
    Psychic,
    Randomizer,
    Retributionist,
    Reverie,
    Sheriff,
    Snitch,
    SpeedBooster,
    Spiritualist,
    Spy,
    SuperStar,
    Swapper,
    TaskManager,
    Telecommunication,
    TimeManager,
    TimeMaster,
    Tracefinder,
    Transporter,
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
    Cultist, 
    CursedSoul,
    Demon, 
    Doomsayer,
    Doppelganger,
    Executioner,
    Follower,
    Glitch,
    God,
    Hater,
    HexMaster,
    Huntsman,
    Imitator,
    Infectious,
    Innocent,
    Jackal,
    Jester,
    Jinx,
    Juggernaut,
    Lawyer,
    Maverick,
    Medusa,
    Necromancer,
    Opportunist,
    Pelican,
    Pestilence,
    Pickpocket,
    Pirate,
    Pixie,
    PlagueBearer,
    PlagueDoctor,
    Poisoner,
    PotionMaster,
    Provocateur,
    PunchingBag,
    Pursuer,
    Pyromaniac,
    Quizmaster,
    Revolutionist,
    Romantic,
    RuthlessRomantic,
    SchrodingersCat,
    Seeker,
    SerialKiller,
    Shaman,
    Shroud,
    Sidekick,
    Solsticer,
    SoulCollector,
    Specter,
    Spiritcaller,
    Stalker,
    Sunnyboy,
    Taskinator,
    Terrorist,
    Traitor,
    Vector,
    VengefulRomantic,
    Virus,
    Vulture,
    Werewolf,
    Workaholic,
    Wraith,

    //two-way camp
    Mini,

    //FFA
    Killer,

    //GM
    GM,

    // Sub-role after 500
    NotAssigned = 500,

    // Add-ons
    Admired,
    Antidote,
    Autopsy,
    Avanger,
    Aware,
    Bait,
    Bewilder,
    Bloodthirst,
    Burst,
    Charmed,
    Circumvent,
    Cleansed,
    Clumsy,
    Contagious,
    Cyber,
    Diseased,
    DoubleShot,
    Egoist,
    EvilSpirit,
    Flash,
    Fool,
    Fragile,
    Ghoul,
    Glow,
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
    Nimble,
    Oblivious,
    Oiiai,
    Onbound,
    Overclocked,
    Paranoia,
    Radar,
    Rainbow,
    Rascal,
    Reach,
    Rebound,
    Recruit,
    Seer,
    Silent,
    Sleuth,
    Soulless,
    Statue,
    Stubborn,
    Susceptible,
    Swift,
    Tiebreaker,
    TicketsStealer, //stealer
    Torch,
    Trapper,
    Tricky,
    Tired,
    Unlucky,
    Unreportable, //disregarded
    VoidBallot,
    Watcher,
    Workhorse,
    Youtuber   
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
    Vector = CustomRoles.Vector,
    Innocent = CustomRoles.Innocent,
    Pelican = CustomRoles.Pelican,
    Youtuber = CustomRoles.Youtuber,
    Egoist = CustomRoles.Egoist,
    Demon = CustomRoles.Demon,
    Stalker = CustomRoles.Stalker,
    Workaholic = CustomRoles.Workaholic,
    Collector = CustomRoles.Collector,
    BloodKnight = CustomRoles.BloodKnight,
    Poisoner = CustomRoles.Poisoner,
    HexMaster = CustomRoles.HexMaster,
    Quizmaster = CustomRoles.Quizmaster,
    Cultist = CustomRoles.Cultist,
    Wraith = CustomRoles.Wraith,
    Bandit = CustomRoles.Bandit,
    Pirate = CustomRoles.Pirate,
    SerialKiller = CustomRoles.SerialKiller,
    Werewolf = CustomRoles.Werewolf,
    Necromancer = CustomRoles.Necromancer,
    Huntsman = CustomRoles.Huntsman,
    Juggernaut = CustomRoles.Juggernaut,
    Infectious = CustomRoles.Infectious,
    Virus = CustomRoles.Virus,
    Specter = CustomRoles.Specter,
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
    PlagueDoctor = CustomRoles.PlagueDoctor,
    PunchingBag = CustomRoles.PunchingBag,
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
    Hater = CustomRoles.Hater,
    Provocateur = CustomRoles.Provocateur,
    Sunnyboy = CustomRoles.Sunnyboy,
    Follower = CustomRoles.Follower,
    Romantic = CustomRoles.Romantic,
    VengefulRomantic = CustomRoles.VengefulRomantic,
    RuthlessRomantic = CustomRoles.RuthlessRomantic,
    Jackal = CustomRoles.Jackal,
    Sidekick = CustomRoles.Sidekick,
    Pursuer = CustomRoles.Pursuer,
    Specter = CustomRoles.Specter,
    Maverick = CustomRoles.Maverick,
    Shaman = CustomRoles.Shaman,
    Taskinator = CustomRoles.Taskinator,
    Pixie = CustomRoles.Pixie,
    Quizmaster = CustomRoles.Quizmaster,
    SchrodingersCat = CustomRoles.SchrodingersCat,
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
public class Coroutines : MonoBehaviour
{
}
