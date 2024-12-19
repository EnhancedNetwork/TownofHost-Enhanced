using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Cleanser : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Cleanser;
    private const int Id = 6600;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Cleanser);
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    //==================================================================\\

    private static OptionItem CleanserUsesOpt;
    private static OptionItem CleansedCanGetAddon;
    //private static OptionItem AbilityUseGainWithEachTaskCompleted;

    private readonly HashSet<byte> CleansedPlayers = [];
    private readonly Dictionary<byte, byte> CleanserTarget = [];
    private bool DidVote;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Cleanser);
        CleanserUsesOpt = IntegerOptionItem.Create(Id + 10, "MaxCleanserUses", new(1, 14, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cleanser])
            .SetValueFormat(OptionFormat.Times);
        CleansedCanGetAddon = BooleanOptionItem.Create(Id + 11, "CleansedCanGetAddon", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cleanser]);

    }
    public override void Add(byte playerId)
    {
        CleanserTarget.Add(playerId, byte.MaxValue);
        AbilityLimit = CleanserUsesOpt.GetInt();
        DidVote = false;
    }
    public static bool CantGetAddon() => !CleansedCanGetAddon.GetBool();
    public override string GetProgressText(byte playerId, bool comms)
    {
        Color x;
        if (AbilityLimit > 0)
            x = Utils.GetRoleColor(CustomRoles.Cleanser);
        else x = Color.gray;
        return (Utils.ColorString(x, $"({AbilityLimit})"));
    }
    public override bool CheckVote(PlayerControl voter, PlayerControl target)
    {
        if (!voter.Is(CustomRoles.Cleanser)) return true;
        if (DidVote) return true;
        DidVote = true;
        if (AbilityLimit < 1) return true;
        if (target.PlayerId == voter.PlayerId)
        {
            Utils.SendMessage(GetString("CleanserRemoveSelf"), voter.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cleanser), GetString("CleanserTitle")));
            return true;
        }
        if (target.Is(CustomRoles.Stubborn))
        {
            Utils.SendMessage(GetString("CleanserCantRemove"), voter.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cleanser), GetString("CleanserTitle")));
            return true;
        }
        if (CleanserTarget[voter.PlayerId] != byte.MaxValue) return true;

        AbilityLimit--;
        CleanserTarget[voter.PlayerId] = target.PlayerId;
        Logger.Info($"{voter.GetNameWithRole()} cleansed {target.GetNameWithRole()}", "Cleansed");
        CleansedPlayers.Add(target.PlayerId);
        Utils.SendMessage(string.Format(GetString("CleanserRemovedRole"), target.GetRealName()), voter.PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cleanser), GetString("CleanserTitle")));
        SendSkillRPC();
        return false;
    }
    public override void OnReportDeadBody(PlayerControl baba, NetworkedPlayerInfo lilelam)
    {
        DidVote = false;
        foreach (var pid in CleanserTarget.Keys)
        {
            CleanserTarget[pid] = byte.MaxValue;
        }
    }
    public override void NotifyAfterMeeting()
    {
        foreach (var pid in CleanserTarget.Keys.ToArray())
        {
            var targetid = CleanserTarget[pid];
            if (targetid == byte.MaxValue) continue;
            var targetpc = Utils.GetPlayerById(targetid);
            if (targetpc == null) continue;

            targetpc.Notify(GetString("LostAddonByCleanser"));
        }
    }
    public override void AfterMeetingTasks()
    {
        foreach (var pid in CleanserTarget.Keys.ToArray())
        {
            if (pid == byte.MaxValue) continue;
            var targetid = CleanserTarget[pid];
            if (targetid == byte.MaxValue) continue;
            var targetpc = Utils.GetPlayerById(targetid);
            if (targetpc == null) continue;
            //var allAddons = targetpc.GetCustomSubRoles();
            targetpc.RpcSetCustomRole(CustomRoles.Cleansed);
            Logger.Info($"Removed all the add ons of {targetpc.GetNameWithRole()}", "Cleanser");
            //foreach (var role in allAddons)
            //{
            //    Main.PlayerStates[targetid].RemoveSubRole(role);
            //}
        }
        Utils.MarkEveryoneDirtySettings();
    }
}
