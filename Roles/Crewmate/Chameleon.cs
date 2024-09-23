using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System.Text;
using UnityEngine;
using TOHE.Roles.Core;
using static TOHE.Utils;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Chameleon : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 7600;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Chameleon);
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem ChameleonCooldown;
    private static OptionItem ChameleonDuration;
    private static OptionItem UseLimitOpt;

    private int VentedId;
    private long InvisCooldown;
    private long InvisDuration;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Chameleon);
        ChameleonCooldown = FloatOptionItem.Create(Id + 2, "ChameleonCooldown", new(1f, 60f, 1f), 30f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Chameleon])
            .SetValueFormat(OptionFormat.Seconds);
        ChameleonDuration = FloatOptionItem.Create(Id + 4, "ChameleonDuration", new(1f, 30f, 1f), 15f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Chameleon])
            .SetValueFormat(OptionFormat.Seconds);
        UseLimitOpt = IntegerOptionItem.Create(Id + 5, "AbilityUseLimit", new(0, 20, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Chameleon])
            .SetValueFormat(OptionFormat.Times);
        ChameleonAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 6, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Chameleon])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        InvisCooldown = -1;
        InvisDuration = -1;
        VentedId = -1;
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(UseLimitOpt.GetInt());
    }
    private void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, _Player.GetClientId());
        writer.WriteNetObject(_Player);
        writer.Write(InvisCooldown.ToString());
        writer.Write(InvisDuration.ToString());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        InvisCooldown = -1;
        InvisDuration = -1;

        long cooldown = long.Parse(reader.ReadString());
        long invis = long.Parse(reader.ReadString());

        if (cooldown > 0) InvisCooldown = cooldown;
        if (invis > 0) InvisDuration = invis;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = ChameleonCooldown.GetFloat() + 1f;
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }

    private bool CanGoInvis() => InvisDuration == -1 && InvisCooldown == -1;

    private bool IsInvis() => InvisDuration != -1;
   
    public override void OnReportDeadBody(PlayerControl y, NetworkedPlayerInfo x)
    {
        if (_Player == null || !IsInvis()) return;

        _Player.MyPhysics?.RpcBootFromVent(VentedId);

        InvisCooldown = -1;
        InvisDuration = -1;
        SendRPC();
    }
    public override void AfterMeetingTasks()
    {
        InvisCooldown = -1;
        InvisDuration = -1;

        if (!_Player.IsAlive()) return;

        InvisCooldown = GetTimeStamp();
        SendRPC();
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (lowLoad || !player.IsAlive()) return;
        var chameleon = player;
        var needSync = false;

        if (InvisCooldown != -1 && (InvisCooldown + (long)ChameleonCooldown.GetFloat() - nowTime) < 0)
        {
            InvisCooldown = -1;
            if (!player.IsModded()) player.Notify(GetString("ChameleonCanVent"));
            needSync = true;
        }
        if (InvisDuration != -1)
        {
            var remainTime = InvisDuration + (long)ChameleonDuration.GetFloat() - nowTime;

            if (remainTime <= 0)
            {
                InvisCooldown = nowTime;
                chameleon?.MyPhysics?.RpcBootFromVent(VentedId);
                chameleon.Notify(GetString("ChameleonInvisStateOut"));

                needSync = true;
                InvisDuration = -1;
            }
            else if (remainTime <= 10)
            {
                if (!chameleon.IsModded())
                    chameleon.Notify(string.Format(GetString("ChameleonInvisStateCountdown"), remainTime), sendInLog: false);
            }
        }

        if (needSync)
        {
            SendRPC();
        }
    }
    public override void OnCoEnterVent(PlayerPhysics physics, int ventId)
    {
        var chameleon = physics.myPlayer;
        var chameleonId = chameleon.Data.PlayerId;

        if (!AmongUsClient.Instance.AmHost || IsInvis()) return;

        _ = new LateTask(() =>
        {
            if (CanGoInvis())
            {
                if (chameleon.GetAbilityUseLimit() >= 1)
                {
                    VentedId = ventId;

                    physics.RpcBootFromVentDesync(ventId, chameleon);

                    InvisDuration = GetTimeStamp();
                    chameleon.RpcRemoveAbilityUse();
                    SendRPC();

                    chameleon.Notify(GetString("ChameleonInvisState"), ChameleonDuration.GetFloat());
                }
                else
                {
                    chameleon.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
                }
            }
            else
            {
                //__instance.myPlayer.MyPhysics.RpcBootFromVent(ventId);
                chameleon.Notify(GetString("ChameleonInvisInCooldown"));
            }
        }, 0.8f, "Chameleon Vent");
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (!IsInvis()) return;

        InvisDuration = GetTimeStamp();
        SendRPC();

        pc?.MyPhysics?.RpcBootFromVent(vent.Id);
        pc.Notify(GetString("ChameleonInvisStateOut"));
    }
    public override string GetLowerText(PlayerControl pc, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        // Only for modded
        if (pc == null || !isForHud || isForMeeting || !pc.IsAlive()) return string.Empty;

        var str = new StringBuilder();
        if (IsInvis())
        {
            var remainTime = InvisDuration + (long)ChameleonDuration.GetFloat() - GetTimeStamp();
            str.Append(string.Format(GetString("ChameleonInvisStateCountdown"), remainTime + 1));
        }
        else if (InvisCooldown != 1)
        {
            var cooldown = InvisCooldown + (long)ChameleonCooldown.GetFloat() - GetTimeStamp();
            str.Append(string.Format(GetString("ChameleonInvisCooldownRemain"), cooldown + 1));
        }
        else
        {
            str.Append(GetString("ChameleonCanVent"));
        }
        return str.ToString();
    }

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (!IsInvis()) return true;
        target?.MyPhysics?.RpcBootFromVent(VentedId);
        return true;
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.AbilityButton.OverrideText(GetString(IsInvis() ? "ChameleonRevertDisguise" : "ChameleonDisguise"));
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("invisible");
}