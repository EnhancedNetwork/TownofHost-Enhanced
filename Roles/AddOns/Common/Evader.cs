
namespace TOHE.Roles.AddOns.Common;

public class Evader : IAddon
{
    public CustomRoles Role => CustomRoles.Evader;
    private const int Id = 29600;
    public AddonTypes Type => AddonTypes.Helpful;

    private static OptionItem SkillLimitTimes;
    private static OptionItem ChanceNotExiled;

    private static readonly Dictionary<byte, bool> AlredyCheck = [];
    private static readonly Dictionary<byte, int> SkillLimit = [];
    private static int RememberRandomForExile;

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
    public void Init()
    {
        AlredyCheck.Clear();
        SkillLimit.Clear();
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    {
        AlredyCheck[playerId] = false;
        SkillLimit[playerId] = SkillLimitTimes.GetInt();
    }
    public void Remove(byte playerId)
    {
        AlredyCheck.Remove(playerId);
        SkillLimit.Remove(playerId);
    }
    public static void ReportDeadBody()
    {
        if (AlredyCheck.Any())
        {
            foreach (var evaderId in AlredyCheck.Keys)
            {
                AlredyCheck[evaderId] = false;
            }
        }
    }
    public static void RememberRandom()
    {
        RememberRandomForExile = IRandom.Instance.Next(1, 100);
    }
    public static void CheckExile(byte evaderId, ref int VoteNum)
    {
        if (AlredyCheck[evaderId] && RememberRandomForExile < ChanceNotExiled.GetInt())
        {
            VoteNum = 0;
            return;
        }
        if (SkillLimit[evaderId] <= 0) return;

        if (RememberRandomForExile < ChanceNotExiled.GetInt())
        {
            SkillLimit[evaderId]--;
            AlredyCheck[evaderId] = true;
            VoteNum = 0;
        }
    }
}
