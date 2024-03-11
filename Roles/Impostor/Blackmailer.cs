using AmongUs.GameOptions;
using System.Collections.Generic;
using TOHE.Roles.Core;
using TOHE.Roles.Neutral;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Blackmailer : RoleBase
{
    private const int Id = 24600;
    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;

    private static OptionItem SkillCooldown;

    private static List<byte> ForBlackmailer = [];

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Blackmailer);
        SkillCooldown = FloatOptionItem.Create(Id + 2, "BlackmailerSkillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Blackmailer])
           .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        ForBlackmailer = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;

        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        }
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = SkillCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public override void OnShapeshift(PlayerControl blackmailer, PlayerControl target, bool shapeshifting, bool shapeshiftIsHidden)
    {
        if (!shapeshifting && !shapeshiftIsHidden) return;

        if (!target.IsAlive())
        {
            blackmailer.Notify(Utils.ColorString(Utils.GetRoleColor(blackmailer.GetCustomRole()), GetString("NotAssassin")));
            return;
        }

        ForBlackmailer.Add(target.PlayerId);
        blackmailer.Notify(GetString("RejectShapeshift.AbilityWasUsed"), time: 2f);
    }

    public override void AfterMeetingTasks()
    {
        ClearBlackmaile();
    }
    public override void OnCoEndGame()
    {
        ClearBlackmaile();
    }

    private static void ClearBlackmaile() => ForBlackmailer.Clear();
    public static bool CheckBlackmaile(PlayerControl player) => On && ForBlackmailer.Contains(player.PlayerId);

    private string GetMarkOthers(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
    {
        if (!isForMeeting) return string.Empty;
        
        target ??= seer;
        return CheckBlackmaile(target) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Blackmailer), "╳") : string.Empty;
    }
    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (CheckBlackmaile(pc))
        {
            var playername = pc.GetRealName();
            if (Doppelganger.DoppelVictim.ContainsKey(pc.PlayerId)) playername = Doppelganger.DoppelVictim[pc.PlayerId];
            AddMsg(string.Format(GetString("BlackmailerDead"), playername, pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Blackmailer), GetString("BlackmaileKillTitle"))));
        }
    }
}