namespace TOHE.Roles.Impostor;

internal class Bard: RoleBase
{
    //===========================SETUP================================\\
    public static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\
    public override void Init()
    {
        PlayerIds.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
    }

    public static bool CheckSpawn()
    {
        var Rand = IRandom.Instance;
        return Rand.Next(0, 100) < Arrogance.BardChance.GetInt();
    }

    public override void OnPlayerExiled(PlayerControl bard, NetworkedPlayerInfo exiled)
    {
        if (exiled != null) Main.AllPlayerKillCooldown[bard.PlayerId] /= 2;
    }
}
