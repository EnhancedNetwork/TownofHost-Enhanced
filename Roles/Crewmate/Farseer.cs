using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate
{
    public static class Farseer
    {
        private static readonly int Id = 12200;

        private static readonly string fontSize = "1.5";
        public static bool IsEnable = false;

        public static Dictionary<int, string> RandomRole = new();
        public static Dictionary<byte, (PlayerControl, float)> FarseerTimer = new();

        public static OptionItem FarseerCooldown;
        public static OptionItem FarseerRevealTime;
        public static OptionItem Vision;

        private static List<CustomRoles> randomRolesForTrickster = new()
        {
            CustomRoles.Snitch,
            CustomRoles.Luckey,
            CustomRoles.Needy,
            CustomRoles.SuperStar,
            CustomRoles.CyberStar,
            CustomRoles.TaskManager,
            CustomRoles.Mayor,
            CustomRoles.Paranoia,
            CustomRoles.Psychic,
            CustomRoles.SabotageMaster,
            CustomRoles.Snitch,
            CustomRoles.Marshall,
            CustomRoles.ParityCop,
            CustomRoles.Bastion,
            CustomRoles.Dictator,
            CustomRoles.Doctor,
            CustomRoles.Detective,
            CustomRoles.Lookout,
            CustomRoles.Monitor,
            CustomRoles.NiceGuesser,
            CustomRoles.Transporter,
            CustomRoles.TimeManager,
            CustomRoles.Veteran,
            CustomRoles.Bodyguard,
            CustomRoles.Grenadier,
            CustomRoles.Lighter,
            CustomRoles.Divinator,
            CustomRoles.Oracle,
            CustomRoles.Tracefinder,
      //      CustomRoles.Glitch,
            CustomRoles.Judge,
            CustomRoles.Mortician,
            CustomRoles.Mediumshiper,
            CustomRoles.Observer,
            CustomRoles.DovesOfNeace,
            CustomRoles.Bloodhound,
            CustomRoles.Retributionist,
            CustomRoles.Guardian,
            CustomRoles.Spiritualist,
            CustomRoles.Tracker,
        };

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Farseer);
            FarseerCooldown = FloatOptionItem.Create(Id + 10, "FarseerRevealCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Farseer])
                .SetValueFormat(OptionFormat.Seconds);
            FarseerRevealTime = FloatOptionItem.Create(Id + 11, "FarseerRevealTime", new(0f, 60f, 1f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Farseer])
                .SetValueFormat(OptionFormat.Seconds);
            Vision = FloatOptionItem.Create(Id + 12, "FarseerVision", new(0f, 5f, 0.05f), 0.25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Farseer])
                .SetValueFormat(OptionFormat.Multiplier);
        }
        public static void Init()
        {
            FarseerTimer = new();
            RandomRole = new();
            IsEnable = false;
        }
        public static void Add(byte playerId)
        {
            IsEnable = true;

            if (!AmongUsClient.Instance.AmHost) return;
            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);
        }

        public static void SetCooldown(byte id) => Main.AllPlayerKillCooldown[id] = FarseerCooldown.GetFloat();
        public static void OnCheckMurder(PlayerControl killer, PlayerControl target, PlayerControl __instance)
        {
            killer.SetKillCooldown(FarseerRevealTime.GetFloat());
            if (!Main.isRevealed[(killer.PlayerId, target.PlayerId)] && !FarseerTimer.ContainsKey(killer.PlayerId))
            {
                FarseerTimer.TryAdd(killer.PlayerId, (target, 0f));
                NotifyRoles(SpecifySeer: __instance);
                RPC.SetCurrentRevealTarget(killer.PlayerId, target.PlayerId);
            }
        }
        public static void OnFixedUpdate(PlayerControl player)
        {
            if (!FarseerTimer.ContainsKey(player.PlayerId)) return;

            var playerId = player.PlayerId;
            if (!player.IsAlive() || Pelican.IsEaten(playerId))
            {
                FarseerTimer.Remove(playerId);
                NotifyRoles(SpecifySeer: player);
                RPC.ResetCurrentRevealTarget(playerId);
            }
            else
            {
                var (farTarget, farTime) = FarseerTimer[playerId];
                
                if (!farTarget.IsAlive())
                {
                    FarseerTimer.Remove(playerId);
                }
                else if (farTime >= FarseerRevealTime.GetFloat())
                {
                    player.SetKillCooldown();
                    FarseerTimer.Remove(playerId);
                    Main.isRevealed[(playerId, farTarget.PlayerId)] = true;
                    player.RpcSetRevealtPlayer(farTarget, true);
                    NotifyRoles(SpecifySeer: player);
                    RPC.ResetCurrentRevealTarget(playerId);
                }
                else
                {

                    float range = NormalGameOptionsV07.KillDistances[Mathf.Clamp(player.Is(CustomRoles.Reach) ? 2 : Main.NormalOptions.KillDistance, 0, 2)] + 0.5f;
                    float dis = Vector2.Distance(player.GetTruePosition(), farTarget.GetTruePosition());
                    if (dis <= range)
                    {
                        FarseerTimer[playerId] = (farTarget, farTime + Time.fixedDeltaTime);
                    }
                    else
                    {
                        FarseerTimer.Remove(playerId);
                        NotifyRoles(SpecifySeer: player, SpecifyTarget: farTarget, ForceLoop: true);
                        RPC.ResetCurrentRevealTarget(playerId);

                        Logger.Info($"Canceled: {player.GetNameWithRole()}", "Farseer");
                    }
                }
            }
        }
        public static void OnReportDeadBody()
        {
            FarseerTimer.Clear();
        }
        public static string GetRandomCrewRoleString()
        {
            var rd = IRandom.Instance;
            var randomRole = randomRolesForTrickster[rd.Next(0, randomRolesForTrickster.Count)];

            string roleName = GetRoleName(randomRole);
            string RoleText = ColorString(GetRoleColor(randomRole), GetString(randomRole.ToString()));

            return $"<size={fontSize}>{RoleText}</size>";
        }

        public static string GetTaskState()
        {
            var playersWithTasks = Main.PlayerStates.Where(a => a.Value.GetTaskState().hasTasks).ToArray();
            if (playersWithTasks.Length == 0)
            {
                return "\r\n";
            }

            var rd = IRandom.Instance;
            var randomPlayer = playersWithTasks[rd.Next(0, playersWithTasks.Length)];
            var taskState = randomPlayer.Value.GetTaskState();

            Color TextColor;
            var TaskCompleteColor = Color.green;
            var NonCompleteColor = Color.yellow;
            var NormalColor = taskState.IsTaskFinished ? TaskCompleteColor : NonCompleteColor;

            TextColor = Camouflager.AbilityActivated ? Color.gray : NormalColor;
            string Completed = Camouflager.AbilityActivated ? "?" : $"{taskState.CompletedTasksCount}";

            return $" <size={fontSize}>" + ColorString(TextColor, $"({Completed}/{taskState.AllTasksCount})") + "</size>\r\n";
        }
    }
}
