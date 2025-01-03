using AmongUs.GameOptions;
using static TOHE.Translator;
using UnityEngine;
using Hazel;
using TOHE.Modules;
using AmongUs.Data;
using System.Text;
using System;

namespace TOHE;

internal static class CopsAndRobbersManager
{
    private const int Id = 67_224_001;

    public static readonly HashSet<byte> cops = [];
    public static readonly HashSet<byte> robbers = [];
    public static readonly Dictionary<byte, Vector2> captured = [];

    private static readonly Dictionary<byte, int> capturedScore = [];
    private static readonly Dictionary<byte, long> capturedTime = [];
    private static readonly Dictionary<byte, long> releaseTime = [];
    private static readonly Dictionary<byte, int> timesCaptured = [];
    private static readonly Dictionary<byte, int> saved = [];
    private static readonly Dictionary<byte, float> defaultSpeed = [];

    private static readonly Dictionary<CopAbility, int> copAbilityChances = [];
    private static readonly Dictionary<RobberAbility, int> robberAbilityChances = [];
    private static readonly Dictionary<byte, CopAbility?> RemoveCopAbility = [];
    private static readonly Dictionary<Vector2, CopAbility?> trapLocation = [];
    private static readonly HashSet<Vector2> removeTrap = [];
    private static readonly Dictionary<byte, long> spikeTrigger = [];
    private static readonly Dictionary<byte, long> flashTrigger = [];
    private static readonly Dictionary<byte, byte> k9 = [];
    private static readonly Dictionary<byte, bool> scopeAltered = [];
    private static int killDistance;
    public static Dictionary<CustomRoles, List<OptionItem>> roleSettings = [];

    private static readonly Dictionary<byte, RobberAbility?> RemoveRobberAbility = [];
    private static readonly HashSet<byte> energyShieldActive = [];
    private static readonly HashSet<byte> smokeBombActive = [];
    private static readonly Dictionary<byte, long> smokeBombTriggered = [];
    private static readonly HashSet<byte> adrenalineRushActive = [];
    private static readonly Dictionary<byte, byte> radar = [];
    private static readonly HashSet<byte> disguise = [];

    private static int numCops;
    private static int numCaptures;
    private static int numRobbers;
    public static int RoundTime;

    public static OptionItem CandR_NumCops;
    private static OptionItem CandR_CaptureCooldown;
    private static OptionItem CandR_TeleportCaptureToRandomLoc;
    private static OptionItem CandR_CopAbilityTriggerChance;
    private static OptionItem CandR_CopAbilityCooldown;
    private static OptionItem CandR_CopAbilityDuration;
    private static OptionItem CandR_HotPursuitChance;
    private static OptionItem CandR_HotPursuitSpeed;
    private static OptionItem CandR_SpikeStripChance;
    private static OptionItem CandR_SpikeStripRadius;
    private static OptionItem CandR_SpikeStripDuration;
    private static OptionItem CandR_FlashBangChance;
    private static OptionItem CandR_FlashBangRadius;
    private static OptionItem CandR_FlashBangDuration;
    private static OptionItem CandR_K9Chance;
    private static OptionItem CandR_ScopeChance;
    private static OptionItem CandR_ScopeIncrease;

 

    private static OptionItem CandR_NotifyRobbersWhenCaptured;
    private static OptionItem CandR_RobberVentDuration;
    private static OptionItem CandR_RobberVentCooldown;
    private static OptionItem CandR_RobberAbilityDuration;
    private static OptionItem CandR_RobberAbilityTriggerChance;
    private static OptionItem CandR_AdrenalineRushChance;
    private static OptionItem CandR_AdrenalineRushSpeed;
    private static OptionItem CandR_EnergyShieldChance;
    private static OptionItem CandR_SmokeBombChance;
    private static OptionItem CandR_SmokeBombDuration;
    private static OptionItem CandR_DisguiseChance;
    private static OptionItem CandR_RadarChance;
    private static OptionItem CandR_ReleaseCooldownForCaptured;
    private static OptionItem CandR_ReleaseCooldownForRobber;
    private static OptionItem CandR_GameTime;
    public static OptionItem CandR_ShowChatInGame;


    public static void SetupCustomOption()
    {
        CandR_GameTime = IntegerOptionItem.Create(Id, "CandR_GameTime", new(30, 600, 10), 300, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetValueFormat(OptionFormat.Seconds)
            .SetHeader(true);

        CandR_ShowChatInGame = CandR_NotifyRobbersWhenCaptured = BooleanOptionItem.Create(Id + 1, "C&R_ShowChatInGame", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR);

        /*********** Cops ***********/
        TextOptionItem.Create(Id + 2, "Cop", TabGroup.ModSettings)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue));

        CandR_NumCops = IntegerOptionItem.Create(Id + 3, "C&R_NumCops", new(1, 5, 1), 2, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue));
        CandR_CaptureCooldown = FloatOptionItem.Create(Id + 4, "C&R_CaptureCooldown", new(5f, 60f, 2.5f), 15f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        CandR_TeleportCaptureToRandomLoc = BooleanOptionItem.Create(Id + 5, "C&R_TeleportCaptureToRandomLoc", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue));

        CandR_CopAbilityTriggerChance = IntegerOptionItem.Create(Id + 6, "C&R_CopAbilityTriggerChance", new(0, 100, 5), 50, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Percent);

        CandR_CopAbilityDuration = IntegerOptionItem.Create(Id + 7, "C&R_CopAbilityDuration", new(1, 10, 1), 10, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds)
            .SetParent(CandR_CopAbilityTriggerChance);
        CandR_CopAbilityCooldown = FloatOptionItem.Create(Id + 8, "C&R_CopAbilityCooldown", new(10f, 60f, 2.5f), 20f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds)
            .SetParent(CandR_CopAbilityTriggerChance);


        CandR_HotPursuitChance = IntegerOptionItem.Create(Id + 9, "C&R_HotPursuitChance", new(0, 100, 5), 35, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Percent)
            .SetParent(CandR_CopAbilityTriggerChance);
        CandR_HotPursuitSpeed = FloatOptionItem.Create(Id + 10, "C&R_HotPursuitSpeed", new(0f, 2f, 0.25f), 1f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Multiplier)
            .SetParent(CandR_HotPursuitChance);


        CandR_SpikeStripChance = IntegerOptionItem.Create(Id + 11, "C&R_SpikeStripChance", new(0, 100, 5), 20, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Percent)
            .SetParent(CandR_CopAbilityTriggerChance);
        CandR_SpikeStripRadius = FloatOptionItem.Create(Id + 12, "C&R_SpikeStripRadius", new(0.5f, 2f, 0.5f), 1f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Multiplier)
            .SetParent(CandR_SpikeStripChance);
        CandR_SpikeStripDuration = IntegerOptionItem.Create(Id + 13, "C&R_SpikeStripDuration", new(1, 10, 1), 5, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds)
            .SetParent(CandR_SpikeStripChance);

        CandR_FlashBangChance = IntegerOptionItem.Create(Id + 14, "C&R_FlashBangChance", new(0, 100, 5), 15, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Percent)
            .SetParent(CandR_CopAbilityTriggerChance);
        CandR_FlashBangRadius = FloatOptionItem.Create(Id + 15, "C&R_FlashBangRadius", new(0.5f, 2f, 0.5f), 1f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Multiplier)
            .SetParent(CandR_FlashBangChance);
        CandR_FlashBangDuration = IntegerOptionItem.Create(Id + 16, "C&R_FlashBangDuration", new(1, 10, 1), 5, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds)
            .SetParent(CandR_FlashBangChance);

        CandR_ScopeChance = IntegerOptionItem.Create(Id + 17, "C&R_ScopeChance", new(0, 100, 5), 10, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Percent)
            .SetParent(CandR_CopAbilityTriggerChance);
        CandR_ScopeIncrease = IntegerOptionItem.Create(Id + 18, "C&R_ScopeIncrease", new(1, 5, 1), 1, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Multiplier)
            .SetParent(CandR_ScopeChance);

        CandR_K9Chance = IntegerOptionItem.Create(Id + 19, "C&R_K9Chance", new(0, 100, 5), 20, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Percent)
            .SetParent(CandR_CopAbilityTriggerChance);

        roleSettings[CustomRoles.Cop] = [CandR_NumCops, CandR_CaptureCooldown, CandR_CopAbilityTriggerChance];

        /*********** Robbers ***********/

        TextOptionItem.Create(Id + 20, "Robber", TabGroup.ModSettings)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue));

        CandR_NotifyRobbersWhenCaptured = BooleanOptionItem.Create(Id + 21, "C&R_NotifyRobbersWhenCaptured", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue));

        CandR_RobberVentDuration = FloatOptionItem.Create(Id + 22, "C&R_RobberVentDuration", new(1f, 20f, 0.5f), 10f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        CandR_RobberVentCooldown = FloatOptionItem.Create(Id + 23, "C&R_RobberVentCooldown", new(10f, 60f, 2.5f), 20f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        CandR_ReleaseCooldownForCaptured = IntegerOptionItem.Create(Id + 24, "C&R_ReleaseCooldownForCaptured", new(5, 20, 1), 5, TabGroup.ModSettings, false)
            .SetGameMode (CustomGameMode.CandR)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        CandR_ReleaseCooldownForRobber = IntegerOptionItem.Create(Id + 25, "C&R_ReleaseCooldownForRobber", new(5, 20, 1), 15, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        CandR_RobberAbilityTriggerChance = IntegerOptionItem.Create(Id + 26, "C&R_RobberAbilityTriggerChance", new(0, 100, 5), 50, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Percent);
        CandR_RobberAbilityDuration = IntegerOptionItem.Create(Id + 27, "C&R_RobberAbilityDuration", new(1, 10, 1), 10, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds)
            .SetParent(CandR_RobberAbilityTriggerChance);

        CandR_AdrenalineRushChance = IntegerOptionItem.Create(Id + 28, "C&R_AdrenalineRushChance", new(0, 100, 5), 30, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Percent)
            .SetParent(CandR_RobberAbilityTriggerChance);
        CandR_AdrenalineRushSpeed = FloatOptionItem.Create(Id + 29, "C&R_AdrenalineRushSpeed", new(0f, 2f, 0.25f), 1f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Multiplier)
            .SetParent(CandR_AdrenalineRushChance);

        CandR_EnergyShieldChance = IntegerOptionItem.Create(Id + 30, "C&R_EnergyShieldChance", new(0, 100, 5), 25, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Percent)
            .SetParent(CandR_RobberAbilityTriggerChance);
        
        CandR_SmokeBombChance = IntegerOptionItem.Create(Id + 31, "C&R_SmokeBombChance", new(0, 100, 5), 20, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Percent)
            .SetParent(CandR_RobberAbilityTriggerChance);
        CandR_SmokeBombDuration = IntegerOptionItem.Create(Id + 32, "C&R_SmokeBombDuration", new(1, 10, 1), 5, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds)
            .SetParent(CandR_SmokeBombChance);

        CandR_DisguiseChance = IntegerOptionItem.Create(Id + 33, "C&R_DisguiseChance", new(0, 100, 5), 10, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Percent)
            .SetParent(CandR_RobberAbilityTriggerChance);

        CandR_RadarChance = IntegerOptionItem.Create(Id + 34, "C&R_RadarChance", new(0, 100, 5), 15, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Percent)
            .SetParent(CandR_RobberAbilityTriggerChance);

        roleSettings[CustomRoles.Robber] = [CandR_NotifyRobbersWhenCaptured, CandR_RobberVentDuration, CandR_RobberVentCooldown, CandR_ReleaseCooldownForCaptured, CandR_ReleaseCooldownForRobber, CandR_RobberAbilityTriggerChance];
    }

    public enum RoleType
    {
        Cop,
        Robber,
        Captured
    }
    private enum RobberAbility
    {
        AdrenalineRush, // Increases speed for a set amount of time.
        EnergyShield, // Protects from the next capture attempt.
        SmokeBomb, // Blinds the Cop when captured.
        Disguise, // Wear the same costume as a Cop for confusion.
        Radar // Points towards closest captured player, if no captured player, it points towards a cop (colored arrows indicating cops or captured)
    }
    private enum CopAbility
    {
        HotPursuit, // Speed boost for a set amount of time.
        SpikeStrip, // Sets a trap that slows down the robber.
        FlashBang, // Sets a trap that blinds the robber temporarily.
        K9, // Points to the closest robber.
        Scope // Increases the capture range.
    }

    public static bool HasTasks(CustomRoles role)
    {
        return role switch
        {
            CustomRoles.Cop => false,
            CustomRoles.Robber => true,
            _ => false,
        };
    }

    private static void CumulativeAbilityChances()
    {
        Dictionary<CopAbility, OptionItem> copOptionItems = new()
        {
            {CopAbility.HotPursuit, CandR_HotPursuitChance },
            {CopAbility.SpikeStrip, CandR_SpikeStripChance },
            {CopAbility.FlashBang, CandR_FlashBangChance },
            {CopAbility.K9, CandR_K9Chance },
            {CopAbility.Scope, CandR_ScopeChance },
        };
        copAbilityChances.Clear();
        var cumulativeChance = 0;
        foreach (var optionItem in copOptionItems)
        {
            if (optionItem.Value.GetInt() == 0) continue;
            cumulativeChance += optionItem.Value.GetInt();
            copAbilityChances.Add(optionItem.Key, cumulativeChance);
        }

        Dictionary<RobberAbility, OptionItem> robberOptionItems = new()
        {
            {RobberAbility.AdrenalineRush, CandR_AdrenalineRushChance },
            {RobberAbility.EnergyShield, CandR_EnergyShieldChance },
            {RobberAbility.SmokeBomb, CandR_SmokeBombChance },
            {RobberAbility.Radar, CandR_RadarChance },
            {RobberAbility.Disguise, CandR_DisguiseChance },
        };
        robberAbilityChances.Clear();
        cumulativeChance = 0;
        foreach (var optionItem in robberOptionItems)
        {
            if (optionItem.Value.GetInt() == 0) continue;
            cumulativeChance += optionItem.Value.GetInt();
            robberAbilityChances.Add(optionItem.Key, cumulativeChance);
        }
    }

    public static void Init()
    {
        if (Options.CurrentGameMode != CustomGameMode.CandR) return;

        cops.Clear();
        robbers.Clear();
        captured.Clear();
        numCaptures = 0;
        numRobbers = 0;
        capturedScore.Clear();
        timesCaptured.Clear();
        saved.Clear();
        numCops = CandR_NumCops.GetInt();
        defaultSpeed.Clear();
        RemoveCopAbility.Clear();
        RemoveRobberAbility.Clear();
        trapLocation.Clear();
        removeTrap.Clear();
        spikeTrigger.Clear();
        flashTrigger.Clear();
        k9.Clear();
        scopeAltered.Clear();
        energyShieldActive.Clear();
        smokeBombActive.Clear();
        smokeBombTriggered.Clear();
        adrenalineRushActive.Clear();
        radar.Clear();
        disguise.Clear();
        killDistance = Main.RealOptionsData.GetInt(Int32OptionNames.KillDistance);
        CumulativeAbilityChances();
        capturedTime.Clear();
        releaseTime.Clear();
    }
    public static void SetData()
    {
        if (Options.CurrentGameMode != CustomGameMode.CandR) return;

        RoundTime = CandR_GameTime.GetInt() + 8;
        var now = Utils.GetTimeStamp() + 8;
        foreach (byte robber in robbers)
        {
            releaseTime[robber] = now;
        }
    }
    public static Dictionary<byte, CustomRoles> SetRoles()
    {
        Dictionary<byte, CustomRoles> finalRoles = [];
        var random = IRandom.Instance;
        List<PlayerControl> AllPlayers = Main.AllPlayerControls.Shuffle(random).ToList();

        if (Main.EnableGM.Value)
        {
            finalRoles[PlayerControl.LocalPlayer.PlayerId] = CustomRoles.GM;
            AllPlayers.Remove(PlayerControl.LocalPlayer);
        }

        int optImpNum = numCops;
        foreach (PlayerControl pc in AllPlayers)
        {
            if (pc == null) continue;
            if (optImpNum > 0)
            {
                finalRoles[pc.PlayerId] = CustomRoles.Cop;
                RoleType.Cop.Add(pc.PlayerId);
                optImpNum--;
            }
            else
            {
                finalRoles[pc.PlayerId] = CustomRoles.Robber;
                RoleType.Robber.Add(pc.PlayerId);
            }
            Logger.Msg($"set role for {pc.PlayerId}: {finalRoles[pc.PlayerId]}", "SetRoles");
        }
        SendCandRData(5);
        SendCandRData(6);
        return finalRoles;
    }
    private static void Add(this RoleType role, byte playerId)
    {
        defaultSpeed[playerId] = Main.AllPlayerSpeed[playerId];
        role.SetCostume(playerId: playerId);

        switch (role)
        {
            case RoleType.Cop:
                cops.Add(playerId);
                capturedScore[playerId] = 0;
                Main.UnShapeShifter.Add(playerId);
                return;

            case RoleType.Robber:
                robbers.Add(playerId);
                timesCaptured[playerId] = 0;
                saved[playerId] = 0;
                numRobbers++;
                return;
        }
    }
    private static void AddCaptured(this PlayerControl robber, Vector2 capturedLocation)
    {
        RoleType.Captured.SetCostume(playerId: robber.PlayerId);
        if (CandR_TeleportCaptureToRandomLoc.GetBool())
        {
            var rand = IRandom.Instance;
            if (rand.Next(100) > 50)
            {
                RandomSpawn.SpawnMap spawnMap = Utils.GetActiveMapName() switch
                {
                    MapNames.Skeld => new RandomSpawn.SkeldSpawnMap(),
                    MapNames.Mira => new RandomSpawn.MiraHQSpawnMap(),
                    MapNames.Polus => new RandomSpawn.PolusSpawnMap(),
                    MapNames.Dleks => new RandomSpawn.DleksSpawnMap(),
                    MapNames.Fungle => new RandomSpawn.FungleSpawnMap(),
                    MapNames.Airship => new RandomSpawn.AirshipSpawnMap(),
                    _ => null,
                };
                if (spawnMap != null)
                {
                    capturedLocation = spawnMap.GetLocation();
                    
                }
            }
            else
            {
                var vent = ShipStatus.Instance.AllVents.RandomElement();
                capturedLocation = new Vector2(vent.transform.position.x, vent.transform.position.y + 0.3636f);
            }
            robber.RpcTeleport(capturedLocation);

        }
        captured[robber.PlayerId] = capturedLocation;
        Main.AllPlayerSpeed[robber.PlayerId] = Main.MinSpeed;
        robber.RpcSetVentInteraction();
        robber?.MarkDirtySettings();
    }
    private static void RemoveCaptured(this PlayerControl rescued)
    {
        if (rescued == null) return;
        captured.Remove(rescued.PlayerId);
        if (disguise.Contains(rescued.PlayerId)) RoleType.Cop.SetCostume(playerId: rescued.PlayerId);
        else RoleType.Robber.SetCostume(playerId: rescued.PlayerId); //for robber
        Main.AllPlayerSpeed[rescued.PlayerId] = defaultSpeed[rescued.PlayerId];
        if (adrenalineRushActive.Contains(rescued.PlayerId)) Main.AllPlayerSpeed[rescued.PlayerId] += CandR_AdrenalineRushSpeed.GetFloat();
        rescued.RpcSetVentInteraction();
        rescued?.MarkDirtySettings();
    }

    public static void SetCostume(this RoleType opMode, byte playerId)
    {
        if (playerId == byte.MaxValue) return;
        PlayerControl player = Utils.GetPlayerById(playerId);
        if (player == null) return;
        var playerOutfit = new NetworkedPlayerInfo.PlayerOutfit();
        switch (opMode)
        {
            case RoleType.Cop:

                playerOutfit.Set(player.GetRealName(isMeeting:true),
                    1, //blue
                    "hat_police", //hat
                    "skin_Police", //skin 
                    "visor_pk01_Security1Visor", //visor
                    player.CurrentOutfit.PetId,
                    player.CurrentOutfit.NamePlateId);
                break;
            case RoleType.Robber:
                playerOutfit.Set(player.GetRealName(isMeeting: true),
                    6, //black
                    "hat_pk04_Vagabond", //hat
                    "skin_None", //skin 
                    "visor_None", //visor
                    player.CurrentOutfit.PetId,
                    player.CurrentOutfit.NamePlateId);
                break;
            case RoleType.Captured:
                playerOutfit.Set(player.GetRealName(isMeeting: true),
                    5, //yellow
                    "hat_tombstone", //hat
                    "skin_prisoner", //skin 
                    "visor_pk01_DumStickerVisor", //visor
                    player.CurrentOutfit.PetId,
                    player.CurrentOutfit.NamePlateId);
                break;
        }
        player.SetNewOutfit(newOutfit: playerOutfit, setName: false, setNamePlate: false);
        Main.OvverideOutfit[player.PlayerId] = (playerOutfit, Main.PlayerStates[player.PlayerId].NormalOutfit.PlayerName);
    }
    public static void CaptureCooldown(PlayerControl cop) =>
    Main.AllPlayerKillCooldown[cop.PlayerId] = CandR_CaptureCooldown.GetFloat();

    private static void SendCandRData(byte op, byte playerId = byte.MaxValue, int capturedCount = 0)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncCandRData, SendOption.Reliable, -1);
        writer.Write(op);
        switch (op)
        {
            case 0:
                writer.Write(playerId);
                writer.Write(k9[playerId]);
                break;
            case 1:
                writer.Write(playerId);
                break;
            case 2:
                writer.Write(playerId);
                writer.Write(radar[playerId]);
                break;
            case 3:
                writer.Write(playerId);
                break;
            case 4:
                writer.Write(capturedCount);
                break;
            case 5:
                writer.Write(numRobbers);
                break;
            case 6:
                writer.Write(cops.Count);
                foreach (var pid in cops) writer.Write(pid);
                writer.Write(robbers.Count);
                foreach (var pid in robbers) writer.Write(pid);
                break;
            case 7:
                writer.Write(playerId);
                writer.Write(saved[playerId]);
                break;
            case 8:
                writer.Write(playerId);
                writer.Write(capturedScore[playerId]);
                break;
        }

        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveCandRData(MessageReader reader)
    {
        byte op = reader.ReadByte();
        switch (op)
        {
            case 0:
                byte copId = reader.ReadByte();
                byte k9Id = reader.ReadByte();
                k9[copId] = k9Id;
                break;
            case 1:
                byte removeCopId = reader.ReadByte();
                k9.Remove(removeCopId);
                break;
            case 2:
                byte robberId = reader.ReadByte();
                byte radarId = reader.ReadByte();
                radar[robberId] = radarId;
                break;
            case 3:
                byte removeRobberId = reader.ReadByte();
                radar.Remove(removeRobberId);
                break;
            case 4:
                numCaptures = reader.ReadInt32();
                break;
            case 5:
                numRobbers = reader.ReadInt32();
                break;
            case 6:
                cops.Clear();
                int copLength = reader.ReadInt32();
                for (int i = 0; i < copLength; i++) cops.Add(reader.ReadByte());
                robbers.Clear();
                int robberLength = reader.ReadInt32();
                for (int i = 0; i < robberLength; i++) robbers.Add(reader.ReadByte());
                break;
            case 7:
                byte robber = reader.ReadByte();
                saved[robber] = reader.ReadInt32();
                break;
            case 8:
                byte cop = reader.ReadByte();
                capturedScore[cop] = reader.ReadInt32();
                break;
        }      
    }

    private static CopAbility? RandomCopAbility()
    {
        var random = IRandom.Instance;
        var shouldTrigger = random.Next(100);
        if (copAbilityChances.Count == 0 || shouldTrigger >= CandR_CopAbilityTriggerChance.GetInt()) return null;

        int randomChance = random.Next(copAbilityChances.Values.Last());

        foreach (var ability in copAbilityChances)
        {
            if (randomChance < ability.Value)
            {
                return ability.Key;
            }
        }
        return null; // shouldn't happen
    }
    private static void DeactivateCopAbility(this PlayerControl cop, CopAbility? ability, Vector2 loc)
    {
        if (ability == null || cop == null) return;
        switch (ability)
        {
            case CopAbility.HotPursuit:
                Main.AllPlayerSpeed[cop.PlayerId] -= CandR_HotPursuitSpeed.GetFloat();
                cop.MarkDirtySettings();
                break;
            case CopAbility.SpikeStrip: //it might also be removed in fixed update when trap triggered.
            case CopAbility.FlashBang: //it might also be removed in fixed update when trap triggered.
                trapLocation.Remove(loc);
                break;
            case CopAbility.K9:
                byte targetId = k9[cop.PlayerId];
                Logger.Info($"Removed k9 for {cop.PlayerId}", "Remove k9");
                k9.Remove(cop.PlayerId);
                SendCandRData(1, cop.PlayerId);
                TargetArrow.Remove(cop.PlayerId, targetId);
                break;
            case CopAbility.Scope:
                scopeAltered.Remove(cop.PlayerId);
                cop.MarkDirtySettings();
                break;
            default:
                return;
        }
        RemoveCopAbility.Remove(cop.PlayerId);
    }
    private static void ActivateCopAbility(this PlayerControl cop, CopAbility? ability)
    {
        Vector2 loc = cop.GetCustomPosition();
        switch (ability)
        {
            case CopAbility.HotPursuit: //increase speed of cop
                Main.AllPlayerSpeed[cop.PlayerId] += CandR_HotPursuitSpeed.GetFloat();
                cop?.MarkDirtySettings();
                break;
            case CopAbility.SpikeStrip: //add location of spike trap, when triggered, player speed will reduce
            case CopAbility.FlashBang: //add location of flash trap, when triggered, player vision will reduce
                if (trapLocation.ContainsKey(loc))
                {
                    Logger.Info("Location was already trapped", "SpikeStrip activate");
                    return;
                }
                trapLocation.Add(loc, ability);
                break;

            case CopAbility.K9:
                if (k9.ContainsKey(cop.PlayerId))
                    return;
                k9.Add(cop.PlayerId, byte.MaxValue);
                SendCandRData(0, cop.PlayerId);
                Logger.Info($"Added {cop.PlayerId} for k9", "Ability activated");
                break;
            case CopAbility.Scope:
                if (scopeAltered.ContainsKey(cop.PlayerId))
                    return;
                scopeAltered[cop.PlayerId] = false;
                cop.MarkDirtySettings();
                break;
            default:
                return;
        }
        RemoveCopAbility[cop.PlayerId] = ability;
        var notifyMsg = GetString("C&R_AbilityActivated");
        cop.Notify(string.Format(notifyMsg.Replace("{Ability.Name}", "{0}"), GetString($"CopAbility.{ability}")), CandR_CopAbilityDuration.GetFloat());
        _ = new LateTask(() =>
        {
            if (!GameStates.IsInGame || !RemoveCopAbility.ContainsKey(cop.PlayerId)) return;
            cop.DeactivateCopAbility(ability: ability, loc: loc);
        }, CandR_CopAbilityDuration.GetInt(), "Remove cop ability");
    }
    public static void UnShapeShiftButton(PlayerControl shapeshifter)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (shapeshifter == null) return;
        if (!shapeshifter.Is(CustomRoles.Cop)) return;


        CopAbility? ability = RandomCopAbility();
        if (ability == null) return;
        shapeshifter.ActivateCopAbility(ability);
        Logger.Info($"Activating {ability} for id: {shapeshifter.PlayerId}", "C&R OnCheckShapeshift");
        return;
    }
    public static string GetClosestArrow(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (isForMeeting || seer.PlayerId != target.PlayerId) return string.Empty;
        if (k9.ContainsKey(seer.PlayerId))
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cop), TargetArrow.GetArrows(seer));
        else if (radar.ContainsKey(seer.PlayerId))
        {
            bool isCaptured = captured.ContainsKey(radar[seer.PlayerId]);
            return Utils.ColorString(isCaptured? Utils.GetRoleColor(CustomRoles.Robber) : Utils.GetRoleColor(CustomRoles.Cop), TargetArrow.GetArrows(seer));
        }
        return string.Empty;
    }
    
    public static void OnCopAttack(PlayerControl cop, PlayerControl robber)
    {
        if (cop == null || robber == null || Options.CurrentGameMode != CustomGameMode.CandR) return;
        if (!cop.Is(CustomRoles.Cop) || !robber.Is(CustomRoles.Robber)) return;

        if (captured.ContainsKey(robber.PlayerId))
        {
            cop.Notify("C&R_AlreadyCaptured");
            return;
        }
        if (robber.inVent)
        {
            Logger.Info($"Robber, playerID {robber.PlayerId}, is in a vent, capture blocked", "C&R");
            return;
        }

        if (!energyShieldActive.Contains(robber.PlayerId))
        {    
            Vector2 robberLocation = robber.GetCustomPosition();
            robber.AddCaptured(robberLocation);
            numCaptures = captured.Count;
            foreach (var pid in cops)
            {
                var player = Utils.GetPlayerById(pid);
                if (player != null)
                {
                    Utils.NotifyRoles(SpecifySeer: player);
                }
            }

            if (CandR_NotifyRobbersWhenCaptured.GetBool())
            {
                foreach (byte pid in robbers)
                {
                    if (pid == byte.MaxValue) continue;
                    PlayerControl pc = Utils.GetPlayerById(pid);
                    pc?.KillFlash();
                }
            }

            if (!capturedScore.ContainsKey(cop.PlayerId)) capturedScore[cop.PlayerId] = 0;
            capturedScore[cop.PlayerId]++;

            capturedTime[robber.PlayerId] = Utils.GetTimeStamp();

            if (!timesCaptured.ContainsKey(robber.PlayerId)) timesCaptured[robber.PlayerId] = 0;
            timesCaptured[robber.PlayerId]++;
            SendCandRData(4, capturedCount: numCaptures);
            SendCandRData(8, playerId: cop.PlayerId);
        }
        else
        {
            Logger.Info($"capture canceled, Energy shield active {robber.PlayerId}", "Capture canceled");
            cop.Notify($"Could not capture, {GetString("RobberAbility.EnergyShield")} active");
        }
        if (smokeBombActive.Contains(robber.PlayerId))
        {
            smokeBombTriggered[cop.PlayerId] = Utils.GetTimeStamp();
            cop.MarkDirtySettings();
            smokeBombActive.Remove(robber.PlayerId);
            Logger.Info($"smoke bomb triggered for {cop.PlayerId}", "smoke trigger");
        }
        CaptureCooldown(cop);
        cop.ResetKillCooldown();
        cop.SetKillCooldown();
    }
    private static RobberAbility? RandomRobberAbility()
    {
        var random = IRandom.Instance;
        var shouldTrigger = random.Next(100);
        if (!robberAbilityChances.Any() || shouldTrigger >= CandR_RobberAbilityTriggerChance.GetInt()) return null;
        
        int randomChance = random.Next(copAbilityChances.Values.Last());
        foreach (var ability in robberAbilityChances)
        {
            if (randomChance < ability.Value)
            {
                return ability.Key;
            }
        }
        return null; // shouldn't happen
    }
    private static void DeactivateRobberAbility(this PlayerControl robber, RobberAbility? ability)
    {
        if (ability == null || robber == null) return;
        switch (ability)
        {
            case RobberAbility.AdrenalineRush:
                adrenalineRushActive.Remove(robber.PlayerId);
                if (!spikeTrigger.ContainsKey(robber.PlayerId))
                {
                    Main.AllPlayerSpeed[robber.PlayerId] -= CandR_AdrenalineRushSpeed.GetFloat();
                    robber.MarkDirtySettings();
                }
                break;
            case RobberAbility.EnergyShield:
                energyShieldActive.Remove(robber.PlayerId);
                break;
            case RobberAbility.SmokeBomb:
                smokeBombActive.Remove(robber.PlayerId);
                break;
            case RobberAbility.Disguise:
                disguise.Remove(robber.PlayerId);
                if (captured.ContainsKey(robber.PlayerId))
                {
                    Logger.Info($"disguise finished for captured player {robber.PlayerId}", "disguise finish");
                    break;
                }
                RoleType.Robber.SetCostume(robber.PlayerId);
                Logger.Info($"reverting costume because disguise finished for player: {robber.PlayerId}", "disguise finish");
                break;
            case RobberAbility.Radar:
                byte targetId = radar[robber.PlayerId];
                Logger.Info($"Removed k9 for {robber.PlayerId}", "Remove k9");
                radar.Remove(robber.PlayerId);
                SendCandRData(3, robber.PlayerId);
                TargetArrow.Remove(robber.PlayerId, targetId);
                break;
            default:
                return;
        }
        RemoveRobberAbility.Remove(robber.PlayerId);
    }
    private static void ActivateRobberAbility(this PlayerControl robber, RobberAbility? ability)
    {
        if (ability == null || robber == null) return;
        switch (ability)
        {
            case RobberAbility.AdrenalineRush:
                adrenalineRushActive.Add(robber.PlayerId);
                Main.AllPlayerSpeed[robber.PlayerId] += CandR_AdrenalineRushSpeed.GetFloat();
                robber.MarkDirtySettings();
                break;
            case RobberAbility.EnergyShield:
                energyShieldActive.Add(robber.PlayerId);
                break;
            case RobberAbility.SmokeBomb:
                smokeBombActive.Add(robber.PlayerId);
                break;
            case RobberAbility.Disguise:
                RoleType.Cop.SetCostume(robber.PlayerId);
                disguise.Add(robber.PlayerId);
                break;
            case RobberAbility.Radar:
                if (radar.ContainsKey(robber.PlayerId))
                    return;
                radar.Add(robber.PlayerId, byte.MaxValue);
                SendCandRData(2, robber.PlayerId);
                Logger.Info($"Added {robber.PlayerId} for radar", "radar activated");
                break;
            default:
                return;
        }
        RemoveRobberAbility[robber.PlayerId] = ability;
        var notifyMsg = GetString("C&R_AbilityActivated");
        robber.Notify(string.Format(notifyMsg.Replace("{Ability.Name}", "{0}"), GetString($"RobberAbility.{ability}")), CandR_RobberAbilityDuration.GetFloat());
        _ = new LateTask(() =>
        {
            if (!GameStates.IsInGame || !RemoveRobberAbility.ContainsKey(robber.PlayerId)) return;
            robber.DeactivateRobberAbility(ability: ability);
        }, CandR_RobberAbilityDuration.GetInt(), "Remove robber ability");
    }
    public static void OnRobberExitVent(PlayerControl pc)
    {
        if (pc == null) return; 
        if (!pc.Is(CustomRoles.Robber)) return;
        if (captured.ContainsKey(pc.PlayerId))
        {
            Logger.Info($"Player {pc.PlayerId} was captured", "robber activity cancel");
            return;
        }
        if (spikeTrigger.ContainsKey(pc.PlayerId))
        {
            Logger.Info($"Ability canceled for {pc.PlayerId}, robber triggered spike strip", "robber ability cancel");
            return;
        }
        var ability = RandomRobberAbility();
        if (ability == null) return;

        float delay = Utils.GetActiveMapId() != 5 ? 0.1f : 0.4f;
        _ = new LateTask(() =>
        {
            ActivateRobberAbility(pc, ability);
        }, delay, "Robber On Exit Vent");
    }

    public static void ApplyGameOptions(ref IGameOptions opt, PlayerControl player)
    {
        if (player.Is(CustomRoles.Cop) && CandR_CopAbilityTriggerChance.GetFloat() > 0f)
        {
            AURoleOptions.ShapeshifterCooldown = CandR_CopAbilityCooldown.GetFloat();
            AURoleOptions.ShapeshifterDuration = CandR_CopAbilityDuration.GetFloat();
        }

        if (player.Is(CustomRoles.Robber))
        {
            AURoleOptions.EngineerCooldown = CandR_RobberVentCooldown.GetFloat();
            AURoleOptions.EngineerInVentMaxTime = CandR_RobberVentDuration.GetFloat();
        }

        if (scopeAltered.TryGetValue(player.PlayerId, out bool isAltered))
        {
            if (!isAltered)
            {
                killDistance = opt.GetInt(Int32OptionNames.KillDistance) + CandR_ScopeIncrease.GetInt();
                scopeAltered[player.PlayerId] = true;
            }
            opt.SetInt(Int32OptionNames.KillDistance, killDistance);
        }
        if (flashTrigger.ContainsKey(player.PlayerId) || smokeBombTriggered.ContainsKey(player.PlayerId))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, 0.05f);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0.05f);
            Logger.Warn($"vision for {player.PlayerId} set to 0.05f", "blind vision");
        }
        else
        {
            opt.SetVision(player.Is(CustomRoles.Cop));
            opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
        }
        return;
    }

    public static void AbilityDescription(string ability, byte playerId = byte.MaxValue)
    {
        ability = ability.ToLower().Trim().TrimStart('*').Replace(" ", string.Empty);
        StringBuilder copAbilities = new();
        int i = 0;
        foreach (var ab in Enum.GetValues(typeof(CopAbility)))
        {
            i++;
            copAbilities.Append($"<b>{i}. {GetString($"CopAbility.{ab}")}</b>:\n");
            copAbilities.Append($"{GetString($"Description.{ab}")}\n\n");
        }
        StringBuilder robberAbilities = new();
        i = 0;
        foreach (var ab in Enum.GetValues(typeof(RobberAbility)))
        {
            i++;
            robberAbilities.Append($"<b>{i}. {GetString($"RobberAbility.{ab}")}</b>:\n");
            robberAbilities.Append($"{GetString($"Description.{ab}")}\n\n");
        }

        if (ability == GetString(CustomRoles.Cop.ToString()).ToLower().Trim().TrimStart('*').Replace(" ", string.Empty) || ability == "cop")
        {
            Utils.SendMessage(copAbilities.ToString(), sendTo:playerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cop), Utils.GetRoleName(CustomRoles.Cop)));
        }
        else if (ability == GetString(CustomRoles.Cop.ToString()).ToLower().Trim().TrimStart('*').Replace(" ", string.Empty) || ability == "robber")
        {
            Utils.SendMessage(robberAbilities.ToString(), sendTo: playerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Robber), Utils.GetRoleName(CustomRoles.Robber)));
        }
        else
        {
            Utils.SendMessage(copAbilities.ToString(), sendTo: playerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cop), Utils.GetRoleName(CustomRoles.Cop)));
            Utils.SendMessage(robberAbilities.ToString(), sendTo: playerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Robber), Utils.GetRoleName(CustomRoles.Robber)));
        }
    }

    public static void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (playerId == byte.MaxValue) return;
        PlayerControl player = Utils.GetPlayerById(playerId);
        if (player == null) return;
        if (player.Is(CustomRoles.Cop))
        { 
            hud.AbilityButton?.OverrideText(GetString("CopAbilityText"));
            hud.KillButton?.OverrideText(GetString("CopKillButtonText"));
        }
    }
    public static string SummaryTexts(byte id, bool disableColor = true, bool check = false)
    {
        string name;
        try
        {
            if (id == PlayerControl.LocalPlayer.PlayerId) name = DataManager.player.Customization.Name;
            else name = Main.AllClientRealNames[GameData.Instance.GetPlayerById(id).ClientId];
        }
        catch
        {
            Logger.Error("Failed to get name for {id} by real client names, try assign with AllPlayerNames", "Utils.SummaryTexts");
            name = Main.AllPlayerNames[id].RemoveHtmlTags().Replace("\r\n", string.Empty) ?? "<color=#ff0000>ERROR</color>";
        }

        // Impossible to output summarytexts for a player without playerState
        if (!Main.PlayerStates.TryGetValue(id, out var ps))
        {
            Logger.Error("playerState for {id} not found", "CopsAndRobbersManager.SummaryTexts");
            return $"[{id}]" + name + " : <b>ERROR</b>";
        }

        string TaskCount = string.Empty;
        Color nameColor = Color.white;
        string roleName = Utils.ColorString(Utils.GetRoleColor(CustomRoles.GM), Utils.GetRoleName(CustomRoles.GM));
        string capturedCountText = string.Empty;
        if (robbers.Contains(id))
        {
            var taskState = Main.PlayerStates?[id].TaskState;
            Color CurrentСolor;
            CurrentСolor = taskState.IsTaskFinished ? Color.green : Utils.GetRoleColor(CustomRoles.Robber);
            TaskCount = Utils.ColorString(CurrentСolor, $" ({taskState.CompletedTasksCount}/{taskState.AllTasksCount})");
            nameColor = Utils.GetRoleColor(CustomRoles.Robber);
            roleName = Utils.ColorString(nameColor, Utils.GetRoleName(CustomRoles.Robber));
            if (!saved.ContainsKey(id)) saved[id] = 0;
            capturedCountText = Utils.ColorString(new Color32(255, 69, 0, byte.MaxValue), $"{GetString("Saved")}: {saved[id]}");
        }  
        else if (cops.Contains(id))
        {
            nameColor = Utils.GetRoleColor(CustomRoles.Cop);
            roleName = Utils.ColorString(nameColor, Utils.GetRoleName(CustomRoles.Cop));
            if (!capturedScore.ContainsKey(id)) capturedScore[id] = 0;
            capturedCountText = Utils.ColorString(new Color32(255, 69, 0, byte.MaxValue), $"{GetString("Captured")}: {capturedScore[id]}");

        }

        Main.PlayerStates.TryGetValue(id, out var playerState);
        var disconnectedText = playerState.deathReason != PlayerState.DeathReason.etc && playerState.Disconnected ? $"({GetString("Disconnected")})" : string.Empty;
 
        string summary = $"{Utils.ColorString(nameColor, name)} - {roleName}{TaskCount} {capturedCountText} 『{Utils.GetVitalText(id, true)}』{disconnectedText}";
       
        return check && Utils.GetDisplayRoleAndSubName(id, id, true).RemoveHtmlTags().Contains("INVALID:NotAssigned")
            ? "INVALID"
            : disableColor ? summary.RemoveHtmlTags() : summary;
    }

    public static string GetProgressText(byte playerId)
    {
        string progressText = string.Empty;
        if (playerId == byte.MaxValue) return progressText;
        Color32 textColor = Color.white;
        if (cops.Contains(playerId))
        { 
            progressText = $" ({numCaptures}/{numRobbers})";
            textColor = Utils.GetRoleColor(CustomRoles.Cop);
        }
        else if (robbers.Contains(playerId))
        {
            var taskState = Main.PlayerStates?[playerId].TaskState;
            string Completed = $"{taskState.CompletedTasksCount}";
            progressText= $" ({Completed}/{taskState.AllTasksCount})";
            textColor = taskState.IsTaskFinished? Color.green : Utils.GetRoleColor(CustomRoles.Robber);
        }
        return Utils.ColorString(textColor, progressText);
    }

    public static void OnPlayerDisconnect(byte playerId)
    {
        if (robbers.Contains(playerId))
        {
            if (captured.ContainsKey(playerId))
            {
                captured.Remove(playerId);
                numCaptures = captured.Count;
                SendCandRData(4, capturedCount: numCaptures);
            }
            numRobbers--;
            SendCandRData(5);
            foreach (var pid in cops)
            {
                var player = Utils.GetPlayerById(pid);
                if (player != null)
                {
                    Utils.NotifyRoles(SpecifySeer: player);
                }
            }
        }
    }
    public static string GetHudText()
    {
        return string.Format(GetString("FFATimeRemain"), RoundTime.ToString());
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdateInGameModeCandRPatch
    {
        private static long LastCheckedCop;
        private static long LastCheckedRobber;
        private static long lastRoundTime;
        private static readonly Dictionary<byte, long> LastCheckedReleaseCooldownCaptured = [];
        private static readonly Dictionary<byte, long> LastCheckedReleaseCooldownRobber = [];
        public static void Postfix(PlayerControl __instance)
        {
            if (!GameStates.IsInTask || Options.CurrentGameMode != CustomGameMode.CandR) return;
            var now = Utils.GetTimeStamp();

            if (lastRoundTime != now)
            {
                lastRoundTime = now;
                RoundTime--;
            }
            if (!AmongUsClient.Instance.AmHost) return;

            if (__instance.AmOwner)
            {
                if (Main.UnShapeShifter.Any(x => Utils.GetPlayerById(x) != null && Utils.GetPlayerById(x).CurrentOutfitType != PlayerOutfitType.Shapeshifted)
                            && !__instance.IsMushroomMixupActive() && Main.GameIsLoaded)
                {
                    foreach (var UnShapeshifterId in Main.UnShapeShifter)
                    {
                        var UnShapeshifter = Utils.GetPlayerById(UnShapeshifterId);
                        if (UnShapeshifter == null)
                        {
                            Main.UnShapeShifter.Remove(UnShapeshifterId);
                            continue;
                        }
                        if (UnShapeshifter.CurrentOutfitType == PlayerOutfitType.Shapeshifted) continue;

                        var randomPlayer = Main.AllPlayerControls.FirstOrDefault(x => x != UnShapeshifter);
                        UnShapeshifter.RpcShapeshift(randomPlayer, false);
                        UnShapeshifter.RpcRejectShapeshift();
                        RoleType.Cop.SetCostume(UnShapeshifter.PlayerId);
                        Utils.NotifyRoles(SpecifyTarget: UnShapeshifter);
                        Logger.Info($"Revert to shapeshifting state for: {__instance.GetRealName()}", "UnShapeShifter_FixedUpdate");
                    }
                }
            }

            captured.Remove(byte.MaxValue);

            robbers.Remove(byte.MaxValue);
            captured.Remove(byte.MaxValue);

            Dictionary<byte, byte> removeCaptured = [];
            foreach (byte robberId in robbers)
            {
                if (robberId == byte.MaxValue) continue;
                PlayerControl robber = Utils.GetPlayerById(robberId);
                if (robber == null) continue;
                Vector2 currentRobberLocation = robber.GetCustomPosition();

                // check if duration of trap is completed every second
                if (now != LastCheckedRobber)
                {
                    LastCheckedRobber = now;
                    // If Spike duration finished, reset the speed of trapped player
                    if (spikeTrigger.ContainsKey(robberId) && now - spikeTrigger[robberId] > CandR_SpikeStripDuration.GetFloat())
                    {
                        if (!captured.ContainsKey(robberId))
                        {
                            Main.AllPlayerSpeed[robberId] = defaultSpeed[robberId];
                            if (adrenalineRushActive.Contains(robberId))
                                Main.AllPlayerSpeed[robberId] += CandR_AdrenalineRushSpeed.GetFloat();
                            robber?.MarkDirtySettings();
                        }
                        spikeTrigger.Remove(robberId);
                    }
                    // If flash duration finished, reset the vision of trapped player
                    if (flashTrigger.ContainsKey(robberId) && now - flashTrigger[robberId] > CandR_FlashBangDuration.GetFloat())
                    {
                        flashTrigger.Remove(robberId);
                        robber.MarkDirtySettings();
                        Logger.Info($"Removed {robberId} from Flash trigger", "Flash remove");
                    }
                }

                // Check if captured release
                if (captured.Any())
                {                  
                    foreach ((var captureId, Vector2 capturedLocation) in captured)
                    {
                        if (captureId == byte.MaxValue) continue;
                        PlayerControl capturedPC = Utils.GetPlayerById(captureId);
                        if (capturedPC == null) continue;

                        if (captured.ContainsKey(robberId)) continue; // excluding all the captured players

                        float dis = Utils.GetDistance(capturedLocation, currentRobberLocation);
                        if (dis < 0.3f)
                        {
                            var releaseCDforCapture = now - capturedTime[captureId];
                            if (releaseCDforCapture < CandR_ReleaseCooldownForCaptured.GetInt())
                            {
                                if (!LastCheckedReleaseCooldownCaptured.ContainsKey(captureId)) LastCheckedReleaseCooldownCaptured[captureId] = now - 1;
                                if (now != LastCheckedReleaseCooldownCaptured[captureId])
                                {
                                    LastCheckedReleaseCooldownCaptured[captureId] = now;
                                    robber.Notify(GetString("C&R_CapturedInReleaseCooldown"), time: CandR_ReleaseCooldownForCaptured.GetInt() - releaseCDforCapture);
                                    Logger.Info($"Time left in release cooldown for captured player, {captureId}: {now - capturedTime[captureId]}", "release canceled");
                                }
                                continue;
                            }
                            if (!releaseTime.ContainsKey(robberId)) releaseTime[robberId] = now;
                            var releaseCDforRobber = now - releaseTime[robberId];
                            if (releaseCDforRobber < CandR_ReleaseCooldownForRobber.GetInt())
                            {
                                if (!LastCheckedReleaseCooldownRobber.ContainsKey(robberId)) LastCheckedReleaseCooldownRobber[robberId] = now - 1;
                                if (now != LastCheckedReleaseCooldownRobber[robberId])
                                {
                                    LastCheckedReleaseCooldownRobber[robberId] = now;
                                    robber.Notify(GetString("C&R_RobberInReleaseCooldown"), time: CandR_ReleaseCooldownForRobber.GetInt() - releaseCDforRobber);
                                    Logger.Info($"Time left in release cooldown for robber, {robberId}: {now - releaseTime[robberId]}", "release canceled");
                                }
                                continue;
                            }

                            removeCaptured[captureId] = robberId;
                            Logger.Info($"to remove captured {captureId}, rob: {robberId}", "C&R FixedUpdate");
                        }
                    }
                    // remove capture players if possible
                    if (removeCaptured.Any())
                    {
                        foreach ((byte rescued, byte saviour) in removeCaptured)
                        {
                            if (!saved.ContainsKey(saviour)) saved[saviour] = 0;
                            saved[saviour]++;
                            Utils.GetPlayerById(rescued).RemoveCaptured();
                            SendCandRData(7, playerId: saviour);
                            releaseTime[saviour] = now;
                        }
                        removeCaptured.Clear();
                        numCaptures = captured.Count;
                        SendCandRData(4, capturedCount: numCaptures);
                        foreach (var pid in cops)
                        {
                            var player = Utils.GetPlayerById(pid);
                            if (player != null)
                            {
                                Utils.NotifyRoles(SpecifySeer: player);
                            }
                        }
                    }
                }

                if (radar.ContainsKey(robberId))
                {
                    PlayerControl closest = null;
                    if (captured.Any())
                    {
                        closest = Main.AllAlivePlayerControls.Where(pc => captured.ContainsKey(pc.PlayerId) && pc != null && pc.PlayerId != robberId)
                            .MinBy(capturedPC => Utils.GetDistance(robber.GetCustomPosition(), capturedPC.GetCustomPosition()));
                    }
                    if (closest == null)
                    {
                        closest = Main.AllAlivePlayerControls.Where(pc => pc.Is(CustomRoles.Cop) && pc != null)
                            .MinBy(closestCop => Utils.GetDistance(robber.GetCustomPosition(), closestCop.GetCustomPosition()));
                    }
                    
                    if (closest != null)
                    {
                        if (radar.TryGetValue(robberId, out byte targetId) && targetId != byte.MaxValue)
                        {
                            if (targetId != closest.PlayerId)
                            {
                                radar[robberId] = closest.PlayerId;
                                SendCandRData(2, robberId);
                                Logger.Info($"Set radar for {robberId}, closest: {closest.PlayerId}", "radar Change");
                                TargetArrow.Remove(robberId, targetId);
                                TargetArrow.Add(robberId, closest.PlayerId);
                            }
                        }
                        else
                        {
                            radar[robberId] = closest.PlayerId;
                            SendCandRData(2, robberId);
                            TargetArrow.Add(robberId, closest.PlayerId);
                            Logger.Info($"Add radar for {robberId}, closest: {closest.PlayerId}", "radar add");
                        }
                    }                   
                }

                // Check if trap triggered
                if (trapLocation.Any())
                {
                    foreach (KeyValuePair<Vector2, CopAbility?> trap in trapLocation)
                    {
                        // captured player can not trigger a trap
                        if (captured.ContainsKey(robberId)) continue;

                        // If player already trapped then continue
                        if (spikeTrigger.ContainsKey(robberId)) continue;
                        if (flashTrigger.ContainsKey(robberId)) continue;

                        var trapDistance = Utils.GetDistance(trap.Key, currentRobberLocation);
                        // check for spike strip
                        if (trap.Value is CopAbility.SpikeStrip && trapDistance <= CandR_SpikeStripRadius.GetFloat())
                        {
                            spikeTrigger[robberId] = now;
                            Main.AllPlayerSpeed[robberId] = Main.MinSpeed;
                            robber?.MarkDirtySettings();
                            removeTrap.Add(trap.Key); // removed the trap from trap location because it was triggered
                            break;
                        }
                        // check for flash bang
                        if (trap.Value is CopAbility.FlashBang && trapDistance <= CandR_FlashBangRadius.GetFloat())
                        {
                            flashTrigger[robberId] = now;
                            robber.MarkDirtySettings();
                            Logger.Info($"added {robberId} to flashTrigger", "Flash trigger");
                            removeTrap.Add(trap.Key);
                            break;
                        }
                    }
                    if (removeTrap.Any())
                    {
                        foreach (Vector2 removeTrapLoc in removeTrap)
                            trapLocation.Remove(removeTrapLoc);
                    }
                }
            }

            cops.Remove(byte.MaxValue);
            foreach (byte copId in cops)
            {
                if (copId == byte.MaxValue) continue;
                PlayerControl copPC = Utils.GetPlayerById(copId);
                if (copPC == null) continue;

                if (smokeBombTriggered.Any())
                {
                    if (now != LastCheckedCop)
                    {
                        LastCheckedCop = now;
                        if (smokeBombTriggered.ContainsKey(copId) && now - smokeBombTriggered[copId] > CandR_SmokeBombDuration.GetFloat())
                        {
                            smokeBombTriggered.Remove(copId);
                            copPC.MarkDirtySettings();
                            Logger.Info($"Removed smoke bomb effect from {copId}", "remove smoke bomb");
                        }
                    }
                }
                //check for k9
                if (k9.ContainsKey(copId))
                {
                    PlayerControl closest = Main.AllAlivePlayerControls.Where(pc => pc.Is(CustomRoles.Robber) && !captured.ContainsKey(pc.PlayerId))
                        .MinBy(robberPC => Utils.GetDistance(copPC.GetCustomPosition(), robberPC.GetCustomPosition()));
                    if (closest == null) continue;
                    if (k9.TryGetValue(copId, out var targetId) && targetId != byte.MaxValue)
                    {
                        if (targetId != closest.PlayerId)
                        {
                            k9[copId] = closest.PlayerId;
                            SendCandRData(0, copId);
                            Logger.Info($"Set k9 for {copId}, closest: {closest.PlayerId}", "Arrow Change");
                            TargetArrow.Remove(copId, targetId);
                            TargetArrow.Add(copId, closest.PlayerId);
                        }
                    }
                    else
                    {
                        k9[copId] = closest.PlayerId;
                        SendCandRData(0, copId);
                        TargetArrow.Add(copId, closest.PlayerId);
                        Logger.Info($"Add k9 for {copId}, closest: {closest.PlayerId}", "Arrow Change");
                    }
                }
            }
        }
    }
}
