using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System;
using System.Text;
using TOHE.Modules;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

static class ExtendedPlayerControl
{
    public static void SetRole(this PlayerControl player, RoleTypes role, bool canOverride = false)
    {
        player.StartCoroutine(player.CoSetRole(role, canOverride));
    }

    public static void RpcSetCustomRole(this PlayerControl player, CustomRoles role)
    {
        if (role < CustomRoles.NotAssigned)
        {
            Main.PlayerStates[player.PlayerId].SetMainRole(role);
        }
        else if (role >= CustomRoles.NotAssigned)   //500:NoSubRole 501~:SubRole 
        {
            if (Cleanser.CantGetAddon() && player.Is(CustomRoles.Cleansed)) return;
            if (role == CustomRoles.Cleansed) Main.PlayerStates[player.PlayerId].SetSubRole(role, pc: player);
            else Main.PlayerStates[player.PlayerId].SetSubRole(role);            
        }
        if (AmongUsClient.Instance.AmHost)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.WritePacked((int)role);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    public static void RpcSetCustomRole(byte PlayerId, CustomRoles role)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, SendOption.Reliable, -1);
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
    public static CustomRoles GetCustomRole(this NetworkedPlayerInfo player)
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
            Logger.Warn($"{callerClassName}.{callerMethodName} tried to retrieve CustomRole, but the target was null", "GetCustomRole");
            return CustomRoles.Crewmate;
        }
        return Main.PlayerStates.TryGetValue(player.PlayerId, out var State) ? State.MainRole : CustomRoles.Crewmate;
    }

    public static List<CustomRoles> GetCustomSubRoles(this PlayerControl player)
    {
        if (player == null)
        {
            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            Logger.Warn($"{callerClassName}.{callerMethodName} tried to get CustomSubRole, but the target was null", "GetCustomRole");
            return [CustomRoles.NotAssigned];
        }
        return Main.PlayerStates.TryGetValue(player.PlayerId, out var State) ? State.SubRoles : [CustomRoles.NotAssigned];
    }
    public static CountTypes GetCountTypes(this PlayerControl player)
    {
        if (player == null)
        {
            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            Logger.Warn($"{callerClassName}.{callerMethodName} tried to get CountTypes, but the target was null", "GetCountTypes");
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

    public static void RpcSetNamePrivate(this PlayerControl player, string name, PlayerControl seer = null, bool force = false)
    {
        //player: player whose name needs to be changed
        //seer: player who can see name changes
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

        if (seer == null || player == null) return;

        var clientId = seer.GetClientId();

        var sender = CustomRpcSender.Create(name: $"SetNamePrivate");
        sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetName, clientId)
            .Write(seer.Data.NetId)
            .Write(name)
        .EndRpc();
        sender.SendMessage();
    }
    public static void RpcEnterVentDesync(this PlayerPhysics physics, int ventId, PlayerControl seer)
    {
        if (physics == null) return;

        var clientId = seer.GetClientId();
        if (AmongUsClient.Instance.ClientId == clientId)
        {
            physics.StopAllCoroutines();
            physics.StartCoroutine(physics.CoEnterVent(ventId));
            return;
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(physics.NetId, (byte)RpcCalls.EnterVent, SendOption.Reliable, seer.GetClientId());
        writer.WritePacked(ventId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcExitVentDesync(this PlayerPhysics physics, int ventId, PlayerControl seer)
    {
        if (physics == null) return;

        var clientId = seer.GetClientId();
        if (AmongUsClient.Instance.ClientId == clientId)
        {
            physics.StopAllCoroutines();
            physics.StartCoroutine(physics.CoExitVent(ventId));
            return;
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(physics.NetId, (byte)RpcCalls.ExitVent, SendOption.Reliable, seer.GetClientId());
        writer.WritePacked(ventId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcBootFromVentDesync(this PlayerPhysics physics, int ventId, PlayerControl seer)
    {
        if (physics == null) return;

        var clientId = seer.GetClientId();
        if (AmongUsClient.Instance.ClientId == clientId)
        {
            physics.BootFromVent(ventId);
            return;
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(physics.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, seer.GetClientId());
        writer.WritePacked(ventId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcSetRoleDesync(this PlayerControl player, RoleTypes role, bool canOverride, int clientId)
    {
        if (player == null) return;
        if (AmongUsClient.Instance.ClientId == clientId)
        {
            player.SetRole(role, canOverride);
            return;
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetRole, SendOption.Reliable, clientId);
        writer.Write((ushort)role);
        writer.Write(canOverride);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void RpcGuardAndKill(this PlayerControl killer, PlayerControl target = null, bool forObserver = false, bool fromSetKCD = false)
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
        if (Observer.HasEnabled && !forObserver && !MeetingStates.FirstMeeting)
        {
            Observer.ActivateGuardAnimation(killer.PlayerId, target);
        }

        // Host
        if (killer.AmOwner)
        {
            killer.MurderPlayer(target, MurderResultFlags.FailedProtected);
        }
        // Other Clients
        if (!killer.OwnedByHost())
        {
            var writer = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable, killer.GetClientId());
            writer.WriteNetObject(target);
            writer.Write((int)MurderResultFlags.FailedProtected);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        if (!fromSetKCD) killer.SetKillTimer(half: true);
    }
    public static void SetKillCooldownV2(this PlayerControl player, float time = -1f)
    {
        if (player == null) return;
        if (!player.CanUseKillButton()) return;
        player.SetKillTimer(CD: time);
        if (time >= 0f) Main.AllPlayerKillCooldown[player.PlayerId] = time * 2;
        else Main.AllPlayerKillCooldown[player.PlayerId] *= 2;
        player.SyncSettings();
        player.RpcGuardAndKill(fromSetKCD: true);
        player.ResetKillCooldown();
    }
    public static void SetKillCooldown(this PlayerControl player, float time = -1f, PlayerControl target = null, bool forceAnime = false)
    {
        if (player == null) return;

        if (!player.HasImpKillButton(considerVanillaShift: true)) return;
        if (player.HasImpKillButton(false) && !player.CanUseKillButton()) return;
        
        player.SetKillTimer(CD: time);
        if (target == null) target = player;
        if (time >= 0f) Main.AllPlayerKillCooldown[player.PlayerId] = time * 2;
        else Main.AllPlayerKillCooldown[player.PlayerId] *= 2;
        if (player.GetRoleClass() is Glitch gc)
        {
            gc.LastKill = Utils.GetTimeStamp() + ((int)(time / 2) - Glitch.KillCooldown.GetInt());
            gc.KCDTimer = (int)(time / 2);
        }
        else if (forceAnime || !player.IsModClient() || !Options.DisableShieldAnimations.GetBool())
        {
            player.SyncSettings();
            player.RpcGuardAndKill(target, fromSetKCD: true);
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
            if (Observer.HasEnabled)
            {
                Observer.ActivateGuardAnimation(target.PlayerId, target);
            }
        }
        player.ResetKillCooldown();
    }
    public static void ResetPlayerOutfit(this PlayerControl player, NetworkedPlayerInfo.PlayerOutfit Outfit = null, bool force = false)
    {
        Outfit ??= Main.PlayerStates[player.PlayerId].NormalOutfit;

        void Setoutfit() 
        {
            var sender = CustomRpcSender.Create(name: $"Reset PlayerOufit for 『{player.Data.PlayerName}』");

            player.SetName(Outfit.PlayerName);
            sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetName)
                .Write(player.Data.NetId)
                .Write(Outfit.PlayerName)
            .EndRpc();

            Main.AllPlayerNames[player.PlayerId] = Outfit.PlayerName;

            player.SetColor(Outfit.ColorId);
            sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetColor)
                .Write(player.Data.NetId)
                .Write((byte)Outfit.ColorId)
            .EndRpc();

            player.SetHat(Outfit.HatId, Outfit.ColorId);
            sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetHatStr)
                .Write(Outfit.HatId)
                .Write(player.GetNextRpcSequenceId(RpcCalls.SetHatStr))
            .EndRpc();

            player.SetSkin(Outfit.SkinId, Outfit.ColorId);
            sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetSkinStr)
                .Write(Outfit.SkinId)
                .Write(player.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
            .EndRpc();

            player.SetVisor(Outfit.VisorId, Outfit.ColorId);
            sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetVisorStr)
                .Write(Outfit.VisorId)
                .Write(player.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
            .EndRpc();

            player.SetPet(Outfit.PetId);
            sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetPetStr)
                .Write(Outfit.PetId)
                .Write(player.GetNextRpcSequenceId(RpcCalls.SetPetStr))
                .EndRpc();

            player.SetNamePlate(Outfit.NamePlateId);
            sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetNamePlateStr)
                .Write(Outfit.NamePlateId)
                .Write(player.GetNextRpcSequenceId(RpcCalls.SetNamePlateStr))
                .EndRpc();

            sender.SendMessage();

            //cannot use currentoutfit type because of mushroom mixup . .
            var OutfitTypeSet = player.CurrentOutfitType != PlayerOutfitType.Shapeshifted ? PlayerOutfitType.Default : PlayerOutfitType.Shapeshifted;

            player.Data.SetOutfit(OutfitTypeSet, Outfit);

            //Used instead of GameData.Instance.DirtyAllData();
            foreach (var innerNetObject in GameData.Instance.AllPlayers)
            {
                innerNetObject.SetDirtyBit(uint.MaxValue);
            }
        }
        if (player.CheckCamoflague() && !force)
        {
            Main.LateOutfits[player.PlayerId] = Setoutfit;
        }
        else
        {
            Main.LateOutfits.Remove(player.PlayerId);
            Setoutfit();
        }
    }
    public static void SetKillCooldownV3(this PlayerControl player, float time = -1f, PlayerControl target = null, bool forceAnime = false)
    {
        if (player == null) return;
        if (!player.CanUseKillButton()) return;
        player.SetKillTimer(CD: time);
        if (target == null) target = player;
        if (time >= 0f) Main.AllPlayerKillCooldown[player.PlayerId] = time * 2;
        else Main.AllPlayerKillCooldown[player.PlayerId] *= 2;
        if (forceAnime || !player.IsModClient() || !Options.DisableShieldAnimations.GetBool())
        {
            player.SyncSettings();
            player.RpcGuardAndKill(target, fromSetKCD: true);
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
            if (Observer.HasEnabled)
            {
                Observer.ActivateGuardAnimation(target.PlayerId, target);
            }
        }
        player.ResetKillCooldown();
    }
    public static void RpcSpecificShapeshift(this PlayerControl player, PlayerControl target, bool shouldAnimate)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (player.OwnedByHost())
        {
            player.Shapeshift(target, shouldAnimate);
            return;
        }
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Shapeshift, SendOption.Reliable, player.GetClientId());
        messageWriter.WriteNetObject(target);
        messageWriter.Write(shouldAnimate);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }
    public static void RpcSpecificRejectShapeshift(this PlayerControl player, PlayerControl target, bool shouldAnimate)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var seer in Main.AllPlayerControls)
        {
            if (seer != player)
            {
                MessageWriter msg = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.RejectShapeshift, SendOption.Reliable, seer.GetClientId());
                AmongUsClient.Instance.FinishRpcImmediately(msg);
            }
            else
            {
                player.RpcSpecificShapeshift(target, shouldAnimate);
            }
        }
    }
    public static void RpcSetSpecificScanner(this PlayerControl target, PlayerControl seer, bool IsActive)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        byte cnt = ++PlayerControl.LocalPlayer.scannerCount;

        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(target.NetId, (byte)RpcCalls.SetScanner, SendOption.Reliable, seer.GetClientId());
        messageWriter.Write(IsActive);
        messageWriter.Write(cnt);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }

    public static void RpcSpecificVanish(this PlayerControl player, PlayerControl seer)
    {
        /*
         *  Unluckily the vanish animation cannot be disabled
         *  For vanila client seer side, the player must be with Phantom Role behavior, or the rpc will do nothing
         */
        if (!AmongUsClient.Instance.AmHost) return;

        MessageWriter msg = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.StartVanish, SendOption.None, seer.GetClientId());
        AmongUsClient.Instance.FinishRpcImmediately(msg);
    }

    public static void RpcSpecificAppear(this PlayerControl player, PlayerControl seer, bool shouldAnimate)
    {
        /*
         *  For vanila client seer side, the player must be with Phantom Role behavior, or the rpc will do nothing
         */
        if (!AmongUsClient.Instance.AmHost) return;

        MessageWriter msg = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.StartAppear, SendOption.None, seer.GetClientId());
        msg.Write(shouldAnimate);
        AmongUsClient.Instance.FinishRpcImmediately(msg);
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
        if (target.GetRoleClass() is Glitch gc)
        {
            gc.LastHack = Utils.GetTimeStamp();
            gc.LastMimic = Utils.GetTimeStamp();
            gc.MimicCDTimer = 10;
            gc.HackCDTimer = 10;
        }
        else if (PlayerControl.LocalPlayer == target && !target.GetCustomRole().IsGhostRole() && !target.IsAnySubRole(x => x.IsGhostRole()))
        {
            //if target is the host, except for guardian angel, that breaks it.
            PlayerControl.LocalPlayer.Data.Role.SetCooldown();
        }
        else
        {
            // target is other than the host (not ghosts)
            try { if (PlayerControl.LocalPlayer == target) target.MarkDirtySettings(); }
            catch (Exception e) { Logger.Warn($"{e}", "RpcResetAbilityCooldown.HostAbility"); } 
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

    public static bool OwnedByHost(this InnerNetObject innerObject)
        => innerObject.OwnerId == AmongUsClient.Instance.HostId;

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
        if (clientData)
        {
            var client = player.GetClient();

            if (client != null)
            {
                if (Main.AllClientRealNames.TryGetValue(client.Id, out var realname))
                {
                    return realname;
                }
                return player.GetClient().PlayerName;
            }
        }
        return isMeeting || player == null ? player?.Data?.PlayerName : player?.name;
    }
    public static bool CanUseKillButton(this PlayerControl pc)
    {
        if (GameStates.IsLobby) return false;
        if (!pc.IsAlive() || Pelican.IsEaten(pc.PlayerId)) return false;
        if (DollMaster.IsDoll(pc.PlayerId)) return false;
        if (pc.Is(CustomRoles.Killer) || Mastermind.PlayerIsManipulated(pc)) return true;

        var playerRoleClass = pc.GetRoleClass();
        if (playerRoleClass != null && playerRoleClass.CanUseKillButton(pc)) return true;

        return false;
    }
    public static bool HasKillButton(PlayerControl pc = null)
    {
        if (pc == null) return false;
        if (!pc.IsAlive() || pc.Data.Role.Role == RoleTypes.GuardianAngel || Pelican.IsEaten(pc.PlayerId)) return false;
        
        var role = pc.GetCustomRole();
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
        if (!pc.IsAlive()) return false;
        if (GameStates.IsHideNSeek) return true;
        if (DollMaster.IsDoll(pc.PlayerId) || Circumvent.CantUseVent(pc)) return false;
        if (Necromancer.Killer && !pc.Is(CustomRoles.Necromancer)) return false;
        if (pc.Is(CustomRoles.Killer) || pc.Is(CustomRoles.Nimble)) return true;
        if (Amnesiac.PreviousAmnesiacCanVent(pc)) return true; //this is done because amnesiac has imp basis and if amnesiac remembers a role with different basis then player will not vent as `CanUseImpostorVentButton` is false

        var playerRoleClass = pc.GetRoleClass();
        if (playerRoleClass != null && playerRoleClass.CanUseImpostorVentButton(pc)) return true;

        return false;
    }
    public static bool CanUseSabotage(this PlayerControl pc)
    {
        if (pc.Is(Custom_Team.Impostor) && !pc.IsAlive() && Options.DeadImpCantSabotage.GetBool()) return false;

        var playerRoleClass = pc.GetRoleClass();
        if (playerRoleClass != null && playerRoleClass.CanUseSabotage(pc)) return true;

        return false;
    }
    public static void ResetKillCooldown(this PlayerControl player)
    {
        Main.AllPlayerKillCooldown[player.PlayerId] = Options.DefaultKillCooldown;

        // FFA
        if (player.Is(CustomRoles.Killer))
        {
            Main.AllPlayerKillCooldown[player.PlayerId] = FFAManager.FFA_KCD.GetFloat();
        }
        else
        {
            player.GetRoleClass()?.SetKillCooldown(player.PlayerId);
        }

        var playerSubRoles = player.GetCustomSubRoles();

        if (playerSubRoles.Any())
            foreach (var subRole in playerSubRoles)
            {
                switch (subRole)
                {
                    case CustomRoles.LastImpostor when player.PlayerId == LastImpostor.currentId:
                        LastImpostor.SetKillCooldown();
                        break;

                    case CustomRoles.Mare:
                        Main.AllPlayerKillCooldown[player.PlayerId] = Mare.KillCooldownInLightsOut.GetFloat();
                        break;

                    case CustomRoles.Overclocked:
                        Main.AllPlayerKillCooldown[player.PlayerId] -= Main.AllPlayerKillCooldown[player.PlayerId] * (Overclocked.OverclockedReduction.GetFloat() / 100);
                        break;

                    case CustomRoles.Diseased:
                        Diseased.IncreaseKCD(player);
                        break;

                    case CustomRoles.Antidote:
                        Antidote.ReduceKCD(player);
                        break;
                }
            }

        if (!player.HasImpKillButton(considerVanillaShift: false))
            Main.AllPlayerKillCooldown[player.PlayerId] = 300f;

        if (player.GetRoleClass() is Chronomancer ch)
        {
            ch.realcooldown = Main.AllPlayerKillCooldown[player.PlayerId];
            ch.SetCooldown();
        }


        if (Main.AllPlayerKillCooldown[player.PlayerId] == 0)
        {
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
    /// <summary>
    /// ONLY to be used when killer surely may kill the target, please check with killer.RpcCheckAndMurder(target, check: true) for indirect kill.
    /// </summary>
    public static void RpcMurderPlayer(this PlayerControl killer, PlayerControl target)
    {
        // If Target is Dollmaster or Possessed Player run Dollmasters kill check instead.
        if (DollMaster.SwapPlayerInfo(target) != target)
        {
            DollMaster.CheckMurderAsPossessed(killer, target);
            return;
        }
        
        killer.RpcMurderPlayer(target, true);
    }

    public static void AddInSwitchAddons(PlayerControl Killed, PlayerControl target, CustomRoles Addon = CustomRoles.NotAssigned, CustomRoles? IsAddon = CustomRoles.NotAssigned)
    {
        if (Addon == CustomRoles.NotAssigned)
        {
            Addon = IsAddon ?? CustomRoles.NotAssigned;
        }
        switch (Addon)
        {
            case CustomRoles.Tired:
                Tired.Remove(Killed.PlayerId);
                Tired.Add(target.PlayerId);
                break;
            case CustomRoles.Bewilder:
                Bewilder.Add();
                break;
            case CustomRoles.Lucky:
                Lucky.Remove(Killed.PlayerId);
                Lucky.Add(target.PlayerId);
                break;
            case CustomRoles.Clumsy:
                Clumsy.Remove(Killed.PlayerId);
                Clumsy.Add(target.PlayerId);
                break;
            case CustomRoles.Statue:
                Statue.Remove(Killed.PlayerId);
                Statue.Add(target.PlayerId);
                break;
            case CustomRoles.Glow:
                Glow.Remove(Killed.PlayerId);
                Glow.Add(target.PlayerId);
                break;
            case CustomRoles.Radar:
                Radar.Remove(Killed.PlayerId);
                Radar.Add(target.PlayerId);
                break;
        }
    }
    public static bool RpcCheckAndMurder(this PlayerControl killer, PlayerControl target, bool check = false)
    {
        var caller = new System.Diagnostics.StackFrame(1, false);
        var callerMethod = caller.GetMethod();
        string callerMethodName = callerMethod.Name;
        string callerClassName = callerMethod.DeclaringType.FullName;
        Logger.Msg($"RpcCheckAndMurder activated from: {callerClassName}.{callerMethodName}", "RpcCheckAndMurder");

        return CheckMurderPatch.RpcCheckAndMurder(killer, target, check);
    }
    public static bool CheckForInvalidMurdering(this PlayerControl killer, PlayerControl target, bool checkCanUseKillButton = false) => CheckMurderPatch.CheckForInvalidMurdering(killer, target, checkCanUseKillButton);
    public static void NoCheckStartMeeting(this PlayerControl reporter, NetworkedPlayerInfo target, bool force = false)
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
    
    public static bool KnowDeathReason(this PlayerControl seer, PlayerControl target)
        => (Options.EveryoneCanSeeDeathReason.GetBool()
        || seer.Is(CustomRoles.Doctor) || seer.Is(CustomRoles.Autopsy)
        || (seer.Data.IsDead && Options.GhostCanSeeDeathReason.GetBool()))
        && target.Data.IsDead || target.Is(CustomRoles.Gravestone) && target.Data.IsDead;

    public static bool KnowDeadTeam(this PlayerControl seer, PlayerControl target)
        => (seer.Is(CustomRoles.Necroview))
        && target.Data.IsDead;

    public static bool KnowLivingTeam(this PlayerControl seer, PlayerControl target)
        => (seer.Is(CustomRoles.Visionary))
        && !target.Data.IsDead;

    private readonly static LogHandler logger = Logger.Handler("KnowRoleTarget");
    public static bool KnowRoleTarget(PlayerControl seer, PlayerControl target, bool isVanilla = false)
    {
        if (Options.CurrentGameMode == CustomGameMode.FFA || GameEndCheckerForNormal.ShowAllRolesWhenGameEnd)
        {
            if (isVanilla)
                logger.Info($"IsFFA {Options.CurrentGameMode == CustomGameMode.FFA} or Game End {GameEndCheckerForNormal.ShowAllRolesWhenGameEnd}");
            return true;
        }
        else if (seer.Is(CustomRoles.GM) || target.Is(CustomRoles.GM) || (PlayerControl.LocalPlayer.PlayerId == seer.PlayerId && Main.GodMode.Value))
        {
            if (isVanilla)
                logger.Info($"Is GM {seer.Is(CustomRoles.GM)} or {target.Is(CustomRoles.GM)} or GodMode {(PlayerControl.LocalPlayer.PlayerId == seer.PlayerId && Main.GodMode.Value)}");
            return true;
        }
        else if (Main.VisibleTasksCount && !seer.IsAlive() && Options.GhostCanSeeOtherRoles.GetBool())
        {
            if (isVanilla)
                logger.Info($"Is dead and can see other roles");
            return true;
        }
        else if (Options.SeeEjectedRolesInMeeting.GetBool() && Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.Vote)
        {
            if (isVanilla)
                logger.Info($"See Ejected Roles In Meeting");
            return true;
        }
        else if (seer.GetCustomRole() == target.GetCustomRole() && seer.GetCustomRole().IsNK())
        {
            if (isVanilla)
                logger.Info("Roles == and IsNK");
            return true;
        }
        else if (Options.LoverKnowRoles.GetBool() && seer.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers))
        {
            if (isVanilla)
                logger.Info($"Lover Know Roles");
            return true;
        }
        else if (Options.ImpsCanSeeEachOthersRoles.GetBool() && seer.Is(Custom_Team.Impostor) && target.Is(Custom_Team.Impostor))
        {
            if (isVanilla)
                logger.Info($"Imps Can See Each Others Roles");
            return true;
        }
        else if (Madmate.MadmateKnowWhosImp.GetBool() && seer.Is(CustomRoles.Madmate) && target.Is(Custom_Team.Impostor))
        {
            if (isVanilla)
                logger.Info($"Madmate Know Whos Imp");
            return true;
        }
        else if (Madmate.ImpKnowWhosMadmate.GetBool() && target.Is(CustomRoles.Madmate) && seer.Is(Custom_Team.Impostor))
        {
            if (isVanilla)
                logger.Info($"Imp Know Whos Madmate");
            return true;
        }
        else if (seer.Is(Custom_Team.Impostor) && target.GetCustomRole().IsGhostRole() && target.GetCustomRole().IsImpostor())
        {
            if (isVanilla)
                logger.Info("Impostor see Imp Ghost Role");
            return true;
        }
        else if (target.GetRoleClass().KnowRoleTarget(seer, target))
        {
            if (isVanilla)
                logger.Info($"target {target.GetCustomRole()} GetRoleClass().KnowRoleTarget");
            return true;
        }
        else if (seer.GetRoleClass().KnowRoleTarget(seer, target))
        {
            if (isVanilla)
                logger.Info($"seer {seer.GetCustomRole()} GetRoleClass().KnowRoleTarget");
            return true;
        }
        else if (Solsticer.OtherKnowSolsticer(target))
        {
            if (isVanilla)
                logger.Info("Solsticer Other Know Solsticer");
            return true;
        }
        else if (Overseer.IsRevealedPlayer(seer, target) && !target.Is(CustomRoles.Trickster))
        {
            if (isVanilla)
                logger.Info($"Overseer.IsRevealedPlayer");
            return true;
        }
        else if (Gravestone.EveryoneKnowRole(target))
        {
            if (isVanilla)
                logger.Info("Gravestone.EveryoneKnowRole");
            return true;
        }
        else if (Mimic.CanSeeDeadRoles(seer, target))
        {
            if (isVanilla)
                logger.Info("Mimic.CanSeeDeadRoles");
            return true;
        }
        else if (Workaholic.OthersKnowWorka(target))
        {
            if (isVanilla)
                logger.Info("Workaholic Others Know Worka");
            return true;
        }
        else if (Jackal.JackalKnowRole(seer, target))
        {
            if (isVanilla)
                logger.Info("Jackal Know Role");
            return true;
        }
        else if (Cultist.KnowRole(seer, target))
        {
            if (isVanilla)
                logger.Info("Cultist Know Role");
            return true;
        }
        else if (Infectious.KnowRole(seer, target))
        {
            if (isVanilla)
                logger.Info("Infectious Know Role");
            return true;
        }
        else if (Virus.KnowRole(seer, target))
        {
            if (isVanilla)
                logger.Info($"Virus Know Role");
            return true;
        }


        else return false;
    }
    public static bool ShowSubRoleTarget(this PlayerControl seer, PlayerControl target, CustomRoles subRole = CustomRoles.NotAssigned)
    {
        if (seer == null) return false;
        if (target == null) target = seer;

        if (seer.PlayerId == target.PlayerId) return true;
        else if (seer.Is(CustomRoles.GM) || target.Is(CustomRoles.GM) || seer.Is(CustomRoles.God) || (seer.AmOwner && Main.GodMode.Value)) return true;
        else if (Main.VisibleTasksCount && !seer.IsAlive() && Options.GhostCanSeeOtherRoles.GetBool()) return true;
        else if (Options.ImpsCanSeeEachOthersAddOns.GetBool() && seer.Is(Custom_Team.Impostor) && target.Is(Custom_Team.Impostor) && !subRole.IsBetrayalAddon()) return true;

        else if ((subRole is CustomRoles.Madmate
                or CustomRoles.Sidekick
                or CustomRoles.Recruit
                or CustomRoles.Admired
                or CustomRoles.Charmed
                or CustomRoles.Infected
                or CustomRoles.Contagious
                or CustomRoles.Egoist) 
            && KnowSubRoleTarget(seer, target))
            return true;

        
        return false;
    }
    public static bool KnowSubRoleTarget(PlayerControl seer, PlayerControl target)
    {
        //if (seer.GetRoleClass().KnowRoleTarget(seer, target)) return true;
        
        if (seer.Is(Custom_Team.Impostor))
        {
            // Imp know Madmate
            if (target.Is(CustomRoles.Madmate) && Madmate.ImpKnowWhosMadmate.GetBool())
                return true;

            // Ego-Imp know other Ego-Imp
            else if (seer.Is(CustomRoles.Egoist) && target.Is(CustomRoles.Egoist) && Egoist.ImpEgoistVisibalToAllies.GetBool())
                return true;
        }
        else if (Admirer.HasEnabled && Admirer.CheckKnowRoleTarget(seer, target)) return true;
        else if (Cultist.HasEnabled && Cultist.KnowRole(seer, target)) return true;
        else if (Infectious.HasEnabled && Infectious.KnowRole(seer, target)) return true;
        else if (Virus.HasEnabled && Virus.KnowRole(seer, target)) return true;
        else if (Jackal.HasEnabled)
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

    public static Vector2 GetCustomPosition(this PlayerControl player) => new(player.transform.position.x, player.transform.position.y);

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
    
    public static void RpcTeleportAllPlayers(Vector2 location)
    {
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            pc.RpcTeleport(location);
        }
    }
    public static void RpcDesyncTeleport(this PlayerControl player, Vector2 position, PlayerControl seer)
    {
        if (player == null) return;
        var netTransform = player.NetTransform;
        var clientId = seer.GetClientId();
        ushort addSid = GameStates.IsLocalGame ? (ushort)4 : (ushort)40;
        if (AmongUsClient.Instance.ClientId == clientId)
        {
            netTransform.SnapTo(position, (ushort)(netTransform.lastSequenceId + addSid));
            return;
        }
        ushort newSid = (ushort)(netTransform.lastSequenceId + addSid);
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(netTransform.NetId, (byte)RpcCalls.SnapTo, SendOption.Reliable, clientId);
        NetHelpers.WriteVector2(position, writer);
        writer.Write(newSid);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcTeleport(this PlayerControl player, Vector2 position, bool isRandomSpawn = false, bool sendInfoInLogs = true)
    {
        if (sendInfoInLogs)
        {
            Logger.Info($" {player.GetNameWithRole().RemoveHtmlTags()} => {position}", "RpcTeleport");
            Logger.Info($" Player Id: {player.PlayerId}", "RpcTeleport");
        }

        // Don't check player status during random spawn
        if (!isRandomSpawn)
        {
            var cancelTeleport = false;

            if (player.inVent || player.MyPhysics.Animations.IsPlayingEnterVentAnimation())
            {
                Logger.Info($"Target: ({player.GetNameWithRole().RemoveHtmlTags()}) in vent", "RpcTeleport");
                cancelTeleport = true;
            }
            else if (player.onLadder || player.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
            {
                Logger.Warn($"Teleporting canceled - Target: ({player.GetNameWithRole().RemoveHtmlTags()}) is in on Ladder", "RpcTeleport");
                cancelTeleport = true;
            }
            else if (player.inMovingPlat)
            {
                Logger.Warn($"Teleporting canceled - Target: ({player.GetNameWithRole().RemoveHtmlTags()}) use moving platform (Airship/Fungle)", "RpcTeleport");
                cancelTeleport = true;
            }

            if (cancelTeleport)
            {
                player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), GetString("ErrorTeleport")));
                return;
            }
        }

        var netTransform = player.NetTransform;

        if (AmongUsClient.Instance.AmClient)
        {
            // +328 because lastSequenceId has delay between the host and the vanilla client
            // And this cannot forced teleport the player
            netTransform.SnapTo(position, (ushort)(netTransform.lastSequenceId + 328));
        }

        ushort newSid = (ushort)(netTransform.lastSequenceId + 8);
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(netTransform.NetId, (byte)RpcCalls.SnapTo, SendOption.Reliable);
        NetHelpers.WriteVector2(position, messageWriter);
        messageWriter.Write(newSid);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }
    public static void RpcRandomVentTeleport(this PlayerControl player)
    {
        var vents = ShipStatus.Instance.AllVents;
        var vent = vents.RandomElement();

        Logger.Info($" {vent.transform.position}", "RpcVentTeleportPosition");
        player.RpcTeleport(new Vector2(vent.transform.position.x, vent.transform.position.y + 0.3636f));
    }
    public static int GetPlayerVentId(this PlayerControl player)
    {
        if (!(ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out var systemType) &&
              systemType.TryCast<VentilationSystem>() is VentilationSystem ventilationSystem))
            return 99;

        return ventilationSystem.PlayersInsideVents.TryGetValue(player.PlayerId, out var playerIdVentId) ? playerIdVentId : 99;
    }
    public static string GetRoleInfo(this PlayerControl player, bool InfoLong = false)
    {
        var role = player.GetCustomRole();
        if (role is CustomRoles.Crewmate or CustomRoles.Impostor)
            InfoLong = false;

        var text = role.ToString();

        var Prefix = "";
        if (!InfoLong && role == CustomRoles.Nemesis)
            Prefix = Nemesis.CheckCanUseKillButton() ? "After" : "Before";
            
        var Info = (role.IsVanilla() ? "Blurb" : "Info");
        return !InfoLong ? GetString($"{Prefix}{text}{Info}") : role.GetInfoLong();
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
        var killerId = Main.PlayerStates[target.Data.PlayerId].GetRealKiller();
        return killerId == byte.MaxValue ? null : Utils.GetPlayerById(killerId);
    }
    public static PlayerControl GetRealKillerById(this byte targetId)
    {
        var killerId = Main.PlayerStates[targetId].GetRealKiller();
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

    /// <summary>
    /// Make sure to call PlayerState.Deathreason and not Vanilla Deathreason
    /// </summary>
    public static void SetDeathReason(this PlayerControl target, PlayerState.DeathReason reason) => target.PlayerId.SetDeathReason(reason);
    public static void SetDeathReason(this byte targetId, PlayerState.DeathReason reason)
    {
        Main.PlayerStates[targetId].deathReason = reason;
    }

    public static bool Is(this PlayerControl target, CustomRoles role) =>
        role > CustomRoles.NotAssigned ? target.GetCustomSubRoles().Contains(role) : target.GetCustomRole() == role;
    public static bool Is(this PlayerControl target, Custom_Team type) { return target.GetCustomRole().GetCustomRoleTeam() == type; }
    public static bool Is(this PlayerControl target, RoleTypes type) { return target.GetCustomRole().GetRoleTypes() == type; }
    public static bool Is(this PlayerControl target, CountTypes type) { return target.GetCountTypes() == type; }
    public static bool IsAnySubRole(this PlayerControl target, Func<CustomRoles, bool> predicate) => target != null && target.GetCustomSubRoles().Any() && target.GetCustomSubRoles().Any(predicate);

    public static bool IsAlive(this PlayerControl target)
    {
        //In lobby all is alive
        if (GameStates.IsLobby && !GameStates.IsInGame)
        {
            return true;
        }
        //if target is null, it is not alive
        if (target == null)
        {
            return false;
        }

        //if the target status is alive
        return !Main.PlayerStates.TryGetValue(target.PlayerId, out var playerState) || !playerState.IsDead;
    }
    public static bool IsDisconnected(this PlayerControl target)
    {
        //In lobby all not disconnected
        if (GameStates.IsLobby && !GameStates.IsInGame)
        {
            return false;
        }
        //if target is null, is disconnected
        if (target == null)
        {
            return true;
        }

        //if the target status is disconnected
        return !Main.PlayerStates.TryGetValue(target.PlayerId, out var playerState) || playerState.Disconnected;
    }
    public static bool IsExiled(this PlayerControl target)
    {
        return GameStates.InGame || (target != null && (Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.Vote));
    }
    ///<summary>Is the player currently protected</summary>
    public static bool IsProtected(this PlayerControl self) => self.protectedByGuardianId > -1;

    public const MurderResultFlags ResultFlags = MurderResultFlags.Succeeded; //No need for DecisonByHost
}
