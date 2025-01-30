using System;
using UnityEngine;

namespace TOHE.Modules;

// Credit: EHR by Gurge44: https://github.com/Gurge44/EndlessHostRoles
public static class KillTimerManager
{
    public static readonly Dictionary<byte, float> AllKillTimers = [];

    public static void Initializate()
    {
        AllKillTimers.Clear();
    }

    public static float GetKillTimer(this PlayerControl pc) => pc.AmOwner ? pc.killTimer : AllKillTimers[pc.PlayerId];
    public static float GetKillTimer(this byte playerId) => AllKillTimers[playerId];

    public static void SetKillTimer(this PlayerControl pc, bool half = false, float CD = -1f)
    {
        float resultKCD;
        if (Math.Abs(CD - (-1f)) < 0.5f)
        {
            resultKCD = Main.AllPlayerKillCooldown.GetValueOrDefault(pc.PlayerId, 0f);

            if (half)
            {
                resultKCD /= 2f;
            }
        }
        else
        {
            resultKCD = CD;
        }

        if (AllKillTimers.TryGetValue(pc.PlayerId, out var timer) && timer > resultKCD) return;
        AllKillTimers[pc.PlayerId] = resultKCD;
    }

    public static void FixedUpdate(PlayerControl player)
    {
        if (GameStates.IsMeeting || player.inVent || player.MyPhysics.Animations.IsPlayingEnterVentAnimation()) return;

        var playerId = player.PlayerId;
        if (!AllKillTimers.TryAdd(playerId, 10f) && player.GetKillTimer() > 0)
        {
            AllKillTimers[playerId] -= Time.fixedDeltaTime;
        }
    }
}
