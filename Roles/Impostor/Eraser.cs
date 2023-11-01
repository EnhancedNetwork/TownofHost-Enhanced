using Hazel;
using System.Collections.Generic;
using TOHE.Roles.Crewmate;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal static class Eraser
{
    private static readonly int Id = 16800;
    private static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    private static OptionItem EraseLimitOpt;
    public static OptionItem HideVote;

    private static List<byte> didVote = new();
    public static Dictionary<byte, int> EraseLimit = new();
    private static List<byte> PlayerToErase = new();
    public static Dictionary<byte, int> TempEraseLimit = new();

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Eraser);
        EraseLimitOpt = IntegerOptionItem.Create(Id + 10, "EraseLimit", new(1, 15, 1), 2, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Eraser])
            .SetValueFormat(OptionFormat.Times);
        HideVote = BooleanOptionItem.Create(Id + 11, "EraserHideVote", false, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Eraser]);
    }
    public static void Init()
    {
        playerIdList = new();
        EraseLimit = new();
        PlayerToErase = new();
        didVote = new();
        TempEraseLimit = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        EraseLimit.Add(playerId, EraseLimitOpt.GetInt());
        IsEnable = true;

        Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 剩余{EraseLimit[playerId]}次", "Eraser");
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetEraseLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();

        EraseLimit[playerId]--;
    }
    public static string GetProgressText(byte playerId) => Utils.ColorString(EraseLimit[playerId] > 0 ? Utils.GetRoleColor(CustomRoles.Eraser) : Color.gray, EraseLimit.TryGetValue(playerId, out var x) ? $"({x})" : "Invalid");

    public static void OnVote(PlayerControl player, PlayerControl target)
    {
        if (!IsEnable) return;
        if (player == null || target == null) return;
        if (target.Is(CustomRoles.Eraser)) return;
        if (EraseLimit[player.PlayerId] <= 0) return;

        if (didVote.Contains(player.PlayerId)) return;
        didVote.Add(player.PlayerId);

        Logger.Info($"{player.GetCustomRole()} votes for {target.GetCustomRole()}", "Vote Eraser");

        if (target.PlayerId == player.PlayerId)
        {
            Utils.SendMessage(GetString("EraserEraseSelf"), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Eraser), GetString("EraserEraseMsgTitle")));
            return;
        }

        var targetRole = target.GetCustomRole();
        if (targetRole.IsTasklessCrewmate() || targetRole.IsNeutral() || Main.TasklessCrewmate.Contains(target.PlayerId) || CopyCat.playerIdList.Contains(target.PlayerId) || target.Is(CustomRoles.Stubborn))
        {
            Utils.SendMessage(string.Format(GetString("EraserEraseBaseImpostorOrNeutralRoleNotice"), target.GetRealName()), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Eraser), GetString("EraserEraseMsgTitle")));
            return;
        }

        EraseLimit[player.PlayerId]--;
        SendRPC(player.PlayerId);

        if (!PlayerToErase.Contains(target.PlayerId))
            PlayerToErase.Add(target.PlayerId);

        Utils.SendMessage(string.Format(GetString("EraserEraseNotice"), target.GetRealName()), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Eraser), GetString("EraserEraseMsgTitle")));

        Utils.NotifyRoles(SpecifySeer: player);
    }
    public static void OnReportDeadBody()
    {
        foreach (var eraserId in playerIdList)
        {
            TempEraseLimit[eraserId] = EraseLimit[eraserId];
        }

        PlayerToErase = new();
        didVote = new();
    }
    public static void AfterMeetingTasks()
    {
        if (!IsEnable) return;

        foreach (var pc in PlayerToErase)
        {
            var player = Utils.GetPlayerById(pc);
            if (player == null) continue;
            if (!Main.ErasedRoleStorage.ContainsKey(player.PlayerId))
            {
                Main.ErasedRoleStorage.Add(player.PlayerId, player.GetCustomRole());
                Logger.Info($"Added {player.GetNameWithRole()} to ErasedRoleStorage", "Eraser");
            }
            else
            {
                Logger.Info($"Canceled {player.GetNameWithRole()} Eraser bcz already erased.", "Eraser");
                return;
            }
            player.RpcSetCustomRole(CustomRolesHelper.GetErasedRole(player.GetCustomRole().GetRoleTypes(), player.GetCustomRole()));
            NameNotifyManager.Notify(player, GetString("LostRoleByEraser"));
            player.ResetKillCooldown();
            player.SetKillCooldown();
            Logger.Info($"{player.GetNameWithRole()} 被擦除了", "Eraser");
        }
        Utils.MarkEveryoneDirtySettings();
    }
}
