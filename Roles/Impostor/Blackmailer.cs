using AmongUs.GameOptions;
using System.Collections.Generic;

namespace TOHE.Roles.Impostor;

internal class Blackmailer : RoleBase
{
    private const int Id = 24600;
    private static List<byte> playerIdList = [];
    public static bool On;
    public override bool IsEnable => On;

    private static OptionItem SkillCooldown;
    //private static OptionItem BlackmailerMax;

    private static List<byte> ForBlackmailer = [];
    //private static Dictionary<byte, int> BlackmailerMaxUp = [];

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Blackmailer);
        SkillCooldown = FloatOptionItem.Create(Id + 42, "BlackmailerSkillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Blackmailer])
           .SetValueFormat(OptionFormat.Seconds);
        //BlackmailerMax = FloatOptionItem.Create(Id + 43, "BlackmailerMax", new(2.5f, 900f, 2.5f), 20f, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Blackmailer])
        //    .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        //BlackmailerMaxUp = [];
        ForBlackmailer = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
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
            blackmailer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), Translator.GetString("NotAssassin")));
            return;
        }

        ForBlackmailer.Add(target.PlayerId);
        blackmailer.Notify(Translator.GetString("RejectShapeshift.AbilityWasUsed"), time: 2f);
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
}