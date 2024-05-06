using System;
using UnityEngine;

namespace TOHE;

static class DoubleTrigger
{
    public static readonly HashSet<byte> PlayerIdList = [];

    public static readonly Dictionary<byte, float> FirstTriggerTimer = [];
    public static readonly Dictionary<byte, byte> FirstTriggerTarget = [];
    public static readonly Dictionary<byte, Action> FirstTriggerAction = [];

    public static void Init()
    {
        PlayerIdList.Clear();
        FirstTriggerTimer.Clear();
        FirstTriggerAction.Clear();
    }
    public static void AddDoubleTrigger(this PlayerControl killer)
    {
        PlayerIdList.Add(killer.PlayerId);
    }
    private static bool CanDoubleTrigger(this PlayerControl killer)
    {
        return PlayerIdList.Contains(killer.PlayerId);
    }

    /// <summary>
    /// <returns>returns false on first action, returns true on second action</returns>
    /// </summary>
    public static bool CheckDoubleTrigger(this PlayerControl killer, PlayerControl target, Action firstAction)
    {
        if (FirstTriggerTimer.ContainsKey(killer.PlayerId))
        {
            if (FirstTriggerTarget[killer.PlayerId] != target.PlayerId)
            {
                // Single action on the first opponent if the second one is off target
                return false;
            }
            Logger.Info($"{killer.name} DoDoubleAction", "DoubleTrigger");
            FirstTriggerTimer.Remove(killer.PlayerId);
            FirstTriggerTarget.Remove(killer.PlayerId);
            FirstTriggerAction.Remove(killer.PlayerId);
            return true;
        }
        // Ignore kill interval when single action
        CheckMurderPatch.TimeSinceLastKill.Remove(killer.PlayerId);
        FirstTriggerTimer.Add(killer.PlayerId, 1f);
        FirstTriggerTarget.Add(killer.PlayerId, target.PlayerId);
        FirstTriggerAction.Add(killer.PlayerId, firstAction);
        return false;
    }
    public static void OnFixedUpdate(PlayerControl player)
    {
        if (!CanDoubleTrigger(player)) return;

        if (!GameStates.IsInTask)
        {
            FirstTriggerTimer.Clear();
            FirstTriggerTarget.Clear();
            FirstTriggerAction.Clear();
            return;
        }

        var playerId = player.PlayerId;
        if (!FirstTriggerTimer.ContainsKey(playerId)) return;

        FirstTriggerTimer[playerId] -= Time.fixedDeltaTime;
        if (FirstTriggerTimer[playerId] <= 0)
        {
            Logger.Info($"{player.name} Do single action", "DoubleTrigger");
            FirstTriggerAction[playerId]();

            FirstTriggerTimer.Remove(playerId);
            FirstTriggerTarget.Remove(playerId);
            FirstTriggerAction.Remove(playerId);
        }
    }
}
