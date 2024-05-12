using Hazel;
using System;
using System.Runtime.CompilerServices;
using TOHE.Modules;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.RandomSpawn;

namespace TOHE;

public static class AntiBlackout
{
    public static SolutionAntiBlackScreen currentSolution;

    ///<summary>
    /// Check num alive Impostors & Crewmates & NeutralKillers
    ///</summary>
    public static bool BlackOutIsActive => currentSolution != SolutionAntiBlackScreen.AntiBlackout_DisableProtect && CheckBlackOut();

    ///<summary>
    /// Count alive players and check black out 
    ///</summary>
    public static bool CheckBlackOut()
    {
        HashSet<byte> Impostors = [];
        HashSet<byte> Crewmates = [];
        HashSet<byte> NeutralKillers = [];

        var lastExiled = ExileControllerWrapUpPatch.AntiBlackout_LastExiled;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            // if player is ejected, do not count him as alive
            if (lastExiled != null && pc.PlayerId == lastExiled.PlayerId) continue;

            // Impostors and Madmates
            if (pc.Is(CountTypes.Impostor))
                Impostors.Add(pc.PlayerId);
            
            // Crewmate
            else if (pc.Is(CountTypes.Crew) 
                || pc.Is(CountTypes.OutOfGame) 
                || pc.Is(CountTypes.None))
                Crewmates.Add(pc.PlayerId); // Crewmates

            // Other CountTypes counts as neutral killers
            else NeutralKillers.Add(pc.PlayerId);
        }

        var numAliveImpostors = Impostors.Count;
        var numAliveCrewmates = Crewmates.Count;
        var numAliveNeutralKillers = NeutralKillers.Count;

        Logger.Info($" {numAliveImpostors}", "AntiBlackout Num Alive Impostors");
        Logger.Info($" {numAliveCrewmates}", "AntiBlackout Num Alive Crewmates");
        Logger.Info($" {numAliveNeutralKillers}", "AntiBlackout Num Alive Neutral Killers");

        var BlackOutIsActive = false;

        // if all Crewmates is dead
        if (!BlackOutIsActive)
            BlackOutIsActive = numAliveCrewmates <= 0;

        // if all Impostors is dead and neutral killers > or = num alive crewmates
        if (!BlackOutIsActive)
            BlackOutIsActive = numAliveImpostors <= 0 && (numAliveNeutralKillers >= numAliveCrewmates);

        // if num alive Impostors > or = num alive Crewmates/Neutral killers
        if (!BlackOutIsActive)
            BlackOutIsActive = numAliveImpostors >= (numAliveNeutralKillers + numAliveCrewmates);

        Logger.Info($" {BlackOutIsActive}", "BlackOut Is Active");
        return BlackOutIsActive;
    }

    public static bool IsCached { get; private set; } = false;
    private static Dictionary<byte, (bool isDead, bool Disconnected)> isDeadCache = [];
    private readonly static LogHandler logger = Logger.Handler("AntiBlackout");

    public static void SetIsDead(bool doSend = true, [CallerMemberName] string callerMethodName = "")
    {
        logger.Info($"SetIsDead is called from {callerMethodName}");
        if (IsCached)
        {
            logger.Info("Please run RestoreIsDead before running SetIsDead again.");
            return;
        }
        isDeadCache.Clear();
        foreach (var info in GameData.Instance.AllPlayers)
        {
            if (info == null) continue;
            isDeadCache[info.PlayerId] = (info.IsDead, info.Disconnected);
            info.IsDead = false;
            info.Disconnected = false;
        }
        IsCached = true;
        if (doSend) SendGameData();
    }
    public static void RestoreIsDead(bool doSend = true, [CallerMemberName] string callerMethodName = "")
    {
        logger.Info($"RestoreIsDead is called from {callerMethodName}");
        foreach (var info in GameData.Instance.AllPlayers)
        {
            if (info == null) continue;
            if (isDeadCache.TryGetValue(info.PlayerId, out var val))
            {
                info.IsDead = val.isDead;
                info.Disconnected = val.Disconnected;
            }
        }
        isDeadCache.Clear();
        IsCached = false;
        if (doSend) SendGameData();
    }

    public static void SendGameData([CallerMemberName] string callerMethodName = "")
    {
        logger.Info($"SendGameData is called from {callerMethodName}");
        MessageWriter writer = MessageWriter.Get(SendOption.Reliable);
        // The writing {} is for readability.
        writer.StartMessage(5); //0x05 GameData
        {
            writer.Write(AmongUsClient.Instance.GameId);
            writer.StartMessage(1); //0x01 Data
            {
                writer.WritePacked(GameData.Instance.NetId);
                GameData.Instance.Serialize(writer, true);

            }
            writer.EndMessage();
        }
        writer.EndMessage();

        AmongUsClient.Instance.SendOrDisconnect(writer);
        writer.Recycle();
    }
    public static void OnDisconnect(GameData.PlayerInfo player)
    {
        // Execution conditions: Client is the host, IsDead is overridden, player is already disconnected
        if (!AmongUsClient.Instance.AmHost || !IsCached || !player.Disconnected) return;
        isDeadCache[player.PlayerId] = (true, true);
        player.IsDead = player.Disconnected = false;
        SendGameData();
    }

    ///<summary>
    ///Execute the code with IsDead temporarily set back to what it should be
    ///<param name="action">Execution details</param>
    ///</summary>
    public static void TempRestore(Action action)
    {
        logger.Info("==Temp Restore==");
        // Whether TempRestore was executed with IsDead overwritten
        bool before_IsCached = IsCached;
        try
        {
            if (before_IsCached) RestoreIsDead(doSend: false);
            action();
        }
        catch (Exception ex)
        {
            logger.Warn("An exception occurred within AntiBlackout.TempRestore");
            logger.Exception(ex);
        }
        finally
        {
            if (before_IsCached) SetIsDead(doSend: false);
            logger.Info("==/Temp Restore==");
        }
    }

    public static void FullResetCamForPlayer(PlayerControl player)
    {
        if (player == null || !AmongUsClient.Instance.AmHost || player.AmOwner || player.IsModClient()) return;

        var ghostPlayer = Main.AllPlayerControls.FirstOrDefault(pc => !pc.IsAlive() && pc.PlayerId != player.PlayerId);
        if (ghostPlayer == null) return;

        var playerPosition = player.GetCustomPosition();
        var systemtypes = Utils.GetCriticalSabotageSystemType();

        var sender = CustomRpcSender.Create("ResetPlayerCam");
        {
            // Start critical sabotage locally for the player (clean up black screen)
            sender.AutoStartRpc(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, targetClientId: player.GetClientId());
            {
                sender.Write((byte)systemtypes);
                sender.WriteNetObject(player);
                sender.Write((byte)128);
            }
            sender.EndRpc();
            // Teleport a ghost player locally for the player
            sender.AutoStartRpc(ghostPlayer.NetTransform.NetId, (byte)RpcCalls.SnapTo, targetClientId: player.GetClientId());
            {
                NetHelpers.WriteVector2(new Vector2(100f, 100f), sender.stream);
                sender.Write((ushort)(ghostPlayer.NetTransform.lastSequenceId + 8));
            }
            sender.EndRpc();
            // Player kill ghost player locally for the player
            sender.AutoStartRpc(player.NetId, (byte)RpcCalls.MurderPlayer, targetClientId: player.GetClientId());
            {
                sender.WriteNetObject(ghostPlayer);
                sender.Write((int)MurderResultFlags.Succeeded);
            }
            sender.EndRpc();
        }
        sender.SendMessage();

        // In case the dead body is in plain sight
        Main.UnreportableBodies.Add(ghostPlayer.PlayerId);

        // End critical sabotage locally for the player
        _ = new LateTask(() =>
        {
            if (player == null) return;

            player.RpcDesyncUpdateSystem(systemtypes, 16);

            if (GameStates.AirshipIsActive)
            {
                player.RpcDesyncUpdateSystem(systemtypes, 17);
            }
        }, 0.2f, "Fix Desync Reactor for reset cam", shoudLog: false);

        // Teleport player back
        _ = new LateTask(() =>
        {
            if (player == null) return;

            if (GameStates.AirshipIsActive)
            {
                new AirshipSpawnMap().FirstTeleport(player);
            }
            else
            {
                player.RpcTeleport(playerPosition);
            }

        }, 0.4f, "Teleport player back", shoudLog: false);
    }

    public static void AntiBlackRpcVotingComplete(this MeetingHud __instance, MeetingHud.VoterState[] states, GameData.PlayerInfo exiled, bool tie)
    {
        if (AmongUsClient.Instance.AmClient)
        {
            __instance.VotingComplete(states, exiled, tie);
        }

        var sender = CustomRpcSender.Create("AntiBlack RpcVotingComplete", SendOption.None);
        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.AmOwner) continue;
            if (pc.IsModClient()) //For mod client show real result
            {
                sender.AutoStartRpc(__instance.NetId, (byte)RpcCalls.VotingComplete, pc.GetClientId());
                {
                    sender.WritePacked(states.Length);
                    foreach (MeetingHud.VoterState voterState in states)
                    {
                        sender.WriteMessageType(voterState.VoterId);
                        sender.Write(voterState.VotedForId);
                        sender.WriteEndMessage();
                    }
                    sender.Write(exiled != null ? exiled.PlayerId : byte.MaxValue);
                    sender.Write(tie);
                    sender.EndRpc();
                }
            }
            else //For vanilla client show a tie
            {
                sender.AutoStartRpc(__instance.NetId, (byte)RpcCalls.VotingComplete, pc.GetClientId());
                {
                    sender.WritePacked(states.Length);
                    foreach (MeetingHud.VoterState voterState in states)
                    {
                        sender.WriteMessageType(voterState.VoterId);
                        sender.Write(voterState.VotedForId);
                        sender.WriteEndMessage();
                    }
                    sender.Write(byte.MaxValue);
                    sender.Write(true);
                    sender.EndRpc();
                }
            }
        }
        sender.SendMessage();
    }
    public static void AfterMeetingTasks()
    {
        var timeNotify = 0f;

        if (BlackOutIsActive)
        {
            if (currentSolution == SolutionAntiBlackScreen.AntiBlackout_SkipVoting && CheckForEndVotingPatch.TempExileMsg != null)
            {
                timeNotify = 4f;
                foreach (var pc in Main.AllPlayerControls.Where(p => p != null && !(p.AmOwner || p.IsModClient())).ToArray())
                {
                    pc.Notify(CheckForEndVotingPatch.TempExileMsg, time: timeNotify);
                }
            }
            else if (currentSolution == SolutionAntiBlackScreen.AntiBlackout_FullResetCamera)
            {
                timeNotify = 12f;
                foreach (var pc in Main.AllPlayerControls.Where(p => p != null && !(p.AmOwner || p.IsModClient())).ToArray())
                {
                    pc.Notify(Translator.GetString("AntiBlackout_ClickMapButtonToShowAllButtons"), time: timeNotify);
                }
            }
        }

        try
        {
            _ = new LateTask(() =>
            {
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    pc.GetRoleClass()?.NotifyAfterMeeting();
                }
            }, timeNotify + 0.2f, "Notify AfterMeetingTasks");
        }
        catch (Exception error)
        {
            Logger.Error($"{error}", "AntiBlackout.AfterMeetingTasks");
        }
    }
    public static void Reset()
    {
        logger.Info("==Reset==");
        isDeadCache ??= [];
        isDeadCache.Clear();
        IsCached = false;
        ShowExiledInfo = false;
        StoreExiledMessage = "";
        currentSolution = (SolutionAntiBlackScreen)Options.SolutionAntiBlackScreen.GetValue();
    }

    public static bool ShowExiledInfo = false;
    public static string StoreExiledMessage = "";
}

public enum SolutionAntiBlackScreen
{
    AntiBlackout_SkipVoting,
    AntiBlackout_FullResetCamera,
    AntiBlackout_DisableProtect
}