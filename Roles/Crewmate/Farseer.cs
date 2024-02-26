using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Crewmate
{
    internal class Farseer : RoleBase
    {
        private static readonly int Id = 12200;

        public static bool On = false;
        public override bool IsEnable => false;
        public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

        public static Dictionary<byte, string> RandomRole = [];
        public static Dictionary<byte, (PlayerControl, float)> FarseerTimer = [];

        public static OptionItem FarseerCooldown;
        public static OptionItem FarseerRevealTime;
        public static OptionItem Vision;

        private static readonly List<CustomRoles> randomRolesForTrickster =
        [
            CustomRoles.Snitch,
            //CustomRoles.Luckey,
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
            CustomRoles.Inspector,
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
        ];

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
        public override void Init()
        {
            FarseerTimer = [];
            RandomRole = [];
            On = false;
        }
        public override void Add(byte playerId)
        {
            On = true;

            if (!AmongUsClient.Instance.AmHost) return;
            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);
        }
        public override void Remove(byte playerId)
        {
            FarseerTimer.Remove(playerId);
            RandomRole.Remove(playerId);
        }
        public override void ApplyGameOptions(IGameOptions opt, byte playerId)
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Vision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Vision.GetFloat());
        }
        public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = FarseerCooldown.GetFloat();
        public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
        {
            killer.SetKillCooldown(FarseerRevealTime.GetFloat());
            if (!Main.isRevealed[(killer.PlayerId, target.PlayerId)] && !FarseerTimer.ContainsKey(killer.PlayerId))
            {
                FarseerTimer.TryAdd(killer.PlayerId, (target, 0f));
                NotifyRoles(SpecifySeer: killer);
                RPC.SetCurrentRevealTarget(killer.PlayerId, target.PlayerId);
            }
            return false;
        }
        public override void OnFixedUpdate(PlayerControl player)
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

                    float range = NormalGameOptionsV07.KillDistances[Mathf.Clamp(player.Is(Reach.IsReach) ? 2 : Main.NormalOptions.KillDistance, 0, 2)] + 0.5f;
                    float dis = Vector2.Distance(player.GetCustomPosition(), farTarget.GetCustomPosition());
                    if (dis <= range)
                    {
                        FarseerTimer[playerId] = (farTarget, farTime + Time.fixedDeltaTime);
                    }
                    else
                    {
                        FarseerTimer.Remove(playerId);
                        NotifyRoles(SpecifySeer: player, SpecifyTarget: farTarget, ForceLoop: true);
                        RPC.ResetCurrentRevealTarget(playerId);

                        Logger.Info($"Canceled: {player.GetNameWithRole()}", "Overseer");
                    }
                }
            }
        }
        public static string GetRandomCrewRoleString() // Random role for trickster
        {
            var rd = IRandom.Instance;
            var randomRole = randomRolesForTrickster[rd.Next(0, randomRolesForTrickster.Count)];

            //string roleName = GetRoleName(randomRole);
            string RoleText = ColorString(GetRoleColor(randomRole), GetString(randomRole.ToString()));

            return $"<size={1.5}>{RoleText}</size>";
        }

        public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        {
            if (seer.Is(CustomRoles.Farseer))
            if (FarseerTimer.TryGetValue(seer.PlayerId, out var fa_kvp) && fa_kvp.Item1 == seen)
                return $"<color={GetRoleColorCode(CustomRoles.Farseer)}>○</color>";

            return string.Empty;
        }
        public override void SetAbilityButtonText(HudManager hud, byte id)
        {
            hud.ReportButton.OverrideText(GetString("ReportButtonText"));
            hud.KillButton.OverrideText(GetString("FarseerKillButtonText"));
        }
        public override (string, Sprite, string, Sprite) SetAbilityButtonSprite()
        {
            return ("Kill", CustomButton.Get("prophecies"), string.Empty, CustomButton.Get("Happy"));
        }
    }
}
