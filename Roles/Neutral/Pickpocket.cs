using AmongUs.GameOptions;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Pickpocket : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Pickpocket;
    private const int Id = 17400;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Pickpocket);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    private static OptionItem VotesPerKill;
    private static OptionItem HideAdditionalVotes;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Pickpocket, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Pickpocket])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pickpocket]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Pickpocket]);
        VotesPerKill = FloatOptionItem.Create(Id + 12, "VotesPerKill", new(0.1f, 10f, 0.1f), 0.5f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Pickpocket]);
        HideAdditionalVotes = BooleanOptionItem.Create(Id + 14, GeneralOption.HideAdditionalVotes, false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Pickpocket]);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();

    public override int AddRealVotesNum(PlayerVoteArea ps)
    {
        return (int)(Main.AllPlayerControls.Count(x => x.GetRealKiller()?.PlayerId == ps.TargetPlayerId) * VotesPerKill.GetFloat());
    }
    public override void AddVisualVotes(PlayerVoteArea votedPlayer, ref List<MeetingHud.VoterState> statesList)
    {
        if (HideAdditionalVotes.GetBool()) return;

        var additionalVotes = (int)(Main.AllPlayerControls.Count(x => x.GetRealKiller()?.PlayerId == votedPlayer.TargetPlayerId) * VotesPerKill.GetFloat());

        for (var i = 0; i < additionalVotes; i++)
        {
            statesList.Add(new MeetingHud.VoterState()
            {
                VoterId = votedPlayer.TargetPlayerId,
                VotedForId = votedPlayer.VotedFor
            });
        }
    }
    public override void OnMurderPlayerAsKiller(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (isSuicide || inMeeting) return;

        _ = new LateTask(() =>
        {
            killer.Notify(string.Format(GetString("PickpocketGetVote"),
                ((Main.AllPlayerControls.Count(x => x.GetRealKiller()?.PlayerId == killer.PlayerId)) * VotesPerKill.GetFloat() + 1f)
                .ToString("0.0#####")));
        }, target.Is(CustomRoles.Burst) ? Burst.BurstKillDelay.GetFloat() : 0f, "BurstKillCheck");
    }
}
