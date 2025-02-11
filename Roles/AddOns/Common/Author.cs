using static TOHE.Options;
namespace TOHE.Roles.AddOns.Common;
internal class Author : IAddon
{
    public CustomRoles Role => CustomRoles.Author;
    private const int Id = 33900;
    public AddonTypes Type => AddonTypes.Helpful;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Author, canSetNum: true, teamSpawnOptions: true);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}
