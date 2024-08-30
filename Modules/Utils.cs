using AmongUs.Data;
using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using UnityEngine;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using TOHE.Roles.Core;
using static TOHE.Translator;
using TOHE.Patches;


namespace TOHE;

public static class Utils
{
    private static readonly DateTime timeStampStartTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static long TimeStamp => (long)(DateTime.Now.ToUniversalTime() - timeStampStartTime).TotalSeconds;
    public static long GetTimeStamp(DateTime? dateTime = null) => (long)((dateTime ?? DateTime.Now).ToUniversalTime() - timeStampStartTime).TotalSeconds;
    
    public static void ErrorEnd(string text)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            Logger.Fatal($"Error: {text} - triggering critical error", "Anti-black");
            ChatUpdatePatch.DoBlockChat = true;
            Main.OverrideWelcomeMsg = GetString("AntiBlackOutNotifyInLobby");
            
            _ = new LateTask(() =>
            {
                Logger.SendInGame(GetString("AntiBlackOutLoggerSendInGame"));
            }, 3f, "Anti-Black Msg SendInGame Error During Loading");

            if (GameStates.IsShip || !GameStates.IsLobby || GameStates.IsCoStartGame)
            {
                _ = new LateTask(() =>
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Error);
                    GameManager.Instance.LogicFlow.CheckEndCriteria();
                    RPC.ForceEndGame(CustomWinner.Error);
                }, 5.5f, "Anti-Black End Game As Critical Error");
            }
            else if (GameStartManager.Instance != null)
            {
                GameStartManager.Instance.ResetStartState();
                AmongUsClient.Instance.RemoveUnownedObjects();
                Logger.SendInGame(GetString("AntiBlackOutLoggerSendInGame"));
            }
            else
            {
                Logger.SendInGame("Host in a unknow antiblack bugged state.");
                Logger.Fatal($"Host in a unknow antiblack bugged state.", "Anti-black");
            }
        }
        else
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.AntiBlackout, SendOption.Reliable);
            writer.Write(text);
            writer.EndMessage();

            Logger.Fatal($"Error: {text} - I'm triggering critical error", "Anti-black");

            if (Options.EndWhenPlayerBug.GetBool())
            {
                _ = new LateTask(() =>
                {
                    Logger.SendInGame(GetString("AntiBlackOutRequestHostToForceEnd"));
                }, 3f, "Anti-Black Msg SendInGame Non-Host Modded Has Error During Loading");
            }
            else
            {
                _ = new LateTask(() =>
                {
                    Logger.SendInGame(GetString("AntiBlackOutHostRejectForceEnd"));
                }, 3f, "Anti-Black Msg SendInGame Host Reject Force End");
                
                _ = new LateTask(() =>
                {
                    if (AmongUsClient.Instance.AmConnected)
                    {
                        AmongUsClient.Instance.ExitGame(DisconnectReasons.Custom);
                        Logger.Fatal($"Error: {text} - Disconnected from the game due critical error", "Anti-black");
                    }
                }, 8f, "Anti-Black Exit Game Due Critical Error");
            }
        }
    }
    public static ClientData GetClientById(int id)
    {
        try
        {
            var client = AmongUsClient.Instance.allClients.ToArray().FirstOrDefault(cd => cd.Id == id);
            return client;
        }
        catch
        {
            return null;
        }
    }

    public static bool AnySabotageIsActive()
        => IsActive(SystemTypes.Electrical)
           || IsActive(SystemTypes.Comms)
           || IsActive(SystemTypes.MushroomMixupSabotage)
           || IsActive(SystemTypes.Laboratory)
           || IsActive(SystemTypes.LifeSupp)
           || IsActive(SystemTypes.Reactor)
           || IsActive(SystemTypes.HeliSabotage);

    public static bool IsActive(SystemTypes type)
    {
        if (GameStates.IsHideNSeek) return false;

        // if ShipStatus not have current SystemTypes, return false
        if (!ShipStatus.Instance.Systems.ContainsKey(type))
        {
            return false;
        }

        int mapId = GetActiveMapId();
        /*
            The Skeld    = 0
            MIRA HQ      = 1
            Polus        = 2
            Dleks        = 3
            The Airship  = 4
            The Fungle   = 5
        */

        //Logger.Info($"{type}", "SystemTypes");

        switch (type)
        {
            case SystemTypes.Electrical:
                {
                    if (mapId == 5) return false; // if The Fungle return false
                    var SwitchSystem = ShipStatus.Instance.Systems[type].Cast<SwitchSystem>();
                    return SwitchSystem != null && SwitchSystem.IsActive;
                }
            case SystemTypes.Reactor:
                {
                    if (mapId == 2) return false; // if Polus return false
                    else
                    {
                        var ReactorSystemType = ShipStatus.Instance.Systems[type].Cast<ReactorSystemType>();
                        return ReactorSystemType != null && ReactorSystemType.IsActive;
                    }
                }
            case SystemTypes.Laboratory:
                {
                    if (mapId != 2) return false; // Only Polus
                    var ReactorSystemType = ShipStatus.Instance.Systems[type].Cast<ReactorSystemType>();
                    return ReactorSystemType != null && ReactorSystemType.IsActive;
                }
            case SystemTypes.LifeSupp:
                {
                    if (mapId is 2 or 4 or 5) return false; // Only Skeld & Dleks & Mira HQ
                    var LifeSuppSystemType = ShipStatus.Instance.Systems[type].Cast<LifeSuppSystemType>();
                    return LifeSuppSystemType != null && LifeSuppSystemType.IsActive;
                }
            case SystemTypes.HeliSabotage:
                {
                    if (mapId != 4) return false; // Only Airhip
                    var HeliSabotageSystem = ShipStatus.Instance.Systems[type].Cast<HeliSabotageSystem>();
                    return HeliSabotageSystem != null && HeliSabotageSystem.IsActive;
                }
            case SystemTypes.Comms:
                {
                    if (mapId is 1 or 5) // Only Mira HQ & The Fungle
                    {
                        var HqHudSystemType = ShipStatus.Instance.Systems[type].Cast<HqHudSystemType>();
                        return HqHudSystemType != null && HqHudSystemType.IsActive;
                    }
                    else
                    {
                        var HudOverrideSystemType = ShipStatus.Instance.Systems[type].Cast<HudOverrideSystemType>();
                        return HudOverrideSystemType != null && HudOverrideSystemType.IsActive;
                    }
                }
            case SystemTypes.MushroomMixupSabotage:
                {
                    if (mapId != 5) return false; // Only The Fungle
                    var MushroomMixupSabotageSystem = ShipStatus.Instance.Systems[type].TryCast<MushroomMixupSabotageSystem>();
                    return MushroomMixupSabotageSystem != null && MushroomMixupSabotageSystem.IsActive;
                }
            default:
                return false;
        }
    }
    public static SystemTypes GetCriticalSabotageSystemType() => GetActiveMapName() switch
    {
        MapNames.Polus => SystemTypes.Laboratory,
        MapNames.Airship => SystemTypes.HeliSabotage,
        _ => SystemTypes.Reactor,
    };

    public static MapNames GetActiveMapName() => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId;
    public static byte GetActiveMapId() => GameOptionsManager.Instance.CurrentGameOptions.MapId;

    public static void SetVision(this IGameOptions opt, bool HasImpVision)
    {
        if (HasImpVision)
        {
            opt.SetFloat(
                FloatOptionNames.CrewLightMod,
                opt.GetFloat(FloatOptionNames.ImpostorLightMod));
            if (IsActive(SystemTypes.Electrical))
            {
                opt.SetFloat(
                FloatOptionNames.CrewLightMod,
                opt.GetFloat(FloatOptionNames.CrewLightMod) * 5);
            }
            return;
        }
        else
        {
            opt.SetFloat(
                FloatOptionNames.ImpostorLightMod,
                opt.GetFloat(FloatOptionNames.CrewLightMod));
            if (IsActive(SystemTypes.Electrical))
            {
                opt.SetFloat(
                FloatOptionNames.ImpostorLightMod,
                opt.GetFloat(FloatOptionNames.ImpostorLightMod) / 5);
            }
            return;
        }
    }
    //誰かが死亡したときのメソッド
    public static void SetVisionV2(this IGameOptions opt)
    {
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, opt.GetFloat(FloatOptionNames.CrewLightMod));
        if (IsActive(SystemTypes.Electrical))
        {
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, opt.GetFloat(FloatOptionNames.ImpostorLightMod) / 5);
        }
        return;
    }
    
    public static void TargetDies(PlayerControl killer, PlayerControl target)
    {
        if (!target.Data.IsDead || GameStates.IsMeeting) return;

        foreach (var seer in Main.AllPlayerControls)
        {
            if (KillFlashCheck(killer, target, seer))
            {
                seer.KillFlash();
                continue;
            }
        }

        if (target.Is(CustomRoles.Cyber))
        {
            Cyber.AfterCyberDeadTask(target, false);
        }
    }
    public static bool KillFlashCheck(PlayerControl killer, PlayerControl target, PlayerControl seer)
    {
        if (seer.Is(CustomRoles.GM) || seer.Is(CustomRoles.Seer)) return true;

        // Global Kill Flash
        if (target.GetRoleClass().GlobalKillFlashCheck(killer, target, seer)) return true;

        // if seer is alive
        if (seer.IsAlive())
        {
            // Kill Flash as killer
            if (seer.GetRoleClass().KillFlashCheck(killer, target, seer)) return true;
        }
        return false;
    }
    public static void KillFlash(this PlayerControl player, bool playKillSound = true)
    {
        // Kill flash (blackout flash + reactor flash)
        bool ReactorCheck = IsActive(GetCriticalSabotageSystemType());

        var Duration = Options.KillFlashDuration.GetFloat();
        if (ReactorCheck) Duration += 0.2f; // Prolong blackout during reactor for vanilla

        //Start
        Main.PlayerStates[player.PlayerId].IsBlackOut = true; //Set black out for player
        if (player.AmOwner)
        {
            FlashColor(new(1f, 0f, 0f, 0.3f));
            if (Constants.ShouldPlaySfx()) RPC.PlaySound(player.PlayerId, playKillSound ? Sounds.KillSound : Sounds.SabotageSound);
        }
        else if (player.IsModClient())
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.KillFlash, SendOption.Reliable, player.GetClientId());
            writer.Write(playKillSound);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        else if (!ReactorCheck) player.ReactorFlash(0f); //Reactor flash for vanilla
        player.MarkDirtySettings();
        _ = new LateTask(() =>
        {
            Main.PlayerStates[player.PlayerId].IsBlackOut = false; //Remove black out for player
            player.MarkDirtySettings();
        }, Options.KillFlashDuration.GetFloat(), "Remove Kill Flash");
    }
    public static void BlackOut(this IGameOptions opt, bool IsBlackOut)
    {
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
        opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision);
        if (IsBlackOut)
        {
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0);
            opt.SetFloat(FloatOptionNames.CrewLightMod, 0);
        }
        return;
    }
    public static string GetRoleTitle(this CustomRoles role)
    {
        string ColorName = ColorString(GetRoleColor(role), GetString($"{role}"));
        
        string chance = GetRoleMode(role);
        if (role.IsAdditionRole() && !role.IsEnable()) chance = ColorString(Color.red, "(OFF)");
        
        return $"{ColorName} {chance}";
    }
    public static string GetInfoLong(this CustomRoles role) 
    {
        var InfoLong = GetString($"{role}" + "InfoLong");
        var CustomName = GetString($"{role}");
        var ColorName = ColorString(GetRoleColor(role).ShadeColor(0.25f), CustomName);
        
        Translator.GetActualRoleName(role, out var RealRole);

        return InfoLong.Replace(RealRole, $"{ColorName}");
    }
    public static string GetDisplayRoleAndSubName(byte seerId, byte targetId, bool notShowAddOns = false)
    {
        var TextData = GetRoleAndSubText(seerId, targetId, notShowAddOns);
        return ColorString(TextData.Item2, TextData.Item1);
    }
    public static string GetRoleName(CustomRoles role, bool forUser = true)
    {
        return GetRoleString(Enum.GetName(typeof(CustomRoles), role), forUser);
    }
    public static string GetRoleMode(CustomRoles role, bool parentheses = true)
    {
        if (Options.HideGameSettings.GetBool() && Main.AllPlayerControls.Length > 1)
            return string.Empty;

        string mode = GetChance(role.GetMode());
        if (role is CustomRoles.Lovers) mode = GetChance(Options.LoverSpawnChances.GetInt());
        else if (role.IsAdditionRole() && Options.CustomAdtRoleSpawnRate.ContainsKey(role))
        {
            mode = GetChance(Options.CustomAdtRoleSpawnRate[role].GetFloat());
            
        }
        
        return parentheses ? $"({mode})" : mode;
    }
    public static string GetChance(float percent)
    {
        return percent switch 
        {
            0 => "<color=#444444>0%</color>",
            5 => "<color=#EE5015>5%</color>",
            10 => "<color=#EC6817>10%</color>",
            15 => "<color=#EC7B17>15%</color>",
            20 => "<color=#EC8E17>20%</color>",
            25 => "<color=#EC9817>25%</color>",
            30 => "<color=#ECAF17>30%</color>",
            35 => "<color=#ECC217>35%</color>",
            40 => "<color=#ECD217>40%</color>",
            45 => "<color=#ECE217>45%</color>",
            50 => "<color=#DFEC17>50%</color>",
            55 => "<color=#DCEC17>55%</color>",
            60 => "<color=#C9EC17>60%</color>",
            65 => "<color=#BFEC17>65%</color>",
            70 => "<color=#ABEC17>70%</color>",
            75 => "<color=#92EC17>75%</color>",
            80 => "<color=#92EC17>80%</color>",
            85 => "<color=#7BEC17>85%</color>",
            90 => "<color=#6EEC17>90%</color>",
            95 => "<color=#5EEC17>95%</color>",
            100 => "<color=#51EC17>100%</color>",
            _ => $"<color=#4287f5>{percent}%</color>"
        };
    }
    public static string GetDeathReason(PlayerState.DeathReason status)
    {
        return GetString("DeathReason." + Enum.GetName(typeof(PlayerState.DeathReason), status));
    }

    public static void SyncGeneralOptions(this PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || !GameStates.IsInGame) return;

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncGeneralOptions, SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.WritePacked((int)player.GetCustomRole());
        writer.Write(Main.PlayerStates[player.PlayerId].IsDead);
        writer.Write(Main.PlayerStates[player.PlayerId].Disconnected);
        writer.WritePacked((int)Main.PlayerStates[player.PlayerId].deathReason);
        writer.Write(Main.AllPlayerKillCooldown[player.PlayerId]);
        writer.Write(Main.AllPlayerSpeed[player.PlayerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static float GetDistance(Vector2 pos1, Vector2 pos2) => Vector2.Distance(pos1, pos2);
    public static Color GetRoleColor(CustomRoles role)
    {
        if (Main.roleColors.TryGetValue(role, out var hexColor))
        {
            _ = ColorUtility.TryParseHtmlString(hexColor, out var color);
            return color;
        }
        return Color.white;
    }
    public static Color GetTeamColor(PlayerControl player)
    {
        string hexColor = string.Empty;
        var Team = player.GetCustomRole().GetCustomRoleTeam();

        switch (Team)
        {
            case Custom_Team.Crewmate:
                hexColor = "#8cffff";
                break;
            case Custom_Team.Impostor:
                hexColor = "#ff1919";
                break;
            case Custom_Team.Neutral:
                hexColor = "#7f8c8d";
                break;
        }

        _ = ColorUtility.TryParseHtmlString(hexColor, out Color c);
        return c;
    }
    public static string GetRoleColorCode(CustomRoles role)
    {
        if (!Main.roleColors.TryGetValue(role, out var hexColor)) hexColor = "#ffffff";
        return hexColor;
    }
    public static (string, Color) GetRoleAndSubText(byte seerId, byte targetId, bool notShowAddOns = false)
    {
        string RoleText = "Invalid Role";
        Color RoleColor = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

        var targetMainRole = Main.PlayerStates[targetId].MainRole;
        var targetSubRoles = Main.PlayerStates[targetId].SubRoles;
        
        // If a player is possessed by the Dollmaster swap each other's role and add-ons for display for every other client other than Dollmaster and target.
        if (DollMaster.IsControllingPlayer)
        {
            if (!(DollMaster.DollMasterTarget == null || DollMaster.controllingTarget == null))
            {
                if (seerId != DollMaster.DollMasterTarget.PlayerId && targetId == DollMaster.DollMasterTarget.PlayerId)
                    targetSubRoles = Main.PlayerStates[DollMaster.controllingTarget.PlayerId].SubRoles;
                else if (seerId != DollMaster.controllingTarget.PlayerId && targetId == DollMaster.controllingTarget.PlayerId)
                    targetSubRoles = Main.PlayerStates[DollMaster.DollMasterTarget.PlayerId].SubRoles;
            }
        }

        RoleText = GetRoleName(targetMainRole);
        RoleColor = GetRoleColor(targetMainRole);

        try
        {
            if (targetSubRoles.Any())
            {
                var seer = GetPlayerById(seerId);
                var target = GetPlayerById(targetId);

                if (seer == null || target == null) return (RoleText, RoleColor);

                // if player last imp
                if (LastImpostor.currentId == targetId)
                    RoleText = GetRoleString("Last-") + RoleText;

                if (Options.NameDisplayAddons.GetBool() && !notShowAddOns)
                {
                    var seerPlatform = seer.GetClient()?.PlatformData.Platform;
                    var addBracketsToAddons = Options.AddBracketsToAddons.GetBool();

                    static bool Checkif(string str) {

                        string[] strings = ["*Prefix", "INVALID"];
                        return strings.Any(str.Contains); 
                    }
                    static string Getname(string str) => !Checkif(GetString($"Prefix.{str}")) ? GetString($"Prefix.{str}") : GetString($"{str}");

                    // if the player is playing on a console platform
                    if (seerPlatform is Platforms.Playstation or Platforms.Xbox or Platforms.Switch)
                    {
                        // By default, censorship is enabled on consoles
                        // Need to set add-ons colors without endings "</color>"


                        // colored role
                        RoleText = ColorStringWithoutEnding(GetRoleColor(targetMainRole), RoleText);

                        // colored add-ons
                        foreach (var subRole in targetSubRoles.Where(subRole => subRole.ShouldBeDisplayed() && seer.ShowSubRoleTarget(target, subRole)).ToArray())
                            RoleText = ColorStringWithoutEnding(GetRoleColor(subRole), addBracketsToAddons ? $"({Getname($"{subRole}")}) " : $"{Getname($"{subRole}")} ") + RoleText;
                    }
                    // default
                    else
                    {
                        foreach (var subRole in targetSubRoles.Where(subRole => subRole.ShouldBeDisplayed() && seer.ShowSubRoleTarget(target, subRole)).ToArray())
                            RoleText = ColorString(GetRoleColor(subRole), addBracketsToAddons ? $"({Getname($"{subRole}")}) " : $"{Getname($"{subRole}")} ") + RoleText;
                    }
                }

                foreach (var subRole in targetSubRoles.ToArray())
                {
                    if (seer.ShowSubRoleTarget(target, subRole))
                        switch (subRole)
                        {
                            case CustomRoles.Madmate:
                            case CustomRoles.Recruit:
                            case CustomRoles.Charmed:
                            case CustomRoles.Soulless:
                            case CustomRoles.Infected:
                            case CustomRoles.Contagious:
                            case CustomRoles.Admired:
                                RoleColor = GetRoleColor(subRole);
                                RoleText = GetRoleString($"{subRole}-") + RoleText;
                                break;

                        }
                }
            }

            return (RoleText, RoleColor);
        }
        catch
        {
            return (RoleText, RoleColor);
        }
    }
    public static string GetKillCountText(byte playerId, bool ffa = false)
    {
        int count = Main.PlayerStates.Count(x => x.Value.GetRealKiller() == playerId);
        if (count < 1 && !ffa) return "";
        return ColorString(new Color32(255, 69, 0, byte.MaxValue), string.Format(GetString("KillCount"), count));
    }
    public static string GetVitalText(byte playerId, bool RealKillerColor = false)
    {
        var state = Main.PlayerStates[playerId];
        string deathReason = state.IsDead ? state.deathReason == PlayerState.DeathReason.etc && state.Disconnected ? GetString("Disconnected") : GetString("DeathReason." + state.deathReason) : GetString("Alive");
        if (RealKillerColor)
        {
            var KillerId = state.GetRealKiller();
            Color color = KillerId != byte.MaxValue ? GetRoleColor(Main.PlayerStates[KillerId].MainRole) : GetRoleColor(CustomRoles.Doctor);
            if (state.deathReason == PlayerState.DeathReason.etc && state.Disconnected) color = new Color(255, 255, 255, 50);
            deathReason = ColorString(color, deathReason);
        }
        return deathReason;
    }

    public static (RoleTypes roleType, CustomRoles customRole) GetRoleMap(byte seerId, byte targetId = byte.MaxValue)
    {
        if (targetId == byte.MaxValue) targetId = seerId;

        return RpcSetRoleReplacer.RoleMap[(seerId, targetId)];
    }
    public static bool HasTasks(NetworkedPlayerInfo playerData, bool ForRecompute = true)
    {
        if (GameStates.IsLobby) return false;

        //Tasks may be null, in which case no task is assumed
        if (playerData == null) return false;
        if (playerData.Tasks == null) return false;
        if (playerData.Role == null) return false;

        var hasTasks = true;
        if (!Main.PlayerStates.TryGetValue(playerData.PlayerId, out var States))
        {
            return false;
        }

        if (States.Disconnected) return false;
        //if (playerData.Role.IsImpostor)
        //    hasTasks = false; //Tasks are determined based on CustomRole

        if (Options.CurrentGameMode == CustomGameMode.FFA) return false;
        if (playerData.IsDead && Options.GhostIgnoreTasks.GetBool()) hasTasks = false;

        if (GameStates.IsHideNSeek) return hasTasks;

        var role = States.MainRole;

        if (States.RoleClass != null && States.RoleClass.HasTasks(playerData, role, ForRecompute) == false)
            hasTasks = false;

        switch (role)
        {
            case CustomRoles.GM:
                hasTasks = false;
                break;
            default:
                // player based on an impostor not should have tasks
                if (States.RoleClass.ThisRoleBase is CustomRoles.Impostor or CustomRoles.Shapeshifter or CustomRoles.Phantom)
                    hasTasks = false;
                break;
        }

        foreach (var subRole in States.SubRoles.ToArray())
            switch (subRole)
            {
                case CustomRoles.Madmate:
                case CustomRoles.Charmed:
                case CustomRoles.Recruit:
                case CustomRoles.Egoist:
                case CustomRoles.Infected:
                case CustomRoles.EvilSpirit:
                case CustomRoles.Contagious:
                case CustomRoles.Soulless:
                case CustomRoles.Rascal:
                    hasTasks &= !ForRecompute;
                    break;
                case CustomRoles.Mundane:
                    if (!hasTasks) hasTasks = !ForRecompute;
                    break;

            }

        if (CopyCat.NoHaveTask(playerData.PlayerId)) hasTasks = false;
        if (Main.TasklessCrewmate.Contains(playerData.PlayerId)) hasTasks = false;

        return hasTasks;
    }

    public static string GetProgressText(PlayerControl pc)
    {
        try
        {
            if (!Main.playerVersion.ContainsKey(AmongUsClient.Instance.HostId)) return string.Empty;
            var taskState = pc.GetPlayerTaskState();
            var Comms = false;
            if (taskState.hasTasks)
            {
                if (IsActive(SystemTypes.Comms)) Comms = true;
                if (Camouflager.AbilityActivated) Comms = true;
            }
            return GetProgressText(pc.PlayerId, Comms);
        }
        catch (Exception error)
        {
            ThrowException(error);
            Logger.Error($"PlayerId: {pc.PlayerId}, Role: {Main.PlayerStates[pc.PlayerId].MainRole}", "GetProgressText(PlayerControl pc)");
            return "Error1";
        }
    }
    public static string GetProgressText(byte playerId, bool comms = false)
    {
        try
        {
            if (!Main.playerVersion.ContainsKey(AmongUsClient.Instance.HostId)) return string.Empty;
            var ProgressText = new StringBuilder();
            var role = Main.PlayerStates[playerId].MainRole;
            
            if (Options.CurrentGameMode == CustomGameMode.FFA && role == CustomRoles.Killer)
            {
                ProgressText.Append(FFAManager.GetDisplayScore(playerId));
            }
            else
            {
                ProgressText.Append(playerId.GetRoleClassById()?.GetProgressText(playerId, comms));

                if (ProgressText.Length == 0)
                {
                    var taskState = Main.PlayerStates?[playerId].TaskState;
                    if (taskState.hasTasks)
                    {
                        Color TextColor;
                        var info = GetPlayerInfoById(playerId);
                        var TaskCompleteColor = HasTasks(info) ? Color.green : GetRoleColor(role).ShadeColor(0.5f);
                        var NonCompleteColor = HasTasks(info) ? Color.yellow : Color.white;

                        if (Workhorse.IsThisRole(playerId))
                            NonCompleteColor = Workhorse.RoleColor;

                        var NormalColor = taskState.IsTaskFinished ? TaskCompleteColor : NonCompleteColor;
                        if (Main.PlayerStates.TryGetValue(playerId, out var ps) && ps.MainRole == CustomRoles.Crewpostor)
                            NormalColor = Color.red;

                        TextColor = comms ? Color.gray : NormalColor;
                        string Completed = comms ? "?" : $"{taskState.CompletedTasksCount}";
                        ProgressText.Append(ColorString(TextColor, $" ({Completed}/{taskState.AllTasksCount})"));
                    }
                }
                else
                {
                    ProgressText.Insert(0, " ");
                }
            }
            return ProgressText.ToString();
        }
        catch (Exception error)
        {
            ThrowException(error);
            Logger.Error($"PlayerId: {playerId}, Role: {Main.PlayerStates[playerId].MainRole}", "GetProgressText(byte playerId, bool comms = false)");
            return "Error2";
        }
    }
    public static void ShowActiveSettingsHelp(byte PlayerId = byte.MaxValue)
    {
        SendMessage(GetString("CurrentActiveSettingsHelp") + ":", PlayerId);

        if (Options.DisableDevices.GetBool()) { SendMessage(GetString("DisableDevicesInfo"), PlayerId); }
        if (Options.SyncButtonMode.GetBool()) { SendMessage(GetString("SyncButtonModeInfo"), PlayerId); }
        if (Options.SabotageTimeControl.GetBool()) { SendMessage(GetString("SabotageTimeControlInfo"), PlayerId); }
        if (Options.RandomMapsMode.GetBool()) { SendMessage(GetString("RandomMapsModeInfo"), PlayerId); }
        if (Main.EnableGM.Value) { SendMessage(GetRoleName(CustomRoles.GM) + GetString("GMInfoLong"), PlayerId); }
        
        foreach (var role in CustomRolesHelper.AllRoles)
        {
            if (role.IsEnable() && !role.IsVanilla()) SendMessage(GetRoleName(role) + GetRoleMode(role) + GetString(Enum.GetName(typeof(CustomRoles), role) + "InfoLong"), PlayerId);
        }

        if (Options.NoGameEnd.GetBool()) { SendMessage(GetString("NoGameEndInfo"), PlayerId); }
    }
    public static void ShowActiveSettings(byte PlayerId = byte.MaxValue)
    {
        if (Options.HideGameSettings.GetBool() && PlayerId != byte.MaxValue)
        {
            SendMessage(GetString("Message.HideGameSettings"), PlayerId);
            return;
        }

        var sb = new StringBuilder();
        sb.Append(" ★ " + GetString("TabGroup.SystemSettings"));
        foreach (var opt in OptionItem.AllOptions.Where(x => x.GetBool() && x.Parent == null && x.Tab is TabGroup.SystemSettings && !x.IsHiddenOn(Options.CurrentGameMode)).ToArray())
        {
            sb.Append($"\n{opt.GetName(true)}: {opt.GetString()}");
            //ShowChildrenSettings(opt, ref sb);
            var text = sb.ToString();
            sb.Clear().Append(text.RemoveHtmlTags());
        }
        sb.Append("\n\n ★ " + GetString("TabGroup.ModSettings"));
        foreach (var opt in OptionItem.AllOptions.Where(x => x.GetBool() && x.Parent == null && x.Tab is TabGroup.ModSettings && !x.IsHiddenOn(Options.CurrentGameMode)).ToArray())
        {
            sb.Append($"\n{opt.GetName(true)}: {opt.GetString()}");
            //ShowChildrenSettings(opt, ref sb);
            var text = sb.ToString();
            sb.Clear().Append(text.RemoveHtmlTags());
        }

        SendMessage(sb.ToString(), PlayerId);
    }
    
    public static void ShowAllActiveSettings(byte PlayerId = byte.MaxValue)
    {
        if (Options.HideGameSettings.GetBool() && PlayerId != byte.MaxValue)
        {
            SendMessage(GetString("Message.HideGameSettings"), PlayerId);
            return;
        }
        var sb = new StringBuilder();

        sb.Append(GetString("Settings")).Append(':');
        foreach (var role in Options.CustomRoleCounts.Keys.ToArray())
        {
            if (!role.IsEnable()) continue;
            string mode = GetChance(role.GetMode());
            sb.Append($"\n【{GetRoleName(role)}:{mode} ×{role.GetCount()}】\n");
            ShowChildrenSettings(Options.CustomRoleSpawnChances[role], ref sb);
            var text = sb.ToString();
            sb.Clear().Append(text.RemoveHtmlTags());
        }
        foreach (var opt in OptionItem.AllOptions.Where(x => x.GetBool() && x.Parent == null && x.Id > 59999 && !x.IsHiddenOn(Options.CurrentGameMode)).ToArray())
        {
            if (opt.Name is "KillFlashDuration" or "RoleAssigningAlgorithm")
                sb.Append($"\n【{opt.GetName(true)}: {opt.GetString()}】\n");
            else
                sb.Append($"\n【{opt.GetName(true)}】\n");
            ShowChildrenSettings(opt, ref sb);
            var text = sb.ToString();
            sb.Clear().Append(text.RemoveHtmlTags());
        }

        SendMessage(sb.ToString(), PlayerId);
    }
    public static void CopyCurrentSettings()
    {
        var sb = new StringBuilder();
        if (Options.HideGameSettings.GetBool() && !AmongUsClient.Instance.AmHost)
        {
            ClipboardHelper.PutClipboardString(GetString("Message.HideGameSettings"));
            return;
        }
        sb.Append($"━━━━━━━━━━━━【{GetString("Roles")}】━━━━━━━━━━━━");
        foreach (var role in Options.CustomRoleCounts.Keys.ToArray())
        {
            if (!role.IsEnable()) continue;
            string mode = GetChance(role.GetMode());
            sb.Append($"\n【{GetRoleName(role)}:{mode} ×{role.GetCount()}】\n");
            ShowChildrenSettings(Options.CustomRoleSpawnChances[role], ref sb);
            var text = sb.ToString();
            sb.Clear().Append(text.RemoveHtmlTags());
        }
        sb.Append($"━━━━━━━━━━━━【{GetString("Settings")}】━━━━━━━━━━━━");
        foreach (var opt in OptionItem.AllOptions.Where(x => x.GetBool() && x.Parent == null && x.Id > 59999 && !x.IsHiddenOn(Options.CurrentGameMode)).ToArray())
        {
            if (opt.Name == "KillFlashDuration")
                sb.Append($"\n【{opt.GetName(true)}: {opt.GetString()}】\n");
            else
                sb.Append($"\n【{opt.GetName(true)}】\n");
            ShowChildrenSettings(opt, ref sb);
            var text = sb.ToString();
            sb.Clear().Append(text.RemoveHtmlTags());
        }
        sb.Append($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        ClipboardHelper.PutClipboardString(sb.ToString());
    }
    public static void ShowActiveRoles(byte PlayerId = byte.MaxValue)
    {
        if (Options.HideGameSettings.GetBool() && PlayerId != byte.MaxValue)
        {
            SendMessage(GetString("Message.HideGameSettings"), PlayerId);
            return;
        }

        List<string> impsb = [];
        List<string> neutralsb = [];
        List<string> crewsb = [];
        List<string> addonsb = [];

        foreach (var role in CustomRolesHelper.AllRoles)
        {
            string mode = GetChance(role.GetMode());
            if (role.IsEnable())
            {
                if (role is CustomRoles.Lovers) mode = GetChance(Options.LoverSpawnChances.GetInt());
                else if (role.IsAdditionRole() && Options.CustomAdtRoleSpawnRate.ContainsKey(role))
                {
                    mode = GetChance(Options.CustomAdtRoleSpawnRate[role].GetFloat());

                }
                var roleDisplay = $"{GetRoleName(role)}: {mode} x{role.GetCount()}";
                if (role.IsAdditionRole()) addonsb.Add(roleDisplay);
                else if (role.IsCrewmate()) crewsb.Add(roleDisplay);
                else if (role.IsImpostor() || role.IsMadmate()) impsb.Add(roleDisplay);
                else if (role.IsNeutral()) neutralsb.Add(roleDisplay);
            }
        }

        impsb.Sort();
        crewsb.Sort();
        neutralsb.Sort();
        addonsb.Sort();
        
        SendMessage(string.Join("\n", impsb), PlayerId, ColorString(GetRoleColor(CustomRoles.Impostor), GetString("ImpostorRoles")), ShouldSplit: true);
        SendMessage(string.Join("\n", crewsb), PlayerId, ColorString(GetRoleColor(CustomRoles.Crewmate), GetString("CrewmateRoles")), ShouldSplit: true);
        SendMessage(string.Join("\n", neutralsb), PlayerId, GetString("NeutralRoles"), ShouldSplit: true);
        SendMessage(string.Join("\n", addonsb), PlayerId, GetString("AddonRoles"), ShouldSplit: true);
    }
    public static void ShowChildrenSettings(OptionItem option, ref StringBuilder sb, int deep = 0, bool command = false)
    {
        if (Options.HideGameSettings.GetBool()) return;

        foreach (var opt in option.Children.Select((v, i) => new { Value = v, Index = i + 1 }).ToArray())
        {
            if (command)
            {
                sb.Append("\n\n");
                command = false;
            }

            if (opt.Value.Name == "Maximum") continue; //Maximumの項目は飛ばす
            if (opt.Value.Name == "DisableSkeldDevices" && !GameStates.SkeldIsActive && !GameStates.DleksIsActive) continue;
            if (opt.Value.Name == "DisableMiraHQDevices" && !GameStates.MiraHQIsActive) continue;
            if (opt.Value.Name == "DisablePolusDevices" && !GameStates.PolusIsActive) continue;
            if (opt.Value.Name == "DisableAirshipDevices" && !GameStates.AirshipIsActive) continue;
            if (opt.Value.Name == "PolusReactorTimeLimit" && !GameStates.PolusIsActive) continue;
            if (opt.Value.Name == "AirshipReactorTimeLimit" && !GameStates.AirshipIsActive) continue;
            if (deep > 0)
            {
                sb.Append(string.Concat(Enumerable.Repeat("┃", Mathf.Max(deep - 1, 0))));
                sb.Append(opt.Index == option.Children.Count ? "┗ " : "┣ ");
            }
            sb.Append($"{opt.Value.GetName(true)}: {opt.Value.GetString()}\n");
            if (opt.Value.GetBool()) ShowChildrenSettings(opt.Value, ref sb, deep + 1);
        }
    }
    public static void ShowLastRoles(byte PlayerId = byte.MaxValue)
    {
        if (AmongUsClient.Instance.IsGameStarted)
        {
            SendMessage(GetString("CantUse.lastroles"), PlayerId);
            return;
        }

        var sb = new StringBuilder();

        sb.Append($"<#ffffff>{GetString("RoleSummaryText")}</color>");

        List<byte> cloneRoles = new(Main.PlayerStates.Keys);
        foreach (byte id in Main.winnerList.ToArray())
        {
            if (EndGamePatch.SummaryText[id].Contains("<INVALID:NotAssigned>")) continue;
            sb.Append($"\n<#c4aa02>★</color> ").Append(EndGamePatch.SummaryText[id]/*.RemoveHtmlTags()*/);
            cloneRoles.Remove(id);
        }
        switch (Options.CurrentGameMode)
        {
            case CustomGameMode.FFA:
                List<(int, byte)> listFFA = [];
                foreach (byte id in cloneRoles.ToArray())
                {
                    listFFA.Add((FFAManager.GetRankOfScore(id), id));
                }
                listFFA.Sort();
                foreach ((int, byte) id in listFFA.ToArray())
                {
                    sb.Append($"\n　 ").Append(EndGamePatch.SummaryText[id.Item2]);
                }
                break;
            default: // Normal game
                foreach (byte id in cloneRoles.ToArray())
                {
                    if (EndGamePatch.SummaryText[id].Contains("<INVALID:NotAssigned>"))
                        continue;
                    sb.Append($"\n　 ").Append(EndGamePatch.SummaryText[id]);
                    
                }
                break;
        }
        string lr = sb.ToString();
        try{
            if (lr.Length > 2024 && (!GetPlayerById(PlayerId).IsModClient()))
            {
                lr = lr.Replace("<color=", "<");
                lr.SplitMessage().Do(x => SendMessage("\n", PlayerId, $"<size=75%>" + x + "</size>")); //Since it will always capture a newline, there's more than enough space to put this in
            }
            else
            {
                SendMessage("\n", PlayerId,  "<size=75%>" + lr + "</size>");
            }
        }
        catch (Exception err)
        {
            Logger.Warn($"Error after try split the msg {lr} at: {err}", "Utils.ShowLastRoles..LastRoles");
        }
    }
    public static void ShowKillLog(byte PlayerId = byte.MaxValue)
    {
        if (GameStates.IsInGame)
        {
            SendMessage(GetString("CantUse.killlog"), PlayerId);
            return;
        }
        if (EndGamePatch.KillLog != "") 
        {
            string kl = EndGamePatch.KillLog;
            kl = Options.OldKillLog.GetBool() ? kl.RemoveHtmlTags() : kl.Replace("<color=", "<");
            var tytul = !Options.OldKillLog.GetBool() ? ColorString(new Color32(102, 16, 16, 255),  "《 " + GetString("KillLog") + " 》") : "";
            SendSpesificMessage(kl, PlayerId, tytul);
        }
    }
    public static void ShowLastResult(byte PlayerId = byte.MaxValue)
    {
        if (GameStates.IsInGame)
        {
            SendMessage(GetString("CantUse.lastresult"), PlayerId);
            return;
        }
        var sb = new StringBuilder();
        if (SetEverythingUpPatch.LastWinsText != "") sb.Append($"{GetString("LastResult")}: {SetEverythingUpPatch.LastWinsText}");
        if (SetEverythingUpPatch.LastWinsReason != "") sb.Append($"\n{GetString("LastEndReason")}: {SetEverythingUpPatch.LastWinsReason}");
        if (sb.Length > 0 && Options.CurrentGameMode != CustomGameMode.FFA) SendMessage(sb.ToString(), PlayerId);
    }
    public static string GetSubRolesText(byte id, bool disableColor = false, bool intro = false, bool summary = false)
    {
        var SubRoles = Main.PlayerStates[id].SubRoles;
        if (SubRoles.Count == 0 && intro == false) return "";
        var sb = new StringBuilder();

        if (summary)
            sb.Append(' ');

        foreach (var role in SubRoles.ToArray())
        {
            if (role is CustomRoles.NotAssigned or
                        CustomRoles.LastImpostor) continue;
            if (summary && role is CustomRoles.Madmate or CustomRoles.Charmed or CustomRoles.Recruit or CustomRoles.Admired or CustomRoles.Infected or CustomRoles.Contagious or CustomRoles.Soulless) continue;

            var RoleColor = GetRoleColor(role);
            var RoleText = disableColor ? GetRoleName(role) : ColorString(RoleColor, GetRoleName(role));
            
            if (summary)
                sb.Append($"{ColorString(RoleColor, "(")}{RoleText}{ColorString(RoleColor, ")")}");
            else
                sb.Append($"{ColorString(Color.white, " + ")}{RoleText}");
        }

        return sb.ToString();
    }

    public static string GetRegionName(IRegionInfo region = null)
    {
        region ??= ServerManager.Instance.CurrentRegion;

        string name = region.Name;

        if (AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
        {
            name = "Local Games";
            return name;
        }

        if (region.PingServer.EndsWith("among.us", StringComparison.Ordinal))
        {
            // Official server
            if (name == "North America") name = "NA";
            else if (name == "Europe") name = "EU";
            else if (name == "Asia") name = "AS";

            return name;
        }

        var Ip = region.Servers.FirstOrDefault()?.Ip ?? string.Empty;

        if (Ip.Contains("aumods.us", StringComparison.Ordinal)
            || Ip.Contains("duikbo.at", StringComparison.Ordinal))
        {
            // Official Modded Server
            if (Ip.Contains("au-eu")) name = "MEU";
            else if (Ip.Contains("au-as")) name = "MAS";
            else if (Ip.Contains("www.")) name = "MNA";

            return name;
        }

        if (name.Contains("nikocat233", StringComparison.OrdinalIgnoreCase))
        {
            name = name.Replace("nikocat233", "Niko233", StringComparison.OrdinalIgnoreCase);
        }

        return name;
    }
    // From EHR by Gurge44
    public static void ThrowException(Exception ex, [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string callerMemberName = "")
    {
        try
        {
            StackTrace st = new(0, true);
            StackFrame[] stFrames = st.GetFrames();

            StackFrame firstFrame = stFrames.FirstOrDefault();

            var sb = new StringBuilder();
            sb.Append($" Exception: {ex.Message}\n      thrown by {ex.Source}\n      at {ex.TargetSite}\n      in {fileName}\n      at line {lineNumber}\n      in method \"{callerMemberName}\"\n------ Method Stack Trace ------");

            bool skip = true;
            foreach (StackFrame sf in stFrames)
            {
                if (skip)
                {
                    skip = false;
                    continue;
                }

                var callerMethod = sf.GetMethod();

                string callerMethodName = callerMethod?.Name;
                string callerClassName = callerMethod?.DeclaringType?.FullName;

                var line = $"line {sf.GetFileLineNumber()} ({sf.GetFileColumnNumber()}) in {sf.GetFileName()}";

                sb.Append($"\n      at {callerClassName}.{callerMethodName} ({line})");
            }

            sb.Append("\n------ End of Method Stack Trace ------");
            sb.Append("\n------ Exception Stack Trace ------");

            sb.Append(ex.StackTrace?.Replace("\r\n", "\n").Replace("\\n", "\n").Replace("\n", "\n      "));

            sb.Append("\n------ End of Exception Stack Trace ------");

            Logger.Error(sb.ToString(), firstFrame?.GetMethod()?.ToString(), multiLine: true);
        }
        catch
        {
        }
    }
    public static byte MsgToColor(string text, bool isHost = false)
    {
        text = text.ToLowerInvariant();
        text = text.Replace("色", string.Empty);
        int color;
        try { color = int.Parse(text); } catch { color = -1; }
        switch (text)
        {
            case "0":
            case "红":
            case "紅":
            case "red":
            case "Red":
            case "vermelho":
            case "Vermelho":
            case "крас":
            case "Крас":
            case "красн":
            case "Красн":
            case "красный":
            case "Красный":
                color = 0; break;
            case "1":
            case "蓝":
            case "藍":
            case "深蓝":
            case "blue":
            case "Blue":
            case "azul":
            case "Azul":
            case "син":
            case "Син":
            case "синий":
            case "Синий":
                color = 1; break;
            case "2":
            case "绿":
            case "綠":
            case "深绿":
            case "green":
            case "Green":
            case "verde-escuro":
            case "Verde-Escuro":
            case "Зел":
            case "зел":
            case "Зелёный":
            case "Зеленый":
            case "зелёный":
            case "зеленый":
                color = 2; break;
            case "3":
            case "粉红":
            case "pink":
            case "Pink":
            case "rosa":
            case "Rosa":
            case "Роз":
            case "роз":
            case "Розовый":
            case "розовый":
                color = 3; break;
            case "4":
            case "橘":
            case "orange":
            case "Orange":
            case "laranja":
            case "Laranja":
            case "оранж":
            case "Оранж":
            case "оранжевый":
            case "Оранжевый":
                color = 4; break;
            case "5":
            case "黄":
            case "黃":
            case "yellow":
            case "Yellow":
            case "amarelo":
            case "Amarelo":
            case "Жёлт":
            case "Желт":
            case "жёлт":
            case "желт":
            case "Жёлтый":
            case "Желтый":
            case "жёлтый":
            case "желтый":
                color = 5; break;
            case "6":
            case "黑":
            case "black":
            case "Black":
            case "preto":
            case "Preto":
            case "Чёрн":
            case "Черн":
            case "Чёрный":
            case "Черный":
            case "чёрный":
            case "черный":
                color = 6; break;
            case "7":
            case "白":
            case "white":
            case "White":
            case "branco":
            case "Branco":
            case "Белый":
            case "белый":
                color = 7; break;
            case "8":
            case "紫":
            case "purple":
            case "Purple":
            case "roxo":
            case "Roxo":
            case "Фиол":
            case "фиол":
            case "Фиолетовый":
            case "фиолетовый":
                color = 8; break;
            case "9":
            case "棕":
            case "brown":
            case "Brown":
            case "marrom":
            case "Marrom":
            case "Корич":
            case "корич":
            case "Коричневый":
            case "коричевый":
                color = 9; break;
            case "10":
            case "青":
            case "cyan":
            case "Cyan":
            case "ciano":
            case "Ciano":
            case "Голуб":
            case "голуб":
            case "Голубой":
            case "голубой":
                color = 10; break;
            case "11":
            case "黄绿":
            case "黃綠":
            case "浅绿":
            case "lime":
            case "Lime":
            case "verde-claro":
            case "Verde-Claro":
            case "Лайм":
            case "лайм":
            case "Лаймовый":
            case "лаймовый":
                color = 11; break;
            case "12":
            case "红褐":
            case "紅褐":
            case "深红":
            case "maroon":
            case "Maroon":
            case "bordô":
            case "Bordô":
            case "vinho":
            case "Vinho":
            case "Борд":
            case "борд":
            case "Бордовый":
            case "бордовый":
                color = 12; break;
            case "13":
            case "玫红":
            case "玫紅":
            case "浅粉":
            case "rose":
            case "Rose":
            case "rosa-claro":
            case "Rosa-Claro":
            case "Светло роз":
            case "светло роз":
            case "Светло розовый":
            case "светло розовый":
            case "Сирень":
            case "сирень":
            case "Сиреневый":
            case "сиреневый":
                color = 13; break;
            case "14":
            case "焦黄":
            case "焦黃":
            case "淡黄":
            case "banana":
            case "Banana":
            case "Банан":
            case "банан":
            case "Банановый":
            case "банановый":
                color = 14; break;
            case "15":
            case "灰":
            case "gray":
            case "Gray":
            case "cinza":
            case "Cinza":
            case "grey":
            case "Grey":
            case "Сер":
            case "сер":
            case "Серый":
            case "серый":
                color = 15; break;
            case "16":
            case "茶":
            case "tan":
            case "Tan":
            case "bege":
            case "Bege":
            case "Загар":
            case "загар":
            case "Загаровый":
            case "загаровый":
                color = 16; break;
            case "17":
            case "珊瑚":
            case "coral":
            case "Coral":
            case "salmão":
            case "Salmão":
            case "Корал":
            case "корал":
            case "Коралл":
            case "коралл":
            case "Коралловый":
            case "коралловый":
                color = 17; break;
                
            case "18": case "隐藏": case "?": color = 18; break;
        }
        return !isHost && color == 18 ? byte.MaxValue : color is < 0 or > 18 ? byte.MaxValue : Convert.ToByte(color);
    }

    public static void ShowHelpToClient(byte ID)
    {
        SendMessage(
            GetString("CommandList")
            + $"\n  ○ /n {GetString("Command.now")}"
            + $"\n  ○ /r {GetString("Command.roles")}"
            + $"\n  ○ /m {GetString("Command.myrole")}"
            + $"\n  ○ /xf {GetString("Command.solvecover")}"
            + $"\n  ○ /l {GetString("Command.lastresult")}"
            + $"\n  ○ /win {GetString("Command.winner")}"
            + "\n\n" + GetString("CommandOtherList")
            + $"\n  ○ /color {GetString("Command.color")}"
            + $"\n  ○ /qt {GetString("Command.quit")}"
            + $"\n ○ /death {GetString("Command.death")}"
            + $"\n ○ /icons {GetString("Command.iconinfo")}"
            , ID);
    }
    public static void ShowHelp(byte ID)
    {
        SendMessage(
            GetString("CommandList")
            + $"\n  ○ /n {GetString("Command.now")}"
            + $"\n  ○ /r {GetString("Command.roles")}"
            + $"\n  ○ /m {GetString("Command.myrole")}"
            + $"\n  ○ /l {GetString("Command.lastresult")}"
            + $"\n  ○ /win {GetString("Command.winner")}"
            + "\n\n" + GetString("CommandOtherList")
            + $"\n  ○ /color {GetString("Command.color")}"
            + $"\n  ○ /rn {GetString("Command.rename")}"
            + $"\n  ○ /qt {GetString("Command.quit")}"
            + $"\n  ○ /icons {GetString("Command.iconinfo")}"
            + $"\n  ○ /death {GetString("Command.death")}"
            + "\n\n" + GetString("CommandHostList")
            + $"\n  ○ /s {GetString("Command.say")}"
            + $"\n  ○ /rn {GetString("Command.rename")}"
            + $"\n  ○ /poll {GetString("Command.Poll")}"
            + $"\n  ○ /xf {GetString("Command.solvecover")}"
            + $"\n  ○ /mw {GetString("Command.mw")}"
            + $"\n  ○ /kill {GetString("Command.kill")}"
            + $"\n  ○ /exe {GetString("Command.exe")}"
            + $"\n  ○ /level {GetString("Command.level")}"
            + $"\n  ○ /id {GetString("Command.idlist")}"
            + $"\n  ○ /qq {GetString("Command.qq")}"
            + $"\n  ○ /dump {GetString("Command.dump")}"
        //    + $"\n  ○ /iconhelp {GetString("Command.iconhelp")}"
            , ID);
    }
    public static string[] SplitMessage(this string LongMsg)
    {
        List<string> result = [];
        var lines = LongMsg.Split('\n');
        var shortenedtext = string.Empty;

        foreach (var line in lines)
        {

            if (shortenedtext.Length + line.Length < 1200)
            {
                shortenedtext += line + "\n";
                continue;
            }

            if (shortenedtext.Length >= 1200) result.AddRange(shortenedtext.Chunk(1200).Select(x => new string(x)));
            else result.Add(shortenedtext);
            shortenedtext = line + "\n";

        }

        if (shortenedtext.Length > 0) result.Add(shortenedtext);

        return [.. result];
    }
    private static string TryRemove(this string text) => text.Length >= 1200 ? text.Remove(0, 1200) : string.Empty;
    
    
    public static void SendSpesificMessage(string text, byte sendTo = byte.MaxValue, string title = "") 
    {
        // Always splits it, this is incase you want to very heavily modify msg and use the splitmsg functionality.
        bool isfirst = true;
        if (text.Length > 1200 && !(Utils.GetPlayerById(sendTo).IsModClient()))
        {
            foreach(var txt in text.SplitMessage())
            {
                var titleW = isfirst ? title : "<alpha=#00>.";
                var m = Regex.Replace(txt, "^<voffset=[-]?\\d+em>", ""); // replaces the first instance of voffset, if any.
                m += $"<voffset=-1.3em><alpha=#00>.</voffset>"; // fix text clipping OOB
                if (m.IndexOf("\n") <= 4) m = m[(m.IndexOf("\n")+1)..m.Length];
                SendMessage(m, sendTo, titleW);
                isfirst = false;
            }
        }
        else 
        {
            text += $"<voffset=-1.3em><alpha=#00>.</voffset>";
            if (text.IndexOf("\n") <= 4) text = text[(text.IndexOf("\n") + 1)..text.Length];
            SendMessage(text, sendTo, title);
        }


    }
    public static void SendMessage(string text, byte sendTo = byte.MaxValue, string title = "", bool logforChatManager = false, bool noReplay = false, bool ShouldSplit = false)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        try
        {
            if (ShouldSplit && text.Length > 1200 && (!GetPlayerById(sendTo).IsModClient()))
            {
                text.SplitMessage().Do(x => SendMessage(x, sendTo, title));
                return;
            }
            //else if (text.Length > 1200 && (!GetPlayerById(sendTo).IsModClient()))
            //{
            //    text = text.RemoveHtmlTagsIfNeccessary();
            //}
        }
        catch (Exception exx)
        {
            Logger.Warn($"Error after try split the msg {text} at: {exx}", "Utils.SendMessage.SplitMessage");
        }

        // set noReplay to false when you want to send previous sys msg or do not want to add a sys msg in the history
        if (!noReplay && GameStates.IsInGame) ChatManager.AddSystemChatHistory(sendTo, text);

        if (title == "") title = "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>";

        if (!logforChatManager)
            ChatManager.AddToHostMessage(text.RemoveHtmlTagsTemplate());

        Main.MessagesToSend.Add((text.RemoveHtmlTagsTemplate(), sendTo, title));
    }
    public static bool IsPlayerModerator(string friendCode)
    {
        if (friendCode == "") return false;
        var friendCodesFilePath = @"./TOHE-DATA/Moderators.txt";
        var friendCodes = File.ReadAllLines(friendCodesFilePath);
        return friendCodes.Any(code => code.Contains(friendCode));
    }
    public static bool IsPlayerVIP(string friendCode)
    {
        if (friendCode == "") return false;
        var friendCodesFilePath = @"./TOHE-DATA/VIP-List.txt";
        var friendCodes = File.ReadAllLines(friendCodesFilePath);
        return friendCodes.Any(code => code.Contains(friendCode));
    }
    public static bool CheckColorHex(string ColorCode)
    {
        Regex regex = new("^[0-9A-Fa-f]{6}$");
        if (!regex.IsMatch(ColorCode)) return false;
        return true;
    }
    public static bool CheckGradientCode(string ColorCode)
    {
        Regex regex = new(@"^[0-9A-Fa-f]{6}\s[0-9A-Fa-f]{6}$");
        if (!regex.IsMatch(ColorCode)) return false;
        return true;
    }
    public static string GradientColorText(string startColorHex, string endColorHex, string text)
    {
        if (startColorHex.Length != 6 || endColorHex.Length != 6)
        {
            Logger.Error("Invalid color hex code. Hex code should be 6 characters long (without #) (e.g., FFFFFF).", "GradientColorText");
            //throw new ArgumentException("Invalid color hex code. Hex code should be 6 characters long (e.g., FFFFFF).");
            return text;
        }

        Color startColor = HexToColor(startColorHex);
        Color endColor = HexToColor(endColorHex);

        int textLength = text.Length;
        float stepR = (endColor.r - startColor.r) / textLength;
        float stepG = (endColor.g - startColor.g) / textLength;
        float stepB = (endColor.b - startColor.b) / textLength;
        float stepA = (endColor.a - startColor.a) / textLength;

        string gradientText = "";

        for (int i = 0; i < textLength; i++)
        {
            float r = startColor.r + (stepR * i);
            float g = startColor.g + (stepG * i);
            float b = startColor.b + (stepB * i);
            float a = startColor.a + (stepA * i);


            string colorHex = ColorToHex(new Color(r, g, b, a));
            //Logger.Msg(colorHex, "color");
            gradientText += $"<color=#{colorHex}>{text[i]}</color>";
        }

        return gradientText;
    }

    public static Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out var color))
        {
            return color;
        }
        return Color.white;
    }

    private static string ColorToHex(Color color)
    {
        Color32 color32 = (Color32)color;
        return $"{color32.r:X2}{color32.g:X2}{color32.b:X2}{color32.a:X2}";
    }
    public static void ApplySuffix(PlayerControl player)
    {
        // Only host
        if (!AmongUsClient.Instance.AmHost || player == null) return;
        // Check invalid color
        if (player.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= player.Data.DefaultOutfit.ColorId) return;

        // Hide all tags
        if (Options.HideAllTagsAndText.GetBool())
        {
            SetRealName();
            return;
        }

        if (!(player.AmOwner || player.FriendCode.GetDevUser().HasTag()))
        {
            if (!IsPlayerModerator(player.FriendCode) && !IsPlayerVIP(player.FriendCode))
            {
                SetRealName();
                return;
            }
        }

        void SetRealName()
        {
            string realName = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var namePlayer) ? namePlayer : "";
            if (GameStates.IsLobby && realName != player.name && player.CurrentOutfitType == PlayerOutfitType.Default)
                player.RpcSetName(realName);
        }

        string name = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var n) ? n : "";
        if (Main.HostRealName != "" && player.AmOwner) name = Main.HostRealName;
        if (name == "" || !GameStates.IsLobby) return;

        if (player.AmOwner && player.IsModClient())
        {
            if (GameStates.IsOnlineGame || GameStates.IsLocalGame)
            {
                name = Options.HideHostText.GetBool() ? $"<color={GetString("NameColor")}>{name}</color>"
                                                      : $"<color={GetString("HostColor")}>{GetString("HostText")}</color><color={GetString("IconColor")}>{GetString("Icon")}</color><color={GetString("NameColor")}>{name}</color>";
            }
            if (Options.CurrentGameMode == CustomGameMode.FFA)
                name = $"<color=#00ffff><size=1.7>{GetString("ModeFFA")}</size></color>\r\n" + name;
        }

        var modtag = "";
        if (Options.ApplyVipList.GetValue() == 1 && player.FriendCode != PlayerControl.LocalPlayer.FriendCode)
        {
            if (IsPlayerVIP(player.FriendCode))
            {
                string colorFilePath = @$"./TOHE-DATA/Tags/VIP_TAGS/{player.FriendCode}.txt";
                //static color
                if (!Options.GradientTagsOpt.GetBool())
                {
                    string startColorCode = "ffff00";
                    if (File.Exists(colorFilePath))
                    {
                        string ColorCode = File.ReadAllText(colorFilePath);
                        _ = ColorCode.Trim();
                        if (CheckColorHex(ColorCode)) startColorCode = ColorCode;
                    }
                    //"ffff00"
                    modtag = $"<color=#{startColorCode}>{GetString("VipTag")}</color>";
                }
                else //gradient color
                {
                    string startColorCode = "ffff00";
                    string endColorCode = "ffff00";
                    string ColorCode = "";
                    if (File.Exists(colorFilePath))
                    {
                        ColorCode = File.ReadAllText(colorFilePath);
                        if (ColorCode.Split(" ").Length == 2)
                        {
                            startColorCode = ColorCode.Split(" ")[0];
                            endColorCode = ColorCode.Split(" ")[1];
                        }
                    }
                    if (!CheckGradientCode(ColorCode))
                    {
                        startColorCode = "ffff00";
                        endColorCode = "ffff00";
                    }
                    //"33ccff", "ff99cc"
                    if (startColorCode == endColorCode) modtag = $"<color=#{startColorCode}>{GetString("VipTag")}</color>";

                    else modtag = GradientColorText(startColorCode, endColorCode, GetString("VipTag"));
                }
            }
        }
        if (Options.ApplyModeratorList.GetValue() == 1 && player.FriendCode != PlayerControl.LocalPlayer.FriendCode)
        {
            if (IsPlayerModerator(player.FriendCode))
            {
                string colorFilePath = @$"./TOHE-DATA/Tags/MOD_TAGS/{player.FriendCode}.txt";
                //static color
                if (!Options.GradientTagsOpt.GetBool())
                {
                    string startColorCode = "8bbee0";
                    if (File.Exists(colorFilePath))
                    {
                        string ColorCode = File.ReadAllText(colorFilePath);
                        _ = ColorCode.Trim();
                        if (CheckColorHex(ColorCode)) startColorCode = ColorCode;
                    }
                    //"33ccff", "ff99cc"
                    modtag = $"<color=#{startColorCode}>{GetString("ModTag")}</color>";
                }
                else //gradient color
                {
                    string startColorCode = "8bbee0";
                    string endColorCode = "8bbee0";
                    string ColorCode = "";
                    if (File.Exists(colorFilePath))
                    {
                        ColorCode = File.ReadAllText(colorFilePath);
                        if (ColorCode.Split(" ").Length == 2)
                        {
                            startColorCode = ColorCode.Split(" ")[0];
                            endColorCode = ColorCode.Split(" ")[1];
                        }
                    }
                    if (!CheckGradientCode(ColorCode))
                    {
                        startColorCode = "8bbee0";
                        endColorCode = "8bbee0";
                    }
                    //"33ccff", "ff99cc"
                    if (startColorCode == endColorCode) modtag = $"<color=#{startColorCode}>{GetString("ModTag")}</color>";

                    else modtag = GradientColorText(startColorCode, endColorCode, GetString("ModTag"));
                }
            }
        }
        if (player.AmOwner)
        {
            name = Options.GetSuffixMode() switch
            {
                SuffixModes.TOHE => name += $"\r\n<color={Main.ModColor}>TOHE v{Main.PluginDisplayVersion}</color>",
                SuffixModes.Streaming => name += $"\r\n<size=1.7><color={Main.ModColor}>{GetString("SuffixMode.Streaming")}</color></size>",
                SuffixModes.Recording => name += $"\r\n<size=1.7><color={Main.ModColor}>{GetString("SuffixMode.Recording")}</color></size>",
                SuffixModes.RoomHost => name += $"\r\n<size=1.7><color={Main.ModColor}>{GetString("SuffixMode.RoomHost")}</color></size>",
                SuffixModes.OriginalName => name += $"\r\n<size=1.7><color={Main.ModColor}>{DataManager.player.Customization.Name}</color></size>",
                SuffixModes.DoNotKillMe => name += $"\r\n<size=1.7><color={Main.ModColor}>{GetString("SuffixModeText.DoNotKillMe")}</color></size>",
                SuffixModes.NoAndroidPlz => name += $"\r\n<size=1.7><color={Main.ModColor}>{GetString("SuffixModeText.NoAndroidPlz")}</color></size>",
                SuffixModes.AutoHost => name += $"\r\n<size=1.7><color={Main.ModColor}>{GetString("SuffixModeText.AutoHost")}</color></size>",
                _ => name
            };
        }

        if (!name.Contains($"\r\r") && player.FriendCode.GetDevUser().HasTag() && (player.AmOwner || player.IsModClient()))
        {
            name = player.FriendCode.GetDevUser().GetTag() + "<size=1.5>" + modtag + "</size>" + name;
        }
        else name = modtag + name;

        // Set name
        if (name != player.name && player.CurrentOutfitType == PlayerOutfitType.Default)
            player.RpcSetName(name);
    }
    public static bool CheckCamoflague(this PlayerControl PC) => Camouflage.IsCamouflage || Camouflager.AbilityActivated || Utils.IsActive(SystemTypes.MushroomMixupSabotage) 
        || (Main.CheckShapeshift.TryGetValue(PC.PlayerId, out bool isShapeshifitng) && isShapeshifitng);
    public static PlayerControl GetPlayerById(int PlayerId)
    {
        return Main.AllPlayerControls.FirstOrDefault(pc => pc.PlayerId == PlayerId);
    }
    public static List<PlayerControl> GetPlayerListByIds(this IEnumerable<byte> PlayerIdList)
    {
        var PlayerList = PlayerIdList?.ToList().Select(x => GetPlayerById(x)).ToList();

        return PlayerList != null && PlayerList.Any() ? PlayerList : null;
    }
    public static List<PlayerControl> GetPlayerListByRole(this CustomRoles role)
        => GetPlayerListByIds(Main.PlayerStates.Values.Where(x => x.MainRole == role).Select(r => r.PlayerId));
    public static bool IsSameTeammate(this PlayerControl player, PlayerControl target, out Custom_Team team)
    {
        team = default;
        if (player.IsAnySubRole(x => x.IsConverted()))
        {
            var Compare = player.GetCustomSubRoles().First(x => x.IsConverted());

            team = player.Is(CustomRoles.Madmate) ? Custom_Team.Impostor : Custom_Team.Neutral;
            return target.Is(Compare);
        }
        else if (!target.IsAnySubRole(x => x.IsConverted()))
        {
            team = player.GetCustomRole().GetCustomRoleTeam();
            return target.Is(team);
        }


        return false;
    }
    public static IEnumerable<t> GetRoleBasesByType <t>() where t : RoleBase
    {
        try
        {
            var cache = Main.PlayerStates.Values.Where(x => x.RoleClass != null);

            if (cache.Any())
            {
                var Get = cache.Select(x => x.RoleClass);
                return Get.OfType<t>().Any() ? Get.OfType<t>() : null;
            }
        }
        catch (Exception exx)
        {
            Logger.Exception(exx, "Utils.GetRoleBasesByType");
        }
        return null;
    }

    public static bool IsMethodOverridden(this RoleBase roleInstance, string methodName)
    {
        Type baseType = typeof(RoleBase);
        Type derivedType = roleInstance.GetType();

        MethodInfo baseMethod = baseType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        MethodInfo derivedMethod = derivedType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);

        return baseMethod.DeclaringType != derivedMethod.DeclaringType;
    }

    public static NetworkedPlayerInfo GetPlayerInfoById(int PlayerId) =>
        GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => info.PlayerId == PlayerId);
    private static readonly StringBuilder SelfSuffix = new();
    private static readonly StringBuilder SelfMark = new(20);
    private static readonly StringBuilder TargetSuffix = new();
    private static readonly StringBuilder TargetMark = new(20);
    public static async void NotifyRoles(PlayerControl SpecifySeer = null, PlayerControl SpecifyTarget = null, bool isForMeeting = false, bool NoCache = false, bool ForceLoop = true, bool CamouflageIsForMeeting = false, bool MushroomMixupIsActive = false)
    {
        if (!AmongUsClient.Instance.AmHost || GameStates.IsHideNSeek || OnPlayerLeftPatch.StartingProcessing) return;
        if (Main.AllPlayerControls == null) return;

        //Do not update NotifyRoles during meetings
        if (GameStates.IsMeeting && !GameEndCheckerForNormal.ShowAllRolesWhenGameEnd) return;

        //var caller = new System.Diagnostics.StackFrame(1, false);
        //var callerMethod = caller.GetMethod();
        //string callerMethodName = callerMethod.Name;
        //string callerClassName = callerMethod.DeclaringType.FullName;
        //Logger.Info($" Was called from: {callerClassName}.{callerMethodName}", "NotifyRoles");

        await DoNotifyRoles(SpecifySeer, SpecifyTarget, isForMeeting, NoCache, ForceLoop, CamouflageIsForMeeting, MushroomMixupIsActive);
    }
    public static Task DoNotifyRoles(PlayerControl SpecifySeer = null, PlayerControl SpecifyTarget = null, bool isForMeeting = false, bool NoCache = false, bool ForceLoop = true, bool CamouflageIsForMeeting = false, bool MushroomMixupIsActive = false)
    {
        if (!AmongUsClient.Instance.AmHost || GameStates.IsHideNSeek || OnPlayerLeftPatch.StartingProcessing) return Task.CompletedTask;
        if (Main.AllPlayerControls == null) return Task.CompletedTask;

        //Do not update NotifyRoles during meetings
        if (GameStates.IsMeeting && !GameEndCheckerForNormal.ShowAllRolesWhenGameEnd) return Task.CompletedTask;

        //var logger = Logger.Handler("DoNotifyRoles");

        HudManagerPatch.NowCallNotifyRolesCount++;
        HudManagerPatch.LastSetNameDesyncCount = 0;

        PlayerControl[] seerList = SpecifySeer != null 
            ? ([SpecifySeer]) 
            : Main.AllPlayerControls;

        PlayerControl[] targetList = SpecifyTarget != null
            ? ([SpecifyTarget])
            : Main.AllPlayerControls;

        if (!MushroomMixupIsActive)
        {
            MushroomMixupIsActive = IsActive(SystemTypes.MushroomMixupSabotage);
        }

        Logger.Info($" START - Count Seers: {seerList.Length} & Count Target: {targetList.Length}", "DoNotifyRoles");

        //seer: player who updates the nickname/role/mark
        //target: seer updates nickname/role/mark of other targets
        foreach (var seer in seerList)
        {
            // Do nothing when the seer is not present in the game
            if (seer == null) continue;
            
            // Only non-modded players
            if (seer.IsModClient()) continue;

            // During intro scene to set team name and role info for non-modded clients and skip the rest.
            // Note: When Neutral is based on the Crewmate role then it is impossible to display the info for it
            // If not a Desync Role remove team display
            if (SetUpRoleTextPatch.IsInIntro)
            {
                //Get role info font size based on the length of the role info
                static int GetInfoSize(string RoleInfo)
                {
                    RoleInfo = Regex.Replace(RoleInfo, "<[^>]*>", "");
                    RoleInfo = Regex.Replace(RoleInfo, "{[^}]*}", "");

                    var BaseFontSize = 200;
                    int BaseFontSizeMin = 100;

                    BaseFontSize -= 3 * RoleInfo.Length;
                    if (BaseFontSize < BaseFontSizeMin)
                        BaseFontSize = BaseFontSizeMin;
                    return BaseFontSize;
                }

                string IconText = "<color=#ffffff>|</color>";
                string Font = "<font=\"VCR SDF\" material=\"VCR Black Outline\">";
                string SelfTeamName = $"<size=450%>{IconText} {Font}{ColorString(GetTeamColor(seer), $"{seer.GetCustomRole().GetCustomRoleTeam()}")}</font> {IconText}</size><size=900%>\n \n</size>\r\n";
                string SelfRoleName = $"<size=185%>{Font}{ColorString(seer.GetRoleColor(), GetRoleName(seer.GetCustomRole()))}</font></size>";
                string SelfSubRolesName = string.Empty;
                string SeerRealName = seer.GetRealName();
                string SelfName = ColorString(seer.GetRoleColor(), SeerRealName);
                string RoleInfo = $"<size=25%>\n</size><size={GetInfoSize(seer.GetRoleInfo())}%>{Font}{ColorString(seer.GetRoleColor(), seer.GetRoleInfo())}</font></size>";
                string RoleNameUp = "<size=1350%>\n\n</size>";

                if (!seer.HasDesyncRole())
                {
                    SelfTeamName = string.Empty;
                    RoleNameUp = "<size=565%>\n</size>";
                    RoleInfo = $"<size=50%>\n</size><size={GetInfoSize(seer.GetRoleInfo())}%>{Font}{ColorString(seer.GetRoleColor(), seer.GetRoleInfo())}</font></size>";
                }

                // Format addons
                bool isFirstSub = true;
                foreach (var subRole in seer.GetCustomSubRoles().ToArray())
                {
                    if (isFirstSub)
                    {
                        SelfSubRolesName += $"<size=150%>\n</size=><size=125%>{Font}{ColorString(GetRoleColor(subRole), GetString($"{subRole}"))}</font></size>";
                        RoleNameUp += "\n";
                    }
                    else
                        SelfSubRolesName += $"<size=125%> {Font}{ColorString(Color.white, "+")} {ColorString(GetRoleColor(subRole), GetString($"{subRole}"))}</font></size=>";
                    isFirstSub = false;
                }

                SelfName = $"{SelfTeamName}{SelfRoleName}{SelfSubRolesName}\r\n{RoleInfo}{RoleNameUp}";

                // Privately sent name.
                seer.RpcSetNamePrivate(SelfName, seer);
                continue;
            }
            
            // Size of player roles
            string fontSize = isForMeeting ? "1.6" : "1.8";
            if (isForMeeting && (seer.GetClient().PlatformData.Platform is Platforms.Playstation or Platforms.Xbox or Platforms.Switch)) fontSize = "70%";

            //logger.Info("NotifyRoles-Loop1-" + seer.GetNameWithRole() + ":START");

            var seerRole = seer.GetCustomRole();
            var seerRoleClass = seer.GetRoleClass();

            // Hide player names in during Mushroom Mixup if seer is alive and desync impostor
            if (!CamouflageIsForMeeting && MushroomMixupIsActive && seer.IsAlive() && !seer.Is(Custom_Team.Impostor) && seer.HasDesyncRole())
            {
                seer.RpcSetNamePrivate("<size=0%>", force: NoCache);
            }
            else
            {
                // Clear marker after name seer
                SelfMark.Clear();

                // ====== Add SelfMark for seer ======
                SelfMark.Append(seerRoleClass?.GetMark(seer, seer, isForMeeting: isForMeeting));
                SelfMark.Append(CustomRoleManager.GetMarkOthers(seer, seer, isForMeeting: isForMeeting));

                if (seer.Is(CustomRoles.Lovers))
                    SelfMark.Append(ColorString(GetRoleColor(CustomRoles.Lovers), "♥"));

                if (seer.Is(CustomRoles.Cyber) && Cyber.CyberKnown.GetBool())
                    SelfMark.Append(ColorString(GetRoleColor(CustomRoles.Cyber), "★"));


                // ====== Add SelfSuffix for seer ======

                SelfSuffix.Clear();

                SelfSuffix.Append(seerRoleClass?.GetLowerText(seer, seer, isForMeeting: isForMeeting));
                SelfSuffix.Append(CustomRoleManager.GetLowerTextOthers(seer, seer, isForMeeting: isForMeeting));

                if (Radar.IsEnable)
                    SelfSuffix.Append(Radar.GetPlayerArrow(seer, isForMeeting: isForMeeting));

                SelfSuffix.Append(seerRoleClass?.GetSuffix(seer, seer, isForMeeting: isForMeeting));
                SelfSuffix.Append(CustomRoleManager.GetSuffixOthers(seer, seer, isForMeeting: isForMeeting));

                SelfSuffix.Append(Spurt.GetSuffix(seer, isformeeting: isForMeeting));


                switch (Options.CurrentGameMode)
                {
                    case CustomGameMode.FFA:
                        SelfSuffix.Append(FFAManager.GetPlayerArrow(seer));
                        break;
                }


                // ====== Get SeerRealName ======

                string SeerRealName = seer.GetRealName(isForMeeting);

                // ====== Combine SelfRoleName, SelfTaskText, SelfName, SelfDeathReason for seer ======
                string SelfTaskText = GetProgressText(seer);

                string SelfRoleName = $"<size={fontSize}>{seer.GetDisplayRoleAndSubName(seer, false)}{SelfTaskText}</size>";
                string SelfDeathReason = seer.KnowDeathReason(seer) ? $" ({ColorString(GetRoleColor(CustomRoles.Doctor), GetVitalText(seer.PlayerId))})" : string.Empty;
                string SelfName = $"{ColorString(seer.GetRoleColor(), SeerRealName)}{SelfDeathReason}{SelfMark}";

                // Add protected player icon from ShieldPersonDiedFirst
                if (seer.GetClient().GetHashedPuid() == Main.FirstDiedPrevious && MeetingStates.FirstMeeting && !isForMeeting)
                {
                    SelfName = $"{ColorString(seer.GetRoleColor(), $"<color=#4fa1ff><u></color>{SeerRealName}</u>")}{SelfDeathReason}<color=#4fa1ff>✚</color>{SelfMark}";
                }

                bool IsDisplayInfo = false;
                if (MeetingStates.FirstMeeting && Options.ChangeNameToRoleInfo.GetBool() && !isForMeeting && Options.CurrentGameMode != CustomGameMode.FFA)
                {
                    IsDisplayInfo = true;
                    var SeerRoleInfo = seer.GetRoleInfo();
                    string RoleText = string.Empty;
                    string Font = "<font=\"VCR SDF\" material=\"VCR Black Outline\">";

                    if (seerRole.IsImpostor()) { RoleText = ColorString(GetTeamColor(seer), GetString("TeamImpostor")); }
                    else if (seerRole.IsCrewmate()) { RoleText = ColorString(GetTeamColor(seer), GetString("TeamCrewmate")); }
                    else if (seerRole.IsNeutral()) { RoleText = ColorString(GetTeamColor(seer), GetString("TeamNeutral")); }
                    else if (seerRole.IsMadmate()) { RoleText = ColorString(GetTeamColor(seer), GetString("TeamMadmate")); }

                    SelfName = $"{SelfName}<size=600%>\n \n</size><size=150%>{Font}{ColorString(seer.GetRoleColor(), RoleText)}</size>\n<size=75%>{ColorString(seer.GetRoleColor(), seer.GetRoleInfo())}</size></font>\n";
                }

                if (NameNotifyManager.GetNameNotify(seer, out var name))
                {
                    SelfName = name;
                }

                if (Pelican.HasEnabled && Pelican.IsEaten(seer.PlayerId))
                    SelfName = $"{ColorString(GetRoleColor(CustomRoles.Pelican), GetString("EatenByPelican"))}";

                if (CustomRoles.Deathpact.HasEnabled() && Deathpact.IsInActiveDeathpact(seer))
                    SelfName = Deathpact.GetDeathpactString(seer);

                // Devourer
                if (CustomRoles.Devourer.HasEnabled())
                {
                    bool playerDevoured = Devourer.HideNameOfTheDevoured(seer.PlayerId);
                    if (playerDevoured && !CamouflageIsForMeeting)
                        SelfName = GetString("DevouredName");
                }

                // Dollmaster, Prevent seeing self in mushroom cloud
                if (CustomRoles.DollMaster.HasEnabled() && seerRole != CustomRoles.DollMaster)
                {
                    if (DollMaster.IsDoll(seer.PlayerId))
                        SelfName = "<size=10000%><color=#000000>■</color></size>";
                }

                // Camouflage
                if (!CamouflageIsForMeeting && Camouflage.IsCamouflage)
                    SelfName = $"<size=0%>{SelfName}</size>";

                if (!Regex.IsMatch(SelfName, seer.GetRealName()))
                    IsDisplayInfo = false;

                switch (Options.CurrentGameMode)
                {
                    case CustomGameMode.FFA:
                        FFAManager.GetNameNotify(seer, ref SelfName);
                        SelfName = $"<size={fontSize}>{SelfTaskText}</size>\r\n{SelfName}";
                        break;
                    default:
                        if (!IsDisplayInfo)
                            SelfName = SelfRoleName + "\r\n" + SelfName;
                        else
                            SelfName = "<size=425%>\n \n</size>" + SelfRoleName + "\r\n" + SelfName;
                        break;
                }
                SelfName += SelfSuffix.Length == 0 ? string.Empty : "\r\n " + SelfSuffix.ToString();

                if (!isForMeeting) SelfName += "\r\n";

                seer.RpcSetNamePrivate(SelfName, force: NoCache);
            }

            // Start run loop for target only when condition is "true"
            if (ForceLoop && (seer.Data.IsDead || !seer.IsAlive()
                || seerList.Length == 1
                || targetList.Length == 1
                || MushroomMixupIsActive
                || NoCache
                || ForceLoop))
            {
                foreach (var realTarget in targetList)
                {
                    // if the target is the seer itself, do nothing
                    if (realTarget.PlayerId == seer.PlayerId) continue;

                    var target = realTarget;

                    if (seer != target && seer != DollMaster.DollMasterTarget)
                        target = DollMaster.SwapPlayerInfo(realTarget); // If a player is possessed by the Dollmaster swap each other's controllers.

                    //logger.Info("NotifyRoles-Loop2-" + target.GetNameWithRole() + ":START");

                    // Hide player names in during Mushroom Mixup if seer is alive and desync impostor
                    if (!CamouflageIsForMeeting && MushroomMixupIsActive && target.IsAlive() && !seer.Is(Custom_Team.Impostor) && seer.HasDesyncRole())
                    {
                        realTarget.RpcSetNamePrivate("<size=0%>", seer, force: NoCache);
                    }
                    else
                    {
                        // ====== Add TargetMark for target ======

                        TargetMark.Clear();

                        TargetMark.Append(seerRoleClass?.GetMark(seer, target, isForMeeting));
                        TargetMark.Append(CustomRoleManager.GetMarkOthers(seer, target, isForMeeting));

                        if (seer.Is(Custom_Team.Impostor) && target.Is(CustomRoles.Snitch) && target.Is(CustomRoles.Madmate) && target.GetPlayerTaskState().IsTaskFinished)
                            TargetMark.Append(ColorString(GetRoleColor(CustomRoles.Impostor), "★"));

                        if (target.Is(CustomRoles.Cyber) && Cyber.CyberKnown.GetBool())
                            TargetMark.Append(ColorString(GetRoleColor(CustomRoles.Cyber), "★"));

                        if (seer.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers))
                        {
                            TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.Lovers)}>♥</color>");
                        }
                        else if (seer.Data.IsDead && !seer.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers))
                        {
                            TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.Lovers)}>♥</color>");
                        }

                        // ====== Seer know target role ======

                        bool KnowRoleTarget = ExtendedPlayerControl.KnowRoleTarget(seer, target, true);
                        
                        string TargetRoleText = KnowRoleTarget
                                ? $"<size={fontSize}>{seer.GetDisplayRoleAndSubName(target, false)}{GetProgressText(target)}</size>\r\n" : "";

                        if (seer.IsAlive() && Overseer.IsRevealedPlayer(seer, target) && target.Is(CustomRoles.Trickster))
                        {
                            TargetRoleText = Overseer.GetRandomRole(seer.PlayerId); // Random trickster role
                            TargetRoleText += TaskState.GetTaskState(); // Random task count for revealed trickster
                        }

                        // ====== Target player name ======

                        string TargetPlayerName = target.GetRealName(isForMeeting);

                        var tempNameText = seer.GetRoleClass()?.NotifyPlayerName(seer, target, TargetPlayerName, isForMeeting);
                        if (tempNameText != string.Empty)
                            TargetPlayerName = tempNameText;

                        // ========= Only During Meeting =========
                        if (isForMeeting)
                        {
                            // Guesser Mode is On ID
                            if (Options.GuesserMode.GetBool())
                            {
                                // seer & target is alive
                                if (seer.IsAlive() && target.IsAlive())
                                {
                                    var GetTragetId = ColorString(GetRoleColor(seer.GetCustomRole()), target.PlayerId.ToString()) + " " + TargetPlayerName;

                                    //Crewmates
                                    if (Options.CrewmatesCanGuess.GetBool() && seer.GetCustomRole().IsCrewmate() && !seer.Is(CustomRoles.Judge) && !seer.Is(CustomRoles.Inspector) && !seer.Is(CustomRoles.Lookout) && !seer.Is(CustomRoles.Swapper))
                                        TargetPlayerName = GetTragetId;

                                    else if (seer.Is(CustomRoles.NiceGuesser) && !Options.CrewmatesCanGuess.GetBool())
                                        TargetPlayerName = GetTragetId;



                                    //Impostors
                                    if (Options.ImpostorsCanGuess.GetBool() && (seer.GetCustomRole().IsImpostor() || seer.GetCustomRole().IsMadmate()) && !seer.Is(CustomRoles.Councillor) && !seer.Is(CustomRoles.Nemesis))
                                        TargetPlayerName = GetTragetId;

                                    else if (seer.Is(CustomRoles.EvilGuesser) && !Options.ImpostorsCanGuess.GetBool())
                                        TargetPlayerName = GetTragetId;



                                    // Neutrals
                                    if (Options.NeutralKillersCanGuess.GetBool() && seer.GetCustomRole().IsNK())
                                        TargetPlayerName = GetTragetId;

                                    if (Options.NeutralApocalypseCanGuess.GetBool() && seer.GetCustomRole().IsNA())
                                        TargetPlayerName = GetTragetId;

                                    if (Options.PassiveNeutralsCanGuess.GetBool() && seer.GetCustomRole().IsNonNK() && !seer.Is(CustomRoles.Doomsayer))
                                        TargetPlayerName = GetTragetId;
                                }
                            }
                            else // Guesser Mode is Off ID
                            {
                                if (seer.IsAlive() && target.IsAlive())
                                {
                                    if (seer.Is(CustomRoles.NiceGuesser) || seer.Is(CustomRoles.EvilGuesser) ||
                                        (seer.Is(CustomRoles.Guesser) && !seer.Is(CustomRoles.Inspector) && !seer.Is(CustomRoles.Swapper) && !seer.Is(CustomRoles.Lookout)))
                                        TargetPlayerName = ColorString(GetRoleColor(seer.GetCustomRole()), target.PlayerId.ToString()) + " " + TargetPlayerName;
                                }
                            }
                        }

                        TargetPlayerName = TargetPlayerName.ApplyNameColorData(seer, target, isForMeeting);

                        // ====== Add TargetSuffix for target (TargetSuffix visible ​​only to the seer) ======
                        TargetSuffix.Clear();

                        TargetSuffix.Append(CustomRoleManager.GetLowerTextOthers(seer, target, isForMeeting: isForMeeting));

                        TargetSuffix.Append(seerRoleClass?.GetSuffix(seer, target, isForMeeting: isForMeeting));
                        TargetSuffix.Append(CustomRoleManager.GetSuffixOthers(seer, target, isForMeeting: isForMeeting));

                        if (TargetSuffix.Length > 0)
                        {
                            TargetSuffix.Insert(0, "\r\n");
                        }

                        // ====== Target Death Reason for target (Death Reason visible ​​only to the seer) ======
                        string TargetDeathReason = seer.KnowDeathReason(target) 
                            ? $" ({ColorString(GetRoleColor(CustomRoles.Doctor), GetVitalText(target.PlayerId))})" : string.Empty;

                        // Devourer
                        if (CustomRoles.Devourer.HasEnabled())
                        {
                            bool targetDevoured = Devourer.HideNameOfTheDevoured(target.PlayerId);
                            if (targetDevoured && !CamouflageIsForMeeting)
                                TargetPlayerName = GetString("DevouredName");
                        }

                        // Camouflage
                        if (!CamouflageIsForMeeting && Camouflage.IsCamouflage)
                            TargetPlayerName = $"<size=0%>{TargetPlayerName}</size>";

                        // Target Name
                        string TargetName = $"{TargetRoleText}{TargetPlayerName}{TargetDeathReason}{TargetMark}{TargetSuffix}";
                        //TargetName += TargetSuffix.ToString() == "" ? "" : ("\r\n" + TargetSuffix.ToString());

                        // Add protected player icon from ShieldPersonDiedFirst
                        if (target.GetClient().GetHashedPuid() == Main.FirstDiedPrevious && MeetingStates.FirstMeeting && !isForMeeting && Options.ShowShieldedPlayerToAll.GetBool())
                        {
                            TargetName = $"{TargetRoleText}<color=#4fa1ff><u></color>{TargetPlayerName}</u>{TargetDeathReason}<color=#4fa1ff>✚</color>{TargetMark}{TargetSuffix}";
                        }

                        realTarget.RpcSetNamePrivate(TargetName, seer, force: NoCache);
                    }
                }
            }
        }
        //Logger.Info($" Loop for Targets: {}", "DoNotifyRoles", force: true);
        Logger.Info($" END", "DoNotifyRoles");
        return Task.CompletedTask;
    }
    public static void MarkEveryoneDirtySettings()
    {
        PlayerGameOptionsSender.SetDirtyToAll();
    }
    public static void SyncAllSettings()
    {
        PlayerGameOptionsSender.SetDirtyToAll();
        GameOptionsSender.SendAllGameOptions();
    }
    public static bool DeathReasonIsEnable(this PlayerState.DeathReason reason, bool checkbanned = false)
    {
        static bool BannedReason(PlayerState.DeathReason rso)
        {
            return rso is PlayerState.DeathReason.Overtired 
                or PlayerState.DeathReason.etc
                or PlayerState.DeathReason.Vote 
                or PlayerState.DeathReason.Gambled
                or PlayerState.DeathReason.Armageddon;
        }

        return checkbanned ? !BannedReason(reason) : reason switch
        {
            PlayerState.DeathReason.Eaten => (CustomRoles.Pelican.IsEnable()),
            PlayerState.DeathReason.Spell => (CustomRoles.Witch.IsEnable()),
            PlayerState.DeathReason.Hex => (CustomRoles.HexMaster.IsEnable()),
            PlayerState.DeathReason.Curse => (CustomRoles.CursedWolf.IsEnable()),
            PlayerState.DeathReason.Jinx => (CustomRoles.Jinx.IsEnable()),
            PlayerState.DeathReason.Shattered => (CustomRoles.Fragile.IsEnable()),
            PlayerState.DeathReason.Bite => (CustomRoles.Vampire.IsEnable()),
            PlayerState.DeathReason.Poison => (CustomRoles.Poisoner.IsEnable()),
            PlayerState.DeathReason.Bombed => (CustomRoles.Bomber.IsEnable() || CustomRoles.Burst.IsEnable()
                                || CustomRoles.Trapster.IsEnable() || CustomRoles.Fireworker.IsEnable() || CustomRoles.Bastion.IsEnable()),
            PlayerState.DeathReason.Misfire => (CustomRoles.ChiefOfPolice.IsEnable() || CustomRoles.Sheriff.IsEnable()
                                || CustomRoles.Reverie.IsEnable() || CustomRoles.Sheriff.IsEnable() || CustomRoles.Fireworker.IsEnable()
                                || CustomRoles.Hater.IsEnable() || CustomRoles.Pursuer.IsEnable() || CustomRoles.Romantic.IsEnable()),
            PlayerState.DeathReason.Torched => (CustomRoles.Arsonist.IsEnable()),
            PlayerState.DeathReason.Sniped => (CustomRoles.Sniper.IsEnable()),
            PlayerState.DeathReason.Revenge => (CustomRoles.Avanger.IsEnable() || CustomRoles.Retributionist.IsEnable()
                                || CustomRoles.Nemesis.IsEnable() || CustomRoles.Randomizer.IsEnable()),
            PlayerState.DeathReason.Quantization => (CustomRoles.Lightning.IsEnable()),
            //PlayerState.DeathReason.Overtired => (CustomRoles.Workaholic.IsEnable()),
            PlayerState.DeathReason.Ashamed => (CustomRoles.Workaholic.IsEnable()),
            PlayerState.DeathReason.PissedOff => (CustomRoles.Pestilence.IsEnable() || CustomRoles.Provocateur.IsEnable()),
            PlayerState.DeathReason.Dismembered => (CustomRoles.Butcher.IsEnable()),
            PlayerState.DeathReason.LossOfHead => (CustomRoles.Hangman.IsEnable()),
            PlayerState.DeathReason.Trialed => (CustomRoles.Judge.IsEnable() || CustomRoles.Councillor.IsEnable()),
            PlayerState.DeathReason.Infected => (CustomRoles.Infectious.IsEnable()),
            PlayerState.DeathReason.Hack => (CustomRoles.Glitch.IsEnable()),
            PlayerState.DeathReason.Pirate => (CustomRoles.Pirate.IsEnable()),
            PlayerState.DeathReason.Shrouded => (CustomRoles.Shroud.IsEnable()),
            PlayerState.DeathReason.Mauled => (CustomRoles.Werewolf.IsEnable()),
            PlayerState.DeathReason.Suicide => (CustomRoles.Unlucky.IsEnable() || CustomRoles.Ghoul.IsEnable()
                                || CustomRoles.Terrorist.IsEnable() || CustomRoles.Dictator.IsEnable()
                                || CustomRoles.Addict.IsEnable() || CustomRoles.Mercenary.IsEnable()
                                || CustomRoles.Mastermind.IsEnable() || CustomRoles.Deathpact.IsEnable()),
            PlayerState.DeathReason.FollowingSuicide => (CustomRoles.Lovers.IsEnable()),
            PlayerState.DeathReason.Execution => (CustomRoles.Jailer.IsEnable()),
            PlayerState.DeathReason.Fall => Options.LadderDeath.GetBool(),
            PlayerState.DeathReason.Sacrifice => (CustomRoles.Bodyguard.IsEnable() || CustomRoles.Revolutionist.IsEnable()
                                || CustomRoles.Hater.IsEnable()),
            PlayerState.DeathReason.Drained => CustomRoles.Puppeteer.IsEnable(),
            PlayerState.DeathReason.Trap => CustomRoles.Trapster.IsEnable(),
            PlayerState.DeathReason.Targeted => CustomRoles.Kamikaze.IsEnable(),
            PlayerState.DeathReason.Retribution => CustomRoles.Instigator.IsEnable(),
            PlayerState.DeathReason.WrongAnswer => CustomRoles.Quizmaster.IsEnable(),
            var Breason when BannedReason(Breason) => false,
            PlayerState.DeathReason.Slice => CustomRoles.Hawk.IsEnable(),
            PlayerState.DeathReason.BloodLet => CustomRoles.Bloodmoon.IsEnable(),
            PlayerState.DeathReason.Starved => CustomRoles.Baker.IsEnable(),
            PlayerState.DeathReason.Sacrificed => CustomRoles.Altruist.IsEnable(),
            PlayerState.DeathReason.Kill => true,
            _ => true,
        };
    }
    public static HashSet<Action<bool>> LateExileTask = [];
    public static void AfterMeetingTasks()
    {
        ChatManager.ClearLastSysMsg();

        if (Diseased.IsEnable) Diseased.AfterMeetingTasks();
        if (Antidote.IsEnable) Antidote.AfterMeetingTasks();

        AntiBlackout.AfterMeetingTasks();

        foreach (var playerState in Main.PlayerStates.Values.ToArray())
        {
            if (playerState.RoleClass == null) continue;

            playerState.RoleClass.AfterMeetingTasks();
            playerState.RoleClass.HasVoted = false;
        }

        //Set kill timer
        foreach (var player in Main.AllAlivePlayerControls)
        {
            player.SetKillTimer();
        }

        if (LateExileTask.Any())
        {
            LateExileTask.Do(t => t.Invoke(true));
            LateExileTask.Clear();
        }


        if (Statue.IsEnable) Statue.AfterMeetingTasks();
        if (Burst.IsEnable) Burst.AfterMeetingTasks();

        if (CustomRoles.CopyCat.HasEnabled()) CopyCat.UnAfterMeetingTasks(); // All crew hast to be before this
        

        if (Options.AirshipVariableElectrical.GetBool())
            AirshipElectricalDoors.Initialize();

        DoorsReset.ResetDoors();

        // Empty Deden bug support Empty vent after meeting
        var ventilationSystem = ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out var systemType) ? systemType.TryCast<VentilationSystem>() : null;
        if (ventilationSystem != null)
        {
            ventilationSystem.PlayersInsideVents.Clear();
            ventilationSystem.IsDirty = true;
        }
    }
    public static string ToColoredString(this CustomRoles role) => Utils.ColorString(Utils.GetRoleColor(role), Translator.GetString($"{role}"));
    public static void ChangeInt(ref int ChangeTo, int input, int max)
    {
        var tmp = ChangeTo * 10;
        tmp += input;
        ChangeTo = Math.Clamp(tmp, 0, max);
    }
    public static void CountAlivePlayers(bool sendLog = false, bool checkGameEnd = false)
    {
        int AliveImpostorCount = Main.AllAlivePlayerControls.Count(pc => pc.Is(Custom_Team.Impostor));
        if (Main.AliveImpostorCount != AliveImpostorCount)
        {
            Logger.Info("Number Impostor left: " + AliveImpostorCount, "CountAliveImpostors");
            Main.AliveImpostorCount = AliveImpostorCount;
            LastImpostor.SetSubRole();
        }

        if (sendLog)
        {
            var sb = new StringBuilder(100);
            if (Options.CurrentGameMode != CustomGameMode.FFA)
            { 
                foreach (var countTypes in EnumHelper.GetAllValues<CountTypes>())
                {
                    var playersCount = PlayersCount(countTypes);
                    if (playersCount == 0) continue;
                    sb.Append($"{countTypes}:{AlivePlayersCount(countTypes)}/{playersCount}, ");
                }
            }
            sb.Append($"All:{AllAlivePlayersCount}/{AllPlayersCount}");
            Logger.Info(sb.ToString(), "CountAlivePlayers");
        }

        if (AmongUsClient.Instance.AmHost && checkGameEnd)
            GameEndCheckerForNormal.Prefix();
    }
    public static string GetVoteName(byte num)
    {
        //  HasNotVoted = 255;
        //  MissedVote = 254;
        //  SkippedVote = 253;
        //  DeadVote = 252;

        string name = "invalid";
        var player = GetPlayerById(num);
        var playerCount = Main.AllPlayerControls.Length;
        if (num < playerCount && player != null) name = player?.GetNameWithRole();
        if (num == 252) name = "Dead";
        if (num == 253) name = "Skip";
        if (num == 254) name = "MissedVote";
        if (num == 255) name = "HasNotVoted";
        return name;
    }
    public static string PadRightV2(this object text, int num)
    {
        int bc = 0;
        var t = text.ToString();
        foreach (char c in t) bc += Encoding.GetEncoding("UTF-8").GetByteCount(c.ToString()) == 1 ? 1 : 2;
        return t?.PadRight(Mathf.Max(num - (bc - t.Length), 0));
    }
    public static void DumpLog()
    {
        string f = $"{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}/TOHE-logs/";
        string t = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
        string filename = $"{f}TOHE-v{Main.PluginVersion}-{t}.log";
        if (!Directory.Exists(f)) Directory.CreateDirectory(f);
        FileInfo file = new(@$"{Environment.CurrentDirectory}/BepInEx/LogOutput.log");
        file.CopyTo(@filename);

        if (PlayerControl.LocalPlayer != null)
            HudManager.Instance?.Chat?.AddChat(PlayerControl.LocalPlayer, string.Format(GetString("Message.DumpfileSaved"), $"TOHE - v{Main.PluginVersion}-{t}.log"));

        SendMessage(string.Format(GetString("Message.DumpcmdUsed"), PlayerControl.LocalPlayer.GetNameWithRole()));

        ProcessStartInfo psi = new("Explorer.exe") { Arguments = "/e,/select," + @filename.Replace("/", "\\") };
        Process.Start(psi);
    }
    
    
    public static string SummaryTexts(byte id, bool disableColor = true, bool check = false)
    {
        var name = Main.AllPlayerNames[id].RemoveHtmlTags().Replace("\r\n", string.Empty);
        if (id == PlayerControl.LocalPlayer.PlayerId) name = DataManager.player.Customization.Name;
        else name = GetPlayerById(id)?.Data.PlayerName ?? name;

        var taskState = Main.PlayerStates?[id].TaskState;

        Main.PlayerStates.TryGetValue(id, out var playerState);

        string TaskCount;

        if (taskState.hasTasks)
        {
            Color CurrentСolor;
            var TaskCompleteColor = Color.green; // Color after task completion
            var NonCompleteColor = taskState.CompletedTasksCount > 0 ? Color.yellow : Color.white; // Uncountable out of person is white

            if (Workhorse.IsThisRole(id))
                NonCompleteColor = Workhorse.RoleColor;

            CurrentСolor = taskState.IsTaskFinished ? TaskCompleteColor : NonCompleteColor;

            if (playerState.MainRole is CustomRoles.Crewpostor)
                CurrentСolor = Color.red;

            if (playerState.SubRoles.Contains(CustomRoles.Workhorse))
                GetRoleColor(playerState.MainRole).ShadeColor(0.5f);

            TaskCount = ColorString(CurrentСolor, $" ({taskState.CompletedTasksCount}/{taskState.AllTasksCount})");
        }
        else { TaskCount = GetProgressText(id); }

        var disconnectedText = playerState.deathReason != PlayerState.DeathReason.etc && playerState.Disconnected ? $"({GetString("Disconnected")})" : string.Empty;
        string summary = $"{ColorString(Main.PlayerColors[id], name)} - {GetDisplayRoleAndSubName(id, id, true)}{GetSubRolesText(id, summary: true)}{TaskCount} {GetKillCountText(id)} 『{GetVitalText(id, true)}』{disconnectedText}";
        switch (Options.CurrentGameMode)
        {
            case CustomGameMode.FFA:
                summary = $"{ColorString(Main.PlayerColors[id], name)} {GetKillCountText(id, ffa: true)}";
                break;
        }
        return check && GetDisplayRoleAndSubName(id, id, true).RemoveHtmlTags().Contains("INVALID:NotAssigned")
            ? "INVALID"
            : disableColor ? summary.RemoveHtmlTags() : summary;
    }
    public static string RemoveHtmlTagsTemplate(this string str) => Regex.Replace(str, "", "");
    public static string RemoveHtmlTags(this string str) => Regex.Replace(str, "<[^>]*?>", "");
    public static string RemoveHtmlTagsIfNeccessary(this string str) => str.Replace("<color=", "<").Length > 1200 ? str.RemoveHtmlTags() : str.Replace("<color=", "<");

    public static void FlashColor(Color color, float duration = 1f)
    {
        var hud = DestroyableSingleton<HudManager>.Instance;
        if (hud.FullScreen == null) return;
        var obj = hud.transform.FindChild("FlashColor_FullScreen")?.gameObject;
        if (obj == null)
        {
            obj = UnityEngine.Object.Instantiate(hud.FullScreen.gameObject, hud.transform);
            obj.name = "FlashColor_FullScreen";
        }
        hud.StartCoroutine(Effects.Lerp(duration, new Action<float>((t) =>
        {
            obj.SetActive(t != 1f);
            obj.GetComponent<SpriteRenderer>().color = new(color.r, color.g, color.b, Mathf.Clamp01((-2f * Mathf.Abs(t - 0.5f) + 1) * color.a / 2)); //アルファ値を0→目標→0に変化させる
        })));
    }

    public static Dictionary<string, Sprite> CachedSprites = [];
    public static Sprite LoadSprite(string path, float pixelsPerUnit = 1f)
    {
        try
        {
            if (CachedSprites.TryGetValue(path + pixelsPerUnit, out var sprite)) return sprite;
            Texture2D texture = LoadTextureFromResources(path);
            sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            return CachedSprites[path + pixelsPerUnit] = sprite;
        }
        catch
        {
            Logger.Error($"Failed to read Texture： {path}", "LoadSprite");
        }
        return null;
    }
    public static Texture2D LoadTextureFromResources(string path)
    {
        try
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            using MemoryStream ms = new();
            stream.CopyTo(ms);
            ImageConversion.LoadImage(texture, ms.ToArray(), false);
            return texture;
        }
        catch
        {
            Logger.Error($"Failed to read Texture： {path}", "LoadTextureFromResources");
        }
        return null;
    }
    public static string ColorString(Color32 color, string str) => $"<color=#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>{str}</color>";
    public static string ColorStringWithoutEnding(Color32 color, string str) => $"<color=#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>{str}";
    /// <summary>
    /// Darkness:１の比率で黒色と元の色を混ぜる。マイナスだと白色と混ぜる。
    /// </summary>
    public static Color ShadeColor(this Color color, float Darkness = 0)
    {
        bool IsDarker = Darkness >= 0; //黒と混ぜる
        if (!IsDarker) Darkness = -Darkness;
        float Weight = IsDarker ? 0 : Darkness; //黒/白の比率
        float R = (color.r + Weight) / (Darkness + 1);
        float G = (color.g + Weight) / (Darkness + 1);
        float B = (color.b + Weight) / (Darkness + 1);
        return new Color(R, G, B, color.a);
    }

    public static void SetChatVisibleForEveryone()
    {
        if (!GameStates.IsInGame || !AmongUsClient.Instance.AmHost) return;
        
        MeetingHud.Instance = UnityEngine.Object.Instantiate(HudManager.Instance.MeetingPrefab);
        MeetingHud.Instance.ServerStart(PlayerControl.LocalPlayer.PlayerId);
        AmongUsClient.Instance.Spawn(MeetingHud.Instance, -2, SpawnFlags.None);
        MeetingHud.Instance.RpcClose();
    }

    public static void SetChatVisibleSpecific(this PlayerControl player)
    {
        if (!GameStates.IsInGame || !AmongUsClient.Instance.AmHost || GameStates.IsMeeting) return;

        if (player.AmOwner)
        {
            HudManager.Instance.Chat.SetVisible(true);
            return;
        }

        if (player.IsModClient())
        {
            var modsend = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ShowChat, SendOption.Reliable, player.OwnerId);
            modsend.WritePacked(player.OwnerId);
            modsend.Write(true);
            AmongUsClient.Instance.FinishRpcImmediately(modsend);
            return;
        }

        var customNetId = AmongUsClient.Instance.NetIdCnt++;
        var vanillasend = MessageWriter.Get(SendOption.Reliable);
        vanillasend.StartMessage(6);
        vanillasend.Write(AmongUsClient.Instance.GameId);
        vanillasend.Write(player.OwnerId);

        vanillasend.StartMessage((byte)GameDataTag.SpawnFlag);
        vanillasend.WritePacked(1); // 1 Meeting Hud Spawn id
        vanillasend.WritePacked(-2); // Owned by host
        vanillasend.Write((byte)SpawnFlags.None);
        vanillasend.WritePacked(1);
        vanillasend.WritePacked(customNetId);

        vanillasend.StartMessage(1);
        vanillasend.WritePacked(0);
        vanillasend.EndMessage();

        vanillasend.EndMessage();
        vanillasend.EndMessage();

        vanillasend.StartMessage(6);
        vanillasend.Write(AmongUsClient.Instance.GameId);
        vanillasend.Write(player.OwnerId);
        vanillasend.StartMessage((byte)GameDataTag.RpcFlag);
        vanillasend.WritePacked(customNetId);
        vanillasend.Write((byte)RpcCalls.CloseMeeting);
        vanillasend.EndMessage();
        vanillasend.EndMessage();

        AmongUsClient.Instance.SendOrDisconnect(vanillasend);
        vanillasend.Recycle();
    }

    public static int AllPlayersCount => Main.PlayerStates.Values.Count(state => state.countTypes != CountTypes.OutOfGame);
    public static int AllAlivePlayersCount => Main.AllAlivePlayerControls.Count(pc => !pc.Is(CountTypes.OutOfGame));
    public static bool IsAllAlive => Main.PlayerStates.Values.All(state => state.countTypes == CountTypes.OutOfGame || !state.IsDead);
    public static int PlayersCount(CountTypes countTypes) => Main.PlayerStates.Values.Count(state => state.countTypes == countTypes);
    public static int AlivePlayersCount(CountTypes countTypes) => Main.AllAlivePlayerControls.Count(pc => pc.Is(countTypes));
}
