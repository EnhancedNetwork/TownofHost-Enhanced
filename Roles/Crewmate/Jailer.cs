using Hazel;
using System.Collections.Generic;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

public static class Jailer
{
    private static readonly int Id = 10600;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static Dictionary<byte, byte> JailerTarget = new();
    public static Dictionary<byte, int> JailerExeLimit = new();
    public static Dictionary<byte, bool> JailerHasExe = new();
    public static Dictionary<byte, bool> JailerDidVote = new();

    public static OptionItem JailCooldown;
    public static OptionItem MaxExecution;
    public static OptionItem NBCanBeExe;
    public static OptionItem NCCanBeExe;
    public static OptionItem NECanBeExe;
    public static OptionItem NKCanBeExe;
    public static OptionItem CKCanBeExe;
    public static OptionItem notifyJailedOnMeeting;


    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Jailer);
        JailCooldown = FloatOptionItem.Create(Id + 10, "JailerJailCooldown", new(0f, 999f, 1f), 15f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Jailer])
            .SetValueFormat(OptionFormat.Seconds);
        MaxExecution = IntegerOptionItem.Create(Id + 11, "JailerMaxExecution", new(1, 14, 1), 3, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Jailer])
            .SetValueFormat(OptionFormat.Times);
        NBCanBeExe = BooleanOptionItem.Create(Id + 12, "JailerNBCanBeExe", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Jailer]);
        NCCanBeExe = BooleanOptionItem.Create(Id + 13, "JailerNCCanBeExe", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Jailer]);
        NECanBeExe = BooleanOptionItem.Create(Id + 14, "JailerNECanBeExe", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Jailer]);
        NKCanBeExe = BooleanOptionItem.Create(Id + 15, "JailerNKCanBeExe", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Jailer]);
        CKCanBeExe = BooleanOptionItem.Create(Id + 16, "JailerCKCanBeExe", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Jailer]);
        notifyJailedOnMeeting = BooleanOptionItem.Create(Id + 18, "notifyJailedOnMeeting", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Jailer]);
    }

    public static void Init()
    {
        playerIdList = new();
        JailerExeLimit = new();
        JailerTarget = new();
        JailerHasExe = new();
        JailerDidVote = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        JailerExeLimit.Add(playerId, MaxExecution.GetInt());
        JailerTarget.Add(playerId, byte.MaxValue);
        JailerHasExe.Add(playerId, false);
        JailerDidVote.Add(playerId, false);
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Utils.GetPlayerById(id).IsAlive() ? JailCooldown.GetFloat() : 300f;
    public static string GetProgressText(byte playerId) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jailer).ShadeColor(0.25f), JailerExeLimit.TryGetValue(playerId, out var exeLimit) ? $"({exeLimit})" : "Invalid");


    public static void SendRPC(byte jailerId, byte targetId = byte.MaxValue, bool setTarget = true)
    {
        MessageWriter writer;
        if (!setTarget)
        {
            writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetJailerExeLimit, SendOption.Reliable, -1);
            writer.Write(jailerId);
            writer.Write(JailerExeLimit[jailerId]);
            writer.Write(JailerHasExe[jailerId]);
            writer.Write(JailerDidVote[jailerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            return;
        }
        writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetJailerTarget, SendOption.Reliable, -1);
        writer.Write(jailerId);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader, bool setTarget = true)
    {
        byte jailerId = reader.ReadByte();
        if (!setTarget)
        {
            int points = reader.ReadInt32();
            if (JailerExeLimit.ContainsKey(jailerId)) JailerExeLimit[jailerId] = points;
            else JailerExeLimit.Add(jailerId, MaxExecution.GetInt());

            bool executed = reader.ReadBoolean();
            if (JailerHasExe.ContainsKey(jailerId)) JailerHasExe[jailerId] = executed;
            else JailerHasExe.Add(jailerId, false);

            bool didvote = reader.ReadBoolean();
            if (JailerDidVote.ContainsKey(jailerId)) JailerDidVote[jailerId] = didvote;
            else JailerDidVote.Add(jailerId, false);

            return;
        }

        byte targetId = reader.ReadByte();
        JailerTarget[jailerId] = targetId;
    }

    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (!killer.Is(CustomRoles.Jailer)) return true;
        if (killer == null || target == null) return true;
        if (JailerTarget[killer.PlayerId] != byte.MaxValue)
        {
            killer.Notify(GetString("JailerTargetAlreadySelected"));
            return false;
        }
        JailerTarget[killer.PlayerId] = target.PlayerId;
        killer.Notify(GetString("SuccessfullyJailed"));
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        SendRPC(killer.PlayerId, target.PlayerId, true);
        return false;
    }

    public static void OnReportDeadBody()
    {
        foreach (var targetId in JailerTarget.Values)
        {
            if (targetId == byte.MaxValue) continue;
            var tpc = Utils.GetPlayerById(targetId);
            if (tpc == null) continue;
            if (notifyJailedOnMeeting.GetBool() && tpc.IsAlive())
                _ = new LateTask(() =>
                {
                    Utils.SendMessage(GetString("JailedNotifyMsg"), targetId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jailer), GetString("JailerTitle")));
                }, 0.3f, "Jailer Notify Jailed");
        }
    }

    public static void OnVote(PlayerControl voter, PlayerControl target)
    {
        if (voter == null || target == null) return;
        if (!voter.Is(CustomRoles.Jailer)) return;
        if (JailerDidVote[voter.PlayerId]) return;
        if (JailerTarget[voter.PlayerId] == byte.MaxValue) return;
        JailerDidVote[voter.PlayerId] = true;
        if (target.PlayerId == JailerTarget[voter.PlayerId])
        {
            if (JailerExeLimit[voter.PlayerId] > 0)
            {
                JailerExeLimit[voter.PlayerId] = JailerExeLimit[voter.PlayerId] - 1;
                JailerHasExe[voter.PlayerId] = true;
            }
            else JailerHasExe[voter.PlayerId] = false;
        }
        SendRPC(voter.PlayerId, setTarget: false);
    }

    public static bool CanBeExecuted(this CustomRoles role)
    {
        return ((role.IsNB() && NBCanBeExe.GetBool()) ||
                (role.IsNC() && NCCanBeExe.GetBool()) ||
                (role.IsNE() && NCCanBeExe.GetBool()) ||
                (role.IsNK() && NKCanBeExe.GetBool()) ||
                (role.IsCK() && CKCanBeExe.GetBool()) ||
                (role.IsImpostorTeamV3()));
    }

    public static void AfterMeetingTasks()
    {
        if (!IsEnable) return;

        foreach (var pid in JailerHasExe.Keys)
        {
            var targetId = JailerTarget[pid];
            if (targetId != byte.MaxValue && JailerHasExe[pid])
            {
                var tpc = Utils.GetPlayerById(targetId);
                if (tpc.IsAlive())
                {
                    CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Execution, targetId);
                    tpc.SetRealKiller(Utils.GetPlayerById(pid));
                }
                if (!tpc.GetCustomRole().CanBeExecuted())
                {
                    JailerExeLimit[pid] = 0;
                    SendRPC(pid, setTarget: false);
                }
            }
            JailerHasExe[pid] = false;
            JailerTarget[pid] = byte.MaxValue;
            JailerDidVote[pid] = false;
            SendRPC(pid, byte.MaxValue, setTarget: true);
        }
    }

}