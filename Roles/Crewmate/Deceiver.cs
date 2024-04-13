using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Deceiver : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 10500;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem DeceiverSkillCooldown;
    private static OptionItem DeceiverSkillLimitTimes;
    private static OptionItem DeceiverAbilityLost;

    private static readonly HashSet<byte> notActiveList = [];
    private static readonly Dictionary<byte, HashSet<byte>> clientList = [];
    private static readonly Dictionary<byte, int> SeelLimit = [];

    public override void SetupCustomOption()
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
        playerIdList.Clear();
        clientList.Clear();
        notActiveList.Clear();
        SeelLimit.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        SeelLimit.Add(playerId, DeceiverSkillLimitTimes.GetInt());

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
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
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
    private static bool IsClient(byte playerId)
    {
        foreach (var pc in clientList)
            if (pc.Value.Contains(playerId)) return true;
        return false;
    }
    private static bool CanBeClient(PlayerControl pc) => pc != null && pc.IsAlive() && !GameStates.IsMeeting && !IsClient(pc.PlayerId);
    private static bool CanSeel(byte playerId) => playerIdList.Contains(playerId) && SeelLimit.TryGetValue(playerId, out int x) && x > 0;
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return true;
        if (target.Is(CustomRoles.Pestilence) || target.Is(CustomRoles.SerialKiller)) return true;

        if (!(CanBeClient(target) && CanSeel(killer.PlayerId))) return false;

        SeelLimit[killer.PlayerId]--;
        SendRPC(killer.PlayerId);

        if (target.Is(CustomRoles.KillingMachine))
        {
            Logger.Info("target is Killing Machine, ability used count reduced, but target will not die", "Deceiver");
            return false;
        }

        if (!clientList.ContainsKey(killer.PlayerId)) clientList.Add(killer.PlayerId, []);
        clientList[killer.PlayerId].Add(target.PlayerId);

        notActiveList.Add(killer.PlayerId);
        killer.RpcGuardAndKill(killer);
        killer.SetKillCooldown();

        killer.RPCPlayCustomSound("Bet");

        Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target);

        Logger.Info($"Counterfeiters {killer.GetRealName()} sell counterfeits to {target.GetRealName()}", "Deceiver");
        return false;
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl pc, PlayerControl _)
    {
        if (!IsClient(pc.PlayerId) || notActiveList.Contains(pc.PlayerId)) return false;
        
        byte cfId = byte.MaxValue;
        foreach (var cf in clientList)
            if (cf.Value.Contains(pc.PlayerId)) cfId = cf.Key;

        if (cfId == byte.MaxValue) return false;

        var killer = Utils.GetPlayerById(cfId);
        var target = pc;
        if (killer == null) return true;
        
        Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
        target.RpcMurderPlayer(target);
        target.SetRealKiller(killer);

        Logger.Info($"The customer {target.GetRealName()} of {pc.GetRealName()}, a counterfeiter, commits suicide by using counterfeits", "Deceiver");
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
                    (role.IsCrewmate() && !role.IsCrewKiller()) ||
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