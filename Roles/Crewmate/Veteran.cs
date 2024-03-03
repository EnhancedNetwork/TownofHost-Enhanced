using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOHE.Roles.Core;
using static TOHE.Options;
using UnityEngine;
using static TOHE.Translator;
using static TOHE.Utils;
using TOHE.Modules;

namespace TOHE.Roles.Crewmate
{
    internal class Veteran : RoleBase
    {
        private const int Id = 11350;
        private static bool On = false;
        public static Dictionary<byte, long> VeteranInProtect = [];
        public static Dictionary<byte, float> VeteranNumOfUsed = [];
        public override bool IsEnable => On;
        public static bool HasEnabled => CustomRoles.Veteran.IsClassEnable();
        public override CustomRoles ThisRoleBase => CustomRoles.Engineer;

        public static OptionItem VeteranSkillCooldown;
        public static OptionItem VeteranSkillDuration;
        public static OptionItem VeteranSkillMaxOfUseage;
        public static OptionItem VeteranAbilityUseGainWithEachTaskCompleted;
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
            VeteranInProtect = [];
            VeteranNumOfUsed = [];
            On = false;
        }
        public override void Add(byte playerId)
        {

            VeteranNumOfUsed.Add(playerId, VeteranSkillMaxOfUseage.GetInt());
            On = true;
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
            if (Veteran.VeteranInProtect.ContainsKey(target.PlayerId) && killer.PlayerId != target.PlayerId)
                if (Veteran.VeteranInProtect[target.PlayerId] + Veteran.VeteranSkillDuration.GetInt() >= Utils.GetTimeStamp())
                {
                    if (!killer.Is(CustomRoles.Pestilence))
                    {
                        killer.SetRealKiller(target);
                        target.RpcMurderPlayerV3(killer);
                        Logger.Info($"{target.GetRealName()} 老兵反弹击杀：{killer.GetRealName()}", "Veteran Kill");
                        return false;
                    }
                    if (killer.Is(CustomRoles.Pestilence))
                    {
                        target.SetRealKiller(killer);
                        killer.RpcMurderPlayerV3(target);
                        Logger.Info($"{target.GetRealName()} 老兵反弹击杀：{target.GetRealName()}", "Pestilence Reflect");
                        return false;
                    }
                }
            return true;
        }
        public override void OnFixedUpdateLowLoad(PlayerControl pc)
        {
            if (Veteran.VeteranInProtect.TryGetValue(pc.PlayerId, out var vtime) && vtime + Veteran.VeteranSkillDuration.GetInt() < Utils.GetTimeStamp())
            {
                Veteran.VeteranInProtect.Remove(pc.PlayerId);

                if (!Options.DisableShieldAnimations.GetBool())
                {
                    pc.RpcGuardAndKill();
                }
                else
                {
                    pc.RpcResetAbilityCooldown();
                }

                pc.Notify(string.Format(GetString("VeteranOffGuard"), Veteran.VeteranNumOfUsed[pc.PlayerId]));
            }
        }
        public override void OnEnterVent(PlayerControl pc, Vent vent)
        {
            if (!Veteran.VeteranInProtect.ContainsKey(pc.PlayerId))
            {
                Veteran.VeteranInProtect.Remove(pc.PlayerId);
                Veteran.VeteranInProtect.Add(pc.PlayerId, Utils.GetTimeStamp(DateTime.Now));
                Veteran.VeteranNumOfUsed[pc.PlayerId] -= 1;
                if (!Options.DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
                pc.RPCPlayCustomSound("Gunload");
                pc.Notify(GetString("VeteranOnGuard"), Veteran.VeteranSkillDuration.GetFloat());
            }
            if (VeteranNumOfUsed[pc.PlayerId] >= 0) pc.Notify(GetString("VeteranMaxUsage"));
        }
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
            if (Veteran.VeteranNumOfUsed[playerId] < 1) TextColor21 = Color.red;
            else TextColor21 = Color.white;
            ProgressText.Append(ColorString(TextColor2, $"({Completed2}/{taskState2.AllTasksCount})"));
            ProgressText.Append(ColorString(TextColor21, $" <color=#ffffff>-</color> {Math.Round(Veteran.VeteranNumOfUsed[playerId], 1)}"));
            return ProgressText.ToString();
        }
        public override void SetAbilityButtonText(HudManager hud, byte id)
        {
            hud.AbilityButton.buttonLabelText.text = GetString("VeteranVentButtonText");
        }
        public override Sprite ImpostorVentButtonSprite => CustomButton.Get("Veteran");
    }
}
