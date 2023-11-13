using Hazel;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;
public static class SoulCollector
{
    private static readonly int Id = 15300;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static Dictionary<byte, byte> SoulCollectorTarget = new();
    public static Dictionary<byte, int> SoulCollectorPoints = new();
    public static Dictionary<byte, bool> DidVote = new();

    public static OptionItem SoulCollectorPointsOpt;
    public static OptionItem CollectOwnSoulOpt;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.SoulCollector);
        SoulCollectorPointsOpt = IntegerOptionItem.Create(Id + 10, "SoulCollectorPointsToWin", new(1, 14, 1), 3, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.SoulCollector])
            .SetValueFormat(OptionFormat.Times);
        CollectOwnSoulOpt = BooleanOptionItem.Create(Id + 11, "CollectOwnSoulOpt", true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.SoulCollector]);
    }
    public static void Init()
    {
        playerIdList = new();
        SoulCollectorTarget = new();
        SoulCollectorPoints = new();
        DidVote = new();
        IsEnable = false;
    }

    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        SoulCollectorTarget.Add(playerId, byte.MaxValue);
        SoulCollectorPoints.Add(playerId, 0);
        DidVote[playerId] = false;
        IsEnable = true;
    }

    public static string GetProgressText(byte playerId) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector).ShadeColor(0.25f), SoulCollectorPoints.TryGetValue(playerId, out var x) ? $"({x}/{SoulCollectorPointsOpt.GetInt()})" : "Invalid");

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSoulCollectorLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(SoulCollectorPoints[playerId]);
        writer.Write(SoulCollectorTarget[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        byte SoulCollectorId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        byte target = reader.ReadByte();
        if (SoulCollectorPoints.ContainsKey(SoulCollectorId))
            SoulCollectorPoints[SoulCollectorId] = Limit;
        else
            SoulCollectorPoints.Add(SoulCollectorId, 0);
        if (SoulCollectorTarget.ContainsKey(SoulCollectorId))
            SoulCollectorTarget[SoulCollectorId] = target;
        else
            SoulCollectorTarget.Add(SoulCollectorId, byte.MaxValue);
    }

    public static void OnVote(PlayerControl voter, PlayerControl target)
    {
        if (!voter.Is(CustomRoles.SoulCollector)) return;
        if (DidVote[voter.PlayerId]) return;
        if (SoulCollectorTarget[voter.PlayerId] != byte.MaxValue) return;
        DidVote[voter.PlayerId] = true;
        if (!CollectOwnSoulOpt.GetBool() && voter.PlayerId == target.PlayerId)
        {
            Utils.SendMessage(GetString("SoulCollectorSelfVote"), voter.PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector), GetString("SoulCollectorTitle")));
            Logger.Info($"{voter.GetNameWithRole()} Self vote Not Allowed", "SoulCollector");
            SoulCollectorTarget[voter.PlayerId] = byte.MaxValue;

            return;
        }


        SoulCollectorTarget[voter.PlayerId] = target.PlayerId;
        Logger.Info($"{voter.GetNameWithRole()} predicted the death of {target.GetNameWithRole()}", "SoulCollector");
        Utils.SendMessage(string.Format(GetString("SoulCollectorTarget"), target.GetRealName()), voter.PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector), GetString("SoulCollectorTitle")));
        SendRPC(voter.PlayerId);
    }

    public static void OnReportDeadBody()
    {
        foreach (var playerId in SoulCollectorTarget.Keys) 
        { 
            SoulCollectorTarget[playerId] = byte.MaxValue;
            DidVote[playerId] = false;
        }
    }

    public static void OnPlayerDead(PlayerControl deadPlayer)
    {
        foreach (var playerId in SoulCollectorTarget.Keys.ToArray())
        {
            var targetId = SoulCollectorTarget[playerId];
            if (targetId == byte.MaxValue) continue;

            if ((targetId == deadPlayer.PlayerId) && (Main.PlayerStates[targetId].deathReason != PlayerState.DeathReason.Disconnected))
            {
                SoulCollectorTarget[playerId] = byte.MaxValue;
                SoulCollectorPoints[playerId]++;
                SendRPC(playerId);
            }
            if (SoulCollectorPoints[playerId] >= SoulCollectorPointsOpt.GetInt())
            {
                SoulCollectorPoints[playerId] = SoulCollectorPointsOpt.GetInt();
                if (!CustomWinnerHolder.CheckForConvertedWinner(playerId))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SoulCollector);
                    CustomWinnerHolder.WinnerIds.Add(playerId);
                }
            }
        }
    }

}