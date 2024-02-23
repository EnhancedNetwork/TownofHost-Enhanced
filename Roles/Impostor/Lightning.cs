using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Modules;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

public static class Lightning
{
    private static readonly int Id = 24100;
    public static List<byte> playerIdList = [];
    public static bool IsEnable = false;

    private static OptionItem KillCooldown;
    private static OptionItem ConvertTime;
    private static OptionItem KillerConvertGhost;

    private static List<byte> GhostPlayer = [];
    private static Dictionary<byte, PlayerControl> RealKiller = [];
    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Lightning);
        KillCooldown = FloatOptionItem.Create(Id + 10, "LightningKillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lightning])
            .SetValueFormat(OptionFormat.Seconds);
        ConvertTime = FloatOptionItem.Create(Id + 12, "LightningConvertTime", new(0f, 180f, 2.5f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lightning])
            .SetValueFormat(OptionFormat.Seconds);
        KillerConvertGhost = BooleanOptionItem.Create(Id + 14, "LightningKillerConvertGhost", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lightning]);
    }
    public static void Init()
    {
        playerIdList = [];
        GhostPlayer = [];
        RealKiller = [];
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetGhostPlayer, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(IsGhost(playerId));
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte GhostId = reader.ReadByte();
        bool isGhost = reader.ReadBoolean();
        if (GhostId == byte.MaxValue)
        {
            GhostPlayer = [];
            return;
        }
        if (isGhost)
        {
            if (!GhostPlayer.Contains(GhostId))
                GhostPlayer.Add(GhostId);
        }
        else
        {
            if (GhostPlayer.Contains(GhostId))
                GhostPlayer.Remove(GhostId);
        }
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public static bool IsGhost(PlayerControl player) => GhostPlayer.Contains(player.PlayerId);
    public static bool IsGhost(byte id) => GhostPlayer.Contains(id);
    public static bool CheckMurder(PlayerControl target) => IsGhost(target);
    public static bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null || !killer.Is(CustomRoles.Lightning)) return false;
        if (IsGhost(target)) return false;
        killer.SetKillCooldown();
        killer.RPCPlayCustomSound("Shield");
        StartConvertCountDown(killer, target);
        return true;
    }
    private static void StartConvertCountDown(PlayerControl killer, PlayerControl target)
    {
        _ = new LateTask(() =>
        {
            if (GameStates.IsInGame && GameStates.IsInTask && !GameStates.IsMeeting && target.IsAlive() && !Pelican.IsEaten(target.PlayerId))
            {
                GhostPlayer.Add(target.PlayerId);
                SendRPC(target.PlayerId);
                RealKiller.TryAdd(target.PlayerId, killer);

                if (!killer.inVent)
                    killer.RpcGuardAndKill(killer);

                Utils.NotifyRoles();
                Logger.Info($"{target.GetNameWithRole()} 转化为量子幽灵", "Lightning");
            }
        }, ConvertTime.GetFloat(), "Lightning Convert Player To Ghost");
    }
    public static void MurderPlayer(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null || !target.Is(CustomRoles.Lightning)) return;
        if (!KillerConvertGhost.GetBool() || IsGhost(killer)) return;
        RealKiller.TryAdd(killer.PlayerId, target);
        StartConvertCountDown(target, killer);
    }
    public static void OnFixedUpdate()
    {
        List<byte> deList = [];
        foreach (var ghost in GhostPlayer.ToArray())
        {
            var gs = Utils.GetPlayerById(ghost);
            if (gs == null || !gs.IsAlive() || gs.Data.Disconnected)
            {
                deList.Add(gs.PlayerId);
                continue;
            }
            var allAlivePlayerControls = Main.AllAlivePlayerControls.Where(x => x.PlayerId != gs.PlayerId && x.IsAlive() && !x.Is(CustomRoles.Lightning) && !IsGhost(x) && !Pelican.IsEaten(x.PlayerId)).ToArray();
            foreach (var pc in allAlivePlayerControls)
            {
                var pos = gs.transform.position;
                var dis = Vector2.Distance(pos, pc.transform.position);
                if (dis > 0.3f) continue;

                deList.Add(gs.PlayerId);
                Main.PlayerStates[gs.PlayerId].IsDead = true;
                Main.PlayerStates[gs.PlayerId].deathReason = PlayerState.DeathReason.Quantization;
                gs.SetRealKiller(RealKiller[gs.PlayerId]);
                gs.RpcMurderPlayerV3(gs);

                Logger.Info($"{gs.GetNameWithRole()} 作为量子幽灵因碰撞而死", "BallLightning");
                break;
            }
        }
        if (deList.Count > 0)
        {
            GhostPlayer.RemoveAll(deList.Contains);
            foreach (var gs in deList.ToArray()) SendRPC(gs);
            Utils.NotifyRoles();
        }
    }
    public static void OnReportDeadBody()
    {
        foreach (var ghost in GhostPlayer.ToArray())
        {
            var gs = Utils.GetPlayerById(ghost);
            if (gs == null) continue;
            CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Quantization, gs.PlayerId);
            gs.SetRealKiller(RealKiller[gs.PlayerId]);
            Logger.Info($"{gs.GetNameWithRole()} 作为量子幽灵参与会议，将在会议后死亡", "BallLightning");
        }
        GhostPlayer = [];
        SendRPC(byte.MaxValue);
        Utils.NotifyRoles();
    }
}