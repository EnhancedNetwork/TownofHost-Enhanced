using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Coven;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Oracle : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Oracle;
    private const int Id = 9100;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Oracle);
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem CheckLimitOpt;
    private static OptionItem FailChance;
    private static OptionItem ChangeRecruitTeam;

    private readonly HashSet<byte> DidVote = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Oracle);
        CheckLimitOpt = IntegerOptionItem.Create(Id + 10, "OracleSkillLimit", new(0, 10, 1), 1, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Oracle])
            .SetValueFormat(OptionFormat.Times);
        FailChance = IntegerOptionItem.Create(Id + 13, "FailChance", new(0, 100, 5), 0, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Oracle])
            .SetValueFormat(OptionFormat.Percent);
        OracleAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 14, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Oracle])
            .SetValueFormat(OptionFormat.Times);
        ChangeRecruitTeam = BooleanOptionItem.Create(Id + 15, "OracleCheckAddons", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Oracle]);
        OverrideTasksData.Create(Id + 16, TabGroup.CrewmateRoles, CustomRoles.Oracle);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(CheckLimitOpt.GetFloat());
    }
    public override bool CheckVote(PlayerControl player, PlayerControl target)
    {
        if (player == null || target == null) return true;
        if (DidVote.Contains(player.PlayerId)) return true;
        DidVote.Add(player.PlayerId);

        var abilityUse = player.GetAbilityUseLimit();
        if (abilityUse < 1)
        {
            SendMessage(GetString("OracleCheckReachLimit"), player.PlayerId, ColorString(GetRoleColor(CustomRoles.Oracle), GetString("OracleCheckMsgTitle")));
            return true;
        }

        player.RpcRemoveAbilityUse();
        abilityUse--;

        if (player.PlayerId == target.PlayerId)
        {
            SendMessage(GetString("OracleCheckSelfMsg") + "\n\n" + string.Format(GetString("OracleCheckLimit"), abilityUse), player.PlayerId, ColorString(GetRoleColor(CustomRoles.Oracle), GetString("OracleCheckMsgTitle")));
            return true;
        }
        if (target.Is(CustomRoles.Stubborn))
        {
            SendMessage(GetString("StubbornNotify") + "\n\n" + string.Format(GetString("OracleCheckLimit"), abilityUse), player.PlayerId, ColorString(GetRoleColor(CustomRoles.Oracle), GetString("OracleCheckMsgTitle")));
            return true;
        }

        {
            string msg;

            {
                bool targetIsVM = false;
                if (target.Is(CustomRoles.VoodooMaster) && VoodooMaster.Dolls[target.PlayerId].Count > 0)
                {
                    target = GetPlayerById(VoodooMaster.Dolls[target.PlayerId].Where(x => x.GetPlayer().IsAlive()).ToList().RandomElement());
                    SendMessage(string.Format(GetString("VoodooMasterTargetInMeeting"), target.GetRealName()), Utils.GetPlayerListByRole(CustomRoles.VoodooMaster).First().PlayerId);
                    targetIsVM = true;
                }
                var targetName = target.GetRealName();
                if (targetIsVM) targetName = Utils.GetPlayerListByRole(CustomRoles.VoodooMaster).First().GetRealName();
                string text = "Crewmate";
                if (ChangeRecruitTeam.GetBool())
                {
                    if (target.Is(CustomRoles.Admired)) text = "Crewmate";
                    else if (Illusionist.IsCovIllusioned(target.PlayerId)) text = "Crewmate";
                    else if (Illusionist.IsNonCovIllusioned(target.PlayerId)) text = "Coven";
                    else if (target.GetCustomRole().IsImpostorTeamV2() || target.GetCustomSubRoles().Any(role => role.IsImpostorTeamV2())) text = "Impostor";
                    else if (target.GetCustomRole().IsNeutralTeamV2() || target.GetCustomSubRoles().Any(role => role.IsNeutralTeamV2())) text = "Neutral";
                    else if (target.IsPlayerCoven() || target.Is(CustomRoles.Enchanted)) text = "Coven";
                    else if (target.GetCustomRole().IsCrewmateTeamV2() && (target.GetCustomSubRoles().Any(role => role.IsCrewmateTeamV2()) || (target.GetCustomSubRoles().Count == 0))) text = "Crewmate";
                }
                else
                {
                    if (Illusionist.IsCovIllusioned(target.PlayerId)) text = "Crewmate";
                    else if (Illusionist.IsNonCovIllusioned(target.PlayerId)) text = "Coven";
                    else if (target.Is(Custom_Team.Impostor) && !target.Is(CustomRoles.Trickster)) text = "Impostor";
                    else if (target.GetCustomRole().IsNeutral()) text = "Neutral";
                    else if (target.Is(Custom_Team.Coven)) text = "Coven";
                    else text = "Crewmate";
                }

                if (FailChance.GetInt() > 0)
                {
                    int random_number_1 = IRandom.Instance.Next(1, 100);
                    if (random_number_1 <= FailChance.GetInt())
                    {
                        int random_number_2 = IRandom.Instance.Next(1, 4);
                        if (text == "Crewmate")
                        {
                            if (random_number_2 == 1) text = "Neutral";
                            if (random_number_2 == 2) text = "Impostor";
                            if (random_number_2 == 3) text = "Coven";
                        }
                        if (text == "Neutral")
                        {
                            if (random_number_2 == 1) text = "Crewmate";
                            if (random_number_2 == 2) text = "Impostor";
                            if (random_number_2 == 3) text = "Coven";
                        }
                        if (text == "Impostor")
                        {
                            if (random_number_2 == 1) text = "Neutral";
                            if (random_number_2 == 2) text = "Crewmate";
                            if (random_number_2 == 3) text = "Coven";
                        }
                        if (text == "Coven")
                        {
                            if (random_number_2 == 1) text = "Crewmate";
                            if (random_number_2 == 2) text = "Impostor";
                            if (random_number_2 == 3) text = "Neutral";
                        }
                    }
                }
                msg = string.Format(GetString("OracleCheck." + text), targetName);
            }

            SendMessage(GetString("OracleCheck") + "\n" + msg + "\n\n" + string.Format(GetString("OracleCheckLimit"), abilityUse), player.PlayerId, ColorString(GetRoleColor(CustomRoles.Oracle), GetString("OracleCheckMsgTitle")));
            SendMessage(GetString("VoteHasReturned"), player.PlayerId, title: ColorString(GetRoleColor(CustomRoles.Oracle), string.Format(GetString("VoteAbilityUsed"), GetString("Oracle"))));
            return false;
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo tagret)
    {
        DidVote.Clear();
    }
}
