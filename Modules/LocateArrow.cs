using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TOHE;

static class LocateArrow
{
    class ArrowInfo
    {
        public byte From;
        public Vector3 To;
        public ArrowInfo(byte from, Vector3 to)
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

    static Dictionary<ArrowInfo, string> LocateArrows = new();
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
        LocateArrows.Clear();
    }
    /// <summary>
    /// Register a new target arrow object
    /// </summary>
    /// <param name="seer"></param>
    /// <param name="target"></param>
    /// <param name="coloredArrow"></param>
    public static void Add(byte seer, Vector3 locate)
    {
        var arrowInfo = new ArrowInfo(seer, locate);
        if (!LocateArrows.Any(a => a.Key.Equals(arrowInfo)))
            LocateArrows[arrowInfo] = "・";
    }
    /// <summary>
    /// Delete target
    /// </summary>
    /// <param name="seer"></param>
    /// <param name="target"></param>
    public static void Remove(byte seer, Vector3 locate)
    {
        var arrowInfo = new ArrowInfo(seer, locate);
        var removeList = new List<ArrowInfo>(LocateArrows.Keys.Where(k => k.Equals(arrowInfo)));
        foreach (var a in removeList)
        {
            LocateArrows.Remove(a);
        }
    }
    /// <summary>
    /// Delete all targets of the same type
    /// </summary>
    /// <param name="seer"></param>
    public static void RemoveAllTarget(byte seer)
    {
        var removeList = new List<ArrowInfo>(LocateArrows.Keys.Where(k => k.From == seer));
        foreach (var arrowInfo in removeList)
        {
            LocateArrows.Remove(arrowInfo);
        }
    }
    /// <summary>
    /// Get all visible target arrows
    /// </summary>
    /// <param name="seer"></param>
    /// <returns></returns>
    public static string GetArrows(PlayerControl seer)
    {
        var arrows = "";
        foreach (var arrowInfo in LocateArrows.Keys.Where(ai => ai.From == seer.PlayerId))
        {
            arrows += LocateArrows[arrowInfo];
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

        var arrowList = new List<ArrowInfo>(LocateArrows.Keys.Where(a => a.From == seer.PlayerId));
        if (!arrowList.Any()) return;

        var update = false;
        foreach (var arrowInfo in arrowList)
        {
            var loc = arrowInfo.To;
            if (seerIsDead)
            {
                LocateArrows.Remove(arrowInfo);
                update = true;
                continue;
            }
            //Get the target direction vector
            var dir = loc - seer.transform.position;
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
            if (LocateArrows[arrowInfo] != arrow)
            {
                LocateArrows[arrowInfo] = arrow;
                update = true;
            }
        }
        if (update)
        {
            Utils.NotifyRoles(SpecifySeer: seer, ForceLoop: false);
        }
    }
}
