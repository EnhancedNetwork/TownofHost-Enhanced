using System;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class FortuneTeller : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 8000;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.FortuneTeller);
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem CheckLimitOpt;
    private static OptionItem AccurateCheckMode;
    private static OptionItem ShowSpecificRole;
    private static OptionItem RandomActiveRoles;

    private readonly HashSet<byte> didVote = [];
    private readonly HashSet<byte> targetList = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.FortuneTeller);
        CheckLimitOpt = IntegerOptionItem.Create(Id + 10, GeneralOption.SkillLimitTimes, new(0, 20, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller])
            .SetValueFormat(OptionFormat.Times);
        RandomActiveRoles = BooleanOptionItem.Create(Id + 11, "RandomActiveRoles", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller]);
        AccurateCheckMode = BooleanOptionItem.Create(Id + 12, "AccurateCheckMode", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller]);
        ShowSpecificRole = BooleanOptionItem.Create(Id + 13, "ShowSpecificRole", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller]);
        FortuneTellerAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 15, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller])
            .SetValueFormat(OptionFormat.Times);
        OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.FortuneTeller);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(CheckLimitOpt.GetInt());
    }
    private static string GetTargetRoleList(CustomRoles[] roles)
    {
        return roles != null ? string.Join("\n", roles.Select(role => $"    ★ {GetRoleName(role)}")) : "";
    }
    public override bool CheckVote(PlayerControl player, PlayerControl target)
    {
        if (player == null || target == null) return true;
        if (didVote.Contains(player.PlayerId)) return true;
        didVote.Add(player.PlayerId);

        var abilityUse = player.GetAbilityUseLimit();
        if (abilityUse < 1)
        {
            SendMessage(GetString("FortuneTellerCheckReachLimit"), player.PlayerId, ColorString(GetRoleColor(CustomRoles.FortuneTeller), GetString("FortuneTellerCheckMsgTitle")));
            return true;
        }

        if (RandomActiveRoles.GetBool())
        {
            if (targetList.Contains(target.PlayerId))
            {
                SendMessage(GetString("FortuneTellerAlreadyCheckedMsg") + "\n\n" + string.Format(GetString("FortuneTellerCheckLimit"), abilityUse), player.PlayerId, ColorString(GetRoleColor(CustomRoles.FortuneTeller), GetString("FortuneTellerCheckMsgTitle")));
                return true;
            }
        }

        player.RpcRemoveAbilityUse();

        abilityUse = player.GetAbilityUseLimit();
        if (player.PlayerId == target.PlayerId)
        {
            SendMessage(GetString("FortuneTellerCheckSelfMsg") + "\n\n" + string.Format(GetString("FortuneTellerCheckLimit"), abilityUse), player.PlayerId, ColorString(GetRoleColor(CustomRoles.FortuneTeller), GetString("FortuneTellerCheckMsgTitle")));
            return true;
        }

        string msg;

        if ((player.AllTasksCompleted() || AccurateCheckMode.GetBool()) && ShowSpecificRole.GetBool())
        {
            msg = string.Format(GetString("FortuneTellerCheck.TaskDone"), target.GetRealName(), GetString(target.GetCustomRole().ToString()));
        }
        else if (RandomActiveRoles.GetBool())
        {
            targetList.Add(target.PlayerId);
            var targetRole = target.GetCustomRole();
            var activeRoleList = CustomRolesHelper.AllRoles.Where(role => (role.IsEnable() || role.RoleExist(countDead: true)) && role != targetRole && !role.IsAdditionRole()).ToList();
            var count = Math.Min(4, activeRoleList.Count);
            List<CustomRoles> roleList = [targetRole];
            var rand = IRandom.Instance;
            for (int i = 0; i < count; i++)
            {
                int randomIndex = rand.Next(activeRoleList.Count);
                roleList.Add(activeRoleList[randomIndex]);
                activeRoleList.RemoveAt(randomIndex);
            }
            for (int i = roleList.Count - 1; i > 0; i--)
            {
                int j = rand.Next(0, i + 1);
                (roleList[j], roleList[i]) = (roleList[i], roleList[j]);
            }
            var text = GetTargetRoleList([.. roleList]);
            msg = string.Format(GetString("FortuneTellerCheck.Result"), target.GetRealName(), text);
        }
        else
        {
            List<CustomRoles[]> completeRoleList = EnumHelper.Achunk<CustomRoles>(chunkSize: 6, shuffle: true, exclude: (x) => !x.IsGhostRole() && !x.IsAdditionRole() && !x.IsVanilla() && x is not CustomRoles.NotAssigned and not CustomRoles.ChiefOfPolice and not CustomRoles.Killer and not CustomRoles.GM);

            var targetRole = target.GetCustomRole();
            string text = string.Empty;

            text = GetTargetRoleList(completeRoleList.FirstOrDefault(x => x.Contains(targetRole)));

            if (text == string.Empty)
            {
                msg = string.Format(GetString("FortuneTellerCheck.Null"), target.GetRealName());
            }
            else
            {
                msg = string.Format(GetString("FortuneTellerCheck.Result"), target.GetRealName(), text);
            }
        }

        SendMessage(GetString("FortuneTellerCheck") + "\n" + msg + "\n\n" + string.Format(GetString("FortuneTellerCheckLimit"), abilityUse), player.PlayerId, ColorString(GetRoleColor(CustomRoles.FortuneTeller), GetString("FortuneTellerCheckMsgTitle")));
        SendMessage(GetString("VoteHasReturned"), player.PlayerId, title: ColorString(GetRoleColor(CustomRoles.FortuneTeller), string.Format(GetString("VoteAbilityUsed"), GetString("FortuneTeller"))));
        return false;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        didVote.Clear();
    }
}
