using AmongUs.GameOptions;
using Hazel;
using System.Text;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Wraith : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Wraith;
    private const int Id = 18500;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Wraith);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem WraithCooldown;
    private static OptionItem WraithDuration;
    private static OptionItem WraithVentNormallyOnCooldown;
    private static OptionItem HasImpostorVision;

    private static Dictionary<byte, long> InvisTime = [];
    private static readonly Dictionary<byte, long> lastTime = [];
    private static readonly Dictionary<byte, int> ventedId = [];

    private static long lastFixedTime = 0;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Wraith, 1, zeroOne: false);
        WraithCooldown = FloatOptionItem.Create(Id + 2, "WraithCooldown", new(1f, 180f, 1f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wraith])
            .SetValueFormat(OptionFormat.Seconds);
        WraithDuration = FloatOptionItem.Create(Id + 4, "WraithDuration", new(1f, 60f, 1f), 15f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wraith])
            .SetValueFormat(OptionFormat.Seconds);
        WraithVentNormallyOnCooldown = BooleanOptionItem.Create(Id + 5, "WraithVentNormallyOnCooldown", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wraith]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 6, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wraith]);
    }
    public override void Init()
    {
        InvisTime.Clear();
        lastTime.Clear();
        ventedId.Clear();
    }
    private void SendRPC(PlayerControl pc)
    {
        if (!pc.IsNonHostModdedClient()) return;
        var writer = MessageWriter.Get(SendOption.Reliable);//SetWraithTimer
        writer.Write((InvisTime.TryGetValue(pc.PlayerId, out var x) ? x : -1).ToString());
        writer.Write((lastTime.TryGetValue(pc.PlayerId, out var y) ? y : -1).ToString());
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        InvisTime.Clear();
        lastTime.Clear();
        long invis = long.Parse(reader.ReadString());
        long last = long.Parse(reader.ReadString());
        if (invis > 0) InvisTime.Add(PlayerControl.LocalPlayer.PlayerId, invis);
        if (last > 0) lastTime.Add(PlayerControl.LocalPlayer.PlayerId, last);
    }
    private static bool CanGoInvis(byte id)
        => GameStates.IsInTask && !InvisTime.ContainsKey(id) && !lastTime.ContainsKey(id);
    private static bool IsInvis(byte id) => InvisTime.ContainsKey(id);

    public override void OnReportDeadBody(PlayerControl pa, NetworkedPlayerInfo dum)
    {
        foreach (var wraithId in _playerIdList.ToArray())
        {
            if (!IsInvis(wraithId)) continue;
            var wraith = Utils.GetPlayerById(wraithId);
            if (wraith == null) continue;

            wraith?.MyPhysics?.RpcBootFromVent(ventedId.TryGetValue(wraithId, out var id) ? id : Main.LastEnteredVent[wraithId].Id);
            InvisTime.Remove(wraithId);
            ventedId.Remove(wraithId);
            SendRPC(wraith);
        }

        lastTime.Clear();
        InvisTime.Clear();
        ventedId.Clear();
    }
    public override void AfterMeetingTasks()
    {
        lastTime.Clear();
        InvisTime.Clear();

        foreach (var wraithId in _playerIdList)
        {
            var wraith = Utils.GetPlayerById(wraithId);
            if (!wraith.IsAlive()) continue;

            lastTime.Add(wraith.PlayerId, Utils.GetTimeStamp());
            SendRPC(wraith);
        }
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (lowLoad) return;

        if (lastTime.TryGetValue(player.PlayerId, out var time) && time + (long)WraithCooldown.GetFloat() < nowTime)
        {
            lastTime.Remove(player.PlayerId);
            if (!player.IsModded()) player.Notify(GetString("WraithCanVent"));
            SendRPC(player);
        }

        if (lastFixedTime != nowTime)
        {
            lastFixedTime = nowTime;
            Dictionary<byte, long> newList = [];
            List<byte> refreshList = [];
            foreach (var it in InvisTime)
            {
                var pc = it.Key.GetPlayer();
                if (pc == null) continue;
                var remainTime = it.Value + (long)WraithDuration.GetFloat() - nowTime;
                if (remainTime < 0 || !pc.IsAlive())
                {
                    lastTime.Add(pc.PlayerId, nowTime);
                    pc?.MyPhysics?.RpcBootFromVent(ventedId.TryGetValue(pc.PlayerId, out var id) ? id : Main.LastEnteredVent[pc.PlayerId].Id);
                    ventedId.Remove(pc.PlayerId);
                    pc.Notify(GetString("WraithInvisStateOut"));
                    SendRPC(pc);
                    continue;
                }
                else if (remainTime <= 10)
                {
                    if (!pc.IsModded()) pc.Notify(string.Format(GetString("WraithInvisStateCountdown"), remainTime + 1));
                }
                newList.Add(it.Key, it.Value);
            }
            InvisTime.Where(x => !newList.ContainsKey(x.Key)).Do(x => refreshList.Add(x.Key));
            InvisTime = newList;
            refreshList.Do(x => SendRPC(Utils.GetPlayerById(x)));
        }
    }
    public override void OnCoEnterVent(PlayerPhysics __instance, int ventId)
    {
        var pc = __instance.myPlayer;
        if (!AmongUsClient.Instance.AmHost || IsInvis(pc.PlayerId)) return;
        _ = new LateTask(() =>
        {
            if (CanGoInvis(pc.PlayerId))
            {
                ventedId.Remove(pc.PlayerId);
                ventedId.Add(pc.PlayerId, ventId);

                __instance.RpcBootFromVentDesync(ventId, pc);

                InvisTime.Add(pc.PlayerId, Utils.GetTimeStamp());
                SendRPC(pc);
                pc.Notify(GetString("WraithInvisState"), WraithDuration.GetFloat());
            }
            else
            {
                if (!WraithVentNormallyOnCooldown.GetBool())
                {
                    __instance.myPlayer.MyPhysics.RpcBootFromVent(ventId);
                    pc.Notify(GetString("WraithInvisInCooldown"));
                }
            }
        }, 0.8f, "Wraith Vent");
    }
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => pc.IsAlive();
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (!IsInvis(pc.PlayerId)) return;

        InvisTime.Remove(pc.PlayerId);
        lastTime.Add(pc.PlayerId, Utils.GetTimeStamp());
        SendRPC(pc);

        pc?.MyPhysics?.RpcBootFromVent(vent.Id);
        pc.Notify(GetString("WraithInvisStateOut"));
    }
    public override string GetLowerText(PlayerControl pc, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (pc == null || !GameStates.IsInTask || !PlayerControl.LocalPlayer.IsAlive()) return "";
        var str = new StringBuilder();
        if (IsInvis(pc.PlayerId))
        {
            var remainTime = InvisTime[pc.PlayerId] + (long)WraithDuration.GetFloat() - Utils.GetTimeStamp();
            str.Append(string.Format(GetString("WraithInvisStateCountdown"), remainTime + 1));
        }
        else if (lastTime.TryGetValue(pc.PlayerId, out var time))
        {
            var cooldown = time + (long)WraithCooldown.GetFloat() - Utils.GetTimeStamp();
            str.Append(string.Format(GetString("WraithInvisCooldownRemain"), cooldown + 1));
        }
        else
        {
            str.Append(GetString("WraithCanVent"));
        }
        return str.ToString();
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("KillButtonText"));
        hud.ImpostorVentButton.OverrideText(GetString(IsInvis(PlayerControl.LocalPlayer.PlayerId) ? "WraithRevertVentButtonText" : "WraithVentButtonText"));
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!IsInvis(killer.PlayerId)) return true;

        RPC.PlaySoundRPC(Sounds.KillSound, killer.PlayerId);
        killer.RpcGuardAndKill(target);
        killer.SetKillCooldown();

        target.RpcMurderPlayer(target);
        target.SetRealKiller(killer);
        return false;
    }
    public override Sprite ImpostorVentButtonSprite(PlayerControl player) => CustomButton.Get("invisible");
}
