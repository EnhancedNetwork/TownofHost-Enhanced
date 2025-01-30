namespace TOHE.Roles.Impostor;

internal class Inhibitor : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Inhibitor;
    private const int Id = 1600;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem InhibitorCD;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Inhibitor);
        InhibitorCD = FloatOptionItem.Create(Id + 2, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Inhibitor])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = InhibitorCD.GetFloat();

    public override bool CanUseKillButton(PlayerControl pc)
        => !Saboteur.IsCriticalSabotage();
}
