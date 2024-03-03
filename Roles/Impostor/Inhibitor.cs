
namespace TOHE.Roles.Impostor;

internal class Inhibitor : RoleBase
{
    private const int Id = 1600;
    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    private static OptionItem InhibitorCD;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Inhibitor);
        InhibitorCD = FloatOptionItem.Create(Id + 2, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Inhibitor])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = InhibitorCD.GetFloat();

    public override bool CanUseKillButton(PlayerControl pc) => !Utils.AnySabotageIsActive();
}