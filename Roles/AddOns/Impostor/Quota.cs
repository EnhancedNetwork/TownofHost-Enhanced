using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;
public class Quota : IAddon
{
    public CustomRoles Role => CustomRoles.Quota;
    private const int Id = 35100;
    public AddonTypes Type => AddonTypes.Impostor;

    private static OptionItem AmountKillsNeededToWin;
    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Quota, canSetNum: true, tab: TabGroup.Addons);
        AmountKillsNeededToWin = IntegerOptionItem.Create(Id + 10, "AmountKillsNeededToWin351", (1, 5, 1), 3, TabGroup.Addons, false)
            .SetValueFormat(OptionFormat.Times)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Quota]);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    public static bool CheckWinState(PlayerControl pc)
    {
        if (pc == null) return false;
        if (!pc.Is(CustomRoles.Quota)) return true;
        if (!pc.HasKillButton()) return true;

        if (Main.PlayerStates[pc.PlayerId].GetKillCount() >= AmountKillsNeededToWin.GetInt()) return true;

        foreach (var role in pc.GetCustomSubRoles())
        {
            if (!role.IsConverted()) continue;
            return true;
        }
        return false;
    }
}
