using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Translator;

using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class Snitch : RoleBase
{
    private static readonly int Id = 9500;
    private static readonly List<byte> playerIdList = [];
    public static bool On = false;
    public override bool IsEnable => On;
    public static bool HasEnabled => CustomRoles.Snitch.IsClassEnable();
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Snitch);

    private static OptionItem OptionEnableTargetArrow;
    private static OptionItem OptionCanGetColoredArrow;
    private static OptionItem OptionCanFindNeutralKiller;
    private static OptionItem OptionCanFindMadmate;
    private static OptionItem OptionRemainingTasks;

    private static bool EnableTargetArrow;
    private static bool CanGetColoredArrow;
    private static bool CanFindNeutralKiller;
    private static bool CanFindMadmate;
    private static int RemainingTasksToBeFound;

    public static readonly Dictionary<byte, bool> IsExposed = [];
    public static readonly Dictionary<byte, bool> IsComplete = [];

    private static readonly HashSet<byte> TargetList = [];
    private static readonly Dictionary<byte, Color> TargetColorlist = [];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Snitch);
        OptionEnableTargetArrow = BooleanOptionItem.Create(Id + 10, "SnitchEnableTargetArrow", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Snitch]);
        OptionCanGetColoredArrow = BooleanOptionItem.Create(Id + 11, "SnitchCanGetArrowColor", true, TabGroup.CrewmateRoles, false).SetParent(OptionEnableTargetArrow);
        OptionCanFindNeutralKiller = BooleanOptionItem.Create(Id + 12, "SnitchCanFindNeutralKiller", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Snitch]);
        OptionCanFindMadmate = BooleanOptionItem.Create(Id + 14, "SnitchCanFindMadmate", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Snitch]);
        OptionRemainingTasks = IntegerOptionItem.Create(Id + 13, "SnitchRemainingTaskFound", new(0, 10, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Snitch]);
        OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.Snitch);
    }
    public override void Init()
    {
        playerIdList.Clear();
        On = false;

        EnableTargetArrow = OptionEnableTargetArrow.GetBool();
        CanGetColoredArrow = OptionCanGetColoredArrow.GetBool();
        CanFindNeutralKiller = OptionCanFindNeutralKiller.GetBool();
        CanFindMadmate = OptionCanFindMadmate.GetBool();
        RemainingTasksToBeFound = OptionRemainingTasks.GetInt();

        IsExposed.Clear();
        IsComplete.Clear();

        TargetList.Clear();
        TargetColorlist.Clear();
    }

    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        On = true;

        IsExposed[playerId] = false;
        IsComplete[playerId] = false;
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);

        IsExposed.Remove(playerId);
        IsComplete.Remove(playerId);
    }

    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
    private static bool GetExpose(PlayerControl pc)
    {
        if (!IsThisRole(pc.PlayerId) || !pc.IsAlive() || pc.Is(CustomRoles.Madmate)) return false;

        var snitchId = pc.PlayerId;
        return IsExposed[snitchId];
    }
    private static bool IsSnitchTarget(PlayerControl target) => HasEnabled && (target.Is(CustomRoleTypes.Impostor) && !target.Is(CustomRoles.Trickster) || (target.IsSnitchTarget() && CanFindNeutralKiller) || (target.Is(CustomRoles.Madmate) && CanFindMadmate) || (target.Is(CustomRoles.Rascal) && CanFindMadmate));
    public static void CheckTask(PlayerControl snitch)
    {
        if (!snitch.IsAlive() || snitch.Is(CustomRoles.Madmate)) return;

        var snitchId = snitch.PlayerId;
        var snitchTask = snitch.GetPlayerTaskState();

        if (!IsExposed[snitchId] && snitchTask.RemainingTasksCount <= RemainingTasksToBeFound)
        {
            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (!IsSnitchTarget(target)) continue;

                TargetArrow.Add(target.PlayerId, snitchId);
            }
            IsExposed[snitchId] = true;
        }

        if (IsComplete[snitchId] || !snitchTask.IsTaskFinished) return;

        foreach (var target in Main.AllAlivePlayerControls)
        {
            if (!IsSnitchTarget(target)) continue;

            var targetId = target.PlayerId;
            NameColorManager.Add(snitchId, targetId);

            if (!EnableTargetArrow) continue;

            TargetArrow.Add(snitchId, targetId);

            //ターゲットは共通なので2回登録する必要はない
            if (!TargetList.Contains(targetId))
            {
                TargetList.Add(targetId);

                if (CanGetColoredArrow)
                    TargetColorlist.Add(targetId, target.GetRoleColor());
            }
        }

        NameNotifyManager.Notify(snitch, Translator.GetString("SnitchDoneTasks"));

        IsComplete[snitchId] = true;
    }

    /// <summary>
    /// タスクが進んだスニッチに警告マーク
    /// </summary>
    /// <param name="seer">キラーの場合有効</param>
    /// <param name="target">スニッチの場合有効</param>
    /// <returns></returns>
    public static string GetWarningMark(PlayerControl seer, PlayerControl target)
        => IsSnitchTarget(seer) && GetExpose(target) ? Utils.ColorString(RoleColor, "⚠") : "";

    /// <summary>
    /// キラーからスニッチに対する矢印
    /// </summary>
    /// <param name="seer">キラーの場合有効</param>
    /// <param name="target">キラーの場合有効</param>
    /// <returns></returns>
    public static string GetWarningArrow(PlayerControl seer, PlayerControl target = null)
    {
        if (!HasEnabled) return "";
        if (!IsSnitchTarget(seer) || GameStates.IsMeeting) return "";
        if (target != null && seer.PlayerId != target.PlayerId) return "";

        var exposedSnitch = playerIdList.Where(s => !Main.PlayerStates[s].IsDead && IsExposed[s]);
        if (!exposedSnitch.Any()) return "";

        var warning = "⚠";
        if (EnableTargetArrow)
            warning += TargetArrow.GetArrows(seer, exposedSnitch.ToArray());

        return Utils.ColorString(RoleColor, warning);
    }
    /// <summary>
    /// スニッチからキラーへの矢印
    /// </summary>
    /// <param name="seer">スニッチの場合有効</param>
    /// <param name="target">スニッチの場合有効</param>
    /// <returns></returns>
    public override string GetSuffix(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
    {
        if (!IsThisRole(seer.PlayerId) || seer.Is(CustomRoles.Madmate)) return "";
        if (!EnableTargetArrow || GameStates.IsMeeting) return "";
        if (target != null && seer.PlayerId != target.PlayerId) return "";
        var arrows = "";
        foreach (var targetId in TargetList)
        {
            var arrow = TargetArrow.GetArrows(seer, targetId);
            arrows += CanGetColoredArrow ? Utils.ColorString(TargetColorlist[targetId], arrow) : arrow;
        }
        return arrows;
    }
    public static void OnCompleteTask(PlayerControl player)
    {
        if (!IsThisRole(player.PlayerId) || player.Is(CustomRoles.Madmate)) return;
        CheckTask(player);
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl pc)
    {
        if (target.Is(CustomRoles.Snitch) && target.GetPlayerTaskState().IsTaskFinished)
        {
            if (!isUI) Utils.SendMessage(GetString("EGGuessSnitchTaskDone"), pc.PlayerId);
            else pc.ShowPopUp(GetString("EGGuessSnitchTaskDone"));
            return true;
        }
        return false;
    }
}
