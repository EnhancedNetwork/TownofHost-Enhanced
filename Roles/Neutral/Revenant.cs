using TOHE.Roles.Core;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;
internal class Revenant : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 30100;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Revenant);

    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    // private static OptionItem RevenantCanCopyAddons;
    public override void SetupCustomOption()
    {   
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Revenant);
        //RevenantCanCopyAddons = BooleanOptionItem.Create(Id + 10, "RevenantCanCopyAddons", false, TabGroup.NeutralRoles, false)
         //   .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Revenant]);
    }

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        CustomRoles role = killer.GetCustomRole();

        killer.RpcMurderPlayer(killer);
        killer.SetRealKiller(target);

        target.RpcChangeRoleBasis(role);
        target.RpcSetCustomRole(role);
        target.GetRoleClass()?.OnAdd(target.PlayerId);

        target.Notify(string.Format(GetString("RenevantTargeted"), Utils.GetRoleName(role)));

        return false;
    }
}
