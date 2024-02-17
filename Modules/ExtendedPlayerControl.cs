using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHE.Modules;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

static class ExtendedPlayerControl
{
    public static void RpcSetCustomRole(this PlayerControl player, CustomRoles role)
    {
        if (role < CustomRoles.NotAssigned)
        {
            Main.PlayerStates[player.PlayerId].SetMainRole(role);
        }
        else if (role >= CustomRoles.NotAssigned)   //500:NoSubRole 501~:SubRole
        {
            if (!Cleanser.CleansedCanGetAddon.GetBool() && player.Is(CustomRoles.Cleansed)) return;
            if (role == CustomRoles.Cleansed) Main.PlayerStates[player.PlayerId].SetSubRole(role, pc: player);
            else Main.PlayerStates[player.PlayerId].SetSubRole(role);            
            //if (role == CustomRoles.Cleanser) Main.PlayerStates[player.PlayerId].SetSubRole(role, AllReplace:true);
            //else Main.PlayerStates[player.PlayerId].SetSubRole(role);
        }
        if (AmongUsClient.Instance.AmHost)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, Hazel.SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.WritePacked((int)role);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    public static void RpcSetCustomRole(byte PlayerId, CustomRoles role)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, Hazel.SendOption.Reliable, -1);
            writer.Write(PlayerId);
            writer.WritePacked((int)role);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }

    public static void RpcExile(this PlayerControl player)
    {
        RPC.ExileAsync(player);
    }
    public static ClientData GetClient(this PlayerControl player)
    {
        try
        {
            var client = AmongUsClient.Instance.allClients.ToArray().FirstOrDefault(cd => cd.Character.PlayerId == player.PlayerId);
            return client;
        }
        catch
        {
            return null;
        }
    }
    public static int GetClientId(this PlayerControl player)
    {
        if (player == null) return -1;
        var client = player.GetClient();
        return client == null ? -1 : client.Id;
    }
    public static CustomRoles GetCustomRole(this GameData.PlayerInfo player)
    {
        return player == null || player.Object == null ? CustomRoles.Crewmate : player.Object.GetCustomRole();
    }
    /// <summary>
    /// Only roles (no add-ons)
    /// </summary>
    public static CustomRoles GetCustomRole(this PlayerControl player)
    {
        if (player == null)
        {
            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            Logger.Warn(callerClassName + "." + callerMethodName + "tried to retrieve CustomRole, but the target was null", "GetCustomRole");
            return CustomRoles.Crewmate;
        }
        var GetValue = Main.PlayerStates.TryGetValue(player.PlayerId, out var State);

        return GetValue ? State.MainRole : CustomRoles.Crewmate;
    }

    public static List<CustomRoles> GetCustomSubRoles(this PlayerControl player)
    {
        if (player == null)
        {
            Logger.Warn("tried to get CustomSubRole, but the target was null", "GetCustomSubRole");
            return [CustomRoles.NotAssigned];
        }
        return Main.PlayerStates[player.PlayerId].SubRoles;
    }
    public static CountTypes GetCountTypes(this PlayerControl player)
    {
        if (player == null)
        {
            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            Logger.Warn(callerClassName + "." + callerMethodName + "tried to get CountTypes, but the target was null", "GetCountTypes");
            return CountTypes.None;
        }

        return Main.PlayerStates.TryGetValue(player.PlayerId, out var State) ? State.countTypes : CountTypes.None;
    }
    public static void RpcSetNameEx(this PlayerControl player, string name)
    {
        foreach (var seer in Main.AllPlayerControls)
        {
            Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = name;
        }
        HudManagerPatch.LastSetNameDesyncCount++;

        Logger.Info($"Set:{player?.Data?.PlayerName}:{name} for All", "RpcSetNameEx");
        player.RpcSetName(name);
    }

    public static void RpcSetNamePrivate(this PlayerControl player, string name, bool DontShowOnModdedClient = false, PlayerControl seer = null, bool force = false)
    {
        //player: 名前の変更対象
        //seer: 上の変更を確認することができるプレイヤー
        if (player == null || name == null || !AmongUsClient.Instance.AmHost) return;
        if (seer == null) seer = player;

        if (!force && Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] == name)
        {
            //Logger.info($"Cancel:{player.name}:{name} for {seer.name}", "RpcSetNamePrivate");
            return;
        }
        Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = name;
        HudManagerPatch.LastSetNameDesyncCount++;
        Logger.Info($"Set:{player?.Data?.PlayerName}:{name} for {seer.GetNameWithRole().RemoveHtmlTags()}", "RpcSetNamePrivate");

        var clientId = seer.GetClientId();
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetName, SendOption.Reliable, clientId);
        writer.Write(name);
        writer.Write(DontShowOnModdedClient);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcSetRoleDesync(this PlayerControl player, RoleTypes role, int clientId)
    {
        //player: 名前の変更対象

        if (player == null) return;
        if (AmongUsClient.Instance.ClientId == clientId)
        {
            player.SetRole(role);
            return;
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetRole, SendOption.Reliable, clientId);
        writer.Write((ushort)role);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void RpcGuardAndKill(this PlayerControl killer, PlayerControl target = null, int colorId = 0, bool forObserver = false)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            Logger.Warn($"Modded non-host client activated RpcGuardAndKill from {callerClassName}.{callerMethodName}", "RpcGuardAndKill");
            return;
        }

        if (target == null) target = killer;

        // Check Observer
        if (Observer.IsEnable && !forObserver && !MeetingStates.FirstMeeting)
        {
            Observer.ActivateGuardAnimation(killer.PlayerId, target, colorId);
        }

        // Host
        if (killer.AmOwner)
        {
            killer.MurderPlayer(target, MurderResultFlags.FailedProtected);
        }
        // Other Clients
        if (killer.PlayerId != 0)
        {
            var writer = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable);
            writer.WriteNetObject(target);
            writer.Write((int)MurderResultFlags.FailedProtected);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    public static void SetKillCooldownV2(this PlayerControl player, float time = -1f)
    {
        if (player == null) return;
        if (!player.CanUseKillButton()) return;
        if (time >= 0f) Main.AllPlayerKillCooldown[player.PlayerId] = time * 2;
        else Main.AllPlayerKillCooldown[player.PlayerId] *= 2;
        player.SyncSettings();
        player.RpcGuardAndKill();
        player.ResetKillCooldown();
    }
    public static void SetKillCooldown(this PlayerControl player, float time = -1f, PlayerControl target = null, bool forceAnime = false)
    {
        if (player == null) return;

        if (!player.HasImpKillButton(considerVanillaShift: true)) return;
        if (player.HasImpKillButton(false) && !player.CanUseKillButton()) return;

        if (target == null) target = player;
        if (time >= 0f) Main.AllPlayerKillCooldown[player.PlayerId] = time * 2;
        else Main.AllPlayerKillCooldown[player.PlayerId] *= 2;
        if (player.Is(CustomRoles.Glitch))
        {
            Glitch.LastKill = Utils.GetTimeStamp() + ((int)(time / 2) - Glitch.KillCooldown.GetInt());
            Glitch.KCDTimer = (int)(time / 2);
        }
        else if (forceAnime || !player.IsModClient() || !Options.DisableShieldAnimations.GetBool())
        {
            player.SyncSettings();
            player.RpcGuardAndKill(target, 11);
        }
        else
        {
            time = Main.AllPlayerKillCooldown[player.PlayerId] / 2;
            if (player.AmOwner) PlayerControl.LocalPlayer.SetKillTimer(time);
            else
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetKillTimer, SendOption.Reliable, player.GetClientId());
                writer.Write(time);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
            // Check Observer
            if (Observer.IsEnable)
            {
                Observer.ActivateGuardAnimation(target.PlayerId, target, 11);
            }
        }
        player.ResetKillCooldown();
    }
    public static void SetKillCooldownV3(this PlayerControl player, float time = -1f, PlayerControl target = null, bool forceAnime = false)
    {
        if (player == null) return;
        if (!player.CanUseKillButton()) return;
        if (target == null) target = player;
        if (time >= 0f) Main.AllPlayerKillCooldown[player.PlayerId] = time * 2;
        else Main.AllPlayerKillCooldown[player.PlayerId] *= 2;
        if (forceAnime || !player.IsModClient() || !Options.DisableShieldAnimations.GetBool())
        {
            player.SyncSettings();
            player.RpcGuardAndKill(target, 11);
        }
        else
        {
            time = Main.AllPlayerKillCooldown[player.PlayerId] / 2;
            if (player.AmOwner) PlayerControl.LocalPlayer.SetKillTimer(time);
            else
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetKillTimer, SendOption.Reliable, player.GetClientId());
                writer.Write(time);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
            Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Observer) && target.PlayerId != x.PlayerId).Do(x => x.RpcGuardAndKill(target, 11, true));
        }
        player.ResetKillCooldown();
    }
    public static void RpcSpecificMurderPlayer(this PlayerControl killer, PlayerControl target, PlayerControl seer)
    {
        if (seer.AmOwner)
        {
            killer.MurderPlayer(target, MurderResultFlags.Succeeded);
        }
        else
        {
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, seer.GetClientId());
            messageWriter.WriteNetObject(target);
            messageWriter.Write((int)MurderResultFlags.Succeeded);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
    } //Must provide seer, target
    [Obsolete]
    public static void RpcSpecificProtectPlayer(this PlayerControl killer, PlayerControl target = null, int colorId = 0)
    {
        if (AmongUsClient.Instance.AmClient)
        {
            killer.ProtectPlayer(target, colorId);
        }
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.Reliable, killer.GetClientId());
        messageWriter.WriteNetObject(target);
        messageWriter.Write(colorId);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }
    public static void RpcResetAbilityCooldown(this PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return; // Nothing happens when run by anyone other than the host.
        Logger.Info($"Ability cooldown reset: {target.name}({target.PlayerId})", "RpcResetAbilityCooldown");
        if (target.Is(CustomRoles.Glitch))
        {
            Glitch.LastHack = Utils.GetTimeStamp();
            Glitch.LastMimic = Utils.GetTimeStamp();
            Glitch.MimicCDTimer = 10;
            Glitch.HackCDTimer = 10;
        }
        else if (PlayerControl.LocalPlayer == target)
        {
            //if target is the host
            PlayerControl.LocalPlayer.Data.Role.SetCooldown();
        }
        else
        {
            // target is other than the host
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(target.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.None, target.GetClientId());
            writer.WriteNetObject(target);
            writer.Write(0);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        /*
            When a player puts up a barrier, the cooldown of the ability is reset regardless of the player's position.
            Due to the addition of logs, it is no longer possible to put up a barrier to nothing, so we have changed it to put up a 0 second barrier to oneself instead.
            This change disables guardian angel as a position.
            The cooldown of the host resets directly.
        */
    }
    public static void RpcDesyncUpdateSystem(this PlayerControl target, SystemTypes systemType, int amount)
    {
        var messageWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.Reliable, target.GetClientId());
        messageWriter.Write((byte)systemType);
        messageWriter.WriteNetObject(target);
        messageWriter.Write((byte)amount);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }

    /*public static void RpcBeKilled(this PlayerControl player, PlayerControl KilledBy = null) {
        if(!AmongUsClient.Instance.AmHost) return;
        byte KilledById;
        if(KilledBy == null)
            KilledById = byte.MaxValue;
        else
            KilledById = KilledBy.PlayerId;

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)CustomRPC.BeKilled, Hazel.SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write(KilledById);
        AmongUsClient.Instance.FinishRpcImmediately(writer);

        RPC.BeKilled(player.PlayerId, KilledById);
    }*/
    public static void MarkDirtySettings(this PlayerControl player)
    {
        PlayerGameOptionsSender.SetDirty(player.PlayerId);
    }
    public static void SyncSettings(this PlayerControl player)
    {
        PlayerGameOptionsSender.SetDirty(player.PlayerId);
        GameOptionsSender.SendAllGameOptions();
    }
    public static TaskState GetPlayerTaskState(this PlayerControl player)
    {
        return Main.PlayerStates[player.PlayerId].TaskState;
    }
    public static string GetDisplayRoleAndSubName(this PlayerControl seer, PlayerControl target, bool notShowAddOns = false)
    {
        return Utils.GetDisplayRoleAndSubName(seer.PlayerId, target.PlayerId, notShowAddOns);
    }
    public static string GetSubRoleName(this PlayerControl player, bool forUser = false)
    {
        if (GameStates.IsHideNSeek) return string.Empty;

        var SubRoles = Main.PlayerStates[player.PlayerId].SubRoles.ToArray();
        if (SubRoles.Length == 0) return string.Empty;

        var sb = new StringBuilder();
        foreach (var role in SubRoles)
        {
            if (role == CustomRoles.NotAssigned) continue;
            sb.Append($"{Utils.ColorString(Color.white, " + ")}{Utils.GetRoleName(role, forUser)}");
        }

        return sb.ToString();
    }
    public static string GetAllRoleName(this PlayerControl player, bool forUser = true)
    {   
        if (!player) return null;
        var text = Utils.GetRoleName(player.GetCustomRole(), forUser);
        text += player.GetSubRoleName(forUser);
        return text;
    }
    public static string GetNameWithRole(this PlayerControl player, bool forUser = false)
    {
        if (!forUser)
        {
        return GetRealName(player);
        }
        return $"{player?.Data?.PlayerName}" + (GameStates.IsInGame && Options.CurrentGameMode != CustomGameMode.FFA ? $"({player?.GetAllRoleName(forUser)})" : string.Empty);
    }
    public static string GetRoleColorCode(this PlayerControl player)
    {
        return Utils.GetRoleColorCode(player.GetCustomRole());
    }
    public static Color GetRoleColor(this PlayerControl player)
    {
        return Utils.GetRoleColor(player.GetCustomRole());
    }
    public static void ResetPlayerCam(this PlayerControl pc, float delay = 0f)
    {
        if (pc == null || !AmongUsClient.Instance.AmHost || pc.AmOwner || pc.IsModClient()) return;

        var systemtypes = Utils.GetCriticalSabotageSystemType();

        _ = new LateTask(() =>
        {
            pc.RpcDesyncUpdateSystem(systemtypes, 128);
        }, 0f + delay, "Reactor Desync");

        _ = new LateTask(() =>
        {
            pc.RpcSpecificMurderPlayer(pc, pc);
        }, 0.2f + delay, "Murder To Reset Cam");

        _ = new LateTask(() =>
        {
            pc.RpcDesyncUpdateSystem(systemtypes, 16);

            if (GameStates.AirshipIsActive)
                pc.RpcDesyncUpdateSystem(systemtypes, 17);
        }, 0.4f + delay, "Fix Desync Reactor");
    }
    public static void ReactorFlash(this PlayerControl pc, float delay = 0f)
    {
        if (pc == null) return;
        // Logger.Info($"{pc}", "ReactorFlash");
        var systemtypes = Utils.GetCriticalSabotageSystemType();
        float FlashDuration = Options.KillFlashDuration.GetFloat();

        pc.RpcDesyncUpdateSystem(systemtypes, 128);

        _ = new LateTask(() =>
        {
            pc.RpcDesyncUpdateSystem(systemtypes, 16);

            if (GameStates.AirshipIsActive)
                pc.RpcDesyncUpdateSystem(systemtypes, 17);

        }, FlashDuration + delay, "Fix Desync Reactor");
    }

    public static string GetRealName(this PlayerControl player, bool isMeeting = false, bool clientData = false)
    {
        if (clientData) return player.GetClient().PlayerName;
        return isMeeting ? player?.Data?.PlayerName : player?.name;
    }
    public static bool CanUseKillButton(this PlayerControl pc)
    {
        int playerCount = Main.AllAlivePlayerControls.Length;
        if (!pc.IsAlive() || pc.Data.Role.Role == RoleTypes.GuardianAngel || Pelican.IsEaten(pc.PlayerId)) return false;
        if (Mastermind.ManipulatedPlayers.ContainsKey(pc.PlayerId)) return true;

        return pc.GetCustomRole() switch
        {
            //FFA
            CustomRoles.Killer => pc.IsAlive(),
            //Standard
            CustomRoles.Fireworker => Fireworker.CanUseKillButton(pc),
            CustomRoles.Mafia => Utils.CanMafiaKill(),
            CustomRoles.Shaman => pc.IsAlive(),
            CustomRoles.Underdog => playerCount <= Options.UnderdogMaximumPlayersNeededToKill.GetInt(),
            CustomRoles.Inhibitor => !Utils.IsActive(SystemTypes.Electrical) && !Utils.IsActive(SystemTypes.Comms) && !Utils.IsActive(SystemTypes.MushroomMixupSabotage) && !Utils.IsActive(SystemTypes.Laboratory) && !Utils.IsActive(SystemTypes.LifeSupp) && !Utils.IsActive(SystemTypes.Reactor) && !Utils.IsActive(SystemTypes.HeliSabotage),
            CustomRoles.Saboteur => Utils.IsActive(SystemTypes.Electrical) || Utils.IsActive(SystemTypes.Comms) || Utils.IsActive(SystemTypes.MushroomMixupSabotage) || Utils.IsActive(SystemTypes.Laboratory) || Utils.IsActive(SystemTypes.LifeSupp) || Utils.IsActive(SystemTypes.Reactor) || Utils.IsActive(SystemTypes.HeliSabotage),
            CustomRoles.Sniper => Sniper.CanUseKillButton(pc),
            CustomRoles.Sheriff => Sheriff.CanUseKillButton(pc.PlayerId),
            CustomRoles.Vigilante => pc.IsAlive(),
            CustomRoles.Jailer => pc.IsAlive(),
            CustomRoles.Crusader => Crusader.CanUseKillButton(pc.PlayerId),
            CustomRoles.CopyCat => pc.IsAlive(),
            CustomRoles.Pelican => pc.IsAlive(),
            CustomRoles.Mastermind => pc.IsAlive(),
            CustomRoles.Arsonist => Options.ArsonistCanIgniteAnytime.GetBool() ? Utils.GetDousedPlayerCount(pc.PlayerId).Item1 < Options.ArsonistMaxPlayersToIgnite.GetInt() : !pc.IsDouseDone(),
            CustomRoles.Revolutionist => !pc.IsDrawDone(),
            CustomRoles.Pyromaniac => pc.IsAlive(),
            CustomRoles.PlagueDoctor => pc.IsAlive() && PlagueDoctor.CanUseKillButton(),
            CustomRoles.Huntsman => pc.IsAlive(),
            CustomRoles.SwordsMan => pc.IsAlive(),
            CustomRoles.Jackal => pc.IsAlive(),
            CustomRoles.Bandit => pc.IsAlive(),
            CustomRoles.Sidekick => pc.IsAlive(),
            CustomRoles.Necromancer => pc.IsAlive(),
            CustomRoles.HexMaster => pc.IsAlive(),
            //CustomRoles.Occultist => pc.IsAlive(),
            CustomRoles.Poisoner => pc.IsAlive(),
            CustomRoles.Juggernaut => pc.IsAlive(),
            CustomRoles.Reverie => pc.IsAlive(),
            CustomRoles.PotionMaster => pc.IsAlive(),
            CustomRoles.SerialKiller => pc.IsAlive(),
            CustomRoles.Werewolf => pc.IsAlive(),
            CustomRoles.Medusa => pc.IsAlive(),
            CustomRoles.Traitor => pc.IsAlive(),
            CustomRoles.Glitch => pc.IsAlive(),
            CustomRoles.Pickpocket => pc.IsAlive(),
            CustomRoles.Maverick => pc.IsAlive(),
            CustomRoles.Jinx => pc.IsAlive(),
            CustomRoles.Parasite => pc.IsAlive(),
            CustomRoles.Refugee => pc.IsAlive(),
    //        CustomRoles.Minion => pc.IsAlive(),
            CustomRoles.Witness => pc.IsAlive(),
            CustomRoles.Shroud => pc.IsAlive(),
            CustomRoles.Wraith => pc.IsAlive(),
            CustomRoles.Bomber => Options.BomberCanKill.GetBool() && pc.IsAlive(),
            CustomRoles.Nuker => Options.BomberCanKill.GetBool() && pc.IsAlive(),
            CustomRoles.Innocent => pc.IsAlive(),
            CustomRoles.Counterfeiter => Counterfeiter.CanUseKillButton(pc.PlayerId),
            CustomRoles.Pursuer => Pursuer.CanUseKillButton(pc.PlayerId),
            CustomRoles.Morphling => Morphling.CanUseKillButton(pc.PlayerId),
            CustomRoles.Hater => pc.IsAlive(),
            CustomRoles.Medic => Medic.CanUseKillButton(pc.PlayerId),
            CustomRoles.Gamer => pc.IsAlive(),
            CustomRoles.DarkHide => DarkHide.CanUseKillButton(pc),
            CustomRoles.Provocateur => pc.IsAlive(),
            CustomRoles.Assassin => Assassin.CanUseKillButton(pc),
            CustomRoles.BloodKnight => pc.IsAlive(),
            CustomRoles.Crewpostor => false,
            CustomRoles.Totocalcio => Totocalcio.CanUseKillButton(pc),
            CustomRoles.Romantic => pc.IsAlive(),
            CustomRoles.RuthlessRomantic => pc.IsAlive(),
            CustomRoles.VengefulRomantic => VengefulRomantic.CanUseKillButton(pc),
            CustomRoles.Succubus => Succubus.CanUseKillButton(pc),
            CustomRoles.CursedSoul => CursedSoul.CanUseKillButton(pc),
            CustomRoles.Admirer => Admirer.CanUseKillButton(pc),
            CustomRoles.Imitator => Imitator.CanUseKillButton(pc),
            //CustomRoles.Warlock => !Main.isCurseAndKill.TryGetValue(pc.PlayerId, out bool wcs) || !wcs,
            CustomRoles.Infectious => Infectious.CanUseKillButton(pc),
            CustomRoles.Monarch => Monarch.CanUseKillButton(pc),
            CustomRoles.Deputy => Deputy.CanUseKillButton(pc),
            CustomRoles.Investigator => Investigator.CanUseKillButton(pc),
            CustomRoles.Virus => pc.IsAlive(),
            CustomRoles.Farseer => pc.IsAlive(),
            CustomRoles.Spiritcaller => pc.IsAlive(),
            CustomRoles.PlagueBearer => pc.IsAlive(),
            CustomRoles.Pestilence => pc.IsAlive(),
            CustomRoles.Pirate => pc.IsAlive(),
            CustomRoles.Pixie => pc.IsAlive(),
            CustomRoles.Seeker => pc.IsAlive(),
            CustomRoles.Agitater => pc.IsAlive(),
            CustomRoles.ChiefOfPolice => ChiefOfPolice.CanUseKillButton(pc.PlayerId),
            CustomRoles.EvilMini => pc.IsAlive(),
            CustomRoles.Doppelganger => pc.IsAlive(),
            CustomRoles.Quizmaster => Quizmaster.CanUseKillButton(pc),

            _ => pc.Is(CustomRoleTypes.Impostor),
        };
    }
    public static bool HasKillButton(PlayerControl pc = null, CustomRoles role = new())
    {
        if (pc == null) return false;
        if (!pc.IsAlive() || pc.Data.Role.Role == RoleTypes.GuardianAngel || Pelican.IsEaten(pc.PlayerId)) return false;
        role = pc.GetCustomRole();
        if (!role.IsImpostor())
        {
            return role.GetDYRole() == RoleTypes.Impostor;
        }
        return role.GetVNRole() switch
        { 
            CustomRoles.Impostor => true,
            CustomRoles.Shapeshifter => true,
            _ => false
        };
    }
    public static bool CanUseImpostorVentButton(this PlayerControl pc)
    {
        if (!pc.IsAlive() || pc.Data.Role.Role == RoleTypes.GuardianAngel) return false;
        if (GameStates.IsHideNSeek) return true;
        if (CopyCat.playerIdList.Contains(pc.PlayerId)) return true;
        if (Main.TasklessCrewmate.Contains(pc.PlayerId)) return true;
        if (Necromancer.Killer && !pc.Is(CustomRoles.Necromancer)) return false;
        if (pc.Is(CustomRoles.Nimble)) return true;
        if (pc.Is(CustomRoles.Circumvent)) return false;

        return pc.GetCustomRole() switch
        {
            CustomRoles.KillingMachine or
            CustomRoles.Sheriff or
            CustomRoles.Vigilante or
            CustomRoles.Deputy or
            CustomRoles.Investigator or
            CustomRoles.Innocent or
            //    CustomRoles.SwordsMan or
            CustomRoles.Hater or
            CustomRoles.Medic or
            //      CustomRoles.NWitch or
            CustomRoles.Monarch or
            CustomRoles.Romantic or
            CustomRoles.Provocateur or
            CustomRoles.Totocalcio or
            CustomRoles.Succubus or
            CustomRoles.CursedSoul or
            CustomRoles.PlagueBearer or
            CustomRoles.PlagueDoctor or
            CustomRoles.Admirer or
            CustomRoles.Crusader or
            CustomRoles.ChiefOfPolice or
            CustomRoles.Wildling
            => false,

            CustomRoles.Jackal => Jackal.CanVent.GetBool(),
            CustomRoles.Bandit => Bandit.CanVent.GetBool(),
            CustomRoles.VengefulRomantic => Romantic.VengefulCanVent.GetBool(),
            CustomRoles.Glitch => Glitch.CanVent.GetBool(),
            CustomRoles.RuthlessRomantic => Romantic.RuthlessCanVent.GetBool(),
            CustomRoles.Huntsman => Huntsman.CanVent.GetBool(),
            CustomRoles.Sidekick => Jackal.CanVentSK.GetBool(),
            CustomRoles.Poisoner => Poisoner.CanVent.GetBool(),
            CustomRoles.Vampiress => Vampire.CanVent.GetBool(),
            CustomRoles.Vampire => Vampire.CanVent.GetBool(),
            CustomRoles.DarkHide => DarkHide.CanVent.GetBool(),
            CustomRoles.SerialKiller => SerialKiller.CanVent.GetBool(),
            CustomRoles.Werewolf => Werewolf.CanVent.GetBool(),
            CustomRoles.Pestilence => PlagueBearer.PestilenceCanVent.GetBool(),
            CustomRoles.Medusa => Medusa.CanVent.GetBool(),
            CustomRoles.Traitor => Traitor.CanVent.GetBool(),
            CustomRoles.Necromancer => Necromancer.CanVent.GetBool(),
            CustomRoles.Shroud => Shroud.CanVent.GetBool(),
            CustomRoles.Maverick => Maverick.CanVent.GetBool(),
            CustomRoles.Jinx => Jinx.CanVent.GetBool(),
            CustomRoles.Pelican => Pelican.CanVent.GetBool(),
            CustomRoles.Gamer => Gamer.CanVent.GetBool(),
            CustomRoles.BloodKnight => BloodKnight.CanVent.GetBool(),
            CustomRoles.Juggernaut => Juggernaut.CanVent.GetBool(),
            CustomRoles.Infectious => Infectious.CanVent.GetBool(),
            CustomRoles.Doppelganger => Doppelganger.CanVent.GetBool(),
            CustomRoles.PotionMaster => PotionMaster.CanVent.GetBool(),
            CustomRoles.Virus => Virus.CanVent.GetBool(),
            CustomRoles.SwordsMan => SwordsMan.CanVent.GetBool(),
            CustomRoles.Pickpocket => Pickpocket.CanVent.GetBool(),
            CustomRoles.HexMaster => true,
            //CustomRoles.Occultist => true,
            CustomRoles.Wraith => true,
            CustomRoles.Pyromaniac => Pyromaniac.CanVent.GetBool(),
            CustomRoles.Amnesiac => true,
            //   CustomRoles.Chameleon => true,
            CustomRoles.Parasite => true,
            CustomRoles.Refugee => true,
            CustomRoles.Spiritcaller => Spiritcaller.CanVent.GetBool(),
            CustomRoles.Quizmaster => Quizmaster.CanUseVentButton(pc),

            CustomRoles.Arsonist => pc.IsDouseDone() || (Options.ArsonistCanIgniteAnytime.GetBool() && (Utils.GetDousedPlayerCount(pc.PlayerId).Item1 >= Options.ArsonistMinPlayersToIgnite.GetInt() || pc.inVent)),
            CustomRoles.Revolutionist => pc.IsDrawDone(),

            //FFA
            CustomRoles.Killer => true,

            _ => pc.Is(CustomRoleTypes.Impostor),
        };
    }
    public static bool CanUseSabotage(this PlayerControl pc) // NOTE: THIS IS FOR THE HUD FOR MODDED CLIENTS, THIS DOES NOT DETERMINE WHETHER A ROLE CAN SABOTAGE
    {
        if (pc.Data.Role.Role == RoleTypes.GuardianAngel) return false;

        if (pc.Is(CustomRoleTypes.Impostor))
        {
            return pc.GetCustomRole() switch
            {
                CustomRoles.KillingMachine => false,

                _ => !(!pc.IsAlive() && Options.DeadImpCantSabotage.GetBool()),
            };
        }
        else
        {
            return pc.GetCustomRole() switch
            {
                CustomRoles.Bandit => Bandit.CanUseSabotage.GetBool(),
                CustomRoles.Jackal => Jackal.CanUseSabotage.GetBool(),
                CustomRoles.Sidekick => Jackal.CanUseSabotageSK.GetBool(),
                CustomRoles.Traitor => Traitor.CanUseSabotage.GetBool(),

                CustomRoles.Parasite or
                CustomRoles.Glitch or
                CustomRoles.PotionMaster or
                CustomRoles.Refugee
                => true,

                _ => false,
            };
        }
    }
    public static bool IsDousedPlayer(this PlayerControl arsonist, PlayerControl target)
    {
        if (arsonist == null || target == null || Main.isDoused == null) return false;
        Main.isDoused.TryGetValue((arsonist.PlayerId, target.PlayerId), out bool isDoused);
        return isDoused;
    }
    public static bool IsDrawPlayer(this PlayerControl arsonist, PlayerControl target)
    {
        if (arsonist == null && target == null && Main.isDraw == null) return false;
        Main.isDraw.TryGetValue((arsonist.PlayerId, target.PlayerId), out bool isDraw);
        return isDraw;
    }
    public static bool IsRevealedPlayer(this PlayerControl player, PlayerControl target)
    {
        if (player == null || target == null || Main.isRevealed == null) return false;
        Main.isRevealed.TryGetValue((player.PlayerId, target.PlayerId), out bool isDoused);
        return isDoused;
    }
    public static void RpcSetDousedPlayer(this PlayerControl player, PlayerControl target, bool isDoused)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDousedPlayer, SendOption.Reliable, -1);//RPCによる同期
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        writer.Write(isDoused);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcSetDrawPlayer(this PlayerControl player, PlayerControl target, bool isDoused)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDrawPlayer, SendOption.Reliable, -1);//RPCによる同期
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        writer.Write(isDoused);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcSetRevealtPlayer(this PlayerControl player, PlayerControl target, bool isDoused)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRevealedPlayer, SendOption.Reliable, -1);//RPCによる同期
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        writer.Write(isDoused);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ResetKillCooldown(this PlayerControl player)
    {
        Main.AllPlayerKillCooldown[player.PlayerId] = GameStates.IsNormalGame ? Options.DefaultKillCooldown : 1f; //キルクールをデフォルトキルクールに変更
        switch (player.GetCustomRole())
        {
            case CustomRoles.Mercenary:
                Mercenary.ApplyKillCooldown(player.PlayerId); //シリアルキラーはシリアルキラーのキルクールに。
                break;
            case CustomRoles.Jailer:
                Jailer.SetKillCooldown(player.PlayerId); //シリアルキラーはシリアルキラーのキルクールに。
                break;
            case CustomRoles.Vigilante:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.VigilanteKillCooldown.GetFloat();
                break;
            case CustomRoles.TimeThief:
                TimeThief.SetKillCooldown(player.PlayerId); //タイムシーフはタイムシーフのキルクールに。
                break;
            case CustomRoles.Agitater:
                Agitater.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.PlagueDoctor:
                PlagueDoctor.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Berserker:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.BerserkerKillCooldown.GetFloat();
                break;
            case CustomRoles.Kamikaze:
                Kamikaze.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Penguin:
                Penguin.SetKillCooldown(player.PlayerId);
                break;
            /* case CustomRoles.Mare:
                 Mare.SetKillCooldown(player.PlayerId);
                 break; */
            case CustomRoles.EvilDiviner:
                EvilDiviner.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Morphling:
                Morphling.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.PotionMaster:
                PotionMaster.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Pyromaniac:
                Pyromaniac.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Pickpocket:
                Pickpocket.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Arsonist:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.ArsonistCooldown.GetFloat(); //アーソニストはアーソニストのキルクールに。
                break;
            case CustomRoles.Inhibitor:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.InhibitorCD.GetFloat(); //アーソニストはアーソニストのキルクールに。
                break;
            case CustomRoles.Saboteur:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.SaboteurCD.GetFloat(); //アーソニストはアーソニストのキルクールに。
                break;
            case CustomRoles.Mastermind:
                Main.AllPlayerKillCooldown[player.PlayerId] = Mastermind.KillCooldown.GetFloat();
                break;
            case CustomRoles.Revolutionist:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.RevolutionistCooldown.GetFloat();
                break;
            case CustomRoles.Underdog:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.UnderdogKillCooldown.GetFloat();
                break;
            case CustomRoles.Undertaker:
                Undertaker.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.RiftMaker:
                RiftMaker.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Jackal:
                Jackal.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Sidekick:
                Sidekick.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Bandit:
                Bandit.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Instigator:
                Instigator.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Doppelganger:
                Doppelganger.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.PlagueBearer:
                PlagueBearer.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Pestilence:
                PlagueBearer.SetKillCooldownPestilence(player.PlayerId);
                break;

            case CustomRoles.Councillor:
                Councillor.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Parasite:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.ParasiteCD.GetFloat();
                break;
            case CustomRoles.Shaman:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.VoodooCooldown.GetFloat();
                break;
            case CustomRoles.Refugee:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.RefugeeKillCD.GetFloat();
                break;
            case CustomRoles.SerialKiller:
                SerialKiller.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Werewolf:
                Main.AllPlayerKillCooldown[player.PlayerId] = Werewolf.KillCooldown.GetFloat();
                break;
            case CustomRoles.Necromancer:
                Necromancer.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Traitor:
                Traitor.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Glitch:
                Glitch.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Huntsman:
                Main.AllPlayerKillCooldown[player.PlayerId] = Huntsman.KCD;
                break;
            case CustomRoles.Chronomancer:
                Chronomancer.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Stealth:
                Stealth.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Shroud:
                Shroud.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Maverick:
                Maverick.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Jinx:
                Jinx.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Poisoner:
                Poisoner.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Sheriff:
                Sheriff.SetKillCooldown(player.PlayerId); //シェリフはシェリフのキルクールに。
                break;
            case CustomRoles.CopyCat:
                CopyCat.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.KillingMachine:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.MNKillCooldown.GetFloat();
                break;
            case CustomRoles.SwordsMan:
                SwordsMan.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Zombie:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.ZombieKillCooldown.GetFloat();
                Main.AllPlayerSpeed[player.PlayerId] -= Options.ZombieSpeedReduce.GetFloat();
                break;
            case CustomRoles.BoobyTrap:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.BTKillCooldown.GetFloat();
                break;
            case CustomRoles.Scavenger:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.ScavengerKillCooldown.GetFloat();
                break;
            case CustomRoles.Bomber:
            case CustomRoles.Nuker:
                if (Options.BomberCanKill.GetBool())
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.BomberKillCD.GetFloat();
                else
                Main.AllPlayerKillCooldown[player.PlayerId] = 300f;
                break;
            case CustomRoles.Witness:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.WitnessCD.GetFloat();
                break;
            //case CustomRoles.Capitalism:
            //    Main.AllPlayerKillCooldown[player.PlayerId] = Options.CapitalismSkillCooldown.GetFloat();
            //    break;
            case CustomRoles.Pelican:
                Main.AllPlayerKillCooldown[player.PlayerId] = Pelican.KillCooldown.GetFloat();
                break;
            case CustomRoles.Counterfeiter:
                Counterfeiter.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Pursuer:
                Pursuer.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Hater:
                Main.AllPlayerKillCooldown[player.PlayerId] = 1f;
                break;
            case CustomRoles.Medusa:
                Medusa.SetKillCooldown(player.PlayerId);
                break;

            case CustomRoles.Cleaner:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.CleanerKillCooldown.GetFloat();
                break;
            case CustomRoles.Ludopath:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.LudopathRandomKillCD.GetFloat();
                break;
            case CustomRoles.Medic:
                Medic.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Gamer:
                Gamer.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.BallLightning:
                BallLightning.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.DarkHide:
                DarkHide.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Greedier:
                Greedier.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.QuickShooter:
                QuickShooter.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Provocateur:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.ProvKillCD.GetFloat();
                break;
            case CustomRoles.Assassin:
                Assassin.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Vampiress:
                Vampiress.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Arrogance:
                Arrogance.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Juggernaut:
                Juggernaut.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Reverie:
                Reverie.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Anonymous:
                Anonymous.SetKillCooldown(player.PlayerId);
                break;
            //FFA
            case CustomRoles.Killer:
                Main.AllPlayerKillCooldown[player.PlayerId] = FFAManager.FFA_KCD.GetFloat();
                break;
            case CustomRoles.BloodKnight:
                BloodKnight.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Totocalcio:
                Totocalcio.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Romantic:
                Romantic.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.VengefulRomantic:
                Main.AllPlayerKillCooldown[player.PlayerId] = Romantic.VengefulKCD.GetFloat();
                break;
            case CustomRoles.RuthlessRomantic:
                Main.AllPlayerKillCooldown[player.PlayerId] = Romantic.RuthlessKCD.GetFloat();
                break;
            case CustomRoles.Gangster:
                Gangster.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Succubus:
                Succubus.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.CursedSoul:
                CursedSoul.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Admirer:
                Admirer.SetKillCooldown(player.PlayerId);
                break;
        /*    case CustomRoles.Imitator:
                Imitator.SetKillCooldown(player.PlayerId);
                break; */
            case CustomRoles.Infectious:
                Infectious.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Monarch:
                Monarch.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Pirate:
                Pirate.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Pixie:
                Pixie.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Deputy:
                Deputy.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Investigator:
                Investigator.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Virus:
                Virus.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Farseer:
                Farseer.SetCooldown(player.PlayerId);
                break;
            case CustomRoles.Dazzler:
                Dazzler.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Deathpact:
                Deathpact.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Devourer:
                Devourer.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Spiritcaller:
                Spiritcaller.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Lurker:
                Lurker.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Crusader:
                Crusader.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Seeker:
                Seeker.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.ChiefOfPolice:
                ChiefOfPolice.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.EvilMini:
                Main.AllPlayerKillCooldown[player.PlayerId] = Mini.GetKillCoolDown();
                break;
            case CustomRoles.Quizmaster:
                Quizmaster.SetKillCooldown(player.PlayerId);
                break;
        }
        if (player.PlayerId == LastImpostor.currentId)
            LastImpostor.SetKillCooldown();

        if (player.Is(CustomRoles.Mare))
            Main.AllPlayerKillCooldown[player.PlayerId] = Mare.KillCooldownInLightsOut.GetFloat();

        if (player.Is(CustomRoles.Overclocked))
            Main.AllPlayerKillCooldown[player.PlayerId] -= Main.AllPlayerKillCooldown[player.PlayerId] * (Options.OverclockedReduction.GetFloat() / 100);
        
        if (Main.KilledDiseased.ContainsKey(player.PlayerId))
        {
            Main.AllPlayerKillCooldown[player.PlayerId] = Main.AllPlayerKillCooldown[player.PlayerId] + Main.KilledDiseased[player.PlayerId] * Options.DiseasedCDOpt.GetFloat();
            Logger.Info($"kill cd of player set to {Main.AllPlayerKillCooldown[player.PlayerId]}", "Diseased");
        }
        if (Main.KilledAntidote.ContainsKey(player.PlayerId))
        {
            var kcd = Main.AllPlayerKillCooldown[player.PlayerId] - Main.KilledAntidote[player.PlayerId] * Options.AntidoteCDOpt.GetFloat();
            if (kcd < 0) kcd = 0;
            Main.AllPlayerKillCooldown[player.PlayerId] = kcd;
            Logger.Info($"kill cd of player set to {Main.AllPlayerKillCooldown[player.PlayerId]}", "Antidote");
        }
        if (!player.HasImpKillButton(considerVanillaShift: false))
            Main.AllPlayerKillCooldown[player.PlayerId] = 300f;
        if (Main.AllPlayerKillCooldown[player.PlayerId] == 0)
        {
            if (player.Is(CustomRoles.Chronomancer)) return;
            Main.AllPlayerKillCooldown[player.PlayerId] = 0.3f;
        }
    }
    public static bool IsNonCrewSheriff(this PlayerControl sheriff)
    {
        return sheriff.Is(CustomRoles.Madmate)
            || sheriff.Is(CustomRoles.Charmed)
            || sheriff.Is(CustomRoles.Infected)
            || sheriff.Is(CustomRoles.Contagious)
            || sheriff.Is(CustomRoles.Egoist);
    }
    public static bool IsEvilAddons(this PlayerControl target)
    {
        return target.Is(CustomRoles.Madmate)
            || target.Is(CustomRoles.Egoist)
            || target.Is(CustomRoles.Charmed)
            || target.Is(CustomRoles.Recruit)
            || target.Is(CustomRoles.Infected)
            || target.Is(CustomRoles.Contagious)
            || target.Is(CustomRoles.Rogue)
            || target.Is(CustomRoles.Rascal)
            || target.Is(CustomRoles.Soulless);
    }
    public static bool ShouldBeDisplayed(this CustomRoles subRole)
    {
        return subRole is not 
            CustomRoles.LastImpostor and not
            CustomRoles.Madmate and not
            CustomRoles.Charmed and not
            CustomRoles.Recruit and not
            CustomRoles.Admired and not
            CustomRoles.Soulless and not
            CustomRoles.Lovers and not
            CustomRoles.Infected and not
            CustomRoles.Contagious;
    }
    public static bool IsAmneCrew(this PlayerControl target)
    {
        return //target.Is(CustomRoles.Luckey)
            target.Is(CustomRoles.Needy)
            || target.Is(CustomRoles.SuperStar)
            || target.Is(CustomRoles.CyberStar)
            || target.Is(CustomRoles.Mayor)
            || target.Is(CustomRoles.Paranoia)
            || target.Is(CustomRoles.Dictator)
            || target.Is(CustomRoles.NiceGuesser)
            || target.Is(CustomRoles.Bodyguard)
            || target.Is(CustomRoles.Observer)
            || target.Is(CustomRoles.Retributionist)
            || target.Is(CustomRoles.Lookout)
            || target.Is(CustomRoles.Bodyguard);
    }
    public static bool IsCrewVenter(this PlayerControl target)
    {
        return target.Is(CustomRoles.EngineerTOHE)
            || target.Is(CustomRoles.SabotageMaster)
            || target.Is(CustomRoles.CopyCat)
            || target.Is(CustomRoles.Monitor) && Monitor.CanVent.GetBool()
            || target.Is(CustomRoles.SwordsMan) && SwordsMan.CanVent.GetBool()
            || target.Is(CustomRoles.Nimble);
    }
    public static void TrapperKilled(this PlayerControl killer, PlayerControl target)
    {
        Logger.Info($"{target?.Data?.PlayerName} was Trapper", "Trapper");
        var tmpSpeed = Main.AllPlayerSpeed[killer.PlayerId];
        Main.AllPlayerSpeed[killer.PlayerId] = Main.MinSpeed;
        ReportDeadBodyPatch.CanReport[killer.PlayerId] = false;
        killer.MarkDirtySettings();
        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[killer.PlayerId] = Main.AllPlayerSpeed[killer.PlayerId] - Main.MinSpeed + tmpSpeed;
            ReportDeadBodyPatch.CanReport[killer.PlayerId] = true;
            killer.MarkDirtySettings();
            RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
        }, Options.TrapperBlockMoveTime.GetFloat(), "Trapper BlockMove");
    }
    public static bool IsDouseDone(this PlayerControl player)
    {
        if (!player.Is(CustomRoles.Arsonist)) return false;
        var (countItem1, countItem2) = Utils.GetDousedPlayerCount(player.PlayerId);
        return countItem1 >= countItem2;
    }
    public static bool IsDrawDone(this PlayerControl player)
    {
        if (!player.Is(CustomRoles.Revolutionist)) return false;
        var (countItem1, countItem2) = Utils.GetDrawPlayerCount(player.PlayerId, out var _);
        return countItem1 >= countItem2;
    }
    public static void RpcExileV2(this PlayerControl player)
    {
        if (player.Is(CustomRoles.Susceptible))
        {
            Susceptible.CallEnabledAndChange(player);
        }
        player.Exiled();
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Exiled, SendOption.None, -1);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcMurderPlayerV3(this PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.Pestilence) && killer != target) { killer.SetRealKiller(target); (killer, target) = (target, killer); Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.PissedOff; goto postPest; }
        else if (target.Is(CustomRoles.Pestilence) && target.GetRealKiller() != null && target.GetRealKiller() != target) { var truekill = target.GetRealKiller(); (killer, target) = (target, truekill); Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.PissedOff; goto postPest; }
        else if(target.Is(CustomRoles.Pestilence)) { return; }
        postPest:

        if (target.Is(CustomRoles.Susceptible))
        {
            Susceptible.CallEnabledAndChange(target);
        }
        
        if (target.Is(CustomRoles.Solsticer))
        {
            if (!GameStates.IsMeeting)
            {
                if (target.PlayerId != killer.PlayerId)
                {
                    killer.RpcTeleport(target.GetTruePosition());
                    killer.RpcGuardAndKill(target);
                    killer.SetKillCooldown(forceAnime: true);
                    NameNotifyManager.Notify(killer, GetString("MurderSolsticer"));
                }

                target.RpcGuardAndKill();
                Solsticer.patched = true;
                Solsticer.ResetTasks(target);
                target.MarkDirtySettings();

                NameNotifyManager.Notify(target, string.Format(GetString("SolsticerMurdered"), killer.GetRealName()));
                if (Solsticer.SolsticerKnowKiller.GetBool())
                    Solsticer.MurderMessage = string.Format(GetString("SolsticerMurderMessage"), killer.GetRealName(), GetString(killer.GetCustomRole().ToString()));
                else Solsticer.MurderMessage = "";
            }
            //Solsticer wont die anyway.
            return;
        }

        if (killer.PlayerId == target.PlayerId && killer.shapeshifting)
        {
            _ = new LateTask(() => { killer.RpcMurderPlayer(target, true); }, 1.5f, "Shapeshifting Suicide Delay");
            return;
        }

        killer.RpcMurderPlayer(target, true);
    }
    public static void RpcMurderPlayerV2(this PlayerControl killer, PlayerControl target)
    {
        if (target == null) target = killer;
        if (AmongUsClient.Instance.AmClient)
        {
            killer.MurderPlayer(target, ResultFlags);
        }
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, -1);
        messageWriter.WriteNetObject(target);
        messageWriter.Write((int)ResultFlags);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        Utils.NotifyRoles();
    }
    public static bool RpcCheckAndMurder(this PlayerControl killer, PlayerControl target, bool check = false) => CheckMurderPatch.RpcCheckAndMurder(killer, target, check);
    public static void NoCheckStartMeeting(this PlayerControl reporter, GameData.PlayerInfo target, bool force = false)
    { 
        //Method that can cause a meeting to occur regardless of whether it is in sabotage.
        //If target is null, it becomes a button.
        if (Options.DisableMeeting.GetBool() && !force) return;

        ReportDeadBodyPatch.AfterReportTasks(reporter, target);
        MeetingRoomManager.Instance.AssignSelf(reporter, target);
        DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(reporter);
        reporter.RpcStartMeeting(target);
    }
    public static bool IsModClient(this PlayerControl player) => Main.playerVersion.ContainsKey(player.GetClientId());
    ///<summary>
    ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、戻り値を返します。
    ///</summary>
    ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
    ///<returns>GetPlayersInAbilityRangeSortedの戻り値</returns>
    public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, bool ignoreColliders = false) => GetPlayersInAbilityRangeSorted(player, pc => true, ignoreColliders);
    ///<summary>
    ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、predicateの条件に合わないものを除外して返します。
    ///</summary>
    ///<param name="predicate">リストに入れるプレイヤーの条件 このpredicateに入れてfalseを返すプレイヤーは除外されます。</param>
    ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
    ///<returns>GetPlayersInAbilityRangeSortedの戻り値から条件に合わないプレイヤーを除外したもの。</returns>
    public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, Predicate<PlayerControl> predicate, bool ignoreColliders = false)
    {
        var rangePlayersIL = RoleBehaviour.GetTempPlayerList();
        List<PlayerControl> rangePlayers = [];
        player.Data.Role.GetPlayersInAbilityRangeSorted(rangePlayersIL, ignoreColliders);
        foreach (var pc in rangePlayersIL.ToArray())
        {
            if (predicate(pc)) rangePlayers.Add(pc);
        }
        return rangePlayers;
    }
    public static bool IsNeutralKiller(this PlayerControl player) => player.GetCustomRole().IsNK();
    public static bool IsNeutralBenign(this PlayerControl player) => player.GetCustomRole().IsNB();
    public static bool IsNeutralEvil(this PlayerControl player) => player.GetCustomRole().IsNE();
    public static bool IsNeutralChaos(this PlayerControl player) => player.GetCustomRole().IsNC();
    public static bool IsNonNeutralKiller(this PlayerControl player) => player.GetCustomRole().IsNonNK();
    public static bool IsSnitchTarget(this PlayerControl player) => player.GetCustomRole().IsSnitchTarget();
    
    public static bool KnowDeathReason(this PlayerControl seer, PlayerControl target)
        => (seer.Is(CustomRoles.Doctor) || seer.Is(CustomRoles.Autopsy)
        || (seer.Data.IsDead && Options.GhostCanSeeDeathReason.GetBool()))
        && target.Data.IsDead || target.Is(CustomRoles.Gravestone) && target.Data.IsDead;

    public static bool KnowDeadTeam(this PlayerControl seer, PlayerControl target)
        => (seer.Is(CustomRoles.Necroview))
        && target.Data.IsDead;

    public static bool KnowLivingTeam(this PlayerControl seer, PlayerControl target)
        => (seer.Is(CustomRoles.Visionary))
        && !target.Data.IsDead;

    public static bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
    {
        if (seer.Is(CustomRoles.GM) || target.Is(CustomRoles.GM) || seer.Is(CustomRoles.God) || (seer.AmOwner && Main.GodMode.Value)) return true;
        else if (Options.CurrentGameMode == CustomGameMode.FFA) return true;
        else if (target.Is(CustomRoles.Solsticer) && Solsticer.EveryOneKnowSolsticer.GetBool()) return true;
        else if (Main.VisibleTasksCount && !seer.IsAlive() && Options.GhostCanSeeOtherRoles.GetBool()) return true;
        else if (target.Is(CustomRoles.Gravestone) && !target.IsAlive()) return true;
        else if (Options.SeeEjectedRolesInMeeting.GetBool() && Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.Vote) return true;
        else if (Options.MimicCanSeeDeadRoles.GetBool() && Main.VisibleTasksCount && seer.Is(CustomRoles.Mimic) && target.Data.IsDead) return true;
        else if (Options.LoverKnowRoles.GetBool() && (seer.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers)) || target.Is(CustomRoles.Ntr)) return true;
        else if (Options.ImpKnowAlliesRole.GetBool() && seer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoleTypes.Impostor)) return true;
        else if (Options.MadmateKnowWhosImp.GetBool() && seer.Is(CustomRoles.Madmate) && target.Is(CustomRoleTypes.Impostor)) return true;
        else if (Options.ImpKnowWhosMadmate.GetBool() && target.Is(CustomRoles.Madmate) && seer.Is(CustomRoleTypes.Impostor)) return true;
        else if (Options.AlliesKnowCrewpostor.GetBool() && seer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoles.Crewpostor)) return true;
        else if (Options.CrewpostorKnowsAllies.GetBool() && seer.Is(CustomRoles.Crewpostor) && target.Is(CustomRoleTypes.Impostor)) return true;
        else if (Options.WorkaholicVisibleToEveryone.GetBool() && target.Is(CustomRoles.Workaholic)) return true;
        else if (Options.DoctorVisibleToEveryone.GetBool() && target.Is(CustomRoles.Doctor)) return true;
        else if (Options.MayorRevealWhenDoneTasks.GetBool() && target.Is(CustomRoles.Mayor) && target.GetPlayerTaskState().IsTaskFinished) return true;
        else if (target.GetPlayerTaskState().IsTaskFinished && seer.Is(CustomRoleTypes.Crewmate) && target.Is(CustomRoles.Marshall)) return true;
        else if (seer.Is(CustomRoles.Jackal) && (target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit))) return true;
        else if (seer.Is(CustomRoles.Sidekick) && (target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.Recruit) || target.Is(CustomRoles.Sidekick))) return true;
        else if (seer.Is(CustomRoles.Recruit) && (target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit))) return true;
        else if (seer.IsRevealedPlayer(target) && !target.Is(CustomRoles.Trickster)) return true;
        else if (Totocalcio.KnowRole(seer, target)) return true;
        else if (Romantic.KnowRole(seer, target)) return true;
        else if (Lawyer.KnowRole(seer, target)) return true;
        else if (EvilDiviner.IsShowTargetRole(seer, target)) return true;
        else if (PotionMaster.IsShowTargetRole(seer, target)) return true;
        else if (Executioner.KnowRole(seer, target)) return true;
        else if (Succubus.KnowRole(seer, target)) return true;
        else if (CursedSoul.KnowRole(seer, target)) return true;
        else if (Admirer.KnowRole(seer, target)) return true;
        else if (Amnesiac.KnowRole(seer, target)) return true;
        else if (Infectious.KnowRole(seer, target)) return true;
        else if (Virus.KnowRole(seer, target)) return true;
        else if ((target.Is(CustomRoles.President) && seer.GetCustomRole().IsCrewmate() && !seer.Is(CustomRoles.Madmate) && President.CheckPresidentReveal[target.PlayerId] == true) ||
                (target.Is(CustomRoles.President) && seer.Is(CustomRoles.Madmate) && President.MadmatesSeePresident.GetBool() && President.CheckPresidentReveal[target.PlayerId] == true) ||
                (target.Is(CustomRoles.President) && seer.GetCustomRole().IsNeutral() && President.NeutralsSeePresident.GetBool() && President.CheckPresidentReveal[target.PlayerId] == true) ||
                (target.Is(CustomRoles.President) && (seer.GetCustomRole().IsImpostorTeam()) && President.ImpsSeePresident.GetBool() && President.CheckPresidentReveal[target.PlayerId] == true)) return true;


        else return false;
    }
    public static bool ShowSubRoleTarget(this PlayerControl seer, PlayerControl target, CustomRoles subRole = CustomRoles.NotAssigned)
    {
        if (seer == null) return false;
        if (target == null) target = seer;

        if (seer.PlayerId == target.PlayerId) return true;
        else if (seer.Is(CustomRoles.GM) || target.Is(CustomRoles.GM) || seer.Is(CustomRoles.God) || (seer.AmOwner && Main.GodMode.Value)) return true;
        else if (Main.VisibleTasksCount && !seer.IsAlive() && Options.GhostCanSeeOtherRoles.GetBool()) return true;
        
        else if (
            (subRole.Is(CustomRoles.Madmate)
                || subRole.Is(CustomRoles.Sidekick) 
                || subRole.Is(CustomRoles.Recruit)
                || subRole.Is(CustomRoles.Admired)
                || subRole.Is(CustomRoles.Charmed)
                || subRole.Is(CustomRoles.Infected)
                || subRole.Is(CustomRoles.Contagious)
                || subRole.Is(CustomRoles.Egoist)) 
            && KnowSubRoleTarget(seer, target))
            return true;

        
        return false;
    }
    public static bool KnowSubRoleTarget(PlayerControl seer, PlayerControl target)
    {
        //if (seer == null) return false;
        //if (target == null) target = seer;
        
        if (seer.Is(CustomRoleTypes.Impostor))
        {
            // Imp know Madmate
            if (target.Is(CustomRoles.Madmate) && Options.ImpKnowWhosMadmate.GetBool())
                return true;

            // Ego-Imp know other Ego-Imp
            else if (seer.Is(CustomRoles.Egoist) && target.Is(CustomRoles.Egoist) && Options.ImpEgoistVisibalToAllies.GetBool())
                return true;
        }
        else if (Admirer.IsEnable && Admirer.KnowRole(seer, target)) return true;
        else if (Succubus.IsEnable && Succubus.KnowRole(seer, target)) return true;
        else if (Infectious.IsEnable && Infectious.KnowRole(seer, target)) return true;
        else if (Virus.IsEnable && Virus.KnowRole(seer, target)) return true;
        else if (Jackal.IsEnable)
        {
            if (seer.Is(CustomRoles.Jackal) || seer.Is(CustomRoles.Recruit))
                return target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit);

            else if (seer.Is(CustomRoles.Sidekick))
                return target.Is(CustomRoles.Recruit) || target.Is(CustomRoles.Sidekick);
        }

        return false;
    }

    public static bool CanBeTeleported(this PlayerControl player)
    {
        if (player.Data == null // Check if PlayerData is not null
            || Main.MeetingIsStarted
            // Check target status
            || !player.IsAlive()
            || player.inVent
            || player.inMovingPlat // Moving Platform on Airhip and Zipline on Fungle
            || player.MyPhysics.Animations.IsPlayingEnterVentAnimation()
            || player.onLadder || player.MyPhysics.Animations.IsPlayingAnyLadderAnimation()
            || Pelican.IsEaten(player.PlayerId))
        {
            return false;
        }
        return true;
    }
    public static Vector2 GetBlackRoomPosition()
    {
        return Utils.GetActiveMapId() switch
        {
            0 => new Vector2(-27f, 3.3f), // The Skeld
            1 => new Vector2(-11.4f, 8.2f), // MIRA HQ
            2 => new Vector2(42.6f, -19.9f), // Polus
            3 => new Vector2(27f, 3.3f), // dlekS ehT
            4 => new Vector2(-16.8f, -6.2f), // Airship
            5 => new Vector2(10.2f, 18.1f), // The Fungle
            _ => throw new NotImplementedException(),
        };
    }
    public static Vector2 GetCustomPosition(this PlayerControl player) => new (player.transform.position.x, player.transform.position.y);
    public static string GetRoleInfo(this PlayerControl player, bool InfoLong = false)
    {
        var role = player.GetCustomRole();
        if (role is CustomRoles.Crewmate or CustomRoles.Impostor)
            InfoLong = false;

        var text = role.ToString();

        var Prefix = "";
        if (!InfoLong)
            switch (role)
            {
                case CustomRoles.Mafia:
                    Prefix = Utils.CanMafiaKill() ? "After" : "Before";
                    break;
            };
        var Info = (role.IsVanilla() ? "Blurb" : "Info") + (InfoLong ? "Long" : "");
        return GetString($"{Prefix}{text}{Info}");
    }
    public static void SetRealKiller(this PlayerControl target, PlayerControl killer, bool NotOverRide = false)
    {
        if (target == null)
        {
            Logger.Info("target=null", "SetRealKiller");
            return;
        }
        var State = Main.PlayerStates[target.PlayerId];
        if (State.RealKiller.Item1 != DateTime.MinValue && NotOverRide) return; //既に値がある場合上書きしない
        byte killerId = killer == null ? byte.MaxValue : killer.PlayerId;
        RPC.SetRealKiller(target.PlayerId, killerId);
    }
    public static PlayerControl GetRealKiller(this PlayerControl target)
    {
        var killerId = Main.PlayerStates[target.PlayerId].GetRealKiller();
        return killerId == byte.MaxValue ? null : Utils.GetPlayerById(killerId);
    }
    public static PlainShipRoom GetPlainShipRoom(this PlayerControl pc)
    {
        if (!pc.IsAlive() || Pelican.IsEaten(pc.PlayerId)) return null;
        var Rooms = ShipStatus.Instance.AllRooms.ToArray();
        if (Rooms == null) return null;
        foreach (var room in Rooms)
        {
            if (!room.roomArea) continue;
            if (pc.Collider.IsTouching(room.roomArea))
                return room;
        }
        return null;
    }

    //汎用
    public static bool Is(this PlayerControl target, CustomRoles role) =>
        role > CustomRoles.NotAssigned ? target.GetCustomSubRoles().Contains(role) : target.GetCustomRole() == role;
    public static bool Is(this PlayerControl target, CustomRoleTypes type) { return target.GetCustomRole().GetCustomRoleTypes() == type; }
    public static bool Is(this PlayerControl target, RoleTypes type) { return target.GetCustomRole().GetRoleTypes() == type; }
    public static bool Is(this PlayerControl target, CountTypes type) { return target.GetCountTypes() == type; }
    public static bool Is(this CustomRoles trueRole, CustomRoles checkRole) { return trueRole == checkRole; }
    public static bool IsAlive(this PlayerControl target)
    {
        //In lobby all is alive
        if (GameStates.IsLobby)
        {
            return true;
        }
        //if target is null, it is not alive
        if (target == null)
        {
            return false;
        }
        //if the target status is alive
        if (Main.PlayerStates.TryGetValue(target.PlayerId, out var playerState))
        {
            return !playerState.IsDead;
        }

        // else player is dead
        return false;
    }
    public static bool IsExiled(this PlayerControl target)
    {
        return GameStates.InGame || (target != null && (Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.Vote));
    }
    ///<summary>Is the player currently protected</summary>
    public static bool IsProtected(this PlayerControl self) => self.protectedByGuardianId > -1;

    public const MurderResultFlags ResultFlags = MurderResultFlags.Succeeded; //No need for DecisonByHost
}
