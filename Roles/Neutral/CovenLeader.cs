using Hazel;
using UnityEngine;
using System.Linq;
using AmongUs.GameOptions;
using System.Collections.Generic;

using static TOHE.Options;

namespace TOHE.Roles.Neutral;

public static class CovenLeader
{
    private static readonly int Id = 10350;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static Dictionary<byte, byte> CovenLeaderList = new();

    private static OptionItem ControlCooldown;
    public static OptionItem CanVent;

    public static void SetupCustomOption()
    {
        //CovenLeaderは1人固定
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.CovenLeader, 1, zeroOne: false);
        ControlCooldown = FloatOptionItem.Create(Id + 12, "ControlCooldown", new(0f, 180f, 1f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CovenLeader])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CovenLeader]);
    }
    public static void Init()
    {
        playerIdList = new();
        CovenLeaderList = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    private static void SendRPC(byte leaderId, byte targetId, byte typeId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncCovenLeader, SendOption.Reliable, -1);
        writer.Write(typeId);
        writer.Write(leaderId);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        var typeId = reader.ReadByte();
        var leaderId = reader.ReadByte();
        var targetId = reader.ReadByte();

        switch (typeId)
        {
            case 0:
                CovenLeaderList.Clear();
                break;
            case 1:
                CovenLeaderList[targetId] = leaderId;
                break;
            case 2:
                CovenLeaderList.Remove(targetId);
                break;
        }
    }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        CovenLeaderList[target.PlayerId] = killer.PlayerId;
        SendRPC(killer.PlayerId, target.PlayerId, 1);
        killer.SetKillCooldown();

        Utils.NotifyRoles(SpecifySeer: killer);

        return false;
    }

    public static void OnFixedUpdate(PlayerControl leader)
    {
        if (!IsEnable) return;
        if (!GameStates.IsInTask) return;
        if (!CovenLeaderList.ContainsKey(leader.PlayerId)) return;

        if (!leader.IsAlive() || Pelican.IsEaten(leader.PlayerId))
        {
            CovenLeaderList.Remove(leader.PlayerId);
        }
        else
        {
            var covenleaderPos = leader.transform.position;
            Dictionary<byte, float> targetDistance = new();
            float dis;

            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (target.PlayerId != leader.PlayerId && !(/*target.GetCustomRole().IsCoven() ||*/ target.Is(CustomRoles.Glitch) || target.Is(CustomRoles.Pestilence)))
                {
                    dis = Vector2.Distance(covenleaderPos, target.transform.position);
                    targetDistance.Add(target.PlayerId, dis);
                }
            }

            if (targetDistance.Any())
            {
                var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();
                var target = Utils.GetPlayerById(min.Key);
                var KillRange = NormalGameOptionsV07.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];

                if (min.Value <= KillRange && leader.CanMove && target.CanMove)
                {
                    if (leader.RpcCheckAndMurder(target, true))
                    {
                        var puppeteerId = CovenLeaderList[leader.PlayerId];
                        RPC.PlaySoundRPC(puppeteerId, Sounds.KillSound);
                        target.SetRealKiller(Utils.GetPlayerById(puppeteerId));
                        leader.RpcMurderPlayerV3(target);
                        Utils.MarkEveryoneDirtySettings();
                        CovenLeaderList.Remove(leader.PlayerId);
                        SendRPC(byte.MaxValue, leader.PlayerId, 2);
                        Utils.NotifyRoles(SpecifySeer: leader);
                        if (!leader.Is(CustomRoles.Pestilence))
                        {
                            leader.RpcMurderPlayerV3(leader);
                            Main.PlayerStates[leader.PlayerId].deathReason = PlayerState.DeathReason.Drained;
                            leader.SetRealKiller(Utils.GetPlayerById(puppeteerId));
                        }
                    }
                }
            }
        }
    }

    public static void OnReportDeadBody()
    {
        CovenLeaderList.Clear();
        SendRPC(byte.MaxValue, byte.MaxValue, 0);
    }
    public static string TargetMark(PlayerControl seer, PlayerControl target)
        => (CovenLeaderList.ContainsValue(seer.PlayerId) && CovenLeaderList.ContainsKey(target.PlayerId)) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.CovenLeader), "◆") : "";
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = ControlCooldown.GetFloat();
    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(true);
    public static void CanUseVent(PlayerControl player)
    {
        bool CovenLeader_canUse = CanVent.GetBool();
        DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(CovenLeader_canUse && !player.Data.IsDead);
        player.Data.Role.CanVent = CovenLeader_canUse;
    }
}
