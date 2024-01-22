using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TOHE.Modules;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnityEngine;

namespace TOHE;

[Flags]
public enum CustomGameMode
{
    Standard = 0x01,
    FFA = 0x02,

    HidenSeekTOHE = 0x08, // HidenSeekTOHE must be after other game modes
    All = int.MaxValue
}

[HarmonyPatch]
public static class Options
{
    static Task taskOptionsLoad;
    [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.Initialize)), HarmonyPostfix]
    public static void OptionsLoadStart_Postfix()
    {
        Logger.Msg("Mod option loading start", "Load Options");
        taskOptionsLoad = Task.Run(Load);
        taskOptionsLoad.ContinueWith(t => { Logger.Msg("Mod option loading end", "Load Options"); });
    }
    //[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix]
    //public static void WaitOptionsLoad_Postfix()
    //{
    //    taskOptionsLoad.Wait();
    //    Logger.Info("Mod option loading eng", "Load Options");
    //}

    // Presets
    private static readonly string[] presets =
    {
        Main.Preset1.Value, Main.Preset2.Value, Main.Preset3.Value,
        Main.Preset4.Value, Main.Preset5.Value
    };

    // Custom Game Mode
    public static OptionItem GameMode;
    public static CustomGameMode CurrentGameMode
        => GameMode.GetInt() switch
        {
            1 => CustomGameMode.FFA,
            2 => CustomGameMode.HidenSeekTOHE, // HidenSeekTOHE must be after other game modes
            _ => CustomGameMode.Standard
        };

    public static readonly string[] gameModes =
    {
        "Standard",
        "FFA",


        "Hide&SeekTOHE", // HidenSeekTOHE must be after other game modes
    };

    // 役職数・確率
    public static Dictionary<CustomRoles, int> roleCounts;
    public static Dictionary<CustomRoles, float> roleSpawnChances;
    public static Dictionary<CustomRoles, OptionItem> CustomRoleCounts;
    public static Dictionary<CustomRoles, StringOptionItem> CustomRoleSpawnChances;
    public static Dictionary<CustomRoles, IntegerOptionItem> CustomAdtRoleSpawnRate;
    public static readonly string[] rates =
    {
        "Rate0",  "Rate5",  "Rate10", "Rate20", "Rate30", "Rate40",
        "Rate50", "Rate60", "Rate70", "Rate80", "Rate90", "Rate100",
    };
    public static readonly string[] ratesZeroOne =
    {
        "RoleOff", /*"Rate10", "Rate20", "Rate30", "Rate40", "Rate50",
        "Rate60", "Rate70", "Rate80", "Rate90", */"RoleRate",
    };
    public static readonly string[] ratesToggle =
    {
        "RoleOff", "RoleRate", "RoleOn"
    };
    public static readonly string[] CheatResponsesName =
    {
        "Ban", "Kick", "NoticeMe","NoticeEveryone", "TempBan", "OnlyCancel"
    };
    public static readonly string[] ConfirmEjectionsMode =
    {
        "ConfirmEjections.None",
        "ConfirmEjections.Team",
        "ConfirmEjections.Role"
    };
    public static readonly string[] CamouflageMode =
    {
        "CamouflageMode.Default",
        "CamouflageMode.Host",
        "CamouflageMode.Random",
        "CamouflageMode.OnlyRandomColor",
        "CamouflageMode.Karpe",
        "CamouflageMode.Lauryn",
        "CamouflageMode.Moe",
        "CamouflageMode.Pyro",
        "CamouflageMode.ryuk",
        "CamouflageMode.Gurge44",
        "CamouflageMode.TommyXL"
    };

    // 各役職の詳細設定
    //public static OptionItem EnableGM;
    public static float DefaultKillCooldown = Main.NormalOptions?.KillCooldown ?? 20;
    public static OptionItem GhostsDoTasks;


    // ------------ System Settings Tab ------------
    public static OptionItem TemporaryAntiBlackoutFix;
    public static OptionItem GradientTagsOpt;
    public static OptionItem EnableKillerLeftCommand;
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

    public static OptionItem KickPlayerFriendCodeNotExist;
    public static OptionItem TempBanPlayerFriendCodeNotExist;

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
    public static OptionItem AutoPlayAgain;
    public static OptionItem AutoPlayAgainCountdown;

    //public static OptionItem ShowLobbyCode;
    public static OptionItem LowLoadMode;
    public static OptionItem EndWhenPlayerBug;
    public static OptionItem HideExileChat;
    public static OptionItem RemovePetsAtDeadPlayers;

    public static OptionItem CheatResponses;
    public static OptionItem NewHideMsg;

    public static OptionItem AutoDisplayKillLog;
    public static OptionItem AutoDisplayLastRoles;
    public static OptionItem AutoDisplayLastResult;

    public static OptionItem SuffixMode;
    public static OptionItem HideHostText;
    public static OptionItem HideGameSettings;

    public static OptionItem PlayerCanSetColor;
    public static OptionItem PlayerCanSetName;
    public static OptionItem PlayerCanUseQuitCommand;
    public static OptionItem FormatNameMode;
    public static OptionItem DisableEmojiName;
    //public static OptionItem ColorNameMode;
    public static OptionItem ChangeNameToRoleInfo;
    public static OptionItem SendRoleDescriptionFirstMeeting;

    public static OptionItem NoGameEnd;
    public static OptionItem AllowConsole;

    public static OptionItem RoleAssigningAlgorithm;
    public static OptionItem KPDCamouflageMode;
    public static OptionItem EnableUpMode;


    // ------------ Game Settings Tab ------------

    // Hide & Seek Setting
    public static OptionItem NumImpostorsHnS;

    // Confirm Ejection
    public static OptionItem CEMode;
    public static OptionItem ShowImpRemainOnEject;
    public static OptionItem ShowNKRemainOnEject;
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

    public static OptionItem RandomSpawn;
    public static OptionItem SpawnRandomLocation;
    public static OptionItem AirshipAdditionalSpawn;
    public static OptionItem SpawnRandomVents;

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
    public static OptionItem DisableDevicesIgnoreCrewmates;
    public static OptionItem DisableDevicesIgnoreAfterAnyoneDied;

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
    public static OptionItem FixKillCooldownValue;
    public static OptionItem ShieldPersonDiedFirst;

    public static OptionItem KillFlashDuration;

    // Ghost
    public static OptionItem GhostIgnoreTasks;
    public static OptionItem GhostCanSeeOtherRoles;
    public static OptionItem GhostCanSeeOtherVotes;
    public static OptionItem GhostCanSeeDeathReason;


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
    public static OptionItem PassiveNeutralsCanGuess;
    public static OptionItem CanGuessAddons;
    public static OptionItem ImpCanGuessImp;
    public static OptionItem CrewCanGuessCrew;
    public static OptionItem HideGuesserCommands;
    public static OptionItem ShowOnlyEnabledRolesInGuesserUI;

    public static OptionItem ImpCanBeOnbound;
    public static OptionItem CrewCanBeOnbound;
    public static OptionItem NeutralCanBeOnbound;


    // ------------ General Role Settings ------------

    // Imp
    public static OptionItem ImpKnowAlliesRole;
    public static OptionItem ImpKnowWhosMadmate;
    public static OptionItem ImpCanKillMadmate;
    public static OptionItem MadmateKnowWhosMadmate;
    public static OptionItem MadmateKnowWhosImp;
    public static OptionItem MadmateCanKillImp;
    public static OptionItem MadmateHasImpostorVision;
    public static OptionItem RefugeeKillCD;
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

    // Add-on
    public static OptionItem NameDisplayAddons;
    public static OptionItem NoLimitAddonsNumMax;
    public static OptionItem AddBracketsToAddons;

    // Impostors role settings
    public static OptionItem ShapeshiftCD;
    public static OptionItem ShapeshiftDur;

    public static OptionItem BerserkerKillCooldown;
    public static OptionItem BerserkerMax;
    public static OptionItem BerserkerOneCanKillCooldown;
    public static OptionItem BerserkerKillCooldownLevel;
    public static OptionItem BerserkerOneKillCooldown;
    public static OptionItem BerserkerTwoCanScavenger;
    public static OptionItem BerserkerScavengerLevel;
    public static OptionItem BerserkerThreeCanBomber;
    public static OptionItem BerserkerBomberLevel;
    //public static OptionItem BerserkerFourCanFlash;
    //public static OptionItem BerserkerSpeed;
    public static OptionItem BerserkerFourCanNotKill;
    public static OptionItem BerserkerImmortalLevel;

    public static OptionItem BomberRadius;
    public static OptionItem BomberCanKill;
    public static OptionItem BomberKillCD;
    public static OptionItem BombCooldown;
    public static OptionItem ImpostorsSurviveBombs;
    public static OptionItem BomberDiesInExplosion;
    public static OptionItem NukerChance;
    public static OptionItem NukeRadius;
    public static OptionItem NukeCooldown;

    public static OptionItem GuardSpellTimes;
    public static OptionItem killAttacker;

    public static OptionItem EGCanGuessTime;
    public static OptionItem EGCanGuessImp;
    public static OptionItem EGCanGuessAdt;
    public static OptionItem EGCanGuessTaskDoneSnitch;
    public static OptionItem EGTryHideMsg;

    public static OptionItem InhibitorCD;

    public static OptionItem LudopathRandomKillCD;

    public static OptionItem SaboteurCD;

    public static OptionItem BTKillCooldown;
    public static OptionItem TrapConsecutiveBodies;
    public static OptionItem TrapTrapsterBody;
    public static OptionItem TrapConsecutiveTrapsterBodies;
    //public static OptionItem TrapOnlyWorksOnTheBodyBoobyTrap;

    public static OptionItem UnderdogMaximumPlayersNeededToKill;
    public static OptionItem UnderdogKillCooldown;

    public static OptionItem CleanerKillCooldown;
    public static OptionItem KillCooldownAfterCleaning;

    public static OptionItem GodfatherChangeOpt;

    public static OptionItem MafiaCanKillNum;
    public static OptionItem LegacyMafia;
    public static OptionItem MafiaShapeshiftCD;
    public static OptionItem MafiaShapeshiftDur;

    public static OptionItem VindicatorAdditionalVote;
    public static OptionItem VindicatorHideVote;

    public static OptionItem EscapeeSSDuration;
    public static OptionItem EscapeeSSCD;

    public static OptionItem MinerSSDuration;
    public static OptionItem MinerSSCD;

    public static OptionItem ScavengerKillCooldown;

    public static OptionItem ShapeMasterShapeshiftDuration;

    public static OptionItem ShapeImperiusCurseShapeshiftDuration;
    public static OptionItem ImperiusCurseShapeshiftCooldown;

    public static OptionItem WarlockCanKillAllies;
    public static OptionItem WarlockCanKillSelf;
    public static OptionItem WarlockShiftDuration;

    // Madmate
    public static OptionItem CrewpostorCanKillAllies;
    public static OptionItem CrewpostorKnowsAllies;
    public static OptionItem AlliesKnowCrewpostor;
    public static OptionItem CrewpostorLungeKill;
    public static OptionItem CrewpostorKillAfterTask;

    public static OptionItem ParasiteCD;


    // Crewmates role settings
    public static OptionItem ScientistCD;
    public static OptionItem ScientistDur;

    public static OptionItem ImpKnowCyberStarDead;
    public static OptionItem NeutralKnowCyberStarDead;

    public static OptionItem DoctorTaskCompletedBatteryCharge;
    public static OptionItem DoctorVisibleToEveryone;

   //public static OptionItem LuckeyProbability;

    public static OptionItem EveryOneKnowSuperStar;

    public static OptionItem TransporterTeleportMax;

    public static OptionItem BecomeBaitDelayNotify;
    public static OptionItem BecomeBaitDelayMin;
    public static OptionItem BecomeBaitDelayMax;
    public static OptionItem BecomeTrapperBlockMoveTime;

    public static OptionItem DetectiveCanknowKiller;

    public static OptionItem GrenadierSkillCooldown;
    public static OptionItem GrenadierSkillDuration;
    public static OptionItem GrenadierCauseVision;
    public static OptionItem GrenadierCanAffectNeutral;
    public static OptionItem GrenadierSkillMaxOfUseage;
    public static OptionItem GrenadierAbilityUseGainWithEachTaskCompleted;

    public static OptionItem LighterVisionNormal;
    public static OptionItem LighterVisionOnLightsOut;
    public static OptionItem LighterSkillCooldown;
    public static OptionItem LighterSkillDuration;
    public static OptionItem LighterSkillMaxOfUseage;
    public static OptionItem LighterAbilityUseGainWithEachTaskCompleted;

    public static OptionItem DovesOfNeaceCooldown;
    public static OptionItem DovesOfNeaceMaxOfUseage;
    public static OptionItem DovesOfNeaceAbilityUseGainWithEachTaskCompleted;

    //public static OptionItem ParanoiaNumOfUseButton;
    //public static OptionItem ParanoiaVentCooldown;

    public static OptionItem TimeMasterSkillCooldown;
    public static OptionItem TimeMasterSkillDuration;
    public static OptionItem TimeMasterMaxUses;
    public static OptionItem TimeMasterAbilityUseGainWithEachTaskCompleted;

    public static OptionItem WitnessCD;
    public static OptionItem WitnessTime;

    public static OptionItem BombsClearAfterMeeting;
    public static OptionItem BastionBombCooldown;
    public static OptionItem BastionAbilityUseGainWithEachTaskCompleted;
    public static OptionItem BastionMaxBombs;

    public static OptionItem BodyguardProtectRadius;

    public static OptionItem GGCanGuessTime;
    public static OptionItem GGCanGuessCrew;
    public static OptionItem GGCanGuessAdt;
    public static OptionItem GGTryHideMsg;

    public static OptionItem RetributionistCanKillNum;
    public static OptionItem MinimumPlayersAliveToRetri;
    public static OptionItem CanOnlyRetributeWithTasksDone;

    public static OptionItem VeteranSkillCooldown;
    public static OptionItem VeteranSkillDuration;
    public static OptionItem VeteranSkillMaxOfUseage;
    public static OptionItem VeteranAbilityUseGainWithEachTaskCompleted;

    public static OptionItem VigilanteKillCooldown;

    public static OptionItem MayorAdditionalVote;
    public static OptionItem MayorHasPortableButton;
    public static OptionItem MayorNumOfUseButton;
    public static OptionItem MayorHideVote;
    public static OptionItem MayorRevealWhenDoneTasks;


    // Neutrals role settings
    public static OptionItem OppoImmuneToAttacksWhenTasksDone;

    public static OptionItem VoodooCooldown;

    public static OptionItem InnocentCanWinByImp;

    public static OptionItem JesterCanUseButton;
    public static OptionItem JesterHasImpostorVision;
    public static OptionItem JesterCanVent;
    public static OptionItem JesterVision;
    public static OptionItem MeetingsNeededForJesterWin;
    public static OptionItem HideJesterVote;
    public static OptionItem SunnyboyChance;

    public static OptionItem MasochistKillMax;

    public static OptionItem PhantomCanVent;
    public static OptionItem PhantomSnatchesWin;
    public static OptionItem PhantomCanGuess;

    public static OptionItem ProvKillCD;

    public static OptionItem RevolutionistDrawTime;
    public static OptionItem RevolutionistCooldown;
    public static OptionItem RevolutionistDrawCount;
    public static OptionItem RevolutionistKillProbability;
    public static OptionItem RevolutionistVentCountDown;

    public static OptionItem CanTerroristSuicideWin;
    public static OptionItem TerroristCanGuess;

    public static OptionItem MarioVentNumWin;
    public static OptionItem MarioVentCD;

    public static OptionItem WorkaholicCannotWinAtDeath;
    public static OptionItem WorkaholicVentCooldown;
    public static OptionItem WorkaholicVisibleToEveryone;
    public static OptionItem WorkaholicGiveAdviceAlive;
    public static OptionItem WorkaholicCanGuess;

    public static OptionItem ArsonistDouseTime;
    public static OptionItem ArsonistCooldown;
    //public static OptionItem ArsonistKeepsGameGoing;
    public static OptionItem ArsonistCanIgniteAnytime;
    public static OptionItem ArsonistMinPlayersToIgnite;
    public static OptionItem ArsonistMaxPlayersToIgnite;


    // Add-Ons settings
    public static OptionItem ImpCanBeAutopsy;
    public static OptionItem CrewCanBeAutopsy;
    public static OptionItem NeutralCanBeAutopsy;

    public static OptionItem ImpCanBeBait;
    public static OptionItem CrewCanBeBait;
    public static OptionItem NeutralCanBeBait;
    public static OptionItem BaitDelayMin;
    public static OptionItem BaitDelayMax;
    public static OptionItem BaitDelayNotify;
    public static OptionItem BaitNotification;
    public static OptionItem BaitCanBeReportedUnderAllConditions;

    public static OptionItem ImpCanBeTrapper;
    public static OptionItem CrewCanBeTrapper;
    public static OptionItem NeutralCanBeTrapper;
    public static OptionItem TrapperBlockMoveTime;

    public static OptionItem BewilderVision;
    public static OptionItem ImpCanBeBewilder;
    public static OptionItem CrewCanBeBewilder;
    public static OptionItem NeutralCanBeBewilder;
    public static OptionItem KillerGetBewilderVision;

    public static OptionItem ImpCanBeBurst;
    public static OptionItem CrewCanBeBurst;
    public static OptionItem NeutralCanBeBurst;
    public static OptionItem BurstKillDelay;

    public static OptionItem ImpCanBeCyber;
    public static OptionItem CrewCanBeCyber;
    public static OptionItem NeutralCanBeCyber;
    public static OptionItem ImpKnowCyberDead;
    public static OptionItem CrewKnowCyberDead;
    public static OptionItem NeutralKnowCyberDead;
    public static OptionItem CyberKnown;

    public static OptionItem ImpCanBeDoubleShot;
    public static OptionItem CrewCanBeDoubleShot;
    public static OptionItem NeutralCanBeDoubleShot;

    public static OptionItem TasklessCrewCanBeLazy;
    public static OptionItem TaskBasedCrewCanBeLazy;

    public static OptionItem ImpCanBeLoyal;
    public static OptionItem CrewCanBeLoyal;

    public static OptionItem LuckyProbability;
    public static OptionItem ImpCanBeLucky;
    public static OptionItem CrewCanBeLucky;
    public static OptionItem NeutralCanBeLucky;

    public static OptionItem ImpCanBeNecroview;
    public static OptionItem CrewCanBeNecroview;
    public static OptionItem NeutralCanBeNecroview;

    //public static OptionItem NeutralCanBeNimble;
    //public static OptionItem CrewCanBeNimble;

    public static OptionItem OverclockedReduction;

    public static OptionItem ImpCanBeSeer;
    public static OptionItem CrewCanBeSeer;
    public static OptionItem NeutralCanBeSeer;

    public static OptionItem ImpCanBeSleuth;
    public static OptionItem CrewCanBeSleuth;
    public static OptionItem NeutralCanBeSleuth;
    public static OptionItem SleuthCanKnowKillerRole;

    public static OptionItem ImpCanBeTiebreaker;
    public static OptionItem CrewCanBeTiebreaker;
    public static OptionItem NeutralCanBeTiebreaker;

    public static OptionItem TorchVision;
    public static OptionItem TorchAffectedByLights;

    public static OptionItem ImpCanBeWatcher;
    public static OptionItem CrewCanBeWatcher;
    public static OptionItem NeutralCanBeWatcher;
    //public static OptionItem EvilWatcherChance;

    public static OptionItem ImpCanBeUnreportable;
    public static OptionItem CrewCanBeUnreportable;
    public static OptionItem NeutralCanBeUnreportable;

    public static OptionItem ImpCanBeFragile;
    public static OptionItem CrewCanBeFragile;
    public static OptionItem NeutralCanBeFragile;
    public static OptionItem ImpCanKillFragile;
    public static OptionItem CrewCanKillFragile;
    public static OptionItem NeutralCanKillFragile;
    public static OptionItem FragileKillerLunge;

    public static OptionItem ImpCanBeOblivious;
    public static OptionItem CrewCanBeOblivious;
    public static OptionItem NeutralCanBeOblivious;
    public static OptionItem ObliviousBaitImmune;

    public static OptionItem RascalAppearAsMadmate;

    //public static OptionItem SunglassesVision;
    //public static OptionItem ImpCanBeSunglasses;
    //public static OptionItem CrewCanBeSunglasses;
    //public static OptionItem NeutralCanBeSunglasses;

    public static OptionItem UnluckyTaskSuicideChance;
    public static OptionItem UnluckyKillSuicideChance;
    public static OptionItem UnluckyVentSuicideChance;
    public static OptionItem UnluckyReportSuicideChance;
    public static OptionItem UnluckySabotageSuicideChance;
    public static OptionItem ImpCanBeUnlucky;
    public static OptionItem CrewCanBeUnlucky;
    public static OptionItem NeutralCanBeUnlucky;

    public static OptionItem ImpCanBeVoidBallot;
    public static OptionItem CrewCanBeVoidBallot;
    public static OptionItem NeutralCanBeVoidBallot;

    public static OptionItem ImpCanBeAntidote;
    public static OptionItem CrewCanBeAntidote;
    public static OptionItem NeutralCanBeAntidote;
    public static OptionItem AntidoteCDOpt;
    public static OptionItem AntidoteCDReset;

    public static OptionItem ImpCanBeAvanger;
    public static OptionItem CrewCanBeAvanger;
    public static OptionItem NeutralCanBeAvanger;

    public static OptionItem ImpCanBeAware;
    public static OptionItem CrewCanBeAware;
    public static OptionItem NeutralCanBeAware;
    public static OptionItem AwareknowRole;

    public static OptionItem ImpCanBeDiseased;
    public static OptionItem CrewCanBeDiseased;
    public static OptionItem NeutralCanBeDiseased;
    public static OptionItem DiseasedCDOpt;
    public static OptionItem DiseasedCDReset;

    //public static OptionItem ImpCanBeGlow;
    //public static OptionItem CrewCanBeGlow;
    //public static OptionItem NeutralCanBeGlow;
    //public static OptionItem GlowVision;

    public static OptionItem ImpCanBeGravestone;
    public static OptionItem CrewCanBeGravestone;
    public static OptionItem NeutralCanBeGravestone;

    public static OptionItem ImpCanBeGuesser;
    public static OptionItem CrewCanBeGuesser;
    public static OptionItem NeutralCanBeGuesser;
    public static OptionItem GCanGuessAdt;
    public static OptionItem GCanGuessTaskDoneSnitch;
    public static OptionItem GTryHideMsg;

    public static OptionItem ImpCanBeRebound;
    public static OptionItem CrewCanBeRebound;
    public static OptionItem NeutralCanBeRebound;

    public static OptionItem ImpCanBeDualPersonality;
    public static OptionItem CrewCanBeDualPersonality;
    public static OptionItem DualVotes;
    //public static OptionItem HideDualVotes;

    public static OptionItem ImpCanBeStubborn;
    public static OptionItem CrewCanBeStubborn;
    public static OptionItem NeutralCanBeStubborn;

    public static OptionItem ChanceToMiss;

    public static OptionItem MadmateSpawnMode;
    public static OptionItem MadmateCountMode;
    public static OptionItem SheriffCanBeMadmate;
    public static OptionItem MayorCanBeMadmate;
    public static OptionItem NGuesserCanBeMadmate;
    public static OptionItem MarshallCanBeMadmate;
    public static OptionItem FarseerCanBeMadmate;
    public static OptionItem RetributionistCanBeMadmate;
    public static OptionItem SnitchCanBeMadmate;
    public static OptionItem MadSnitchTasks;
    public static OptionItem JudgeCanBeMadmate;

    public static OptionItem MimicCanSeeDeadRoles;

    public static OptionItem TicketsPerKill;

    public static OptionItem CrewCanBeEgoist;
    public static OptionItem ImpCanBeEgoist;
    public static OptionItem ImpEgoistVisibalToAllies;
    public static OptionItem EgoistCountAsConverted;

    public static OptionItem LoverSpawnChances;
    public static OptionItem LoverKnowRoles;
    public static OptionItem LoverSuicide;
    public static OptionItem ImpCanBeInLove;
    public static OptionItem CrewCanBeInLove;
    public static OptionItem NeutralCanBeInLove;

    // Experimental Roles
    public static OptionItem MNKillCooldown;

    public static OptionItem ZombieKillCooldown;
    public static OptionItem ZombieSpeedReduce;

    //public static OptionItem CapitalismSkillCooldown;

    //public static OptionItem SpeedBoosterUpSpeed;
    //public static OptionItem SpeedBoosterTimes;

    public static OptionItem NotifyGodAlive;
    public static OptionItem GodCanGuess;

    public static OptionItem ImpCanBeFool;
    public static OptionItem CrewCanBeFool;
    public static OptionItem NeutralCanBeFool;


    public static VoteMode GetWhenSkipVote() => (VoteMode)WhenSkipVote.GetValue();
    public static VoteMode GetWhenNonVote() => (VoteMode)WhenNonVote.GetValue();

    public static readonly string[] voteModes =
    {
        "Default", "Suicide", "SelfVote", "Skip"
    };
    public static readonly string[] tieModes =
    {
        "TieMode.Default", "TieMode.All", "TieMode.Random"
    };
    /* public static readonly string[] addonGuessModeCrew =
     {
         "GuesserMode.All", "GuesserMode.Harmful", "GuesserMode.Random"
     }; */
    public static readonly string[] madmateSpawnMode =
    {
        "MadmateSpawnMode.Assign",
        "MadmateSpawnMode.FirstKill",
        "MadmateSpawnMode.SelfVote",
    };
    public static readonly string[] madmateCountMode =
    {
        "MadmateCountMode.None",
        "MadmateCountMode.Imp",
        "MadmateCountMode.Original",
    };
    public static readonly string[] sidekickCountMode =
    {
        "SidekickCountMode.Jackal",
        "SidekickCountMode.None",
        "SidekickCountMode.Original",
    };
    public static readonly string[] GodfatherChangeMode =
    {
        "GodfatherCount.Refugee",
        "GodfatherCount.Madmate"
    };
    public static readonly string[] suffixModes =
    {
        "SuffixMode.None",
        "SuffixMode.Version",
        "SuffixMode.Streaming",
        "SuffixMode.Recording",
        "SuffixMode.RoomHost",
        "SuffixMode.OriginalName",
        "SuffixMode.DoNotKillMe",
        "SuffixMode.NoAndroidPlz",
        "SuffixMode.AutoHost"    };
    public static readonly string[] roleAssigningAlgorithms =
    {
        "RoleAssigningAlgorithm.Default",
        "RoleAssigningAlgorithm.NetRandom",
        "RoleAssigningAlgorithm.HashRandom",
        "RoleAssigningAlgorithm.Xorshift",
        "RoleAssigningAlgorithm.MersenneTwister",
    };
    public static readonly string[] formatNameModes =
    {
        "FormatNameModes.None",
        "FormatNameModes.Color",
        "FormatNameModes.Snacks",
    };
    public static SuffixModes GetSuffixMode() => (SuffixModes)SuffixMode.GetValue();


    // Options not using
    /*public static OptionItem ButtonBarryButtons;
      public static OptionItem GlitchCanVote;
      public static OptionItem JackalWinWithSidekick;
      public static OptionItem FlashWhenTrapBoobyTrap;
      public static OptionItem CrewmateCanBeSidekick;
      public static OptionItem NeutralCanBeSidekick;
      public static OptionItem ImpostorCanBeSidekick;
      public static OptionItem ImpCanBeReflective;
      public static OptionItem CrewCanBeReflective;
      public static OptionItem NeutralCanBeReflective;
      public static OptionItem ImpCanBeRogue;
      public static OptionItem CrewCanBeRogue;
      public static OptionItem NeutralCanBeRogue;
      public static OptionItem RogueKnowEachOther;
      public static OptionItem RogueKnowEachOtherRoles;
      public static OptionItem ControlCooldown;
      public static OptionItem LawyerVision;
      public static OptionItem BardChance;
      public static OptionItem BaitCanBeSold;
      public static OptionItem WatcherCanBeSold;
      public static OptionItem SeerCanBeSold;
      public static OptionItem TrapperCanBeSold;
      public static OptionItem TiebreakerCanBeSold;
      public static OptionItem KnightedCanBeSold;
      public static OptionItem NecroviewCanBeSold;
      public static OptionItem SoullessCanBeSold;
      public static OptionItem SchizoCanBeSold;
      public static OptionItem OnboundCanBeSold;
      public static OptionItem GuesserCanBeSold;
      public static OptionItem UnreportableCanBeSold;
      public static OptionItem LuckyCanBeSold;
      public static OptionItem ObliviousCanBeSold;
      public static OptionItem BewilderCanBeSold; */

    // Override Tasks
    public static OverrideTasksData TerroristTasks;
    public static OverrideTasksData TransporterTasks;
    public static OverrideTasksData WorkaholicTasks;
    public static OverrideTasksData CrewpostorTasks;
    public static OverrideTasksData PhantomTasks;
    public static OverrideTasksData GuardianTasks;
    public static OverrideTasksData OpportunistTasks;
    public static OverrideTasksData MayorTasks;
    public static OverrideTasksData RetributionistTasks;
    public static OverrideTasksData TimeManagerTasks;


    public static int SnitchExposeTaskLeft = 1;

    public static bool IsLoaded = false;

    static Options()
    {
        ResetRoleCounts();
    }
    public static void ResetRoleCounts()
    {
        roleCounts = new Dictionary<CustomRoles, int>();
        roleSpawnChances = new Dictionary<CustomRoles, float>();

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

    public static int GetRoleSpawnMode(CustomRoles role)
    {
        var mode = CustomRoleSpawnChances.TryGetValue(role, out var sc) ? sc.GetChance() : 0;
        return mode switch
        {
            0 => 0,
            1 => 1,
            2 => 2,
            100 => 1,
            _ => 1,
        };
    }
    public static int GetRoleCount(CustomRoles role)
    {
        var mode = GetRoleSpawnMode(role);
        return mode is 0 ? 0 : CustomRoleCounts.TryGetValue(role, out var option) ? option.GetInt() : roleCounts[role];
    }
    public static float GetRoleChance(CustomRoles role)
    {
        return CustomRoleSpawnChances.TryGetValue(role, out var option) ? option.GetValue()/* / 10f */ : roleSpawnChances[role];
    }
    public static void Load()
    {
        //#######################################
        // 27200 lasted id for roles/add-ons (Next use 27300)
        // Limit id for  roles/add-ons --- "59999"
        //#######################################

        // Start Load Settings
        if (IsLoaded) return;
        OptionSaver.Initialize();

        // Preset Option
        _ = PresetOptionItem.Create(0, TabGroup.SystemSettings)
                .SetColor(new Color32(255, 235, 4, byte.MaxValue))
                .SetHeader(true);

        // Game Mode
        GameMode = StringOptionItem.Create(60000, "GameMode", gameModes, 0, TabGroup.GameSettings, false)
            .SetHeader(true);


        #region Roles/Add-ons Settings
        CustomRoleCounts = new();
        CustomRoleSpawnChances = new();
        CustomAdtRoleSpawnRate = new();

        // GM
        //EnableGM = BooleanOptionItem.Create(60001, "GM", false, TabGroup.GameSettings, false)
        //    .SetColor(Utils.GetRoleColor(CustomRoles.GM))
        //    .SetHidden(true)
        //    .SetHeader(true);


        ImpKnowAlliesRole = BooleanOptionItem.Create(60002, "ImpKnowAlliesRole", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true);
        ImpKnowWhosMadmate = BooleanOptionItem.Create(60003, "ImpKnowWhosMadmate", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);
        ImpCanKillMadmate = BooleanOptionItem.Create(60004, "ImpCanKillMadmate", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);

        MadmateKnowWhosMadmate = BooleanOptionItem.Create(60005, "MadmateKnowWhosMadmate", true, TabGroup.ImpostorRoles, false)
            .SetHeader(true)
            .SetGameMode(CustomGameMode.Standard);
        MadmateKnowWhosImp = BooleanOptionItem.Create(60006, "MadmateKnowWhosImp", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);
        MadmateCanKillImp = BooleanOptionItem.Create(60007, "MadmateCanKillImp", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);
        MadmateHasImpostorVision = BooleanOptionItem.Create(60008, "MadmateHasImpostorVision", false, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);
        RefugeeKillCD = FloatOptionItem.Create(60009, "RefugeeKillCD", new(0f, 180f, 2.5f), 22.5f, TabGroup.ImpostorRoles, false)
            .SetHeader(true)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
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


        NeutralRoleWinTogether = BooleanOptionItem.Create(60017, "NeutralRoleWinTogether", false, TabGroup.NeutralRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true);
        NeutralWinTogether = BooleanOptionItem.Create(60018, "NeutralWinTogether", false, TabGroup.NeutralRoles, false)
            .SetParent(NeutralRoleWinTogether)
            .SetGameMode(CustomGameMode.Standard);

        NameDisplayAddons = BooleanOptionItem.Create(60019, "NameDisplayAddons", true, TabGroup.Addons, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true);
        NoLimitAddonsNumMax = IntegerOptionItem.Create(60020, "NoLimitAddonsNumMax", new(1, 15, 1), 1, TabGroup.Addons, false)
            .SetGameMode(CustomGameMode.Standard);
        AddBracketsToAddons = BooleanOptionItem.Create(60021, "BracketAddons", false, TabGroup.Addons, false)
            .SetGameMode(CustomGameMode.Standard);
        #endregion

        #region Impostors Settings
        // Impostor
        TextOptionItem.Create(10000000, "RoleType.VanillaRoles", TabGroup.ImpostorRoles) // Vanilla
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(255, 25, 25, byte.MaxValue));

        SetupRoleOptions(300, TabGroup.ImpostorRoles, CustomRoles.ImpostorTOHE);

        SetupRoleOptions(400, TabGroup.ImpostorRoles, CustomRoles.ShapeshifterTOHE);
        ShapeshiftCD = FloatOptionItem.Create(402, "ShapeshiftCooldown", new(1f, 180f, 1f), 15f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.ShapeshifterTOHE])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftDur = FloatOptionItem.Create(403, "ShapeshiftDuration", new(1f, 180f, 1f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.ShapeshifterTOHE])
            .SetValueFormat(OptionFormat.Seconds);


        TextOptionItem.Create(10000001, "RoleType.ImpKilling", TabGroup.ImpostorRoles) // KILLING
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(255, 25, 25, byte.MaxValue));// KILLING

        /*
         * Arrogance
         */
        Sans.SetupCustomOption();

        /*
         * Berserker
         */
        SetupRoleOptions(600, TabGroup.ImpostorRoles, CustomRoles.Berserker);
        BerserkerKillCooldown = FloatOptionItem.Create(602, "BerserkerKillCooldown", new(25f, 250f, 2.5f), 35f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker])
            .SetValueFormat(OptionFormat.Seconds);
        BerserkerMax = IntegerOptionItem.Create(603, "BerserkerMax", new(1, 10, 1), 4, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker])
            .SetValueFormat(OptionFormat.Level);
        BerserkerOneCanKillCooldown = BooleanOptionItem.Create(604, "BerserkerOneCanKillCooldown", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker]);
        BerserkerOneKillCooldown = FloatOptionItem.Create(605, "BerserkerOneKillCooldown", new(10f, 45f, 2.5f), 15f, TabGroup.ImpostorRoles, false).SetParent(BerserkerOneCanKillCooldown)
            .SetValueFormat(OptionFormat.Seconds);
        BerserkerKillCooldownLevel = IntegerOptionItem.Create(606, "BerserkerLevelRequirement", new(1, 10, 1), 1, TabGroup.ImpostorRoles, false).SetParent(BerserkerOneCanKillCooldown)
            .SetValueFormat(OptionFormat.Level);
        BerserkerTwoCanScavenger = BooleanOptionItem.Create(607, "BerserkerTwoCanScavenger", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker]);
        BerserkerScavengerLevel = IntegerOptionItem.Create(608, "BerserkerLevelRequirement", new(1, 10, 1), 2, TabGroup.ImpostorRoles, false).SetParent(BerserkerTwoCanScavenger)
            .SetValueFormat(OptionFormat.Level);
        BerserkerThreeCanBomber = BooleanOptionItem.Create(609, "BerserkerThreeCanBomber", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker]);
        BerserkerBomberLevel = IntegerOptionItem.Create(610, "BerserkerLevelRequirement", new(1, 10, 1), 3, TabGroup.ImpostorRoles, false).SetParent(BerserkerThreeCanBomber)
            .SetValueFormat(OptionFormat.Level);
        //BerserkerFourCanFlash = BooleanOptionItem.Create(611, "BerserkerFourCanFlash", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker]);
        //BerserkerSpeed = FloatOptionItem.Create(611, "BerserkerSpeed", new(1.5f, 5f, 0.25f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(BerserkerOneCanKillCooldown)
        //    .SetValueFormat(OptionFormat.Multiplier);
        BerserkerFourCanNotKill = BooleanOptionItem.Create(612, "BerserkerFourCanNotKill", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker]);
        BerserkerImmortalLevel = IntegerOptionItem.Create(613, "BerserkerLevelRequirement", new(1, 10, 1), 4, TabGroup.ImpostorRoles, false).SetParent(BerserkerFourCanNotKill)
            .SetValueFormat(OptionFormat.Level);

        /*
         * Bomber
         */
        SetupRoleOptions(700, TabGroup.ImpostorRoles, CustomRoles.Bomber);
        BomberRadius = FloatOptionItem.Create(702, "BomberRadius", new(0.5f, 5f, 0.5f), 2f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bomber])
            .SetValueFormat(OptionFormat.Multiplier);
        BomberCanKill = BooleanOptionItem.Create(703, "CanKill", false, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bomber]);
        BomberKillCD = FloatOptionItem.Create(704, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(BomberCanKill)
            .SetValueFormat(OptionFormat.Seconds);
        BombCooldown = FloatOptionItem.Create(705, "BombCooldown", new(5f, 180f, 2.5f), 60f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bomber])
            .SetValueFormat(OptionFormat.Seconds);
        ImpostorsSurviveBombs = BooleanOptionItem.Create(706, "ImpostorsSurviveBombs", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bomber]);
        BomberDiesInExplosion = BooleanOptionItem.Create(707, "BomberDiesInExplosion", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bomber]);
        NukerChance = IntegerOptionItem.Create(708, "NukerChance", new(0, 100, 5), 0, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bomber])
            .SetValueFormat(OptionFormat.Percent);
        NukeCooldown = FloatOptionItem.Create(709, "NukeCooldown", new(5f, 180f, 2.5f), 60f, TabGroup.ImpostorRoles, false)
            .SetParent(NukerChance)
            .SetValueFormat(OptionFormat.Seconds);
        NukeRadius = FloatOptionItem.Create(710, "NukeRadius", new(1f, 100f, 1f), 25f, TabGroup.ImpostorRoles, false)
            .SetParent(NukerChance)
            .SetValueFormat(OptionFormat.Multiplier);

        /*
         * Bounty Hunter
         */
        BountyHunter.SetupCustomOption();

        /*
         * Butcher
         */
        OverKiller.SetupCustomOption();

        /*
         * Chronomancer
         */
        Chronomancer.SetupCustomOption();

        /*
         * Councillor
         */
        Councillor.SetupCustomOption();

        /*
         * Cursed Wolf (From: TOH_Y)
         */
        SetupRoleOptions(1100, TabGroup.ImpostorRoles, CustomRoles.CursedWolf);
        GuardSpellTimes = IntegerOptionItem.Create(1102, "GuardSpellTimes", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.CursedWolf])
            .SetValueFormat(OptionFormat.Times);
        killAttacker = BooleanOptionItem.Create(1103, "killAttacker", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CursedWolf]);


        /*
         * Deathpact
         */
        Deathpact.SetupCustomOption();

        /*
         * Evil Guesser
         */
        SetupRoleOptions(1300, TabGroup.ImpostorRoles, CustomRoles.EvilGuesser);
        EGCanGuessTime = IntegerOptionItem.Create(1302, "GuesserCanGuessTimes", new(1, 15, 1), 15, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.EvilGuesser])
            .SetValueFormat(OptionFormat.Times);
        EGCanGuessImp = BooleanOptionItem.Create(1303, "EGCanGuessImp", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
        EGCanGuessAdt = BooleanOptionItem.Create(1304, "EGCanGuessAdt", false, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
        EGCanGuessTaskDoneSnitch = BooleanOptionItem.Create(1305, "EGCanGuessTaskDoneSnitch", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
        EGTryHideMsg = BooleanOptionItem.Create(1306, "GuesserTryHideMsg", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.EvilGuesser])
            .SetColor(Color.green);

        /*
         * Evil Tracker
         */
        EvilTracker.SetupCustomOption();

        /*
         * Greedy
         */
        Greedier.SetupCustomOption();

        /*
         * Hangman
         */
        Hangman.SetupCustomOption();

        /*
         * Inhibitor
         */
        SetupRoleOptions(1600, TabGroup.ImpostorRoles, CustomRoles.Inhibitor);
        InhibitorCD = FloatOptionItem.Create(1602, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Inhibitor])
            .SetValueFormat(OptionFormat.Seconds);

        /*
         * Instigator
         */
        Instigator.SetupCustomOption();

        /*
         * Killing Machine
         */
        SetupRoleOptions(23800, TabGroup.ImpostorRoles, CustomRoles.Minimalism);
        MNKillCooldown = FloatOptionItem.Create(23805, "KillCooldown", new(2.5f, 180f, 2.5f), 10f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Minimalism])
            .SetValueFormat(OptionFormat.Seconds);

        /*
         * Ludopath
         */
        SetupRoleOptions(1800, TabGroup.ImpostorRoles, CustomRoles.Ludopath);
        LudopathRandomKillCD = IntegerOptionItem.Create(1802, "LudopathRandomKillCD", new(1, 100, 1), 45, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Ludopath])
            .SetValueFormat(OptionFormat.Seconds);

        /*
         * Lurker
         */
        Lurker.SetupCustomOption();

        // Mare.SetupCustomOption();

        /*
         * Mercenary
         */
        SerialKiller.SetupCustomOption();

        /*
         * Ninja
         */
        Assassin.SetupCustomOption();

        /*
         * Quick Shooter
         */
        QuickShooter.SetupCustomOption();

        /*
         * Saboteur
         */
        SetupRoleOptions(2300, TabGroup.ImpostorRoles, CustomRoles.Saboteur);
        SaboteurCD = FloatOptionItem.Create(2302, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Saboteur])
            .SetValueFormat(OptionFormat.Seconds);

        /*
         * Sniper
         */
        Sniper.SetupCustomOption();

        /*
         * Spellcaster
         */
        Witch.SetupCustomOption();

        /*
         * Trapster
         */
        SetupRoleOptions(2600, TabGroup.ImpostorRoles, CustomRoles.BoobyTrap);
        BTKillCooldown = FloatOptionItem.Create(2602, "KillCooldown", new(2.5f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.BoobyTrap])
            .SetValueFormat(OptionFormat.Seconds);
        TrapConsecutiveBodies = BooleanOptionItem.Create(2603, "TrapConsecutiveBodies", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.BoobyTrap]);
        TrapTrapsterBody = BooleanOptionItem.Create(2604, "TrapTrapsterBody", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.BoobyTrap]);
        TrapConsecutiveTrapsterBodies = BooleanOptionItem.Create(2605, "TrapConsecutiveBodies", true, TabGroup.ImpostorRoles, false)
            .SetParent(TrapTrapsterBody);

        /*
         * Underdog
         */
        SetupRoleOptions(2700, TabGroup.ImpostorRoles, CustomRoles.Underdog);
        UnderdogMaximumPlayersNeededToKill = IntegerOptionItem.Create(2702, "UnderdogMaximumPlayersNeededToKill", new(1, 15, 1), 5, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Underdog])
            .SetValueFormat(OptionFormat.Players);
        UnderdogKillCooldown = FloatOptionItem.Create(2703, "KillCooldown", new(0f, 180f, 2.5f), 12.5f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Underdog])
            .SetValueFormat(OptionFormat.Seconds);

        /*
         * Zombie
         */
        SetupRoleOptions(23900, TabGroup.ImpostorRoles, CustomRoles.Zombie);
        ZombieKillCooldown = FloatOptionItem.Create(23903, "KillCooldown", new(0f, 180f, 2.5f), 5f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Zombie])
            .SetValueFormat(OptionFormat.Seconds);
        ZombieSpeedReduce = FloatOptionItem.Create(23904, "ZombieSpeedReduce", new(0.0f, 1.0f, 0.1f), 0.1f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Zombie])
            .SetValueFormat(OptionFormat.Multiplier);

        /*
         * SUPPORT ROLES
         */
        TextOptionItem.Create(10000002, "RoleType.ImpSupport", TabGroup.ImpostorRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 25, 25, byte.MaxValue));

        /*
         * Anti Adminer
         */
        AntiAdminer.SetupCustomOption();

        /*
         * Blackmailer
        */
        Blackmailer.SetupCustomOption();

        /*
         * Camouflager
         */
        Camouflager.SetupCustomOption();

        /*
         * Cleaner
         */
        SetupRoleOptions(3000, TabGroup.ImpostorRoles, CustomRoles.Cleaner);
        CleanerKillCooldown = FloatOptionItem.Create(3002, "KillCooldown", new(5f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Cleaner])
            .SetValueFormat(OptionFormat.Seconds);
        KillCooldownAfterCleaning = FloatOptionItem.Create(3003, "KillCooldownAfterCleaning", new(5f, 180f, 2.5f), 60f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Cleaner])
            .SetValueFormat(OptionFormat.Seconds);

        /* 
         * Consigliere
         */
        EvilDiviner.SetupCustomOption();

        /*
         * Fireworker
         */
        FireWorks.SetupCustomOption();

        /*
         * Gangster
         */
        Gangster.SetupCustomOption();

        /*
         * Godfather
         */
        SetupRoleOptions(3400, TabGroup.ImpostorRoles, CustomRoles.Godfather);
        GodfatherChangeOpt = StringOptionItem.Create(3402, "GodfatherTargetCountMode", GodfatherChangeMode, 0, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Godfather]);

        /*
         * Kamikaze
         */
        Kamikaze.SetupCustomOption();

        /*
         * Morphling
         */
        Morphling.SetupCustomOption();

        /*
         * Nemesis
         */
        SetupRoleOptions(3600, TabGroup.ImpostorRoles, CustomRoles.Mafia);
        MafiaCanKillNum = IntegerOptionItem.Create(3602, "MafiaCanKillNum", new(0, 15, 1), 1, TabGroup.ImpostorRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Mafia])
            .SetValueFormat(OptionFormat.Players);
        LegacyMafia = BooleanOptionItem.Create(3603, "LegacyMafia", false, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mafia]);
        MafiaShapeshiftCD = FloatOptionItem.Create(3604, "ShapeshiftCooldown", new(1f, 180f, 1f), 15f, TabGroup.ImpostorRoles, false)
            .SetParent(LegacyMafia)
            .SetValueFormat(OptionFormat.Seconds);
        MafiaShapeshiftDur = FloatOptionItem.Create(3605, "ShapeshiftDuration", new(1f, 180f, 1f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(LegacyMafia)
            .SetValueFormat(OptionFormat.Seconds);

        /*
         * Time Thief
         */
        TimeThief.SetupCustomOption();

        /*
         * Vindicator
         */
        SetupRoleOptions(3800, TabGroup.ImpostorRoles, CustomRoles.Vindicator);
        VindicatorAdditionalVote = IntegerOptionItem.Create(3802, "MayorAdditionalVote", new(1, 20, 1), 3, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Vindicator])
            .SetValueFormat(OptionFormat.Votes);
        VindicatorHideVote = BooleanOptionItem.Create(3803, "MayorHideVote", false, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Vindicator]);

        /*
         * Visionary
         */
        SetupRoleOptions(3900, TabGroup.ImpostorRoles, CustomRoles.Visionary);

        /*
         * CONCEALING ROLES
         */
        TextOptionItem.Create(10000003, "RoleType.ImpConcealing", TabGroup.ImpostorRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 25, 25, byte.MaxValue));

        /*
         * Escapist
         */
        SetupRoleOptions(4000, TabGroup.ImpostorRoles, CustomRoles.Escapee);
        EscapeeSSDuration = FloatOptionItem.Create(4002, "ShapeshiftDuration", new(1f, 180f, 1f), 1, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Escapee])
            .SetValueFormat(OptionFormat.Seconds);
        EscapeeSSCD = FloatOptionItem.Create(4003, "ShapeshiftCooldown", new(1f, 180f, 1f), 5f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Escapee])
            .SetValueFormat(OptionFormat.Seconds);

        /*
         * Lightning
         */
        BallLightning.SetupCustomOption();

        /*
         * Mastermind
         */
        Mastermind.SetupCustomOption();

        /*
         * Miner
         */
        SetupRoleOptions(4200, TabGroup.ImpostorRoles, CustomRoles.Miner);
        MinerSSDuration = FloatOptionItem.Create(4202, "ShapeshiftDuration", new(1f, 180f, 1f), 1, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Miner])
            .SetValueFormat(OptionFormat.Seconds);
        MinerSSCD = FloatOptionItem.Create(4203, "ShapeshiftCooldown", new(1f, 180f, 1f), 15f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Miner])
            .SetValueFormat(OptionFormat.Seconds);

        /*
         * Puppeteer
         */
        Puppeteer.SetupCustomOption();

        /*
         * Rift Maker
         */
        RiftMaker.SetupCustomOption();

        /*
         * Scavenger
         */
        SetupRoleOptions(4400, TabGroup.ImpostorRoles, CustomRoles.Scavenger);
        ScavengerKillCooldown = FloatOptionItem.Create(4402, "KillCooldown", new(5f, 180f, 2.5f), 40f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Scavenger])
            .SetValueFormat(OptionFormat.Seconds);

        /*
         * Shapemaster
         */
        SetupRoleOptions(4500, TabGroup.ImpostorRoles, CustomRoles.ShapeMaster);
        ShapeMasterShapeshiftDuration = FloatOptionItem.Create(4502, "ShapeshiftDuration", new(1, 60, 1), 10, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.ShapeMaster])
            .SetValueFormat(OptionFormat.Seconds);

        /*
         * Soul Catcher
         */
        SetupRoleOptions(4600, TabGroup.ImpostorRoles, CustomRoles.ImperiusCurse);
        ShapeImperiusCurseShapeshiftDuration = FloatOptionItem.Create(4602, "ShapeshiftDuration", new(2.5f, 180f, 2.5f), 300, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.ImperiusCurse])
            .SetValueFormat(OptionFormat.Seconds);
        ImperiusCurseShapeshiftCooldown = FloatOptionItem.Create(4603, "ShapeshiftCooldown", new(1f, 180f, 1f), 15f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.ImperiusCurse])
            .SetValueFormat(OptionFormat.Seconds);

        /*
         * Swooper
         */
        Swooper.SetupCustomOption();

        /*
         * Trickster
         */
        SetupRoleOptions(4800, TabGroup.ImpostorRoles, CustomRoles.Trickster);

        /*
         * Undertaker
         */
        Undertaker.SetupCustomOption();

        /*
         * Vampire
         */
        Vampire.SetupCustomOption();

        /*
         * Warlock
         */
        SetupRoleOptions(5100, TabGroup.ImpostorRoles, CustomRoles.Warlock);
        WarlockCanKillAllies = BooleanOptionItem.Create(5102, "CanKillAllies", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Warlock]);
        WarlockCanKillSelf = BooleanOptionItem.Create(5103, "CanKillSelf", false, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Warlock]);
        WarlockShiftDuration = FloatOptionItem.Create(5104, "ShapeshiftDuration", new(1, 180, 1), 1, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Warlock])
            .SetValueFormat(OptionFormat.Seconds);

        /*
         * Wildling
         */
        Wildling.SetupCustomOption();

        /*
         * HINDERING ROLES
         */
        TextOptionItem.Create(10000004, "RoleType.ImpHindering", TabGroup.ImpostorRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 25, 25, byte.MaxValue));

        /*
         * Anonymous
         */
        Hacker.SetupCustomOption();

        /*
         * Dazzler
         */
        Dazzler.SetupCustomOption();

        /*
         * Devourer
         */
        Devourer.SetupCustomOption();

        /*
         * Eraser
         */
        Eraser.SetupCustomOption();

        /*
         * Pitfall
         */
        Pitfall.SetupCustomOption();

        /*
         * Twister
         */
        Twister.SetupCustomOption();

        /*
         * MADMATE ROLES
         */
        TextOptionItem.Create(10000005, "RoleType.Madmate", TabGroup.ImpostorRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 25, 25, byte.MaxValue));

        /*
         * Crewpostor
         */
        SetupRoleOptions(5800, TabGroup.ImpostorRoles, CustomRoles.Crewpostor);
        CrewpostorCanKillAllies = BooleanOptionItem.Create(5802, "CanKillAllies", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        CrewpostorKnowsAllies = BooleanOptionItem.Create(5803, "CrewpostorKnowsAllies", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        AlliesKnowCrewpostor = BooleanOptionItem.Create(5804, "AlliesKnowCrewpostor", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        CrewpostorLungeKill = BooleanOptionItem.Create(5805, "CrewpostorLungeKill", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        CrewpostorKillAfterTask = IntegerOptionItem.Create(5806, "CrewpostorKillAfterTask", new(1, 50, 1), 1, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        CrewpostorTasks = OverrideTasksData.Create(5807, TabGroup.ImpostorRoles, CustomRoles.Crewpostor);

        /*
         * Parasite
         */
        SetupSingleRoleOptions(5900, TabGroup.ImpostorRoles, CustomRoles.Parasite, 1, zeroOne: false);
        ParasiteCD = FloatOptionItem.Create(5902, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Parasite])
            .SetValueFormat(OptionFormat.Seconds);

        #endregion

        #region Crewmates Settings
        /*
         * VANILLA ROLES
         */
        TextOptionItem.Create(10000006, "RoleType.VanillaRoles", TabGroup.CrewmateRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(140, 255, 255, byte.MaxValue));
        
        /*
         * Crewmate
         */
        SetupRoleOptions(6000, TabGroup.CrewmateRoles, CustomRoles.CrewmateTOHE);

        /*
         * Engineer
         */
        SetupRoleOptions(6100, TabGroup.CrewmateRoles, CustomRoles.EngineerTOHE);

        /*
         * Scientist
         */
        SetupRoleOptions(6200, TabGroup.CrewmateRoles, CustomRoles.ScientistTOHE);
        ScientistCD = FloatOptionItem.Create(6202, "VitalsCooldown", new(1f, 250f, 1f), 3f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.ScientistTOHE])
            .SetValueFormat(OptionFormat.Seconds);
        ScientistDur = FloatOptionItem.Create(6203, "VitalsDuration", new(1f, 250f, 1f), 15f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.ScientistTOHE])
            .SetValueFormat(OptionFormat.Seconds);

        /*
         * BASIC ROLES
         */
        TextOptionItem.Create(10000007, "RoleType.CrewBasic", TabGroup.CrewmateRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(140, 255, 255, byte.MaxValue));

        /*
         * Addict
         */
        Addict.SetupCustomOption();

        /*
         * Alchemist
         */
        Alchemist.SetupCustomOption();

        /*
         * Celebrity
         */
        SetupRoleOptions(6500, TabGroup.CrewmateRoles, CustomRoles.CyberStar);
        ImpKnowCyberStarDead = BooleanOptionItem.Create(6502, "ImpKnowCyberStarDead", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.CyberStar]);
        NeutralKnowCyberStarDead = BooleanOptionItem.Create(6503, "NeutralKnowCyberStarDead", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.CyberStar]);

        /*
         * Cleanser
         */
        Cleanser.SetupCustomOption();

        /*
         * Doctor
         */
        SetupRoleOptions(6700, TabGroup.CrewmateRoles, CustomRoles.Doctor);
        DoctorTaskCompletedBatteryCharge = FloatOptionItem.Create(6702, "DoctorTaskCompletedBatteryCharge", new(0f, 250f, 1f), 50f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Doctor])
            .SetValueFormat(OptionFormat.Seconds);
        DoctorVisibleToEveryone = BooleanOptionItem.Create(6703, "DoctorVisibleToEveryone", false, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Doctor]);

        /*
         * Guess Master
         */
        GuessMaster.SetupCustomOption();

        /*
         * Lazy Guy
         */
        SetupRoleOptions(6800, TabGroup.CrewmateRoles, CustomRoles.Needy);

        /*
         * Luckey
         */
        //SetupRoleOptions(6900, TabGroup.CrewmateRoles, CustomRoles.Luckey);
        //LuckeyProbability = IntegerOptionItem.Create(6902, "LuckeyProbability", new(0, 100, 5), 50, TabGroup.CrewmateRoles, false)
        //    .SetParent(CustomRoleSpawnChances[CustomRoles.Luckey])
        //    .SetValueFormat(OptionFormat.Percent);

        /*
         * Mini
         */
        Mini.SetupCustomOption();

        /*
         * Mole
         */
        Mole.SetupCustomOption();

        /*
         * Superstar
         */
        SetupRoleOptions(7150, TabGroup.CrewmateRoles, CustomRoles.SuperStar);
        EveryOneKnowSuperStar = BooleanOptionItem.Create(7152, "EveryOneKnowSuperStar", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.SuperStar]);

        /*
         * Task Manager
         */
        SetupRoleOptions(7200, TabGroup.CrewmateRoles, CustomRoles.TaskManager);

        /*
         * Tracefinder
         */
        Tracefinder.SetupCustomOption();

        /*
         * Transporter
         */
        SetupRoleOptions(7400, TabGroup.CrewmateRoles, CustomRoles.Transporter);
        TransporterTeleportMax = IntegerOptionItem.Create(7402, "TransporterTeleportMax", new(1, 100, 1), 5, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Transporter])
            .SetValueFormat(OptionFormat.Times);
        TransporterTasks = OverrideTasksData.Create(7403, TabGroup.CrewmateRoles, CustomRoles.Transporter);

        /*
         * Randomizer
         */
        SetupRoleOptions(7500, TabGroup.CrewmateRoles, CustomRoles.Randomizer);
        BecomeBaitDelayNotify = BooleanOptionItem.Create(7502, "BecomeBaitDelayNotify", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Randomizer]);
        BecomeBaitDelayMin = FloatOptionItem.Create(7503, "BaitDelayMin", new(0f, 5f, 1f), 0f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Randomizer])
            .SetValueFormat(OptionFormat.Seconds);
        BecomeBaitDelayMax = FloatOptionItem.Create(7504, "BaitDelayMax", new(0f, 10f, 1f), 0f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Randomizer])
            .SetValueFormat(OptionFormat.Seconds);
        BecomeTrapperBlockMoveTime = FloatOptionItem.Create(7505, "BecomeTrapperBlockMoveTime", new(1f, 180f, 1f), 5f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Randomizer])
            .SetValueFormat(OptionFormat.Seconds);
        
        /*
         * SUPPORT ROLES
         */
        TextOptionItem.Create(10000008, "RoleType.CrewSupport", TabGroup.CrewmateRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(140, 255, 255, byte.MaxValue));

        /*
         * Benefactor 
         */
        Benefactor.SetupCustomOption();

        /*
         * Chameleon
         */
        Chameleon.SetupCustomOption();

        /*
         * Coroner
         */
        Bloodhound.SetupCustomOption();

        /*
         * Deputy
         */
        Deputy.SetupCustomOption();

        /*
         * Detective
         */
        SetupRoleOptions(7900, TabGroup.CrewmateRoles, CustomRoles.Detective);
        DetectiveCanknowKiller = BooleanOptionItem.Create(7902, "DetectiveCanknowKiller", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Detective]);

        /*
         * Fortune Teller
         */
        Divinator.SetupCustomOption();

        /*
         * Enigma
         */
        Enigma.SetupCustomOption();

        /*
         * Grenadier
         */
        SetupRoleOptions(8200, TabGroup.CrewmateRoles, CustomRoles.Grenadier);
        GrenadierSkillCooldown = FloatOptionItem.Create(8202, "GrenadierSkillCooldown", new(1f, 180f, 1f), 25f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
            .SetValueFormat(OptionFormat.Seconds);
        GrenadierSkillDuration = FloatOptionItem.Create(8203, "GrenadierSkillDuration", new(1f, 60f, 1f), 10f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
            .SetValueFormat(OptionFormat.Seconds);
        GrenadierCauseVision = FloatOptionItem.Create(8204, "GrenadierCauseVision", new(0f, 5f, 0.05f), 0.3f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
            .SetValueFormat(OptionFormat.Multiplier);
        GrenadierCanAffectNeutral = BooleanOptionItem.Create(8205, "GrenadierCanAffectNeutral", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier]);
        GrenadierSkillMaxOfUseage = IntegerOptionItem.Create(8206, "GrenadierSkillMaxOfUseage", new(0, 20, 1), 2, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
            .SetValueFormat(OptionFormat.Times);
        GrenadierAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(8207, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
            .SetValueFormat(OptionFormat.Times);

        /*
         * ParityCop
         */
        ParityCop.SetupCustomOption();

        /*
         *  Keeper
         */
        Keeper.SetupCustomOption();

        /*
         * Lighter
         */
        SetupSingleRoleOptions(8400, TabGroup.CrewmateRoles, CustomRoles.Lighter, 1);
        LighterSkillCooldown = FloatOptionItem.Create(8402, "LighterSkillCooldown", new(1f, 180f, 1f), 25f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Seconds);
        LighterSkillDuration = FloatOptionItem.Create(8403, "LighterSkillDuration", new(1f, 180f, 1f), 10f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Seconds);
        LighterVisionNormal = FloatOptionItem.Create(8404, "LighterVisionNormal", new(0f, 5f, 0.05f), 1.35f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Multiplier);
        LighterVisionOnLightsOut = FloatOptionItem.Create(8405, "LighterVisionOnLightsOut", new(0f, 5f, 0.05f), 0.5f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Multiplier);
        LighterSkillMaxOfUseage = IntegerOptionItem.Create(8406, "AbilityUseLimit", new(0, 180, 1), 4, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Times);
        LighterAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(8407, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Times);

        /*
         * Mechanic
         */
        SabotageMaster.SetupCustomOption();

        /*
         * Medic
         */
        Medic.SetupCustomOption();

        /*
         * Medium
         */
        Mediumshiper.SetupCustomOption();

        /*
         * Merchant
         */
        Merchant.SetupCustomOption();

        /*
         * Mortician
         */
        Mortician.SetupCustomOption();

        /*
         * Observer
         */
        SetupRoleOptions(9000, TabGroup.CrewmateRoles, CustomRoles.Observer);

        /*
         * Oracle
         */
        Oracle.SetupCustomOption();

        /*
         * DovesOfNeace
         */
        SetupRoleOptions(9200, TabGroup.CrewmateRoles, CustomRoles.DovesOfNeace);
        DovesOfNeaceCooldown = FloatOptionItem.Create(9202, "DovesOfNeaceCooldown", new(1f, 180f, 1f), 30f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DovesOfNeace])
            .SetValueFormat(OptionFormat.Seconds);
        DovesOfNeaceMaxOfUseage = IntegerOptionItem.Create(9203, "DovesOfNeaceMaxOfUseage", new(0, 20, 1), 3, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DovesOfNeace])
            .SetValueFormat(OptionFormat.Times);
        DovesOfNeaceAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(9204, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DovesOfNeace])
            .SetValueFormat(OptionFormat.Times);

        /*SetupRoleOptions(9300, TabGroup.CrewmateRoles, CustomRoles.Paranoia);
        ParanoiaNumOfUseButton = IntegerOptionItem.Create(9302, "ParanoiaNumOfUseButton", new(1, 20, 1), 3, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Paranoia])
            .SetValueFormat(OptionFormat.Times);
        ParanoiaVentCooldown = FloatOptionItem.Create(9303, "ParanoiaVentCooldown", new(0, 180, 1), 10, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Paranoia])
            .SetValueFormat(OptionFormat.Seconds); */

        /*
         * Psychic
         */
        Psychic.SetupCustomOption();

        /*
         * Snitch
         */
        Snitch.SetupCustomOption();

        /*
         * Spiritualist
         */
        Spiritualist.SetupCustomOption();

        /*
         * Spy
         */
        Spy.SetupCustomOption();

        /*
         * Time Manager
         */
        TimeManager.SetupCustomOption();

        /*
         * Time Master
         */
        SetupRoleOptions(9900, TabGroup.CrewmateRoles, CustomRoles.TimeMaster);
        TimeMasterSkillCooldown = FloatOptionItem.Create(9902, "TimeMasterSkillCooldown", new(1f, 180f, 1f), 20f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.TimeMaster])
            .SetValueFormat(OptionFormat.Seconds);
        TimeMasterSkillDuration = FloatOptionItem.Create(9903, "TimeMasterSkillDuration", new(1f, 180f, 1f), 20f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.TimeMaster])
            .SetValueFormat(OptionFormat.Seconds);
        TimeMasterMaxUses = IntegerOptionItem.Create(9904, "TimeMasterMaxUses", new(0, 20, 1), 1, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.TimeMaster])
            .SetValueFormat(OptionFormat.Times);
        TimeMasterAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(9905, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.TimeMaster])
            .SetValueFormat(OptionFormat.Times);

        /*
         * Tracker
         */
        Tracker.SetupCustomOption();

        /*
         * Witness
         */
        SetupSingleRoleOptions(10100, TabGroup.CrewmateRoles, CustomRoles.Witness, 1);
        WitnessCD = FloatOptionItem.Create(10102, "AbilityCD", new(0f, 60f, 2.5f), 15f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Witness])
            .SetValueFormat(OptionFormat.Seconds);
        WitnessTime = IntegerOptionItem.Create(10103, "WitnessTime", new(1, 30, 1), 10, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Witness])
            .SetValueFormat(OptionFormat.Seconds);


        TextOptionItem.Create(10000009, "RoleType.CrewKilling", TabGroup.CrewmateRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(140, 255, 255, byte.MaxValue));

        SetupSingleRoleOptions(10200, TabGroup.CrewmateRoles, CustomRoles.Bastion, 1);
        BombsClearAfterMeeting = BooleanOptionItem.Create(10202, "BombsClearAfterMeeting", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bastion]);
        BastionBombCooldown = FloatOptionItem.Create(10203, "BombCooldown", new(0, 180, 1), 15, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bastion])
            .SetValueFormat(OptionFormat.Seconds);
        BastionAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(10204, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bastion])
            .SetValueFormat(OptionFormat.Times);
        BastionMaxBombs = IntegerOptionItem.Create(10205, "BastionMaxBombs", new(1, 20, 1), 5, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bastion]);
        
        SetupRoleOptions(10300, TabGroup.CrewmateRoles, CustomRoles.Bodyguard);
        BodyguardProtectRadius = FloatOptionItem.Create(10302, "BodyguardProtectRadius", new(0.5f, 5f, 0.5f), 1.5f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bodyguard])
            .SetValueFormat(OptionFormat.Multiplier);
        
        Crusader.SetupCustomOption();
        
        Counterfeiter.SetupCustomOption();
        
        Jailer.SetupCustomOption();
        
        Judge.SetupCustomOption();
        
        SwordsMan.SetupCustomOption();
        
        SetupRoleOptions(10900, TabGroup.CrewmateRoles, CustomRoles.NiceGuesser);
        GGCanGuessTime = IntegerOptionItem.Create(10902, "GuesserCanGuessTimes", new(1, 15, 1), 15, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser])
            .SetValueFormat(OptionFormat.Times);
        GGCanGuessCrew = BooleanOptionItem.Create(10903, "GGCanGuessCrew", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser]);
        GGCanGuessAdt = BooleanOptionItem.Create(10904, "GGCanGuessAdt", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser]);
        GGTryHideMsg = BooleanOptionItem.Create(10905, "GuesserTryHideMsg", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser])
            .SetColor(Color.green);
        
        SetupRoleOptions(11000, TabGroup.CrewmateRoles, CustomRoles.Retributionist);
        RetributionistCanKillNum = IntegerOptionItem.Create(11002, "RetributionistCanKillNum", new(1, 15, 1), 1, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Retributionist])
            .SetValueFormat(OptionFormat.Players);
        MinimumPlayersAliveToRetri = IntegerOptionItem.Create(11003, "MinimumPlayersAliveToRetri", new(0, 15, 1), 5, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Retributionist])
            .SetValueFormat(OptionFormat.Players);
        CanOnlyRetributeWithTasksDone = BooleanOptionItem.Create(11004, "CanOnlyRetributeWithTasksDone", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Retributionist]);
        RetributionistTasks = OverrideTasksData.Create(11005, TabGroup.CrewmateRoles, CustomRoles.Retributionist);
        
        Reverie.SetupCustomOption();
        
        Sheriff.SetupCustomOption();
        
        SetupRoleOptions(11350, TabGroup.CrewmateRoles, CustomRoles.Veteran);
        VeteranSkillCooldown = FloatOptionItem.Create(11358, "VeteranSkillCooldown", new(1f, 180f, 1f), 20f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Veteran])
            .SetValueFormat(OptionFormat.Seconds);
        VeteranSkillDuration = FloatOptionItem.Create(11359, "VeteranSkillDuration", new(1f, 180f, 1f), 20f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Veteran])
            .SetValueFormat(OptionFormat.Seconds);
        VeteranSkillMaxOfUseage = IntegerOptionItem.Create(11360, "VeteranSkillMaxOfUseage", new(0, 20, 1), 10, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Veteran])
            .SetValueFormat(OptionFormat.Times);
        VeteranAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(11361, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Veteran])
            .SetValueFormat(OptionFormat.Times);

        SetupRoleOptions(11400, TabGroup.CrewmateRoles, CustomRoles.Vigilante);
        VigilanteKillCooldown = FloatOptionItem.Create(11402, "KillCooldown", new(5f, 180f, 2.5f), 30f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Vigilante])
            .SetValueFormat(OptionFormat.Seconds);

        TextOptionItem.Create(10000010, "RoleType.CrewPower", TabGroup.CrewmateRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(140, 255, 255, byte.MaxValue));

        Admirer.SetupCustomOption();

        Captain.SetupCustomOption();

        CopyCat.SetupCustomOption();

        SetupRoleOptions(11600, TabGroup.CrewmateRoles, CustomRoles.Dictator);

        SetupRoleOptions(11700, TabGroup.CrewmateRoles, CustomRoles.Guardian);
        GuardianTasks = OverrideTasksData.Create(11702, TabGroup.CrewmateRoles, CustomRoles.Guardian);

        SetupRoleOptions(11800, TabGroup.CrewmateRoles, CustomRoles.Lookout);

        Marshall.SetupCustomOption();

        SetupRoleOptions(12000, TabGroup.CrewmateRoles, CustomRoles.Mayor);
        MayorAdditionalVote = IntegerOptionItem.Create(12002, "MayorAdditionalVote", new(1, 20, 1), 3, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mayor])
            .SetValueFormat(OptionFormat.Votes);
        MayorHasPortableButton = BooleanOptionItem.Create(12003, "MayorHasPortableButton", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mayor]);
        MayorNumOfUseButton = IntegerOptionItem.Create(12004, "MayorNumOfUseButton", new(1, 20, 1), 1, TabGroup.CrewmateRoles, false)
            .SetParent(MayorHasPortableButton)
            .SetValueFormat(OptionFormat.Times);
        MayorHideVote = BooleanOptionItem.Create(12005, "MayorHideVote", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mayor]);
        MayorRevealWhenDoneTasks = BooleanOptionItem.Create(12006, "MayorRevealWhenDoneTasks", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mayor]);
        MayorTasks = OverrideTasksData.Create(12007, TabGroup.CrewmateRoles, CustomRoles.Mayor);
        
        Monarch.SetupCustomOption();
        
        Farseer.SetupCustomOption();
        
        President.SetupCustomOption();
        
        Swapper.SetupCustomOption();
        
        Monitor.SetupCustomOption();

        //ChiefOfPolice.SetupCustomOption();

        #endregion

        #region Neutrals Settings
        // Neutral
        TextOptionItem.Create(10000011, "RoleType.NeutralBenign", TabGroup.NeutralRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(127, 140, 141, byte.MaxValue));

        Amnesiac.SetupCustomOption();

        Totocalcio.SetupCustomOption();

        FFF.SetupCustomOption();

        Imitator.SetupCustomOption();

        Lawyer.SetupCustomOption();

        Maverick.SetupCustomOption();

        SetupRoleOptions(13300, TabGroup.NeutralRoles, CustomRoles.Opportunist);
        OppoImmuneToAttacksWhenTasksDone = BooleanOptionItem.Create(13302, "ImmuneToAttacksWhenTasksDone", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Opportunist]);
        OpportunistTasks = OverrideTasksData.Create(13303, TabGroup.NeutralRoles, CustomRoles.Opportunist);
        Pixie.SetupCustomOption();
        Pursuer.SetupCustomOption();

        Romantic.SetupCustomOption();

        SchrodingersCat.SetupCustomOption();

        SetupRoleOptions(13600, TabGroup.NeutralRoles, CustomRoles.Shaman);
        VoodooCooldown = FloatOptionItem.Create(13602, "VoodooCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Shaman])
            .SetValueFormat(OptionFormat.Seconds);

        Taskinator.SetupCustomOption();

        NWitch.SetupCustomOption();


        TextOptionItem.Create(10000012, "RoleType.NeutralEvil", TabGroup.NeutralRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(127, 140, 141, byte.MaxValue));

        CursedSoul.SetupCustomOption();

        Doomsayer.SetupCustomOption();

        Executioner.SetupCustomOption();

        SetupRoleOptions(14300, TabGroup.NeutralRoles, CustomRoles.Innocent);
        InnocentCanWinByImp = BooleanOptionItem.Create(14302, "InnocentCanWinByImp", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Innocent]);

        SetupRoleOptions(14400, TabGroup.NeutralRoles, CustomRoles.Jester);
        JesterCanUseButton = BooleanOptionItem.Create(14402, "JesterCanUseButton", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester]);
        JesterCanVent = BooleanOptionItem.Create(14403, "CanVent", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester]);
        JesterHasImpostorVision = BooleanOptionItem.Create(14404, "ImpostorVision", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester]);
        HideJesterVote = BooleanOptionItem.Create(14405, "HideJesterVote", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester]);
        MeetingsNeededForJesterWin = IntegerOptionItem.Create(14406, "MeetingsNeededForWin", new(0, 10, 1), 2, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester])
            .SetValueFormat(OptionFormat.Times);
        SunnyboyChance = IntegerOptionItem.Create(14407, "SunnyboyChance", new(0, 100, 5), 0, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester])
            .SetValueFormat(OptionFormat.Percent);

        SetupRoleOptions(14500, TabGroup.NeutralRoles, CustomRoles.Masochist);
        MasochistKillMax = IntegerOptionItem.Create(14502, "MasochistKillMax", new(1, 30, 1), 5, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Masochist])
            .SetValueFormat(OptionFormat.Times);
        
        Seeker.SetupCustomOption();


        TextOptionItem.Create(10000013, "RoleType.NeutralChaos", TabGroup.NeutralRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(127, 140, 141, byte.MaxValue));
        
        Collector.SetupCustomOption();
        
        Succubus.SetupCustomOption();
        
        SetupRoleOptions(14900, TabGroup.NeutralRoles, CustomRoles.Phantom);
        PhantomCanVent = BooleanOptionItem.Create(14902, "CanVent", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Phantom]);
        PhantomSnatchesWin = BooleanOptionItem.Create(14903, "SnatchesWin", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Phantom]);
        PhantomCanGuess = BooleanOptionItem.Create(14904, "CanGuess", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Phantom]);
        PhantomTasks = OverrideTasksData.Create(14905, TabGroup.NeutralRoles, CustomRoles.Phantom);
        
        Pirate.SetupCustomOption();
        
        SetupRoleOptions(15100, TabGroup.NeutralRoles, CustomRoles.Provocateur);
        ProvKillCD = FloatOptionItem.Create(15102, "KillCooldown", new(0f, 100f, 2.5f), 15f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Provocateur])
            .SetValueFormat(OptionFormat.Seconds);
        
        SetupRoleOptions(15200, TabGroup.NeutralRoles, CustomRoles.Revolutionist);
        RevolutionistDrawTime = FloatOptionItem.Create(15202, "RevolutionistDrawTime", new(0f, 10f, 1f), 3f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Revolutionist])
            .SetValueFormat(OptionFormat.Seconds);
        RevolutionistCooldown = FloatOptionItem.Create(15203, "RevolutionistCooldown", new(5f, 100f, 1f), 10f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Revolutionist])
            .SetValueFormat(OptionFormat.Seconds);
        RevolutionistDrawCount = IntegerOptionItem.Create(15204, "RevolutionistDrawCount", new(1, 14, 1), 6, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Revolutionist])
            .SetValueFormat(OptionFormat.Players);
        RevolutionistKillProbability = IntegerOptionItem.Create(15205, "RevolutionistKillProbability", new(0, 100, 5), 15, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Revolutionist])
            .SetValueFormat(OptionFormat.Percent);
        RevolutionistVentCountDown = FloatOptionItem.Create(15206, "RevolutionistVentCountDown", new(1f, 180f, 1f), 15f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Revolutionist])
            .SetValueFormat(OptionFormat.Seconds);

        /*
         * Solsticer
        */
        Solsticer.SetupCustomOption();

        SoulCollector.SetupCustomOption();
        
        SetupRoleOptions(15400, TabGroup.NeutralRoles, CustomRoles.Terrorist);
        CanTerroristSuicideWin = BooleanOptionItem.Create(15402, "CanTerroristSuicideWin", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Terrorist]);
        TerroristCanGuess = BooleanOptionItem.Create(15403, "CanGuess", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Terrorist]);
        TerroristTasks = OverrideTasksData.Create(15404, TabGroup.NeutralRoles, CustomRoles.Terrorist);
        
        SetupRoleOptions(15500, TabGroup.NeutralRoles, CustomRoles.Mario);
        MarioVentNumWin = IntegerOptionItem.Create(15502, "MarioVentNumWin", new(5, 500, 5), 40, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mario])
            .SetValueFormat(OptionFormat.Times);
        MarioVentCD = FloatOptionItem.Create(15503, "VentCooldown", new(0f, 180f, 1f), 15f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mario])
            .SetValueFormat(OptionFormat.Seconds);
        
        Vulture.SetupCustomOption();
        
        SetupRoleOptions(15700, TabGroup.NeutralRoles, CustomRoles.Workaholic); //TOH_Y
        WorkaholicCannotWinAtDeath = BooleanOptionItem.Create(15702, "WorkaholicCannotWinAtDeath", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Workaholic]);
        WorkaholicVentCooldown = FloatOptionItem.Create(15703, "VentCooldown", new(0f, 180f, 2.5f), 0f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Workaholic])
            .SetValueFormat(OptionFormat.Seconds);
        WorkaholicVisibleToEveryone = BooleanOptionItem.Create(15704, "WorkaholicVisibleToEveryone", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Workaholic]);
        WorkaholicGiveAdviceAlive = BooleanOptionItem.Create(15705, "WorkaholicGiveAdviceAlive", true, TabGroup.NeutralRoles, false)
            .SetParent(WorkaholicVisibleToEveryone);
        WorkaholicCanGuess = BooleanOptionItem.Create(15706, "CanGuess", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Workaholic]);
        WorkaholicTasks = OverrideTasksData.Create(15707, TabGroup.NeutralRoles, CustomRoles.Workaholic);

        TextOptionItem.Create(10000014, "RoleType.NeutralKilling", TabGroup.NeutralRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(127, 140, 141, byte.MaxValue));
        
        Agitater.SetupCustomOption();
        
        SetupRoleOptions(15900, TabGroup.NeutralRoles, CustomRoles.Arsonist);
        ArsonistDouseTime = FloatOptionItem.Create(15902, "ArsonistDouseTime", new(0f, 10f, 1f), 0f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Arsonist])
            .SetValueFormat(OptionFormat.Seconds);
        ArsonistCooldown = FloatOptionItem.Create(15903, "Cooldown", new(0f, 180f, 1f), 25f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Arsonist])
            .SetValueFormat(OptionFormat.Seconds);
        ArsonistCanIgniteAnytime = BooleanOptionItem.Create(15904, "ArsonistCanIgniteAnytime", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Arsonist]);
        ArsonistMinPlayersToIgnite = IntegerOptionItem.Create(15905, "ArsonistMinPlayersToIgnite", new(1, 14, 1), 1, TabGroup.NeutralRoles, false)
            .SetParent(ArsonistCanIgniteAnytime);
        ArsonistMaxPlayersToIgnite = IntegerOptionItem.Create(15906, "ArsonistMaxPlayersToIgnite", new(1, 14, 1), 3, TabGroup.NeutralRoles, false)
            .SetParent(ArsonistCanIgniteAnytime);
        
        Bandit.SetupCustomOption();

        BloodKnight.SetupCustomOption();

        Gamer.SetupCustomOption();

        Glitch.SetupCustomOption();

        HexMaster.SetupCustomOption();

        Huntsman.SetupCustomOption();

        Infectious.SetupCustomOption();

        Jackal.SetupCustomOption();

        Jinx.SetupCustomOption();

        Juggernaut.SetupCustomOption();

        Medusa.SetupCustomOption();

        Necromancer.SetupCustomOption();

        //Occultist.SetupCustomOption();

        Pelican.SetupCustomOption();

        Pickpocket.SetupCustomOption();

        Poisoner.SetupCustomOption();

        PlagueBearer.SetupCustomOption();

        PotionMaster.SetupCustomOption();

        Pyromaniac.SetupCustomOption();

        if (!Quizmaster.InExperimental)
            Quizmaster.SetupCustomOption();

        NSerialKiller.SetupCustomOption(); // Serial Killer

        Shroud.SetupCustomOption();

        DarkHide.SetupCustomOption(); // Stalker (TOHY)

        Traitor.SetupCustomOption();

        Virus.SetupCustomOption();

        Werewolf.SetupCustomOption();

        Wraith.SetupCustomOption();

        #endregion

        #region Add-Ons Settings
        // Add-Ons 
        TextOptionItem.Create(10000015, "RoleType.Helpful", TabGroup.Addons) // HELPFUL
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 154, 206, byte.MaxValue));


        SetupAdtRoleOptions(18600, CustomRoles.Autopsy, canSetNum: true);
        ImpCanBeAutopsy = BooleanOptionItem.Create(18603, "ImpCanBeAutopsy", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Autopsy]);
        CrewCanBeAutopsy = BooleanOptionItem.Create(18604, "CrewCanBeAutopsy", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Autopsy]);
        NeutralCanBeAutopsy = BooleanOptionItem.Create(18605, "NeutralCanBeAutopsy", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Autopsy]);
        
        SetupAdtRoleOptions(18700, CustomRoles.Bait, canSetNum: true);
        ImpCanBeBait = BooleanOptionItem.Create(18703, "ImpCanBeBait", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bait]);
        CrewCanBeBait = BooleanOptionItem.Create(18704, "CrewCanBeBait", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bait]);
        NeutralCanBeBait = BooleanOptionItem.Create(18705, "NeutralCanBeBait", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bait]);
        BaitDelayMin = FloatOptionItem.Create(18706, "BaitDelayMin", new(0f, 5f, 1f), 0f, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bait])
            .SetValueFormat(OptionFormat.Seconds);
        BaitDelayMax = FloatOptionItem.Create(18707, "BaitDelayMax", new(0f, 10f, 1f), 0f, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bait])
            .SetValueFormat(OptionFormat.Seconds);
        BaitDelayNotify = BooleanOptionItem.Create(18708, "BaitDelayNotify", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bait]);
        BaitNotification = BooleanOptionItem.Create(18709, "BaitNotification", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bait]);
        BaitCanBeReportedUnderAllConditions = BooleanOptionItem.Create(18710, "BaitCanBeReportedUnderAllConditions", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bait]);

        SetupAdtRoleOptions(18800, CustomRoles.Trapper, canSetNum: true);
        ImpCanBeTrapper = BooleanOptionItem.Create(18803, "ImpCanBeTrapper", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Trapper]);
        CrewCanBeTrapper = BooleanOptionItem.Create(18804, "CrewCanBeTrapper", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Trapper]);
        NeutralCanBeTrapper = BooleanOptionItem.Create(18805, "NeutralCanBeTrapper", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Trapper]);
        TrapperBlockMoveTime = FloatOptionItem.Create(18806, "TrapperBlockMoveTime", new(1f, 180f, 1f), 5f, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Trapper])
            .SetValueFormat(OptionFormat.Seconds);
        
        SetupAdtRoleOptions(18900, CustomRoles.Bewilder, canSetNum: true);
        BewilderVision = FloatOptionItem.Create(18903, "BewilderVision", new(0f, 5f, 0.05f), 0.6f, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bewilder])
            .SetValueFormat(OptionFormat.Multiplier);
        ImpCanBeBewilder = BooleanOptionItem.Create(18904, "ImpCanBeBewilder", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bewilder]);
        CrewCanBeBewilder = BooleanOptionItem.Create(18905, "CrewCanBeBewilder", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bewilder]);
        NeutralCanBeBewilder = BooleanOptionItem.Create(18906, "NeutralCanBeBewilder", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bewilder]);
        KillerGetBewilderVision = BooleanOptionItem.Create(18907, "KillerGetBewilderVision", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bewilder]);
        
        SetupAdtRoleOptions(19000, CustomRoles.Burst, canSetNum: true);
        ImpCanBeBurst = BooleanOptionItem.Create(19003, "ImpCanBeBurst", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Burst]);
        CrewCanBeBurst = BooleanOptionItem.Create(19004, "CrewCanBeBurst", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Burst]);
        NeutralCanBeBurst = BooleanOptionItem.Create(19005, "NeutralCanBeBurst", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Burst]);
        BurstKillDelay = FloatOptionItem.Create(19006, "BurstKillDelay", new(1f, 180f, 1f), 5f, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Burst])
            .SetValueFormat(OptionFormat.Seconds);
        
        SetupAdtRoleOptions(19100, CustomRoles.Cyber, canSetNum: true);
        ImpCanBeCyber = BooleanOptionItem.Create(19103, "ImpCanBeCyber", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        CrewCanBeCyber = BooleanOptionItem.Create(19104, "CrewCanBeCyber", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        NeutralCanBeCyber = BooleanOptionItem.Create(19105, "NeutralCanBeCyber", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        ImpKnowCyberDead = BooleanOptionItem.Create(19106, "ImpKnowCyberDead", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        CrewKnowCyberDead = BooleanOptionItem.Create(19107, "CrewKnowCyberDead", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        NeutralKnowCyberDead = BooleanOptionItem.Create(19108, "NeutralKnowCyberDead", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        CyberKnown = BooleanOptionItem.Create(19109, "CyberKnown", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        
        SetupAdtRoleOptions(19200, CustomRoles.DoubleShot, canSetNum: true, tab: TabGroup.Addons);
        ImpCanBeDoubleShot = BooleanOptionItem.Create(19203, "ImpCanBeDoubleShot", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DoubleShot]);
        CrewCanBeDoubleShot = BooleanOptionItem.Create(19204, "CrewCanBeDoubleShot", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DoubleShot]);
        NeutralCanBeDoubleShot = BooleanOptionItem.Create(19205, "NeutralCanBeDoubleShot", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DoubleShot]);

        Flash.SetupCustomOption();

        SetupAdtRoleOptions(19300, CustomRoles.Lazy, canSetNum: true);
        TasklessCrewCanBeLazy = BooleanOptionItem.Create(19303, "TasklessCrewCanBeLazy", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lazy]);
        TaskBasedCrewCanBeLazy = BooleanOptionItem.Create(19304, "TaskBasedCrewCanBeLazy", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lazy]);
        
        SetupAdtRoleOptions(19400, CustomRoles.Loyal, canSetNum: true);
        ImpCanBeLoyal = BooleanOptionItem.Create(19403, "ImpCanBeLoyal", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Loyal]);
        CrewCanBeLoyal = BooleanOptionItem.Create(19404, "CrewCanBeLoyal", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Loyal]);
        
        SetupAdtRoleOptions(19500, CustomRoles.Lucky, canSetNum: true);
        LuckyProbability = IntegerOptionItem.Create(19503, "LuckyProbability", new(0, 100, 5), 50, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lucky])
            .SetValueFormat(OptionFormat.Percent);
        ImpCanBeLucky = BooleanOptionItem.Create(19504, "ImpCanBeLucky", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lucky]);
        CrewCanBeLucky = BooleanOptionItem.Create(19505, "CrewCanBeLucky", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lucky]);
        NeutralCanBeLucky = BooleanOptionItem.Create(19506, "NeutralCanBeLucky", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lucky]);
        
        SetupAdtRoleOptions(19600, CustomRoles.Necroview, canSetNum: true, tab: TabGroup.Addons);
        ImpCanBeNecroview = BooleanOptionItem.Create(19603, "ImpCanBeNecroview", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Necroview]);
        CrewCanBeNecroview = BooleanOptionItem.Create(19604, "CrewCanBeNecroview", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Necroview]);
        NeutralCanBeNecroview = BooleanOptionItem.Create(19605, "NeutralCanBeNecroview", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Necroview]);
        
        SetupAdtRoleOptions(19700, CustomRoles.Nimble, canSetNum: true, tab: TabGroup.Addons);
        /*CrewCanBeNimble = BooleanOptionItem.Create(19704, "CrewCanBeNimble", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Nimble]);
        NeutralCanBeNimble = BooleanOptionItem.Create(19705, "NeutralCanBeNimble", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Nimble]); */

        SetupAdtRoleOptions(19800, CustomRoles.Overclocked, canSetNum: true);
        OverclockedReduction = FloatOptionItem.Create(19803, "OverclockedReduction", new(0f, 90f, 5f), 40f, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Overclocked])
            .SetValueFormat(OptionFormat.Percent);
        
        Repairman.SetupCustomOption(); //Repairman
        
        SetupAdtRoleOptions(20000, CustomRoles.Seer, canSetNum: true);
        ImpCanBeSeer = BooleanOptionItem.Create(20003, "ImpCanBeSeer", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Seer]);
        CrewCanBeSeer = BooleanOptionItem.Create(20004, "CrewCanBeSeer", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Seer]);
        NeutralCanBeSeer = BooleanOptionItem.Create(20005, "NeutralCanBeSeer", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Seer]);

        Silent.SetupCustomOptions();

        SetupAdtRoleOptions(20100, CustomRoles.Sleuth, canSetNum: true);
        ImpCanBeSleuth = BooleanOptionItem.Create(20103, "ImpCanBeSleuth", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Sleuth]);
        CrewCanBeSleuth = BooleanOptionItem.Create(20104, "CrewCanBeSleuth", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Sleuth]);
        NeutralCanBeSleuth = BooleanOptionItem.Create(20105, "NeutralCanBeSleuth", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Sleuth]);
        SleuthCanKnowKillerRole = BooleanOptionItem.Create(20106, "SleuthCanKnowKillerRole", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Sleuth]);

        SetupAdtRoleOptions(20200, CustomRoles.Brakar, canSetNum: true);
        ImpCanBeTiebreaker = BooleanOptionItem.Create(20203, "ImpCanBeTiebreaker", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Brakar]);
        CrewCanBeTiebreaker = BooleanOptionItem.Create(20204, "CrewCanBeTiebreaker", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Brakar]);
        NeutralCanBeTiebreaker = BooleanOptionItem.Create(20205, "NeutralCanBeTiebreaker", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Brakar]);
        
        SetupAdtRoleOptions(20300, CustomRoles.Torch, canSetNum: true);
        TorchVision = FloatOptionItem.Create(20303, "TorchVision", new(0.5f, 5f, 0.25f), 1.25f, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Torch])
            .SetValueFormat(OptionFormat.Multiplier);
        TorchAffectedByLights = BooleanOptionItem.Create(20304, "TorchAffectedByLights", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Torch]);
        
        SetupAdtRoleOptions(20400, CustomRoles.Watcher, canSetNum: true);
        ImpCanBeWatcher = BooleanOptionItem.Create(20403, "ImpCanBeWatcher", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Watcher]);
        CrewCanBeWatcher = BooleanOptionItem.Create(20404, "CrewCanBeWatcher", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Watcher]);
        NeutralCanBeWatcher = BooleanOptionItem.Create(20405, "NeutralCanBeWatcher", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Watcher]);

        TextOptionItem.Create(10000016, "RoleType.Harmful", TabGroup.Addons) // HARMFUL
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 154, 206, byte.MaxValue));

        SetupAdtRoleOptions(20500, CustomRoles.Unreportable, canSetNum: true);
        ImpCanBeUnreportable = BooleanOptionItem.Create(20503, "ImpCanBeUnreportable", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Unreportable]);
        CrewCanBeUnreportable = BooleanOptionItem.Create(20504, "CrewCanBeUnreportable", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Unreportable]);
        NeutralCanBeUnreportable = BooleanOptionItem.Create(20505, "NeutralCanBeUnreportable", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Unreportable]);
        /*
         * Fool
         */
        SetupAdtRoleOptions(25600, CustomRoles.Fool, canSetNum: true, tab: TabGroup.Addons);
        ImpCanBeFool = BooleanOptionItem.Create(25603, "ImpCanBeFool", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Fool]);
        CrewCanBeFool = BooleanOptionItem.Create(25604, "CrewCanBeFool", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Fool]);
        NeutralCanBeFool = BooleanOptionItem.Create(25605, "NeutralCanBeFool", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Fool]);

        SetupAdtRoleOptions(20600, CustomRoles.Fragile, canSetNum: true);
        ImpCanBeFragile = BooleanOptionItem.Create(20603, "ImpCanBeFragile", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        CrewCanBeFragile = BooleanOptionItem.Create(20604, "CrewCanBeFragile", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        NeutralCanBeFragile = BooleanOptionItem.Create(20605, "NeutralCanBeFragile", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        ImpCanKillFragile = BooleanOptionItem.Create(20606, "ImpCanKillFragile", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        CrewCanKillFragile = BooleanOptionItem.Create(20607, "CrewCanKillFragile", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        NeutralCanKillFragile = BooleanOptionItem.Create(20608, "NeutralCanKillFragile", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);
        FragileKillerLunge = BooleanOptionItem.Create(20609, "FragileKillerLunge", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Fragile]);

        Hurried.SetupCustomOption();

        Influenced.SetupCustomOption();

        Mundane.SetupCustomOption();

        SetupAdtRoleOptions(20700, CustomRoles.Oblivious, canSetNum: true);
        ImpCanBeOblivious = BooleanOptionItem.Create(20703, "ImpCanBeOblivious", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Oblivious]);
        CrewCanBeOblivious = BooleanOptionItem.Create(20704, "CrewCanBeOblivious", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Oblivious]);
        NeutralCanBeOblivious = BooleanOptionItem.Create(20705, "NeutralCanBeOblivious", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Oblivious]);
        ObliviousBaitImmune = BooleanOptionItem.Create(20706, "ObliviousBaitImmune", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Oblivious]);
        
        SetupAdtRoleOptions(20800, CustomRoles.Rascal, canSetNum: true, tab: TabGroup.Addons);
        RascalAppearAsMadmate = BooleanOptionItem.Create(20803, "RascalAppearAsMadmate", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Rascal]);

        //SetupAdtRoleOptions(20900, CustomRoles.Sunglasses, canSetNum: true);
        //SunglassesVision = FloatOptionItem.Create(20903, "SunglassesVision", new(0f, 5f, 0.05f), 0.75f, TabGroup.Addons, false)
        //    .SetParent(CustomRoleSpawnChances[CustomRoles.Sunglasses])
        //    .SetValueFormat(OptionFormat.Multiplier);
        //ImpCanBeSunglasses = BooleanOptionItem.Create(20904, "ImpCanBeSunglasses", true, TabGroup.Addons, false)
        //    .SetParent(CustomRoleSpawnChances[CustomRoles.Sunglasses]);
        //CrewCanBeSunglasses = BooleanOptionItem.Create(20905, "CrewCanBeSunglasses", true, TabGroup.Addons, false)
        //    .SetParent(CustomRoleSpawnChances[CustomRoles.Sunglasses]);
        //NeutralCanBeSunglasses = BooleanOptionItem.Create(20906, "NeutralCanBeSunglasses", true, TabGroup.Addons, false)
        //    .SetParent(CustomRoleSpawnChances[CustomRoles.Sunglasses]);

        SetupAdtRoleOptions(21000, CustomRoles.Unlucky, canSetNum: true);
        UnluckyKillSuicideChance = IntegerOptionItem.Create(21003, "UnluckyKillSuicideChance", new(0, 100, 1), 2, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky])
            .SetValueFormat(OptionFormat.Percent);
        UnluckyTaskSuicideChance = IntegerOptionItem.Create(21004, "UnluckyTaskSuicideChance", new(0, 100, 1), 5, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky])
            .SetValueFormat(OptionFormat.Percent);
        UnluckyVentSuicideChance = IntegerOptionItem.Create(21005, "UnluckyVentSuicideChance", new(0, 100, 1), 3, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky])
            .SetValueFormat(OptionFormat.Percent);
        UnluckyReportSuicideChance = IntegerOptionItem.Create(21006, "UnluckyReportSuicideChance", new(0, 100, 1), 1, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky])
            .SetValueFormat(OptionFormat.Percent);
        UnluckySabotageSuicideChance = IntegerOptionItem.Create(21007, "UnluckySabotageSuicideChance", new(0, 100, 1), 4, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky])
            .SetValueFormat(OptionFormat.Percent);
        ImpCanBeUnlucky = BooleanOptionItem.Create(21008, "ImpCanBeUnlucky", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky]);
        CrewCanBeUnlucky = BooleanOptionItem.Create(21009, "CrewCanBeUnlucky", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky]);
        NeutralCanBeUnlucky = BooleanOptionItem.Create(21010, "NeutralCanBeUnlucky", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky]);

        SetupAdtRoleOptions(21100, CustomRoles.VoidBallot, canSetNum: true);
        ImpCanBeVoidBallot = BooleanOptionItem.Create(21103, "ImpCanBeVoidBallot", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.VoidBallot]);
        CrewCanBeVoidBallot = BooleanOptionItem.Create(21104, "CrewCanBeVoidBallot", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.VoidBallot]);
        NeutralCanBeVoidBallot = BooleanOptionItem.Create(21105, "NeutralCanBeVoidBallot", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.VoidBallot]);

        TextOptionItem.Create(10000017, "RoleType.Mixed", TabGroup.Addons) // MIXED
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 154, 206, byte.MaxValue));
        
        SetupAdtRoleOptions(21400, CustomRoles.Antidote, canSetNum: true);
        ImpCanBeAntidote = BooleanOptionItem.Create(21403, "ImpCanBeAntidote", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Antidote]);
        CrewCanBeAntidote = BooleanOptionItem.Create(21404, "CrewCanBeAntidote", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Antidote]);
        NeutralCanBeAntidote = BooleanOptionItem.Create(21405, "NeutralCanBeAntidote", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Antidote]);
        AntidoteCDOpt = FloatOptionItem.Create(21406, "AntidoteCDOpt", new(0f, 180f, 1f), 5f, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Antidote])
            .SetValueFormat(OptionFormat.Seconds);
        AntidoteCDReset = BooleanOptionItem.Create(21407, "AntidoteCDReset", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Antidote]);
        
        SetupAdtRoleOptions(21500, CustomRoles.Avanger, canSetNum: true);
        ImpCanBeAvanger = BooleanOptionItem.Create(21503, "ImpCanBeAvanger", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Avanger]);
        CrewCanBeAvanger = BooleanOptionItem.Create(21504, "CrewCanBeAvanger", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Avanger]);
        NeutralCanBeAvanger = BooleanOptionItem.Create(21505, "NeutralCanBeAvanger", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Avanger]);
        
        SetupAdtRoleOptions(21600, CustomRoles.Aware, canSetNum: true);
        ImpCanBeAware = BooleanOptionItem.Create(21603, "ImpCanBeAware", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Aware]);
        CrewCanBeAware = BooleanOptionItem.Create(21604, "CrewCanBeAware", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Aware]);
        NeutralCanBeAware = BooleanOptionItem.Create(21605, "NeutralCanBeAware", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Aware]);
        AwareknowRole = BooleanOptionItem.Create(21606, "AwareKnowRole", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Aware]);
        
        SetupAdtRoleOptions(21700, CustomRoles.Bloodlust, canSetNum: true);
        
        SetupAdtRoleOptions(21800, CustomRoles.Diseased, canSetNum: true);
        ImpCanBeDiseased = BooleanOptionItem.Create(21803, "ImpCanBeDiseased", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Diseased]);
        CrewCanBeDiseased = BooleanOptionItem.Create(21804, "CrewCanBeDiseased", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Diseased]);
        NeutralCanBeDiseased = BooleanOptionItem.Create(21805, "NeutralCanBeDiseased", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Diseased]);
        DiseasedCDOpt = FloatOptionItem.Create(21806, "DiseasedCDOpt", new(0f, 180f, 1f), 25f, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Diseased])
            .SetValueFormat(OptionFormat.Seconds);
        DiseasedCDReset = BooleanOptionItem.Create(21807, "DiseasedCDReset", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Diseased]);
        
        SetupAdtRoleOptions(21900, CustomRoles.Ghoul, canSetNum: true);

        //BYE GLOW, SEE YOU NEVER - LOOOOOL
        // SetupAdtRoleOptions(22000, CustomRoles.Glow, canSetNum: true);
        //ImpCanBeGlow = BooleanOptionItem.Create(22003, "ImpCanBeGlow", true, TabGroup.Addons, false)
        //.SetParent(CustomRoleSpawnChances[CustomRoles.Glow]);
        //CrewCanBeGlow = BooleanOptionItem.Create(22004, "CrewCanBeGlow", true, TabGroup.Addons, false)
        //.SetParent(CustomRoleSpawnChances[CustomRoles.Glow]);
        //NeutralCanBeGlow = BooleanOptionItem.Create(22005, "NeutralCanBeGlow", true, TabGroup.Addons, false)
        //.SetParent(CustomRoleSpawnChances[CustomRoles.Glow]);

        SetupAdtRoleOptions(22100, CustomRoles.Gravestone, canSetNum: true);
        ImpCanBeGravestone = BooleanOptionItem.Create(22103, "ImpCanBeGravestone", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Gravestone]);
        CrewCanBeGravestone = BooleanOptionItem.Create(22104, "CrewCanBeGravestone", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Gravestone]);
        NeutralCanBeGravestone = BooleanOptionItem.Create(22105, "NeutralCanBeGravestone", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Gravestone]);

        
        SetupAdtRoleOptions(22200, CustomRoles.Guesser, canSetNum: true, tab: TabGroup.Addons);
        ImpCanBeGuesser = BooleanOptionItem.Create(22203, "ImpCanBeGuesser", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        CrewCanBeGuesser = BooleanOptionItem.Create(22204, "CrewCanBeGuesser", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        NeutralCanBeGuesser = BooleanOptionItem.Create(22205, "NeutralCanBeGuesser", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        GCanGuessAdt = BooleanOptionItem.Create(22206, "GCanGuessAdt", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        GCanGuessTaskDoneSnitch = BooleanOptionItem.Create(22207, "GCanGuessTaskDoneSnitch", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        GTryHideMsg = BooleanOptionItem.Create(22208, "GuesserTryHideMsg", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Guesser])
            .SetColor(Color.green);

        Oiiai.SetupCustomOptions();

        SetupAdtRoleOptions(22300, CustomRoles.Rebound, canSetNum: true, tab: TabGroup.Addons);
        ImpCanBeRebound = BooleanOptionItem.Create(22303, "ImpCanBeRebound", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Rebound]);
        CrewCanBeRebound = BooleanOptionItem.Create(22304, "CrewCanBeRebound", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Rebound]);
        NeutralCanBeRebound = BooleanOptionItem.Create(22305, "NeutralCanBeRebound", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Rebound]);

        SetupAdtRoleOptions(22400, CustomRoles.DualPersonality, canSetNum: true);
        ImpCanBeDualPersonality = BooleanOptionItem.Create(22403, "ImpCanBeDualPersonality", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DualPersonality]);
        CrewCanBeDualPersonality = BooleanOptionItem.Create(22404, "CrewCanBeDualPersonality", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DualPersonality]);
        DualVotes = BooleanOptionItem.Create(22405, "DualVotes", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DualPersonality]);
      //HideDualVotes = BooleanOptionItem.Create(22406, "HideDualVotes", true, TabGroup.Addons, false)
      //    .SetParent(DualVotes);

        SetupAdtRoleOptions(22500, CustomRoles.Stubborn, canSetNum: true);
        ImpCanBeStubborn = BooleanOptionItem.Create(22503, "ImpCanBeStubborn", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Stubborn]);
        CrewCanBeStubborn = BooleanOptionItem.Create(22504, "CrewCanBeStubborn", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Stubborn]);
        NeutralCanBeStubborn = BooleanOptionItem.Create(22505, "NeutralCanBeStubborn", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Stubborn]);

        Susceptible.SetupCustomOptions();

        TextOptionItem.Create(10000018, "RoleType.Impostor", TabGroup.Addons) // IMPOSTOR
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 25, 25, byte.MaxValue));

        /*
         * Circumvent
         */
        SetupAdtRoleOptions(22600, CustomRoles.Circumvent, canSetNum: true, tab: TabGroup.Addons);

        /*
         * Clumsy
         */
        SetupAdtRoleOptions(22700, CustomRoles.Clumsy, canSetNum: true, tab: TabGroup.Addons);
        ChanceToMiss = IntegerOptionItem.Create(22703, "ChanceToMiss", new(0, 100, 5), 50, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Clumsy])
            .SetValueFormat(OptionFormat.Percent);

        /*
         * Last Impostor
         */
        LastImpostor.SetupCustomOption();

        /*
         * Madmate
         */
        SetupAdtRoleOptions(22900, CustomRoles.Madmate, canSetNum: true, canSetChance: false);
        MadmateSpawnMode = StringOptionItem.Create(22903, "MadmateSpawnMode", madmateSpawnMode, 0, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        MadmateCountMode = StringOptionItem.Create(22904, "MadmateCountMode", madmateCountMode, 1, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        SheriffCanBeMadmate = BooleanOptionItem.Create(22905, "SheriffCanBeMadmate", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        MayorCanBeMadmate = BooleanOptionItem.Create(22906, "MayorCanBeMadmate", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        NGuesserCanBeMadmate = BooleanOptionItem.Create(22907, "NGuesserCanBeMadmate", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        MarshallCanBeMadmate = BooleanOptionItem.Create(22908, "MarshallCanBeMadmate", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        FarseerCanBeMadmate = BooleanOptionItem.Create(22909, "FarseerCanBeMadmate", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        RetributionistCanBeMadmate = BooleanOptionItem.Create(22910, "RetributionistCanBeMadmate", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        SnitchCanBeMadmate = BooleanOptionItem.Create(22911, "SnitchCanBeMadmate", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        MadSnitchTasks = IntegerOptionItem.Create(22912, "MadSnitchTasks", new(1, 30, 1), 3, TabGroup.Addons, false)
            .SetParent(SnitchCanBeMadmate)
            .SetValueFormat(OptionFormat.Pieces);
        JudgeCanBeMadmate = BooleanOptionItem.Create(22913, "JudgeCanBeMadmate", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);

        /*
         * Mare
         */
        Mare.SetupCustomOption();

        /*
         * Mimic
         */
        SetupAdtRoleOptions(23100, CustomRoles.Mimic, canSetNum: true, tab: TabGroup.Addons);
        MimicCanSeeDeadRoles = BooleanOptionItem.Create(23103, "MimicCanSeeDeadRoles", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mimic]);
        
        SetupAdtRoleOptions(23200, CustomRoles.TicketsStealer, canSetNum: true, tab: TabGroup.Addons);
        TicketsPerKill = FloatOptionItem.Create(23203, "TicketsPerKill", new(0.1f, 10f, 0.1f), 0.5f, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.TicketsStealer]);
        
        SetupAdtRoleOptions(23300, CustomRoles.Swift, canSetNum: true, tab: TabGroup.Addons);

      //SetupAdtRoleOptions(23400, CustomRoles.Minimalism, canSetNum: true, tab: TabGroup.Addons);


        TextOptionItem.Create(10000019, "RoleType.Misc", TabGroup.Addons) // NEUTRAL
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(127, 140, 141, byte.MaxValue));
        
        SetupAdtRoleOptions(23500, CustomRoles.Egoist, canSetNum: true, tab: TabGroup.Addons);
        CrewCanBeEgoist = BooleanOptionItem.Create(23503, "CrewCanBeEgoist", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Egoist]);
        ImpCanBeEgoist = BooleanOptionItem.Create(23504, "ImpCanBeEgoist", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Egoist]);
        ImpEgoistVisibalToAllies = BooleanOptionItem.Create(23505, "ImpEgoistVisibalToAllies", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Egoist]);
        EgoistCountAsConverted = BooleanOptionItem.Create(23506, "EgoistCountAsConverted", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Egoist]);

        SetupLoversRoleOptionsToggle(23600);
        
        SetupAdtRoleOptions(23700, CustomRoles.Reach, canSetNum: true);
        
        Workhorse.SetupCustomOption();

        #endregion

        #region Experimental Roles/Add-ons Settings
        TextOptionItem.Create(10000020, "OtherRoles.ImpostorRoles", TabGroup.OtherRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(247, 70, 49, byte.MaxValue));

        
        /*SetupRoleOptions(24000, TabGroup.OtherRoles, CustomRoles.Capitalism);
        CapitalismSkillCooldown = FloatOptionItem.Create(24003, "CapitalismSkillCooldown", new(2.5f, 180f, 2.5f), 20f, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Capitalism])
            .SetValueFormat(OptionFormat.Seconds);*/
        
        Disperser.SetupCustomOption();        

        // 船员
        TextOptionItem.Create(10000021, "OtherRoles.CrewmateRoles", TabGroup.OtherRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(140, 255, 255, byte.MaxValue));

        /*SetupRoleOptions(24700, TabGroup.OtherRoles, CustomRoles.SpeedBooster);
        SpeedBoosterUpSpeed = FloatOptionItem.Create(24703, "SpeedBoosterUpSpeed", new(0.1f, 1.0f, 0.1f), 0.2f, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.SpeedBooster])
            .SetValueFormat(OptionFormat.Multiplier);
        SpeedBoosterTimes = IntegerOptionItem.Create(24704, "SpeedBoosterTimes", new(1, 99, 1), 5, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.SpeedBooster])
            .SetValueFormat(OptionFormat.Times); */

        //Divinator.SetupCustomOption();
        
        Investigator.SetupCustomOption();
        
        // 中立
        TextOptionItem.Create(10000022, "OtherRoles.NeutralRoles", TabGroup.OtherRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(127, 140, 141, byte.MaxValue));
        
        Doppelganger.SetupCustomOption();
        
        SetupRoleOptions(25100, TabGroup.OtherRoles, CustomRoles.God);
        NotifyGodAlive = BooleanOptionItem.Create(25103, "NotifyGodAlive", true, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.God]);
        GodCanGuess = BooleanOptionItem.Create(25104, "CanGuess", false, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.God]);

        if (Quizmaster.InExperimental)
            Quizmaster.SetupCustomOption();

        Spiritcaller.SetupCustomOption();

        // 副职
        TextOptionItem.Create(10000023, "OtherRoles.Addons", TabGroup.OtherRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 154, 206, byte.MaxValue));

        //SetupAdtRoleOptions(25300, CustomRoles.Ntr, tab: TabGroup.OtherRoles);

        SetupAdtRoleOptions(25500, CustomRoles.Youtuber, canSetNum: true, tab: TabGroup.OtherRoles);
        

        #endregion

        #region System Settings
        TemporaryAntiBlackoutFix = BooleanOptionItem.Create(60030, "TemporaryAntiBlackoutFix", true, TabGroup.SystemSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(Color.red);
        GradientTagsOpt = BooleanOptionItem.Create(60031, "EnableGadientTags", false, TabGroup.SystemSettings, false)
            .SetHeader(true);
        EnableKillerLeftCommand = BooleanOptionItem.Create(60040, "EnableKillerLeftCommand", true, TabGroup.SystemSettings, false)
            .SetColor(Color.green)
            .HideInHnS();
        SeeEjectedRolesInMeeting = BooleanOptionItem.Create(60041, "SeeEjectedRolesInMeeting", true, TabGroup.SystemSettings, false)
            .SetColor(Color.green)
            .HideInHnS();
        
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
        KickPlayerFriendCodeNotExist = BooleanOptionItem.Create(60080, "KickPlayerFriendCodeNotExist", true, TabGroup.SystemSettings, false);
        TempBanPlayerFriendCodeNotExist = BooleanOptionItem.Create(60081, "TempBanPlayerFriendCodeNotExist", false, TabGroup.SystemSettings, false)
            .SetParent(KickPlayerFriendCodeNotExist);
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
        PlayerAutoStart = IntegerOptionItem.Create(60190, "PlayerAutoStart", new(1, 15, 1), 14, TabGroup.SystemSettings, false);
        AutoStartTimer = IntegerOptionItem.Create(60200, "AutoStartTimer", new(10, 600, 1), 20, TabGroup.SystemSettings, false)
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
        EndWhenPlayerBug = BooleanOptionItem.Create(60240, "EndWhenPlayerBug", true, TabGroup.SystemSettings, false)
            .SetColor(Color.blue);
        HideExileChat = BooleanOptionItem.Create(60292, "HideExileChat", true, TabGroup.SystemSettings, false)
            .SetColor(Color.blue)
            .HideInHnS();
        RemovePetsAtDeadPlayers = BooleanOptionItem.Create(60294, "RemovePetsAtDeadPlayers", false, TabGroup.SystemSettings, false)
            .SetColor(Color.magenta);

        CheatResponses = StringOptionItem.Create(60250, "CheatResponses", CheatResponsesName, 0, TabGroup.SystemSettings, false)
            .SetHeader(true);

        AutoDisplayKillLog = BooleanOptionItem.Create(60270, "AutoDisplayKillLog", true, TabGroup.SystemSettings, false)
            .SetHeader(true)
            .HideInHnS();
        AutoDisplayLastRoles = BooleanOptionItem.Create(60280, "AutoDisplayLastRoles", true, TabGroup.SystemSettings, false)
            .HideInHnS();
        AutoDisplayLastResult = BooleanOptionItem.Create(60290, "AutoDisplayLastResult", true, TabGroup.SystemSettings, false)
            .HideInHnS();
        SuffixMode = StringOptionItem.Create(60300, "SuffixMode", suffixModes, 0, TabGroup.SystemSettings, true)
            .SetHeader(true);
        HideHostText = BooleanOptionItem.Create(60311, "HideHostText", false, TabGroup.SystemSettings, false);
        HideGameSettings = BooleanOptionItem.Create(60310, "HideGameSettings", false, TabGroup.SystemSettings, false);
        //DIYGameSettings = BooleanOptionItem.Create(60320, "DIYGameSettings", false, TabGroup.SystemSettings, false);
        PlayerCanSetColor = BooleanOptionItem.Create(60330, "PlayerCanSetColor", false, TabGroup.SystemSettings, false);
        PlayerCanUseQuitCommand = BooleanOptionItem.Create(60331, "PlayerCanUseQuitCommand", false, TabGroup.SystemSettings, false);
        PlayerCanSetName = BooleanOptionItem.Create(60332, "PlayerCanSetName", false, TabGroup.SystemSettings, false);
        FormatNameMode = StringOptionItem.Create(60340, "FormatNameMode", formatNameModes, 0, TabGroup.SystemSettings, false);
        DisableEmojiName = BooleanOptionItem.Create(60350, "DisableEmojiName", true, TabGroup.SystemSettings, false);
        ChangeNameToRoleInfo = BooleanOptionItem.Create(60360, "ChangeNameToRoleInfo", true, TabGroup.SystemSettings, false)
            .HideInHnS();
        SendRoleDescriptionFirstMeeting = BooleanOptionItem.Create(60370, "SendRoleDescriptionFirstMeeting", false, TabGroup.SystemSettings, false)
            .HideInHnS();

        NoGameEnd = BooleanOptionItem.Create(60380, "NoGameEnd", false, TabGroup.SystemSettings, false)
            .SetColor(Color.red)
            .SetHeader(true);
        AllowConsole = BooleanOptionItem.Create(60390, "AllowConsole", false, TabGroup.SystemSettings, false)
            .SetColor(Color.red);
        RoleAssigningAlgorithm = StringOptionItem.Create(60400, "RoleAssigningAlgorithm", roleAssigningAlgorithms, 4, TabGroup.SystemSettings, true)
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

        #region Game Settings
        //FFA
        FFAManager.SetupCustomOption();

        // Hide & Seek
        TextOptionItem.Create(10000055, "MenuTitle.Hide&Seek", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.HidenSeekTOHE)
            .SetColor(Color.red);

        // Num impostors in Hide & Seek
        NumImpostorsHnS = IntegerOptionItem.Create(60891, "NumImpostorsHnS", new(1, 3, 1), 1, TabGroup.GameSettings, false)
            .SetHeader(true)
            .SetColor(Color.red)
            .SetGameMode(CustomGameMode.HidenSeekTOHE)
            .SetValueFormat(OptionFormat.Players);



        // Confirm Ejections Mode
        TextOptionItem.Create(10000024, "MenuTitle.Ejections", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
        CEMode = StringOptionItem.Create(60440, "ConfirmEjectionsMode", ConfirmEjectionsMode, 2, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
        ShowImpRemainOnEject = BooleanOptionItem.Create(60441, "ShowImpRemainOnEject", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
        ShowNKRemainOnEject = BooleanOptionItem.Create(60442, "ShowNKRemainOnEject", true, TabGroup.GameSettings, false)
            .SetParent(ShowImpRemainOnEject)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
        ShowTeamNextToRoleNameOnEject = BooleanOptionItem.Create(60443, "ShowTeamNextToRoleNameOnEject", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
        ConfirmEgoistOnEject = BooleanOptionItem.Create(60444, "ConfirmEgoistOnEject", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue))
            .SetHeader(true);
        ConfirmLoversOnEject = BooleanOptionItem.Create(60445, "ConfirmLoversOnEject", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));

        //Maps Settings
        TextOptionItem.Create(10000025, "MenuTitle.MapsSettings", TabGroup.GameSettings)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Random Maps Mode
        RandomMapsMode = BooleanOptionItem.Create(60450, "RandomMapsMode", false, TabGroup.GameSettings, false)
            .SetHeader(true)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        SkeldChance = IntegerOptionItem.Create(60451, "SkeldChance", new(0, 100, 5), 10, TabGroup.GameSettings, false)
            .SetParent(RandomMapsMode)
            .SetValueFormat(OptionFormat.Percent);
        MiraChance = IntegerOptionItem.Create(60452, "MiraChance", new(0, 100, 5), 10, TabGroup.GameSettings, false)
            .SetParent(RandomMapsMode)
            .SetValueFormat(OptionFormat.Percent);
        PolusChance = IntegerOptionItem.Create(60453, "PolusChance", new(0, 100, 5), 10, TabGroup.GameSettings, false)
            .SetParent(RandomMapsMode)
            .SetValueFormat(OptionFormat.Percent);
        DleksChance = IntegerOptionItem.Create(60457, "DleksChance", new(0, 100, 5), 10, TabGroup.GameSettings, false)
            .SetParent(RandomMapsMode)
            .SetValueFormat(OptionFormat.Percent);
        AirshipChance = IntegerOptionItem.Create(60454, "AirshipChance", new(0, 100, 5), 10, TabGroup.GameSettings, false)
            .SetParent(RandomMapsMode)
            .SetValueFormat(OptionFormat.Percent);
        FungleChance = IntegerOptionItem.Create(60455, "FungleChance", new(0, 100, 5), 10, TabGroup.GameSettings, false)
            .SetParent(RandomMapsMode)
            .SetValueFormat(OptionFormat.Percent);
        UseMoreRandomMapSelection = BooleanOptionItem.Create(60456, "UseMoreRandomMapSelection", false, TabGroup.GameSettings, false)
            .SetParent(RandomMapsMode)
            .SetValueFormat(OptionFormat.Percent);

        NewHideMsg = BooleanOptionItem.Create(60460, "NewHideMsg", true, TabGroup.GameSettings, false)
            .SetHidden(true)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(193, 255, 209, byte.MaxValue));
        // Random Spawn
        RandomSpawn = BooleanOptionItem.Create(60470, "RandomSpawn", false, TabGroup.GameSettings, false)
            .HideInFFA()
            //.SetGameMode(CustomGameMode.HidenSeekTOHE) Temporarily removed as additional changes are needed
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        SpawnRandomLocation = BooleanOptionItem.Create(60471, "SpawnRandomLocation", true, TabGroup.GameSettings, false)
            .SetParent(RandomSpawn);
        AirshipAdditionalSpawn = BooleanOptionItem.Create(60472, "AirshipAdditionalSpawn", true, TabGroup.GameSettings, false)
            .SetParent(SpawnRandomLocation);
        SpawnRandomVents = BooleanOptionItem.Create(60475, "SpawnRandomVents", false, TabGroup.GameSettings, false)
            .SetParent(RandomSpawn);
        MapModification = BooleanOptionItem.Create(60480, "MapModification", false, TabGroup.GameSettings, false)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Airship Variable Electrical
        AirshipVariableElectrical = BooleanOptionItem.Create(60481, "AirshipVariableElectrical", false, TabGroup.GameSettings, false)
            //.SetGameMode(CustomGameMode.Standard)
            .SetParent(MapModification)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Disable Airship Moving Platform
        DisableAirshipMovingPlatform = BooleanOptionItem.Create(60482, "DisableAirshipMovingPlatform", false, TabGroup.GameSettings, false)
            //.SetGameMode(CustomGameMode.Standard)
            .SetParent(MapModification)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Disable Spore Trigger On Fungle
        DisableSporeTriggerOnFungle = BooleanOptionItem.Create(60483, "DisableSporeTriggerOnFungle", false, TabGroup.GameSettings, false)
            //.SetGameMode(CustomGameMode.Standard)
            .SetParent(MapModification)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Disable Zipline On Fungle
        DisableZiplineOnFungle = BooleanOptionItem.Create(60490, "DisableZiplineOnFungle", false, TabGroup.GameSettings, false)
            //.SetGameMode(CustomGameMode.Standard)
            .SetParent(MapModification)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Disable Zipline From Top
        DisableZiplineFromTop = BooleanOptionItem.Create(60491, "DisableZiplineFromTop", false, TabGroup.GameSettings, false)
            //.SetGameMode(CustomGameMode.Standard)
            .SetParent(DisableZiplineOnFungle)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Disable Zipline From Under
        DisableZiplineFromUnder = BooleanOptionItem.Create(60492, "DisableZiplineFromUnder", false, TabGroup.GameSettings, false)
            //.SetGameMode(CustomGameMode.Standard)
            .SetParent(DisableZiplineOnFungle)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Reset Doors After Meeting
        ResetDoorsEveryTurns = BooleanOptionItem.Create(60500, "ResetDoorsEveryTurns", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Reset Doors Mode
        DoorsResetMode = StringOptionItem.Create(60501, "DoorsResetMode", EnumHelper.GetAllNames<DoorsReset.ResetMode>(), 2, TabGroup.GameSettings, false)
            .SetParent(ResetDoorsEveryTurns)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Change decontamination time on MiraHQ/Polus
        ChangeDecontaminationTime = BooleanOptionItem.Create(60503, "ChangeDecontaminationTime", false, TabGroup.GameSettings, false)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Decontamination time on MiraHQ
        DecontaminationTimeOnMiraHQ = FloatOptionItem.Create(60504, "DecontaminationTimeOnMiraHQ", new(0.5f, 10f, 0.25f), 3f, TabGroup.GameSettings, false)
            .SetParent(ChangeDecontaminationTime)
            .SetValueFormat(OptionFormat.Seconds)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Decontamination time on Polus
        DecontaminationTimeOnPolus = FloatOptionItem.Create(60505, "DecontaminationTimeOnPolus", new(0.5f, 10f, 0.25f), 3f, TabGroup.GameSettings, false)
            .SetParent(ChangeDecontaminationTime)
            .SetValueFormat(OptionFormat.Seconds)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        // Sabotage
        TextOptionItem.Create(10000026, "MenuTitle.Sabotage", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(243, 96, 96, byte.MaxValue))
            .SetHeader(true);
        // CommsCamouflage
        CommsCamouflage = BooleanOptionItem.Create(60510, "CommsCamouflage", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(243, 96, 96, byte.MaxValue));
        DisableOnSomeMaps = BooleanOptionItem.Create(60511, "DisableOnSomeMaps", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(CommsCamouflage);
        DisableOnSkeld = BooleanOptionItem.Create(60512, "DisableOnSkeld", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(DisableOnSomeMaps);
        DisableOnMira = BooleanOptionItem.Create(60513, "DisableOnMira", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(DisableOnSomeMaps);
        DisableOnPolus = BooleanOptionItem.Create(60514, "DisableOnPolus", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(DisableOnSomeMaps);
        DisableOnDleks = BooleanOptionItem.Create(60517, "DisableOnDleks", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(DisableOnSomeMaps);
        DisableOnAirship = BooleanOptionItem.Create(60515, "DisableOnAirship", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(DisableOnSomeMaps);
        DisableOnFungle = BooleanOptionItem.Create(60516, "DisableOnFungle", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(DisableOnSomeMaps);
        DisableReportWhenCC = BooleanOptionItem.Create(60520, "DisableReportWhenCC", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(CommsCamouflage);
        // Sabotage Cooldown Control
        SabotageCooldownControl = BooleanOptionItem.Create(60530, "SabotageCooldownControl", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(243, 96, 96, byte.MaxValue));
        SabotageCooldown = FloatOptionItem.Create(60535, "SabotageCooldown", new(1f, 60f, 1f), 30f, TabGroup.GameSettings, false)
            .SetParent(SabotageCooldownControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        // Sabotage Duration Control
        SabotageTimeControl = BooleanOptionItem.Create(60540, "SabotageTimeControl", false, TabGroup.GameSettings, false)
            .SetColor(new Color32(243, 96, 96, byte.MaxValue))
            .SetGameMode(CustomGameMode.Standard);
        // The Skeld
        SkeldReactorTimeLimit = FloatOptionItem.Create(60541, "SkeldReactorTimeLimit", new(5f, 90f, 1f), 30f, TabGroup.GameSettings, false)
            .SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        SkeldO2TimeLimit = FloatOptionItem.Create(60542, "SkeldO2TimeLimit", new(5f, 90f, 1f), 30f, TabGroup.GameSettings, false)
            .SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        // Mira HQ
        MiraReactorTimeLimit = FloatOptionItem.Create(60543, "MiraReactorTimeLimit", new(5f, 90f, 1f), 45f, TabGroup.GameSettings, false)
            .SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        MiraO2TimeLimit = FloatOptionItem.Create(60544, "MiraO2TimeLimit", new(5f, 90f, 1f), 45f, TabGroup.GameSettings, false)
            .SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        // Polus
        PolusReactorTimeLimit = FloatOptionItem.Create(60545, "PolusReactorTimeLimit", new(5f, 90f, 1f), 60f, TabGroup.GameSettings, false)
            .SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        // The Airship
        AirshipReactorTimeLimit = FloatOptionItem.Create(60546, "AirshipReactorTimeLimit", new(5f, 90f, 1f), 90f, TabGroup.GameSettings, false)
            .SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        // The Fungle
        FungleReactorTimeLimit = FloatOptionItem.Create(60547, "FungleReactorTimeLimit", new(5f, 90f, 1f), 60f, TabGroup.GameSettings, false)
            .SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        FungleMushroomMixupDuration = FloatOptionItem.Create(60548, "FungleMushroomMixupDuration", new(5f, 90f, 1f), 10f, TabGroup.GameSettings, false)
            .SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        // LightsOutSpecialSettings
        LightsOutSpecialSettings = BooleanOptionItem.Create(60550, "LightsOutSpecialSettings", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(243, 96, 96, byte.MaxValue));
        BlockDisturbancesToSwitches = BooleanOptionItem.Create(60551, "BlockDisturbancesToSwitches", false, TabGroup.GameSettings, false)
            .SetParent(LightsOutSpecialSettings)
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipViewingDeckLightsPanel = BooleanOptionItem.Create(60552, "DisableAirshipViewingDeckLightsPanel", false, TabGroup.GameSettings, false)
            .SetParent(LightsOutSpecialSettings)
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipGapRoomLightsPanel = BooleanOptionItem.Create(60553, "DisableAirshipGapRoomLightsPanel", false, TabGroup.GameSettings, false)
            .SetParent(LightsOutSpecialSettings)
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipCargoLightsPanel = BooleanOptionItem.Create(60554, "DisableAirshipCargoLightsPanel", false, TabGroup.GameSettings, false)
            .SetParent(LightsOutSpecialSettings)
            .SetGameMode(CustomGameMode.Standard);


        // Disable
        TextOptionItem.Create(10000027, "MenuTitle.Disable", TabGroup.GameSettings)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue))
            .HideInHnS();

        DisableShieldAnimations = BooleanOptionItem.Create(60560, "DisableShieldAnimations", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableKillAnimationOnGuess = BooleanOptionItem.Create(60561, "DisableKillAnimationOnGuess", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableVanillaRoles = BooleanOptionItem.Create(60562, "DisableVanillaRoles", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableTaskWin = BooleanOptionItem.Create(60563, "DisableTaskWin", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableMeeting = BooleanOptionItem.Create(60564, "DisableMeeting", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        // Disable Sabotage / CloseDoor
        DisableSabotage = BooleanOptionItem.Create(60565, "DisableSabotage", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableCloseDoor = BooleanOptionItem.Create(60566, "DisableCloseDoor", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        // Disable Devices
        DisableDevices = BooleanOptionItem.Create(60570, "DisableDevices", false, TabGroup.GameSettings, false)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue))
            .HideInHnS();
        //.SetGameMode(CustomGameMode.Standard);
        DisableSkeldDevices = BooleanOptionItem.Create(60571, "DisableSkeldDevices", false, TabGroup.GameSettings, false)
            .SetParent(DisableDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableSkeldAdmin = BooleanOptionItem.Create(60572, "DisableSkeldAdmin", false, TabGroup.GameSettings, false)
            .SetParent(DisableSkeldDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableSkeldCamera = BooleanOptionItem.Create(60573, "DisableSkeldCamera", false, TabGroup.GameSettings, false)
            .SetParent(DisableSkeldDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableMiraHQDevices = BooleanOptionItem.Create(60574, "DisableMiraHQDevices", false, TabGroup.GameSettings, false)
            .SetParent(DisableDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableMiraHQAdmin = BooleanOptionItem.Create(60575, "DisableMiraHQAdmin", false, TabGroup.GameSettings, false)
            .SetParent(DisableMiraHQDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableMiraHQDoorLog = BooleanOptionItem.Create(60576, "DisableMiraHQDoorLog", false, TabGroup.GameSettings, false)
            .SetParent(DisableMiraHQDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisablePolusDevices = BooleanOptionItem.Create(60577, "DisablePolusDevices", false, TabGroup.GameSettings, false)
            .SetParent(DisableDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisablePolusAdmin = BooleanOptionItem.Create(60578, "DisablePolusAdmin", false, TabGroup.GameSettings, false)
            .SetParent(DisablePolusDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisablePolusCamera = BooleanOptionItem.Create(60579, "DisablePolusCamera", false, TabGroup.GameSettings, false)
            .SetParent(DisablePolusDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisablePolusVital = BooleanOptionItem.Create(60580, "DisablePolusVital", false, TabGroup.GameSettings, false)
            .SetParent(DisablePolusDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableAirshipDevices = BooleanOptionItem.Create(60581, "DisableAirshipDevices", false, TabGroup.GameSettings, false)
            .SetParent(DisableDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableAirshipCockpitAdmin = BooleanOptionItem.Create(60582, "DisableAirshipCockpitAdmin", false, TabGroup.GameSettings, false)
            .SetParent(DisableAirshipDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableAirshipRecordsAdmin = BooleanOptionItem.Create(60583, "DisableAirshipRecordsAdmin", false, TabGroup.GameSettings, false)
            .SetParent(DisableAirshipDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableAirshipCamera = BooleanOptionItem.Create(60584, "DisableAirshipCamera", false, TabGroup.GameSettings, false)
            .SetParent(DisableAirshipDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableAirshipVital = BooleanOptionItem.Create(60585, "DisableAirshipVital", false, TabGroup.GameSettings, false)
            .SetParent(DisableAirshipDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableFungleDevices = BooleanOptionItem.Create(60586, "DisableFungleDevices", false, TabGroup.GameSettings, false)
            .SetParent(DisableDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableFungleBinoculars = BooleanOptionItem.Create(60587, "DisableFungleBinoculars", false, TabGroup.GameSettings, false)
            .SetParent(DisableFungleDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableFungleVital = BooleanOptionItem.Create(60588, "DisableFungleVital", false, TabGroup.GameSettings, false)
            .SetParent(DisableFungleDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableDevicesIgnoreConditions = BooleanOptionItem.Create(60589, "IgnoreConditions", false, TabGroup.GameSettings, false)
            .SetParent(DisableDevices);
        //.SetGameMode(CustomGameMode.Standard);
        DisableDevicesIgnoreImpostors = BooleanOptionItem.Create(60590, "IgnoreImpostors", false, TabGroup.GameSettings, false)
            .SetParent(DisableDevicesIgnoreConditions);
        //.SetGameMode(CustomGameMode.Standard);
        DisableDevicesIgnoreNeutrals = BooleanOptionItem.Create(60591, "IgnoreNeutrals", false, TabGroup.GameSettings, false)
            .SetParent(DisableDevicesIgnoreConditions);
        //.SetGameMode(CustomGameMode.Standard);
        DisableDevicesIgnoreCrewmates = BooleanOptionItem.Create(60592, "IgnoreCrewmates", false, TabGroup.GameSettings, false)
            .SetParent(DisableDevicesIgnoreConditions);
        //.SetGameMode(CustomGameMode.Standard);
        DisableDevicesIgnoreAfterAnyoneDied = BooleanOptionItem.Create(60593, "IgnoreAfterAnyoneDied", false, TabGroup.GameSettings, false)
            .SetParent(DisableDevicesIgnoreConditions);
        //.SetGameMode(CustomGameMode.Standard);

        //Disable Short Tasks
        DisableShortTasks = BooleanOptionItem.Create(60594, "DisableShortTasks", false, TabGroup.TaskSettings, false)
            .HideInFFA()
            .SetHeader(true)
            .SetColor(new Color32(239, 89, 175, byte.MaxValue));
        DisableCleanVent = BooleanOptionItem.Create(60595, "DisableCleanVent", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableCalibrateDistributor = BooleanOptionItem.Create(60596, "DisableCalibrateDistributor", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableChartCourse = BooleanOptionItem.Create(60597, "DisableChartCourse", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableStabilizeSteering = BooleanOptionItem.Create(60598, "DisableStabilizeSteering", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableCleanO2Filter = BooleanOptionItem.Create(60599, "DisableCleanO2Filter", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableUnlockManifolds = BooleanOptionItem.Create(60600, "DisableUnlockManifolds", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisablePrimeShields = BooleanOptionItem.Create(60601, "DisablePrimeShields", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableMeasureWeather = BooleanOptionItem.Create(60602, "DisableMeasureWeather", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableBuyBeverage = BooleanOptionItem.Create(60603, "DisableBuyBeverage", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableAssembleArtifact = BooleanOptionItem.Create(60604, "DisableAssembleArtifact", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableSortSamples = BooleanOptionItem.Create(60605, "DisableSortSamples", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableProcessData = BooleanOptionItem.Create(60606, "DisableProcessData", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableRunDiagnostics = BooleanOptionItem.Create(60607, "DisableRunDiagnostics", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableRepairDrill = BooleanOptionItem.Create(60608, "DisableRepairDrill", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableAlignTelescope = BooleanOptionItem.Create(60609, "DisableAlignTelescope", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableRecordTemperature = BooleanOptionItem.Create(60610, "DisableRecordTemperature", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableFillCanisters = BooleanOptionItem.Create(60611, "DisableFillCanisters", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableMonitorTree = BooleanOptionItem.Create(60612, "DisableMonitorTree", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableStoreArtifacts = BooleanOptionItem.Create(60613, "DisableStoreArtifacts", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisablePutAwayPistols = BooleanOptionItem.Create(60614, "DisablePutAwayPistols", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisablePutAwayRifles = BooleanOptionItem.Create(60615, "DisablePutAwayRifles", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableMakeBurger = BooleanOptionItem.Create(60616, "DisableMakeBurger", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableCleanToilet = BooleanOptionItem.Create(60617, "DisableCleanToilet", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableDecontaminate = BooleanOptionItem.Create(60618, "DisableDecontaminate", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableSortRecords = BooleanOptionItem.Create(60619, "DisableSortRecords", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableFixShower = BooleanOptionItem.Create(60620, "DisableFixShower", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisablePickUpTowels = BooleanOptionItem.Create(60621, "DisablePickUpTowels", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisablePolishRuby = BooleanOptionItem.Create(60622, "DisablePolishRuby", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableDressMannequin = BooleanOptionItem.Create(60623, "DisableDressMannequin", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableFixAntenna = BooleanOptionItem.Create(60656, "DisableFixAntenna", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableBuildSandcastle = BooleanOptionItem.Create(60657, "DisableBuildSandcastle", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableCrankGenerator = BooleanOptionItem.Create(60658, "DisableCrankGenerator", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableMonitorMushroom = BooleanOptionItem.Create(60659, "DisableMonitorMushroom", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisablePlayVideoGame = BooleanOptionItem.Create(60660, "DisablePlayVideoGame", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableFindSignal = BooleanOptionItem.Create(60661, "DisableFindSignal", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableThrowFisbee = BooleanOptionItem.Create(60662, "DisableThrowFisbee", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableLiftWeights = BooleanOptionItem.Create(60663, "DisableLiftWeights", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);
        DisableCollectShells = BooleanOptionItem.Create(60664, "DisableCollectShells", false, TabGroup.TaskSettings, false)
            .SetParent(DisableShortTasks);


        //Disable Common Tasks
        DisableCommonTasks = BooleanOptionItem.Create(60627, "DisableCommonTasks", false, TabGroup.TaskSettings, false)
            .HideInFFA()
            .SetColor(new Color32(239, 89, 175, byte.MaxValue));
        DisableSwipeCard = BooleanOptionItem.Create(60628, "DisableSwipeCardTask", false, TabGroup.TaskSettings, false)
            .SetParent(DisableCommonTasks);
        DisableFixWiring = BooleanOptionItem.Create(60629, "DisableFixWiring", false, TabGroup.TaskSettings, false)
            .SetParent(DisableCommonTasks);
        DisableEnterIdCode = BooleanOptionItem.Create(60630, "DisableEnterIdCode", false, TabGroup.TaskSettings, false)
            .SetParent(DisableCommonTasks);
        DisableInsertKeys = BooleanOptionItem.Create(60631, "DisableInsertKeys", false, TabGroup.TaskSettings, false)
            .SetParent(DisableCommonTasks);
        DisableScanBoardingPass = BooleanOptionItem.Create(60632, "DisableScanBoardingPass", false, TabGroup.TaskSettings, false)
            .SetParent(DisableCommonTasks);
        DisableRoastMarshmallow = BooleanOptionItem.Create(60624, "DisableRoastMarshmallow", false, TabGroup.TaskSettings, false)
            .SetParent(DisableCommonTasks);
        DisableCollectSamples = BooleanOptionItem.Create(60625, "DisableCollectSamples", false, TabGroup.TaskSettings, false)
            .SetParent(DisableCommonTasks);
        DisableReplaceParts = BooleanOptionItem.Create(60626, "DisableReplaceParts", false, TabGroup.TaskSettings, false)
            .SetParent(DisableCommonTasks);


        //Disable Long Tasks
        DisableLongTasks = BooleanOptionItem.Create(60640, "DisableLongTasks", false, TabGroup.TaskSettings, false)
            .HideInFFA()
            .SetColor(new Color32(239, 89, 175, byte.MaxValue));
        DisableSubmitScan = BooleanOptionItem.Create(60641, "DisableSubmitScanTask", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisableUnlockSafe = BooleanOptionItem.Create(60642, "DisableUnlockSafeTask", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisableStartReactor = BooleanOptionItem.Create(60643, "DisableStartReactorTask", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisableResetBreaker = BooleanOptionItem.Create(60644, "DisableResetBreakerTask", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisableAlignEngineOutput = BooleanOptionItem.Create(60645, "DisableAlignEngineOutput", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisableInspectSample = BooleanOptionItem.Create(60646, "DisableInspectSample", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisableEmptyChute = BooleanOptionItem.Create(60647, "DisableEmptyChute", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisableClearAsteroids = BooleanOptionItem.Create(60648, "DisableClearAsteroids", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisableWaterPlants = BooleanOptionItem.Create(60649, "DisableWaterPlants", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisableOpenWaterways = BooleanOptionItem.Create(60650, "DisableOpenWaterways", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisableReplaceWaterJug = BooleanOptionItem.Create(60651, "DisableReplaceWaterJug", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisableRebootWifi = BooleanOptionItem.Create(60652, "DisableRebootWifi", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisableDevelopPhotos = BooleanOptionItem.Create(60653, "DisableDevelopPhotos", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisableRewindTapes = BooleanOptionItem.Create(60654, "DisableRewindTapes", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisableStartFans = BooleanOptionItem.Create(60655, "DisableStartFans", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisableCollectVegetables = BooleanOptionItem.Create(60633, "DisableCollectVegetables", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisableMineOres = BooleanOptionItem.Create(60634, "DisableMineOres", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisableExtractFuel = BooleanOptionItem.Create(60635, "DisableExtractFuel", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisableCatchFish = BooleanOptionItem.Create(60636, "DisableCatchFish", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisablePolishGem = BooleanOptionItem.Create(60637, "DisablePolishGem", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisableHelpCritter = BooleanOptionItem.Create(60638, "DisableHelpCritter", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        DisableHoistSupplies = BooleanOptionItem.Create(60639, "DisableHoistSupplies", false, TabGroup.TaskSettings, false)
            .SetParent(DisableLongTasks);
        


        //Disable Divert Power, Weather Nodes and etc. situational Tasks
        DisableOtherTasks = BooleanOptionItem.Create(60665, "DisableOtherTasks", false, TabGroup.TaskSettings, false)
            .HideInFFA()
            .SetColor(new Color32(239, 89, 175, byte.MaxValue));
        DisableUploadData = BooleanOptionItem.Create(60666, "DisableUploadDataTask", false, TabGroup.TaskSettings, false)
            .SetParent(DisableOtherTasks);
        DisableEmptyGarbage = BooleanOptionItem.Create(60667, "DisableEmptyGarbage", false, TabGroup.TaskSettings, false)
            .SetParent(DisableOtherTasks);
        DisableFuelEngines = BooleanOptionItem.Create(60668, "DisableFuelEngines", false, TabGroup.TaskSettings, false)
            .SetParent(DisableOtherTasks);
        DisableDivertPower = BooleanOptionItem.Create(60669, "DisableDivertPower", false, TabGroup.TaskSettings, false)
            .SetParent(DisableOtherTasks);
        DisableActivateWeatherNodes = BooleanOptionItem.Create(60670, "DisableActivateWeatherNodes", false, TabGroup.TaskSettings, false)
            .SetParent(DisableOtherTasks);



        TextOptionItem.Create(10000028, "MenuTitle.Guessers", TabGroup.TaskSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(Color.yellow)
            .SetHeader(true);
        GuesserMode = BooleanOptionItem.Create(60680, "GuesserMode", false, TabGroup.TaskSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(Color.yellow)
            .SetHeader(true);
        CrewmatesCanGuess = BooleanOptionItem.Create(60681, "CrewmatesCanGuess", false, TabGroup.TaskSettings, false)
            .SetParent(GuesserMode);
        ImpostorsCanGuess = BooleanOptionItem.Create(60682, "ImpostorsCanGuess", false, TabGroup.TaskSettings, false)
            .SetParent(GuesserMode);
        NeutralKillersCanGuess = BooleanOptionItem.Create(60683, "NeutralKillersCanGuess", false, TabGroup.TaskSettings, false)
            .SetParent(GuesserMode);
        PassiveNeutralsCanGuess = BooleanOptionItem.Create(60684, "PassiveNeutralsCanGuess", false, TabGroup.TaskSettings, false)
            .SetParent(GuesserMode);
        CanGuessAddons = BooleanOptionItem.Create(60685, "CanGuessAddons", true, TabGroup.TaskSettings, false)
            .SetParent(GuesserMode);
        CrewCanGuessCrew = BooleanOptionItem.Create(60686, "CrewCanGuessCrew", true, TabGroup.TaskSettings, false)
            .SetHidden(true)
            .SetParent(GuesserMode);
        ImpCanGuessImp = BooleanOptionItem.Create(60687, "ImpCanGuessImp", true, TabGroup.TaskSettings, false)
            .SetHidden(true)
            .SetParent(GuesserMode);
        HideGuesserCommands = BooleanOptionItem.Create(60688, "GuesserTryHideMsg", true, TabGroup.TaskSettings, false)
            .SetParent(GuesserMode)
            .SetColor(Color.green);

        ShowOnlyEnabledRolesInGuesserUI = BooleanOptionItem.Create(60689, "ShowOnlyEnabledRolesInGuesserUI", true, TabGroup.TaskSettings, false)
            .SetHeader(true)
            .SetColor(Color.cyan);


        TextOptionItem.Create(10000029, "MenuTitle.GuesserModeRoles", TabGroup.TaskSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(Color.yellow)
            .SetHeader(true);

        SetupAdtRoleOptions(25800, CustomRoles.Onbound, canSetNum: true, tab: TabGroup.TaskSettings);
        ImpCanBeOnbound = BooleanOptionItem.Create(25803, "ImpCanBeOnbound", true, TabGroup.TaskSettings, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Onbound]);
        CrewCanBeOnbound = BooleanOptionItem.Create(25804, "CrewCanBeOnbound", true, TabGroup.TaskSettings, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Onbound]);
        NeutralCanBeOnbound = BooleanOptionItem.Create(25805, "NeutralCanBeOnbound", true, TabGroup.TaskSettings, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Onbound]);



        // Meeting Settings
        TextOptionItem.Create(10000030, "MenuTitle.Meeting", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue));
        // Sync Button
        SyncButtonMode = BooleanOptionItem.Create(60700, "SyncButtonMode", false, TabGroup.GameSettings, false)
            .SetHeader(true)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue))
            .SetGameMode(CustomGameMode.Standard);
        SyncedButtonCount = IntegerOptionItem.Create(60701, "SyncedButtonCount", new(0, 100, 1), 10, TabGroup.GameSettings, false)
            .SetParent(SyncButtonMode)
            .SetValueFormat(OptionFormat.Times)
            .SetGameMode(CustomGameMode.Standard);
        // 全员存活时的会议时间
        AllAliveMeeting = BooleanOptionItem.Create(60710, "AllAliveMeeting", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue));
        AllAliveMeetingTime = FloatOptionItem.Create(60711, "AllAliveMeetingTime", new(1f, 300f, 1f), 10f, TabGroup.GameSettings, false)
            .SetParent(AllAliveMeeting)
            .SetValueFormat(OptionFormat.Seconds);
        // 附加紧急会议
        AdditionalEmergencyCooldown = BooleanOptionItem.Create(60720, "AdditionalEmergencyCooldown", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue));
        AdditionalEmergencyCooldownThreshold = IntegerOptionItem.Create(60721, "AdditionalEmergencyCooldownThreshold", new(1, 15, 1), 1, TabGroup.GameSettings, false)
            .SetParent(AdditionalEmergencyCooldown)
            .SetGameMode(CustomGameMode.Standard)
            .SetValueFormat(OptionFormat.Players);
        AdditionalEmergencyCooldownTime = FloatOptionItem.Create(60722, "AdditionalEmergencyCooldownTime", new(1f, 60f, 1f), 1f, TabGroup.GameSettings, false)
                .SetParent(AdditionalEmergencyCooldown)
            .SetGameMode(CustomGameMode.Standard)
            .SetValueFormat(OptionFormat.Seconds);
        // 投票相关设定
        VoteMode = BooleanOptionItem.Create(60730, "VoteMode", false, TabGroup.GameSettings, false)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue))
            .SetGameMode(CustomGameMode.Standard);
        WhenSkipVote = StringOptionItem.Create(60731, "WhenSkipVote", voteModes[0..3], 0, TabGroup.GameSettings, false)
            .SetParent(VoteMode)
            .SetGameMode(CustomGameMode.Standard);
        WhenSkipVoteIgnoreFirstMeeting = BooleanOptionItem.Create(60732, "WhenSkipVoteIgnoreFirstMeeting", false, TabGroup.GameSettings, false)
            .SetParent(WhenSkipVote)
            .SetGameMode(CustomGameMode.Standard);
        WhenSkipVoteIgnoreNoDeadBody = BooleanOptionItem.Create(60733, "WhenSkipVoteIgnoreNoDeadBody", false, TabGroup.GameSettings, false)
            .SetParent(WhenSkipVote)
            .SetGameMode(CustomGameMode.Standard);
        WhenSkipVoteIgnoreEmergency = BooleanOptionItem.Create(60734, "WhenSkipVoteIgnoreEmergency", false, TabGroup.GameSettings, false)
            .SetParent(WhenSkipVote)
            .SetGameMode(CustomGameMode.Standard);
        WhenNonVote = StringOptionItem.Create(60735, "WhenNonVote", voteModes, 0, TabGroup.GameSettings, false)
            .SetParent(VoteMode)
            .SetGameMode(CustomGameMode.Standard);
        WhenTie = StringOptionItem.Create(60745, "WhenTie", tieModes, 0, TabGroup.GameSettings, false)
            .SetParent(VoteMode)
            .SetGameMode(CustomGameMode.Standard);
        // 其它设定
        TextOptionItem.Create(10000031, "MenuTitle.Other", TabGroup.GameSettings)
            .HideInFFA()
            .SetColor(new Color32(193, 255, 209, byte.MaxValue));
        // 梯子摔死
        LadderDeath = BooleanOptionItem.Create(60760, "LadderDeath", false, TabGroup.GameSettings, false)
            .SetColor(new Color32(193, 255, 209, byte.MaxValue))
            .HideInFFA();
        LadderDeathChance = StringOptionItem.Create(60761, "LadderDeathChance", rates[1..], 0, TabGroup.GameSettings, false)
            .SetParent(LadderDeath);

        // 修正首刀时间
        FixFirstKillCooldown = BooleanOptionItem.Create(60770, "FixFirstKillCooldown", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(193, 255, 209, byte.MaxValue));
        FixKillCooldownValue = FloatOptionItem.Create(60771, "FixKillCooldownValue", new(0f, 180f, 2.5f), 15f, TabGroup.GameSettings, false)
            .SetValueFormat(OptionFormat.Seconds)
            .SetParent(FixFirstKillCooldown);
        // 首刀保护
        ShieldPersonDiedFirst = BooleanOptionItem.Create(60780, "ShieldPersonDiedFirst", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(193, 255, 209, byte.MaxValue));

        // 杀戮闪烁持续
        KillFlashDuration = FloatOptionItem.Create(60790, "KillFlashDuration", new(0.1f, 0.45f, 0.05f), 0.3f, TabGroup.GameSettings, false)
            .SetColor(new Color32(193, 255, 209, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        // 幽灵相关设定
        TextOptionItem.Create(10000032, "MenuTitle.Ghost", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        // 幽灵设置
        GhostIgnoreTasks = BooleanOptionItem.Create(60800, "GhostIgnoreTasks", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        GhostCanSeeOtherRoles = BooleanOptionItem.Create(60810, "GhostCanSeeOtherRoles", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        GhostCanSeeOtherVotes = BooleanOptionItem.Create(60820, "GhostCanSeeOtherVotes", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
             .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        GhostCanSeeDeathReason = BooleanOptionItem.Create(60830, "GhostCanSeeDeathReason", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
           .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        #endregion 


        // End Load Settings
        OptionSaver.Load();
        IsLoaded = true;
    }

    public static void SetupRoleOptions(int id, TabGroup tab, CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard, bool zeroOne = false)
    {
        var spawnOption = StringOptionItem.Create(id, role.ToString(), zeroOne ? ratesZeroOne : ratesToggle, 0, tab, false).SetColor(Utils.GetRoleColor(role))
            .SetHeader(true)
            .SetGameMode(customGameMode) as StringOptionItem;
        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, 15, 1), 1, tab, false)
        .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Players)
            .SetGameMode(customGameMode);

        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }
    private static void SetupLoversRoleOptionsToggle(int id, CustomGameMode customGameMode = CustomGameMode.Standard)
    {
        var role = CustomRoles.Lovers;
        var spawnOption = StringOptionItem.Create(id, role.ToString(), ratesZeroOne, 0, TabGroup.Addons, false).SetColor(Utils.GetRoleColor(role))
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


        var countOption = IntegerOptionItem.Create(id + 1, "NumberOfLovers", new(2, 2, 1), 2, TabGroup.Addons, false)
            .SetParent(spawnOption)
            .SetHidden(true)
            .SetGameMode(customGameMode);

        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }

    public static void SetupAdtRoleOptions(int id, CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard, bool canSetNum = false, TabGroup tab = TabGroup.Addons, bool canSetChance = true)
    {
        var spawnOption = StringOptionItem.Create(id, role.ToString(), ratesZeroOne, 0, tab, false).SetColor(Utils.GetRoleColor(role))
            .SetHeader(true)
            .SetGameMode(customGameMode) as StringOptionItem;

        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, canSetNum ? 10 : 1, 1), 1, tab, false)
        .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Players)
            .SetHidden(!canSetNum)
            .SetGameMode(customGameMode);

        var spawnRateOption = IntegerOptionItem.Create(id + 2, "AdditionRolesSpawnRate", new(0, 100, 5), canSetChance ? 65 : 100, tab, false)
        .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Percent)
            .SetHidden(!canSetChance)
            .SetGameMode(customGameMode) as IntegerOptionItem;

        CustomAdtRoleSpawnRate.Add(role, spawnRateOption);
        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }
    public static void SetupSyndicateRoleOptions(int id, CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard, bool canSetNum = false, TabGroup tab = TabGroup.Addons, bool canSetChance = true)
    {
        var spawnOption = StringOptionItem.Create(id, role.ToString(), ratesZeroOne, 0, tab, false).SetColor(Utils.GetRoleColor(role))
            .SetHeader(true)
            .SetGameMode(customGameMode) as StringOptionItem;

        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, canSetNum ? 5 : 1, 1), 3, tab, false)
        .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Players)
            .SetHidden(!canSetNum)
            .SetGameMode(customGameMode);

        var spawnRateOption = IntegerOptionItem.Create(id + 2, "AdditionRolesSpawnRate", new(0, 100, 5), canSetChance ? 80 : 100, tab, false)
        .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Percent)
            .SetHidden(!canSetChance)
            .SetGameMode(customGameMode) as IntegerOptionItem;

        CustomAdtRoleSpawnRate.Add(role, spawnRateOption);
        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }

    public static void SetupSingleRoleOptions(int id, TabGroup tab, CustomRoles role, int count, CustomGameMode customGameMode = CustomGameMode.Standard, bool zeroOne = false)
    {
        var spawnOption = StringOptionItem.Create(id, role.ToString(), zeroOne ? ratesZeroOne : ratesToggle, 0, tab, false).SetColor(Utils.GetRoleColor(role))
            .SetHeader(true)
            .SetGameMode(customGameMode) as StringOptionItem;
        // 初期値,最大値,最小値が同じで、stepが0のどうやっても変えることができない個数オプション
        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(count, count, count), count, tab, false)
        .SetParent(spawnOption)
            .SetHidden(true)
            .SetGameMode(customGameMode);

        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }
    public class OverrideTasksData
    {
        public static Dictionary<CustomRoles, OverrideTasksData> AllData = new();
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
