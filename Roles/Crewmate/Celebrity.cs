﻿using static TOHE.MeetingHudStartPatch;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Celebrity : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 6500;



    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    //==================================================================\\

    private static OptionItem ImpKnowCelebrityDead;
    private static OptionItem NeutralKnowCelebrityDead;

    private static readonly HashSet<byte> CelebrityDead = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Celebrity);
        ImpKnowCelebrityDead = BooleanOptionItem.Create(Id + 10, "ImpKnowCelebrityDead", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Celebrity]);
        NeutralKnowCelebrityDead = BooleanOptionItem.Create(Id + 11, "NeutralKnowCelebrityDead", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Celebrity]);

    }
    public override void Init()
    {

        CelebrityDead.Clear();
    }
    public override void Add(byte playerId)
    {

    }
    public override bool GlobalKillFlashCheck(PlayerControl killer, PlayerControl target, PlayerControl seer)
    {
        // if Celebrity killed and seer is Celebrity, return true for show kill flash
        if (target.PlayerId == _Player.PlayerId && seer.PlayerId == _Player.PlayerId) return true;

        // Hide kill flash for some team
        if (!ImpKnowCelebrityDead.GetBool() && seer.GetCustomRole().IsImpostorV4Narc()) return false;
        if (!NeutralKnowCelebrityDead.GetBool() && seer.GetCustomRole().IsNeutralV4Narc()) return false;

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
                if (!ImpKnowCelebrityDead.GetBool() && pc.GetCustomRole().IsImpostorV4Narc()) continue;
                if (!NeutralKnowCelebrityDead.GetBool() && pc.GetCustomRole().IsNeutralV4Narc()) continue;

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
            if (!ImpKnowCelebrityDead.GetBool() && (targets.GetCustomRole().IsImpostor() && !targets.Is(CustomRoles.Narc))) continue;
            if (!NeutralKnowCelebrityDead.GetBool() && targets.GetCustomRole().IsNeutral()) continue;
            AddMsg(string.Format(GetString("CelebrityDead"), Main.AllPlayerNames[csId]), targets.PlayerId, ColorString(GetRoleColor(CustomRoles.Celebrity), GetString("CelebrityNewsTitle")));
        }
    }
    public override void MeetingHudClear()
    {
        CelebrityDead.Clear();
    }
}
