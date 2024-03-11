
namespace TOHE.Roles.Impostor;

internal class Refugee : RoleBase
{
    private const int Id = 60009;
    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    private static OptionItem RefugeeKillCD;

    public static void SetupCustomOption()
    {
        RefugeeKillCD = FloatOptionItem.Create(Id, "RefugeeKillCD", new(0f, 180f, 2.5f), 22.5f, TabGroup.ImpostorRoles, false)
            .SetHeader(true)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
    }
    public override void Init()
    {
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = RefugeeKillCD.GetFloat();

    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;
    public override bool CanUseSabotage(PlayerControl pc) => true;
}
