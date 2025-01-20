
using AmongUs.GameOptions;

namespace TOHE.Roles.Impostor;

internal class Refugee : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Refugee;
    private const int Id = 60009;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.Madmate;
    //==================================================================\\

    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(true);

    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;
    public override bool CanUseSabotage(PlayerControl pc) => true;
}
