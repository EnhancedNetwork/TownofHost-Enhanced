using Hazel;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;
public static class Pixie
{
    private static readonly int Id = 25900;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static Dictionary<byte, HashSet<byte>> PixieTargets = new();
    public static Dictionary<byte, int> PixiePoints = new();

    public static OptionItem PixiePointsToWin;
    public static OptionItem PixieMaxTargets;
    public static OptionItem PixieMarkCD;
    public static OptionItem PixieSuicideOpt;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Pixie);
        PixiePointsToWin = IntegerOptionItem.Create(Id + 10, "PixiePointsToWin", new(1, 14, 1), 3, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pixie])
            .SetValueFormat(OptionFormat.Times);
        PixieMaxTargets = IntegerOptionItem.Create(Id + 11, "MaxTargets", new(1, 14, 1), 3, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pixie])
            .SetValueFormat(OptionFormat.Players);
        PixieMarkCD = FloatOptionItem.Create(Id + 12, "MarkCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pixie])
            .SetValueFormat(OptionFormat.Seconds);
        PixieSuicideOpt = BooleanOptionItem.Create(Id + 13, "PixieSuicide", false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pixie]);
    }
    public static void Init()
    {
        playerIdList = new();
        PixieTargets = new();
        PixiePoints = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        PixieTargets[playerId] = new();
        PixiePoints.Add(playerId, 0);
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static string GetProgressText(byte playerId) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.Pixie).ShadeColor(0.25f), PixiePoints.TryGetValue(playerId, out var x) ? $"({x}/{PixiePointsToWin.GetInt()})" : "Invalid");

    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = PixieMarkCD.GetFloat();


    public static void SendRPC(byte pixieId, bool operate, byte targetId = 0xff)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetPixieTargets, SendOption.Reliable, -1);
        writer.Write(pixieId);
        writer.Write(operate);
        if (!operate) // false = 0
        {
            writer.Write(targetId);
        }
        else // true = 1
        {
            writer.Write(PixiePoints[pixieId]);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        byte pixieId = reader.ReadByte();
        bool operate = reader.ReadBoolean();
        if (!operate)
        {
            if (!PixieTargets.ContainsKey(pixieId)) PixieTargets[pixieId] = new();
            byte targetId = reader.ReadByte();
            PixieTargets[pixieId].Add(targetId);
        }
        else
        {
            int pts = reader.ReadInt32();
            if (!PixiePoints.ContainsKey(pixieId)) PixiePoints[pixieId] = 0;
            PixiePoints[pixieId] = pts;
            PixieTargets[pixieId].Clear();
        }
    }

    public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return;
        byte targetId = target.PlayerId;
        byte killerId = killer.PlayerId;
        if (!PixieTargets.ContainsKey(killerId)) PixieTargets[killerId] = new();
        if (PixieTargets[killerId].Count >= PixieMaxTargets.GetInt())
        {
            killer.Notify(GetString("PixieMaxTargetReached"));
            Logger.Info($"Max targets per round already reached, {PixieTargets[killerId].Count}/{PixieMaxTargets.GetInt()}", "Pixie");
            return;
        }
        if (PixieTargets[killerId].Contains(targetId)) 
        {
            killer.Notify(GetString("PixieTargetAlreadySelected"));
            return;
        }
        PixieTargets[killerId].Add(targetId);
        SendRPC(killerId, false, targetId);
        Utils.NotifyRoles(SpecifySeer: killer, ForceLoop: true);
        if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(killer);
        SetKillCooldown(killer.PlayerId);
        return;
    }

    public static void CheckExileTarget(GameData.PlayerInfo exiled)
    {
        if (!IsEnable) return;
        foreach (var pixieId in PixieTargets.Keys.ToArray())
        {
            if (exiled != null)
            { 
                var pc = Utils.GetPlayerById(pixieId);
                if (PixieTargets[pixieId].Count == 0) continue;
                if (!PixiePoints.ContainsKey(pixieId)) PixiePoints[pixieId] = 0;
                if (PixiePoints[pixieId] >= PixiePointsToWin.GetInt()) continue;

                if (PixieTargets[pixieId].Contains(exiled.PlayerId))
                {
                    PixiePoints[pixieId]++;
                }
                else if (PixieSuicideOpt.GetBool() 
                    && PixieTargets[pixieId].Any(eid => Utils.GetPlayerById(eid)?.IsAlive() == true))
                {
                    CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, pixieId);
                    Utils.GetPlayerById(pixieId).SetRealKiller(Utils.GetPlayerById(pixieId));
                    Logger.Info($"{pc.GetNameWithRole()} committed suicide because target not exiled and target(s) were alive during ejection", "Pixie");
                }
            }
            PixieTargets[pixieId].Clear();
            SendRPC(pixieId, true);
        }
    }

    public static void PixieWinCondition(PlayerControl pc)
    {
        if (pc == null) return;
        if (PixiePoints.TryGetValue(pc.PlayerId, out int totalPts))
        {
            if (totalPts >= PixiePointsToWin.GetInt())
            {
                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Pixie);
            }
        }
    }
}

