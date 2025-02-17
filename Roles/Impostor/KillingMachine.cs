using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class KillingMachine : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.KillingMachine;
    private const int Id = 23800;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem MNKillCooldown;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.KillingMachine);
        MNKillCooldown = FloatOptionItem.Create(Id + 5, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 10f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.KillingMachine])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override bool CanUseImpostorVentButton(PlayerControl pc) => false;
    public override bool CanUseSabotage(PlayerControl pc) => false;
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = MNKillCooldown.GetFloat();

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetVision(false);
        opt.SetFloat(FloatOptionNames.CrewLightMod, 0.2f);
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0.2f);
    }

    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
        => !reporter.Is(CustomRoles.KillingMachine);

    public override bool OnCheckStartMeeting(PlayerControl reporter)
        => !reporter.Is(CustomRoles.KillingMachine);

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        killer.RpcMurderPlayer(target);
        killer.ResetKillCooldown();
        return false;
    }
}
