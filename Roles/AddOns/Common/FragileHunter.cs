using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;
internal class FragileHunter : IAddon
{
    public CustomRoles Role => CustomRoles.FragileHunter;
    private const int Id = 33500;
    public AddonTypes Type => AddonTypes.Misc;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.FragileHunter, canSetNum: true);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}
