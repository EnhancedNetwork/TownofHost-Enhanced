using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class Saboteur : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Saboteur;
    private const int Id = 2300;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem SaboteurCD;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Saboteur);
        SaboteurCD = FloatOptionItem.Create(Id + 2, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Saboteur])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = SaboteurCD.GetFloat();

    public override bool CanUseKillButton(PlayerControl pc) => IsCriticalSabotage();

    public static bool IsCriticalSabotage()
        => IsActive(SystemTypes.Laboratory)
           || IsActive(SystemTypes.LifeSupp)
           || IsActive(SystemTypes.Reactor)
           || IsActive(SystemTypes.HeliSabotage);
}
