using Hazel;
using System.Collections.Generic;
using UnityEngine;

namespace TOHE.Roles.Crewmate;

public static class SwordsMan
{
    private static readonly int Id = 10800;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static List<byte> killed = new();
    public static OptionItem CanVent;
    public static OptionItem KillCooldown;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.SwordsMan);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 60f, 2.5f), 15f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.SwordsMan])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.SwordsMan]);
    }
    public static void Init()
    {
        killed = new();
        playerIdList = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = IsKilled(id) ? 300f : KillCooldown.GetFloat();
    public static string GetKillLimit(byte id) => Utils.ColorString(!IsKilled(id) ? Utils.GetRoleColor(CustomRoles.SwordsMan).ShadeColor(0.25f) : Color.gray, !IsKilled(id) ? "(1)" : "(0)");
    public static bool CanUseKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && !IsKilled(playerId);
    public static bool IsKilled(byte playerId) => killed.Contains(playerId);

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SwordsManKill, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte SwordsManId = reader.ReadByte();
        if (!killed.Contains(SwordsManId))
            killed.Add(SwordsManId);
    }
    public static bool OnCheckMurder(PlayerControl killer) => CanUseKillButton(killer.PlayerId);
    public static void OnMurder(PlayerControl killer)
    {
        SendRPC(killer.PlayerId);
        killed.Add(killer.PlayerId);
        Logger.Info($"{killer.GetNameWithRole()} : " + (IsKilled(killer.PlayerId) ? "已使用击杀机会" : "未使用击杀机会"), "SwordsMan");
        SetKillCooldown(killer.PlayerId);
        Utils.NotifyRoles(SpecifySeer: killer);
    }
}