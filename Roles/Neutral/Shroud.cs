using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

public static class Shroud
{
    private static readonly int Id = 18000;
    public static bool IsEnable = false;

    public static Dictionary<byte, byte> ShroudList = new();

    private static OptionItem ShroudCooldown;
    public static OptionItem CanVent;
    private static OptionItem HasImpostorVision;

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Shroud, 1, zeroOne: false);
        ShroudCooldown = FloatOptionItem.Create(Id + 10, "ShroudCooldown", new(0f, 180f, 1f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Shroud])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Shroud]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Shroud]);
    }
    public static void Init()
    {
        ShroudList = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    private static void SendRPC(byte shroudId, byte targetId, byte typeId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncShroud, SendOption.Reliable, -1);
        writer.Write(typeId);
        writer.Write(shroudId);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        var typeId = reader.ReadByte();
        var shroudId = reader.ReadByte();
        var targetId = reader.ReadByte();

        switch (typeId)
        {
            case 0:
                ShroudList.Clear();
                break;
            case 1:
                ShroudList[targetId] = shroudId;
                break;
            case 2:
                ShroudList.Remove(targetId);
                break;
        }
    }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        ShroudList[target.PlayerId] = killer.PlayerId;
        SendRPC(killer.PlayerId, target.PlayerId, 1);

        killer.SetKillCooldown();

        Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);

        return false;
    }

    public static void OnFixedUpdate(PlayerControl shroud)
    {
        if (!ShroudList.ContainsKey(shroud.PlayerId)) return;

        if (!shroud.IsAlive() || Pelican.IsEaten(shroud.PlayerId))
        {
            ShroudList.Remove(shroud.PlayerId);
        }
        else
        {
            var shroudPos = shroud.transform.position;
            Dictionary<byte, float> targetDistance = new();
            float dis;
            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (target.PlayerId != shroud.PlayerId && !target.Is(CustomRoles.Shroud) && !target.Is(CustomRoles.Pestilence))
                {
                    dis = Vector2.Distance(shroudPos, target.transform.position);
                    targetDistance.Add(target.PlayerId, dis);
                }
            }
            if (targetDistance.Any())
            {
                var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();
                var target = Utils.GetPlayerById(min.Key);
                var KillRange = NormalGameOptionsV07.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];
                if (min.Value <= KillRange && shroud.CanMove && target.CanMove)
                {
                    if (shroud.RpcCheckAndMurder(target, true))
                    {
                        var shroudId = ShroudList[shroud.PlayerId];
                        RPC.PlaySoundRPC(shroudId, Sounds.KillSound);
                        target.SetRealKiller(Utils.GetPlayerById(shroudId));
                        Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Shrouded;
                        shroud.RpcMurderPlayerV3(target);
                        Utils.MarkEveryoneDirtySettings();
                        ShroudList.Remove(shroud.PlayerId);
                        SendRPC(byte.MaxValue, shroud.PlayerId, 2);
                        //Utils.NotifyRoles(SpecifySeer: shroud);
                        Utils.NotifyRoles(Utils.GetPlayerById(shroudId), SpecifyTarget: shroud, ForceLoop: true);
                    }
                }
            }
        }
    }

    public static void MurderShroudedPlayers(PlayerControl shrouded)
    {
        if (!ShroudList.ContainsKey(shrouded.PlayerId)) return;
        byte shroudId = ShroudList[shrouded.PlayerId];
        PlayerControl shroudPC = Utils.GetPlayerById(shroudId);
        if (shroudPC == null) return;
        if (shroudPC.IsAlive())
        { 
            shrouded.RpcMurderPlayerV3(shrouded);
            Main.PlayerStates[shrouded.PlayerId].deathReason = PlayerState.DeathReason.Shrouded;
        }
        ShroudList.Remove(shrouded.PlayerId);
        SendRPC(byte.MaxValue, shrouded.PlayerId, 2);
    }

    public static string TargetMark(PlayerControl seer, PlayerControl target)
        => (ShroudList.ContainsValue(seer.PlayerId) && ShroudList.ContainsKey(target.PlayerId)) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Shroud), "◈") : "";
    
    public static string GetShroudMark(byte target, bool isMeeting)
    {
        if (isMeeting && ShroudList.ContainsKey(target))
        {
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Shroud), "◈");
        }
        return "";
    }

    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = ShroudCooldown.GetFloat();
    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision.GetBool());
    public static void CanUseVent(PlayerControl player)
    {
        bool Shroud_canUse = CanVent.GetBool();
        DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(Shroud_canUse && !player.Data.IsDead);
        player.Data.Role.CanVent = Shroud_canUse;
    }
}
