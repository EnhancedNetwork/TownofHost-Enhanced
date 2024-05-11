using AsmResolver.DotNet.Signatures;
using Hazel;
using System.Text;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Swooper : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 4700;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Swooper);
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem SwooperCooldown;
    private static OptionItem SwooperDuration;
    private static OptionItem SwooperVentNormallyOnCooldown;

    private long? InvisTime;
    private long? lastTime;
    private int? ventedId;

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
        lastFixedTime = 0;
    }
    private  void SendRPC(PlayerControl pc)
    {
        if (pc.AmOwner) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, pc.GetClientId());
        writer.WritePacked((int)CustomRoles.Swooper);
        writer.Write((InvisTime).ToString());
        writer.Write((lastTime).ToString());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        InvisTime = new();
        lastTime = new();
        long invis = long.Parse(reader.ReadString());
        long last = long.Parse(reader.ReadString());
        if (invis > 0) InvisTime = invis;
        if (last > 0) lastTime = last;
    }
    private bool CanGoInvis(byte id)
        => GameStates.IsInTask && InvisTime == null && lastTime == null;
    private bool IsInvis(byte id) => InvisTime != null;

    public override void OnEnterVent(PlayerControl swooper, Vent vent)
    {
        var swooperId = swooper.PlayerId;
        if (!IsInvis(swooperId)) return;

        InvisTime = new();
        lastTime = Utils.GetTimeStamp();
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
                ventedId = ventId;

                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(physics.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, swooper.GetClientId());
                writer.WritePacked(ventId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);

                InvisTime = Utils.GetTimeStamp();
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

    private static Dictionary<byte, long> newList = [];
    private static List<byte> refreshList = [];
    public override void OnFixedUpdateLowLoad(PlayerControl player)
    {
        var now = Utils.GetTimeStamp();
        var playerId = player.PlayerId;

        if (lastTime + (long)SwooperCooldown.GetFloat() < now)
        {
            lastTime = new();
            if (!player.IsModClient()) player.Notify(GetString("SwooperCanVent"));
            SendRPC(player);
        }

        if (lastFixedTime != now)
        {
            lastFixedTime = now;


            var swooperId = _state.PlayerId;
            var swooper = Utils.GetPlayerById(swooperId);
            if (swooper == null) return;

            var remainTime = InvisTime + (long)SwooperDuration.GetFloat() - now;

            if (remainTime < 0)
            {
                lastTime = now;

                swooper?.MyPhysics?.RpcBootFromVent(ventedId != null ? ventedId.Value : Main.LastEnteredVent[swooperId].Id);

                ventedId = new();
                SendRPC(swooper);

                swooper.Notify(GetString("SwooperInvisStateOut"));
                return;
            }
            else if (remainTime <= 10)
            {
                if (!swooper.IsModClient())
                    swooper.Notify(string.Format(GetString("SwooperInvisStateCountdown"), remainTime + 1), sendInLog: false);
            }
            newList[player.PlayerId] = InvisTime.Value;
        }
        if (!newList.ContainsKey(playerId))
        {
            refreshList.Add(playerId);
        }
        else
            InvisTime = newList[playerId];

        refreshList.Do(x => SendRPC(Utils.GetPlayerById(x)));

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
        lastTime = new();
        InvisTime = new();

        if (ventedId == null) return;
        var swooper = _Player;
        if (swooper == null) return;

        swooper?.MyPhysics?.RpcBootFromVent(ventedId != null ? ventedId.Value : Main.LastEnteredVent[_state.PlayerId].Id);
        SendRPC(swooper);

        ventedId = new();
    }
    public override void AfterMeetingTasks()
    {
        lastTime = new();
        InvisTime = new();

        var swooper = _Player;
        if (swooper == null) return;

        lastTime = Utils.GetTimeStamp();
        SendRPC(swooper);

    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        // Only for modded
        if (seer == null || !isForHud || isForMeeting || !seer.IsAlive()) return string.Empty;
        
        var str = new StringBuilder();
        var seerId = seer.PlayerId;

        if (IsInvis(seerId))
        {
            var remainTime = InvisTime + (long)SwooperDuration.GetFloat() - Utils.GetTimeStamp();
            str.Append(string.Format(GetString("SwooperInvisStateCountdown"), remainTime + 1));
        }
        else if (lastTime != null)
        {
            var cooldown = lastTime + (long)SwooperCooldown.GetFloat() - Utils.GetTimeStamp();
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