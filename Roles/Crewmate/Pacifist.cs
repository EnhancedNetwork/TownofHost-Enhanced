using Il2CppSystem.CodeDom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static TOHE.Options;
using UnityEngine;
using static TOHE.Utils;
using TOHE.Roles.Core;
using TOHE.Roles.Impostor;
using static TOHE.Translator;
using HarmonyLib;
using TOHE.Modules;
using AmongUs.GameOptions;

namespace TOHE.Roles.Crewmate
{
    internal class Pacifist : RoleBase
    {
        public const int Id = 9200;
        public static bool On = false;

        public static Dictionary<byte, float> PacifistNumOfUsed = [];
        public override bool IsEnable => On;
        public static bool HasEnabled => CustomRoles.Pacifist.IsClassEnable();
        public override CustomRoles ThisRoleBase => CustomRoles.Engineer;

        public static OptionItem PacifistCooldown;
        public static OptionItem PacifistMaxOfUseage;
        public static OptionItem PacifistAbilityUseGainWithEachTaskCompleted;
        public static void SetupCustomOptions()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Pacifist);
            PacifistCooldown = FloatOptionItem.Create(Id + 10, "PacifistCooldown", new(1f, 180f, 1f), 30f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pacifist])
                .SetValueFormat(OptionFormat.Seconds);
            PacifistMaxOfUseage = IntegerOptionItem.Create(Id + 11, "PacifistMaxOfUseage", new(0, 20, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pacifist])
                .SetValueFormat(OptionFormat.Times);
            PacifistAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(9204, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pacifist])
                .SetValueFormat(OptionFormat.Times);
        }
        public override void Init()
        {
            PacifistNumOfUsed = [];
        }
        public override void Add(byte playerId)
        {
            PacifistNumOfUsed.Add(playerId, Pacifist.PacifistMaxOfUseage.GetInt());
        }
        public override void OnEnterVent(PlayerControl pc, Vent vent)
        {

            if (Pacifist.PacifistNumOfUsed[pc.PlayerId] < 1)
            {
                pc?.MyPhysics?.RpcBootFromVent(vent.Id);
                pc.Notify(GetString("PacifistMaxUsage"));
            }
            else
            {
                Pacifist.PacifistNumOfUsed[pc.PlayerId] -= 1;
                if (!Options.DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
                Main.AllAlivePlayerControls.Where(x =>
                pc.Is(CustomRoles.Madmate) ?
                (x.CanUseKillButton() && x.GetCustomRole().IsCrewmate()) :
                (x.CanUseKillButton())
                ).Do(x =>
                {
                    x.RPCPlayCustomSound("Dove");
                    x.ResetKillCooldown();
                    x.SetKillCooldown();
                    if (x.Is(CustomRoles.Mercenary))
                    { Mercenary.OnReportDeadBody(); }
                    x.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Pacifist), GetString("PacifistSkillNotify")));
                });
                pc.RPCPlayCustomSound("Dove");
                pc.Notify(string.Format(GetString("PacifistOnGuard"), Pacifist.PacifistNumOfUsed[pc.PlayerId]));
            }
        }
        public override void OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
        {
            if (!player.IsAlive()) return;
            PacifistNumOfUsed[player.PlayerId] += PacifistAbilityUseGainWithEachTaskCompleted.GetFloat();
        }
        public override void ApplyGameOptions(IGameOptions opt, byte playerId)
        {
            AURoleOptions.EngineerCooldown = PacifistCooldown.GetFloat();
            AURoleOptions.EngineerInVentMaxTime = 1;
        }
        public override string GetProgressText(byte playerId, bool comms)
        {
            var ProgressText = new StringBuilder();
            var taskState5 = Main.PlayerStates?[playerId].TaskState;
            Color TextColor5;
            var TaskCompleteColor5 = Color.green;
            var NonCompleteColor5 = Color.yellow;
            var NormalColor5 = taskState5.IsTaskFinished ? TaskCompleteColor5 : NonCompleteColor5;
            TextColor5 = comms ? Color.gray : NormalColor5;
            string Completed5 = comms ? "?" : $"{taskState5.CompletedTasksCount}";
            Color TextColor51;
            if (PacifistNumOfUsed[playerId] < 1) TextColor51 = Color.red;
            else TextColor51 = Color.white;
            ProgressText.Append(ColorString(TextColor5, $"({Completed5}/{taskState5.AllTasksCount})"));
            ProgressText.Append(ColorString(TextColor51, $" <color=#ffffff>-</color> {Math.Round(PacifistNumOfUsed[playerId], 1)}"));
            return ProgressText.ToString();
        }
        public override void SetAbilityButtonText(HudManager hud, byte id)
        {
            hud.ReportButton.OverrideText(GetString("ReportButtonText"));
            hud.AbilityButton.buttonLabelText.text = GetString("PacifistVentButtonText");
        }
    }
}
