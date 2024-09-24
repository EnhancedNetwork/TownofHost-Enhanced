namespace TOHE.Roles.Impostor;

internal class Scavenger : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 4400;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem ScavengerKillCooldown;

    public static readonly HashSet<byte> KilledPlayersId = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Scavenger);
        ScavengerKillCooldown = FloatOptionItem.Create(Id + 2, GeneralOption.KillCooldown, new(5f, 180f, 2.5f), 40f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Scavenger])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        PlayerIds.Clear();
        KilledPlayersId.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = ScavengerKillCooldown.GetFloat();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        target.RpcTeleport(ExtendedPlayerControl.GetBlackRoomPosition());
        KilledPlayersId.Add(target.PlayerId);

        _ = new LateTask(
            () =>
            {
                target.RpcMurderPlayer(target);
                target.SetRealKiller(killer);
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), Translator.GetString("KilledByScavenger")), time: 8f);
            },
            0.5f, "Scavenger Kill");
        
        killer.SetKillCooldown();
        return false;
    }

    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
        => !killer.Is(CustomRoles.Scavenger);
}
