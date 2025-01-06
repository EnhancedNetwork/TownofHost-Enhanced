using AmongUs.GameOptions;
using Hazel;
using System;
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
    private static OptionItem ChameleonAbilityUseGainWithEachTaskCompleted;

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
    }
    public override void Init()
    {
        InvisCooldown.Clear();
        InvisDuration.Clear();
        ventedId.Clear();
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = UseLimitOpt.GetInt();
    }
    public void SendRPC(PlayerControl pc, bool isLimit = false)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetChameleonTimer, SendOption.Reliable, isLimit ? -1 : pc.GetClientId());
        writer.Write(pc.PlayerId);
        writer.Write(isLimit);
        if (isLimit)
        {
            writer.Write(AbilityLimit);
        }
        else
        {
            writer.Write((InvisCooldown.TryGetValue(pc.PlayerId, out var y) ? y : -1).ToString());
            writer.Write((InvisDuration.TryGetValue(pc.PlayerId, out var x) ? x : -1).ToString());
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC_Custom(MessageReader reader)
    {
        byte pid = reader.ReadByte();
        bool isLimit = reader.ReadBoolean();
        if (isLimit)
        {
            float limit = reader.ReadSingle();
            Main.PlayerStates[pid].RoleClass.AbilityLimit = limit;
        }
        else 
        {
            InvisCooldown.Clear();
            InvisDuration.Clear();
            long cooldown = long.Parse(reader.ReadString());
            long invis = long.Parse(reader.ReadString());
            if (cooldown > 0) InvisCooldown.Add(pid, cooldown);
            if (invis > 0) InvisDuration.Add(pid, invis);
        }
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
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.IsAlive())
        {
            AbilityLimit += ChameleonAbilityUseGainWithEachTaskCompleted.GetFloat();
            SendRPC(player, isLimit: true);
        }
        return true;
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
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
                if (AbilityLimit >= 1)
                {
                    ventedId.Remove(chameleonId);
                    ventedId.Add(chameleonId, ventId);

                    physics.RpcBootFromVentDesync(ventId, chameleon);

                    InvisDuration.Remove(chameleonId);
                    InvisDuration.Add(chameleonId, GetTimeStamp());
                    SendRPC(chameleon);

                    AbilityLimit--;
                    SendRPC(chameleon, isLimit: true);

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

    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var taskState13 = Main.PlayerStates?[playerId].TaskState;
        Color TextColor13;
        var TaskCompleteColor13 = Color.green;
        var NonCompleteColor13 = Color.yellow;
        var NormalColor13 = taskState13.IsTaskFinished ? TaskCompleteColor13 : NonCompleteColor13;
        TextColor13 = comms ? Color.gray : NormalColor13;
        string Completed13 = comms ? "?" : $"{taskState13.CompletedTasksCount}";
        Color TextColor131;
        if (AbilityLimit < 1) TextColor131 = Color.red;
        else TextColor131 = Color.white;
        ProgressText.Append(ColorString(TextColor13, $"({Completed13}/{taskState13.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor131, $" <color=#ffffff>-</color> {Math.Round(AbilityLimit, 1)}"));
        return ProgressText.ToString();
    }
}