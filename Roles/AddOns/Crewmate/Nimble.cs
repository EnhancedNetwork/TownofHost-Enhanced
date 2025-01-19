using static TOHE.Options;

namespace TOHE.Roles.AddOns.Crewmate;

public class Nimble : IAddon
{
    public CustomRoles Role => CustomRoles.Nimble;
    private const int Id = 19700;
    public AddonTypes Type => AddonTypes.Helpful;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Nimble, canSetNum: true, tab: TabGroup.Addons);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}
