using Hazel;
using System.Collections.Generic;
using System.Linq;

namespace TOHE;

// 来源：https://github.com/Yumenopai/TownOfHost_Y
public static class Greedier
{
    private static readonly int Id = 1500;
    public static List<byte> playerIdList = [];
    public static bool IsEnable = false;

    private static OptionItem OddKillCooldown;
    private static OptionItem EvenKillCooldown;

    public static Dictionary<byte, bool> IsOdd = [];

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Greedier);
        OddKillCooldown = FloatOptionItem.Create(Id + 10, "OddKillCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Greedier])
            .SetValueFormat(OptionFormat.Seconds);
        EvenKillCooldown = FloatOptionItem.Create(Id + 11, "EvenKillCooldown", new(0f, 180f, 2.5f), 5f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Greedier])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = [];
        IsOdd = [];
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsOdd.Add(playerId, true);
        IsEnable = true;
    }

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetGreedierOE, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(IsOdd[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        IsOdd[playerId] = reader.ReadBoolean();
    }

    public static void SetKillCooldown(byte id)
    {
        Main.AllPlayerKillCooldown[id] = OddKillCooldown.GetFloat();
    }
    public static void OnReportDeadBody()
    {
        foreach (var pc in Main.AllAlivePlayerControls.Where(x => playerIdList.Contains(x.PlayerId)).ToArray())
        {
            IsOdd[pc.PlayerId] = true;
            SendRPC(pc.PlayerId);
            Main.AllPlayerKillCooldown[pc.PlayerId] = OddKillCooldown.GetFloat();
        }
    }
    public static void OnCheckMurder(PlayerControl killer)
    {
        switch (IsOdd[killer.PlayerId])
        {
            case true:
                Logger.Info($"{killer?.Data?.PlayerName}:奇数击杀冷却", "Greedier");
                Main.AllPlayerKillCooldown[killer.PlayerId] = EvenKillCooldown.GetFloat();
                break;
            case false:
                Logger.Info($"{killer?.Data?.PlayerName}:偶数击杀冷却", "Greedier");
                Main.AllPlayerKillCooldown[killer.PlayerId] = OddKillCooldown.GetFloat();
                break;
        }
        IsOdd[killer.PlayerId] = !IsOdd[killer.PlayerId];
        //RPCによる同期
        SendRPC(killer.PlayerId);
        killer.SyncSettings();//キルクール処理を同期
    }
}