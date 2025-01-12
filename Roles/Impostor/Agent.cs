namespace TOHE.Roles.Impostor;

internal class Agent : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 32600;
    private static readonly HashSet<byte> Playerids = [];
    public static bool HasEnabled => Playerids.Any();

    public override CustomRoles Role => CustomRoles.Agent;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem AgentKillCooldown;

    private static readonly HashSet<byte> BoobyTrapBody = [];
    private static readonly Dictionary<byte, byte> KillerOfBoobyTrapBody = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Agent);
        AgentKillCooldown = FloatOptionItem.Create(Id + 2, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Agent])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        BoobyTrapBody.Clear();
        KillerOfBoobyTrapBody.Clear();
        Playerids.Clear();
    }
    public override void Add(byte playerId)
    {
        Playerids.Clear();
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = AgentKillCooldown.GetFloat();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        BoobyTrapBody.Add(target.PlayerId);
        return true;
    }

    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        // if reporter try reported trap body
        if (BoobyTrapBody.Contains(deadBody.PlayerId) && reporter.IsAlive()
            && !reporter.Is(CustomRoles.Pestilence) && _Player.RpcCheckAndMurder(reporter, true))
        {
            reporter.RpcSetCustomRole(CustomRoles.Madmate);
            BoobyTrapBody.Remove(deadBody.PlayerId);
            return true;
        }

        return true;
    }
}
