using AmongUs.GameOptions;
using System;
using System.Text;
using UnityEngine;
using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Veteran : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 11350;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    //==================================================================\\

    private static OptionItem VeteranSkillCooldown;
    private static OptionItem VeteranSkillDuration;
    private static OptionItem VeteranSkillMaxOfUseage;
    private static OptionItem VeteranAbilityUseGainWithEachTaskCompleted;

    private static readonly Dictionary<byte, long> VeteranInProtect = [];
    private static readonly Dictionary<byte, float> VeteranNumOfUsed = [];

    public static void SetupCustomOptions()
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
        playerIdList.Clear();
        VeteranInProtect.Clear();
        VeteranNumOfUsed.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        VeteranNumOfUsed.Add(playerId, VeteranSkillMaxOfUseage.GetInt());
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = VeteranSkillCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override void OnTaskComplete(PlayerControl pc, int completedTaskCount, int totalTaskCount)
    {
        if (!pc.IsAlive()) return;
        VeteranNumOfUsed[pc.PlayerId] += VeteranAbilityUseGainWithEachTaskCompleted.GetFloat();
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (VeteranInProtect.ContainsKey(target.PlayerId) && killer.PlayerId != target.PlayerId)
            if (VeteranInProtect[target.PlayerId] + VeteranSkillDuration.GetInt() >= GetTimeStamp())
            {
                if (!killer.Is(CustomRoles.Pestilence))
                {
                    killer.SetRealKiller(target);
                    target.RpcMurderPlayer(killer);
                    Logger.Info($"{target.GetRealName()} 老兵反弹击杀：{killer.GetRealName()}", "Veteran Kill");
                    return false;
                }
                if (killer.Is(CustomRoles.Pestilence))
                {
                    target.SetRealKiller(killer);
                    killer.RpcMurderPlayer(target);
                    Logger.Info($"{target.GetRealName()} 老兵反弹击杀：{target.GetRealName()}", "Pestilence Reflect");
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

            pc.Notify(string.Format(GetString("VeteranOffGuard"), VeteranNumOfUsed[pc.PlayerId]));
        }
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (!VeteranInProtect.ContainsKey(pc.PlayerId))
        {
            VeteranInProtect.Remove(pc.PlayerId);
            VeteranInProtect.Add(pc.PlayerId, GetTimeStamp(DateTime.Now));
            VeteranNumOfUsed[pc.PlayerId] -= 1;
            if (!DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
            pc.RPCPlayCustomSound("Gunload");
            pc.Notify(GetString("VeteranOnGuard"), VeteranSkillDuration.GetFloat());
        }
        if (VeteranNumOfUsed[pc.PlayerId] >= 0) pc.Notify(GetString("VeteranMaxUsage"));
    }
    public override bool CheckBootFromVent(PlayerPhysics physics, int ventId)
        => VeteranNumOfUsed.TryGetValue(physics.myPlayer.PlayerId, out var count) && count < 1;

    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target) => VeteranInProtect.Clear();
    
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
        if (VeteranNumOfUsed[playerId] < 1) TextColor21 = Color.red;
        else TextColor21 = Color.white;
        ProgressText.Append(ColorString(TextColor2, $"({Completed2}/{taskState2.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor21, $" <color=#ffffff>-</color> {Math.Round(VeteranNumOfUsed[playerId], 1)}"));
        return ProgressText.ToString();
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.AbilityButton.buttonLabelText.text = GetString("VeteranVentButtonText");
    }
    public override Sprite ImpostorVentButtonSprite(PlayerControl player) => CustomButton.Get("Veteran");
}
