﻿using Hazel;

namespace TOHE.Roles.Impostor;

// Thanks： https://github.com/Yumenopai/TownOfHost_Y
internal class Greedy : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 1500;
    public static HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem OddKillCooldown;
    private static OptionItem EvenKillCooldown;

    private static readonly Dictionary<byte, bool> IsOdd = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Greedy);
        OddKillCooldown = FloatOptionItem.Create(Id + 10, "GreedyOddKillCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Greedy])
            .SetValueFormat(OptionFormat.Seconds);
        EvenKillCooldown = FloatOptionItem.Create(Id + 11, "GreedyEvenKillCooldown", new(0f, 180f, 2.5f), 5f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Greedy])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        playerIdList.Clear();
        IsOdd.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsOdd.Add(playerId, true);
    }

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetGreedy, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(IsOdd[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        IsOdd[playerId] = reader.ReadBoolean();
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = OddKillCooldown.GetFloat();
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        foreach (var greedyId in playerIdList.ToArray())
        {
            IsOdd[greedyId] = true;
            SendRPC(greedyId);
            SetKillCooldown(greedyId);
        }
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        switch (IsOdd[killer.PlayerId])
        {
            case true:
                Logger.Info($"{killer?.Data?.PlayerName}: Odd kill cooldown", "Greedier");
                Main.AllPlayerKillCooldown[killer.PlayerId] = EvenKillCooldown.GetFloat();
                break;
            case false:
                Logger.Info($"{killer?.Data?.PlayerName}: Even kill cooldown", "Greedier");
                Main.AllPlayerKillCooldown[killer.PlayerId] = OddKillCooldown.GetFloat();
                break;
        }

        IsOdd[killer.PlayerId] = !IsOdd[killer.PlayerId];
        SendRPC(killer.PlayerId);

        killer.SyncSettings();
        return true;
    }
}