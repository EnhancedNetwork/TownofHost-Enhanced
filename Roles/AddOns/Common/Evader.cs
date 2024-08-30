
namespace TOHE.Roles.AddOns.Common;

public class Evader : IAddon
{
    private const int Id = 29600;
    public AddonTypes Type => AddonTypes.Helpful;

    private static OptionItem SkillLimitTimes;
    private static OptionItem ChanceNotExiled;

    private static readonly Dictionary<byte, int> SkillLimit = [];

    public void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Evader, canSetNum: true, teamSpawnOptions: true);
        SkillLimitTimes = IntegerOptionItem.Create(Id + 10, "SkillLimitTimes", new(0, 10, 1), 2, TabGroup.Addons, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Evader])
            .SetValueFormat(OptionFormat.Times);
        ChanceNotExiled = IntegerOptionItem.Create(Id + 11, "Evader_ChanceNotExiled", new(0, 100, 5), 25, TabGroup.Addons, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Evader])
            .SetValueFormat(OptionFormat.Percent);
    }
    public static void Init()
    {
        SkillLimit.Clear();
    }
    public static void Add(byte playerId)
    {
        SkillLimit[playerId] = SkillLimitTimes.GetInt();
    }
    public static void CheckExile(byte evaderId, ref int VoteNum)
    {
        if (SkillLimit[evaderId] <= 0) return;

        if (IRandom.Instance.Next(1, 100) < ChanceNotExiled.GetInt())
        {
            SkillLimit[evaderId]--;
            VoteNum = 0;
        }
    }
}
