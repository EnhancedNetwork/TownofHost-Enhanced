namespace TOHE.Roles.Impostor;

class Bard
{
    public static void OnExileWrapUp(PlayerControl Bard, GameData.PlayerInfo exiled)
    {
        if (exiled != null) Main.AllPlayerKillCooldown[Bard.PlayerId] /= 2;
    }
}
