using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TOHE;

static class TargetArrow
{
    class ArrowInfo
    {
        public byte From;
        public byte To;
        public ArrowInfo(byte from, byte to)
        {
            From = from;
            To = to;
        }
        public bool Equals(ArrowInfo obj)
        {
            return From == obj.From && To == obj.To;
        }
        public override string ToString()
        {
            return $"(From:{From} To:{To})";
        }
    }

    static readonly Dictionary<ArrowInfo, string> TargetArrows = new();
    static readonly string[] Arrows = {
        "↑",
        "↗",
        "→",
        "↘",
        "↓",
        "↙",
        "←",
        "↖",
        "・"
    };

    public static void Init()
    {
        TargetArrows.Clear();
    }
    /// <summary>
    /// Register a new target arrow object
    /// </summary>
    /// <param name="seer"></param>
    /// <param name="target"></param>
    /// <param name="coloredArrow"></param>
    public static void Add(byte seer, byte target)
    {
        var arrowInfo = new ArrowInfo(seer, target);
        if (!TargetArrows.Any(a => a.Key.Equals(arrowInfo)))
            TargetArrows[arrowInfo] = "・";
    }
    /// <summary>
    /// Delete target
    /// </summary>
    /// <param name="seer"></param>
    /// <param name="target"></param>
    public static void Remove(byte seer, byte target)
    {
        var arrowInfo = new ArrowInfo(seer, target);
        var removeList = new List<ArrowInfo>(TargetArrows.Keys.Where(k => k.Equals(arrowInfo)));
        foreach (var a in removeList.ToArray())
        {
            TargetArrows.Remove(a);
        }
    }
    /// <summary>
    /// Delete all targets of the same type
    /// </summary>
    /// <param name="seer"></param>
    public static void RemoveAllTarget(byte seer)
    {
        var removeList = new List<ArrowInfo>(TargetArrows.Keys.Where(k => k.From == seer));
        foreach (var arrowInfo in removeList.ToArray())
        {
            TargetArrows.Remove(arrowInfo);
        }
    }
    /// <summary>
    /// Get all visible target arrows
    /// </summary>
    /// <param name="seer"></param>
    /// <returns></returns>
    public static string GetArrows(PlayerControl seer, params byte[] targets)
    {
        var arrows = "";
        foreach (var arrowInfo in TargetArrows.Keys.Where(ai => ai.From == seer.PlayerId && targets.Contains(ai.To)).ToArray())
        {
            arrows += TargetArrows[arrowInfo];
        }
        return arrows;
    }
    /// <summary>
    /// Check target arrow every FixedUpdate
    /// Issue NotifyRoles when there are updates
    /// </summary>
    /// <param name="seer"></param>
    public static void OnFixedUpdate(PlayerControl seer)
    {
        if (!GameStates.IsInTask) return;

        var seerId = seer.PlayerId;
        var seerIsDead = !seer.IsAlive();

        var arrowList = new List<ArrowInfo>(TargetArrows.Keys.Where(a => a.From == seer.PlayerId));
        if (!arrowList.Any()) return;

        var update = false;
        foreach (var arrowInfo in arrowList.ToArray())
        {
            var targetId = arrowInfo.To;
            var target = Utils.GetPlayerById(targetId);
            if (seerIsDead || !target.IsAlive() && !seer.Is(CustomRoles.Spiritualist))
            {
                TargetArrows.Remove(arrowInfo);
                update = true;
                continue;
            }
            //Get the target direction vector
            var dir = target.transform.position - seer.transform.position;
            int index;
            if (dir.magnitude < 2)
            {
                //Display dots when close
                index = 8;
            }
            else
            {
                // Convert to index with -22.5 to 22.5 degrees as 0
                // Bottom is 0 degrees, left side is +180, right side is -180
                // Adding 180 degrees clockwise with top being 0 degrees
                // Add 45/2 to make index in 45 degree units
                var angle = Vector3.SignedAngle(Vector3.down, dir, Vector3.back) + 180 + 22.5;
                index = ((int)(angle / 45)) % 8;
            }
            var arrow = Arrows[index];
            if (TargetArrows[arrowInfo] != arrow)
            {
                TargetArrows[arrowInfo] = arrow;
                update = true;
            }
        }
        if (update)
        {
            Utils.NotifyRoles(SpecifySeer: seer, ForceLoop: false);
        }
    }
}
