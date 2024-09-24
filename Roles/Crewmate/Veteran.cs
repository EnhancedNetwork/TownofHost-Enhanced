﻿using AmongUs.GameOptions;
using System;
using System.Text;
using UnityEngine;
using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;
using TOHE.Roles.Core;

namespace TOHE.Roles.Crewmate;

internal class Veteran : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 11350;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Veteran);
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem VeteranSkillCooldown;
    private static OptionItem VeteranSkillDuration;
    private static OptionItem VeteranSkillMaxOfUseage;
    private static OptionItem VeteranAbilityUseGainWithEachTaskCompleted;

    private static readonly Dictionary<byte, long> VeteranInProtect = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Veteran);
        VeteranSkillCooldown = FloatOptionItem.Create(Id + 10, "VeteranSkillCooldown", new(1f, 180f, 1f), 20f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Veteran])
            .SetValueFormat(OptionFormat.Seconds);
        VeteranSkillDuration = FloatOptionItem.Create(Id + 11, "VeteranSkillDuration", new(1f, 180f, 1f), 20f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Veteran])
            .SetValueFormat(OptionFormat.Seconds);
        VeteranSkillMaxOfUseage = IntegerOptionItem.Create(Id + 12, "VeteranSkillMaxOfUseage", new(0, 20, 1), 10, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Veteran])
            .SetValueFormat(OptionFormat.Times);
        VeteranAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 13, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Veteran])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        VeteranInProtect.Clear();
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = VeteranSkillMaxOfUseage.GetInt();
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = VeteranSkillCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.IsAlive())
            AbilityLimit += VeteranAbilityUseGainWithEachTaskCompleted.GetFloat();
        SendSkillRPC();
        
        return true;
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        var killerRole = killer.GetCustomRole();
        // Not should kill
        if (killerRole is CustomRoles.Taskinator
            or CustomRoles.Crusader
            or CustomRoles.Bodyguard
            or CustomRoles.Deputy)
            return true;

        if (killer.PlayerId != target.PlayerId && VeteranInProtect.TryGetValue(target.PlayerId, out var time))
            if (time + VeteranSkillDuration.GetInt() >= GetTimeStamp())
            {
                if (killer.Is(CustomRoles.Pestilence))
                {
                    killer.RpcMurderPlayer(target);
                    target.SetRealKiller(killer);
                    Logger.Info($"{killer.GetRealName()} kill {target.GetRealName()} because killer Pestilence", "Veteran");
                    return false;
                }
                else if (killer.Is(CustomRoles.Jinx))
                {
                    target.RpcCheckAndMurder(killer);
                    Logger.Info($"{killer.GetRealName()} is Jinx try kill {target.GetRealName()} but it is canceled", "Veteran");
                    return false;
                }
                else
                {
                    target.RpcMurderPlayer(killer);
                    killer.SetRealKiller(target);
                    Logger.Info($"{target.GetRealName()} kill {killer.GetRealName()}", "Veteran");
                    return false;
                }
            }
        return true;
    }
    public override void OnFixedUpdateLowLoad(PlayerControl pc)
    {
        if (VeteranInProtect.TryGetValue(pc.PlayerId, out var vtime) && vtime + VeteranSkillDuration.GetInt() < GetTimeStamp())
        {
            VeteranInProtect.Remove(pc.PlayerId);

            if (!DisableShieldAnimations.GetBool())
            {
                pc.RpcGuardAndKill();
            }
            else
            {
                pc.RpcResetAbilityCooldown();
            }

            pc.Notify(string.Format(GetString("VeteranOffGuard"), AbilityLimit));
        }
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        // Ability use limit reached
        if (AbilityLimit <= 0)
        {
            pc.Notify(GetString("VeteranMaxUsage"));
            return;
        }

        // Use ability
        if (!VeteranInProtect.ContainsKey(pc.PlayerId))
        {
            VeteranInProtect.Remove(pc.PlayerId);
            VeteranInProtect.Add(pc.PlayerId, GetTimeStamp(DateTime.Now));
            AbilityLimit -= 1;
            SendSkillRPC();
            if (!DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
            pc.RPCPlayCustomSound("Gunload");
            pc.Notify(GetString("VeteranOnGuard"), VeteranSkillDuration.GetFloat());
        }
    }
    public override bool CheckBootFromVent(PlayerPhysics physics, int ventId)
        => AbilityLimit < 1;

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) => VeteranInProtect.Clear();
    
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var taskState2 = Main.PlayerStates?[playerId].TaskState;
        Color TextColor2;
        var TaskCompleteColor2 = Color.green;
        var NonCompleteColor2 = Color.yellow;
        var NormalColor2 = taskState2.IsTaskFinished ? TaskCompleteColor2 : NonCompleteColor2;
        TextColor2 = comms ? Color.gray : NormalColor2;
        string Completed2 = comms ? "?" : $"{taskState2.CompletedTasksCount}";
        Color TextColor21;
        if (AbilityLimit < 1) TextColor21 = Color.red;
        else TextColor21 = Color.white;
        ProgressText.Append(ColorString(TextColor2, $"({Completed2}/{taskState2.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor21, $" <color=#ffffff>-</color> {Math.Round(AbilityLimit, 1)}"));
        return ProgressText.ToString();
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.AbilityButton.buttonLabelText.text = GetString("VeteranVentButtonText");
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Veteran");
}
