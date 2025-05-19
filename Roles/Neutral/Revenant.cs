using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral;
internal class Revenant : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Revenant;
    private const int Id = 30200;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Revenant);

    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    // private static OptionItem RevenantCanCopyAddons;
    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Revenant);
        //RevenantCanCopyAddons = BooleanOptionItem.Create(Id + 10, "RevenantCanCopyAddons", false, TabGroup.NeutralRoles, false)
        //   .SetParent(CustomRoleSpawnChances[CustomRoles.Revenant]);
    }

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        CustomRoles role = killer.GetCustomRole();
        if (role.IsTNA()) return false;

        killer.RpcMurderPlayer(killer);
        killer.SetRealKiller(target);

        target.RpcChangeRoleBasis(role);
        target.RpcSetCustomRole(role);
        target.GetRoleClass()?.OnAdd(target.PlayerId);
        if (killer.Is(CustomRoles.Narc)) target.RpcSetCustomRole(CustomRoles.Narc);
        if (killer.Is(CustomRoles.Rebel)) target.RpcSetCustomRole(CustomRoles.Rebel);

        target.Notify(string.Format(GetString("RevenantTargeted"), GetRoleName(role)));

        return false;
    }
}
