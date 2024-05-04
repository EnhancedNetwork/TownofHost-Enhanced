using Hazel;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;
internal class SoulCollector : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 15300;
    public static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    //==================================================================\\

    private static OptionItem SoulCollectorPointsOpt;
    private static OptionItem CollectOwnSoulOpt;
    private static OptionItem GetPassiveSouls;

    private static readonly Dictionary<byte, byte> SoulCollectorTarget = [];
    private static readonly Dictionary<byte, int> SoulCollectorPoints = [];
    private static readonly Dictionary<byte, bool> DidVote = [];

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.SoulCollector, 1, zeroOne: false);
        SoulCollectorPointsOpt = IntegerOptionItem.Create(Id + 10, "SoulCollectorPointsToWin", new(1, 14, 1), 3, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SoulCollector])
            .SetValueFormat(OptionFormat.Times);
        CollectOwnSoulOpt = BooleanOptionItem.Create(Id + 11, "SoulCollector_CollectOwnSoulOpt", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SoulCollector]);
        GetPassiveSouls = BooleanOptionItem.Create(Id + 12, "GetPassiveSouls", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SoulCollector]);
    }
    public override void Init()
    {
        playerIdList.Clear();
        SoulCollectorTarget.Clear();
        SoulCollectorPoints.Clear();
        DidVote.Clear();
    }

    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        SoulCollectorTarget.TryAdd(playerId, byte.MaxValue);
        SoulCollectorPoints.TryAdd(playerId, 0);
        DidVote.TryAdd(playerId, false);

        CustomRoleManager.CheckDeadBodyOthers.Add(OnPlayerDead);
    }

    public override string GetProgressText(byte playerId, bool cvooms) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector).ShadeColor(0.25f), SoulCollectorPoints.TryGetValue(playerId, out var x) ? $"({x}/{SoulCollectorPointsOpt.GetInt()})" : "Invalid");

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.SoulCollector); //SetSoulCollectorLimit
        writer.Write(playerId);
        writer.Write(SoulCollectorPoints[playerId]);
        writer.Write(SoulCollectorTarget[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
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
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => KnowRoleTarget(seer, target);
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
        => (target.IsNeutralApocalypse() && seer.IsNeutralApocalypse());
    public override void OnVote(PlayerControl voter, PlayerControl target)
    {
        if (DidVote.TryGetValue(voter.PlayerId, out var voted) && voted) return;
        if (SoulCollectorTarget[voter.PlayerId] != byte.MaxValue) return;

        DidVote[voter.PlayerId] = true;
        
        if (!CollectOwnSoulOpt.GetBool() && voter.PlayerId == target.PlayerId)
        {
            Utils.SendMessage(GetString("SoulCollectorSelfVote"), voter.PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector), GetString("SoulCollectorTitle")));
            Logger.Info($"{voter.GetNameWithRole()} self vote not allowed", "SoulCollector");
            SoulCollectorTarget[voter.PlayerId] = byte.MaxValue;
            return;
        }

        SoulCollectorTarget.Remove(voter.PlayerId);
        SoulCollectorTarget.TryAdd(voter.PlayerId, target.PlayerId);
        Logger.Info($"{voter.GetNameWithRole()} predicted the death of {target.GetNameWithRole()}", "SoulCollector");
        Utils.SendMessage(string.Format(GetString("SoulCollectorTarget"), target.GetRealName()), voter.PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector), GetString("SoulCollectorTitle")));
        SendRPC(voter.PlayerId);
    }

    public override void OnReportDeadBody(PlayerControl ryuak, PlayerControl iscute)
    {
        foreach (var playerId in SoulCollectorTarget.Keys)
        {
            SoulCollectorTarget[playerId] = byte.MaxValue;
            DidVote[playerId] = false;
            if (GetPassiveSouls.GetBool())
            {
                SoulCollectorPoints[playerId]++;
                _ = new LateTask(() =>
                {
                    Utils.SendMessage(GetString("PassiveSoulGained"), playerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector), GetString("SoulCollectorTitle")));
                }, 3f, "Set Chat Visible for Everyone");
            }
        }
    }
    private void OnPlayerDead(PlayerControl killer, PlayerControl deadPlayer, bool inMeeting)
    {
        foreach (var (playerId, targetId) in SoulCollectorTarget)
        {
            if (targetId == byte.MaxValue) continue;

            if (targetId == deadPlayer.PlayerId && Main.PlayerStates[targetId].deathReason != PlayerState.DeathReason.Disconnected)
            {
                SoulCollectorTarget[playerId] = byte.MaxValue;
                SoulCollectorPoints[playerId]++;
                SendRPC(playerId);
                Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(playerId), ForceLoop: false);
            }
            if (SoulCollectorPoints[playerId] >= SoulCollectorPointsOpt.GetInt())
            {
                SoulCollectorPoints[playerId] = SoulCollectorPointsOpt.GetInt();
            }
        }
    }
    public static void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
        if (!HasEnabled || deathReason != PlayerState.DeathReason.Vote) return;
        if (!CustomRoles.Death.RoleExist()) return;
        if (exileIds.Contains(playerIdList.First())) return;
        var deathList = new List<byte>();
        PlayerControl sc = Utils.GetPlayerById(playerIdList.First());
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.IsNeutralApocalypse()) continue;
            if (sc != null && sc.IsAlive())
            {
                if (!Main.AfterMeetingDeathPlayers.ContainsKey(pc.PlayerId))
                {
                    pc.SetRealKiller(sc);
                    deathList.Add(pc.PlayerId);
                }
            }
            else
            {
                Main.AfterMeetingDeathPlayers.Remove(pc.PlayerId);
            }
        }
        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Armageddon, [.. deathList]);
    }
    public override void AfterMeetingTasks()
    {
        PlayerControl sc = Utils.GetPlayerById(playerIdList.First());
        if (SoulCollectorPoints[sc.PlayerId] < SoulCollectorPointsOpt.GetInt()) return;
        sc.RpcSetCustomRole(CustomRoles.Death);
        sc.Notify(GetString("SoulCollectorToDeath"));
        sc.RpcGuardAndKill(sc);
    }
}