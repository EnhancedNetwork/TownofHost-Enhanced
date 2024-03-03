using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using TOHE.Modules;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Deceiver : RoleBase
{
    private static readonly int Id = 10500;
    private static List<byte> playerIdList = [];
    private static bool On = false;
    public override bool IsEnable => On;
    public static bool HasEnabled => CustomRoles.Deceiver.IsClassEnable();
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    private static Dictionary<byte, List<byte>> clientList = [];
    private static List<byte> notActiveList = [];
    public static Dictionary<byte, int> SeelLimit = [];
    public static OptionItem DeceiverSkillCooldown;
    public static OptionItem DeceiverSkillLimitTimes;
    public static OptionItem DeceiverAbilityLost;
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Deceiver);
        DeceiverSkillCooldown = FloatOptionItem.Create(Id + 10, "DeceiverSkillCooldown", new(2.5f, 180f, 2.5f), 20f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Deceiver])
            .SetValueFormat(OptionFormat.Seconds);
        DeceiverSkillLimitTimes = IntegerOptionItem.Create(Id + 11, "DeceiverSkillLimitTimes", new(1, 15, 1), 2, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Deceiver])
            .SetValueFormat(OptionFormat.Times);
        DeceiverAbilityLost = BooleanOptionItem.Create(Id + 12, "DeceiverAbilityLost", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Deceiver]);
    }
    public override void Init()
    {
        playerIdList = [];
        clientList = [];
        notActiveList = [];
        SeelLimit = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        SeelLimit.Add(playerId, DeceiverSkillLimitTimes.GetInt());
        On = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        SeelLimit.Remove(playerId);
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Deceiver);
        writer.Write(playerId);
        writer.Write(SeelLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (SeelLimit.ContainsKey(PlayerId))
            SeelLimit[PlayerId] = Limit;
        else
            SeelLimit.Add(PlayerId, DeceiverSkillLimitTimes.GetInt());
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override bool CanUseKillButton(PlayerControl pc)
        => !Main.PlayerStates[pc.PlayerId].IsDead
        && SeelLimit.TryGetValue(pc.PlayerId, out var x) && x >= 1;
    public override string GetProgressText(byte playerId, bool comms) => Utils.ColorString(CanUseKillButton(Utils.GetPlayerById(playerId)) ? Utils.GetRoleColor(CustomRoles.Deceiver).ShadeColor(0.25f) : Color.gray, SeelLimit.TryGetValue(playerId, out var x) ? $"({x})" : "Invalid");
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(Utils.GetPlayerById(id)) ? DeceiverSkillCooldown.GetFloat() : 300f;
    public static bool IsClient(byte playerId)
    {
        foreach (var pc in clientList)
            if (pc.Value.Contains(playerId)) return true;
        return false;
    }
    public static bool CanBeClient(PlayerControl pc) => pc != null && pc.IsAlive() && !GameStates.IsMeeting && !IsClient(pc.PlayerId);
    public static bool CanSeel(byte playerId) => playerIdList.Contains(playerId) && SeelLimit.TryGetValue(playerId, out int x) && x > 0;
    public static void SeelToClient(PlayerControl pc, PlayerControl target)
    {
        if (pc == null || target == null || !pc.Is(CustomRoles.Deceiver)) return;
        SeelLimit[pc.PlayerId]--;
        SendRPC(pc.PlayerId);
        if (target.Is(CustomRoles.KillingMachine))
        {
            Logger.Info("target is Killing Machine, ability used count reduced, but target will not die", "Deceiver");
            return;
        }
        if (!clientList.ContainsKey(pc.PlayerId)) clientList.Add(pc.PlayerId, []);
        clientList[pc.PlayerId].Add(target.PlayerId);
        if (!Options.DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
        notActiveList.Add(pc.PlayerId);
        pc.SetKillCooldown();
        pc.RPCPlayCustomSound("Bet");
        Utils.NotifyRoles(SpecifySeer: pc);
        Logger.Info($"赝品商 {pc.GetRealName()} 将赝品卖给了 {target.GetRealName()}", "Deceiver");
    }
    public override bool OnCheckMurderAsKiller(PlayerControl sob, PlayerControl pc)
    {
        if (!IsClient(pc.PlayerId) || notActiveList.Contains(pc.PlayerId)) return false;
        byte cfId = byte.MaxValue;
        foreach (var cf in clientList)
            if (cf.Value.Contains(pc.PlayerId)) cfId = cf.Key;
        if (cfId == byte.MaxValue) return false;
        var killer = Utils.GetPlayerById(cfId);
        var target = pc;
        if (killer == null) return false;
        target.SetRealKiller(killer);
        target.Data.IsDead = true;
        Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
        target.RpcMurderPlayerV3(target);
        Main.PlayerStates[target.PlayerId].SetDead();
        Logger.Info($"赝品商 {pc.GetRealName()} 的客户 {target.GetRealName()} 因使用赝品走火自杀", "Deceiver");
        return true;
    }
    public override void OnReportDeadBody(PlayerControl rafaeu, PlayerControl dinosaurs)
    {
        notActiveList.Clear();
        foreach (var cl in clientList)
            foreach (var pc in cl.Value.ToArray())
            {
                var target = Utils.GetPlayerById(pc);
                if (target == null || !target.IsAlive()) continue;
                var role = target.GetCustomRole();
                if (
                    (role.IsCrewmate() && !role.IsCK()) ||
                    (role.IsNeutral() && !role.IsNK())
                    )
                {
                    var killer = Utils.GetPlayerById(cl.Key);
                    if (killer == null) continue;
                    CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Misfire, target.PlayerId);
                    target.SetRealKiller(Utils.GetPlayerById(pc));
                    target.SetRealKiller(killer);
                    if (DeceiverAbilityLost.GetBool())
                    {
                        SeelLimit[killer.PlayerId] = 0;
                        SendRPC(killer.PlayerId);
                    }
                    Logger.Info($"Deceiver: {killer.GetRealName()} deceived {target.GetRealName()} player without kill button", "Deceiver");
                }
            }
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.KillButton.OverrideText(GetString("DeceiverButtonText"));
    }
}