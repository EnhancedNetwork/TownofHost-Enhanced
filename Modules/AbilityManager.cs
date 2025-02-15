namespace TOHE;
public static class AbilityManager
{
    public static List<byte> PermShields = [];
    public static List<byte> SingleShields = []; 
    public static List<byte> RoundShields = [];
    public static void AddPermanentShield(PlayerControl shielder, PlayerControl shielded)
    {
        shielder.RpcGuardAndKill(shielder);
        PermShields.Add(shielded.PlayerId);
    }
    public static void AddSingleUseShield(PlayerControl shielder, PlayerControl shielded)
    {
        shielder.RpcGuardAndKill(shielder);
        SingleShields.Add(shielded.PlayerId);
    }
    public static void AddSingleRoundShield(PlayerControl shielder, PlayerControl shielded)
    {
        shielder.RpcGuardAndKill(shielder);
        RoundShields.Add(shielded.PlayerId);
    }
    public static void AfterMeetingTasks()
    {
        if (RoundShields.Any())
        {
            RoundShields.Clear();
        }
    }

    public static bool OnMurderPlayer(PlayerControl killer, PlayerControl target)
    {
        if (RoundShields != null)
        {
            foreach (var playerid in RoundShields)
            {
                var player = Utils.GetPlayerById(playerid);
                if (player == target)
                {
                    RoundShields.Remove(playerid);
                    killer.RpcGuardAndKill(killer);
                    return false;
                }
            }
        }
        if (SingleShields != null)
        {
            foreach (var playerid in SingleShields)
            {
                var player = Utils.GetPlayerById(playerid);
                if (player == target)
                {
                    SingleShields.Remove(playerid);
                    killer.RpcGuardAndKill(killer);
                    return false;
                }
            }
        }
        if (PermShields != null)
        {
            foreach (var playerid in PermShields)
            {
                var player = Utils.GetPlayerById(playerid);
                if (player == target)
                {
                    killer.RpcGuardAndKill(killer);
                    return false;
                }
            }
        }
        return true;
    }
}
