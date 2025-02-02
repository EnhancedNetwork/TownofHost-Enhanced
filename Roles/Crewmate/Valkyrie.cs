using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class Valkyrie : RoleBase
{

    private const int Id = 31100;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    public override CustomRoles Role => CustomRoles.Valkyrie;

    public static OptionItem GhostKillCooldown;


    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Valkyrie);
        GhostKillCooldown = FloatOptionItem.Create(Id + 11, "ValkyrieRevengeTime", new(5f, 25f, 2.5f), 15f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Valkyrie])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        killer.RpcMurderPlayer(target);
        killer.SetKillCooldown();
        target.RpcChangeRoleBasis(CustomRoles.ValkyrieB);
        target.RpcSetCustomRole(CustomRoles.ValkyrieB, true);
        return false;
    }
}

internal class ValkyrieB : RoleBase
{
    public override CustomRoles Role => CustomRoles.ValkyrieB;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.GuardianAngelCooldown = Valkyrie.GhostKillCooldown.GetFloat();
        AURoleOptions.ProtectionDurationSeconds = 0f;
    }
    public override bool OnCheckProtect(PlayerControl killer, PlayerControl target)
    {
        killer.RpcMurderPlayer(target);
        killer.RpcRevive();
        killer.RpcChangeRoleBasis(CustomRoles.Crewmate);
        killer.RpcSetCustomRole(CustomRoles.Crewmate, true);
        return false;
    }
}
