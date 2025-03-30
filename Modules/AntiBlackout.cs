using AmongUs.GameOptions;
using Hazel;
using System;
using System.Runtime.CompilerServices;
using TOHE.Modules;
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

        // For vanilla crew basis, we always try serialize host as dummy imp

        var hasValue = false;
        var sender = CustomRpcSender.Create("AntiBlackout.RevivePlayersAndSetDummyImp", SendOption.Reliable);
        foreach (var seer in Main.AllPlayerControls)
        {
            if (seer.IsModded()) continue;
            var seerIsAliveAndHasKillButton = seer.HasImpKillButton() && seer.IsAlive();

            if (Options.CurrentGameMode is CustomGameMode.SpeedRun or CustomGameMode.FFA)
            {
                seerIsAliveAndHasKillButton = false;
            }

            if (Options.CurrentGameMode is not CustomGameMode.SpeedRun)
            {
                foreach (var target in Main.AllPlayerControls)
                {
                    if (seerIsAliveAndHasKillButton)
                    {
                        if (target.PlayerId != seer.PlayerId)
                        {
                            sender.RpcSetRole(target, RoleTypes.Crewmate, seer.GetClientId());
                            hasValue = true;
                        }
                    }
                    else
                    {
                        if (target.PlayerId == dummyImp.PlayerId)
                        {
                            sender.RpcSetRole(target, RoleTypes.Impostor, seer.GetClientId());
                            hasValue = true;
                        }
                        else
                        {
                            sender.RpcSetRole(target, RoleTypes.Crewmate, seer.GetClientId());
                            hasValue = true;
                        }
                    }
                }
            }
            else
            {
                sender.RpcSetRole(seer, RoleTypes.Impostor, seer.OwnerId);
                foreach (var target in Main.AllPlayerControls)
                {
                    if (target.PlayerId != seer.PlayerId)
                    {
                        sender.RpcSetRole(target, RoleTypes.Crewmate, seer.OwnerId);
                    }
                }

                hasValue = true;
            }
        }
        sender.SendMessage(dispose: !hasValue);
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
        Utils.SendGameDataAll();
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

        var sender = CustomRpcSender.Create("AntiBlack RpcVotingComplete", SendOption.Reliable);
        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.IsHost()) continue;
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
        }
        sender.SendMessage();
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

        var hasValue = false;

        var sender = CustomRpcSender.Create("AntiBlackout.SetRealPlayerRoles", SendOption.Reliable);

        List<PlayerControl> selfExiled = [];

        if (Options.CurrentGameMode is CustomGameMode.Standard)
        {
            foreach (var ((seerId, targetId), (roletype, _)) in RpcSetRoleReplacer.RoleMap)
            {
                // skip host
                if (seerId == 0) continue;

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
                        var seerIsKiller = seer.Is(Custom_Team.Impostor) || seer.HasDesyncRole();
                        if (!seerIsKiller && target.Is(Custom_Team.Impostor)) changedRoleType = RoleTypes.ImpostorGhost;
                        else changedRoleType = RoleTypes.CrewmateGhost;
                    }
                }

                if (!isDead && isSelf && seer.HasImpKillButton()) continue;

                sender.RpcSetRole(target, changedRoleType, seer.OwnerId);
                hasValue = true;
            }

            foreach (var pc in selfExiled)
            {
                sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.Exiled, -1);
                sender.EndRpc();
                hasValue = true;

                if (pc.PlayerId == ExileControllerWrapUpPatch.AntiBlackout_LastExiled?.PlayerId)
                {
                    sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.MurderPlayer, pc.OwnerId);
                    sender.WriteNetObject(pc);
                    sender.Write((int)MurderResultFlags.Succeeded);
                    sender.EndRpc();

                    pc.ReactorFlash(0.2f);
                }
            }
        }
        else if (Options.CurrentGameMode is CustomGameMode.SpeedRun)
        {
            foreach (var pc in Main.AllPlayerControls)
            {
                sender.RpcSetRole(pc, RoleTypes.Crewmate, pc.OwnerId);
                if (pc.IsModded()) continue;

                if (!pc.Is(CustomRoles.Runner))
                {
                    sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.MurderPlayer, pc.OwnerId);
                    sender.WriteNetObject(pc);
                    sender.Write((int)MurderResultFlags.Succeeded);
                    sender.EndRpc();

                    pc.ReactorFlash(0.2f);
                }
            }

            hasValue = true;
        }
        else if (Options.CurrentGameMode is CustomGameMode.FFA)
        {
            sender.RpcSetRole(PlayerControl.LocalPlayer, RoleTypes.Crewmate, -1);
            foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Killer)))
            {
                sender.RpcSetRole(pc, RoleTypes.Impostor, pc.OwnerId);
            }

            foreach (var pc in Main.AllPlayerControls.Where(x => !x.Is(CustomRoles.Killer)))
            {
                if (pc.IsModded()) continue;
                sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.MurderPlayer, pc.OwnerId);
                sender.WriteNetObject(pc);
                sender.Write((int)MurderResultFlags.Succeeded);
                sender.EndRpc();

                pc.ReactorFlash(0.2f);
            }

            hasValue = true;
        }

        sender.SendMessage(dispose: !hasValue);
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
