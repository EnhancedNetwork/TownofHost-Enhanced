namespace TOHE.Roles.Impostor;

internal class Underdog : RoleBase
{
    private const int Id = 2700;
    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    private static OptionItem UnderdogMaximumPlayersNeededToKill;
    private static OptionItem UnderdogKillCooldown;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(2700, TabGroup.ImpostorRoles, CustomRoles.Underdog);
        UnderdogMaximumPlayersNeededToKill = IntegerOptionItem.Create(Id + 2, "UnderdogMaximumPlayersNeededToKill", new(1, 15, 1), 5, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Underdog])
            .SetValueFormat(OptionFormat.Players);
        UnderdogKillCooldown = FloatOptionItem.Create(Id + 3, "KillCooldown", new(0f, 180f, 2.5f), 12.5f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Underdog])
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

    public override bool CanUseKillButton(PlayerControl pc) => Main.AllAlivePlayerControls.Length <= UnderdogMaximumPlayersNeededToKill.GetInt();

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = UnderdogKillCooldown.GetFloat();
}
