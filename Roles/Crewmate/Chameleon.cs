using AmongUs.GameOptions;
using Hazel;
using System.Text;
using TOHE.Modules;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Chameleon : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Chameleon;
    private const int Id = 7600;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Chameleon);
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem ChameleonCooldown;
    private static OptionItem ChameleonDuration;
    private static OptionItem UseLimitOpt;

    private static readonly Dictionary<byte, int> ventedId = [];
    private static readonly Dictionary<byte, long> InvisCooldown = [];
    private static readonly Dictionary<byte, long> InvisDuration = [];

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
        OverrideTasksData.Create(Id + 7, TabGroup.CrewmateRoles, CustomRoles.Chameleon);
    }
    public override void Init()
    {
        InvisCooldown.Clear();
        InvisDuration.Clear();
        ventedId.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(UseLimitOpt.GetInt());
    }
    public static void SendRPC(PlayerControl pc)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetChameleonTimer, SendOption.Reliable, pc.GetClientId());
        writer.Write(pc.PlayerId);
        writer.Write((InvisCooldown.TryGetValue(pc.PlayerId, out var y) ? y : -1).ToString());
        writer.Write((InvisDuration.TryGetValue(pc.PlayerId, out var x) ? x : -1).ToString());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC_Custom(MessageReader reader)
    {
        byte playerId = reader.ReadByte();

        InvisCooldown.Clear();
        InvisDuration.Clear();
        long cooldown = long.Parse(reader.ReadString());
        long invis = long.Parse(reader.ReadString());
        if (cooldown > 0) InvisCooldown.Add(playerId, cooldown);
        if (invis > 0) InvisDuration.Add(playerId, invis);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = ChameleonCooldown.GetFloat() + 1f;
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }

    private static bool CanGoInvis(byte id)
        => GameStates.IsInTask && !InvisDuration.ContainsKey(id) && !InvisCooldown.ContainsKey(id);

    private static bool IsInvis(byte id) => InvisDuration.ContainsKey(id);

    public override void OnReportDeadBody(PlayerControl y, NetworkedPlayerInfo x)
    {
        foreach (var chameleonId in _playerIdList.ToArray())
        {
            if (!IsInvis(chameleonId)) continue;
            var chameleon = GetPlayerById(chameleonId);
            if (chameleon == null) return;

            chameleon?.MyPhysics?.RpcBootFromVent(ventedId.TryGetValue(chameleonId, out var id) ? id : Main.LastEnteredVent[chameleonId].Id);
            InvisDuration.Remove(chameleonId);
            ventedId.Remove(chameleonId);
            SendRPC(chameleon);
        }

        InvisCooldown.Clear();
        InvisDuration.Clear();
        ventedId.Clear();
    }
    public override void AfterMeetingTasks()
    {
        InvisCooldown.Clear();
        InvisDuration.Clear();

        foreach (var chameleonId in _playerIdList)
        {
            var chameleon = GetPlayerById(chameleonId);
            if (!chameleon.IsAlive()) continue;

            InvisCooldown.Add(chameleon.PlayerId, GetTimeStamp());
            SendRPC(chameleon);
        }
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (lowLoad) return;
        var playerId = player.PlayerId;
        var needSync = false;

        if (InvisCooldown.TryGetValue(playerId, out var oldTime) && (oldTime + (long)ChameleonCooldown.GetFloat() - nowTime) < 0)
        {
            InvisCooldown.Remove(playerId);
            if (!player.IsModded()) player.Notify(GetString("ChameleonCanVent"));
            needSync = true;
        }

        foreach (var chameleonInfo in InvisDuration)
        {
            var chameleonId = chameleonInfo.Key;
            var chameleon = chameleonId.GetPlayer();
            if (chameleon == null) continue;

            var remainTime = chameleonInfo.Value + (long)ChameleonDuration.GetFloat() - nowTime;

            if (remainTime < 0 || !chameleon.IsAlive())
            {
                chameleon?.MyPhysics?.RpcBootFromVent(ventedId.TryGetValue(chameleonId, out var id) ? id : Main.LastEnteredVent[chameleonId].Id);

                ventedId.Remove(chameleonId);

                InvisCooldown.Remove(chameleonId);
                InvisCooldown.Add(chameleonId, nowTime);

                chameleon.Notify(GetString("ChameleonInvisStateOut"));

                needSync = true;
                InvisDuration.Remove(chameleonId);
            }
            else if (remainTime <= 10)
            {
                if (!chameleon.IsModded())
                    chameleon.Notify(string.Format(GetString("ChameleonInvisStateCountdown"), remainTime), sendInLog: false);
            }
        }

        if (needSync)
        {
            SendRPC(player);
        }
    }
    public override void OnCoEnterVent(PlayerPhysics physics, int ventId)
    {
        var chameleon = physics.myPlayer;
        var chameleonId = chameleon.Data.PlayerId;

        if (!AmongUsClient.Instance.AmHost || IsInvis(chameleonId)) return;

        _ = new LateTask(() =>
        {
            if (CanGoInvis(chameleonId))
            {
                if (chameleon.GetAbilityUseLimit() >= 1)
                {
                    ventedId.Remove(chameleonId);
                    ventedId.Add(chameleonId, ventId);

                    physics.RpcBootFromVentDesync(ventId, chameleon);

                    InvisDuration.Remove(chameleonId);
                    InvisDuration.Add(chameleonId, GetTimeStamp());
                    SendRPC(chameleon);

                    chameleon.RpcRemoveAbilityUse();

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
        if (!IsInvis(pc.PlayerId)) return;

        InvisDuration.Remove(pc.PlayerId);
        InvisCooldown.Add(pc.PlayerId, GetTimeStamp());
        SendRPC(pc);

        pc?.MyPhysics?.RpcBootFromVent(vent.Id);
        pc.Notify(GetString("ChameleonInvisStateOut"));
    }
    public override string GetLowerText(PlayerControl pc, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        // Only for modded
        if (pc == null || !isForHud || isForMeeting || !pc.IsAlive()) return string.Empty;

        var str = new StringBuilder();
        if (IsInvis(pc.PlayerId))
        {
            var remainTime = InvisDuration[pc.PlayerId] + (long)ChameleonDuration.GetFloat() - GetTimeStamp();
            str.Append(string.Format(GetString("ChameleonInvisStateCountdown"), remainTime + 1));
        }
        else if (InvisCooldown.TryGetValue(pc.PlayerId, out var time))
        {
            var cooldown = time + (long)ChameleonCooldown.GetFloat() - GetTimeStamp();
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
        if (!IsInvis(killer.PlayerId)) return true;
        target?.MyPhysics?.RpcBootFromVent(ventedId.TryGetValue(target.PlayerId, out var id) ? id : Main.LastEnteredVent[target.PlayerId].Id);
        return true;
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.AbilityButton.OverrideText(GetString(IsInvis(PlayerControl.LocalPlayer.PlayerId) ? "ChameleonRevertDisguise" : "ChameleonDisguise"));
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("invisible");
}
