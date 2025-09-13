using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Jailer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Jailer;
    private const int Id = 10600;
    public override bool IsDesyncRole => true;
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
    private static OptionItem CovenCanBeExe;
    private static OptionItem CKCanBeExe;
    private static OptionItem NotifyJailedOnMeetingOpt;

    private static readonly Dictionary<byte, int> JailerTarget = [];
    private static readonly Dictionary<byte, bool> JailerHasExe = [];
    private static readonly Dictionary<byte, bool> JailerDidVote = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Jailer);
        JailCooldown = FloatOptionItem.Create(Id + 10, "JailerJailCooldown", new(0f, 999f, 1f), 15f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jailer])
            .SetValueFormat(OptionFormat.Seconds);
        MaxExecution = IntegerOptionItem.Create(Id + 11, "JailerMaxExecution", new(1, 14, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jailer])
            .SetValueFormat(OptionFormat.Times);
        NBCanBeExe = BooleanOptionItem.Create(Id + 12, "JailerNBCanBeExe", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jailer]);
        NCCanBeExe = BooleanOptionItem.Create(Id + 13, "JailerNCCanBeExe", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jailer]);
        NECanBeExe = BooleanOptionItem.Create(Id + 14, "JailerNECanBeExe", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jailer]);
        NKCanBeExe = BooleanOptionItem.Create(Id + 15, "JailerNKCanBeExe", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jailer]);
        NACanBeExe = BooleanOptionItem.Create(Id + 17, "JailerNACanBeExe", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jailer]);
        CovenCanBeExe = BooleanOptionItem.Create(Id + 19, "JailerCovenCanBeExe", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jailer]);
        CKCanBeExe = BooleanOptionItem.Create(Id + 16, "JailerCKCanBeExe", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jailer]);
        NotifyJailedOnMeetingOpt = BooleanOptionItem.Create(Id + 18, "notifyJailedOnMeeting", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jailer]);
    }

    public override void Init()
    {
        JailerTarget.Clear();
        JailerHasExe.Clear();
        JailerDidVote.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(MaxExecution.GetInt());
        JailerTarget[playerId] = byte.MaxValue;
        JailerHasExe[playerId] = false;
        JailerDidVote[playerId] = false;
    }
    public override void Remove(byte playerId)
    {
        JailerHasExe.Remove(playerId);
        JailerDidVote.Remove(playerId);
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;

    public static bool IsTarget(byte playerId) => JailerTarget.ContainsValue(playerId);
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = id.GetPlayer().IsAlive() ? JailCooldown.GetFloat() : 300f;

    public static void SendRPC(byte jailerId)
    {
        var msg = new RpcSyncJailerData(PlayerControl.LocalPlayer.NetId, jailerId, JailerTarget[jailerId], JailerHasExe[jailerId], JailerDidVote[jailerId]);
        RpcUtils.LateBroadcastReliableMessage(msg);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        byte jailerId = reader.ReadByte();

        int targetId = reader.ReadPackedInt32();
        JailerTarget[jailerId] = targetId;

        bool executed = reader.ReadBoolean();
        JailerHasExe[jailerId] = executed;

        bool didvote = reader.ReadBoolean();
        JailerDidVote[jailerId] = didvote;
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
        SendRPC(killer.PlayerId);
        return false;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);

    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (!NotifyJailedOnMeetingOpt.GetBool()) return;

        foreach (var targetId in JailerTarget.Values)
        {
            var targetIdByte = (byte)targetId;
            if (targetIdByte == byte.MaxValue) continue;

            var tpc = targetIdByte.GetPlayer();
            if (!tpc.IsAlive()) continue;

            MeetingHudStartPatch.AddMsg(GetString("JailedNotifyMsg"), targetIdByte, ColorString(GetRoleColor(CustomRoles.Jailer), GetString("Jailer").ToUpper()));
        }
    }

    public override void AfterMeetingTasks()
    {
        JailerTarget.Clear();
    }

    public override void OnVote(PlayerControl voter, PlayerControl target)
    {
        if (voter == null || target == null) return;
        if (JailerDidVote.TryGetValue(voter.PlayerId, out var didVote) && didVote) return;
        if (JailerTarget.TryGetValue(voter.PlayerId, out var jTarget) && jTarget == byte.MaxValue) return;

        JailerDidVote[voter.PlayerId] = true;
        if (target.PlayerId == jTarget)
        {
            if (voter.GetAbilityUseLimit() > 0)
            {
                voter.RpcRemoveAbilityUse();
                JailerHasExe[voter.PlayerId] = true;
            }
            else JailerHasExe[voter.PlayerId] = false;
        }
        SendRPC(voter.PlayerId);
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting)
    {
        return seer.PlayerId != seen.PlayerId && JailerTarget.TryGetValue(seer.PlayerId, out var targetID) && seen.PlayerId == targetID ? ColorString(GetRoleColor(CustomRoles.Jailer), "⊠") : string.Empty;
    }

    private static bool CanBeExecuted(CustomRoles role)
    {
        return (role.IsNB() && NBCanBeExe.GetBool()) ||
                (role.IsNC() && NCCanBeExe.GetBool()) ||
                (role.IsNE() && NECanBeExe.GetBool()) ||
                (role.IsNK() && NKCanBeExe.GetBool()) ||
                (role.IsNA() && !role.IsTNA() && NACanBeExe.GetBool()) ||
                (role.IsCoven() && CovenCanBeExe.GetBool()) ||
                (role.IsCrewKiller() && CKCanBeExe.GetBool()) ||
                role.IsImpostorTeamV3();
    }

    public override void OnPlayerExiled(PlayerControl player, NetworkedPlayerInfo exiled)
    {
        var playerId = player.PlayerId;
        if (!JailerTarget.TryGetValue(playerId, out var targetId)) return;

        if (targetId != byte.MaxValue && JailerHasExe[playerId])
        {
            var targetIdByte = (byte)targetId;
            var tpc = targetIdByte.GetPlayer();
            if (tpc != null)
            {
                if (tpc.IsAlive())
                {
                    CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Execution, targetIdByte);
                    tpc.SetRealKiller(player);
                }
                if ((!CanBeExecuted(tpc.GetCustomRole()) && !player.IsAnySubRole(x => x.IsConverted() && x != CustomRoles.Soulless)) || tpc.Is(CustomRoles.Narc))
                {
                    playerId.SetAbilityUseLimit(0);
                }
            }
        }
        JailerHasExe[playerId] = false;
        JailerTarget[playerId] = byte.MaxValue;
        JailerDidVote[playerId] = false;
        SendRPC(playerId);
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.KillButton.OverrideText(GetString("JailorKillButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("penitentiary");
}
