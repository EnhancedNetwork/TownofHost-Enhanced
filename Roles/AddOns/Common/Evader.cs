
namespace TOHE.Roles.AddOns.Common;

public class Evader : IAddon
{
    private const int Id = 29600;
    public AddonTypes Type => AddonTypes.Helpful;

    private static OptionItem ChanceNotExiled;

    public void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Evader, canSetNum: true, teamSpawnOptions: true);
        ChanceNotExiled = IntegerOptionItem.Create(Id + 10, "Evader_ChanceNotExiled", new(0, 100, 5), 25, TabGroup.Addons, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Evader])
            .SetValueFormat(OptionFormat.Percent);
    }

    public static void CheckExile(ref int VoteNum)
    {
        if (IRandom.Instance.Next(1, 100) < ChanceNotExiled.GetInt())
        {
            VoteNum = 0;
        }
    }
}
