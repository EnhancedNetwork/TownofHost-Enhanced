using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using Hazel;
using System;
using System.Runtime.CompilerServices;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using TOHE.Roles.Core.AssignManager;

namespace TOHE;

public static class AntiBlackout
{
    public static bool SkipTasks;

    private static Dictionary<(byte SeerID, byte TargetID), (RoleTypes RoleType, CustomRoles CustomRole)> CachedRoleMap = [];

    // Credit: EHR
    // Optimally, there's 1 living impostor and at least 2 living crewmates in everyone's POV.
    // We force this to prevent black screens after meetings.
    public static void SetOptimalRoleTypes()
    {
        // If there are only 2 or fewer players in the game in total, there's nothing we can do.
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default || PlayerControl.AllPlayerControls.Count <= 2) return;

        SkipTasks = true;
        CachedRoleMap = RpcSetRoleReplacer.RoleMap.ToDictionary(x => (x.Key.seerId, x.Key.targetId), x => (x.Value.roleType, x.Value.customRole));

        var players = Main.AllAlivePlayerControls;
        if (CheckForEndVotingPatch.TempExiledPlayer) players = [.. players.Where(x => x.PlayerId != CheckForEndVotingPatch.TempExiledPlayer.PlayerId)];
        PlayerControl dummyImp = players.OrderByDescending(x => x.GetCustomRole() is not (CustomRoles.DetectiveTOHE or CustomRoles.Detective)).ThenByDescending(x => x.IsModded()).MinBy(x => x.PlayerId);

        if (players.Count == 2)
        {
            // There are only 2 players alive. We need to revive 1 dead player to have 2 living crewmates.
            PlayerControl revived = Main.EnumeratePlayerControls().Where(x => !x.IsAlive() && !x.Data.Disconnected && x != CheckForEndVotingPatch.TempExiledPlayer?.Object).MaxBy(x => x.PlayerId);

            // The black screen cannot be prevented if there are no players to revive in this case.
            if (!revived)
            {
                // Fix the black screen manually for each player after the ejection screen.
                if (CheckForEndVotingPatch.TempExiledPlayer) CheckForEndVotingPatch.TempExiledPlayer.Object.FixBlackScreen();
                players.Do(x => x.FixBlackScreen());

                // Don't skip tasks since we couldn't set the optimal roles.
                SkipTasks = false;
                CachedRoleMap = [];
                return;
            }

            revived.RpcSetRoleGlobal(RoleTypes.Crewmate);
        }

        dummyImp.RpcSetRoleGlobal(RoleTypes.Impostor);
        players.Without(dummyImp).Where(x => x.GetCustomRole() is not (CustomRoles.DetectiveTOHE or CustomRoles.Detective)).Do(x => x.RpcSetRoleGlobal(RoleTypes.Crewmate));
        
        Main.EnumeratePlayerControls().DoIf(x => !x.IsAlive() && x.Data && x.Data.IsDead && (!x.AmOwner || !Utils.TempReviveHostRunning), x => x.RpcSetRoleGlobal(GhostRoleAssign.AssignedGhostRole.TryGetValue(x.PlayerId, out var ghostRole) ? ghostRole.GetRoleTypes() : RoleTypes.CrewmateGhost));
    }

    // After the ejection screen, we revert the role types to their actual values.
    public static void RevertToActualRoleTypes()
    {
        if (CachedRoleMap.Count == 0 || CustomWinnerHolder.WinnerTeam != CustomWinner.Default || GameStates.IsEnded)
        {
            SkipTasks = false;
            ExileControllerWrapUpPatch.AfterMeetingTasks();
            return;
        }

        // Set the temporarily revived crewmate back to dead.
        //foreach (PlayerControl pc in Main.EnumeratePlayerControls())
        //{
        //    try
        //    {
        //        if (pc.AmOwner && Utils.TempReviveHostRunning) continue;

        //        NetworkedPlayerInfo data = pc.Data;

        //        if (data != null && !data.IsDead && !data.Disconnected && !pc.IsAlive())
        //        {
        //            data.IsDead = true;
        //            data.SendGameData();
        //        }
        //    }
        //    catch (Exception e) { Utils.ThrowException(e); }
        //}

        // Reset the role types for all players.
        foreach (((byte seerId, byte targetId), (RoleTypes roleType, CustomRoles _)) in CachedRoleMap)
        {
            try
            {
                PlayerControl seer = seerId.GetPlayer();
                PlayerControl target = targetId.GetPlayer();
                if (!seer || !target || (seerId == targetId && seer.AmOwner && Utils.TempReviveHostRunning)) continue;

                if (target.IsAlive()) target.RpcSetRoleDesync(roleType, seer.OwnerId);
                else target.RpcSetRoleDesync(GhostRoleAssign.AssignedGhostRole.TryGetValue(targetId, out var ghostRole) ? ghostRole.GetRoleTypes() : seerId == targetId && !((target.GetRoleClass().ThisRoleBase.GetRoleTypesDirect() is RoleTypes.Impostor or RoleTypes.Phantom or RoleTypes.Shapeshifter or RoleTypes.Viper) && Options.DeadImpCantSabotage.GetBool()) && Main.PlayerStates.TryGetValue(targetId, out var state) && state.RoleClass.CanUseSabotage(target) ? RoleTypes.ImpostorGhost : RoleTypes.CrewmateGhost, seer.OwnerId);
            }
            catch (Exception e) { Utils.ThrowException(e); }
        }

        // Reset the role map to the original values.
        RpcSetRoleReplacer.RoleMap = CachedRoleMap.ToDictionary(x => (x.Key.SeerID, x.Key.TargetID), x => (x.Value.RoleType, x.Value.CustomRole));
        CachedRoleMap = [];

        LateTask.New(() =>
        {
            var elapsedSeconds = (int)ExileControllerWrapUpPatch.Stopwatch.Elapsed.TotalSeconds;
            
            foreach (PlayerControl pc in Main.EnumeratePlayerControls())
            {
                try
                {
                    if (pc.IsAlive())
                    {
                        // Due to the role base change, we need to reset the cooldowns for abilities.
                        // if (!Utils.ShouldNotApplyAbilityCooldownAfterMeeting(pc))
                        pc.RpcResetAbilityCooldown();

                        if (Main.AllPlayerKillCooldown.TryGetValue(pc.PlayerId, out float kcd))
                        {
                            float time = kcd - elapsedSeconds;
                            if (time > 0) pc.SetKillCooldown(time);
                        }
                        else
                            pc.SetKillCooldown();
                    }
                    else
                    {
                        if (pc.AmOwner && Utils.TempReviveHostRunning) continue;
                        
                        // Ensure that the players who are considered dead by the mod are actually dead in the game.
                        pc.RpcSetRoleGlobal(pc.GetGhostRoleBasis());
                        
                        if (GhostRoleAssign.AssignedGhostRole.TryGetValue(pc.PlayerId, out var ghostRole) && ghostRole.GetRoleTypes() == RoleTypes.GuardianAngel)
                            pc.RpcResetAbilityCooldown();
                    }
                }
                catch (Exception e) { Utils.ThrowException(e); }
            }

            // Only execute AfterMeetingTasks after everything is reset.
            LateTask.New(() =>
            {
                SkipTasks = false;
                ExileControllerWrapUpPatch.AfterMeetingTasks();
            }, 1f, "Reset SkipTasks after SetRealPlayerRoles");
        }, 0.2f, "SetRealPlayerRoles - Reset Cooldowns");
    }

    public static void Reset()
    {
        Logger.Info("==Reset==", "AntiBlackout");
        CachedRoleMap = [];
        SkipTasks = false;
    }
}
