
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

    private static OptionItem RefugeeKillCD;

    public override void SetupCustomOption()
    {
        RefugeeKillCD = FloatOptionItem.Create(Id, "RefugeeKillCD", new(0f, 180f, 2.5f), 22.5f, TabGroup.ImpostorRoles, false)
            .SetHeader(true)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(true);
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = RefugeeKillCD.GetFloat();

    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;
    public override bool CanUseSabotage(PlayerControl pc) => true;
}
