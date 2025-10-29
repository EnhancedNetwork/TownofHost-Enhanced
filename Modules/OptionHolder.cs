using System;
using TOHE.Modules;
using TOHE.Roles.AddOns;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
using UnityEngine;

namespace TOHE;

[Obfuscation(Exclude = true)]
[Flags]
public enum CustomGameMode
{
    Standard = 0x01,
    FFA = 0x02,
    CandR = 0x03,
    UltimateTeam = 0x04,
    TrickorTreat = 0x05,
    HidenSeekTOHO = 0x08, // HidenSeekTOHO must be after other Gamemodes
    All = int.MaxValue
}

[HarmonyPatch]
public static class Options
{
    [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.Initialize)), HarmonyPostfix]
    public static void OptionsLoadStart_Postfix()
    {
        Logger.Msg("Mod option loading start", "Load Options");
        try
        {
            Main.Instance.StartCoroutine(CoLoadOptions());
        }
        catch (Exception error)
        {
            Logger.Error($"Fatal error after loading mod options: {error}", "Load Options");
        }
    }

    // Presets
    [Obfuscation(Exclude = true)]
    private static readonly string[] presets =
    [
        Main.Preset1.Value, Main.Preset2.Value, Main.Preset3.Value,
        Main.Preset4.Value, Main.Preset5.Value
    ];

    // Custom Game Mode
    public static OptionItem GameMode;
    public static CustomGameMode CurrentGameMode
        => GameMode.GetInt() switch
        {
            1 => CustomGameMode.FFA,
            2 => CustomGameMode.CandR,
            3 => CustomGameMode.UltimateTeam,
            4 => CustomGameMode.TrickorTreat,
            5 => CustomGameMode.HidenSeekTOHO, // HidenSeekTOHO must be after other Gamemodes
            _ => CustomGameMode.Standard
        };
    public static int GetGameModeInt(CustomGameMode mode)
        => mode switch
        {
            CustomGameMode.FFA => 1,
            CustomGameMode.CandR => 2,
            CustomGameMode.HidenSeekTOHO => 3, // HidenSeekTOHO must be after other Gamemodes
            _ => 0
        };

    public static readonly string[] gameModes =
    [
        "Standard",
        "FFA",
        "C&R",
        "UltimateTeam",
        "TrickorTreat",

        "Hide&SeekTOHO", // HidenSeekTOHO must be after other Gamemodes
    ];



    // 役職数・確率
    public static Dictionary<CustomRoles, int> roleCounts;
    public static Dictionary<CustomRoles, float> roleSpawnChances;
    public static Dictionary<CustomRoles, OptionItem> CustomRoleCounts;
    public static Dictionary<CustomRoles, OptionItem> CustomGhostRoleCounts;
    public static Dictionary<CustomRoles, StringOptionItem> CustomRoleSpawnChances;
    public static Dictionary<CustomRoles, IntegerOptionItem> CustomAdtRoleSpawnRate;

    public static readonly Dictionary<CustomRoles, (OptionItem Imp, OptionItem Neutral, OptionItem Crew, OptionItem Coven)> AddonCanBeSettings = [];
    [Obfuscation(Exclude = true)]
    public enum SpawnChance
    {
        Chance0,
        Chance5,
        Chance10,
        Chance15,
        Chance20,
        Chance25,
        Chance30,
        Chance35,
        Chance40,
        Chance45,
        Chance50,
        Chance55,
        Chance60,
        Chance65,
        Chance70,
        Chance75,
        Chance80,
        Chance85,
        Chance90,
        Chance95,
        Chance100,
    }
    [Obfuscation(Exclude = true)]
    private enum RatesZeroOne
    {
        RoleOff,
        RoleRate,
    }
    public static readonly string[] CheatResponsesName =
    [
        "Ban", "Kick", "NoticeMe","NoticeEveryone", "TempBan", "OnlyCancel"
    ];
    public static readonly string[] ConfirmEjectionsMode =
    [
        "ConfirmEjections.None",
        "ConfirmEjections.Team",
        "ConfirmEjections.Role"
    ];
    public static readonly string[] CamouflageMode =
    [
        "CamouflageMode.Default",
        "CamouflageMode.Host",
        "CamouflageMode.Random",
        "CamouflageMode.OnlyRandomColor",
        "CamouflageMode.Karpe",
        "CamouflageMode.Lauryn",
        "CamouflageMode.Lime",
        "CamouflageMode.Pyro",
        "CamouflageMode.ryuk",
        "CamouflageMode.Gurge44",
        "CamouflageMode.TommyXL",
        "CamouflageMode.Sarha"
    ];
    [Obfuscation(Exclude = true)]
    public enum QuickChatSpamMode
    {
        QuickChatSpam_Disabled,
        QuickChatSpam_How2PlayNormal,
        QuickChatSpam_How2PlayHidenSeek,
        QuickChatSpam_Random20,
        QuickChatSpam_EzHacked,
    };
    [Obfuscation(Exclude = true)]
    public enum ShortAddOnNamesMode
    {
        ShortAddOnNamesMode_Disable,
        ShortAddOnNamesMode_Always,
        ShortAddOnNamesMode_OnlyInMeeting,
        ShortAddOnNamesMode_OnlyInGame
    }

    public static OptionItem BastionAbilityUseGainWithEachTaskCompleted;
    public static OptionItem ChameleonAbilityUseGainWithEachTaskCompleted;
    public static OptionItem ContaminatorAbilityUseGainWithEachTaskCompleted;
    public static OptionItem CoronerAbilityUseGainWithEachTaskCompleted;
    public static OptionItem FortuneTellerAbilityUseGainWithEachTaskCompleted;
    public static OptionItem SentinelAbilityUseGainWithEachTaskCompleted;
    public static OptionItem GrenadierAbilityUseGainWithEachTaskCompleted;
    public static OptionItem InspectorAbilityUseGainWithEachTaskCompleted;
    public static OptionItem LighterAbilityUseGainWithEachTaskCompleted;
    public static OptionItem MechanicAbilityUseGainWithEachTaskCompleted;
    public static OptionItem MediumAbilityUseGainWithEachTaskCompleted;
    public static OptionItem OracleAbilityUseGainWithEachTaskCompleted;
    public static OptionItem PacifistAbilityUseGainWithEachTaskCompleted;
    public static OptionItem SpyAbilityUseGainWithEachTaskCompleted;
    public static OptionItem VentguardAbilityUseGainWithEachTaskCompleted;
    public static OptionItem VeteranAbilityUseGainWithEachTaskCompleted;
    public static OptionItem TimeMasterAbilityUseGainWithEachTaskCompleted;
    public static OptionItem PresidentAbilityUseGainWithEachTaskCompleted;
    public static OptionItem KeeperAbilityUseGainWithEachTaskCompleted;
    public static OptionItem ConstableAbilityUseGainWithEachTaskCompleted;
    public static OptionItem CleanserAbilityUseGainWithEachTaskCompleted;

    //public static OptionItem EnableGM;
    public static float DefaultKillCooldown = Main.NormalOptions?.KillCooldown ?? 20;
    public static OptionItem GhostsDoTasks;
    public static Dictionary<AddonTypes, List<CustomRoles>> GroupedAddons = [];


    // ------------ System Settings Tab ------------
    public static OptionItem BypassRateLimitAC;
    public static OptionItem GradientTagsOpt;
    public static OptionItem EnableKillerLeftCommand;
    public static OptionItem ShowMadmatesInLeftCommand;
    public static OptionItem ShowApocalypseInLeftCommand;
    public static OptionItem ShowCovenInLeftCommand;
    public static OptionItem SeeEjectedRolesInMeeting;

    public static OptionItem KickLowLevelPlayer;
    public static OptionItem TempBanLowLevelPlayer;

    public static OptionItem ApplyAllowList;
    public static OptionItem AllowOnlyWhiteList;

    public static OptionItem KickOtherPlatformPlayer;
    public static OptionItem OptKickAndroidPlayer;
    public static OptionItem OptKickIphonePlayer;
    public static OptionItem OptKickXboxPlayer;
    public static OptionItem OptKickPlayStationPlayer;
    public static OptionItem OptKickNintendoPlayer;

    public static OptionItem KickPlayerFriendCodeInvalid;
    public static OptionItem TempBanPlayerFriendCodeInvalid;

    public static OptionItem AutoKickStart;
    public static OptionItem AutoKickStartTimes;
    public static OptionItem AutoKickStartAsBan;

    public static OptionItem TempBanPlayersWhoKeepQuitting;
    public static OptionItem QuitTimesTillTempBan;

    public static OptionItem ApplyVipList;
    public static OptionItem ApplyDenyNameList;
    public static OptionItem ApplyBanList;
    public static OptionItem ApplyModeratorList;
    public static OptionItem AllowSayCommand;
    public static OptionItem AllowStartCommand;
    public static OptionItem StartCommandMinCountdown;
    public static OptionItem StartCommandMaxCountdown;

    //public static OptionItem ApplyReminderMsg;
    //public static OptionItem TimeForReminder;
    //public static OptionItem AutoKickStopWords;
    //public static OptionItem AutoKickStopWordsTimes;
    //public static OptionItem AutoKickStopWordsAsBan;
    //public static OptionItem AutoWarnStopWords;

    public static OptionItem MinWaitAutoStart;
    public static OptionItem MaxWaitAutoStart;
    public static OptionItem PlayerAutoStart;
    public static OptionItem AutoStartTimer;
    public static OptionItem ImmediateAutoStart;
    public static OptionItem ImmediateStartTimer;
    public static OptionItem StartWhenPlayersReach;
    public static OptionItem StartWhenTimerLowerThan;
    public static OptionItem StartWhenTimePassed;

    public static OptionItem AutoPlayAgain;
    public static OptionItem AutoPlayAgainCountdown;

    public static OptionItem EnableVoteCommand;
    public static OptionItem ShouldVoteCmdsSpamChat;

    //public static OptionItem ShowLobbyCode;
    public static OptionItem LowLoadMode;
    public static OptionItem LowLoadDelayUpdateNames;
    public static OptionItem EndWhenPlayerBug;
    public static OptionItem HideExileChat;
    public static OptionItem RemovePetsAtDeadPlayers;

    public static OptionItem CheatResponses;
    public static OptionItem NewHideMsg;

    public static OptionItem AutoDisplayKillLog;
    public static OptionItem AutoDisplayLastRoles;
    public static OptionItem AutoDisplayLastResult;
    public static OptionItem OldKillLog;

    public static OptionItem SuffixMode;
    public static OptionItem HideHostText;
    public static OptionItem HideAllTagsAndText;
    public static OptionItem HideGameSettings;

    public static OptionItem PlayerCanSetColor;
    public static OptionItem PlayerCanSetName;
    public static OptionItem PlayerCanUseQuitCommand;
    public static OptionItem PlayerCanUseTP;
    public static OptionItem CanPlayMiniGames;
    public static OptionItem FormatNameMode;
    public static OptionItem DisableEmojiName;
    //public static OptionItem ColorNameMode;
    public static OptionItem ChangeNameToRoleInfo;
    public static OptionItem SendRoleDescriptionFirstMeeting;

    public static OptionItem NoGameEnd;
    public static OptionItem AllowConsole;
    //public static OptionItem DisableAntiBlackoutProtects;

    public static OptionItem RoleAssigningAlgorithm;
    public static OptionItem KPDCamouflageMode;
    public static OptionItem EnableUpMode;

    // ------------ Game Settings Tab ------------

    // Hide & Seek Setting
    public static OptionItem MaxImpostorsHnS;

    // Confirm Ejection
    public static OptionItem CEMode;
    public static OptionItem ShowImpRemainOnEject;
    public static OptionItem ShowNKRemainOnEject;
    public static OptionItem ShowNARemainOnEject;
    public static OptionItem ShowCovenRemainOnEject;
    public static OptionItem ShowTeamNextToRoleNameOnEject;
    public static OptionItem ConfirmEgoistOnEject;
    public static OptionItem ConfirmLoversOnEject;
    //public static OptionItem ConfirmSidekickOnEject;
    //public static OptionItem ExtendedEjections;

    // Maps Settings
    public static OptionItem RandomMapsMode;
    public static OptionItem SkeldChance;
    public static OptionItem MiraChance;
    public static OptionItem PolusChance;
    public static OptionItem DleksChance;
    public static OptionItem AirshipChance;
    public static OptionItem FungleChance;
    public static OptionItem UseMoreRandomMapSelection;

    public static OptionItem MapModification;
    public static OptionItem AirshipVariableElectrical;
    public static OptionItem DisableAirshipMovingPlatform;
    public static OptionItem DisableSporeTriggerOnFungle;
    public static OptionItem DisableZiplineOnFungle;
    public static OptionItem DisableZiplineFromTop;
    public static OptionItem DisableZiplineFromUnder;

    public static OptionItem ResetDoorsEveryTurns;
    public static OptionItem DoorsResetMode;

    public static OptionItem ChangeDecontaminationTime;
    public static OptionItem DecontaminationTimeOnMiraHQ;
    public static OptionItem DecontaminationTimeOnPolus;

    public static OptionItem EnableHalloweenDecorations;
    public static OptionItem HalloweenDecorationsSkeld;
    public static OptionItem HalloweenDecorationsMira;
    public static OptionItem HalloweenDecorationsDleks;
    public static OptionItem EnableBirthdayDecorationSkeld;
    public static OptionItem RandomBirthdayAndHalloweenDecorationSkeld;

    // Sabotage Settings
    public static OptionItem CommsCamouflage;
    public static OptionItem DisableOnSomeMaps;
    public static OptionItem DisableOnSkeld;
    public static OptionItem DisableOnMira;
    public static OptionItem DisableOnPolus;
    public static OptionItem DisableOnDleks;
    public static OptionItem DisableOnAirship;
    public static OptionItem DisableOnFungle;
    public static OptionItem DisableReportWhenCC;

    public static OptionItem SabotageCooldownControl;
    public static OptionItem SabotageCooldown;

    public static OptionItem SabotageTimeControl;
    public static OptionItem SkeldReactorTimeLimit;
    public static OptionItem SkeldO2TimeLimit;
    public static OptionItem MiraReactorTimeLimit;
    public static OptionItem MiraO2TimeLimit;
    public static OptionItem PolusReactorTimeLimit;
    public static OptionItem AirshipReactorTimeLimit;
    public static OptionItem FungleReactorTimeLimit;
    public static OptionItem FungleMushroomMixupDuration;

    public static OptionItem LightsOutSpecialSettings;
    public static OptionItem BlockDisturbancesToSwitches;
    public static OptionItem DisableAirshipViewingDeckLightsPanel;
    public static OptionItem DisableAirshipGapRoomLightsPanel;
    public static OptionItem DisableAirshipCargoLightsPanel;

    // Disable
    public static OptionItem DisableShieldAnimations;
    public static OptionItem DisableKillAnimationOnGuess;
    public static OptionItem DisableVanillaRoles;
    public static OptionItem DisableTaskWin;
    public static OptionItem DisableTaskWinIfAllCrewsAreDead;
    public static OptionItem DisableTaskWinIfAllCrewsAreConverted;
    public static OptionItem DisableMeeting;
    public static OptionItem DisableSabotage;
    public static OptionItem DisableCloseDoor;

    public static OptionItem DisableDevices;
    public static OptionItem DisableSkeldDevices;
    public static OptionItem DisableSkeldAdmin;
    public static OptionItem DisableSkeldCamera;
    public static OptionItem DisableMiraHQDevices;
    public static OptionItem DisableMiraHQAdmin;
    public static OptionItem DisableMiraHQDoorLog;
    public static OptionItem DisablePolusDevices;
    public static OptionItem DisablePolusAdmin;
    public static OptionItem DisablePolusCamera;
    public static OptionItem DisablePolusVital;
    public static OptionItem DisableAirshipDevices;
    public static OptionItem DisableAirshipCockpitAdmin;
    public static OptionItem DisableAirshipRecordsAdmin;
    public static OptionItem DisableAirshipCamera;
    public static OptionItem DisableAirshipVital;
    public static OptionItem DisableFungleDevices;
    public static OptionItem DisableFungleBinoculars;
    public static OptionItem DisableFungleVital;
    public static OptionItem DisableDevicesIgnoreConditions;
    public static OptionItem DisableDevicesIgnoreImpostors;
    public static OptionItem DisableDevicesIgnoreNeutrals;
    public static OptionItem DisableDevicesIgnoreCoven;
    public static OptionItem DisableDevicesIgnoreCrewmates;
    public static OptionItem DisableDevicesIgnoreAfterAnyoneDied;

    public static OptionItem DisableSpectateCommand;

    // Meeting Settings
    public static OptionItem SyncButtonMode;
    public static OptionItem SyncedButtonCount;
    public static int UsedButtonCount = 0;

    public static OptionItem AllAliveMeeting;
    public static OptionItem AllAliveMeetingTime;

    public static OptionItem AdditionalEmergencyCooldown;
    public static OptionItem AdditionalEmergencyCooldownThreshold;
    public static OptionItem AdditionalEmergencyCooldownTime;

    public static OptionItem VoteMode;
    public static OptionItem WhenSkipVote;
    public static OptionItem WhenSkipVoteIgnoreFirstMeeting;
    public static OptionItem WhenSkipVoteIgnoreNoDeadBody;
    public static OptionItem WhenSkipVoteIgnoreEmergency;
    public static OptionItem WhenNonVote;
    public static OptionItem WhenTie;

    // Other
    public static OptionItem LadderDeath;
    public static OptionItem LadderDeathChance;

    public static OptionItem FixFirstKillCooldown;
    public static OptionItem ChangeFirstKillCooldown;
    public static OptionItem FixKillCooldownValue;
    public static OptionItem ShieldPersonDiedFirst;
    public static OptionItem ShowShieldedPlayerToAll;
    public static OptionItem RemoveShieldOnFirstDead;
    public static OptionItem ShieldedCanUseKillButton;
    public static OptionItem EveryoneCanSeeDeathReason;

    public static OptionItem KillFlashDuration;
    public static OptionItem NonCrewRandomCommonTasks;

    // Ghost
    public static OptionItem GhostIgnoreTasks;
    public static OptionItem GhostCanSeeOtherRoles;
    public static OptionItem PreventSeeRolesImmediatelyAfterDeath;
    public static OptionItem GhostCanSeeOtherVotes;
    public static OptionItem GhostCanSeeDeathReason;
    public static OptionItem ConvertedCanBecomeGhost;
    public static OptionItem NeutralCanBecomeGhost;
    public static OptionItem MaxImpGhost;
    public static OptionItem MaxCrewGhost;
    public static OptionItem MaxNeutralGhost;
    public static OptionItem DefaultAngelCooldown;

    // Modifiers
    public static OptionItem EnableAnomalies;
    public static OptionItem ClownFest;
    public static OptionItem Retrial;
    public static OptionItem NewYear;
    public static OptionItem Holiday;
    public static OptionItem Shuffle;
    public static OptionItem CrazyColors;
    public static OptionItem ColorChangeCoolDown;
    public static OptionItem AnomalyMeetingPCT;
    // public static OptionItem EnableWills;


    // ------------ Task Management Tab ------------

    // Disable Tasks
    public static OptionItem DisableShortTasks;
    public static OptionItem DisableCleanVent;
    public static OptionItem DisableCalibrateDistributor;
    public static OptionItem DisableChartCourse;
    public static OptionItem DisableStabilizeSteering;
    public static OptionItem DisableCleanO2Filter;
    public static OptionItem DisableUnlockManifolds;
    public static OptionItem DisablePrimeShields;
    public static OptionItem DisableMeasureWeather;
    public static OptionItem DisableBuyBeverage;
    public static OptionItem DisableAssembleArtifact;
    public static OptionItem DisableSortSamples;
    public static OptionItem DisableProcessData;
    public static OptionItem DisableRunDiagnostics;
    public static OptionItem DisableRepairDrill;
    public static OptionItem DisableAlignTelescope;
    public static OptionItem DisableRecordTemperature;
    public static OptionItem DisableFillCanisters;
    public static OptionItem DisableMonitorTree;
    public static OptionItem DisableStoreArtifacts;
    public static OptionItem DisablePutAwayPistols;
    public static OptionItem DisablePutAwayRifles;
    public static OptionItem DisableMakeBurger;
    public static OptionItem DisableCleanToilet;
    public static OptionItem DisableDecontaminate;
    public static OptionItem DisableSortRecords;
    public static OptionItem DisableFixShower;
    public static OptionItem DisablePickUpTowels;
    public static OptionItem DisablePolishRuby;
    public static OptionItem DisableDressMannequin;

    public static OptionItem DisableCommonTasks;
    public static OptionItem DisableSwipeCard;
    public static OptionItem DisableFixWiring;
    public static OptionItem DisableEnterIdCode;
    public static OptionItem DisableInsertKeys;
    public static OptionItem DisableScanBoardingPass;

    public static OptionItem DisableLongTasks;
    public static OptionItem DisableSubmitScan;
    public static OptionItem DisableUnlockSafe;
    public static OptionItem DisableStartReactor;
    public static OptionItem DisableResetBreaker;
    public static OptionItem DisableAlignEngineOutput;
    public static OptionItem DisableInspectSample;
    public static OptionItem DisableEmptyChute;
    public static OptionItem DisableClearAsteroids;
    public static OptionItem DisableWaterPlants;
    public static OptionItem DisableOpenWaterways;
    public static OptionItem DisableReplaceWaterJug;
    public static OptionItem DisableRebootWifi;
    public static OptionItem DisableDevelopPhotos;
    public static OptionItem DisableRewindTapes;
    public static OptionItem DisableStartFans;

    public static OptionItem DisableOtherTasks;
    public static OptionItem DisableUploadData;
    public static OptionItem DisableEmptyGarbage;
    public static OptionItem DisableFuelEngines;
    public static OptionItem DisableDivertPower;
    public static OptionItem DisableActivateWeatherNodes;
    public static OptionItem DisableRoastMarshmallow;
    public static OptionItem DisableCollectSamples;
    public static OptionItem DisableReplaceParts;
    public static OptionItem DisableCollectVegetables;
    public static OptionItem DisableMineOres;
    public static OptionItem DisableExtractFuel;
    public static OptionItem DisableCatchFish;
    public static OptionItem DisablePolishGem;
    public static OptionItem DisableHelpCritter;
    public static OptionItem DisableHoistSupplies;
    public static OptionItem DisableFixAntenna;
    public static OptionItem DisableBuildSandcastle;
    public static OptionItem DisableCrankGenerator;
    public static OptionItem DisableMonitorMushroom;
    public static OptionItem DisablePlayVideoGame;
    public static OptionItem DisableFindSignal;
    public static OptionItem DisableThrowFisbee;
    public static OptionItem DisableLiftWeights;
    public static OptionItem DisableCollectShells;

    // Guesser Mode
    public static OptionItem GuesserMode;
    public static OptionItem CrewmatesCanGuess;
    public static OptionItem ImpostorsCanGuess;
    public static OptionItem NeutralKillersCanGuess;
    public static OptionItem NeutralApocalypseCanGuess;
    public static OptionItem PassiveNeutralsCanGuess;
    public static OptionItem CovenCanGuess;
    public static OptionItem CanGuessAddons;
    public static OptionItem ImpCanGuessImp;
    public static OptionItem CrewCanGuessCrew;
    public static OptionItem ApocCanGuessApoc;
    public static OptionItem CovenCanGuessCoven;
    public static OptionItem HideGuesserCommands;
    public static OptionItem ShowOnlyEnabledRolesInGuesserUI;
    public static OptionItem CanOnlyGuessEnabled;
    public static OptionItem UseQuickChatSpamCheat;


    // ------------ General Role Settings ------------

    // Imp
    public static OptionItem ImpsCanSeeEachOthersRoles;
    public static OptionItem ImpsCanSeeEachOthersAddOns;

    //public static OptionItem MadmateCanFixSabotage;
    public static OptionItem DefaultShapeshiftCooldown;
    public static OptionItem DeadImpCantSabotage;

    // Neutral
    public static OptionItem NonNeutralKillingRolesMinPlayer;
    public static OptionItem NonNeutralKillingRolesMaxPlayer;
    public static OptionItem NeutralKillingRolesMinPlayer;
    public static OptionItem NeutralKillingRolesMaxPlayer;
    public static OptionItem NeutralRoleWinTogether;
    public static OptionItem NeutralWinTogether;
    public static OptionItem SpawnOneRandomKillingFraction;

    // Neutral Apocalypse
    public static OptionItem NeutralApocalypseRolesMinPlayer;
    public static OptionItem NeutralApocalypseRolesMaxPlayer;
    public static OptionItem TransformedNeutralApocalypseCanBeGuessed;
    public static OptionItem ApocCanSeeEachOthersAddOns;


    // Coven
    public static OptionItem CovenRolesMinPlayer;
    public static OptionItem CovenRolesMaxPlayer;
    public static OptionItem CovenCanSeeEachOthersAddOns;
    public static OptionItem CovenHasImpVis;
    public static OptionItem CovenImpVisMode;
    public static OptionItem CovenCanVent;
    public static OptionItem CovenVentMode;

    // Add-on
    public static OptionItem NameDisplayAddons;
    public static OptionItem AddBracketsToAddons;
    public static OptionItem ShowShortNamesForAddOns;
    public static OptionItem NoLimitAddonsNumMax;
    public static OptionItem RemoveIncompatibleAddOnsMidGame;

    // Add-Ons settings 
    public static OptionItem LoverSpawnChances;
    public static OptionItem LoverKnowRoles;
    public static OptionItem LoverSuicide;
    public static OptionItem ImpCanBeInLove;
    public static OptionItem CrewCanBeInLove;
    public static OptionItem NeutralCanBeInLove;
    public static OptionItem CovenCanBeInLove;
    public static OptionItem WidowChance;

    // Experimental Roles

    //public static OptionItem SpeedBoosterUpSpeed;
    //public static OptionItem SpeedBoosterTimes;


    public static VoteMode GetWhenSkipVote() => (VoteMode)WhenSkipVote.GetValue();
    public static VoteMode GetWhenNonVote() => (VoteMode)WhenNonVote.GetValue();

    public static readonly string[] voteModes =
    [
        "Default", "Suicide", "SelfVote", "Skip"
    ];
    public static readonly string[] tieModes =
    [
        "TieMode.Default", "TieMode.All", "TieMode.Random"
    ];
    /* public static readonly string[] addonGuessModeCrew =
     {
         "GuesserMode.All", "GuesserMode.Harmful", "GuesserMode.Random"
     }; */

    public static readonly string[] suffixModes =
    [
        "SuffixMode.None",
        "SuffixMode.Version",
        "SuffixMode.Streaming",
        "SuffixMode.Recording",
        "SuffixMode.RoomHost",
        "SuffixMode.OriginalName",
        "SuffixMode.DoNotKillMe",
        "SuffixMode.NoAndroidPlz",
        "SuffixMode.AutoHost"
    ];
    public static readonly string[] roleAssigningAlgorithms =
    [
        "RoleAssigningAlgorithm.NetRandom",
        "RoleAssigningAlgorithm.HashRandom",
        "RoleAssigningAlgorithm.Xorshift",
    ];
    public static readonly string[] formatNameModes =
    [
        "FormatNameModes.None",
        "FormatNameModes.Color",
        "FormatNameModes.Snacks",
    ];
    public static SuffixModes GetSuffixMode() => (SuffixModes)SuffixMode.GetValue();

    private static void GroupAddons()
    {
        GroupedAddons = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(x => x.GetInterfaces().ToList().Contains(typeof(IAddon)))
            .Select(x => (IAddon)Activator.CreateInstance(x))
            .Where(x => x != null)
            .GroupBy(x => x.Type)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Role).ToList());
    }


    public static int SnitchExposeTaskLeft = 1;

    public static bool IsLoaded = false;

    static Options()
    {
        ResetRoleCounts();
    }
    public static void ResetRoleCounts()
    {
        roleCounts = [];
        roleSpawnChances = [];

        foreach (var role in CustomRolesHelper.AllRoles)
        {
            roleCounts.Add(role, 0);
            roleSpawnChances.Add(role, 0);
        }
    }

    public static void SetRoleCount(CustomRoles role, int count)
    {
        roleCounts[role] = count;

        if (CustomRoleCounts.TryGetValue(role, out var option))
        {
            option.SetValue(count - 1);
        }
    }

    public static int GetRoleSpawnMode(CustomRoles role) => CustomRoleSpawnChances.TryGetValue(role, out var sc) ? sc.GetChance() : 0;
    public static int GetRoleCount(CustomRoles role)
    {
        var mode = GetRoleSpawnMode(role);
        return mode is 0 ? 0 : CustomRoleCounts.TryGetValue(role, out var option) ? option.GetInt() : roleCounts[role];
    }
    public static float GetRoleChance(CustomRoles role)
    {
        return CustomRoleSpawnChances.TryGetValue(role, out var option) ? option.GetValue()/* / 10f */ : roleSpawnChances[role];
    }
    private static System.Collections.IEnumerator CoLoadOptions()
    {
        //#######################################
        // 31000 last id for roles/add-ons (Next use 31100)
        // Limit id for roles/add-ons --- "59999"
        //#######################################


        // Start Load Settings
        if (IsLoaded) yield break;
        OptionSaver.Initialize();
        GroupAddons();

        yield return null;

        // Preset Option
        _ = PresetOptionItem.Create(0, TabGroup.SystemSettings)
                .SetColor(new Color32(255, 235, 4, byte.MaxValue))
                .SetHidden(true);

        // Game Mode
        GameMode = StringOptionItem.Create(60000, "GameMode", gameModes, 0, TabGroup.ModSettings, false)
            .SetHeader(true);


        #region Roles/Add-ons Settings
        CustomRoleCounts = [];
        CustomGhostRoleCounts = [];
        CustomRoleSpawnChances = [];
        CustomAdtRoleSpawnRate = [];

        // GM
        //EnableGM = BooleanOptionItem.Create(60001, "GM", false, TabGroup.ModSettings, false)
        //    .SetColor(Utils.GetRoleColor(CustomRoles.GM))
        //    .SetHidden(true)
        //    .SetHeader(true);

        ImpsCanSeeEachOthersRoles = BooleanOptionItem.Create(60001, "ImpsCanSeeEachOthersRoles", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true);
        ImpsCanSeeEachOthersAddOns = BooleanOptionItem.Create(60002, "ImpsCanSeeEachOthersAddOns", true, TabGroup.ImpostorRoles, false)
            .SetParent(ImpsCanSeeEachOthersRoles);

        Madmate.SetupMenuOptions();

        //MadmateCanFixSabotage = BooleanOptionItem.Create(50010, "MadmateCanFixSabotage", false, TabGroup.ImpostorRoles, false)
        //.SetGameMode(CustomGameMode.Standard);

        DefaultShapeshiftCooldown = FloatOptionItem.Create(60011, "DefaultShapeshiftCooldown", new(5f, 180f, 5f), 15f, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetValueFormat(OptionFormat.Seconds);
        DeadImpCantSabotage = BooleanOptionItem.Create(60012, "DeadImpCantSabotage", false, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);

        NonNeutralKillingRolesMinPlayer = IntegerOptionItem.Create(60013, "NonNeutralKillingRolesMinPlayer", new(0, 15, 1), 0, TabGroup.NeutralRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetValueFormat(OptionFormat.Players);
        NonNeutralKillingRolesMaxPlayer = IntegerOptionItem.Create(60014, "NonNeutralKillingRolesMaxPlayer", new(0, 15, 1), 0, TabGroup.NeutralRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetValueFormat(OptionFormat.Players);

        NeutralKillingRolesMinPlayer = IntegerOptionItem.Create(60015, "NeutralKillingRolesMinPlayer", new(0, 15, 1), 0, TabGroup.NeutralRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetValueFormat(OptionFormat.Players);
        NeutralKillingRolesMaxPlayer = IntegerOptionItem.Create(60016, "NeutralKillingRolesMaxPlayer", new(0, 15, 1), 0, TabGroup.NeutralRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetValueFormat(OptionFormat.Players);

        NeutralApocalypseRolesMinPlayer = IntegerOptionItem.Create(60022, "NeutralApocalypseRolesMinPlayer", new(0, 4, 1), 0, TabGroup.NeutralRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetValueFormat(OptionFormat.Players);
        NeutralApocalypseRolesMaxPlayer = IntegerOptionItem.Create(60023, "NeutralApocalypseRolesMaxPlayer", new(0, 4, 1), 0, TabGroup.NeutralRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetValueFormat(OptionFormat.Players);


        NeutralRoleWinTogether = BooleanOptionItem.Create(60017, "NeutralRoleWinTogether", false, TabGroup.NeutralRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true);
        NeutralWinTogether = BooleanOptionItem.Create(60018, "NeutralWinTogether", false, TabGroup.NeutralRoles, false)
            .SetParent(NeutralRoleWinTogether)
            .SetGameMode(CustomGameMode.Standard);
        SpawnOneRandomKillingFraction = BooleanOptionItem.Create(60010, "SpawnOneRandomKillingFraction", true, TabGroup.NeutralRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true);

        CovenRolesMinPlayer = IntegerOptionItem.Create(60026, "CovenRolesMinPlayer", new(0, 15, 1), 0, TabGroup.CovenRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetValueFormat(OptionFormat.Players);
        CovenRolesMaxPlayer = IntegerOptionItem.Create(60027, "CovenRolesMaxPlayer", new(0, 15, 1), 0, TabGroup.CovenRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetValueFormat(OptionFormat.Players);
        CovenHasImpVis = BooleanOptionItem.Create(60028, "CovenHasImpVis", true, TabGroup.CovenRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true);
        CovenImpVisMode = StringOptionItem.Create(60029, "CovenImpVisMode", EnumHelper.GetAllNames<CovenManager.VisOptionList>(), 0, TabGroup.CovenRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(CovenHasImpVis);
        CovenManager.RunSetUpImpVisOptions(160032);
        CovenCanVent = BooleanOptionItem.Create(60030, "CovenCanVent", true, TabGroup.CovenRoles, false)
            .SetGameMode(CustomGameMode.Standard);
        CovenVentMode = StringOptionItem.Create(60032, "CovenVentMode", EnumHelper.GetAllNames<CovenManager.VentOptionList>(), 0, TabGroup.CovenRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(CovenCanVent);
        CovenManager.RunSetUpVentOptions(260032);
        CovenCanSeeEachOthersAddOns = BooleanOptionItem.Create(60033, "CovenCanSeeEachOthersAddOns", true, TabGroup.CovenRoles, false)
            .SetGameMode(CustomGameMode.Standard);

        NameDisplayAddons = BooleanOptionItem.Create(60019, "NameDisplayAddons", true, TabGroup.Addons, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true);
        AddBracketsToAddons = BooleanOptionItem.Create(60021, "BracketAddons", true, TabGroup.Addons, false)
            .SetParent(NameDisplayAddons);
        ShowShortNamesForAddOns = StringOptionItem.Create(60035, "ShowShortNamesForAddOns", EnumHelper.GetAllNames<ShortAddOnNamesMode>(), 0, TabGroup.Addons, false)
            .SetParent(NameDisplayAddons);
        NoLimitAddonsNumMax = IntegerOptionItem.Create(60020, "NoLimitAddonsNumMax", new(0, 15, 1), 1, TabGroup.Addons, false)
            .SetGameMode(CustomGameMode.Standard);
        RemoveIncompatibleAddOnsMidGame = BooleanOptionItem.Create(60034, "RemoveIncompatibleAddOnsMidGame", true, TabGroup.Addons, false)
            .SetGameMode(CustomGameMode.Standard);
        #endregion

        yield return null;

        #region Impostors Settings
        // Impostor
        TextOptionItem.Create(10000000, "RoleType.VanillaRoles", TabGroup.ImpostorRoles) // Vanilla
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(255, 25, 25, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.ImpostorVanilla).ForEach(r => r.SetupCustomOption());

        if (CustomRoleManager.RoleClass.Where(x => x.Key.IsImpostor()).Any(r => r.Value.IsExperimental))
        {
            TextOptionItem.Create(10000020, "Experimental.Roles", TabGroup.ImpostorRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(141, 70, 49, byte.MaxValue));

            CustomRoleManager.GetExperimentalOptions(Custom_Team.Impostor).ForEach(r => r.SetupCustomOption());


        }

        TextOptionItem.Create(10000001, "RoleType.ImpKilling", TabGroup.ImpostorRoles) // KILLING
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(255, 25, 25, byte.MaxValue));// KILLING

        CustomRoleManager.GetNormalOptions(Custom_RoleType.ImpostorKilling).ForEach(r => r.SetupCustomOption());

        /*
         * SUPPORT ROLES
         */
        TextOptionItem.Create(10000002, "RoleType.ImpSupport", TabGroup.ImpostorRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 25, 25, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.ImpostorSupport).ForEach(r => r.SetupCustomOption());

        /*
         * CONCEALING ROLES
         */
        TextOptionItem.Create(10000003, "RoleType.ImpConcealing", TabGroup.ImpostorRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 25, 25, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.ImpostorConcealing).ForEach(r => r.SetupCustomOption());

        /*
         * HINDERING ROLES
         */
        TextOptionItem.Create(10000004, "RoleType.ImpHindering", TabGroup.ImpostorRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 25, 25, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.ImpostorHindering).ForEach(r => r.SetupCustomOption());

        /*
         * MADMATE ROLES
         */
        TextOptionItem.Create(10000005, "RoleType.Madmate", TabGroup.ImpostorRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 25, 25, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.Madmate).ForEach(r => r.SetupCustomOption());

        /*
         * Impostor Ghost Roles
        */
        TextOptionItem.Create(10000111, "RoleType.ImpGhost", TabGroup.ImpostorRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 25, 25, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.ImpostorGhosts).ForEach(r => r.SetupCustomOption());

        #endregion

        yield return null;

        #region Crewmates Settings
        /*
         * VANILLA ROLES
         */
        TextOptionItem.Create(10000006, "RoleType.VanillaRoles", TabGroup.CrewmateRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(140, 255, 255, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.CrewmateVanilla).ForEach(r => r.SetupCustomOption());
        CustomRoleManager.GetNormalOptions(Custom_RoleType.CrewmateVanillaGhosts).ForEach(r => r.SetupCustomOption());

        if (CustomRoleManager.RoleClass.Where(x => x.Key.IsCrewmate()).Any(r => r.Value.IsExperimental))
        {
            TextOptionItem.Create(10000021, "Experimental.Roles", TabGroup.CrewmateRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(141, 70, 49, byte.MaxValue));

            CustomRoleManager.GetExperimentalOptions(Custom_Team.Crewmate).ForEach(r => r.SetupCustomOption());


        }

        /*
         * BASIC ROLES
         */
        TextOptionItem.Create(10000007, "RoleType.CrewBasic", TabGroup.CrewmateRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(140, 255, 255, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.CrewmateBasic).ForEach(r => r.SetupCustomOption());

        /*
         * MINI 
         */
        CustomRoles.Mini.GetStaticRoleClass().SetupCustomOption();

        /*
         * HINDERING ROLES
         */
        TextOptionItem.Create(10000008, "RoleType.CrewHindering", TabGroup.CrewmateRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(140, 255, 255, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.CrewmateHindering).ForEach(r => r.SetupCustomOption());

        /*
         * SUPPORT ROLES
         */
        TextOptionItem.Create(10000009, "RoleType.CrewSupport", TabGroup.CrewmateRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(140, 255, 255, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.CrewmateSupport).ForEach(r => r.SetupCustomOption());

        /*
         * KILLING ROLES
         */
        TextOptionItem.Create(10000010, "RoleType.CrewKilling", TabGroup.CrewmateRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(140, 255, 255, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.CrewmateKilling).ForEach(r => r.SetupCustomOption());

        /*
         * POWER ROLES
         */
        TextOptionItem.Create(10000011, "RoleType.CrewPower", TabGroup.CrewmateRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(140, 255, 255, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.CrewmatePower).ForEach(r => r.SetupCustomOption());

        /*
         * Crewmate Ghost Roles
         */
        TextOptionItem.Create(10000101, "RoleType.CrewGhost", TabGroup.CrewmateRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(140, 255, 255, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.CrewmateGhosts).ForEach(r => r.SetupCustomOption());


        #endregion

        yield return null;

        #region Neutrals Settings


        if (CustomRoleManager.RoleClass.Where(x => x.Key.IsNeutral()).Any(r => r.Value.IsExperimental))
        {
            TextOptionItem.Create(10000022, "Experimental.Roles", TabGroup.NeutralRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(141, 70, 49, byte.MaxValue));

            CustomRoleManager.GetExperimentalOptions(Custom_Team.Neutral).ForEach(r => r.SetupCustomOption());


        }
        // Neutral
        TextOptionItem.Create(10000012, "RoleType.NeutralBenign", TabGroup.NeutralRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(127, 140, 141, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.NeutralBenign).ForEach(r => r.SetupCustomOption());

        TextOptionItem.Create(10000013, "RoleType.NeutralEvil", TabGroup.NeutralRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(127, 140, 141, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.NeutralEvil).ForEach(r => r.SetupCustomOption());

        TextOptionItem.Create(10000014, "RoleType.NeutralChaos", TabGroup.NeutralRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(127, 140, 141, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.NeutralChaos).ForEach(r => r.SetupCustomOption());

        TextOptionItem.Create(10000015, "RoleType.NeutralKilling", TabGroup.NeutralRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(127, 140, 141, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.NeutralKilling).ForEach(r => r.SetupCustomOption());

        TextOptionItem.Create(10000116, "RoleType.NeutralApocalypse", TabGroup.NeutralRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(127, 140, 141, byte.MaxValue));

        TransformedNeutralApocalypseCanBeGuessed = BooleanOptionItem.Create(60024, "TNACanBeGuessed", false, TabGroup.NeutralRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true);

        ApocCanSeeEachOthersAddOns = BooleanOptionItem.Create(60025, "ApocCanSeeEachOthersAddOns", true, TabGroup.NeutralRoles, false)
            .SetGameMode(CustomGameMode.Standard);

        CustomRoleManager.GetNormalOptions(Custom_RoleType.NeutralApocalypse).ForEach(r => r.SetupCustomOption());

        TextOptionItem.Create(10000117, "RoleType.NeutralGhost", TabGroup.NeutralRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(100, 100, 100, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.NeutralGhosts).ForEach(r => r.SetupCustomOption());
        #endregion

        yield return null;

        #region Coven Settings
        if (CustomRoleManager.RoleClass.Where(x => x.Key.IsCoven()).Any(r => r.Value.IsExperimental))
        {
            TextOptionItem.Create(10000023, "Experimental.Roles", TabGroup.NeutralRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(141, 70, 49, byte.MaxValue));

            CustomRoleManager.GetExperimentalOptions(Custom_Team.Coven).ForEach(r => r.SetupCustomOption());


        }

        TextOptionItem.Create(10000017, "RoleType.CovenPower", TabGroup.CovenRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(172, 66, 242, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.CovenPower).ForEach(r => r.SetupCustomOption());

        TextOptionItem.Create(10000018, "RoleType.CovenKilling", TabGroup.CovenRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(172, 66, 242, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.CovenKilling).ForEach(r => r.SetupCustomOption());

        TextOptionItem.Create(10000019, "RoleType.CovenTrickery", TabGroup.CovenRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(172, 66, 242, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.CovenTrickery).ForEach(r => r.SetupCustomOption());

        TextOptionItem.Create(10000024, "RoleType.CovenUtility", TabGroup.CovenRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(172, 66, 242, byte.MaxValue));

        CustomRoleManager.GetNormalOptions(Custom_RoleType.CovenUtility).ForEach(r => r.SetupCustomOption());
        #endregion

        yield return null;

        #region Add-Ons Settings

        int titleId = 100100;

        var IAddonType = typeof(IAddon);
        Dictionary<AddonTypes, IAddon[]> addonTypes = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(t => IAddonType.IsAssignableFrom(t) && !t.IsInterface)
            .OrderBy(t => Translator.GetString(t.Name))
            .Select(type => (IAddon)Activator.CreateInstance(type))
            .Where(x => x != null)
            .GroupBy(x => x.Type)
            .ToDictionary(x => x.Key, x => x.ToArray());

        foreach (var addonType in addonTypes)
        {
            TextOptionItem.Create(titleId, $"RoleType.{addonType.Key}", TabGroup.Addons)
                .SetGameMode(CustomGameMode.Standard)
                .SetColor(GetAddonTypeColor(addonType.Key))
                .SetHeader(true);
            titleId += 10;

            if (addonType.Key == AddonTypes.Impostor)
                Madmate.SetupCustomMenuOptions();

            if (addonType.Key == AddonTypes.Misc)
                SetupLoversRoleOptionsToggle(23600); // KYS 

            foreach (var addon in addonType.Value)
            {
                addon.SetupCustomOption();
            }

            yield return null;
        }
        static Color32 GetAddonTypeColor(AddonTypes type) => type switch
        {
            AddonTypes.Impostor => new Color32(255, 25, 25, byte.MaxValue),
            AddonTypes.Helpful => new Color32(255, 154, 206, byte.MaxValue),
            AddonTypes.Harmful => new Color32(255, 154, 206, byte.MaxValue),
            AddonTypes.Misc => new Color32(127, 140, 141, byte.MaxValue),
            AddonTypes.Mixed => new Color32(255, 154, 206, byte.MaxValue),
            AddonTypes.Guesser => new Color32(214, 177, 73, byte.MaxValue),
            AddonTypes.Experimental => new Color32(141, 140, 141, byte.MaxValue),
            _ => Palette.CrewmateBlue
        };



        #endregion

        yield return null;

        #region Experimental Roles/Add-ons Settings



        #endregion

        yield return null;

        #region System Settings
        BypassRateLimitAC = BooleanOptionItem.Create(60049, "BypassRateLimitAC", true, TabGroup.SystemSettings, false)
            .SetHeader(true);
        GradientTagsOpt = BooleanOptionItem.Create(60031, "EnableGadientTags", false, TabGroup.SystemSettings, false)
            .SetHeader(true);
        EnableKillerLeftCommand = BooleanOptionItem.Create(60040, "EnableKillerLeftCommand", true, TabGroup.SystemSettings, false)
            .HideInHnS()
            .HideInCandR();
        ShowMadmatesInLeftCommand = BooleanOptionItem.Create(60042, "ShowMadmatesInLeftCommand", true, TabGroup.SystemSettings, false)
            .SetParent(EnableKillerLeftCommand);
        ShowApocalypseInLeftCommand = BooleanOptionItem.Create(60043, "ShowApocalypseInLeftCommand", true, TabGroup.SystemSettings, false)
            .SetParent(EnableKillerLeftCommand);
        ShowCovenInLeftCommand = BooleanOptionItem.Create(60044, "ShowCovenInLeftCommand", true, TabGroup.SystemSettings, false)
            .SetParent(EnableKillerLeftCommand);
        SeeEjectedRolesInMeeting = BooleanOptionItem.Create(60041, "SeeEjectedRolesInMeeting", true, TabGroup.SystemSettings, false)
            .HideInHnS()
            .HideInCandR();

        KickLowLevelPlayer = IntegerOptionItem.Create(60050, "KickLowLevelPlayer", new(0, 100, 1), 0, TabGroup.SystemSettings, false)
            .SetValueFormat(OptionFormat.Level)
            .SetHeader(true);
        TempBanLowLevelPlayer = BooleanOptionItem.Create(60051, "TempBanLowLevelPlayer", false, TabGroup.SystemSettings, false)
            .SetParent(KickLowLevelPlayer)
            .SetValueFormat(OptionFormat.Times);
        ApplyAllowList = BooleanOptionItem.Create(60060, "ApplyWhiteList", false, TabGroup.SystemSettings, false);
        AllowOnlyWhiteList = BooleanOptionItem.Create(60061, "AllowOnlyWhiteList", false, TabGroup.SystemSettings, false);

        KickOtherPlatformPlayer = BooleanOptionItem.Create(60070, "KickOtherPlatformPlayer", false, TabGroup.SystemSettings, false);
        OptKickAndroidPlayer = BooleanOptionItem.Create(60071, "OptKickAndroidPlayer", false, TabGroup.SystemSettings, false)
            .SetParent(KickOtherPlatformPlayer);
        OptKickIphonePlayer = BooleanOptionItem.Create(60072, "OptKickIphonePlayer", false, TabGroup.SystemSettings, false)
            .SetParent(KickOtherPlatformPlayer);
        OptKickXboxPlayer = BooleanOptionItem.Create(60073, "OptKickXboxPlayer", false, TabGroup.SystemSettings, false)
            .SetParent(KickOtherPlatformPlayer);
        OptKickPlayStationPlayer = BooleanOptionItem.Create(60074, "OptKickPlayStationPlayer", false, TabGroup.SystemSettings, false)
            .SetParent(KickOtherPlatformPlayer);
        OptKickNintendoPlayer = BooleanOptionItem.Create(60075, "OptKickNintendoPlayer", false, TabGroup.SystemSettings, false)
            .SetParent(KickOtherPlatformPlayer); //Switch
        KickPlayerFriendCodeInvalid = BooleanOptionItem.Create(60080, "KickPlayerFriendCodeInvalid", true, TabGroup.SystemSettings, false);
        TempBanPlayerFriendCodeInvalid = BooleanOptionItem.Create(60081, "TempBanPlayerFriendCodeInvalid", false, TabGroup.SystemSettings, false)
            .SetParent(KickPlayerFriendCodeInvalid);
        AutoKickStart = BooleanOptionItem.Create(60140, "AutoKickStart", false, TabGroup.SystemSettings, false);
        AutoKickStartTimes = IntegerOptionItem.Create(60141, "AutoKickStartTimes", new(0, 99, 1), 1, TabGroup.SystemSettings, false)
            .SetParent(AutoKickStart)
            .SetValueFormat(OptionFormat.Times);
        AutoKickStartAsBan = BooleanOptionItem.Create(60142, "AutoKickStartAsBan", false, TabGroup.SystemSettings, false)
            .SetParent(AutoKickStart);
        TempBanPlayersWhoKeepQuitting = BooleanOptionItem.Create(60150, "TempBanPlayersWhoKeepQuitting", false, TabGroup.SystemSettings, false);
        QuitTimesTillTempBan = IntegerOptionItem.Create(60151, "QuitTimesTillTempBan", new(1, 15, 1), 4, TabGroup.SystemSettings, false)
            .SetValueFormat(OptionFormat.Times)
            .SetParent(TempBanPlayersWhoKeepQuitting);
        ApplyVipList = BooleanOptionItem.Create(60090, "ApplyVipList", true, TabGroup.SystemSettings, false).SetHeader(true);
        ApplyDenyNameList = BooleanOptionItem.Create(60100, "ApplyDenyNameList", true, TabGroup.SystemSettings, true);
        ApplyBanList = BooleanOptionItem.Create(60110, "ApplyBanList", true, TabGroup.SystemSettings, true);
        ApplyModeratorList = BooleanOptionItem.Create(60120, "ApplyModeratorList", false, TabGroup.SystemSettings, false);
        AllowSayCommand = BooleanOptionItem.Create(60121, "AllowSayCommand", false, TabGroup.SystemSettings, false)
            .SetParent(ApplyModeratorList);
        AllowStartCommand = BooleanOptionItem.Create(60122, "AllowStartCommand", false, TabGroup.SystemSettings, false)
            .SetParent(ApplyModeratorList);
        StartCommandMinCountdown = IntegerOptionItem.Create(60123, "StartCommandMinCountdown", new(0, 99, 1), 0, TabGroup.SystemSettings, false)
            .SetParent(AllowStartCommand)
            .SetValueFormat(OptionFormat.Seconds);
        StartCommandMaxCountdown = IntegerOptionItem.Create(60124, "StartCommandMaxCountdown", new(0, 99, 1), 15, TabGroup.SystemSettings, false)
            .SetParent(AllowStartCommand)
            .SetValueFormat(OptionFormat.Seconds);

        //ApplyReminderMsg = BooleanOptionItem.Create(60130, "ApplyReminderMsg", false, TabGroup.SystemSettings, false);
        /*TimeForReminder = IntegerOptionItem.Create(60131, "TimeForReminder", new(0, 99, 1), 3, TabGroup.SystemSettings, false)
            .SetParent(TimeForReminder)
            .SetValueFormat(OptionFormat.Seconds); */
        /*AutoKickStopWords = BooleanOptionItem.Create(60160, "AutoKickStopWords", false, TabGroup.SystemSettings, false);
        AutoKickStopWordsTimes = IntegerOptionItem.Create(60161, "AutoKickStopWordsTimes", new(0, 99, 1), 3, TabGroup.SystemSettings, false)
            .SetParent(AutoKickStopWords)
            .SetValueFormat(OptionFormat.Times);
        AutoKickStopWordsAsBan = BooleanOptionItem.Create(60162, "AutoKickStopWordsAsBan", false, TabGroup.SystemSettings, false)
            .SetParent(AutoKickStopWords);
        AutoWarnStopWords = BooleanOptionItem.Create(60163, "AutoWarnStopWords", false, TabGroup.SystemSettings, false); */
        MinWaitAutoStart = FloatOptionItem.Create(60170, "MinWaitAutoStart", new(0f, 10f, 0.5f), 1.5f, TabGroup.SystemSettings, false).SetHeader(true);
        MaxWaitAutoStart = FloatOptionItem.Create(60180, "MaxWaitAutoStart", new(0f, 10f, 0.5f), 1.5f, TabGroup.SystemSettings, false);
        PlayerAutoStart = IntegerOptionItem.Create(60190, "PlayerAutoStart", new(1, 100, 1), 14, TabGroup.SystemSettings, false)
            .SetValueFormat(OptionFormat.Players);
        StartWhenTimePassed = IntegerOptionItem.Create(60205, "StartWhenTimePassed", new(0, 1200, 10), 300, TabGroup.SystemSettings, false)
            .SetValueFormat(OptionFormat.Seconds);
        AutoStartTimer = IntegerOptionItem.Create(60200, "AutoStartTimer", new(10, 600, 1), 20, TabGroup.SystemSettings, false)
            .SetValueFormat(OptionFormat.Seconds);
        ImmediateAutoStart = BooleanOptionItem.Create(60201, "ImmediateAutoStart", false, TabGroup.SystemSettings, false);
        ImmediateStartTimer = IntegerOptionItem.Create(60202, "ImmediateStartTimer", new(0, 60, 1), 20, TabGroup.SystemSettings, false)
            .SetParent(ImmediateAutoStart)
            .SetValueFormat(OptionFormat.Seconds);
        StartWhenPlayersReach = IntegerOptionItem.Create(60203, "StartWhenPlayersReach", new(0, 100, 1), 14, TabGroup.SystemSettings, false)
            .SetParent(ImmediateAutoStart)
            .SetValueFormat(OptionFormat.Players);
        StartWhenTimerLowerThan = IntegerOptionItem.Create(60204, "StartWhenTimerLowerThan", new(0, 600, 5), 60, TabGroup.SystemSettings, false)
            .SetParent(ImmediateAutoStart)
            .SetValueFormat(OptionFormat.Seconds);
        AutoPlayAgain = BooleanOptionItem.Create(60210, "AutoPlayAgain", false, TabGroup.SystemSettings, false);
        AutoPlayAgainCountdown = IntegerOptionItem.Create(60211, "AutoPlayAgainCountdown", new(1, 20, 1), 10, TabGroup.SystemSettings, false)
            .SetParent(AutoPlayAgain)
            .SetValueFormat(OptionFormat.Seconds);
        /*ShowLobbyCode = BooleanOptionItem.Create(60220, "ShowLobbyCode", true, TabGroup.SystemSettings, false)
            .SetColor(Color.blue); */
        LowLoadMode = BooleanOptionItem.Create(60230, "LowLoadMode", true, TabGroup.SystemSettings, false)
            .SetHeader(true)
            .SetColor(Color.green);
        LowLoadDelayUpdateNames = BooleanOptionItem.Create(60231, "LowLoad_DelayUpdateNames", true, TabGroup.SystemSettings, false)
            .SetParent(LowLoadMode);
        EndWhenPlayerBug = BooleanOptionItem.Create(60240, "EndWhenPlayerBug", true, TabGroup.SystemSettings, false)
            .SetColor(Color.blue);
        HideExileChat = BooleanOptionItem.Create(60292, "HideExileChat", true, TabGroup.SystemSettings, false)
            .SetColor(Color.blue)
            .HideInHnS()
            .HideInCandR();
        RemovePetsAtDeadPlayers = BooleanOptionItem.Create(60294, "RemovePetsAtDeadPlayers", false, TabGroup.SystemSettings, false)
            .SetColor(Color.magenta)
            .HideInCandR();

        CheatResponses = StringOptionItem.Create(60250, "CheatResponses", CheatResponsesName, 0, TabGroup.SystemSettings, false)
            .SetHeader(true);

        AutoDisplayKillLog = BooleanOptionItem.Create(60270, "AutoDisplayKillLog", true, TabGroup.SystemSettings, false)
            .SetHeader(true)
            .HideInHnS();
        OldKillLog = BooleanOptionItem.Create(60291, "RevertOldKillLog", false, TabGroup.SystemSettings, false)
            .HideInHnS();
        AutoDisplayLastRoles = BooleanOptionItem.Create(60280, "AutoDisplayLastRoles", true, TabGroup.SystemSettings, false)
            .HideInHnS();
        AutoDisplayLastResult = BooleanOptionItem.Create(60290, "AutoDisplayLastResult", true, TabGroup.SystemSettings, false)
            .HideInHnS();

        SuffixMode = StringOptionItem.Create(60300, "SuffixMode", suffixModes, 0, TabGroup.SystemSettings, true)
            .SetHeader(true);
        HideHostText = BooleanOptionItem.Create(60311, "HideHostText", false, TabGroup.SystemSettings, false);
        HideAllTagsAndText = BooleanOptionItem.Create(60312, "HideAllTagsAndText", false, TabGroup.SystemSettings, false);
        HideGameSettings = BooleanOptionItem.Create(60310, "HideGameSettings", false, TabGroup.SystemSettings, false);
        //DIYGameSettings = BooleanOptionItem.Create(60320, "DIYGameSettings", false, TabGroup.SystemSettings, false);
        PlayerCanSetColor = BooleanOptionItem.Create(60330, "PlayerCanSetColor", false, TabGroup.SystemSettings, false);
        PlayerCanUseQuitCommand = BooleanOptionItem.Create(60331, "PlayerCanUseQuitCommand", false, TabGroup.SystemSettings, false);
        PlayerCanSetName = BooleanOptionItem.Create(60332, "PlayerCanSetName", false, TabGroup.SystemSettings, false);
        PlayerCanUseTP = BooleanOptionItem.Create(60333, "PlayerCanUseTP", false, TabGroup.SystemSettings, false);
        CanPlayMiniGames = BooleanOptionItem.Create(60334, "CanPlayMiniGames", false, TabGroup.SystemSettings, false);
        FormatNameMode = StringOptionItem.Create(60340, "FormatNameMode", formatNameModes, 0, TabGroup.SystemSettings, false);
        DisableEmojiName = BooleanOptionItem.Create(60350, "DisableEmojiName", true, TabGroup.SystemSettings, false);
        ChangeNameToRoleInfo = BooleanOptionItem.Create(60360, "ChangeNameToRoleInfo", true, TabGroup.SystemSettings, false)
            .HideInHnS();
        SendRoleDescriptionFirstMeeting = BooleanOptionItem.Create(60370, "SendRoleDescriptionFirstMeeting", false, TabGroup.SystemSettings, false)
            .HideInHnS();

        NoGameEnd = BooleanOptionItem.Create(60380, "NoGameEnd", false, TabGroup.SystemSettings, false)
            .SetColor(Color.red)
            .SetHeader(true);
        AllowConsole = BooleanOptionItem.Create(60382, "AllowConsole", false, TabGroup.SystemSettings, false)
            .SetColor(Color.red);
        /* DisableAntiBlackoutProtects = BooleanOptionItem.Create(60384, "DisableAntiBlackoutProtects", false, TabGroup.SystemSettings, false)
             .SetGameMode(CustomGameMode.Standard)
             .SetColor(Color.red);*/

        RoleAssigningAlgorithm = StringOptionItem.Create(60400, "RoleAssigningAlgorithm", roleAssigningAlgorithms, 3, TabGroup.SystemSettings, true)
            .RegisterUpdateValueEvent((object obj, OptionItem.UpdateValueEventArgs args) => IRandom.SetInstanceById(args.CurrentValue))
            .SetHeader(true);
        KPDCamouflageMode = StringOptionItem.Create(60410, "KPDCamouflageMode", CamouflageMode, 0, TabGroup.SystemSettings, false)
            .HideInHnS()
            .SetHeader(true)
            .SetColor(new Color32(255, 192, 203, byte.MaxValue));
        //DebugModeManager.SetupCustomOption();
        EnableUpMode = BooleanOptionItem.Create(60430, "EnableYTPlan", false, TabGroup.SystemSettings, false)
            .HideInHnS()
            .SetColor(Color.cyan)
            .SetHeader(true);
        #endregion 

        yield return null;

        #region Game Settings
        //FFA
        FFAManager.SetupCustomOption();

        //C&R
        CopsAndRobbersManager.SetupCustomOption();

        //Ultimate Team
        UltimateTeam.SetupCustomOption();
        
        TrickorTreat.SetupCustomOption();

        // Hide & Seek
        TextOptionItem.Create(10000055, "MenuTitle.Hide&Seek", TabGroup.ModSettings)
            .SetGameMode(CustomGameMode.HidenSeekTOHO)
            .SetColor(Color.red);

        // Maximum Impostors in Hide & Seek
        MaxImpostorsHnS = IntegerOptionItem.Create(60891, "MaxImpostorsHnS", new(1, 3, 1), 1, TabGroup.ModSettings, false)
            .SetHeader(true)
            .SetColor(Color.red)
            .SetGameMode(CustomGameMode.HidenSeekTOHO)
            .SetValueFormat(OptionFormat.Players);



        // Confirm Ejections Mode
        TextOptionItem.Create(10000024, "MenuTitle.Ejections", TabGroup.ModSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
        CEMode = StringOptionItem.Create(60440, "ConfirmEjectionsMode", ConfirmEjectionsMode, 2, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
        ShowImpRemainOnEject = BooleanOptionItem.Create(60441, "ShowImpRemainOnEject", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
        ShowNKRemainOnEject = BooleanOptionItem.Create(60442, "ShowNKRemainOnEject", true, TabGroup.ModSettings, false)
            .SetParent(ShowImpRemainOnEject)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
        ShowNARemainOnEject = BooleanOptionItem.Create(60446, "ShowNARemainOnEject", true, TabGroup.ModSettings, false)
            .SetParent(ShowImpRemainOnEject)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
        ShowCovenRemainOnEject = BooleanOptionItem.Create(60447, "ShowCovenRemainOnEject", true, TabGroup.ModSettings, false)
            .SetParent(ShowImpRemainOnEject)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
        ShowTeamNextToRoleNameOnEject = BooleanOptionItem.Create(60443, "ShowTeamNextToRoleNameOnEject", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
        ConfirmEgoistOnEject = BooleanOptionItem.Create(60444, "ConfirmEgoistOnEject", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue))
            .SetHeader(true);
        ConfirmLoversOnEject = BooleanOptionItem.Create(60445, "ConfirmLoversOnEject", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));

        TextOptionItem.Create(10000028, "MenuTitle.Guessers", TabGroup.ModSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(Color.yellow)
            .SetHeader(true);
        GuesserMode = BooleanOptionItem.Create(60680, "GuesserMode", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(Color.yellow)
            .SetHeader(true);
        CrewmatesCanGuess = BooleanOptionItem.Create(60681, "CrewmatesCanGuess", false, TabGroup.ModSettings, false)
            .SetParent(GuesserMode);
        CrewCanGuessCrew = BooleanOptionItem.Create(60686, "CrewCanGuessCrew", true, TabGroup.ModSettings, false)
            .SetParent(CrewmatesCanGuess);
        ImpostorsCanGuess = BooleanOptionItem.Create(60682, "ImpostorsCanGuess", false, TabGroup.ModSettings, false)
            .SetParent(GuesserMode);
        ImpCanGuessImp = BooleanOptionItem.Create(60687, "ImpCanGuessImp", true, TabGroup.ModSettings, false)
            .SetParent(ImpostorsCanGuess);
        NeutralKillersCanGuess = BooleanOptionItem.Create(60683, "NeutralKillersCanGuess", false, TabGroup.ModSettings, false)
            .SetParent(GuesserMode);
        NeutralApocalypseCanGuess = BooleanOptionItem.Create(60690, "NeutralApocalypseCanGuess", false, TabGroup.ModSettings, false)
            .SetParent(GuesserMode);
        ApocCanGuessApoc = BooleanOptionItem.Create(60691, "ApocCanGuessApoc", false, TabGroup.ModSettings, false)
            .SetParent(NeutralApocalypseCanGuess);
        PassiveNeutralsCanGuess = BooleanOptionItem.Create(60684, "PassiveNeutralsCanGuess", false, TabGroup.ModSettings, false)
            .SetParent(GuesserMode);
        CovenCanGuess = BooleanOptionItem.Create(60693, "CovenCanGuess", false, TabGroup.ModSettings, false)
            .SetParent(GuesserMode);
        CovenCanGuessCoven = BooleanOptionItem.Create(60692, "CovenCanGuessCoven", false, TabGroup.ModSettings, false)
            .SetParent(CovenCanGuess);
        CanGuessAddons = BooleanOptionItem.Create(60685, "CanGuessAddons", true, TabGroup.ModSettings, false)
            .SetParent(GuesserMode);
        HideGuesserCommands = BooleanOptionItem.Create(60688, "GuesserTryHideMsg", true, TabGroup.ModSettings, false)
            .SetParent(GuesserMode)
            .SetColor(Color.green);

        ShowOnlyEnabledRolesInGuesserUI = BooleanOptionItem.Create(60689, "ShowOnlyEnabledRolesInGuesserUI", true, TabGroup.ModSettings, false)
            .SetHeader(true)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(Color.cyan);

        CanOnlyGuessEnabled = BooleanOptionItem.Create(60696, "CanOnlyGuessEnabled", true, TabGroup.ModSettings, false)
            .SetHeader(true)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(Color.cyan);

        UseQuickChatSpamCheat = StringOptionItem.Create(60695, "UseQuickChatSpamCheat", EnumHelper.GetAllNames<QuickChatSpamMode>(), 0, TabGroup.ModSettings, false)
            .SetColor(Color.cyan);

        //Maps Settings
        TextOptionItem.Create(10000025, "MenuTitle.MapsSettings", TabGroup.ModSettings)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Random Maps Mode
        RandomMapsMode = BooleanOptionItem.Create(60450, "RandomMapsMode", false, TabGroup.ModSettings, false)
            .SetHeader(true)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        SkeldChance = IntegerOptionItem.Create(60451, "SkeldChance", new(0, 100, 5), 10, TabGroup.ModSettings, false)
            .SetParent(RandomMapsMode)
            .SetValueFormat(OptionFormat.Percent);
        MiraChance = IntegerOptionItem.Create(60452, "MiraChance", new(0, 100, 5), 10, TabGroup.ModSettings, false)
            .SetParent(RandomMapsMode)
            .SetValueFormat(OptionFormat.Percent);
        PolusChance = IntegerOptionItem.Create(60453, "PolusChance", new(0, 100, 5), 10, TabGroup.ModSettings, false)
            .SetParent(RandomMapsMode)
            .SetValueFormat(OptionFormat.Percent);
        DleksChance = IntegerOptionItem.Create(60457, "DleksChance", new(0, 100, 5), 10, TabGroup.ModSettings, false)
            .SetParent(RandomMapsMode)
            .SetValueFormat(OptionFormat.Percent);
        AirshipChance = IntegerOptionItem.Create(60454, "AirshipChance", new(0, 100, 5), 10, TabGroup.ModSettings, false)
            .SetParent(RandomMapsMode)
            .SetValueFormat(OptionFormat.Percent);
        FungleChance = IntegerOptionItem.Create(60455, "FungleChance", new(0, 100, 5), 10, TabGroup.ModSettings, false)
            .SetParent(RandomMapsMode)
            .SetValueFormat(OptionFormat.Percent);
        UseMoreRandomMapSelection = BooleanOptionItem.Create(60456, "UseMoreRandomMapSelection", false, TabGroup.ModSettings, false)
            .SetParent(RandomMapsMode)
            .SetValueFormat(OptionFormat.Percent);

        NewHideMsg = BooleanOptionItem.Create(60460, "NewHideMsg", true, TabGroup.ModSettings, false)
            .SetHidden(true)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(193, 255, 209, byte.MaxValue));

        // Random Spawn
        RandomSpawn.SetupCustomOption();

        MapModification = BooleanOptionItem.Create(60480, "MapModification", false, TabGroup.ModSettings, false)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Airship Variable Electrical
        AirshipVariableElectrical = BooleanOptionItem.Create(60481, "AirshipVariableElectrical", false, TabGroup.ModSettings, false)
            //.SetGameMode(CustomGameMode.Standard)
            .SetParent(MapModification)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Disable Airship Moving Platform
        DisableAirshipMovingPlatform = BooleanOptionItem.Create(60482, "DisableAirshipMovingPlatform", false, TabGroup.ModSettings, false)
            //.SetGameMode(CustomGameMode.Standard)
            .SetParent(MapModification)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Disable Spore Trigger On Fungle
        DisableSporeTriggerOnFungle = BooleanOptionItem.Create(60483, "DisableSporeTriggerOnFungle", false, TabGroup.ModSettings, false)
            //.SetGameMode(CustomGameMode.Standard)
            .SetParent(MapModification)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Disable Zipline On Fungle
        DisableZiplineOnFungle = BooleanOptionItem.Create(60490, "DisableZiplineOnFungle", false, TabGroup.ModSettings, false)
            //.SetGameMode(CustomGameMode.Standard)
            .SetParent(MapModification)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Disable Zipline From Top
        DisableZiplineFromTop = BooleanOptionItem.Create(60491, "DisableZiplineFromTop", false, TabGroup.ModSettings, false)
            //.SetGameMode(CustomGameMode.Standard)
            .SetParent(DisableZiplineOnFungle)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Disable Zipline From Under
        DisableZiplineFromUnder = BooleanOptionItem.Create(60492, "DisableZiplineFromUnder", false, TabGroup.ModSettings, false)
            //.SetGameMode(CustomGameMode.Standard)
            .SetParent(DisableZiplineOnFungle)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Reset Doors After Meeting
        ResetDoorsEveryTurns = BooleanOptionItem.Create(60500, "ResetDoorsEveryTurns", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Reset Doors Mode
        DoorsResetMode = StringOptionItem.Create(60501, "DoorsResetMode", EnumHelper.GetAllNames<DoorsReset.ResetModeList>(), 2, TabGroup.ModSettings, false)
            .SetParent(ResetDoorsEveryTurns)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Change decontamination time on MiraHQ/Polus
        ChangeDecontaminationTime = BooleanOptionItem.Create(60503, "ChangeDecontaminationTime", false, TabGroup.ModSettings, false)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Decontamination time on MiraHQ
        DecontaminationTimeOnMiraHQ = FloatOptionItem.Create(60504, "DecontaminationTimeOnMiraHQ", new(0.5f, 10f, 0.25f), 3f, TabGroup.ModSettings, false)
            .SetParent(ChangeDecontaminationTime)
            .SetValueFormat(OptionFormat.Seconds)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Decontamination time on Polus
        DecontaminationTimeOnPolus = FloatOptionItem.Create(60505, "DecontaminationTimeOnPolus", new(0.5f, 10f, 0.25f), 3f, TabGroup.ModSettings, false)
            .SetParent(ChangeDecontaminationTime)
            .SetValueFormat(OptionFormat.Seconds)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Vanilla Map Decorations
        EnableHalloweenDecorations = BooleanOptionItem.Create(60506, "EnableHalloweenDecorations", false, TabGroup.ModSettings, false)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        HalloweenDecorationsSkeld = BooleanOptionItem.Create(60507, "HalloweenDecorationsSkeld", false, TabGroup.ModSettings, false)
            .SetParent(EnableHalloweenDecorations)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        HalloweenDecorationsMira = BooleanOptionItem.Create(60508, "HalloweenDecorationsMira", false, TabGroup.ModSettings, false)
            .SetParent(EnableHalloweenDecorations)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        HalloweenDecorationsDleks = BooleanOptionItem.Create(60509, "HalloweenDecorationsDleks", false, TabGroup.ModSettings, false)
            .SetParent(EnableHalloweenDecorations)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        EnableBirthdayDecorationSkeld = BooleanOptionItem.Create(60518, "EnableBirthdayDecorationSkeld", false, TabGroup.ModSettings, false)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        RandomBirthdayAndHalloweenDecorationSkeld = BooleanOptionItem.Create(60519, "RandomBirthdayAndHalloweenDecorationSkeld", false, TabGroup.ModSettings, false)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));

        // Sabotage
        TextOptionItem.Create(10000026, "MenuTitle.Sabotage", TabGroup.ModSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(243, 96, 96, byte.MaxValue))
            .SetHeader(true);
        // CommsCamouflage
        CommsCamouflage = BooleanOptionItem.Create(60510, "CommsCamouflage", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(243, 96, 96, byte.MaxValue));
        DisableOnSomeMaps = BooleanOptionItem.Create(60511, "DisableOnSomeMaps", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(CommsCamouflage);
        DisableOnSkeld = BooleanOptionItem.Create(60512, "DisableOnSkeld", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(DisableOnSomeMaps);
        DisableOnMira = BooleanOptionItem.Create(60513, "DisableOnMira", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(DisableOnSomeMaps);
        DisableOnPolus = BooleanOptionItem.Create(60514, "DisableOnPolus", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(DisableOnSomeMaps);
        DisableOnDleks = BooleanOptionItem.Create(60517, "DisableOnDleks", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(DisableOnSomeMaps);
        DisableOnAirship = BooleanOptionItem.Create(60515, "DisableOnAirship", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(DisableOnSomeMaps);
        DisableOnFungle = BooleanOptionItem.Create(60516, "DisableOnFungle", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(DisableOnSomeMaps);
        DisableReportWhenCC = BooleanOptionItem.Create(60520, "DisableReportWhenCC", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(CommsCamouflage);
        // Sabotage Cooldown Control
        SabotageCooldownControl = BooleanOptionItem.Create(60530, "SabotageCooldownControl", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(243, 96, 96, byte.MaxValue));
        SabotageCooldown = FloatOptionItem.Create(60535, "SabotageCooldown", new(1f, 60f, 1f), 30f, TabGroup.ModSettings, false)
            .SetParent(SabotageCooldownControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        // Sabotage Duration Control
        SabotageTimeControl = BooleanOptionItem.Create(60540, "SabotageTimeControl", false, TabGroup.ModSettings, false)
            .SetColor(new Color32(243, 96, 96, byte.MaxValue))
            .SetGameMode(CustomGameMode.Standard);
        // The Skeld
        SkeldReactorTimeLimit = FloatOptionItem.Create(60541, "SkeldReactorTimeLimit", new(5f, 90f, 1f), 30f, TabGroup.ModSettings, false)
            .SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        SkeldO2TimeLimit = FloatOptionItem.Create(60542, "SkeldO2TimeLimit", new(5f, 90f, 1f), 30f, TabGroup.ModSettings, false)
            .SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        // Mira HQ
        MiraReactorTimeLimit = FloatOptionItem.Create(60543, "MiraReactorTimeLimit", new(5f, 90f, 1f), 45f, TabGroup.ModSettings, false)
            .SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        MiraO2TimeLimit = FloatOptionItem.Create(60544, "MiraO2TimeLimit", new(5f, 90f, 1f), 45f, TabGroup.ModSettings, false)
            .SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        // Polus
        PolusReactorTimeLimit = FloatOptionItem.Create(60545, "PolusReactorTimeLimit", new(5f, 90f, 1f), 60f, TabGroup.ModSettings, false)
            .SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        // The Airship
        AirshipReactorTimeLimit = FloatOptionItem.Create(60546, "AirshipReactorTimeLimit", new(5f, 90f, 1f), 90f, TabGroup.ModSettings, false)
            .SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        // The Fungle
        FungleReactorTimeLimit = FloatOptionItem.Create(60547, "FungleReactorTimeLimit", new(5f, 90f, 1f), 60f, TabGroup.ModSettings, false)
            .SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        FungleMushroomMixupDuration = FloatOptionItem.Create(60548, "FungleMushroomMixupDuration", new(5f, 90f, 1f), 10f, TabGroup.ModSettings, false)
            .SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        // LightsOutSpecialSettings
        LightsOutSpecialSettings = BooleanOptionItem.Create(60550, "LightsOutSpecialSettings", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(243, 96, 96, byte.MaxValue));
        BlockDisturbancesToSwitches = BooleanOptionItem.Create(60551, "BlockDisturbancesToSwitches", false, TabGroup.ModSettings, false)
            .SetParent(LightsOutSpecialSettings)
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipViewingDeckLightsPanel = BooleanOptionItem.Create(60552, "DisableAirshipViewingDeckLightsPanel", false, TabGroup.ModSettings, false)
            .SetParent(LightsOutSpecialSettings)
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipGapRoomLightsPanel = BooleanOptionItem.Create(60553, "DisableAirshipGapRoomLightsPanel", false, TabGroup.ModSettings, false)
            .SetParent(LightsOutSpecialSettings)
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipCargoLightsPanel = BooleanOptionItem.Create(60554, "DisableAirshipCargoLightsPanel", false, TabGroup.ModSettings, false)
            .SetParent(LightsOutSpecialSettings)
            .SetGameMode(CustomGameMode.Standard);


        // Disable
        TextOptionItem.Create(10000027, "MenuTitle.Disable", TabGroup.ModSettings)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue))
            .HideInHnS();

        DisableShieldAnimations = BooleanOptionItem.Create(60560, "DisableShieldAnimations", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableKillAnimationOnGuess = BooleanOptionItem.Create(60561, "DisableKillAnimationOnGuess", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableVanillaRoles = BooleanOptionItem.Create(60562, "DisableVanillaRoles", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableTaskWin = BooleanOptionItem.Create(60563, "DisableTaskWin", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableTaskWinIfAllCrewsAreDead = BooleanOptionItem.Create(60900, "DisableTaskWinIfAllCrewsAreDead", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableTaskWinIfAllCrewsAreConverted = BooleanOptionItem.Create(60901, "DisableTaskWinIfAllCrewsAreConverted", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableMeeting = BooleanOptionItem.Create(60564, "DisableMeeting", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        // Disable Sabotage / CloseDoor
        DisableSabotage = BooleanOptionItem.Create(60565, "DisableSabotage", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableCloseDoor = BooleanOptionItem.Create(60566, "DisableCloseDoor", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        // Disable Devices
        DisableDevices = BooleanOptionItem.Create(60570, "DisableDevices", false, TabGroup.ModSettings, false)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue))
            .HideInHnS();
        //.SetGameMode(CustomGameMode.Standard);
        DisableSkeldDevices = BooleanOptionItem.Create(60571, "DisableSkeldDevices", false, TabGroup.ModSettings, false)
            .SetParent(DisableDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableSkeldAdmin = BooleanOptionItem.Create(60572, "DisableSkeldAdmin", false, TabGroup.ModSettings, false)
            .SetParent(DisableSkeldDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableSkeldCamera = BooleanOptionItem.Create(60573, "DisableSkeldCamera", false, TabGroup.ModSettings, false)
            .SetParent(DisableSkeldDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableMiraHQDevices = BooleanOptionItem.Create(60574, "DisableMiraHQDevices", false, TabGroup.ModSettings, false)
            .SetParent(DisableDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableMiraHQAdmin = BooleanOptionItem.Create(60575, "DisableMiraHQAdmin", false, TabGroup.ModSettings, false)
            .SetParent(DisableMiraHQDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableMiraHQDoorLog = BooleanOptionItem.Create(60576, "DisableMiraHQDoorLog", false, TabGroup.ModSettings, false)
            .SetParent(DisableMiraHQDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisablePolusDevices = BooleanOptionItem.Create(60577, "DisablePolusDevices", false, TabGroup.ModSettings, false)
            .SetParent(DisableDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisablePolusAdmin = BooleanOptionItem.Create(60578, "DisablePolusAdmin", false, TabGroup.ModSettings, false)
            .SetParent(DisablePolusDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisablePolusCamera = BooleanOptionItem.Create(60579, "DisablePolusCamera", false, TabGroup.ModSettings, false)
            .SetParent(DisablePolusDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisablePolusVital = BooleanOptionItem.Create(60580, "DisablePolusVital", false, TabGroup.ModSettings, false)
            .SetParent(DisablePolusDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableAirshipDevices = BooleanOptionItem.Create(60581, "DisableAirshipDevices", false, TabGroup.ModSettings, false)
            .SetParent(DisableDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableAirshipCockpitAdmin = BooleanOptionItem.Create(60582, "DisableAirshipCockpitAdmin", false, TabGroup.ModSettings, false)
            .SetParent(DisableAirshipDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableAirshipRecordsAdmin = BooleanOptionItem.Create(60583, "DisableAirshipRecordsAdmin", false, TabGroup.ModSettings, false)
            .SetParent(DisableAirshipDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableAirshipCamera = BooleanOptionItem.Create(60584, "DisableAirshipCamera", false, TabGroup.ModSettings, false)
            .SetParent(DisableAirshipDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableAirshipVital = BooleanOptionItem.Create(60585, "DisableAirshipVital", false, TabGroup.ModSettings, false)
            .SetParent(DisableAirshipDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableFungleDevices = BooleanOptionItem.Create(60586, "DisableFungleDevices", false, TabGroup.ModSettings, false)
            .SetParent(DisableDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableFungleBinoculars = BooleanOptionItem.Create(60587, "DisableFungleBinoculars", false, TabGroup.ModSettings, false)
            .SetParent(DisableFungleDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableFungleVital = BooleanOptionItem.Create(60588, "DisableFungleVital", false, TabGroup.ModSettings, false)
            .SetParent(DisableFungleDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableDevicesIgnoreConditions = BooleanOptionItem.Create(60589, "IgnoreConditions", false, TabGroup.ModSettings, false)
            .SetParent(DisableDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableDevicesIgnoreImpostors = BooleanOptionItem.Create(60590, "IgnoreImpostors", false, TabGroup.ModSettings, false)
            .SetParent(DisableDevicesIgnoreConditions);
        //.SetGameMode(CustomGameMode.Standard);
        DisableDevicesIgnoreNeutrals = BooleanOptionItem.Create(60591, "IgnoreNeutrals", false, TabGroup.ModSettings, false)
            .SetParent(DisableDevicesIgnoreConditions);
        //.SetGameMode(CustomGameMode.Standard);
        DisableDevicesIgnoreCoven = BooleanOptionItem.Create(60694, "IgnoreCoven", false, TabGroup.ModSettings, false)
            .SetParent(DisableDevicesIgnoreConditions);
        DisableDevicesIgnoreCrewmates = BooleanOptionItem.Create(60592, "IgnoreCrewmates", false, TabGroup.ModSettings, false)
            .SetParent(DisableDevicesIgnoreConditions);
        //.SetGameMode(CustomGameMode.Standard);
        DisableDevicesIgnoreAfterAnyoneDied = BooleanOptionItem.Create(60593, "IgnoreAfterAnyoneDied", false, TabGroup.ModSettings, false)
            .SetParent(DisableDevicesIgnoreConditions);
        //.SetGameMode(CustomGameMode.Standard);

        //Disable Spectate
        DisableSpectateCommand = BooleanOptionItem.Create(60567, "DisableSpectate", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));

        //Disable Short Tasks
        DisableShortTasks = BooleanOptionItem.Create(60594, "DisableShortTasks", false, TabGroup.ModSettings, false)
            .HideInFFA()
            .HideInUltimate()
            .SetHeader(true)
            .SetColor(new Color32(239, 89, 175, byte.MaxValue));
        DisableCleanVent = BooleanOptionItem.Create(60595, "DisableCleanVent", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableCalibrateDistributor = BooleanOptionItem.Create(60596, "DisableCalibrateDistributor", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableChartCourse = BooleanOptionItem.Create(60597, "DisableChartCourse", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableStabilizeSteering = BooleanOptionItem.Create(60598, "DisableStabilizeSteering", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableCleanO2Filter = BooleanOptionItem.Create(60599, "DisableCleanO2Filter", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableUnlockManifolds = BooleanOptionItem.Create(60600, "DisableUnlockManifolds", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisablePrimeShields = BooleanOptionItem.Create(60601, "DisablePrimeShields", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableMeasureWeather = BooleanOptionItem.Create(60602, "DisableMeasureWeather", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableBuyBeverage = BooleanOptionItem.Create(60603, "DisableBuyBeverage", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableAssembleArtifact = BooleanOptionItem.Create(60604, "DisableAssembleArtifact", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableSortSamples = BooleanOptionItem.Create(60605, "DisableSortSamples", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableProcessData = BooleanOptionItem.Create(60606, "DisableProcessData", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableRunDiagnostics = BooleanOptionItem.Create(60607, "DisableRunDiagnostics", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableRepairDrill = BooleanOptionItem.Create(60608, "DisableRepairDrill", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableAlignTelescope = BooleanOptionItem.Create(60609, "DisableAlignTelescope", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableRecordTemperature = BooleanOptionItem.Create(60610, "DisableRecordTemperature", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableFillCanisters = BooleanOptionItem.Create(60611, "DisableFillCanisters", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableMonitorTree = BooleanOptionItem.Create(60612, "DisableMonitorTree", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableStoreArtifacts = BooleanOptionItem.Create(60613, "DisableStoreArtifacts", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisablePutAwayPistols = BooleanOptionItem.Create(60614, "DisablePutAwayPistols", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisablePutAwayRifles = BooleanOptionItem.Create(60615, "DisablePutAwayRifles", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableMakeBurger = BooleanOptionItem.Create(60616, "DisableMakeBurger", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableCleanToilet = BooleanOptionItem.Create(60617, "DisableCleanToilet", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableDecontaminate = BooleanOptionItem.Create(60618, "DisableDecontaminate", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableSortRecords = BooleanOptionItem.Create(60619, "DisableSortRecords", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableFixShower = BooleanOptionItem.Create(60620, "DisableFixShower", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisablePickUpTowels = BooleanOptionItem.Create(60621, "DisablePickUpTowels", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisablePolishRuby = BooleanOptionItem.Create(60622, "DisablePolishRuby", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableDressMannequin = BooleanOptionItem.Create(60623, "DisableDressMannequin", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableFixAntenna = BooleanOptionItem.Create(60656, "DisableFixAntenna", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableBuildSandcastle = BooleanOptionItem.Create(60657, "DisableBuildSandcastle", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableCrankGenerator = BooleanOptionItem.Create(60658, "DisableCrankGenerator", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableMonitorMushroom = BooleanOptionItem.Create(60659, "DisableMonitorMushroom", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisablePlayVideoGame = BooleanOptionItem.Create(60660, "DisablePlayVideoGame", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableFindSignal = BooleanOptionItem.Create(60661, "DisableFindSignal", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableThrowFisbee = BooleanOptionItem.Create(60662, "DisableThrowFisbee", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableLiftWeights = BooleanOptionItem.Create(60663, "DisableLiftWeights", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);
        DisableCollectShells = BooleanOptionItem.Create(60664, "DisableCollectShells", false, TabGroup.ModSettings, false)
            .SetParent(DisableShortTasks);


        //Disable Common Tasks
        DisableCommonTasks = BooleanOptionItem.Create(60627, "DisableCommonTasks", false, TabGroup.ModSettings, false)
            .HideInFFA()
            .HideInUltimate()
            .SetColor(new Color32(239, 89, 175, byte.MaxValue));
        DisableSwipeCard = BooleanOptionItem.Create(60628, "DisableSwipeCardTask", false, TabGroup.ModSettings, false)
            .SetParent(DisableCommonTasks);
        DisableFixWiring = BooleanOptionItem.Create(60629, "DisableFixWiring", false, TabGroup.ModSettings, false)
            .SetParent(DisableCommonTasks);
        DisableEnterIdCode = BooleanOptionItem.Create(60630, "DisableEnterIdCode", false, TabGroup.ModSettings, false)
            .SetParent(DisableCommonTasks);
        DisableInsertKeys = BooleanOptionItem.Create(60631, "DisableInsertKeys", false, TabGroup.ModSettings, false)
            .SetParent(DisableCommonTasks);
        DisableScanBoardingPass = BooleanOptionItem.Create(60632, "DisableScanBoardingPass", false, TabGroup.ModSettings, false)
            .SetParent(DisableCommonTasks);
        DisableRoastMarshmallow = BooleanOptionItem.Create(60624, "DisableRoastMarshmallow", false, TabGroup.ModSettings, false)
            .SetParent(DisableCommonTasks);
        DisableCollectSamples = BooleanOptionItem.Create(60625, "DisableCollectSamples", false, TabGroup.ModSettings, false)
            .SetParent(DisableCommonTasks);
        DisableReplaceParts = BooleanOptionItem.Create(60626, "DisableReplaceParts", false, TabGroup.ModSettings, false)
            .SetParent(DisableCommonTasks);


        //Disable Long Tasks
        DisableLongTasks = BooleanOptionItem.Create(60640, "DisableLongTasks", false, TabGroup.ModSettings, false)
            .HideInFFA()
            .HideInUltimate()
            .SetColor(new Color32(239, 89, 175, byte.MaxValue));
        DisableSubmitScan = BooleanOptionItem.Create(60641, "DisableSubmitScanTask", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisableUnlockSafe = BooleanOptionItem.Create(60642, "DisableUnlockSafeTask", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisableStartReactor = BooleanOptionItem.Create(60643, "DisableStartReactorTask", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisableResetBreaker = BooleanOptionItem.Create(60644, "DisableResetBreakerTask", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisableAlignEngineOutput = BooleanOptionItem.Create(60645, "DisableAlignEngineOutput", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisableInspectSample = BooleanOptionItem.Create(60646, "DisableInspectSample", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisableEmptyChute = BooleanOptionItem.Create(60647, "DisableEmptyChute", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisableClearAsteroids = BooleanOptionItem.Create(60648, "DisableClearAsteroids", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisableWaterPlants = BooleanOptionItem.Create(60649, "DisableWaterPlants", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisableOpenWaterways = BooleanOptionItem.Create(60650, "DisableOpenWaterways", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisableReplaceWaterJug = BooleanOptionItem.Create(60651, "DisableReplaceWaterJug", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisableRebootWifi = BooleanOptionItem.Create(60652, "DisableRebootWifi", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisableDevelopPhotos = BooleanOptionItem.Create(60653, "DisableDevelopPhotos", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisableRewindTapes = BooleanOptionItem.Create(60654, "DisableRewindTapes", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisableStartFans = BooleanOptionItem.Create(60655, "DisableStartFans", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisableCollectVegetables = BooleanOptionItem.Create(60633, "DisableCollectVegetables", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisableMineOres = BooleanOptionItem.Create(60634, "DisableMineOres", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisableExtractFuel = BooleanOptionItem.Create(60635, "DisableExtractFuel", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisableCatchFish = BooleanOptionItem.Create(60636, "DisableCatchFish", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisablePolishGem = BooleanOptionItem.Create(60637, "DisablePolishGem", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisableHelpCritter = BooleanOptionItem.Create(60638, "DisableHelpCritter", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);
        DisableHoistSupplies = BooleanOptionItem.Create(60639, "DisableHoistSupplies", false, TabGroup.ModSettings, false)
            .SetParent(DisableLongTasks);



        //Disable Divert Power, Weather Nodes and etc. situational Tasks
        DisableOtherTasks = BooleanOptionItem.Create(60665, "DisableOtherTasks", false, TabGroup.ModSettings, false)
            .HideInFFA()
            .HideInUltimate()
            .SetColor(new Color32(239, 89, 175, byte.MaxValue));
        DisableUploadData = BooleanOptionItem.Create(60666, "DisableUploadDataTask", false, TabGroup.ModSettings, false)
            .SetParent(DisableOtherTasks);
        DisableEmptyGarbage = BooleanOptionItem.Create(60667, "DisableEmptyGarbage", false, TabGroup.ModSettings, false)
            .SetParent(DisableOtherTasks);
        DisableFuelEngines = BooleanOptionItem.Create(60668, "DisableFuelEngines", false, TabGroup.ModSettings, false)
            .SetParent(DisableOtherTasks);
        DisableDivertPower = BooleanOptionItem.Create(60669, "DisableDivertPower", false, TabGroup.ModSettings, false)
            .SetParent(DisableOtherTasks);
        DisableActivateWeatherNodes = BooleanOptionItem.Create(60670, "DisableActivateWeatherNodes", false, TabGroup.ModSettings, false)
            .SetParent(DisableOtherTasks);


        // Meeting Settings
        TextOptionItem.Create(10000030, "MenuTitle.Meeting", TabGroup.ModSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue));
        // Sync Button
        SyncButtonMode = BooleanOptionItem.Create(60700, "SyncButtonMode", false, TabGroup.ModSettings, false)
            .SetHeader(true)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue))
            .SetGameMode(CustomGameMode.Standard);
        SyncedButtonCount = IntegerOptionItem.Create(60701, "SyncedButtonCount", new(0, 100, 1), 10, TabGroup.ModSettings, false)
            .SetParent(SyncButtonMode)
            .SetValueFormat(OptionFormat.Times)
            .SetGameMode(CustomGameMode.Standard);
        // 全员存活时的会议时间
        AllAliveMeeting = BooleanOptionItem.Create(60710, "AllAliveMeeting", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue));
        AllAliveMeetingTime = FloatOptionItem.Create(60711, "AllAliveMeetingTime", new(1f, 300f, 1f), 10f, TabGroup.ModSettings, false)
            .SetParent(AllAliveMeeting)
            .SetValueFormat(OptionFormat.Seconds);
        // 附加紧急会议
        AdditionalEmergencyCooldown = BooleanOptionItem.Create(60720, "AdditionalEmergencyCooldown", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue));
        AdditionalEmergencyCooldownThreshold = IntegerOptionItem.Create(60721, "AdditionalEmergencyCooldownThreshold", new(1, 15, 1), 1, TabGroup.ModSettings, false)
            .SetParent(AdditionalEmergencyCooldown)
            .SetGameMode(CustomGameMode.Standard)
            .SetValueFormat(OptionFormat.Players);
        AdditionalEmergencyCooldownTime = FloatOptionItem.Create(60722, "AdditionalEmergencyCooldownTime", new(1f, 60f, 1f), 1f, TabGroup.ModSettings, false)
                .SetParent(AdditionalEmergencyCooldown)
            .SetGameMode(CustomGameMode.Standard)
            .SetValueFormat(OptionFormat.Seconds);
        // 投票相关设定
        VoteMode = BooleanOptionItem.Create(60730, "VoteMode", false, TabGroup.ModSettings, false)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue))
            .SetGameMode(CustomGameMode.Standard);
        WhenSkipVote = StringOptionItem.Create(60731, "WhenSkipVote", voteModes[0..3], 0, TabGroup.ModSettings, false)
            .SetParent(VoteMode)
            .SetGameMode(CustomGameMode.Standard);
        WhenSkipVoteIgnoreFirstMeeting = BooleanOptionItem.Create(60732, "WhenSkipVoteIgnoreFirstMeeting", false, TabGroup.ModSettings, false)
            .SetParent(WhenSkipVote)
            .SetGameMode(CustomGameMode.Standard);
        WhenSkipVoteIgnoreNoDeadBody = BooleanOptionItem.Create(60733, "WhenSkipVoteIgnoreNoDeadBody", false, TabGroup.ModSettings, false)
            .SetParent(WhenSkipVote)
            .SetGameMode(CustomGameMode.Standard);
        WhenSkipVoteIgnoreEmergency = BooleanOptionItem.Create(60734, "WhenSkipVoteIgnoreEmergency", false, TabGroup.ModSettings, false)
            .SetParent(WhenSkipVote)
            .SetGameMode(CustomGameMode.Standard);
        WhenNonVote = StringOptionItem.Create(60735, "WhenNonVote", voteModes, 0, TabGroup.ModSettings, false)
            .SetParent(VoteMode)
            .SetGameMode(CustomGameMode.Standard);
        WhenTie = StringOptionItem.Create(60745, "WhenTie", tieModes, 0, TabGroup.ModSettings, false)
            .SetParent(VoteMode)
            .SetGameMode(CustomGameMode.Standard);
        EnableVoteCommand = BooleanOptionItem.Create(60746, "EnableVote", true, TabGroup.ModSettings, false)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue))
            .SetGameMode(CustomGameMode.Standard);
        ShouldVoteCmdsSpamChat = BooleanOptionItem.Create(60747, "ShouldVoteSpam", false, TabGroup.ModSettings, false)
            .SetParent(EnableVoteCommand)
            .SetGameMode(CustomGameMode.Standard);
        // 其它设定
        TextOptionItem.Create(10000031, "MenuTitle.Other", TabGroup.ModSettings)
            .HideInFFA()
            .HideInUltimate()
            .HideInCandR()
            .SetColor(new Color32(193, 255, 209, byte.MaxValue));
        // 梯子摔死
        LadderDeath = BooleanOptionItem.Create(60760, "LadderDeath", false, TabGroup.ModSettings, false)
            .SetColor(new Color32(193, 255, 209, byte.MaxValue))
            .HideInFFA()
            .HideInUltimate()
            .HideInCandR();
        LadderDeathChance = StringOptionItem.Create(60761, "LadderDeathChance", EnumHelper.GetAllNames<SpawnChance>()[1..], 0, TabGroup.ModSettings, false)
            .SetParent(LadderDeath);

        // Reset Kill Cooldown
        FixFirstKillCooldown = BooleanOptionItem.Create(60770, "FixFirstKillCooldown", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(193, 255, 209, byte.MaxValue));
        ChangeFirstKillCooldown = BooleanOptionItem.Create(60772, "ChangeFirstKillCooldown", true, TabGroup.ModSettings, false)
            .SetParent(FixFirstKillCooldown);
        FixKillCooldownValue = FloatOptionItem.Create(60771, "FixKillCooldownValue", new(0f, 180f, 2.5f), 15f, TabGroup.ModSettings, false)
            .SetValueFormat(OptionFormat.Seconds)
            .SetParent(ChangeFirstKillCooldown);
        // First dead shield
        ShieldPersonDiedFirst = BooleanOptionItem.Create(60780, "ShieldPersonDiedFirst", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(193, 255, 209, byte.MaxValue));

        ShowShieldedPlayerToAll = BooleanOptionItem.Create(60871, "ShowShieldedPlayerToAll", true, TabGroup.ModSettings, false).SetParent(ShieldPersonDiedFirst)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(193, 255, 209, byte.MaxValue));

        RemoveShieldOnFirstDead = BooleanOptionItem.Create(60872, "RemoveShieldOnFirstDead", false, TabGroup.ModSettings, false).SetParent(ShieldPersonDiedFirst)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(193, 255, 209, byte.MaxValue));

        ShieldedCanUseKillButton = BooleanOptionItem.Create(60782, "ShieldedCanUseKillButton", true, TabGroup.ModSettings, false).SetParent(ShieldPersonDiedFirst)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(193, 255, 209, byte.MaxValue));

        EveryoneCanSeeDeathReason = BooleanOptionItem.Create(60781, "EveryoneCanSeeDeathReason", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(193, 255, 209, byte.MaxValue));

        // 杀戮闪烁持续
        KillFlashDuration = FloatOptionItem.Create(60790, "KillFlashDuration", new(0.1f, 0.45f, 0.05f), 0.3f, TabGroup.ModSettings, false)
            .SetColor(new Color32(193, 255, 209, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        NonCrewRandomCommonTasks = BooleanOptionItem.Create(60791, "NonCrewRandomCommonTasks", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(193, 255, 209, byte.MaxValue));
        // 幽灵相关设定
        TextOptionItem.Create(10000032, "MenuTitle.Ghost", TabGroup.ModSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        // 幽灵设置
        GhostIgnoreTasks = BooleanOptionItem.Create(60800, "GhostIgnoreTasks", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        GhostCanSeeOtherRoles = BooleanOptionItem.Create(60810, "GhostCanSeeOtherRoles", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        PreventSeeRolesImmediatelyAfterDeath = BooleanOptionItem.Create(60821, "PreventSeeRolesImmediatelyAfterDeath", true, TabGroup.ModSettings, false)
            .SetParent(GhostCanSeeOtherRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        GhostCanSeeOtherVotes = BooleanOptionItem.Create(60820, "GhostCanSeeOtherVotes", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        GhostCanSeeDeathReason = BooleanOptionItem.Create(60830, "GhostCanSeeDeathReason", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        ConvertedCanBecomeGhost = BooleanOptionItem.Create(60840, "ConvertedCanBeGhostRole", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        NeutralCanBecomeGhost = BooleanOptionItem.Create(60841, "NeutralCanBeGhostRole", false, TabGroup.ModSettings, false)
            .SetParent(ConvertedCanBecomeGhost)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));

        MaxImpGhost = IntegerOptionItem.Create(60850, "MaxImpGhostRole", new(0, 15, 1), 15, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetValueFormat(OptionFormat.Times)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        MaxCrewGhost = IntegerOptionItem.Create(60860, "MaxCrewGhostRole", new(0, 15, 1), 15, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetValueFormat(OptionFormat.Times)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        // MaxNeutralGhost = IntegerOptionItem.Create(60870, "MaxNeutralGhostRole", new(0, 15, 1), 15, TabGroup.ModSettings, false)
        // .SetGameMode(CustomGameMode.Standard)
        // .SetValueFormat(OptionFormat.Times)
        // .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        DefaultAngelCooldown = FloatOptionItem.Create(60880, "DefaultAngelCooldown", new(2.5f, 120f, 2.5f), 35f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetValueFormat(OptionFormat.Seconds)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));

        // Anomaly Settings

        TextOptionItem.Create(10000033, "MenuTitle.GameModifiers", TabGroup.ModSettings)
           .HideInFFA()
           .HideInUltimate()
           .HideInCandR()
           .SetColor(new Color32(168, 50, 62, byte.MaxValue));
        EnableAnomalies = BooleanOptionItem.Create(60890, "EnableAnomalies", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(168, 50, 62, byte.MaxValue));
        ClownFest = BooleanOptionItem.Create(60910, "ClownFest", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(EnableAnomalies);
        Retrial = BooleanOptionItem.Create(60920, "Retrial", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(EnableAnomalies);
        NewYear = BooleanOptionItem.Create(60930, "NewYear", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(EnableAnomalies);
        Holiday = BooleanOptionItem.Create(60940, "Holiday", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(EnableAnomalies);
        Shuffle = BooleanOptionItem.Create(60950, "Shuffle", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(EnableAnomalies);
        CrazyColors = BooleanOptionItem.Create(60960, "CrazyColors", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(EnableAnomalies);
        ColorChangeCoolDown = IntegerOptionItem.Create(60970, "CrazyColorsCD", new(5, 15, 1), 10, TabGroup.ModSettings, false)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(CrazyColors);
        AnomalyMeetingPCT = IntegerOptionItem.Create(60980, "AnomalyMeetingPCT", new(0, 100, 5), 0, TabGroup.ModSettings, false)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(EnableAnomalies);
        /* EnableWills = BooleanOptionItem.Create(60970, "EnableWills", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(3, 219, 252, byte.MaxValue));
        */


        #endregion

        yield return null;

        // End Load Settings
        OptionSaver.Load();
        IsLoaded = true;
        Logger.Msg("Mod option loading eng", "Load Options");
    }

    public static void SetupRoleOptions(int id, TabGroup tab, CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard, bool zeroOne = false)
    {
        var spawnOption = StringOptionItem.Create(id, role.ToString(), zeroOne ? EnumHelper.GetAllNames<RatesZeroOne>() : EnumHelper.GetAllNames<SpawnChance>(), 0, tab, false).SetColor(Utils.GetRoleColor(role))
            .SetHeader(true)
            .SetGameMode(customGameMode) as StringOptionItem;

        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, 15, 1), 1, tab, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Players)
            .SetGameMode(customGameMode);

        if (role.IsGhostRole())
        {
            CustomGhostRoleCounts.Add(role, countOption);
        }

        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }
    private static void SetupLoversRoleOptionsToggle(int id, CustomGameMode customGameMode = CustomGameMode.Standard)
    {
        var role = CustomRoles.Lovers;
        var spawnOption = StringOptionItem.Create(id, role.ToString(), EnumHelper.GetAllNames<RatesZeroOne>(), 0, TabGroup.Addons, false).SetColor(Utils.GetRoleColor(role))
            .SetHeader(true)
            .SetGameMode(customGameMode) as StringOptionItem;

        LoverSpawnChances = IntegerOptionItem.Create(id + 2, "LoverSpawnChances", new(0, 100, 5), 50, TabGroup.Addons, false)
        .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(customGameMode);

        LoverKnowRoles = BooleanOptionItem.Create(id + 4, "LoverKnowRoles", true, TabGroup.Addons, false)
        .SetParent(spawnOption)
            .SetGameMode(customGameMode);

        LoverSuicide = BooleanOptionItem.Create(id + 3, "LoverSuicide", true, TabGroup.Addons, false)
        .SetParent(spawnOption)
            .SetGameMode(customGameMode);

        ImpCanBeInLove = BooleanOptionItem.Create(id + 5, "ImpCanBeInLove", true, TabGroup.Addons, false)
        .SetParent(spawnOption)
            .SetGameMode(customGameMode);

        CrewCanBeInLove = BooleanOptionItem.Create(id + 6, "CrewCanBeInLove", true, TabGroup.Addons, false)
        .SetParent(spawnOption)
            .SetGameMode(customGameMode);

        NeutralCanBeInLove = BooleanOptionItem.Create(id + 7, "NeutralCanBeInLove", true, TabGroup.Addons, false)
        .SetParent(spawnOption)
            .SetGameMode(customGameMode);

        CovenCanBeInLove = BooleanOptionItem.Create(id + 8, "CovenCanBeInLove", true, TabGroup.Addons, false)
        .SetParent(spawnOption)
            .SetGameMode(customGameMode);

        WidowChance = IntegerOptionItem.Create(id + 9, "LoverWidowChance", new(0, 100, 5), 20, TabGroup.Addons, false)
            .SetParent(spawnOption)
            .SetValueFormat((OptionFormat.Percent))
            .SetGameMode(customGameMode);

        var countOption = IntegerOptionItem.Create(id + 1, "NumberOfLovers", new(2, 2, 1), 2, TabGroup.Addons, false)
            .SetParent(spawnOption)
            .SetHidden(true)
            .SetGameMode(customGameMode);

        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }

    public static void SetupAdtRoleOptions(int id, CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard, bool canSetNum = false, TabGroup tab = TabGroup.Addons, bool canSetChance = true, bool teamSpawnOptions = false)
    {
        var spawnOption = StringOptionItem.Create(id, role.ToString(), EnumHelper.GetAllNames<RatesZeroOne>(), 0, tab, false).SetColor(Utils.GetRoleColor(role))
            .SetHeader(true)
            .SetGameMode(customGameMode) as StringOptionItem;

        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, canSetNum ? 15 : 1, 1), 1, tab, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Players)
            .SetHidden(!canSetNum)
            .SetGameMode(customGameMode);

        var spawnRateOption = IntegerOptionItem.Create(id + 2, "AdditionRolesSpawnRate", new(0, 100, 5), canSetChance ? 65 : 100, tab, false)
        .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Percent)
            .SetHidden(!canSetChance)
            .SetGameMode(customGameMode) as IntegerOptionItem;

        if (teamSpawnOptions)
        {
            var impOption = BooleanOptionItem.Create(id + 3, "ImpCanBeRole", true, tab, false)
                .SetParent(spawnOption)
                .SetGameMode(customGameMode)
                .AddReplacement(("{role}", role.ToColoredString()));

            var neutralOption = BooleanOptionItem.Create(id + 4, "NeutralCanBeRole", true, tab, false)
                .SetParent(spawnOption)
                .SetGameMode(customGameMode)
                .AddReplacement(("{role}", role.ToColoredString()));

            var crewOption = BooleanOptionItem.Create(id + 5, "CrewCanBeRole", true, tab, false)
                .SetParent(spawnOption)
                .SetGameMode(customGameMode)
                .AddReplacement(("{role}", role.ToColoredString()));

            var covenOption = BooleanOptionItem.Create(id + 6, "CovenCanBeRole", true, tab, false)
                .SetParent(spawnOption)
                .SetGameMode(customGameMode)
                .AddReplacement(("{role}", role.ToColoredString()));

            AddonCanBeSettings.Add(role, (impOption, neutralOption, crewOption, covenOption));
        }


        CustomAdtRoleSpawnRate.Add(role, spawnRateOption);
        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }

    public static void SetupSingleRoleOptions(int id, TabGroup tab, CustomRoles role, int count = 1, CustomGameMode customGameMode = CustomGameMode.Standard, bool zeroOne = false)
    {
        var spawnOption = StringOptionItem.Create(id, role.ToString(), zeroOne ? EnumHelper.GetAllNames<RatesZeroOne>() : EnumHelper.GetAllNames<SpawnChance>(), 0, tab, false).SetColor(Utils.GetRoleColor(role))
            .SetHeader(true)
            .SetGameMode(customGameMode) as StringOptionItem;

        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(count, count, count), count, tab, false)
            .SetParent(spawnOption)
            .SetHidden(true)
            .SetGameMode(customGameMode);

        if (role.IsGhostRole())
        {
            CustomGhostRoleCounts.Add(role, countOption);
        }

        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }
    public class OverrideTasksData
    {
        public static Dictionary<CustomRoles, OverrideTasksData> AllData = [];
        public CustomRoles Role { get; private set; }
        public int IdStart { get; private set; }
        public OptionItem doOverride;
        public OptionItem assignCommonTasks;
        public OptionItem numLongTasks;
        public OptionItem numShortTasks;

        public OverrideTasksData(int idStart, TabGroup tab, CustomRoles role)
        {
            IdStart = idStart;
            Role = role;
            Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), Utils.GetRoleName(role)) } };
            doOverride = BooleanOptionItem.Create(idStart++, "doOverride", false, tab, false)
                .SetParent(CustomRoleSpawnChances[role])
                .SetValueFormat(OptionFormat.None);
            doOverride.ReplacementDictionary = replacementDic;
            assignCommonTasks = BooleanOptionItem.Create(idStart++, "assignCommonTasks", true, tab, false)
                .SetParent(doOverride)
                .SetValueFormat(OptionFormat.None);
            assignCommonTasks.ReplacementDictionary = replacementDic;
            numLongTasks = IntegerOptionItem.Create(idStart++, "roleLongTasksNum", new(0, 99, 1), 3, tab, false)
                .SetParent(doOverride)
                .SetValueFormat(OptionFormat.Pieces);
            numLongTasks.ReplacementDictionary = replacementDic;
            numShortTasks = IntegerOptionItem.Create(idStart++, "roleShortTasksNum", new(0, 99, 1), 3, tab, false)
                .SetParent(doOverride)
                .SetValueFormat(OptionFormat.Pieces);
            numShortTasks.ReplacementDictionary = replacementDic;

            if (!AllData.ContainsKey(role)) AllData.Add(role, this);
            else Logger.Warn("重複したCustomRolesを対象とするOverrideTasksDataが作成されました", "OverrideTasksData");
        }
        public static OverrideTasksData Create(int idStart, TabGroup tab, CustomRoles role)
        {
            return new OverrideTasksData(idStart, tab, role);
        }
    }
}
