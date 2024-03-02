using AmongUs.GameOptions;
using System.Collections.Generic;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace TOHE.Roles.Crewmate;

internal class Overseer : RoleBase
{
    private static readonly int Id = 12200;

    public static bool On = false;
    public override bool IsEnable => false;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    private static Dictionary<byte, string> RandomRole = [];
    private static Dictionary<byte, (PlayerControl, float)> OverseerTimer = [];

    private static OptionItem OverseerCooldown;
    private static OptionItem OverseerRevealTime;
    private static OptionItem Vision;

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
        CustomRoles.Coroner,
        CustomRoles.Retributionist,
        CustomRoles.Guardian,
        CustomRoles.Spiritualist,
        CustomRoles.Tracker,
    ];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Overseer);
        OverseerCooldown = FloatOptionItem.Create(Id + 10, "OverseerRevealCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Overseer])
            .SetValueFormat(OptionFormat.Seconds);
        OverseerRevealTime = FloatOptionItem.Create(Id + 11, "OverseerRevealTime", new(0f, 60f, 1f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Overseer])
            .SetValueFormat(OptionFormat.Seconds);
        Vision = FloatOptionItem.Create(Id + 12, "OverseerVision", new(0f, 5f, 0.05f), 0.25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Overseer])
            .SetValueFormat(OptionFormat.Multiplier);
    }
    public override void Init()
    {
        OverseerTimer = [];
        RandomRole = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        foreach (var ar in Main.AllPlayerControls)
        {
            Main.isRevealed.Add((playerId, ar.PlayerId), false);
        }

        RandomRole.Add(playerId, GetRandomCrewRoleString());
        On = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        OverseerTimer.Remove(playerId);
        RandomRole.Remove(playerId);
    }

    public static string GetRandomRole(byte playerId) => RandomRole[playerId];
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetVision(false);
        opt.SetFloat(FloatOptionNames.CrewLightMod, Vision.GetFloat());
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, Vision.GetFloat());
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = OverseerCooldown.GetFloat();
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        killer.SetKillCooldown(OverseerRevealTime.GetFloat());
        if (!Main.isRevealed[(killer.PlayerId, target.PlayerId)] && !OverseerTimer.ContainsKey(killer.PlayerId))
        {
            OverseerTimer.TryAdd(killer.PlayerId, (target, 0f));
            NotifyRoles(SpecifySeer: killer);
            RPC.SetCurrentRevealTarget(killer.PlayerId, target.PlayerId);
        }
        return false;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!OverseerTimer.ContainsKey(player.PlayerId)) return;

        var playerId = player.PlayerId;
        if (!player.IsAlive() || Pelican.IsEaten(playerId))
        {
            OverseerTimer.Remove(playerId);
            NotifyRoles(SpecifySeer: player);
            RPC.ResetCurrentRevealTarget(playerId);
        }
        else
        {
            var (farTarget, farTime) = OverseerTimer[playerId];
            
            if (!farTarget.IsAlive())
            {
                OverseerTimer.Remove(playerId);
            }
            else if (farTime >= OverseerRevealTime.GetFloat())
            {
                player.SetKillCooldown();
                OverseerTimer.Remove(playerId);
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
                    OverseerTimer[playerId] = (farTarget, farTime + Time.fixedDeltaTime);
                }
                else
                {
                    OverseerTimer.Remove(playerId);
                    NotifyRoles(SpecifySeer: player, SpecifyTarget: farTarget, ForceLoop: true);
                    RPC.ResetCurrentRevealTarget(playerId);

                    Logger.Info($"Canceled: {player.GetNameWithRole()}", "Overseer");
                }
            }
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    {
        OverseerTimer.Clear();
    }

    private static string GetRandomCrewRoleString() // Random role for trickster
    {
        var rd = IRandom.Instance;
        var randomRole = randomRolesForTrickster[rd.Next(0, randomRolesForTrickster.Count)];

        //string roleName = GetRoleName(randomRole);
        string RoleText = ColorString(GetRoleColor(randomRole), GetString(randomRole.ToString()));

        return $"<size={1.5}>{RoleText}</size>";
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (seer.Is(CustomRoles.Overseer))
        if (OverseerTimer.TryGetValue(seer.PlayerId, out var fa_kvp) && fa_kvp.Item1 == seen)
            return $"<color={GetRoleColorCode(CustomRoles.Overseer)}>○</color>";

        return string.Empty;
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.KillButton.OverrideText(GetString("OverseerKillButtonText"));
    }
    public override Sprite KillButtonSprite => CustomButton.Get("prophecies");
}
