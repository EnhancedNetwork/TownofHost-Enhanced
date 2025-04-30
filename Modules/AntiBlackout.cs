using AmongUs.GameOptions;
using Hazel;
using System;
using System.Runtime.CompilerServices;
using TOHE.Modules;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Core;

namespace TOHE;

public static class AntiBlackout
{
    ///<summary>
    /// Check num alive Impostors & Crewmates & NeutralKillers & Coven
    ///</summary>
    public static bool BlackOutIsActive => false; /*!Options.DisableAntiBlackoutProtects.GetBool() && CheckBlackOut();*/

    //this is simply just called in less places, because antiblackout with role-basis changing is OP
    public static int ExilePlayerId = -1;
    public static bool SkipTasks = false;

    ///<summary>
    /// Count alive players and check black out 
    ///</summary>
    public static bool CheckBlackOut()
    {
        HashSet<byte> Impostors = [];
        HashSet<byte> Crewmates = [];
        HashSet<byte> NeutralKillers = [];
        HashSet<byte> Coven = [];

        var lastExiled = ExileControllerWrapUpPatch.AntiBlackout_LastExiled;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            // if player is ejected, do not count him as alive
            if (lastExiled != null && pc.PlayerId == lastExiled.PlayerId) continue;

            // Impostors
            if (pc.Is(Custom_Team.Impostor) && !Main.PlayerStates[pc.PlayerId].IsNecromancer)
                Impostors.Add(pc.PlayerId);

            // Only Neutral killers
            else if ((pc.IsNeutralKiller() || pc.IsNeutralApocalypse()) && !Main.PlayerStates[pc.PlayerId].IsNecromancer)
                NeutralKillers.Add(pc.PlayerId);

            //Coven
            if (pc.Is(Custom_Team.Coven) || Main.PlayerStates[pc.PlayerId].IsNecromancer)
                Coven.Add(pc.PlayerId);

            // Crewmate
            else Crewmates.Add(pc.PlayerId);
        }

        var numAliveImpostors = Impostors.Count;
        var numAliveCrewmates = Crewmates.Count;
        var numAliveNeutralKillers = NeutralKillers.Count;
        var numAliveCoven = Coven.Count;

        Logger.Info($" {numAliveImpostors}", "AntiBlackout Num Alive Impostors");
        Logger.Info($" {numAliveCrewmates}", "AntiBlackout Num Alive Crewmates");
        Logger.Info($" {numAliveNeutralKillers}", "AntiBlackout Num Alive Neutral Killers");
        Logger.Info($" {numAliveCoven}", "AntiBlackout Num Alive Coven");

        var BlackOutIsActive = false;

        // All real imposotrs is dead
        if (!BlackOutIsActive)
            BlackOutIsActive = numAliveImpostors <= 0;

        // Alive Impostors > or = others team count
        if (!BlackOutIsActive)
            BlackOutIsActive = (numAliveNeutralKillers + numAliveCrewmates + numAliveCoven) <= numAliveImpostors;

        // One Impostor and one Neutral Killer is alive, and living Crewmates very few
        if (!BlackOutIsActive)
            BlackOutIsActive = numAliveNeutralKillers == 1 && numAliveImpostors == 1 && numAliveCrewmates <= 2;

        // One Neutral Killer and one Coven is alive, and living Crewmates very few
        if (!BlackOutIsActive)
            BlackOutIsActive = numAliveNeutralKillers == 1 && numAliveCoven == 1 && numAliveCrewmates <= 2;

        // One Coven and one Impostor is alive, and living Crewmates very few
        if (!BlackOutIsActive)
            BlackOutIsActive = numAliveCoven == 1 && numAliveImpostors == 1 && numAliveCrewmates <= 2;

        Logger.Info($" {BlackOutIsActive}", "BlackOut Is Active");
        return BlackOutIsActive;
    }

    public static bool IsCached { get; private set; } = false;
    private static Dictionary<byte, (bool isDead, bool Disconnected)> isDeadCache = [];
    private readonly static LogHandler logger = Logger.Handler("AntiBlackout");

    public static void SetIsDead(bool doSend = true, [CallerMemberName] string callerMethodName = "")
    {
        SkipTasks = true;
        RevivePlayersAndSetDummyImp();
        logger.Info($"SetIsDead is called from {callerMethodName}");
        if (IsCached)
        {
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
    private static void RevivePlayersAndSetDummyImp()
    {
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return;

        PlayerControl dummyImp = PlayerControl.LocalPlayer;

        var sender = CustomRpcSender.Create("AntiBlackout.RevivePlayersAndSetDummyImp", SendOption.Reliable).StartMessage(-1);

        foreach (var player in Main.AllPlayerControls)
        {
            if (player.PlayerId == dummyImp.PlayerId)
            {
                sender.StartRpc(player.NetId, (byte)RpcCalls.SetRole);
                sender.Write((ushort)RoleTypes.Impostor);
                sender.Write(true);
                sender.EndRpc();
            }
            else
            {
                sender.StartRpc(player.NetId, (byte)RpcCalls.SetRole);
                sender.Write((ushort)RoleTypes.Crewmate);
                sender.Write(true);
                sender.EndRpc();
            }
        }

        sender.EndMessage();
        sender.SendMessage(dispose: false);
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
        if (doSend)
        {
            SendGameData();
            _ = new LateTask(RestoreIsDeadByExile, 0.3f, "AntiBlackOut_RestoreIsDeadByExile");
        }
    }

    private static void RestoreIsDeadByExile()
    {
        var sender = CustomRpcSender.Create("AntiBlackout RestoreIsDeadByExile", SendOption.Reliable);
        var hasValue = false;
        foreach (var player in Main.AllPlayerControls)
        {
            if (player.Data.IsDead && !player.Data.Disconnected)
            {
                sender.AutoStartRpc(player.NetId, (byte)RpcCalls.Exiled);
                sender.EndRpc();
                hasValue = true;
            }
        }
        sender.SendMessage(dispose: !hasValue);
    }

    public static void SendGameData([CallerMemberName] string callerMethodName = "")
    {
        logger.Info($"SendGameData is called from {callerMethodName}");
        Utils.SendGameData();
    }
    public static void OnDisconnect(NetworkedPlayerInfo player)
    {
        // Execution conditions: Client is the host, IsDead is overridden, player is already disconnected
        if (!AmongUsClient.Instance.AmHost || !IsCached || !player.Disconnected) return;
        isDeadCache[player.PlayerId] = (true, true);
        RevivePlayersAndSetDummyImp();
        player.IsDead = player.Disconnected = false;
        SendGameData();
    }
    public static void AntiBlackRpcVotingComplete(this MeetingHud __instance, MeetingHud.VoterState[] states, NetworkedPlayerInfo exiled, bool tie)
    {
        if (AmongUsClient.Instance.AmClient)
        {
            __instance.VotingComplete(states, exiled, tie);
        }

        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.IsHost()) continue;

            var sender = CustomRpcSender.Create("AntiBlack RpcVotingComplete." + pc.PlayerId, SendOption.Reliable);
            if (pc.IsNonHostModdedClient()) //For mod client show real result
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

            sender.SendMessage();
        }
    }
    public static void AfterMeetingTasks()
    {
        var timeNotify = 0f;

        if (BlackOutIsActive && CheckForEndVotingPatch.TempExileMsg != null)
        {
            timeNotify = 4f;
            foreach (var pc in Main.AllPlayerControls.Where(p => !p.IsModded()).ToArray())
            {
                pc.Notify(CheckForEndVotingPatch.TempExileMsg, time: timeNotify);
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
    public static void SetRealPlayerRoles()
    {
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return;

        RpcSetRoleReplacer.ResetRoleMapMidGame();

        Dictionary<int, CustomRpcSender> sendersByOwnerId = [];
        List<PlayerControl> selfExiled = [];

        foreach (var ((seerId, targetId), (roletype, _)) in RpcSetRoleReplacer.RoleMap)
        {
            // skip host
            if (seerId == PlayerControl.LocalPlayer.PlayerId) continue;

            var seer = seerId.GetPlayer();
            var target = targetId.GetPlayer();

            if (seer == null || target == null) continue;
            if (seer.IsModded()) continue;

            var isSelf = seerId == targetId;
            var isDead = target.Data.IsDead;
            var changedRoleType = roletype;

            if (isDead)
            {
                if (isSelf)
                {
                    selfExiled.Add(seer);

                    if (target.HasGhostRole()) changedRoleType = RoleTypes.GuardianAngel;
                    else if (target.Is(Custom_Team.Impostor) || target.HasDesyncRole()) changedRoleType = RoleTypes.ImpostorGhost;
                    else changedRoleType = RoleTypes.CrewmateGhost;
                }
                else
                {
                    if (roletype is RoleTypes.Impostor or RoleTypes.Shapeshifter or RoleTypes.Phantom)
                    {
                        changedRoleType = RoleTypes.ImpostorGhost;
                    }
                    else
                    {
                        changedRoleType = RoleTypes.CrewmateGhost;
                    }
                }
            }

            if (seer.AmOwner)
            {
                target.SetRole(changedRoleType, true);
                continue;
            }

            int ownerId = seer.OwnerId;
            if (!sendersByOwnerId.TryGetValue(ownerId, out var sender))
            {
                sender = CustomRpcSender.Create($"AntiBlackout.SetRealPlayerRoles.OwnerId{ownerId}", SendOption.Reliable);
                sendersByOwnerId[ownerId] = sender;
            }

            sender.RpcSetRole(target, changedRoleType, ownerId);
        }

        foreach (var pc in selfExiled)
        {
            int ownerId = pc.OwnerId;
            if (!sendersByOwnerId.TryGetValue(ownerId, out var sender))
            {
                sender = CustomRpcSender.Create($"AntiBlackout.SetRealPlayerRoles.OwnerId{ownerId}", SendOption.Reliable);
                sendersByOwnerId[ownerId] = sender;
            }

            sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.Exiled, ownerId);
            sender.EndRpc();

            if (!pc.IsModded() && pc.PlayerId == ExileControllerWrapUpPatch.AntiBlackout_LastExiled?.PlayerId)
            {
                sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.MurderPlayer, ownerId);
                sender.WriteNetObject(pc);
                sender.Write((int)MurderResultFlags.Succeeded);
                sender.EndRpc();

                pc.ReactorFlash(0.2f);
            }
        }

        foreach (var sender in sendersByOwnerId.Values)
        {
            sender.SendMessage(dispose: false);
        }

        ResetAllCooldown();
    }
    private static void ResetAllCooldown()
    {
        foreach (var seer in Main.AllPlayerControls)
        {
            seer.RpcResetAbilityCooldown();
        }
    }
    public static void ResetAfterMeeting()
    {
        // add 1 second delay
        _ = new LateTask(() =>
        {
            SkipTasks = false;
            ExilePlayerId = -1;
        }, 1f, "Reset Blackout");
    }
    public static void Reset()
    {
        logger.Info("==Reset==");
        isDeadCache ??= [];
        isDeadCache.Clear();
        IsCached = false;
        ShowExiledInfo = false;
        StoreExiledMessage = "";
        ExilePlayerId = -1;
        SkipTasks = false;
    }

    public static bool ShowExiledInfo = false;
    public static string StoreExiledMessage = "";
}
