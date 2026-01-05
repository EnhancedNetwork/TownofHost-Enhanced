using Hazel;
using TOHE.Modules.Rpc;
using TOHE.Roles.Coven;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Snitch : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Snitch;
    private const int Id = 9500;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();

    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static readonly Color RoleColor = Utils.GetRoleColor(CustomRoles.Snitch);

    private static OptionItem OptionEnableTargetArrow;
    private static OptionItem OptionCanGetColoredArrow;
    private static OptionItem OptionCanFindNeutralKiller;
    private static OptionItem OptionCanFindNeutralApocalypse;
    private static OptionItem OptionCanFindCoven;
    private static OptionItem OptionCanFindMadmate;
    private static OptionItem OptionRemainingTasks;

    private static bool EnableTargetArrow;
    private static bool CanGetColoredArrow;
    private static bool CanFindNeutralKiller;
    private static bool CanFindNeutralApocalypse;
    private static bool CanFindCoven;
    private static bool CanFindMadmate;
    private static int RemainingTasksToBeFound;

    private static readonly Dictionary<byte, bool> IsExposed = [];
    private static readonly Dictionary<byte, bool> IsComplete = [];

    private static readonly HashSet<byte> TargetList = [];
    private static readonly Dictionary<byte, Color> TargetColorlist = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Snitch);
        OptionEnableTargetArrow = BooleanOptionItem.Create(Id + 10, "SnitchEnableTargetArrow", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Snitch]);
        OptionCanGetColoredArrow = BooleanOptionItem.Create(Id + 11, "SnitchCanGetArrowColor", true, TabGroup.CrewmateRoles, false).SetParent(OptionEnableTargetArrow);
        OptionCanFindNeutralKiller = BooleanOptionItem.Create(Id + 12, "SnitchCanFindNeutralKiller", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Snitch]);
        OptionCanFindNeutralApocalypse = BooleanOptionItem.Create(Id + 15, "SnitchCanFindNeutralApoc", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Snitch]);
        OptionCanFindCoven = BooleanOptionItem.Create(Id + 16, "SnitchCanFindCoven", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Snitch]);
        OptionCanFindMadmate = BooleanOptionItem.Create(Id + 14, "SnitchCanFindMadmate", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Snitch]);
        OptionRemainingTasks = IntegerOptionItem.Create(Id + 13, "SnitchRemainingTaskFound", new(0, 10, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Snitch]);
        OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.Snitch);
    }
    public override void Init()
    {
        playerIdList.Clear();

        EnableTargetArrow = OptionEnableTargetArrow.GetBool();
        CanGetColoredArrow = OptionCanGetColoredArrow.GetBool();
        CanFindNeutralKiller = OptionCanFindNeutralKiller.GetBool();
        CanFindNeutralApocalypse = OptionCanFindNeutralApocalypse.GetBool();
        CanFindCoven = OptionCanFindCoven.GetBool();
        CanFindMadmate = OptionCanFindMadmate.GetBool();
        RemainingTasksToBeFound = OptionRemainingTasks.GetInt();

        IsExposed.Clear();
        IsComplete.Clear();

        TargetList.Clear();
        TargetColorlist.Clear();
    }

    public override void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);

        IsExposed[playerId] = false;
        IsComplete[playerId] = false;
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);

        IsExposed.Remove(playerId);
        IsComplete.Remove(playerId);
    }

    private static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

    private static bool GetExpose(PlayerControl pc)
    {
        if (!IsThisRole(pc.PlayerId) || !pc.IsAlive() || pc.Is(CustomRoles.Madmate)) return false;

        var snitchId = pc.PlayerId;
        return IsExposed[snitchId];
    }

    private static bool IsSnitchTarget(PlayerControl target)
        => HasEnabled && ((target.GetCustomRole().IsImpostorTeamV3() && !target.Is(CustomRoles.Trickster) && !target.Is(CustomRoles.Narc))
        || (target.IsNeutralKiller() && CanFindNeutralKiller)
        || ((target.IsNeutralApocalypse() || Lich.IsCursed(target)) && CanFindNeutralApocalypse)
        || (target.IsPlayerCoven() && CanFindCoven)
        || ((target.Is(CustomRoles.Madmate) || target.Is(CustomRoles.Rascal)) && CanFindMadmate));

    private void CheckTask(PlayerControl snitch)
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
            SendRPC(0, snitchId);
        }

        if (IsComplete[snitchId] || !snitchTask.IsTaskFinished) return;

        foreach (var target in Main.AllAlivePlayerControls)
        {
            if (!IsSnitchTarget(target)) continue;

            var targetId = target.PlayerId;
            NameColorManager.Add(snitchId, targetId);

            if (!EnableTargetArrow) continue;

            TargetArrow.Add(snitchId, targetId);

            if (!TargetList.Contains(targetId))
            {
                TargetList.Add(targetId);

                if (CanGetColoredArrow)
                    TargetColorlist.Add(targetId, target.GetRoleColor());
            }
        }

        snitch.Notify(GetString("SnitchDoneTasks"));

        IsComplete[snitchId] = true;
        SendRPC(1, snitchId);
    }

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (!IsThisRole(player.PlayerId) || player.Is(CustomRoles.Madmate)) return true;

        CheckTask(player);
        return true;
    }

    private void SendRPC(byte RpcTypeId, byte snitchId)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(RpcTypeId);
        writer.Write(snitchId);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        var RpcTypeId = reader.ReadByte();
        var snitchId = reader.ReadByte();

        switch (RpcTypeId)
        {
            case 0:
                IsExposed[snitchId] = true;
                break;
            case 1:
                if (EnableTargetArrow)
                {
                    foreach (var target in Main.AllAlivePlayerControls)
                    {
                        if (!IsSnitchTarget(target) || !(Illusionist.IsNonCovIllusioned(target.PlayerId) && Illusionist.SnitchCanIllusioned.GetBool())) continue;

                        var targetId = target.PlayerId;

                        if (!TargetList.Contains(targetId))
                        {
                            TargetList.Add(targetId);

                            if (CanGetColoredArrow)
                                TargetColorlist.Add(targetId, target.GetRoleColor());
                        }
                    }
                }
                IsComplete[snitchId] = true;
                break;
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (seen == seer) return string.Empty;

        return IsSnitchTarget(seen) && IsComplete[seer.PlayerId] ? Utils.ColorString(RoleColor, "⚠") : string.Empty;
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
    {
        if (!EnableTargetArrow || isForMeeting || seer.Is(CustomRoles.Madmate)) return string.Empty;
        if (target != null && seer.PlayerId != target.PlayerId) return string.Empty;

        var arrows = "";
        foreach (var targetId in TargetList)
        {
            var arrow = TargetArrow.GetArrows(seer, targetId);
            arrows += CanGetColoredArrow ? Utils.ColorString(TargetColorlist[targetId], arrow) : arrow;
        }
        return arrows;
    }

    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (target == null) return string.Empty;

        return IsSnitchTarget(seer) && GetExpose(target) ? Utils.ColorString(RoleColor, "⚠") : string.Empty;
    }

    public override string GetSuffixOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (target != null && seer.PlayerId != target.PlayerId) return string.Empty;
        if (!IsSnitchTarget(seer) || isForMeeting) return string.Empty;

        var exposedSnitch = playerIdList.Where(s => !Main.PlayerStates[s].IsDead && IsExposed[s]).ToArray();
        if (exposedSnitch.Length <= 0) return string.Empty;

        var warning = "⚠";
        if (EnableTargetArrow)
            warning += TargetArrow.GetArrows(seer, [.. exposedSnitch]);

        return Utils.ColorString(RoleColor, warning);
    }

    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl pc, CustomRoles role, ref bool guesserSuicide)
    {
        if (role != CustomRoles.Snitch) return false;
        if (target.GetPlayerTaskState().IsTaskFinished)
        {
            pc.ShowInfoMessage(isUI, GetString("EGGuessSnitchTaskDone"));
            return true;
        }
        return false;
    }
}
