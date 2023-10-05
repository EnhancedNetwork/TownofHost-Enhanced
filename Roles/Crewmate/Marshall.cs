using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

public static class Marshall
{
    private static readonly int Id = 9400;
    private static readonly List<byte> playerIdList = new();
    public static bool IsEnable = false;

    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Marshall);

    public static OptionItem OptionMadmateCanFindMarshall;

    public static bool MadmateCanFindMarshall = false;

    private static readonly Dictionary<byte, bool> IsExposed = new();
    private static readonly Dictionary<byte, bool> IsComplete = new();

    private static readonly HashSet<byte> TargetList = new();
    private static readonly Dictionary<byte, Color> TargetColorlist = new();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Marshall);
    //    OptionMadmateCanFindMarshall = BooleanOptionItem.Create(Id + 14, "MadmateCanFindMarshall", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Marshall]);
        OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.Marshall);
    }
    public static void Init()
    {
        playerIdList.Clear();
        IsEnable = false;

        //MadmateCanFindMarshall = OptionMadmateCanFindMarshall.GetBool();

        IsExposed.Clear();
        IsComplete.Clear();

        TargetList.Clear();
        TargetColorlist.Clear();
    }

    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;

        IsExposed[playerId] = false;
        IsComplete[playerId] = false;
    }
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
    private static bool GetExpose(PlayerControl pc)
    {
        if (!IsThisRole(pc.PlayerId) || !pc.IsAlive() || pc.Is(CustomRoles.Madmate)) return false;

        var marshallId = pc.PlayerId;
        return IsExposed[marshallId];
    }
    private static bool IsMarshallTarget(PlayerControl target) => IsEnable && (target.Is(CustomRoleTypes.Crewmate) || (target.Is(CustomRoles.Madmate) && MadmateCanFindMarshall));
    public static void CheckTask(PlayerControl marshall)
    {
        if (!marshall.IsAlive() || marshall.Is(CustomRoles.Madmate)) return;

        var marshallId = marshall.PlayerId;
        var marshallTask = marshall.GetPlayerTaskState();

        if (!IsExposed[marshallId])
        {
            foreach (var target in Main.AllAlivePlayerControls)
            {
           //     if (!IsMarshallTarget(target)) continue;

           //     TargetArrow.Add(target.PlayerId, snitchId);
            }
            IsExposed[marshallId] = true;
        }

    /*    if (IsComplete[marshallId] || !marshallTask.IsTaskFinished) return;

        foreach (var target in Main.AllAlivePlayerControls)
        {
            if (!IsMarshallTarget(target)) continue;

            var targetId = target.PlayerId;
            NameColorManager.Add(marshallId, targetId);

            
            //ターゲットは共通なので2回登録する必要はない
            if (!TargetList.Contains(targetId))
            {
                TargetList.Add(targetId);

                
            }
        } */

        NameNotifyManager.Notify(marshall, Translator.GetString("MarshallDoneTasks"));

        IsComplete[marshallId] = true;
    }

    /// <summary>
    /// タスクが進んだスニッチに警告マーク
    /// </summary>
    /// <param name="seer">キラーの場合有効</param>
    /// <param name="target">スニッチの場合有効</param>
    /// <returns></returns>
    public static string GetWarningMark(PlayerControl seer, PlayerControl target)
        => IsMarshallTarget(seer) && GetExpose(target) ? Utils.ColorString(RoleColor, "★") : "";

    /// <summary>
    /// キラーからスニッチに対する矢印
    /// </summary>
    /// <param name="seer">キラーの場合有効</param>
    /// <param name="target">キラーの場合有効</param>
    /// <returns></returns>
    public static string GetWarningArrow(PlayerControl seer, PlayerControl target = null)
    {
        if (GameStates.IsMeeting || !IsMarshallTarget(seer)) return "";
        if (target != null && seer.PlayerId != target.PlayerId) return "";

        var exposedMarshall = playerIdList.Where(s => !Main.PlayerStates[s].IsDead && IsExposed[s]);
        if (!exposedMarshall.Any()) return "";

        var warning = "★";
        

        return Utils.ColorString(RoleColor, warning);
    }
    /// <summary>
    /// スニッチからキラーへの矢印
    /// </summary>
    /// <param name="seer">スニッチの場合有効</param>
    /// <param name="target">スニッチの場合有効</param>
    /// <returns></returns>
    
    public static void OnCompleteTask(PlayerControl player)
    {
        if (!IsThisRole(player.PlayerId) /*|| player.Is(CustomRoles.Madmate)*/) return;
        CheckTask(player);
    }
}
