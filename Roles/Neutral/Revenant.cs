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

        if (killer.IsAnySubRole(x => x.IsBetrayalAddonV2()))
        {
            foreach (var subrole in killer.GetCustomSubRoles().Where(x => x.IsBetrayalAddonV2()))
            {
                role = subrole switch
                {
                    CustomRoles.Madmate => CustomRoles.Gangster,
                    CustomRoles.Charmed => CustomRoles.Cultist,
                    CustomRoles.Recruit => CustomRoles.Sidekick,
                    CustomRoles.Infected => CustomRoles.Infectious,
                    CustomRoles.Contagious => CustomRoles.Virus,
                    CustomRoles.Admired => CustomRoles.Admirer,
                    CustomRoles.Enchanted => CustomRoles.Ritualist,
                    CustomRoles.Egoist => CustomRoles.Traitor,
                    CustomRoles.Rebel => CustomRoles.Taskinator,
                    _ => role
                };
            }
        }

        killer.RpcMurderPlayer(killer);
        killer.SetRealKiller(target);

        target.RpcChangeRoleBasis(role);
        target.RpcSetCustomRole(role);
        target.GetRoleClass()?.OnAdd(target.PlayerId);

        target.Notify(string.Format(GetString("RevenantTargeted"), GetRoleName(role)));

        return false;
    }
}
