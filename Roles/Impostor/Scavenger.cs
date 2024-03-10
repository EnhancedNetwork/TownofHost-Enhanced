
namespace TOHE.Roles.Impostor;

internal class Scavenger : RoleBase
{
    private const int Id = 4400;
    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    private static OptionItem ScavengerKillCooldown;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Scavenger);
        ScavengerKillCooldown = FloatOptionItem.Create(Id + 2, "KillCooldown", new(5f, 180f, 2.5f), 40f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Scavenger])
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

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = ScavengerKillCooldown.GetFloat();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        target.RpcTeleport(ExtendedPlayerControl.GetBlackRoomPosition());

        _ = new LateTask(
            () =>
            {
                target.SetRealKiller(killer);
                target.RpcMurderPlayerV3(target);
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), Translator.GetString("KilledByScavenger")), time: 8f);
            },
            0.5f, "Scavenger Kill");
        
        killer.SetKillCooldown();
        RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
        
        return false;
    }

    public override bool OnCheckReportDeadBody(PlayerControl reporter, GameData.PlayerInfo deadBody, PlayerControl killer)
        => !killer.Is(CustomRoles.Scavenger);
}
