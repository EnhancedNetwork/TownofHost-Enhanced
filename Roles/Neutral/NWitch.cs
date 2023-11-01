using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

public static class NWitch
{
    private static readonly int Id = 10300;
    public static bool IsEnable = false;

    public static Dictionary<byte, byte> TaglockedList = new();

    private static OptionItem ControlCooldown;
    public static OptionItem CanVent;
    private static OptionItem HasImpostorVision;

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.NWitch, 1, zeroOne: false);
        ControlCooldown = FloatOptionItem.Create(Id + 10, "ControlCooldown", new(0f, 180f, 1f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NWitch])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NWitch]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NWitch]);
    }
    public static void Init()
    {
        TaglockedList = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    private static void SendRPC(byte taglockedId, byte targetId, byte typeId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncNWitch, SendOption.Reliable, -1);
        writer.Write(typeId);
        writer.Write(taglockedId);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        var typeId = reader.ReadByte();
        var taglockedId = reader.ReadByte();
        var targetId = reader.ReadByte();

        switch (typeId)
        {
            case 0:
                TaglockedList.Clear();
                break;
            case 1:
                TaglockedList[targetId] = taglockedId;
                break;
            case 2:
                TaglockedList.Remove(targetId);
                break;
        }
    }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        TaglockedList[target.PlayerId] = killer.PlayerId;
        SendRPC(killer.PlayerId, target.PlayerId, 1);
        
        killer.SetKillCooldown();

        Utils.NotifyRoles(SpecifySeer: killer);

        return false;
    }

    public static void OnFixedUpdate(PlayerControl taglocked)
    {
        if (!TaglockedList.ContainsKey(taglocked.PlayerId)) return;

        if (!taglocked.IsAlive() || Pelican.IsEaten(taglocked.PlayerId))
        {
            TaglockedList.Remove(taglocked.PlayerId);
        }
        else
        {
            var taglockedPos = taglocked.transform.position;
            Dictionary<byte, float> targetDistance = new();
            float dis;
            foreach (var target in Main.AllAlivePlayerControls)
            {

                {
                    if (target.PlayerId != taglocked.PlayerId && !(target.Is(CustomRoles.NWitch) || target.Is(CustomRoles.Pestilence)))
                    {
                        dis = Vector2.Distance(taglockedPos, target.transform.position);
                        targetDistance.Add(target.PlayerId, dis);
                    }
                }
            }
            if (targetDistance.Any())
            {
                var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();
                var target = Utils.GetPlayerById(min.Key);
                var KillRange = NormalGameOptionsV07.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];
                if (min.Value <= KillRange && taglocked.CanMove && target.CanMove)
                {
                    if (taglocked.RpcCheckAndMurder(target, true))
                    {
                        var taglockedId = TaglockedList[taglocked.PlayerId];
                        RPC.PlaySoundRPC(taglockedId, Sounds.KillSound);
                        target.SetRealKiller(Utils.GetPlayerById(taglockedId));
                        taglocked.RpcMurderPlayerV3(target);
                        Utils.MarkEveryoneDirtySettings();
                        TaglockedList.Remove(taglocked.PlayerId);
                        SendRPC(byte.MaxValue, taglocked.PlayerId, 2);
                        Utils.NotifyRoles(SpecifySeer: taglocked);
                    }
                }
            }
        }
    }
    public static void OnReportDeadBody()
    {
        TaglockedList.Clear();
        SendRPC(byte.MaxValue, byte.MaxValue, 0);
    }
    public static string TargetMark(PlayerControl seer, PlayerControl target)
        => (TaglockedList.ContainsValue(seer.PlayerId) && TaglockedList.ContainsKey(target.PlayerId)) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.NWitch), "◆") : "";

    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = ControlCooldown.GetFloat();
    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision.GetBool());
    public static void CanUseVent(PlayerControl player)
    {
        bool NWitch_canUse = CanVent.GetBool();
        DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(NWitch_canUse && !player.Data.IsDead);
        player.Data.Role.CanVent = NWitch_canUse;
    }
}
