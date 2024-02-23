namespace TOHE.Roles.Impostor;

internal class Bard: RoleBase
{
    public static bool On;
    public override bool IsEnable => On;

    public override void Add(byte playerId)
    {
        On = true;
    }

    public override void Init()
    {
        On = false;
    }

    public static bool CheckSpawn()
    {
        var Rand = IRandom.Instance;
        return Rand.Next(1, 101) <= Arrogance.BardChance.GetInt();
    }

    public override void OnPlayerExiled(PlayerControl Bard, GameData.PlayerInfo exiled)
    {
        if (exiled != null) Main.AllPlayerKillCooldown[Bard.PlayerId] /= 2;
    }
}
