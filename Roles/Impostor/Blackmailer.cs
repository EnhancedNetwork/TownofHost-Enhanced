using System.Collections.Generic;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

public static class Blackmailer
{
    private static readonly int Id = 1658974;
    private static List<byte> playerIdList = new();
    public static OptionItem SkillCooldown;
    //public static OptionItem BlackmailerMax;
    public static Dictionary<byte, int> BlackmailerMaxUp;
    public static List<byte> ForBlackmailer = new ();
    public static bool IsEnable = false;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Blackmailer);
        SkillCooldown = FloatOptionItem.Create(Id + 42, "BlackmailerSkillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Blackmailer])
           .SetValueFormat(OptionFormat.Seconds);
        //BlackmailerMax = FloatOptionItem.Create(Id + 43, "BlackmailerMax", new(2.5f, 900f, 2.5f), 20f, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Blackmailer])
        //    .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
        BlackmailerMaxUp = new();
        ForBlackmailer = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;
    }
    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = SkillCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }
}