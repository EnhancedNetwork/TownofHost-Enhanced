﻿using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Jailer : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 10600;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem JailCooldown;
    private static OptionItem MaxExecution;
    private static OptionItem NBCanBeExe;
    private static OptionItem NCCanBeExe;
    private static OptionItem NECanBeExe;
    private static OptionItem NKCanBeExe;
    private static OptionItem NACanBeExe;
    private static OptionItem CKCanBeExe;
    private static OptionItem NotifyJailedOnMeetingOpt;

    private static readonly Dictionary<byte, byte> JailerTarget = [];
    private static readonly Dictionary<byte, int> JailerExeLimit = [];
    private static readonly Dictionary<byte, bool> JailerHasExe = [];
    private static readonly Dictionary<byte, bool> JailerDidVote = [];

    public override void SetupCustomOption()
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
        NACanBeExe = BooleanOptionItem.Create(Id + 17, "JailerNACanBeExe", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Jailer]);
        CKCanBeExe = BooleanOptionItem.Create(Id + 16, "JailerCKCanBeExe", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Jailer]);
        NotifyJailedOnMeetingOpt = BooleanOptionItem.Create(Id + 18, "notifyJailedOnMeeting", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Jailer]);
    }

    public override void Init()
    {
        playerIdList.Clear();
        JailerExeLimit.Clear();
        JailerTarget.Clear();
        JailerHasExe.Clear();
        JailerDidVote.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        JailerExeLimit.Add(playerId, MaxExecution.GetInt());
        JailerTarget.Add(playerId, byte.MaxValue);
        JailerHasExe.Add(playerId, false);
        JailerDidVote.Add(playerId, false);

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        JailerExeLimit.Remove(playerId);
        JailerHasExe.Remove(playerId);
        JailerDidVote.Remove(playerId);
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;

    public static bool IsTarget(byte playerId) => JailerTarget.ContainsValue(playerId);
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Utils.GetPlayerById(id).IsAlive() ? JailCooldown.GetFloat() : 300f;
    public override string GetProgressText(byte playerId, bool cooms) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jailer).ShadeColor(0.25f), JailerExeLimit.TryGetValue(playerId, out var exeLimit) ? $"({exeLimit})" : "Invalid");

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

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;

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
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override void OnReportDeadBody(PlayerControl sob, NetworkedPlayerInfo bakugan)
    {
        foreach (var targetId in JailerTarget.Values)
        {
            if (targetId == byte.MaxValue) continue;
            var tpc = Utils.GetPlayerById(targetId);
            if (tpc == null) continue;

            if (NotifyJailedOnMeetingOpt.GetBool() && tpc.IsAlive())
            {
                _ = new LateTask(() =>
                {
                    if (GameStates.IsInGame)
                    {
                        Utils.SendMessage(GetString("JailedNotifyMsg"), targetId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jailer), GetString("JailerTitle")));
                    }
                }, 5f, $"Jailer Notify Jailed - id:{targetId}");
            }
        }
    }

    public override void OnVote(PlayerControl voter, PlayerControl target)
    {
        if (voter == null || target == null || !voter.Is(CustomRoles.Jailer)) return;
        if (JailerDidVote.TryGetValue(voter.PlayerId, out var didVote) && didVote) return;
        if (JailerTarget.TryGetValue(voter.PlayerId, out var jTarget) && jTarget == byte.MaxValue) return;

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

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting)
    {
        return JailerTarget.TryGetValue(seer.PlayerId, out var targetID) && isForMeeting && seer != seen && seen.PlayerId == targetID ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jailer), "⊠") : "";
    }

    private static bool CanBeExecuted(CustomRoles role)
    {
        return ((role.IsNB() && NBCanBeExe.GetBool()) ||
                (role.IsNC() && NCCanBeExe.GetBool()) ||
                (role.IsNE() && NECanBeExe.GetBool()) ||
                (role.IsNK() && NKCanBeExe.GetBool()) ||
                (role.IsNA() && NACanBeExe.GetBool()) ||
                (role.IsCrewKiller() && CKCanBeExe.GetBool()) ||
                (role.IsImpostorTeamV3()));
    }

    public override void AfterMeetingTasks()
    {
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
                if (!CanBeExecuted(tpc.GetCustomRole()))
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
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.KillButton.OverrideText(GetString("JailorKillButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("penitentiary");
}