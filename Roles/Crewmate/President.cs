using Hazel;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
using TOHE.Modules.Rpc;
using TOHE.Patches;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class President : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.President;
    private const int Id = 12300;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static OptionItem PresidentAbilityUses;
    private static OptionItem PresidentCanBeGuessedAfterRevealing;
    private static OptionItem HidePresidentEndCommand;
    private static OptionItem NeutralsSeePresident;
    private static OptionItem MadmatesSeePresident;
    private static OptionItem ImpsSeePresident;
    private static OptionItem CovenSeePresident;

    private static readonly Dictionary<byte, int> RevealLimit = [];
    private static readonly Dictionary<byte, bool> CheckPresidentReveal = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.President);
        PresidentAbilityUses = IntegerOptionItem.Create(Id + 10, GeneralOption.SkillLimitTimes, new(1, 20, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.President])
            .SetValueFormat(OptionFormat.Times);
        PresidentCanBeGuessedAfterRevealing = BooleanOptionItem.Create(Id + 11, "PresidentCanBeGuessedAfterRevealing", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.President]);
        NeutralsSeePresident = BooleanOptionItem.Create(Id + 12, "NeutralsSeePresident", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.President]);
        MadmatesSeePresident = BooleanOptionItem.Create(Id + 13, "MadmatesSeePresident", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.President]);
        ImpsSeePresident = BooleanOptionItem.Create(Id + 14, "ImpsSeePresident", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.President]);
        CovenSeePresident = BooleanOptionItem.Create(Id + 16, "CovenSeePresident", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.President]);
        HidePresidentEndCommand = BooleanOptionItem.Create(Id + 15, "HidePresidentEndCommand", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.President]);
    }
    public override void Init()
    {
        CheckPresidentReveal.Clear();
        RevealLimit.Clear();
    }
    public override void Add(byte playerId)
    {
        CheckPresidentReveal.Add(playerId, false);
        RevealLimit.Add(playerId, 1);
        playerId.SetAbilityUseLimit(PresidentAbilityUses.GetInt());
    }
    public override void Remove(byte playerId)
    {
        CheckPresidentReveal.Remove(playerId);
        RevealLimit.Remove(playerId);
    }

    public static bool CheckReveal(byte targetId) => CheckPresidentReveal.TryGetValue(targetId, out var canBeReveal) && canBeReveal;

    public static void FinishCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            ChatCommands.RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        var originMsg = text;

        if (!GameStates.IsMeeting || player == null || GameStates.IsExilling) return;
        if (!player.Is(CustomRoles.President)) return;

        if (player.GetAbilityUseLimit() < 1)
        {
            Utils.SendMessage(GetString("PresidentEndMax"), player.PlayerId);
            return;
        }
        player.RpcRemoveAbilityUse();

        foreach (var pva in MeetingHud.Instance.playerStates)
        {
            if (pva == null) continue;

            if (pva.VotedFor < 253)
                MeetingHud.Instance.RpcClearVote(pva.TargetPlayerId);
        }
        List<MeetingHud.VoterState> statesList = [];
        MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), null, true);
        MeetingHud.Instance.RpcClose();
    }

    public static void RevealCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            ChatCommands.RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        var originMsg = text;

        if (!GameStates.IsMeeting || player == null || GameStates.IsExilling) return;
        if (!player.Is(CustomRoles.President)) return;

        if (RevealLimit[player.PlayerId] < 1)
        {
            Utils.SendMessage(GetString("PresidentRevealMax"), player.PlayerId);
            return;
        }

        RevealLimit[player.PlayerId]--;
        CheckPresidentReveal[player.PlayerId] = true;
        foreach (var tar in Main.AllAlivePlayerControls)
        {
            if (!MadmatesSeePresident.GetBool() && tar.Is(CustomRoles.Madmate) && tar != player) continue;
            if (!NeutralsSeePresident.GetBool() && tar.GetCustomRole().IsNeutral() && !tar.GetCustomRole().IsMadmate()) continue;
            if (!ImpsSeePresident.GetBool() && tar.GetCustomRole().IsImpostorTeamV3() && !tar.Is(CustomRoles.Narc)) continue;
            if (!CovenSeePresident.GetBool() && tar.GetCustomRole().IsCoven()) continue;
            Utils.SendMessage(string.Format(GetString("PresidentRevealed"), player.GetRealName()), tar.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.President), GetString("PresidentRevealTitle")));
        }
        SendRPC(player.PlayerId, isEnd: false);
    }

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (CheckPresidentReveal[target.PlayerId])
            killer.SetKillCooldown(0.9f);
        return true;
    }

    private static void SendRPC(byte playerId, bool isEnd = true)
    {

        if (!isEnd)
        {
            var msg1 = new RpcPresidentReveal(PlayerControl.LocalPlayer.NetId, playerId, CheckPresidentReveal[playerId]);
            RpcUtils.LateBroadcastReliableMessage(msg1);
            return;
        }
        var msg2 = new RpcPresidentEnd(PlayerControl.LocalPlayer.NetId, playerId);
        RpcUtils.LateBroadcastReliableMessage(msg2);
    }
    public static void ReceiveRPC(MessageReader reader, PlayerControl pc, bool isEnd = true)
    {
        byte PlayerId = reader.ReadByte();
        if (!isEnd)
        {
            bool revealed = reader.ReadBoolean();
            CheckPresidentReveal[PlayerId] = revealed;
            return;
        }
        // FinishCommand(pc, "Command.Finish", "/finish", []);
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser, CustomRoles role, ref bool guesserSuicide)
    {
        if (role != CustomRoles.President) return false;
        if (CheckPresidentReveal[target.PlayerId] && !PresidentCanBeGuessedAfterRevealing.GetBool())
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessPresident"));
            return true;
        }
        return false;
    }
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
        => (target.Is(CustomRoles.President) && (seer.GetCustomRole().IsCrewmate() || seer.Is(CustomRoles.Narc)) && !seer.Is(CustomRoles.Madmate) && CheckPresidentReveal[target.PlayerId]) ||
            (target.Is(CustomRoles.President) && seer.Is(CustomRoles.Madmate) && MadmatesSeePresident.GetBool() && CheckPresidentReveal[target.PlayerId]) ||
            (target.Is(CustomRoles.President) && seer.GetCustomRole().IsNeutral() && NeutralsSeePresident.GetBool() && CheckPresidentReveal[target.PlayerId]) ||
            (target.Is(CustomRoles.President) && seer.GetCustomRole().IsCoven() && CovenSeePresident.GetBool() && CheckPresidentReveal[target.PlayerId]) ||
            (target.Is(CustomRoles.President) && seer.GetCustomRole().IsImpostor() && ImpsSeePresident.GetBool() && CheckPresidentReveal[target.PlayerId]);

    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => KnowRoleTarget(seer, target);
}
