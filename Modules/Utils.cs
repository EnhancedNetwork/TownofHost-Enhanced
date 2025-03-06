using AmongUs.Data;
using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Generator.Extensions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
using TOHE.Patches;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
using TOHE.Roles.Coven;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[Obfuscation(Exclude = true, Feature = "renaming", ApplyToMembers = true)]
public static class Utils
{
    private static readonly DateTime timeStampStartTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static long TimeStamp => (long)(DateTime.Now.ToUniversalTime() - timeStampStartTime).TotalSeconds;
    public static long GetTimeStamp(DateTime? dateTime = null) => (long)((dateTime ?? DateTime.Now).ToUniversalTime() - timeStampStartTime).TotalSeconds;

    // Should happen before EndGame messages is sent
    public static void NotifyGameEnding()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var player in Main.AllPlayerControls.Where(x => x.GetClient() != null && !x.Data.Disconnected))
        {
            var writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SendChat, ExtendedPlayerControl.RpcSendOption, player.OwnerId);
            writer.Write(GetString("NotifyGameEnding"));
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        var writer2 = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SendChat, SendOption.Reliable, -1);
        writer2.Write(GetString("NotifyGameEnding"));
        AmongUsClient.Instance.FinishRpcImmediately(writer2);
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

        var mapName = GetActiveMapName();
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
                    if (mapName is MapNames.Fungle) return false; // if The Fungle return false
                    var SwitchSystem = ShipStatus.Instance.Systems[type].CastFast<SwitchSystem>();
                    return SwitchSystem != null && SwitchSystem.IsActive;
                }
            case SystemTypes.Reactor:
                {
                    if (mapName is MapNames.Polus) return false; // if Polus return false
                    else
                    {
                        var ReactorSystemType = ShipStatus.Instance.Systems[type].CastFast<ReactorSystemType>();
                        return ReactorSystemType != null && ReactorSystemType.IsActive;
                    }
                }
            case SystemTypes.Laboratory:
                {
                    if (mapName is not MapNames.Polus) return false; // Only Polus
                    var ReactorSystemType = ShipStatus.Instance.Systems[type].CastFast<ReactorSystemType>();
                    return ReactorSystemType != null && ReactorSystemType.IsActive;
                }
            case SystemTypes.HeliSabotage:
                {
                    if (mapName is not MapNames.Airship) return false; // Only Airhip
                    var HeliSabotageSystem = ShipStatus.Instance.Systems[type].CastFast<HeliSabotageSystem>();
                    return HeliSabotageSystem != null && HeliSabotageSystem.IsActive;
                }
            case SystemTypes.LifeSupp:
                {
                    if (mapName is MapNames.Polus or MapNames.Airship or MapNames.Fungle) return false; // Only Skeld & Dleks & Mira HQ
                    var LifeSuppSystemType = ShipStatus.Instance.Systems[type].CastFast<LifeSuppSystemType>();
                    return LifeSuppSystemType != null && LifeSuppSystemType.IsActive;
                }
            case SystemTypes.Comms:
                {
                    if (mapName is MapNames.Mira or MapNames.Fungle) // Only Mira HQ & The Fungle
                    {
                        var HqHudSystemType = ShipStatus.Instance.Systems[type].CastFast<HqHudSystemType>();
                        return HqHudSystemType != null && HqHudSystemType.IsActive;
                    }
                    else
                    {
                        var HudOverrideSystemType = ShipStatus.Instance.Systems[type].CastFast<HudOverrideSystemType>();
                        return HudOverrideSystemType != null && HudOverrideSystemType.IsActive;
                    }
                }
            case SystemTypes.MushroomMixupSabotage:
                {
                    if (mapName is not MapNames.Fungle) return false; // Only The Fungle
                    var MushroomMixupSabotageSystem = ShipStatus.Instance.Systems[type].CastFast<MushroomMixupSabotageSystem>();
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
        if (player.IsHost())
        {
            FlashColor(new(1f, 0f, 0f, 0.3f));
            if (Constants.ShouldPlaySfx()) RPC.PlaySound(player.PlayerId, playKillSound ? Sounds.KillSound : Sounds.SabotageSound);
        }
        else if (player.IsNonHostModdedClient())
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
    public static string GetDisplayRoleAndSubName(byte seerId, byte targetId, bool isMeeting, bool notShowAddOns = false)
    {
        var TextData = GetRoleAndSubText(seerId, targetId, isMeeting, notShowAddOns);
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
    public static void SendRPC(CustomRPC rpc, params object[] data)
    {
        var w = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)rpc, SendOption.Reliable);
        foreach (var o in data)
        {
            switch (o)
            {
                case byte b:
                    w.Write(b);
                    break;
                case int i:
                    w.WritePacked(i);
                    break;
                case float f:
                    w.Write(f);
                    break;
                case string s:
                    w.Write(s);
                    break;
                case bool b:
                    w.Write(b);
                    break;
                case long l:
                    w.Write(l.ToString());
                    break;
                case char c:
                    w.Write(c.ToString());
                    break;
                case Vector2 v:
                    w.Write(v);
                    break;
                case Vector3 v:
                    w.Write(v);
                    break;
                case PlayerControl pc:
                    w.WriteNetObject(pc);
                    break;
                default:
                    try
                    {
                        if (o != null && Enum.TryParse(o.GetType(), o.ToString(), out var e) && e != null)
                            w.WritePacked((int)e);
                    }
                    catch (InvalidCastException e)
                    {
                        ThrowException(e);
                    }
                    break;
            }
        }

        AmongUsClient.Instance.FinishRpcImmediately(w);
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

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncGeneralOptions, SendOption.Reliable);
        writer.Write(player.PlayerId);
        writer.WritePacked((int)player.GetCustomRole());
        writer.Write(Main.PlayerStates[player.PlayerId].IsDead);
        writer.Write(Main.PlayerStates[player.PlayerId].Disconnected);
        writer.WritePacked((int)Main.PlayerStates[player.PlayerId].deathReason);
        writer.Write(Main.AllPlayerKillCooldown[player.PlayerId]);
        writer.Write(Main.AllPlayerSpeed[player.PlayerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SyncSpeed(this PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || !GameStates.IsInGame) return;

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncSpeedPlayer, SendOption.Reliable);
        writer.Write(player.PlayerId);
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
            case Custom_Team.Coven:
                hexColor = "#ac42f2";
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
    public static (string, Color) GetRoleAndSubText(byte seerId, byte targetId, bool isMeeting, bool notShowAddOns = false)
    {
        var RoleText = new StringBuilder("Invalid Role");
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

        RoleText.Clear().Append(GetRoleName(targetMainRole));
        RoleColor = GetRoleColor(targetMainRole);

        try
        {
            if (targetSubRoles.Any())
            {
                var seer = GetPlayerById(seerId);
                var target = GetPlayerById(targetId);

                if (seer == null || target == null) return (RoleText.ToString(), RoleColor);

                var oldRoleText = RoleText.ToString();

                // if player last imp
                if (LastImpostor.currentId == targetId)
                {
                    RoleText.Clear().Append(GetRoleString("Last-") + oldRoleText);
                }

                if (Options.NameDisplayAddons.GetBool() && !notShowAddOns)
                {
                    var seerPlatform = seer.GetClient()?.PlatformData.Platform;
                    var addBracketsToAddons = Options.AddBracketsToAddons.GetBool();

                    static string GetAddOnName(string str, bool meeting, bool addBracketsToAddons)
                    {
                        switch ((Options.ShortAddOnNamesMode)Options.ShowShortNamesForAddOns.GetValue())
                        {
                            case Options.ShortAddOnNamesMode.ShortAddOnNamesMode_Always:
                            case Options.ShortAddOnNamesMode.ShortAddOnNamesMode_OnlyInMeeting when meeting:
                            case Options.ShortAddOnNamesMode.ShortAddOnNamesMode_OnlyInGame when !meeting:
                                return addBracketsToAddons ? $"({GetString(str)[0]}) " : $"{GetString(str)[0]} ";
                            default:
                                return addBracketsToAddons ? $"({GetString(str)}) " : $"{GetString(str)} ";
                        }
                    }

                    oldRoleText = RoleText.ToString();
                    // if the player is playing on a console platform
                    if (seerPlatform is Platforms.Playstation or Platforms.Xbox or Platforms.Switch)
                    {
                        // By default, censorship is enabled on consoles
                        // Need to set add-ons colors without endings "</color>"

                        // colored role
                        RoleText.Clear().Append(ColorString(GetRoleColor(targetMainRole), oldRoleText, withoutEnding: true));

                        // colored add-ons
                        foreach (var subRole in targetSubRoles.Where(subRole => subRole.ShouldBeDisplayed() && seer.ShowSubRoleTarget(target, subRole)).ToArray())
                        {
                            oldRoleText = RoleText.ToString();
                            RoleText.Clear().Append(ColorString(GetRoleColor(subRole), GetAddOnName(subRole.ToString(), isMeeting, addBracketsToAddons), withoutEnding: true) + oldRoleText);
                        }
                    }
                    // default
                    else
                    {
                        foreach (var subRole in targetSubRoles.Where(subRole => subRole.ShouldBeDisplayed() && seer.ShowSubRoleTarget(target, subRole)).ToArray())
                        {
                            oldRoleText = RoleText.ToString();
                            RoleText.Clear().Append(ColorString(GetRoleColor(subRole), GetAddOnName(subRole.ToString(), isMeeting, addBracketsToAddons)) + oldRoleText);
                        }
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
                            case CustomRoles.Enchanted:
                                RoleColor = GetRoleColor(subRole);
                                oldRoleText = RoleText.ToString();
                                RoleText.Clear().Append(GetRoleString($"{subRole}-") + oldRoleText);
                                break;

                        }
                }
            }

            return (RoleText.ToString(), RoleColor);
        }
        catch
        {
            return (RoleText.ToString(), RoleColor);
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

    public static (RoleTypes RoleType, CustomRoles CustomRole) GetRoleMap(byte seerId, byte targetId = byte.MaxValue)
    {
        if (targetId == byte.MaxValue) targetId = seerId;

        return RpcSetRoleReplacer.RoleMap.GetValueOrDefault((seerId, targetId), (RoleTypes.CrewmateGhost, CustomRoles.NotAssigned));
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
                case CustomRoles.Enchanted:
                case CustomRoles.Rascal:
                    hasTasks &= !ForRecompute;
                    break;
                case CustomRoles.Mundane:
                    if (!hasTasks) hasTasks = !ForRecompute;
                    break;

            }

        if (CopyCat.NoHaveTask(playerData.PlayerId, ForRecompute)) hasTasks = false;
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

                if (ProgressText.Length > 0)
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
    public static string GetAbilityUseLimitDisplay(byte playerId, bool displayOnlyUseAbility, bool usingAbility = false)
    {
        try
        {
            float limit = playerId.GetAbilityUseLimit();
            if (float.IsNaN(limit)) return string.Empty;
            Color TextColor;
            if (limit < 1) TextColor = displayOnlyUseAbility ? Color.gray : Color.red;
            else if (usingAbility) TextColor = Color.green;
            else TextColor = GetRoleColor(Main.PlayerStates[playerId].MainRole).ShadeColor(0.25f);
            return (displayOnlyUseAbility ? string.Empty : ColorString(Color.white, " - ")) + ColorString(TextColor, $"({Math.Round(limit, 1)})");
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string GetTaskCount(byte playerId, bool comms)
    {
        try
        {
            if (playerId == 0 && Main.EnableGM.Value) return string.Empty;

            var taskState = Main.PlayerStates[playerId].TaskState;
            if (!taskState.hasTasks) return string.Empty;

            var info = GetPlayerInfoById(playerId);
            var TaskCompleteColor = HasTasks(info) ? Color.green : GetRoleColor(Main.PlayerStates[playerId].MainRole).ShadeColor(0.5f);
            var NonCompleteColor = HasTasks(info) ? Color.yellow : Color.white;

            if (Workhorse.IsThisRole(playerId))
                NonCompleteColor = Workhorse.RoleColor;

            var NormalColor = taskState.IsTaskFinished ? TaskCompleteColor : NonCompleteColor;
            if (Main.PlayerStates.TryGetValue(playerId, out var ps))
            {
                NormalColor = ps.MainRole switch
                {
                    CustomRoles.Crewpostor => Color.red,
                    _ => NormalColor
                };
            }

            Color TextColor = comms ? Color.gray : NormalColor;
            string Completed = comms ? "?" : $"{taskState.CompletedTasksCount}";
            return ColorString(TextColor, $"({Completed}/{taskState.AllTasksCount})");
        }
        catch
        {
            return string.Empty;
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
        List<string> covenb = [];
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
                else if (role.IsCoven()) covenb.Add(roleDisplay);
            }
        }

        impsb.Sort();
        crewsb.Sort();
        neutralsb.Sort();
        covenb.Sort();
        addonsb.Sort();

        SendMessage(string.Join("\n", impsb), PlayerId, ColorString(GetRoleColor(CustomRoles.Impostor), GetString("ImpostorRoles")), ShouldSplit: true);
        SendMessage(string.Join("\n", crewsb), PlayerId, ColorString(GetRoleColor(CustomRoles.Crewmate), GetString("CrewmateRoles")), ShouldSplit: true);
        SendMessage(string.Join("\n", neutralsb), PlayerId, ColorString(new Color32(127, 140, 141, byte.MaxValue), GetString("NeutralRoles")), ShouldSplit: true);
        SendMessage(string.Join("\n", covenb), PlayerId, ColorString(GetRoleColor(CustomRoles.Coven), GetString("CovenRoles")), ShouldSplit: true);
        SendMessage(string.Join("\n", addonsb), PlayerId, ColorString(new Color32(255, 154, 206, byte.MaxValue), GetString("AddonRoles")), ShouldSplit: true);
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
    public static void ShowLastRoles(byte PlayerId = byte.MaxValue, bool sendMessage = true)
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
            case CustomGameMode.SpeedRun:
                sb.Clear();
                sb.Append(SpeedRun.GetGameState(forGameEnd: true));
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
        if (sendMessage)
        {
            try
            {
                SendMessage("\n", PlayerId, $"<size=75%>{sb}</size>");
            }
            catch (Exception err)
            {
                Logger.Warn($"Error after try split the msg {sb} at: {err}", "Utils.ShowLastRoles..LastRoles");
            }
        }
        else
            Main.LastSummaryMessage = $"<size=75%>{sb}</size>";
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
            var tytul = !Options.OldKillLog.GetBool() ? ColorString(new Color32(102, 16, 16, 255), "《 " + GetString("KillLog") + " 》") : "";
            SendSpesificMessage(kl, PlayerId, tytul);
        }
        if (EndGamePatch.MainRoleLog != "")
        {
            SendSpesificMessage(EndGamePatch.MainRoleLog, PlayerId);
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
            if (summary && role is CustomRoles.Madmate or CustomRoles.Charmed or CustomRoles.Recruit or CustomRoles.Admired or CustomRoles.Infected or CustomRoles.Contagious or CustomRoles.Soulless or CustomRoles.Enchanted) continue;

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
            StackTrace st = new(1, true);
            StackFrame[] stFrames = st.GetFrames();

            StackFrame firstFrame = stFrames.FirstOrDefault();

            var sb = new StringBuilder();
            sb.Append($" {ex.GetType().Name}: {ex.Message}\n      thrown by {ex.Source}\n      at {ex.TargetSite}\n      in {fileName.Split('\\')[^1]}\n      at line {lineNumber}\n      in method \"{callerMemberName}\"\n------ Method Stack Trace ------");

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

                sb.Append($"\n      at {callerClassName}.{callerMethodName}");
            }

            sb.Append("\n------ End of Method Stack Trace ------");
            sb.Append("\n------ Exception ------\n   ");

            sb.Append(ex.StackTrace?.Replace("\r\n", "\n").Replace("\\n", "\n").Replace("\n", "\n   "));

            sb.Append("\n------ End of Exception ------");
            sb.Append("\n------ Exception Stack Trace ------\n");

            var stEx = new StackTrace(ex, true);
            var stFramesEx = stEx.GetFrames();

            foreach (StackFrame sf in stFramesEx)
            {
                var callerMethod = sf.GetMethod();

                string callerMethodName = callerMethod?.Name;
                string callerClassName = callerMethod?.DeclaringType?.FullName;

                sb.Append($"\n      at {callerClassName}.{callerMethodName} in {sf.GetFileName()}, line {sf.GetFileLineNumber()}");
            }

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

        try { color = int.Parse(text); }
        catch { color = -1; }

        color = text switch
        {
            "0" or "红" or "紅" or "red" or "Red" or "крас" or "Крас" or "красн" or "Красн" or "красный" or "Красный" or "Vermelho" or "vermelho" => 0,
            "1" or "蓝" or "藍" or "深蓝" or "blue" or "Blue" or "син" or "Син" or "синий" or "Синий" or "Azul" or "azul" => 1,
            "2" or "绿" or "綠" or "深绿" or "green" or "Green" or "Зел" or "зел" or "Зелёный" or "Зеленый" or "зелёный" or "зеленый" or "Verde" or "verde" or "Verde-Escuro" or "verde-escuro" => 2,
            "3" or "粉红" or "pink" or "Pink" or "Роз" or "роз" or "Розовый" or "розовый" or "Rosa" or "rosa" => 3,
            "4" or "橘" or "orange" or "Orange" or "оранж" or "Оранж" or "оранжевый" or "Оранжевый" or "Laranja" or "laranja" => 4,
            "5" or "黄" or "黃" or "yellow" or "Yellow" or "Жёлт" or "Желт" or "жёлт" or "желт" or "Жёлтый" or "Желтый" or "жёлтый" or "желтый" or "Amarelo" or "amarelo" => 5,
            "6" or "黑" or "black" or "Black" or "Чёрный" or "Черный" or "чёрный" or "черный" or "Preto" or "preto" => 6,
            "7" or "白" or "white" or "White" or "Белый" or "белый" or "Branco" or "branco" => 7,
            "8" or "紫" or "purple" or "Purple" or "Фиол" or "фиол" or "Фиолетовый" or "фиолетовый" or "Roxo" or "roxo" => 8,
            "9" or "棕" or "brown" or "Brown" or "Корич" or "корич" or "Коричневый" or "коричевый" or "Marrom" or "marrom" => 9,
            "10" or "青" or "cyan" or "Cyan" or "Голуб" or "голуб" or "Голубой" or "голубой" or "Ciano" or "ciano" => 10,
            "11" or "黄绿" or "黃綠" or "浅绿" or "lime" or "Lime" or "Лайм" or "лайм" or "Лаймовый" or "лаймовый" or "Lima" or "lima" or "Verde-Claro" or "verde-claro" => 11,
            "12" or "红褐" or "紅褐" or "深红" or "maroon" or "Maroon" or "Борд" or "борд" or "Бордовый" or "бордовый" or "Bordô" or "bordô" or "Vinho" or "vinho" => 12,
            "13" or "玫红" or "玫紅" or "浅粉" or "rose" or "Rose" or "Светло роз" or "светло роз" or "Светло розовый" or "светло розовый" or "Сирень" or "сирень" or "Сиреневый" or "сиреневый" or "Rosê" or "rosê" or "rosinha" or "Rosinha" or "Rosa-Claro" or "rosa-claro" => 13,
            "14" or "焦黄" or "焦黃" or "淡黄" or "banana" or "Banana" or "Банан" or "банан" or "Банановый" or "банановый" or "Amarelo-Claro" or "amarelo-claro" => 14,
            "15" or "灰" or "gray" or "Gray" or "Сер" or "сер" or "Серый" or "серый" or "Cinza" or "cinza" => 15,
            "16" or "茶" or "tan" or "Tan" or "Загар" or "загар" or "Загаровый" or "загаровый" or "bege" or "bege" or "Creme" or "creme" => 16,
            "17" or "珊瑚" or "coral" or "Coral" or "Корал" or "корал" or "Коралл" or "коралл" or "Коралловый" or "коралловый" => 17,
            "18" or "隐藏" or "?" or "Fortegreen" or "fortegreen" or "Фортегрин" or "фортегрин" => 18,
            _ => color
        };

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
            + $"\n  ○ /start {GetString("Command.start")}"
        //    + $"\n  ○ /iconhelp {GetString("Command.iconhelp")}"
            , ID);
    }
    public static string[] SplitMessage(this string LongMsg)
    {
        List<string> result = [];
        var lines = LongMsg.Split('\n');
        var shortenedText = string.Empty;

        foreach (var line in lines)
        {
            if (shortenedText.Length + line.Length < 1200)
            {
                shortenedText += line + "\n";
                continue;
            }

            if (shortenedText.Length >= 1200) result.AddRange(shortenedText.Chunk(1200).Select(x => new string(x)));
            else result.Add(shortenedText);

            var sentText = shortenedText;
            shortenedText = line + "\n";

            if (Regex.Matches(sentText, "<size").Count > Regex.Matches(sentText, "</size>").Count)
            {
                var sizeTag = Regex.Matches(sentText, @"<size=\d+\.?\d*%?>")[^1].Value;
                shortenedText = sizeTag + shortenedText;
            }
        }

        if (shortenedText.Length > 0) result.Add(shortenedText);

        return [.. result];
    }

    public static void SendSpesificMessage(string text, byte sendTo = byte.MaxValue, string title = "")
    {
        // Always splits it, this is incase you want to very heavily modify msg and use the splitmsg functionality.
        bool isfirst = true;
        if (text.Length > 1200 && !GetPlayerById(sendTo).IsModded())
        {
            foreach (var txt in text.SplitMessage())
            {
                var titleW = isfirst ? title : "<alpha=#00>.";
                var m = Regex.Replace(txt, "^<voffset=[-]?\\d+em>", ""); // replaces the first instance of voffset, if any.
                m += $"<voffset=-1.3em><alpha=#00>.</voffset>"; // fix text clipping OOB
                if (m.IndexOf("\n") <= 4) m = m[(m.IndexOf("\n") + 1)..m.Length];
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
        if (title == "") title = "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>";
        if (title.Count(x => x == '\u2605') == 2 && !title.Contains('\n'))
        {
            if (title.Contains('<') && title.Contains('>') && title.Contains('#'))
                title = $"{title[..(title.IndexOf('>') + 1)]}\u27a1{title.Replace("\u2605", "")[..(title.LastIndexOf('<') - 2)]}\u2b05";
            else title = "\u27a1" + title.Replace("\u2605", "") + "\u2b05";
        }

        text = text.Replace("color=", string.Empty);

        try
        {
            if (ShouldSplit && text.Length > 1200)
            {
                text.SplitMessage().Do(x => SendMessage(x, sendTo, title, logforChatManager, noReplay, false));
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
    public static string GetColoredTextByRole(this CustomRoles role, string text)
    {
        return ColorString(GetRoleColor(role), text);
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
            if (!IsPlayerModerator(player.FriendCode) && !IsPlayerVIP(player.FriendCode) && !TagManager.CheckFriendCode(player.FriendCode, false))
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

        if (player.IsHost())
        {
            if (GameStates.IsOnlineGame || GameStates.IsLocalGame)
            {
                name = Options.HideHostText.GetBool() ? $"<color={GetString("NameColor")}>{name}</color>"
                                                      : $"<color={GetString("HostColor")}>{GetString("HostText")}</color><color={GetString("IconColor")}>{GetString("Icon")}</color><color={GetString("NameColor")}>{name}</color>";
            }
            if (Options.CurrentGameMode == CustomGameMode.FFA)
                name = $"<color=#00ffff><size=1.7>{GetString("ModeFFA")}</size></color>\r\n" + name;
            if (Options.CurrentGameMode == CustomGameMode.SpeedRun)
                name = $"<color=#fffb00><size=1.7>{GetString("ModeSpeedRun")}</size></color>\r\n" + name;
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
        if (player.FriendCode != PlayerControl.LocalPlayer.FriendCode && TagManager.CheckFriendCode(player.FriendCode, false))
        {
            if ((TagManager.ReadTagColor(player.FriendCode) == " " || TagManager.ReadTagColor(player.FriendCode) == "") && (TagManager.ReadTagName(player.FriendCode) != "" && TagManager.ReadTagName(player.FriendCode) != " ")) modtag = $"{TagManager.ReadTagName(player.FriendCode)}";
            else if (TagManager.ReadTagName(player.FriendCode) == " " || TagManager.ReadTagName(player.FriendCode) == "") modtag = "";
            else modtag = $"<color=#{TagManager.ReadTagColor(player.FriendCode)}>{TagManager.ReadTagName(player.FriendCode)}</color>";
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

        if (!name.Contains($"\r\r") && player.FriendCode.GetDevUser().HasTag() && player.IsModded())
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
        return Main.AllPlayerControls.FirstOrDefault(pc => pc.PlayerId == PlayerId) ?? null;
    }
    public static PlayerControl GetPlayer(this byte id) => GetPlayerById(id);
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

            if (player.Is(CustomRoles.Enchanted)) team = Custom_Team.Coven;
            else team = player.Is(CustomRoles.Madmate) ? Custom_Team.Impostor : Custom_Team.Neutral;
            return target.Is(Compare);
        }
        else if (!target.IsAnySubRole(x => x.IsConverted()))
        {
            team = player.GetCustomRole().GetCustomRoleTeam();
            return target.Is(team);
        }


        return false;
    }
    public static IEnumerable<t> GetRoleBasesByType<t>() where t : RoleBase
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
    public static System.Collections.IEnumerator NotifyEveryoneAsync(int speed = 2)
    {
        var count = 0;
        bool isMeeting = GameStates.IsMeeting;
        if (isMeeting) yield break;

        PlayerControl[] aapc = Main.AllAlivePlayerControls;

        foreach (PlayerControl seer in aapc)
        {
            foreach (PlayerControl target in aapc)
            {
                if (isMeeting) yield break;
                NotifyRoles(SpecifySeer: seer, SpecifyTarget: target);
                if (count++ % speed == 0) yield return null;
            }
        }
    }
    // During intro scene to set team name and role info for non-modded clients and skip the rest.
    // Note: When Neutral is based on the Crewmate role then it is impossible to display the info for it
    // If not a Desync Role remove team display
    public static void SetCustomIntro(this PlayerControl player)
    {
        if (!SetUpRoleTextPatch.IsInIntro || player == null || player.IsModded()) return;

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
        string SelfTeamName = $"<size=450%>{IconText} {Font}{ColorString(GetTeamColor(player), $"{player.GetCustomRole().GetCustomRoleTeam()}")}</font> {IconText}</size><size=900%>\n \n</size>\r\n";
        string SelfRoleName = $"<size=185%>{Font}{ColorString(player.GetRoleColor(), GetRoleName(player.GetCustomRole()))}</font></size>";
        string SelfSubRolesName = string.Empty;
        string RoleInfo = $"<size=25%>\n</size><size={GetInfoSize(player.GetRoleInfo())}%>{Font}{ColorString(player.GetRoleColor(), player.GetRoleInfo())}</font></size>";
        string RoleNameUp = "<size=1350%>\n\n</size>";

        if (!player.HasDesyncRole())
        {
            SelfTeamName = string.Empty;
            RoleNameUp = "<size=565%>\n</size>";
            RoleInfo = $"<size=50%>\n</size><size={GetInfoSize(player.GetRoleInfo())}%>{Font}{ColorString(player.GetRoleColor(), player.GetRoleInfo())}</font></size>";
        }

        // Format addons
        bool isFirstSub = true;
        foreach (var subRole in player.GetCustomSubRoles().ToArray())
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

        var SelfName = $"{SelfTeamName}{SelfRoleName}{SelfSubRolesName}\r\n{RoleInfo}{RoleNameUp}";

        // Privately sent name.
        player.RpcSetNamePrivate(SelfName, player, force: true);
    }

    public static NetworkedPlayerInfo GetPlayerInfoById(int PlayerId) =>
        GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => info.PlayerId == PlayerId);

    // Self
    private static readonly StringBuilder SelfName = new();
    private static readonly StringBuilder SelfRoleName = new();
    private static readonly StringBuilder SelfDeathReason = new();
    private static readonly StringBuilder SelfTaskText = new();
    private static readonly StringBuilder SelfSuffix = new();
    private static readonly StringBuilder SelfMark = new(20);
    // Target
    private static readonly StringBuilder TargetPlayerName = new();
    private static readonly StringBuilder TargetRoleName = new();
    private static readonly StringBuilder TargetDeathReason = new();
    private static readonly StringBuilder TargetSuffix = new();
    private static readonly StringBuilder TargetMark = new(20);
    public static void NotifyRoles(PlayerControl SpecifySeer = null, PlayerControl SpecifyTarget = null, bool isForMeeting = false, bool NoCache = false, bool ForceLoop = true, bool CamouflageIsForMeeting = false, bool MushroomMixupIsActive = false)
    {
        if (!AmongUsClient.Instance.AmHost || GameStates.IsHideNSeek || Main.AllPlayerControls == null || SetUpRoleTextPatch.IsInIntro) return;
        if (MeetingHud.Instance)
        {
            // When the meeting window is active and game is not ended
            if (!GameEndCheckerForNormal.GameIsEnded) return;
        }
        else
        {
            // When some one press report button but NotifyRoles is not for meeting
            if (Main.MeetingIsStarted && !isForMeeting) return;
        }

        //var logger = Logger.Handler("DoNotifyRoles");

        HudManagerUpdatePatch.NowCallNotifyRolesCount++;
        HudManagerUpdatePatch.LastSetNameDesyncCount = 0;

        PlayerControl[] seerList = SpecifySeer != null
            ? [SpecifySeer]
            : Main.AllPlayerControls;

        PlayerControl[] targetList = SpecifyTarget != null
            ? [SpecifyTarget]
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

            Main.LowLoadUpdateName[seer.PlayerId] = true;

            // Only non-modded players or player left
            if (seer.IsModded() || seer.PlayerId == OnPlayerLeftPatch.LeftPlayerId || seer.Data.Disconnected) continue;

            const string fontSizeDeathReason = "1.6";
            string fontSize = isForMeeting
                ? seer.GetClient().PlatformData.Platform is Platforms.Playstation or Platforms.Xbox or Platforms.Switch ? "70%" : "1.6"
                : "1.8";

            //logger.Info("NotifyRoles-Loop1-" + seer.GetNameWithRole() + ":START");

            var seerRole = seer.GetCustomRole();
            var seerRoleClass = seer.GetRoleClass();

            // Hide player names in during Mushroom Mixup if seer is alive and desync impostor
            if (!CamouflageIsForMeeting && MushroomMixupIsActive && seer.IsAlive() && (!seer.Is(Custom_Team.Impostor) || Main.PlayerStates[seer.PlayerId].IsNecromancer) && seer.HasDesyncRole())
            {
                seer.RpcSetNamePrivate("<size=0%>", force: NoCache);
            }
            else
            {
                // Clear marker after name seer
                SelfMark.Clear();
                SelfSuffix.Clear();

                switch (Options.CurrentGameMode)
                {
                    case CustomGameMode.FFA:
                        SelfSuffix.Append(FFAManager.GetPlayerArrow(seer));
                        break;

                    default:
                        SelfMark.Append(seerRoleClass?.GetMark(seer, seer, isForMeeting: isForMeeting));
                        SelfMark.Append(CustomRoleManager.GetMarkOthers(seer, seer, isForMeeting: isForMeeting));

                        if (seer.Is(CustomRoles.Lovers))
                            SelfMark.Append(CustomRoles.Lovers.GetColoredTextByRole("♥"));

                        if (seer.Is(CustomRoles.Cyber) && Cyber.CyberKnown.GetBool())
                            SelfMark.Append(CustomRoles.Cyber.GetColoredTextByRole("★"));

                        SelfSuffix.Append(seerRoleClass?.GetLowerText(seer, seer, isForMeeting: isForMeeting));
                        SelfSuffix.Append(CustomRoleManager.GetLowerTextOthers(seer, seer, isForMeeting: isForMeeting));

                        SelfSuffix.Append(seerRoleClass?.GetSuffix(seer, seer, isForMeeting: isForMeeting));
                        SelfSuffix.Append(CustomRoleManager.GetSuffixOthers(seer, seer, isForMeeting: isForMeeting));

                        SelfSuffix.Append(Radar.GetPlayerArrow(seer, seer, isForMeeting: isForMeeting));
                        SelfSuffix.Append(Spurt.GetSuffix(seer, isformeeting: isForMeeting));
                        break;
                }


                // ====== Get SeerRealName ======

                var SeerRealName = new StringBuilder(seer.GetRealName(isForMeeting));

                // ====== Combine SelfRoleName, SelfTaskText, SelfName, SelfDeathReason for seer ======

                SelfTaskText.Clear().Append(GetProgressText(seer));
                SelfRoleName.Clear().Append($"<size={fontSize}>{seer.GetDisplayRoleAndSubName(seer, isForMeeting, false)}{SelfTaskText}</size>");
                SelfDeathReason.Clear().Append(seer.KnowDeathReason(seer) ? $"\n<size={fontSizeDeathReason}>『{CustomRoles.Doctor.GetColoredTextByRole(GetVitalText(seer.PlayerId))}』</size>" : string.Empty);
                SelfName.Clear().Append($"{ColorString(seer.GetRoleColor(), SeerRealName.ToString())}{SelfDeathReason}{SelfMark}");

                // Add protected player icon from ShieldPersonDiedFirst
                if (seer.GetClient().GetHashedPuid() == Main.FirstDiedPrevious && MeetingStates.FirstMeeting && !isForMeeting)
                {
                    SelfName.Clear().Append($"{ColorString(seer.GetRoleColor(), $"<color=#4fa1ff><u></color>{SeerRealName}</u>")}{SelfDeathReason}<color=#4fa1ff>✚</color>{SelfMark}");
                }

                bool IsDisplayInfo = false;

                if (Options.CurrentGameMode is CustomGameMode.Standard)
                {
                    if (MeetingStates.FirstMeeting && Options.ChangeNameToRoleInfo.GetBool() && !isForMeeting)
                    {
                        IsDisplayInfo = true;
                        var SeerRoleInfo = seer.GetRoleInfo();
                        string RoleText = string.Empty;
                        const string Font = "<font=\"VCR SDF\" material=\"VCR Black Outline\">";

                        if (seerRole.IsImpostor()) { RoleText = ColorString(GetTeamColor(seer), GetString("TeamImpostor")); }
                        else if (seerRole.IsCrewmate()) { RoleText = ColorString(GetTeamColor(seer), GetString("TeamCrewmate")); }
                        else if (seerRole.IsNeutral()) { RoleText = ColorString(GetTeamColor(seer), GetString("TeamNeutral")); }
                        else if (seerRole.IsMadmate()) { RoleText = ColorString(GetTeamColor(seer), GetString("TeamMadmate")); }
                        else if (seerRole.IsCoven()) { RoleText = ColorString(GetTeamColor(seer), GetString("TeamCoven")); }

                        SelfName.Append($"<size=600%>\n \n</size><size=150%>{Font}{ColorString(seer.GetRoleColor(), RoleText)}</size>\n<size=75%>{ColorString(seer.GetRoleColor(), seer.GetRoleInfo())}</size></font>\n");
                    }

                    if (Pelican.HasEnabled && Pelican.IsEaten(seer.PlayerId))
                        SelfName.Clear().Append($"{CustomRoles.Pelican.GetColoredTextByRole(GetString("EatenByPelican"))}");

                    else if (Deathpact.HasEnabled && Deathpact.IsInActiveDeathpact(seer))
                        SelfName.Clear().Append(Deathpact.GetDeathpactString(seer));

                    else if (Devourer.HasEnabled && Devourer.HideNameOfTheDevoured(seer.PlayerId) && !CamouflageIsForMeeting)
                        SelfName.Clear().Append(GetString("DevouredName"));

                    // Dollmaster, Prevent seeing self in mushroom cloud
                    if (seerRole != CustomRoles.DollMaster && DollMaster.IsDoll(seer.PlayerId))
                    {
                        SelfName.Clear().Append("<size=10000%><color=#000000>■</color></size>");
                    }
                }

                if (NameNotifyManager.GetNameNotify(seer, out var notifyName))
                    SelfName.Clear().Append(notifyName);

                // Camouflage
                if (!CamouflageIsForMeeting && Camouflage.IsCamouflage)
                {
                    string oldName = SelfName.ToString();
                    SelfName.Clear().Append($"<size=0%>{oldName}</size>");
                }

                if (!Regex.IsMatch(SelfName.ToString(), seer.GetRealName()))
                    IsDisplayInfo = false;

                switch (Options.CurrentGameMode)
                {
                    case CustomGameMode.FFA:
                        string FFAName = string.Empty;
                        FFAManager.GetNameNotify(seer, ref FFAName);
                        SelfName.Clear().Append($"<size={fontSize}>{SelfTaskText}</size>\r\n{FFAName}");
                        break;
                    default:
                        var oldSelfName = SelfName.ToString();
                        if (!IsDisplayInfo)
                        {
                            SelfName.Clear().Append(SelfRoleName + "\r\n" + oldSelfName);
                        }
                        else
                        {
                            SelfName.Clear().Append("<size=425%>\n \n</size>" + SelfRoleName + "\r\n" + oldSelfName);
                        }
                        break;
                }

                SelfName.Append(SelfSuffix.Length == 0 ? string.Empty : $"\r\n {SelfSuffix}");

                if (!isForMeeting)
                    SelfName.Append("\r\n");

                seer.RpcSetNamePrivate(SelfName.ToString(), force: NoCache);
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
                    if (realTarget == null || (realTarget.PlayerId == seer.PlayerId) || realTarget.PlayerId == OnPlayerLeftPatch.LeftPlayerId || realTarget.Data.Disconnected) continue;

                    var target = realTarget;

                    if (seer != target && seer != DollMaster.DollMasterTarget)
                        target = DollMaster.SwapPlayerInfo(realTarget); // If a player is possessed by the Dollmaster swap each other's controllers.

                    Main.LowLoadUpdateName[target.PlayerId] = true;
                    Main.LowLoadUpdateName[realTarget.PlayerId] = true;

                    //logger.Info("NotifyRoles-Loop2-" + target.GetNameWithRole() + ":START");

                    // Hide player names in during Mushroom Mixup if seer is alive and desync impostor
                    if (!CamouflageIsForMeeting && MushroomMixupIsActive && target.IsAlive() && (!seer.Is(Custom_Team.Impostor) || Main.PlayerStates[seer.PlayerId].IsNecromancer) && seer.HasDesyncRole())
                    {
                        realTarget.RpcSetNamePrivate("<size=0%>", seer, force: NoCache);
                    }
                    else
                    {
                        TargetMark.Clear();
                        TargetSuffix.Clear();

                        switch (Options.CurrentGameMode)
                        {
                            case CustomGameMode.Standard:
                                TargetMark.Append(seerRoleClass?.GetMark(seer, target, isForMeeting));
                                TargetMark.Append(CustomRoleManager.GetMarkOthers(seer, target, isForMeeting));

                                if (seer.Is(Custom_Team.Impostor) && target.Is(CustomRoles.Snitch) && target.Is(CustomRoles.Madmate) && target.GetPlayerTaskState().IsTaskFinished)
                                    TargetMark.Append(CustomRoles.Impostor.GetColoredTextByRole("★"));


                                if (((seer.IsPlayerCovenTeam() && target.IsPlayerCovenTeam()) || !seer.IsAlive()) && CovenManager.HasNecronomicon(target))
                                {
                                    TargetMark.Append(CustomRoles.Coven.GetColoredTextByRole("♣"));
                                }
                                if (target.Is(CustomRoles.Cyber) && Cyber.CyberKnown.GetBool())
                                    TargetMark.Append(CustomRoles.Cyber.GetColoredTextByRole("★"));

                                if (seer.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers))
                                {
                                    TargetMark.Append(CustomRoles.Lovers.GetColoredTextByRole("♥"));
                                }
                                else if (seer.Data.IsDead && !seer.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers))
                                {
                                    TargetMark.Append(CustomRoles.Lovers.GetColoredTextByRole("♥"));
                                }

                                TargetSuffix.Append(CustomRoleManager.GetLowerTextOthers(seer, target, isForMeeting: isForMeeting));

                                TargetSuffix.Append(seerRoleClass?.GetSuffix(seer, target, isForMeeting: isForMeeting));
                                TargetSuffix.Append(CustomRoleManager.GetSuffixOthers(seer, target, isForMeeting: isForMeeting));

                                break;
                        }

                        if (TargetSuffix.Length > 0)
                        {
                            TargetSuffix.Insert(0, "\r\n");
                        }

                        // ====== Seer know target role ======

                        bool KnowRoleTarget = ExtendedPlayerControl.KnowRoleTarget(seer, target);

                        TargetRoleName.Clear().Append(KnowRoleTarget
                                ? $"<size={fontSize}>{seer.GetDisplayRoleAndSubName(target, isForMeeting, false)}{GetProgressText(target)}</size>\r\n" : string.Empty);


                        if (seer.IsAlive() && Overseer.IsRevealedPlayer(seer, target))
                        {
                            var blankRT = new StringBuilder();
                            if (target.Is(CustomRoles.Trickster) || Illusionist.IsCovIllusioned(target.PlayerId))
                            {
                                blankRT.Clear().Append(Overseer.GetRandomRole(seer.PlayerId)); // Random trickster role
                                blankRT.Append(TaskState.GetTaskState()); // Random task count for revealed trickster
                                TargetRoleName.Clear().Append($"<size={fontSize}>{blankRT}</size>\r\n");
                            }
                            if (Illusionist.IsNonCovIllusioned(target.PlayerId))
                            {
                                var randomRole = CustomRolesHelper.AllRoles.Where(role => role.IsEnable() && !role.IsAdditionRole() && role.IsCoven()).ToList().RandomElement();
                                blankRT.Clear().Append(randomRole.GetColoredTextByRole(GetString(randomRole.ToString())));
                                if (randomRole is CustomRoles.CovenLeader or CustomRoles.Jinx or CustomRoles.Illusionist or CustomRoles.VoodooMaster) // Roles with Ability Uses
                                {
                                    blankRT.Append(randomRole.GetStaticRoleClass().GetProgressText(target.PlayerId, false));
                                }
                                TargetRoleName.Clear().Append($"<size={fontSize}>{blankRT}</size>\r\n");
                            }
                        }

                        // ====== Target player name ======

                        TargetPlayerName.Clear().Append(target.GetRealName(isForMeeting));

                        var tempNameText = new StringBuilder(seer.GetRoleClass()?.NotifyPlayerName(seer, target, TargetPlayerName.ToString(), isForMeeting));
                        if (tempNameText.Length > 0)
                        {
                            TargetPlayerName.Clear().Append(tempNameText);
                        }

                        seerRole = seer.GetCustomRole();

                        // ========= Only During Meeting =========
                        if (isForMeeting)
                        {
                            var GetTragetId = new StringBuilder($"{seerRole.GetColoredTextByRole(target.PlayerId.ToString())} {TargetPlayerName}");

                            // Guesser Mode is On ID
                            if (Options.GuesserMode.GetBool())
                            {
                                // seer & target is alive
                                if (seer.IsAlive() && target.IsAlive())
                                {
                                    //Crewmates
                                    if (Options.CrewmatesCanGuess.GetBool() && seerRole.IsCrewmate() && !(seerRole is CustomRoles.Judge or CustomRoles.Inspector or CustomRoles.Lookout or CustomRoles.Swapper))
                                    {
                                        TargetPlayerName.Clear().Append(GetTragetId);
                                    }
                                    else if (seer.Is(CustomRoles.NiceGuesser) && !Options.CrewmatesCanGuess.GetBool())
                                    {
                                        TargetPlayerName.Clear().Append(GetTragetId);
                                    }

                                    //Impostors
                                    if (Options.ImpostorsCanGuess.GetBool() && (seerRole.IsImpostor() || seerRole.IsMadmate()) && !(seerRole is CustomRoles.Councillor or CustomRoles.Nemesis))
                                    {
                                        TargetPlayerName.Clear().Append(GetTragetId);
                                    }
                                    else if (seer.Is(CustomRoles.EvilGuesser) && !Options.ImpostorsCanGuess.GetBool())
                                    {
                                        TargetPlayerName.Clear().Append(GetTragetId);
                                    }

                                    // Neutrals
                                    if (Options.NeutralKillersCanGuess.GetBool() && seerRole.IsNK())
                                    {
                                        TargetPlayerName.Clear().Append(GetTragetId);
                                    }

                                    if (Options.NeutralApocalypseCanGuess.GetBool() && seerRole.IsNA())
                                    {
                                        TargetPlayerName.Clear().Append(GetTragetId);
                                    }

                                    if (Options.PassiveNeutralsCanGuess.GetBool() && seerRole.IsNonNK() && seerRole is not CustomRoles.Doomsayer)
                                    {
                                        TargetPlayerName.Clear().Append(GetTragetId);
                                    }

                                    if (Options.CovenCanGuess.GetBool() && seerRole.IsCoven())
                                    {
                                        TargetPlayerName.Clear().Append(GetTragetId);
                                    }
                                }
                            }
                            else // Guesser Mode is Off ID
                            {
                                if (seer.IsAlive() && target.IsAlive())
                                {
                                    if (seerRole is CustomRoles.NiceGuesser or CustomRoles.EvilGuesser || (seer.Is(CustomRoles.Guesser)
                                        && !(seerRole is CustomRoles.Inspector or CustomRoles.Swapper or CustomRoles.Lookout)))
                                    {
                                        TargetPlayerName.Clear().Append(GetTragetId);
                                    }
                                }
                            }
                        }

                        var nameColor = new StringBuilder(TargetPlayerName.ToString().ApplyNameColorData(seer, target, isForMeeting));
                        TargetPlayerName.Clear().Append(nameColor);

                        // ====== Target Death Reason for target (Death Reason visible ​​only to the seer) ======
                        TargetDeathReason.Clear().Append(seer.KnowDeathReason(target)
                            ? $"\n<size={fontSizeDeathReason}>『{CustomRoles.Doctor.GetColoredTextByRole(GetVitalText(target.PlayerId))}』</size>" : string.Empty);

                        // Devourer
                        if (Devourer.HasEnabled)
                        {
                            bool targetDevoured = Devourer.HideNameOfTheDevoured(target.PlayerId);
                            if (targetDevoured && !CamouflageIsForMeeting)
                                TargetPlayerName.Clear().Append(GetString("DevouredName"));
                        }

                        // Camouflage
                        if (!CamouflageIsForMeeting && Camouflage.IsCamouflage)
                        {
                            var oldName = TargetPlayerName.ToString();
                            TargetPlayerName.Clear().Append($"<size=0%>{oldName}</size>");
                        }

                        // Target Name
                        var TargetName = new StringBuilder();

                        // Add protected player icon from ShieldPersonDiedFirst
                        if (target.GetClient().GetHashedPuid() == Main.FirstDiedPrevious && MeetingStates.FirstMeeting && !isForMeeting && Options.ShowShieldedPlayerToAll.GetBool())
                        {
                            TargetName.Append(TargetRoleName)
                                .Append("<color=#4fa1ff><u></color>")
                                .Append(TargetPlayerName)
                                .Append("</u>")
                                .Append(TargetDeathReason)
                                .Append("<color=#4fa1ff>✚</color>")
                                .Append(TargetMark)
                                .Append(TargetSuffix);
                        }
                        else
                        {
                            TargetName
                                .Append(TargetRoleName)
                                .Append(TargetPlayerName)
                                .Append(TargetDeathReason)
                                .Append(TargetMark)
                                .Append(TargetSuffix);
                        }

                        realTarget.RpcSetNamePrivate(TargetName.ToString(), seer, force: NoCache);
                    }
                }
            }
        }
        //Logger.Info($" Loop for Targets: {}", "DoNotifyRoles", force: true);
        Logger.Info(" END", "DoNotifyRoles");
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
    public static void SendGameData()
    {
        foreach (var playerinfo in GameData.Instance.AllPlayers)
        {
            MessageWriter writer = MessageWriter.Get(SendOption.None);
            writer.StartMessage(5); //0x05 GameData
            writer.Write(AmongUsClient.Instance.GameId);
            {
                writer.StartMessage(1); //0x01 Data
                {
                    writer.WritePacked(playerinfo.NetId);
                    playerinfo.Serialize(writer, true);
                }
                writer.EndMessage();
            }
            writer.EndMessage();

            AmongUsClient.Instance.SendOrDisconnect(writer);
            writer.Recycle();
        }
    }
    public static void SetAllVentInteractions()
    {
        VentSystemDeterioratePatch.SerializeV2(ShipStatus.Instance.Systems[SystemTypes.Ventilation].CastFast<VentilationSystem>());
    }
    public static void CheckAndSetVentInteractions()
    {
        bool shouldPerformVentInteractions = false;

        foreach (var pc in Main.AllPlayerControls)
        {
            if (VentSystemDeterioratePatch.BlockVentInteraction(pc))
            {
                VentSystemDeterioratePatch.LastClosestVent[pc.PlayerId] = pc.GetVentsFromClosest()[0].Id;
                shouldPerformVentInteractions = true;
            }
        }

        if (shouldPerformVentInteractions)
        {
            SetAllVentInteractions();
        }
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
            PlayerState.DeathReason.Electrocuted => CustomRoles.Shocker.IsEnable(),
            PlayerState.DeathReason.Scavenged => CustomRoles.Scavenger.IsEnable(),
            PlayerState.DeathReason.BlastedOff => CustomRoles.MoonDancer.IsEnable(),
            PlayerState.DeathReason.Kill => true,
            _ => true,
        };
    }
    public static void AfterMeetingTasks()
    {
        PhantomRolePatch.AfterMeeting();
        ChatManager.ClearLastSysMsg();
        FallFromLadder.Reset();

        if (Diseased.IsEnable) Diseased.AfterMeetingTasks();
        if (Antidote.IsEnable) Antidote.AfterMeetingTasks();

        AntiBlackout.AfterMeetingTasks();

        try
        {
            CovenManager.CheckNecroVotes();
        }
        catch (Exception error)
        {
            Logger.Error($"Error in CovenManager after meeting: {error}", "AfterMeetingTasks");
        }

        try
        {
            foreach (var playerState in Main.PlayerStates.Values.ToArray())
            {
                if (playerState.RoleClass == null) continue;

                playerState.RoleClass.AfterMeetingTasks();
                playerState.RoleClass.HasVoted = false;

                foreach (var ventId in playerState.RoleClass.LastBlockedMoveInVentVents)
                {
                    CustomRoleManager.BlockedVentsList[playerState.PlayerId].Remove(ventId);
                }
                playerState.RoleClass.LastBlockedMoveInVentVents.Clear();
            }

            //Set kill timer
            foreach (var player in Main.AllAlivePlayerControls)
            {
                player.SetKillTimer();

                if (player.Is(CustomRoles.Prohibited))
                {
                    Prohibited.AfterMeetingTasks(player.PlayerId);
                }
            }

            if (Statue.IsEnable) Statue.AfterMeetingTasks();
            if (Burst.IsEnable) Burst.AfterMeetingTasks();

            if (CustomRoles.CopyCat.HasEnabled()) CopyCat.UnAfterMeetingTasks(); // All crew hast to be before this
            if (CustomRoles.Necromancer.HasEnabled()) Necromancer.UnAfterMeetingTasks();
        }
        catch (Exception error)
        {
            Logger.Error($"Error after meeting: {error}", "AfterMeetingTasks");
        }

        if (Options.AirshipVariableElectrical.GetBool())
            AirshipElectricalDoors.Initialize();

        RPC.SyncDeadPassedMeetingList();
        DoorsReset.ResetDoors();
        CustomNetObject.AfterMeetingTasks();

        // Empty Deden bug support Empty vent after meeting
        var ventilationSystem = ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out var systemType) ? systemType.CastFast<VentilationSystem>() : null;
        if (ventilationSystem != null)
        {
            ventilationSystem.PlayersInsideVents.Clear();
            ventilationSystem.IsDirty = true;
            // Will be synced by ShipStatus patch, SetAllVentInteractions
        }
    }
    public static string ToColoredString(this CustomRoles role) => ColorString(GetRoleColor(role), GetString(role.ToString()));
    public static void ChangeInt(ref int ChangeTo, int input, int max)
    {
        var tmp = ChangeTo * 10;
        tmp += input;
        ChangeTo = Math.Clamp(tmp, 0, max);
    }
    public static void CountAlivePlayers(bool sendLog = false, bool checkGameEnd = false)
    {
        int AliveImpostorCount = Main.AllAlivePlayerControls.Count(pc => pc.Is(Custom_Team.Impostor) || pc.GetCustomRole().IsMadmate());//why cant i just use IsImpostorTeamV3 here
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


        var taskState = Main.PlayerStates?[id].TaskState;

        // Impossible to output summarytexts for a player without playerState
        if (!Main.PlayerStates.TryGetValue(id, out var playerState))
        {
            Logger.Error("playerState for {id} not found", "Utils.SummaryTexts");
            return $"[{id}]" + name + " : <b>ERROR</b>";
        }

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
        string summary = $"{ColorString(Main.PlayerColors[id], name)} - {GetDisplayRoleAndSubName(id, id, false, true)}{GetSubRolesText(id, summary: true)}{TaskCount} {GetKillCountText(id)} 『{GetVitalText(id, true)}』{disconnectedText}";
        switch (Options.CurrentGameMode)
        {
            case CustomGameMode.FFA:
                summary = $"{ColorString(Main.PlayerColors[id], name)} {GetKillCountText(id, ffa: true)}";
                break;
        }
        return check && GetDisplayRoleAndSubName(id, id, false, true).RemoveHtmlTags().Contains("INVALID:NotAssigned")
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

    private readonly static Dictionary<string, Sprite> CachedSprites = [];
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
    private static unsafe Texture2D LoadTextureFromResources(string path)
    {
        try
        {
            Texture2D texture = new(2, 2, TextureFormat.ARGB32, true);
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream(path);
            var length = stream.Length;
            var byteTexture = new Il2CppStructArray<byte>(length);
            stream.Read(new Span<byte>(IntPtr.Add(byteTexture.Pointer, IntPtr.Size * 4).ToPointer(), (int)length));
            ImageConversion.LoadImage(texture, byteTexture, false);
            return texture;
        }
        catch
        {
            Logger.Error($"Failed to read Texture： {path}", "LoadTextureFromResources");
        }
        return null;
    }
    public static string ColorString(Color32 color, string str, bool withoutEnding = false)
    {
        var sb = new StringBuilder();
        sb.Append("<color=#").Append($"{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>").Append(str);
        if (!withoutEnding) sb.Append("</color>");
        return sb.ToString();
    }
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

    public static float GetSettingNameAndValueForRole(CustomRoles role, string settingName)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        var types = Assembly.GetExecutingAssembly().GetTypes();
        var field = types.SelectMany(x => x.GetFields(flags)).FirstOrDefault(x => x.Name == $"{role}{settingName}");
        if (field == null)
        {
            FieldInfo tempField = null;
            foreach (var x in types)
            {
                bool any = false;
                foreach (var f in x.GetFields(flags))
                {
                    if (f.Name.Contains(settingName))
                    {
                        any = true;
                        tempField = f;
                        break;
                    }
                }

                if (any && x.Name == $"{role}")
                {
                    field = tempField;
                    break;
                }
            }
        }

        float add;
        if (field == null)
        {
            add = float.MaxValue;
        }
        else
        {
            if (field.GetValue(null) is OptionItem optionItem) add = optionItem.GetFloat();
            else add = float.MaxValue;
        }

        return add;
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

        if (player.IsHost())
        {
            HudManager.Instance.Chat.SetVisible(true);
            return;
        }

        if (player.IsModded())
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
