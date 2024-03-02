using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TOHE.Options;
using UnityEngine;
using static TOHE.Utils;
using static TOHE.Translator;
using AmongUs.GameOptions;

namespace TOHE.Roles.Crewmate
{
    internal class Lighter : RoleBase
    {
        public const int Id = 8400;
        public static bool On = false;

        public static Dictionary<byte, float> LighterNumOfUsed = [];
        public static Dictionary<byte, long> lighter = [];
        public override bool IsEnable => On;
        public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

        public static OptionItem LighterVisionNormal;
        public static OptionItem LighterVisionOnLightsOut;
        public static OptionItem LighterSkillCooldown;
        public static OptionItem LighterSkillDuration;
        public static OptionItem LighterSkillMaxOfUseage;
        public static OptionItem LighterAbilityUseGainWithEachTaskCompleted;

        public static void SetupCustomOptions()
        {
            SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Lighter, 1);
            LighterSkillCooldown = FloatOptionItem.Create(Id + 10, "LighterSkillCooldown", new(1f, 180f, 1f), 25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
                .SetValueFormat(OptionFormat.Seconds);
            LighterSkillDuration = FloatOptionItem.Create(Id + 11, "LighterSkillDuration", new(1f, 180f, 1f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
                .SetValueFormat(OptionFormat.Seconds);
            LighterVisionNormal = FloatOptionItem.Create(Id + 12, "LighterVisionNormal", new(0f, 5f, 0.05f), 1.35f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
                .SetValueFormat(OptionFormat.Multiplier);
            LighterVisionOnLightsOut = FloatOptionItem.Create(Id +13, "LighterVisionOnLightsOut", new(0f, 5f, 0.05f), 0.5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
                .SetValueFormat(OptionFormat.Multiplier);
            LighterSkillMaxOfUseage = IntegerOptionItem.Create(Id + 14, "AbilityUseLimit", new(0, 180, 1), 4, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
                .SetValueFormat(OptionFormat.Times);
            LighterAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 15, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
                .SetValueFormat(OptionFormat.Times);
        }
        public override void Init()
        {
            lighter = [];
            LighterNumOfUsed = [];
        }
        public override void Add(byte playerId)
        {
            LighterNumOfUsed.Add(playerId, Lighter.LighterSkillMaxOfUseage.GetInt());
        }
        public override void Remove(byte playerId)
        {
            LighterNumOfUsed.Remove(playerId);
        }
        public override void OnFixedUpdateLowLoad(PlayerControl pc)
        {
            if (lighter.TryGetValue(pc.PlayerId, out var ltime) && ltime + LighterSkillDuration.GetInt() < Utils.GetTimeStamp())
            {
                lighter.Remove(pc.PlayerId);
                if (!Options.DisableShieldAnimations.GetBool())
                {
                    pc.RpcGuardAndKill();
                }
                else
                {
                    pc.RpcResetAbilityCooldown();
                }
                pc.Notify(GetString("LighterSkillStop"));
                pc.MarkDirtySettings();
            }
        }
        public override void OnEnterVent(PlayerControl pc, Vent vent)
        {
            if (LighterNumOfUsed[pc.PlayerId] >= 1)
            {
                lighter.Remove(pc.PlayerId);
                lighter.Add(pc.PlayerId, Utils.GetTimeStamp());
                if (!Options.DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
                pc.Notify(GetString("LighterSkillInUse"), LighterSkillDuration.GetFloat());
                LighterNumOfUsed[pc.PlayerId] -= 1;
                pc.MarkDirtySettings();
            }
            else
            {
                pc.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
            }
        }
        public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target) => lighter.Clear();
        public override string GetProgressText(byte playerId, bool comms)
        {
            var ProgressText = new StringBuilder();
            var taskState14 = Main.PlayerStates?[playerId].TaskState;
            Color TextColor14;
            var TaskCompleteColor14 = Color.green;
            var NonCompleteColor14 = Color.yellow;
            var NormalColor14 = taskState14.IsTaskFinished ? TaskCompleteColor14 : NonCompleteColor14;
            TextColor14 = comms ? Color.gray : NormalColor14;
            string Completed14 = comms ? "?" : $"{taskState14.CompletedTasksCount}";
            Color TextColor141;
            if (LighterNumOfUsed[playerId] < 1) TextColor141 = Color.red;
            else TextColor141 = Color.white;
            ProgressText.Append(ColorString(TextColor14, $"({Completed14}/{taskState14.AllTasksCount})"));
            ProgressText.Append(ColorString(TextColor141, $" <color=#ffffff>-</color> {Math.Round(LighterNumOfUsed[playerId], 1)})"));
            return ProgressText.ToString();
        }
        public override void OnTaskComplete(PlayerControl pc, int completedTaskCount, int totalTaskCount)
        {
            if (!pc.IsAlive()) return;
            LighterNumOfUsed[pc.PlayerId] += LighterAbilityUseGainWithEachTaskCompleted.GetFloat();
        }
        public override void ApplyGameOptions(IGameOptions opt, byte playerId)
        {
            AURoleOptions.EngineerInVentMaxTime = 1;
            AURoleOptions.EngineerCooldown = Lighter.LighterSkillCooldown.GetFloat();
            if (Lighter.lighter.Count > 0)
            {
                opt.SetVision(false);
                if (Utils.IsActive(SystemTypes.Electrical)) opt.SetFloat(FloatOptionNames.CrewLightMod, Lighter.LighterVisionOnLightsOut.GetFloat() * 5);
                else opt.SetFloat(FloatOptionNames.CrewLightMod, Lighter.LighterVisionNormal.GetFloat());
            }
        }
        public override void SetAbilityButtonText(HudManager hud, byte id)
        {
            hud.ReportButton.OverrideText(GetString("ReportButtonText"));
            hud.AbilityButton.buttonLabelText.text = GetString("LighterVentButtonText");
        }
        public override Sprite AbilityButtonSprite => CustomButton.Get("Lighter");
    }
}
