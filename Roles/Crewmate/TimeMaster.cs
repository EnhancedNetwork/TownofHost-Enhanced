using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static TOHE.Translator;
using UnityEngine;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Utils;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Crewmate
{
    internal class TimeMaster : RoleBase
    {

        private const int Id = 9900;
        private static bool On = false;
        public override bool IsEnable => On;
        public static bool HasEnabled => CustomRoles.TimeMaster.IsClassEnable();
        public override CustomRoles ThisRoleBase => CustomRoles.Engineer;


        public static Dictionary<byte, Vector2> TimeMasterBackTrack = [];
        public static Dictionary<byte, float> TimeMasterNumOfUsed = [];
        public static Dictionary<byte, int> TimeMasterNum = [];
        public static Dictionary<byte, long> TimeMasterInProtect = [];
        public static Dictionary<byte, Vector2> TimeMasterLocation = [];

        public static OptionItem TimeMasterSkillCooldown;
        public static OptionItem TimeMasterSkillDuration;
        public static OptionItem TimeMasterMaxUses;
        public static OptionItem TimeMasterAbilityUseGainWithEachTaskCompleted;
        public static void SetupCustomOptions()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.TimeMaster);
            TimeMasterSkillCooldown = FloatOptionItem.Create(Id + 10, "TimeMasterSkillCooldown", new(1f, 180f, 1f), 20f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeMaster])
                .SetValueFormat(OptionFormat.Seconds);
            TimeMasterSkillDuration = FloatOptionItem.Create(Id + 11, "TimeMasterSkillDuration", new(1f, 180f, 1f), 20f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeMaster])
                .SetValueFormat(OptionFormat.Seconds);
            TimeMasterMaxUses = IntegerOptionItem.Create(Id + 12, "TimeMasterMaxUses", new(0, 20, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeMaster])
                .SetValueFormat(OptionFormat.Times);
            TimeMasterAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id+ 13, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeMaster])
                .SetValueFormat(OptionFormat.Times);
        }
        public override void Init()
        {
            TimeMasterBackTrack = [];
            TimeMasterNum = [];
            TimeMasterNumOfUsed = [];
            On = false;
        }
        public override void Add(byte playerId)
        {

            TimeMasterNum[playerId] = 0;
            TimeMasterNumOfUsed.Add(playerId, TimeMasterMaxUses.GetInt());
            TimeMasterNumOfUsed.Add(playerId, TimeMasterMaxUses.GetInt());
            On = true;
        }
        public override void ApplyGameOptions(IGameOptions opt, byte playerId)
        {
            AURoleOptions.EngineerCooldown = TimeMasterSkillCooldown.GetFloat();
            AURoleOptions.EngineerInVentMaxTime = 1;
        }
        public override void OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
        {
            if (!player.IsAlive()) return;
            TimeMaster.TimeMasterNumOfUsed[player.PlayerId] += TimeMaster.TimeMasterAbilityUseGainWithEachTaskCompleted.GetFloat();
        }
        public override void SetAbilityButtonText(HudManager hud, byte id)
        {
            hud.ReportButton.OverrideText(GetString("ReportButtonText"));
            hud.AbilityButton.buttonLabelText.text = GetString("TimeMasterVentButtonText");
        }
        public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
        {
            if (TimeMaster.TimeMasterInProtect.ContainsKey(target.PlayerId) && killer.PlayerId != target.PlayerId)
                if (TimeMaster.TimeMasterInProtect[target.PlayerId] + TimeMaster.TimeMasterSkillDuration.GetInt() >= Utils.GetTimeStamp(DateTime.UtcNow))
                {
                    foreach (var player in Main.AllPlayerControls)
                    {
                        if (!killer.Is(CustomRoles.Pestilence) && TimeMaster.TimeMasterBackTrack.TryGetValue(player.PlayerId, out var position))
                        {
                            if (player.CanBeTeleported())
                            {
                                player.RpcTeleport(position);
                            }
                        }
                    }
                    killer.SetKillCooldown(target: target, forceAnime: true);
                    return false;
                }
            return true;
        }
        public override void OnEnterVent(PlayerControl pc, Vent AirConditioning)
        {
            if (TimeMaster.TimeMasterNumOfUsed[pc.PlayerId] >= 1)
            {
                TimeMaster.TimeMasterNumOfUsed[pc.PlayerId] -= 1;
                TimeMaster.TimeMasterInProtect.Remove(pc.PlayerId);
                TimeMaster.TimeMasterInProtect.Add(pc.PlayerId, Utils.GetTimeStamp());

                if (!pc.IsModClient())
                {
                    pc.RpcGuardAndKill(pc);
                }
                pc.Notify(GetString("TimeMasterOnGuard"), TimeMaster.TimeMasterSkillDuration.GetFloat());

                foreach (var player in Main.AllPlayerControls)
                {
                    if (TimeMaster.TimeMasterBackTrack.TryGetValue(player.PlayerId, out var position))
                    {
                        if (player.CanBeTeleported() && player.PlayerId != pc.PlayerId)
                        {
                            player.RpcTeleport(position);
                        }
                        else if (pc == player)
                        {
                            player?.MyPhysics?.RpcBootFromVent(Main.LastEnteredVent.TryGetValue(player.PlayerId, out var vent) ? vent.Id : player.PlayerId);
                        }

                        TimeMaster.TimeMasterBackTrack.Remove(player.PlayerId);
                    }
                    else
                    {
                        TimeMaster.TimeMasterBackTrack.Add(player.PlayerId, player.GetCustomPosition());
                    }
                }
            }
        }
        public override string GetProgressText(byte playerId, bool comms)
        {
            var ProgressText = new StringBuilder();
            var taskState6 = Main.PlayerStates?[playerId].TaskState;
            Color TextColor6;
            var TaskCompleteColor6 = Color.green;
            var NonCompleteColor6 = Color.yellow;
            var NormalColor6 = taskState6.IsTaskFinished ? TaskCompleteColor6 : NonCompleteColor6;
            TextColor6 = comms ? Color.gray : NormalColor6;
            string Completed6 = comms ? "?" : $"{taskState6.CompletedTasksCount}";
            Color TextColor61;
            if (TimeMaster.TimeMasterNumOfUsed[playerId] < 1) TextColor61 = Color.red;
            else TextColor61 = Color.white;
            ProgressText.Append(ColorString(TextColor6, $"({Completed6}/{taskState6.AllTasksCount})"));
            ProgressText.Append(ColorString(TextColor61, $" <color=#ffffff>-</color> {Math.Round(TimeMaster.TimeMasterNumOfUsed[playerId], 1)}"));
            return ProgressText.ToString();
        }
        public override Sprite AbilityButtonSprite => CustomButton.Get("Time Master");
    }
}
