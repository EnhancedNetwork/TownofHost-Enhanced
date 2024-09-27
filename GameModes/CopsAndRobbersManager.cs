using AmongUs.GameOptions;
using static TOHE.Translator;
using UnityEngine;
using Hazel;
using static UnityEngine.GraphicsBuffer;
using TOHE.Modules;
using MS.Internal.Xml.XPath;

namespace TOHE;

internal static class CopsAndRobbersManager
{
    private const int Id = 67_224_001;

    public static readonly HashSet<byte> cops = [];
    public static readonly HashSet<byte> robbers = [];
    public static readonly Dictionary<byte, Vector2> captured = [];

    private static readonly Dictionary<byte, int> capturedScore = [];
    private static readonly Dictionary<byte, int> timesCaptured = [];
    private static readonly Dictionary<byte, int> saved = [];
    private static readonly Dictionary<byte, float> defaultSpeed = [];

    private static readonly Dictionary<CopAbility, int> copAbilityChances = [];
    private static readonly Dictionary<byte, CopAbility?> RemoveAbility = [];
    private static readonly Dictionary<Vector2, CopAbility?> trapLocation = [];
    private static readonly HashSet<Vector2> removeTrap = [];
    private static readonly Dictionary<byte, long> spikeTrigger = [];
    private static readonly Dictionary<byte, long> flashTrigger = [];
    private static readonly Dictionary<byte, byte> radar = [];
    private static readonly Dictionary<byte, bool> scopeAltered = [];
    private static int killDistance;


    private static int numCops;

    public static OptionItem CandR_NumCops;
    private static OptionItem CandR_NotifyRobbersWhenCaptured;
    private static OptionItem CandR_CaptureCooldown;
    private static OptionItem CandR_CopAbilityTriggerChance;
    private static OptionItem CandR_AbilityCooldown;
    private static OptionItem CandR_CopAbilityDuration;
    private static OptionItem CandR_HotPursuitChance;
    private static OptionItem CandR_HotPursuitSpeed;
    private static OptionItem CandR_SpikeStripChance;
    private static OptionItem CandR_SpikeStripRadius;
    private static OptionItem CandR_SpikeStripDuration;
    private static OptionItem CandR_FlashBangChance;
    private static OptionItem CandR_FlashBangRadius;
    private static OptionItem CandR_FlashBangDuration;
    private static OptionItem CandR_RadarChance;
    private static OptionItem CandR_ScopeChance;
    private static OptionItem CandR_ScopeIncrease;
    public static OptionItem CopHeader;
    //public static OptionItem CopActiveHidden;
    public static OptionItem RobberHeader;


    public static void SetupCustomOption()
    {
        CopHeader = TextOptionItem.Create(Id-1, "Cop", TabGroup.ModSettings)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue));

        CandR_NumCops = IntegerOptionItem.Create(Id + 1, "C&R_NumCops", new(1, 5, 1), 2, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue));
        CandR_CaptureCooldown = FloatOptionItem.Create(Id + 2, "C&R_CaptureCooldown", new(10f, 60f, 2.5f), 25f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);

        CandR_CopAbilityTriggerChance = IntegerOptionItem.Create(Id + 3, "C&R_CopAbilityTriggerChance", new(0, 100, 5), 50, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Percent);
            //.SetParent(CopActiveHidden);

        CandR_CopAbilityDuration = IntegerOptionItem.Create(Id + 4, "C&R_CopAbilityDuration", new(1, 10, 1), 10, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds)
            .SetParent(CandR_CopAbilityTriggerChance);
        CandR_AbilityCooldown = FloatOptionItem.Create(Id + 5, "C&R_AbilityCooldown", new(10f, 60f, 2.5f), 20f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds)
            .SetParent(CandR_CopAbilityTriggerChance);


        CandR_HotPursuitChance = IntegerOptionItem.Create(Id + 6, "C&R_HotPursuitChance", new(0, 100, 5), 35, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Percent)
            .SetParent(CandR_CopAbilityTriggerChance);
        CandR_HotPursuitSpeed = FloatOptionItem.Create(Id + 7, "C&R_HotPursuitSpeed", new(0f, 2f, 0.25f), 1f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Multiplier)
            .SetParent(CandR_HotPursuitChance);


        CandR_SpikeStripChance = IntegerOptionItem.Create(Id + 8, "C&R_SpikeStripChance", new(0, 100, 5), 20, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Percent)
            .SetParent(CandR_CopAbilityTriggerChance);
        CandR_SpikeStripRadius = FloatOptionItem.Create(Id + 9, "C&R_SpikeStripRadius", new(0.5f, 2f, 0.5f), 1f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Multiplier)
            .SetParent(CandR_SpikeStripChance);
        CandR_SpikeStripDuration = IntegerOptionItem.Create(Id + 10, "C&R_SpikeStripDuration", new(1, 10, 1), 5, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds)
            .SetParent(CandR_SpikeStripChance);

        CandR_FlashBangChance = IntegerOptionItem.Create(Id + 11, "C&R_FlashBangChance", new(0, 100, 5), 15, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Percent)
            .SetParent(CandR_CopAbilityTriggerChance);
        CandR_FlashBangRadius = FloatOptionItem.Create(Id + 12, "C&R_FlashBangRadius", new(0.5f, 2f, 0.5f), 1f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Multiplier)
            .SetParent(CandR_FlashBangChance);
        CandR_FlashBangDuration = IntegerOptionItem.Create(Id + 13, "C&R_FlashBangDuration", new(1, 10, 1), 5, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds)
            .SetParent(CandR_FlashBangChance);

        CandR_RadarChance = IntegerOptionItem.Create(Id + 14, "C&R_RadarChance", new(0, 100, 5), 20, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Percent)
            .SetParent(CandR_CopAbilityTriggerChance);

        CandR_ScopeChance = IntegerOptionItem.Create(Id + 15, "C&R_ScopeChance", new(0, 100, 5), 10, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Percent)
            .SetParent(CandR_CopAbilityTriggerChance);
        CandR_ScopeIncrease = IntegerOptionItem.Create(Id + 16, "C&R_ScopeIncrease", new(1, 5, 1), 1, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Multiplier)
            .SetParent(CandR_ScopeChance);


        CandR_NotifyRobbersWhenCaptured = BooleanOptionItem.Create(Id + 17, "C&R_NotifyRobbersWhenCaptured", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue));
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
        InvisibilityCloak // Become invisible for a set amount of duration.
    }
    private enum CopAbility
    {
        HotPursuit, // Speed boost for a set amount of time.
        SpikeStrip, // Sets a trap that slows down the robber.
        FlashBang, // Sets a trap that blinds the robber temporarily.
        Radar, // Points to the closest robber.
        Scope // Increases the capture range.
    }

    public static RoleTypes RoleBase(CustomRoles role)
    {
        return role switch
        {
            CustomRoles.Cop => RoleTypes.Shapeshifter,
            CustomRoles.Robber => RoleTypes.Engineer,
            _ => RoleTypes.Engineer
        };
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

    private static void CopCumulativeChances()
    {
        Dictionary<CopAbility, OptionItem> copOptionItems = new()
        {
            {CopAbility.HotPursuit, CandR_HotPursuitChance },
            {CopAbility.SpikeStrip, CandR_SpikeStripChance },
            {CopAbility.FlashBang, CandR_FlashBangChance },
            {CopAbility.Radar, CandR_RadarChance },
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
    }

    public static void Init()
    {
        if (Options.CurrentGameMode != CustomGameMode.CandR) return;

        cops.Clear();
        robbers.Clear();
        captured.Clear();
        capturedScore.Clear();
        timesCaptured.Clear();
        saved.Clear();
        numCops = CandR_NumCops.GetInt();
        defaultSpeed.Clear();
        RemoveAbility.Clear();
        trapLocation.Clear();
        removeTrap.Clear();
        spikeTrigger.Clear();
        flashTrigger.Clear();
        radar.Clear();
        scopeAltered.Clear();
        killDistance = Main.RealOptionsData.GetInt(Int32OptionNames.KillDistance);
        CopCumulativeChances();
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
                return;
        }
    }
    private static void AddCaptured(this PlayerControl robber, Vector2 capturedLocation)
    {
        captured[robber.PlayerId] = capturedLocation;
        RoleType.Captured.SetCostume(playerId: robber.PlayerId);
        Main.AllPlayerSpeed[robber.PlayerId] = Main.MinSpeed;
        robber?.MarkDirtySettings();
    }
    private static void RemoveCaptured(this PlayerControl rescued)
    {
        if (rescued == null) return;
        captured.Remove(rescued.PlayerId);
        RoleType.Robber.SetCostume(playerId: rescued.PlayerId); //for robber
        Main.AllPlayerSpeed[rescued.PlayerId] = defaultSpeed[rescued.PlayerId];
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

                //player.RpcSetColor(1); //blue
                //player.RpcSetHat("hat_police");
                //player.RpcSetSkin("skin_Police");
                //player.RpcSetVisor("visor_pk01_Security1Visor");

                break;
            case RoleType.Robber:
                playerOutfit.Set(player.GetRealName(isMeeting: true),
                    6, //black
                    "hat_pk04_Vagabond", //hat
                    "skin_None", //skin 
                    "visor_None", //visor
                    player.CurrentOutfit.PetId,
                    player.CurrentOutfit.NamePlateId);
                //player.RpcSetColor(6); //black
                //player.RpcSetHat("hat_pk04_Vagabond");
                //player.RpcSetSkin("skin_None");
                break;
            case RoleType.Captured:
                playerOutfit.Set(player.GetRealName(isMeeting: true),
                    5, //yellow
                    "hat_tombstone", //hat
                    "skin_prisoner", //skin 
                    "visor_pk01_DumStickerVisor", //visor
                    player.CurrentOutfit.PetId,
                    player.CurrentOutfit.NamePlateId);
                //player.RpcSetColor(5); //yellow
                //player.RpcSetHat("hat_tombstone");
                //player.RpcSetSkin("skin_prisoner");
                //player.RpcSetVisor("visor_pk01_DumStickerVisor");
                break;
        }
        player.SetNewOutfit(newOutfit: playerOutfit, setName: false, setNamePlate: false);
        Main.OvverideOutfit[player.PlayerId] = (playerOutfit, Main.PlayerStates[player.PlayerId].NormalOutfit.PlayerName);
    }
    public static void CaptureCooldown(PlayerControl cop) =>
    Main.AllPlayerKillCooldown[cop.PlayerId] = CandR_CaptureCooldown.GetFloat();

    private static void SendCandRData(byte op, byte copId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncCandRData, SendOption.Reliable, -1);
        writer.Write(op);
        switch (op)
        {
            case 0:
                writer.Write(copId);
                writer.Write(radar[copId]);
                break;
            case 1:
                writer.Write(copId);
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
                byte radarId = reader.ReadByte();
                radar[copId] = radarId;
                break;
            case 1:
                byte removeCopId = reader.ReadByte();
                radar.Remove(removeCopId);
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
            case CopAbility.Radar:
                byte targetId = radar[cop.PlayerId];
                Logger.Info($"Removed radar for {cop.PlayerId}", "Remove radar");
                radar.Remove(cop.PlayerId);
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
        RemoveAbility.Remove(cop.PlayerId);
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

            case CopAbility.Radar:
                if (radar.ContainsKey(cop.PlayerId))
                    return;
                radar.Add(cop.PlayerId, byte.MaxValue);
                SendCandRData(0, cop.PlayerId);
                Logger.Info($"Added {cop.PlayerId} for radar", "Ability activated");
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
        RemoveAbility[cop.PlayerId] = ability;
        var notifyMsg = GetString("C&R_CopAbilityActivated");
        cop.Notify(string.Format(notifyMsg.Replace("{Ability.Name}", "{0}"), GetString($"CopAbility.{ability}")), CandR_CopAbilityDuration.GetFloat());
        _ = new LateTask(() =>
        {
            if (!GameStates.IsInGame || !RemoveAbility.ContainsKey(cop.PlayerId)) return;
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
        if (isForMeeting || !radar.ContainsKey(seer.PlayerId) || seer.PlayerId != target.PlayerId) return string.Empty;
        return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cop), TargetArrow.GetArrows(seer));
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

        Vector2 robberLocation = robber.GetCustomPosition();
        robber.AddCaptured(robberLocation);

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

        if (!timesCaptured.ContainsKey(robber.PlayerId)) timesCaptured[robber.PlayerId] = 0;
        timesCaptured[robber.PlayerId]++;
        CaptureCooldown(cop);

        cop.ResetKillCooldown();
        cop.SetKillCooldown();
    }

    public static void ApplyGameOptions(ref IGameOptions opt, PlayerControl player)
    {
        if (player.Is(CustomRoles.Cop) && CandR_CopAbilityTriggerChance.GetFloat() > 0f)
        {
            AURoleOptions.ShapeshifterCooldown = CandR_AbilityCooldown.GetFloat();
            AURoleOptions.ShapeshifterDuration = CandR_CopAbilityDuration.GetFloat();
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
        if (flashTrigger.ContainsKey(player.PlayerId))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, 0.25f);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0.25f);
            Logger.Warn($"vision for {player.PlayerId} set to 0.25f", "flash vision");
        }
        else
        {
            opt.SetVision(player.Is(CustomRoles.Cop));
            opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
        }
        return;
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


    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdateInGameModeCandRPatch
    {
        private static long LastChecked;
        public static void Postfix(PlayerControl __instance)
        {
            if (!GameStates.IsInTask || Options.CurrentGameMode != CustomGameMode.CandR) return;

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
                        Logger.Info($"Revert to shapeshifting state for: {__instance.GetRealName()}", "UnShapeShifer_FixedUpdate");
                    }
                }
            }

            captured.Remove(byte.MaxValue);

            robbers.Remove(byte.MaxValue);
            captured.Remove(byte.MaxValue);
            var now = Utils.GetTimeStamp();

            Dictionary<byte, byte> removeCaptured = [];
            foreach (byte robberId in robbers)
            {
                if (robberId == byte.MaxValue) continue;
                PlayerControl robber = Utils.GetPlayerById(robberId);
                if (robber == null) continue;
                Vector2 currentRobberLocation = robber.GetCustomPosition();

                // check if duration of trap is completed every second
                if (now != LastChecked)
                {
                    LastChecked = now;
                    // If Spike duration finished, reset the speed of trapped player
                    if (spikeTrigger.ContainsKey(robberId) && now - spikeTrigger[robberId] > CandR_SpikeStripDuration.GetFloat())
                    {
                        Main.AllPlayerSpeed[robberId] = defaultSpeed[robberId];
                        robber?.MarkDirtySettings();
                        spikeTrigger.Remove(robberId);
                    }
                    // If flash duration finished, reset the vision of trapped player
                    if (flashTrigger.ContainsKey(robberId) && now - flashTrigger[robberId] > CandR_FlashBangDuration.GetFloat())
                    {
                        flashTrigger.Remove(robberId);
                        robber.MarkDirtySettings();
                        Logger.Warn($"Removed {robberId} from Flash trigger", "Flash remove");
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
                        }
                        removeCaptured.Clear();
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
                            Logger.Warn($"added {robberId} to flashtrigger", "Flash trigger");
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
                //check for radar
                if (radar.ContainsKey(copId))
                {
                    PlayerControl closest = Main.AllAlivePlayerControls.Where(pc => pc.Is(CustomRoles.Robber) && !captured.ContainsKey(pc.PlayerId))
                        .MinBy(robberPC => Utils.GetDistance(copPC.GetCustomPosition(), robberPC.GetCustomPosition()));
                    if (closest == null) continue;
                    if (radar.TryGetValue(copId, out var targetId) && targetId != byte.MaxValue)
                    {
                        if (targetId != closest.PlayerId)
                        {
                            radar[copId] = closest.PlayerId;
                            SendCandRData(0, copId);
                            Logger.Info($"Set radar for {copId}, closest: {closest.PlayerId}", "Arrow Change");
                            TargetArrow.Remove(copId, targetId);
                            TargetArrow.Add(copId, closest.PlayerId);
                        }
                    }
                    else
                    {
                        radar[copId] = closest.PlayerId;
                        SendCandRData(0, copId);
                        TargetArrow.Add(copId, closest.PlayerId);
                        Logger.Info($"Add radar for {copId}, closest: {closest.PlayerId}", "Arrow Change");
                    }
                }
            }

            //// below this only captured
            //if (!captured.Any()) return;




            //foreach (byte capturedId in captured)
            //{
            //    PlayerControl capturedPC = Utils.GetPlayerById(capturedId);
            //    if (capturedPC == null) continue;

            //    var capturedPos = capturedPC.GetCustomPosition();

            //    foreach (byte robberId in robbers)
            //    {
            //        if (captured.Contains(robberId)) continue;
            //        PlayerControl robberPC = Utils.GetPlayerById(robberId);
            //        if (robberPC == null) continue;

            //        float dis = Utils.GetDistance(capturedPos, robberPC.GetCustomPosition());

            //    }
            //}

        }
    }

}