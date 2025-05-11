using static TOHE.MeetingHudStartPatch;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Celebrity : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Celebrity;
    private const int Id = 6500;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    //==================================================================\\

    private static OptionItem ImpKnowCelebrityDead;
    private static OptionItem NeutralKnowCelebrityDead;
    private static OptionItem CovenKnowCelebrityDead;

    private static readonly HashSet<byte> CelebrityDead = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Celebrity);
        ImpKnowCelebrityDead = BooleanOptionItem.Create(Id + 10, "ImpKnowCelebrityDead", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Celebrity]);
        NeutralKnowCelebrityDead = BooleanOptionItem.Create(Id + 11, "NeutralKnowCelebrityDead", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Celebrity]);
        CovenKnowCelebrityDead = BooleanOptionItem.Create(Id + 12, "CovenKnowCelebrityDead", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Celebrity]);
    }
    public override void Init()
    {
        CelebrityDead.Clear();
    }
    public override bool GlobalKillFlashCheck(PlayerControl killer, PlayerControl target, PlayerControl seer)
    {
        // if Celebrity killed and seer is Celebrity, return true for show kill flash
        if (target.PlayerId == _Player.PlayerId && seer.PlayerId == _Player.PlayerId) return true;

        // Hide kill flash for some team
        if (!ImpKnowCelebrityDead.GetBool() && seer.GetCustomRole().IsImpostor()) return false;
        if (!NeutralKnowCelebrityDead.GetBool() && seer.GetCustomRole().IsNeutral()) return false;
        if (!CovenKnowCelebrityDead.GetBool() && seer.GetCustomRole().IsCoven()) return false;

        seer.Notify(ColorString(GetRoleColor(CustomRoles.Celebrity), GetString("OnCelebrityDead")));
        return true;
    }
    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (isSuicide && target.IsDisconnected()) return;

        if (inMeeting)
        {
            //Death Message
            foreach (var pc in Main.AllPlayerControls)
            {
                if (!ImpKnowCelebrityDead.GetBool() && pc.IsPlayerImpostorTeam()) continue;
                if (!NeutralKnowCelebrityDead.GetBool() && pc.IsPlayerNeutralTeam()) continue;
                if (!CovenKnowCelebrityDead.GetBool() && pc.IsPlayerCovenTeam()) continue;

                SendMessage(string.Format(GetString("CelebrityDead"), target.GetRealName()), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Celebrity), GetString("Celebrity").ToUpper()));
            }
        }
        else
        {
            if (!CelebrityDead.Contains(target.PlayerId))
                CelebrityDead.Add(target.PlayerId);
        }
    }
    public override void OnOthersMeetingHudStart(PlayerControl targets)
    {
        foreach (var csId in CelebrityDead)
        {
            if (!ImpKnowCelebrityDead.GetBool() && targets.IsPlayerImpostorTeam()) continue;
            if (!NeutralKnowCelebrityDead.GetBool() && targets.IsPlayerNeutralTeam()) continue;
            if (!CovenKnowCelebrityDead.GetBool() && targets.IsPlayerCovenTeam()) continue;
            AddMsg(string.Format(GetString("CelebrityDead"), Main.AllPlayerNames[csId]), targets.PlayerId, ColorString(GetRoleColor(CustomRoles.Celebrity), GetString("Celebrity").ToUpper()));
        }
    }
    public override void MeetingHudClear()
    {
        CelebrityDead.Clear();
    }
}
