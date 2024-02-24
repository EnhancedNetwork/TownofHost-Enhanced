namespace TOHE.Roles.Impostor;

internal class Bard: RoleBase
{
    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    public override void Init()
    {
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
    }

    public static bool CheckSpawn()
    {
        var Rand = IRandom.Instance;
        return Rand.Next(0, 100) < Arrogance.BardChance.GetInt();
    }

    public override void OnPlayerExiled(PlayerControl Bard, GameData.PlayerInfo exiled)
    {
        if (exiled != null) Main.AllPlayerKillCooldown[Bard.PlayerId] /= 2;
    }
}
