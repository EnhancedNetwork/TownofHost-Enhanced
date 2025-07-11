using Hazel;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;
namespace TOHE.Roles.Crewmate;

internal class Socialite : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Socialite;
    private const int Id = 31800;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem AbilityCooldown;
    private static OptionItem AbilityLimit;
    private static OptionItem ResetParties;

    private readonly HashSet<byte> PartiedPlayers = [];
    private readonly HashSet<byte> PreGuestList = [];
    private readonly HashSet<byte> GuestList = [];
    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, Role);
        AbilityCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.AbilityCooldown, new(1f, 180f, 1f), 30f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[Role])
            .SetValueFormat(OptionFormat.Seconds);
        AbilityLimit = IntegerOptionItem.Create(Id + 11, GeneralOption.SkillLimitTimes, new(1, 200, 1), 30, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[Role])
            .SetValueFormat(OptionFormat.Times);
        ResetParties = BooleanOptionItem.Create(Id + 12, "SocialiteResetParties", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[Role]);
    }
    public override void Init()
    {
        PartiedPlayers.Clear();
        PreGuestList.Clear();
        GuestList.Clear();
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(AbilityLimit.GetInt());
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = AbilityCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false) => PartiedPlayers.Contains(seen.PlayerId) ? CustomRoles.Socialite.GetColoredTextByRole("♪") : string.Empty;
    public void SendRPC(PlayerControl player, PlayerControl target)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte playerId = reader.ReadByte();

        PartiedPlayers.Add(reader.ReadByte());
    }
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (PartiedPlayers.Contains(target.PlayerId)) return false;
        PartiedPlayers.Add(target.PlayerId);
        SendRPC(killer, target);
        killer.Notify(string.Format(GetString(("SocialitePartyMsg")), target.GetRealName()));
        killer.RpcRemoveAbilityUse();
        killer.SetKillCooldown();
        killer.ResetKillCooldown();
        return false;
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (GuestList.Contains(killer.PlayerId)) return false;
        if (PartiedPlayers.Contains(target.PlayerId))
        {
            killer.Notify(GetString("SocialiteBlockNotification"));
            PreGuestList.Add(killer.PlayerId);
            return true;
        }
        return false;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        foreach (var playerId in PreGuestList)
        {
            GuestList.Add(playerId);
        }
    }
    public override void OnMeetingHudStart(PlayerControl pc)
    {
        string message = GetString("SocialiteGuestListMsg");
        foreach (var playerId in GuestList)
        {
            message += PreGuestList.Contains(playerId) ? $"<i>{GetPlayerById(playerId).GetRealName()}</i>\n" : $"{GetPlayerById(playerId).GetRealName()}\n";
        }
        SendMessage(message, pc.PlayerId, title: CustomRoles.Socialite.ToColoredString().ToUpper());
    }
    public override bool CheckVote(PlayerControl voter, PlayerControl target)
    {
        if (voter == null || target == null) return true;
        if (GuestList.Contains(voter.PlayerId))
        {
            SendMessage(GetString("SocialiteAlreadyGuest"), voter.PlayerId, title: CustomRoles.Socialite.ToColoredString().ToUpper());
            return false;
        }
        GuestList.Add(target.PlayerId);
        SendMessage(string.Format(GetString("SocialiteVoteMsg"), target.GetRealName()), voter.PlayerId, title: CustomRoles.Socialite.ToColoredString().ToUpper());
        return false;
    }
    public override void AfterMeetingTasks()
    {
        PreGuestList.Clear();
        if (ResetParties.GetBool())
        {
            PartiedPlayers.Clear();
        }
    }
}
