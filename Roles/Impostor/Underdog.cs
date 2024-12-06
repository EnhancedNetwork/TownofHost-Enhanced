namespace TOHE.Roles.Impostor;

internal class Underdog : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 2700;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();

    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem UnderdogMaximumPlayersNeededToKill;
    private static OptionItem UnderdogKillCooldown;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(2700, TabGroup.ImpostorRoles, CustomRoles.Underdog);
        UnderdogMaximumPlayersNeededToKill = IntegerOptionItem.Create(Id + 2, "UnderdogMaximumPlayersNeededToKill", new(1, 15, 1), 5, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Underdog])
            .SetValueFormat(OptionFormat.Players);
        UnderdogKillCooldown = FloatOptionItem.Create(Id + 3, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 12.5f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Underdog])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        PlayerIds.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
    }

    public override bool CanUseKillButton(PlayerControl pc) => Main.AllAlivePlayerControls.Length <= UnderdogMaximumPlayersNeededToKill.GetInt();

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = UnderdogKillCooldown.GetFloat();
}
