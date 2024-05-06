using Hazel;
using System.Text;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Swooper : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 4700;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem SwooperCooldown;
    private static OptionItem SwooperDuration;
    private static OptionItem SwooperVentNormallyOnCooldown;

    private static Dictionary<byte, long> InvisTime = [];
    private static Dictionary<byte, long> lastTime = [];
    private static Dictionary<byte, int> ventedId = [];

    private static long lastFixedTime = 0;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Swooper);
        SwooperCooldown = FloatOptionItem.Create(Id + 2, "SwooperCooldown", new(1f, 180f, 1f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Swooper])
            .SetValueFormat(OptionFormat.Seconds);
        SwooperDuration = FloatOptionItem.Create(Id + 4, "SwooperDuration", new(1f, 60f, 1f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Swooper])
            .SetValueFormat(OptionFormat.Seconds);
        SwooperVentNormallyOnCooldown = BooleanOptionItem.Create(Id + 5, "SwooperVentNormallyOnCooldown", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Swooper]);
    }
    public override void Init()
    {
        playerIdList.Clear();
        InvisTime.Clear();
        lastTime.Clear();
        ventedId.Clear();
        lastFixedTime = 0;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    private static void SendRPC(PlayerControl pc)
    {
        if (pc.AmOwner) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, pc.GetClientId());
        writer.WritePacked((int)CustomRoles.Swooper);
        writer.Write((InvisTime.TryGetValue(pc.PlayerId, out var x) ? x : -1).ToString());
        writer.Write((lastTime.TryGetValue(pc.PlayerId, out var y) ? y : -1).ToString());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        InvisTime = [];
        lastTime = [];
        long invis = long.Parse(reader.ReadString());
        long last = long.Parse(reader.ReadString());
        if (invis > 0) InvisTime.Add(PlayerControl.LocalPlayer.PlayerId, invis);
        if (last > 0) lastTime.Add(PlayerControl.LocalPlayer.PlayerId, last);
    }
    private static bool CanGoInvis(byte id)
        => GameStates.IsInTask && !InvisTime.ContainsKey(id) && !lastTime.ContainsKey(id);
    private static bool IsInvis(byte id) => InvisTime.ContainsKey(id);

    public override void OnEnterVent(PlayerControl swooper, Vent vent)
    {
        var swooperId = swooper.PlayerId;
        if (!IsInvis(swooperId)) return;

        InvisTime.Remove(swooperId);
        lastTime.Add(swooperId, Utils.GetTimeStamp());
        SendRPC(swooper);

        swooper?.MyPhysics?.RpcBootFromVent(vent.Id);
        swooper.Notify(GetString("SwooperInvisStateOut"));
    }
    public override void OnCoEnterVent(PlayerPhysics physics, int ventId)
    {
        var swooper = physics.myPlayer;
        var swooperId = swooper.PlayerId;

        if (!AmongUsClient.Instance.AmHost || IsInvis(swooperId)) return;
        
        _ = new LateTask(() =>
        {
            if (CanGoInvis(swooperId))
            {
                ventedId.Remove(swooperId);
                ventedId.Add(swooperId, ventId);

                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(physics.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, swooper.GetClientId());
                writer.WritePacked(ventId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);

                InvisTime.Add(swooperId, Utils.GetTimeStamp());
                SendRPC(swooper);
                
                swooper.Notify(GetString("SwooperInvisState"), SwooperDuration.GetFloat());
            }
            else
            {
                if (!SwooperVentNormallyOnCooldown.GetBool())
                {
                    physics?.RpcBootFromVent(ventId);
                    swooper.Notify(GetString("SwooperInvisInCooldown"));
                }
            }
        }, 0.5f, "Swooper Vent");
    }

    public override void OnFixedUpdateLowLoad(PlayerControl player)
    {
        var now = Utils.GetTimeStamp();
        var playerId = player.PlayerId;

        if (lastTime.TryGetValue(playerId, out var time) && time + (long)SwooperCooldown.GetFloat() < now)
        {
            lastTime.Remove(playerId);
            if (!player.IsModClient()) player.Notify(GetString("SwooperCanVent"));
            SendRPC(player);
        }

        if (lastFixedTime != now)
        {
            lastFixedTime = now;
            Dictionary<byte, long> newList = [];
            List<byte> refreshList = [];

            foreach (var swoopInfo in InvisTime)
            {
                var swooperId = swoopInfo.Key;
                var swooper = Utils.GetPlayerById(swooperId);
                if (swooper == null) continue;

                var remainTime = swoopInfo.Value + (long)SwooperDuration.GetFloat() - now;
                
                if (remainTime < 0)
                {
                    lastTime.Add(swooperId, now);
                    
                    swooper?.MyPhysics?.RpcBootFromVent(ventedId.TryGetValue(swooperId, out var id) ? id : Main.LastEnteredVent[swooperId].Id);
                    
                    ventedId.Remove(swooperId);
                    SendRPC(swooper);

                    swooper.Notify(GetString("SwooperInvisStateOut"));
                    continue;
                }
                else if (remainTime <= 10)
                {
                    if (!swooper.IsModClient())
                        swooper.Notify(string.Format(GetString("SwooperInvisStateCountdown"), remainTime + 1), sendInLog: false);
                }
                newList.Add(swooperId, swoopInfo.Value);
            }
            InvisTime.Where(x => !newList.ContainsKey(x.Key)).Do(x => refreshList.Add(x.Key));
            InvisTime = newList;
            refreshList.Do(x => SendRPC(Utils.GetPlayerById(x)));
        }
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!IsInvis(killer.PlayerId) || target.Is(CustomRoles.Bait)) return true;

        killer.RpcGuardAndKill(target);
        killer.SetKillCooldown();

        target.SetRealKiller(killer);
        target.RpcCheckAndMurder(target);
        return false;
    }

    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    {
        lastTime = [];
        InvisTime = [];

        foreach (var swooperId in playerIdList.ToArray())
        {
            if (!ventedId.ContainsKey(swooperId)) continue;
            var swooper = Utils.GetPlayerById(swooperId);
            if (swooper == null) continue;

            swooper?.MyPhysics?.RpcBootFromVent(ventedId.TryGetValue(swooperId, out var id) ? id : Main.LastEnteredVent[swooperId].Id);
            SendRPC(swooper);
        }

        ventedId = [];
    }
    public override void AfterMeetingTasks()
    {
        lastTime = [];
        InvisTime = [];

        foreach (var swooperId in playerIdList.ToArray())
        {
            var swooper = Utils.GetPlayerById(swooperId);
            if (swooper == null) continue;

            lastTime.Add(swooperId, Utils.GetTimeStamp());
            SendRPC(swooper);
        }
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        // Only for modded
        if (seer == null || !isForHud || isForMeeting || !seer.IsAlive()) return string.Empty;
        
        var str = new StringBuilder();
        var seerId = seer.PlayerId;

        if (IsInvis(seerId))
        {
            var remainTime = InvisTime[seerId] + (long)SwooperDuration.GetFloat() - Utils.GetTimeStamp();
            str.Append(string.Format(GetString("SwooperInvisStateCountdown"), remainTime + 1));
        }
        else if (lastTime.TryGetValue(seerId, out var time))
        {
            var cooldown = time + (long)SwooperCooldown.GetFloat() - Utils.GetTimeStamp();
            str.Append(string.Format(GetString("SwooperInvisCooldownRemain"), cooldown + 1));
        }
        else
        {
            str.Append(GetString("SwooperCanVent"));
        }
        return str.ToString();
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.ImpostorVentButton?.OverrideText(GetString(IsInvis(playerId) ? "SwooperRevertVentButtonText" : "SwooperVentButtonText"));
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("invisible");
}