using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;
using static TOHE.MeetingHudStartPatch;

namespace TOHE.Roles.Crewmate;

internal class Celebrity : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 6500;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    //==================================================================\\

    private static OptionItem ImpKnowCelebrityDead;
    private static OptionItem NeutralKnowCelebrityDead;

    private static readonly HashSet<byte> CelebrityDead = [];

    public static void SetupCustomOptions()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Celebrity);
        ImpKnowCelebrityDead = BooleanOptionItem.Create(Id + 10, "ImpKnowCelebrityDead", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Celebrity]);
        NeutralKnowCelebrityDead = BooleanOptionItem.Create(Id + 11, "NeutralKnowCelebrityDead", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Celebrity]);

    }
    public override void Init()
    {
        playerIdList.Clear();
        CelebrityDead.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public override bool KillFlashCheck(PlayerControl killer, PlayerControl target, PlayerControl seer)
    {
        if (!ImpKnowCelebrityDead.GetBool() && seer.GetCustomRole().IsImpostor()) return false;
        if (!NeutralKnowCelebrityDead.GetBool() && seer.GetCustomRole().IsNeutral()) return false;

        seer.Notify(ColorString(GetRoleColor(CustomRoles.Celebrity), GetString("OnCelebrityDead")));
        return true;
    }
    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (inMeeting)
        {
            //Death Message
            foreach (var pc in Main.AllPlayerControls)
            {
                if (!ImpKnowCelebrityDead.GetBool() && pc.GetCustomRole().IsImpostor()) continue;
                if (!NeutralKnowCelebrityDead.GetBool() && pc.GetCustomRole().IsNeutral()) continue;

                SendMessage(string.Format(GetString("CelebrityDead"), target.GetRealName()), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Celebrity), GetString("CelebrityNewsTitle")));
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
            if (!ImpKnowCelebrityDead.GetBool() && targets.GetCustomRole().IsImpostor()) continue;
            if (!NeutralKnowCelebrityDead.GetBool() && targets.GetCustomRole().IsNeutral()) continue;
            AddMsg(string.Format(GetString("CelebrityDead"), Main.AllPlayerNames[csId]), targets.PlayerId, ColorString(GetRoleColor(CustomRoles.Celebrity), GetString("CelebrityNewsTitle")));
        }
    }
    public override void MeetingHudClear()
    {
        CelebrityDead.Clear();
    }
}
