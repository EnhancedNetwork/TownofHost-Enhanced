//namespace TOHE.Roles.Crewmate;


// Unused role
/*
public static class ChiefOfPolice
{
    private const int Id = 12600;
    private static List<byte> playerIdList = [];
    public static Dictionary<byte, int> PoliceLimit = [];
    public static bool IsEnable = false;

    private static OptionItem SkillCooldown;
    private static OptionItem CanImpostorAndNeutarl;

    public override void SetupCustomOptions()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.ChiefOfPolice);
        SkillCooldown = FloatOptionItem.Create(Id + 10, "ChiefOfPoliceSkillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.ChiefOfPolice])
            .SetValueFormat(OptionFormat.Seconds);
        CanImpostorAndNeutarl = BooleanOptionItem.Create(Id + 16, "PolicCanImpostorAndNeutarl", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.ChiefOfPolice]);
    }
    public static void Init()
    {
        playerIdList = [];
        PoliceLimit = [];
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        PoliceLimit.TryAdd(playerId, 1);
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.ChiefOfPolice);
        writer.Write(playerId);
        writer.Write(PoliceLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (PoliceLimit.ContainsKey(PlayerId))
            PoliceLimit[PlayerId] = Limit;
        else
            PoliceLimit.Add(PlayerId, 1);
    }
    public static bool CanUseKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && (PoliceLimit.TryGetValue(playerId, out var x) ? x : 1) >= 1;

    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? SkillCooldown.GetFloat() : 300f;

    public static string GetSkillLimit(byte playerId) => Utils.ColorString(CanUseKillButton(playerId) ? Utils.GetRoleColor(CustomRoles.ChiefOfPolice) : Color.gray, PoliceLimit.TryGetValue(playerId, out var policeLimit) ? $"({policeLimit})" : "Invalid");
    
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        PoliceLimit[killer.PlayerId]--;
        SendRPC(killer.PlayerId);
        if (CanBeSheriff(target))
        {
            target.RpcSetCustomRole(CustomRoles.Sheriff);
            var targetId = target.PlayerId;
            foreach (var player in Main.AllAlivePlayerControls)
            {
                if (player.PlayerId == targetId)
                {
                   // Sheriff.Add(player.PlayerId);
                  // Sheriff.Add(player.PlayerId);
                }
            }
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Sheriff), GetString("SheriffSuccessfullyRecruited")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Sheriff), GetString("BeSheriffByPolice")));

            killer.ResetKillCooldown();
            target.ResetKillCooldown();
            killer.RpcGuardAndKill(target);
            killer.SetKillCooldown(forceAnime: true);
            target.RpcGuardAndKill(killer);
            target.SetKillCooldown(forceAnime: true);
        }
        else
        {
            //if (ChiefOfPoliceCountMode.GetInt() == 1)
            //{
            //    killer.RpcMurderPlayer(killer);
            //    return true;
            //}
            //if (ChiefOfPoliceCountMode.GetInt() == 2)
            //{
            //    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Sheriff), GetString("NotSheriff!!!")));
            //    return true;
            //}
        }
        return false;
    }
    public static bool CanBeSheriff(this PlayerControl pc)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate() && pc.CanUseKillButton()) || pc.GetCustomRole().IsNeutral() && pc.CanUseKillButton() && CanImpostorAndNeutarl.GetBool()|| pc.GetCustomRole().IsImpostor() && CanImpostorAndNeutarl.GetBool();
    }

    public static void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("ChiefOfPoliceKillButtonText"));
    }
}
*/