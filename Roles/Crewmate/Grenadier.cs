using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TOHE.Utils;
using static TOHE.Options;
using static TOHE.Translator;
using HarmonyLib;
using TOHE.Modules;
using TOHE.Roles.Core.AssignManager;
using TOHE.Roles.Core;

namespace TOHE.Roles.Crewmate
{
    internal class Grenadier : RoleBase
    {
        private const int Id = 8200;
        public static bool On = false;
        public override bool IsEnable => On;
        public override CustomRoles ThisRoleBase => CustomRoles.Engineer;

        public static Dictionary<byte, long> GrenadierBlinding = [];
        public static Dictionary<byte, long> MadGrenadierBlinding = [];
        public static Dictionary<byte, float> GrenadierNumOfUsed = [];


        public static OptionItem GrenadierSkillCooldown;
        public static OptionItem GrenadierSkillDuration;
        public static OptionItem GrenadierCauseVision;
        public static OptionItem GrenadierCanAffectNeutral;
        public static OptionItem GrenadierSkillMaxOfUseage;
        public static OptionItem GrenadierAbilityUseGainWithEachTaskCompleted;
        public static void SetupCustomOptions() 
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Grenadier);
            GrenadierSkillCooldown = FloatOptionItem.Create(Id + 10, "GrenadierSkillCooldown", new(1f, 180f, 1f), 25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
                .SetValueFormat(OptionFormat.Seconds);
            GrenadierSkillDuration = FloatOptionItem.Create(Id + 11, "GrenadierSkillDuration", new(1f, 60f, 1f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
                .SetValueFormat(OptionFormat.Seconds);
            GrenadierCauseVision = FloatOptionItem.Create(Id + 12, "GrenadierCauseVision", new(0f, 5f, 0.05f), 0.3f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
                .SetValueFormat(OptionFormat.Multiplier);
            GrenadierCanAffectNeutral = BooleanOptionItem.Create(Id + 13, "GrenadierCanAffectNeutral", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier]);
            GrenadierSkillMaxOfUseage = IntegerOptionItem.Create(Id + 14, "GrenadierSkillMaxOfUseage", new(0, 20, 1), 2, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
                .SetValueFormat(OptionFormat.Times);
            GrenadierAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 15, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
                .SetValueFormat(OptionFormat.Times);
        }

        public override void Init()
        {
            GrenadierBlinding = [];
            MadGrenadierBlinding = [];
            GrenadierNumOfUsed = [];
            On = false;
        }
        public override void Add(byte playerId)
        {
            CustomRoleManager.OnFixedUpdateLowLoadOthers.Add(OnGrenaderFixOthers);
            GrenadierNumOfUsed.Add(playerId, GrenadierSkillMaxOfUseage.GetInt());
            On = true;
        }

        public override void ApplyGameOptions(IGameOptions opt, byte playerId)
        {
            AURoleOptions.EngineerCooldown = GrenadierSkillCooldown.GetFloat();
            AURoleOptions.EngineerInVentMaxTime = 1;
        }
        public override void OnTaskComplete(PlayerControl pc, int completedTaskCount, int totalTaskCount)
        {
            if(pc.IsAlive())
                GrenadierNumOfUsed[pc.PlayerId] += GrenadierAbilityUseGainWithEachTaskCompleted.GetFloat();
        }
        public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
        {
            GrenadierBlinding.Clear();
            MadGrenadierBlinding.Clear();
        }

        public override void OnEnterVent(PlayerControl pc, Vent vent)
        {
            if (GrenadierNumOfUsed[pc.PlayerId] >= 1)
            {
                if (pc.Is(CustomRoles.Madmate))
                {
                    MadGrenadierBlinding.Remove(pc.PlayerId);
                    MadGrenadierBlinding.Add(pc.PlayerId, Utils.GetTimeStamp());
                    Main.AllPlayerControls.Where(x => x.IsModClient()).Where(x => !x.GetCustomRole().IsImpostorTeam() && !x.Is(CustomRoles.Madmate)).Do(x => x.RPCPlayCustomSound("FlashBang"));
                }
                else
                {
                    GrenadierBlinding.Remove(pc.PlayerId);
                    GrenadierBlinding.Add(pc.PlayerId, Utils.GetTimeStamp());
                    Main.AllPlayerControls.Where(x => x.IsModClient()).Where(x => x.GetCustomRole().IsImpostor() || (x.GetCustomRole().IsNeutral() && GrenadierCanAffectNeutral.GetBool())).Do(x => x.RPCPlayCustomSound("FlashBang"));
                }
                if (!Options.DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
                pc.RPCPlayCustomSound("FlashBang");
                pc.Notify(GetString("GrenadierSkillInUse"), GrenadierSkillDuration.GetFloat());
                GrenadierNumOfUsed[pc.PlayerId] -= 1;
                MarkEveryoneDirtySettings();
            }
        }

        public static bool stopGrenadierSkill = false;
        public static bool stopMadGrenadierSkill = false;
        public override void OnFixedUpdateLowLoad(PlayerControl player)
        {
            if (stopGrenadierSkill || stopMadGrenadierSkill)
            {
                if (!Options.DisableShieldAnimations.GetBool())
                {
                    player.RpcGuardAndKill();
                }
                else
                {
                    player.RpcResetAbilityCooldown();
                }
                player.Notify(GetString("GrenadierSkillStop"));
                Utils.MarkEveryoneDirtySettings();
                stopGrenadierSkill = false;
                stopMadGrenadierSkill = false;
            }
        }
        private static void OnGrenaderFixOthers(PlayerControl pc)
        {
            if (!GrenadierBlinding.ContainsKey(pc.PlayerId) || !MadGrenadierBlinding.ContainsKey(pc.PlayerId)) return;
            
            if (Grenadier.GrenadierBlinding.TryGetValue(pc.PlayerId, out var grenadierTime) && grenadierTime + Grenadier.GrenadierSkillDuration.GetInt() < Utils.GetTimeStamp())
            {
                Grenadier.GrenadierBlinding.Remove(pc.PlayerId);
                Grenadier.stopGrenadierSkill = true;
            }
            if (Grenadier.MadGrenadierBlinding.TryGetValue(pc.PlayerId, out var madGrenadierTime) && madGrenadierTime + Grenadier.GrenadierSkillDuration.GetInt() < Utils.GetTimeStamp())
            {
                Grenadier.MadGrenadierBlinding.Remove(pc.PlayerId);
                Grenadier.stopMadGrenadierSkill = true;
            }
        }
        public override string GetProgressText(byte playerId, bool comms)
        {
            var ProgressText = new StringBuilder();
            var taskState3 = Main.PlayerStates?[playerId].TaskState;
            Color TextColor3;
            var TaskCompleteColor3 = Color.green;
            var NonCompleteColor3 = Color.yellow;
            var NormalColor3 = taskState3.IsTaskFinished ? TaskCompleteColor3 : NonCompleteColor3;
            TextColor3 = comms ? Color.gray : NormalColor3;
            string Completed3 = comms ? "?" : $"{taskState3.CompletedTasksCount}";
            Color TextColor31;
            if (GrenadierNumOfUsed[playerId] < 1) TextColor31 = Color.red;
            else TextColor31 = Color.white;
            ProgressText.Append(ColorString(TextColor3, $"({Completed3}/{taskState3.AllTasksCount})"));
            ProgressText.Append(ColorString(TextColor31, $" <color=#ffffff>-</color> {Math.Round(GrenadierNumOfUsed[playerId], 1)}"));
            return ProgressText.ToString();
        }

        public override void SetAbilityButtonText(HudManager hud, byte id)
        {
            hud.ReportButton.OverrideText(GetString("ReportButtonText"));
            hud.AbilityButton.buttonLabelText.text = GetString("GrenadierVentButtonText");
        }
    }
}
