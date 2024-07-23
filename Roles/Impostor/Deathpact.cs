using AmongUs.GameOptions;
using System.Data;
using System.Text;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class Deathpact : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 1200;
    private static readonly HashSet<byte> Playerids = [];
    public static bool HasEnabled => Playerids.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem ShapeshiftCooldown;
    private static OptionItem DeathpactDuration;
    private static OptionItem NumberOfPlayersInPact;
    private static OptionItem ShowArrowsToOtherPlayersInPact;
    private static OptionItem ReduceVisionWhileInPact;
    private static OptionItem VisionWhileInPact;
    private static OptionItem KillDeathpactPlayersOnMeeting;
    private static OptionItem PlayersInDeathpactCanCallMeeting;
    private static OptionItem ShowShapeshiftAnimationsOpt;

    private static readonly HashSet<byte> ActiveDeathpacts = [];
    private static readonly Dictionary<byte, HashSet<PlayerControl>> PlayersInDeathpact = [];
    private static readonly Dictionary<byte, long> DeathpactTime = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Deathpact);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 11, "DeathPactCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact])
            .SetValueFormat(OptionFormat.Seconds);
        DeathpactDuration = FloatOptionItem.Create(Id + 13, "DeathpactDuration", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact])
            .SetValueFormat(OptionFormat.Seconds);
        NumberOfPlayersInPact = IntegerOptionItem.Create(Id + 14, "DeathpactNumberOfPlayersInPact", new(2, 5, 1), 2, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact])
            .SetValueFormat(OptionFormat.Times);
        ShowArrowsToOtherPlayersInPact = BooleanOptionItem.Create(Id + 15, "DeathpactShowArrowsToOtherPlayersInPact", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact]);
        ReduceVisionWhileInPact = BooleanOptionItem.Create(Id + 16, "DeathpactReduceVisionWhileInPact", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact]);
        VisionWhileInPact = FloatOptionItem.Create(Id + 17, "DeathpactVisionWhileInPact", new(0f, 5f, 0.05f), 0.65f, TabGroup.ImpostorRoles, false).SetParent(ReduceVisionWhileInPact)
            .SetValueFormat(OptionFormat.Multiplier);
        KillDeathpactPlayersOnMeeting = BooleanOptionItem.Create(Id + 18, "DeathpactKillPlayersInDeathpactOnMeeting", false, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact]);
        PlayersInDeathpactCanCallMeeting = BooleanOptionItem.Create(Id + 19, "DeathpactPlayersInDeathpactCanCallMeeting", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact]);
        ShowShapeshiftAnimationsOpt = BooleanOptionItem.Create(Id + 20, GeneralOption.ShowShapeshiftAnimations, true, TabGroup.ImpostorRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact]);
    }

    public override void Init()
    {
        PlayersInDeathpact.Clear();
        DeathpactTime.Clear();
        ActiveDeathpacts.Clear();
        Playerids.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayersInDeathpact.TryAdd(playerId, []);
        DeathpactTime.TryAdd(playerId, 0);
        Playerids.Add(playerId);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        if (ShowShapeshiftAnimationsOpt.GetBool() || shapeshifter.PlayerId == target.PlayerId)
            return true;

        DoDeathpact(shapeshifter, target);
        return false;
    }
    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool IsAnimate, bool shapeshifting)
    {
        if (!shapeshifting)
            return;

        DoDeathpact(shapeshifter, target);
    }
    private static void DoDeathpact(PlayerControl shapeshifter, PlayerControl target)
    {
        if (!target.IsAlive() || Pelican.IsEaten(target.PlayerId))
        {
            shapeshifter.Notify(GetString("DeathpactCouldNotAddTarget"));
            return;
        }

        var playerDeathpacts = PlayersInDeathpact.GetValueOrDefault(shapeshifter.PlayerId, []);

        if (!playerDeathpacts.Any(b => b.PlayerId == target.PlayerId))
        {
            playerDeathpacts.Add(target);
            PlayersInDeathpact[shapeshifter.PlayerId] = playerDeathpacts;
        }

        if (playerDeathpacts.Count < NumberOfPlayersInPact.GetInt())
            return;

        if (ReduceVisionWhileInPact.GetBool())
        {
            MarkEveryoneDirtySettings();
        }

        shapeshifter.Notify(GetString("DeathpactComplete"));
        DeathpactTime[shapeshifter.PlayerId] = GetTimeStamp() + DeathpactDuration.GetInt();
        ActiveDeathpacts.Add(shapeshifter.PlayerId);

        if (!ShowArrowsToOtherPlayersInPact.GetBool())
            return;

        foreach (var player in playerDeathpacts)
        {
            foreach (var otherPlayerInPact in playerDeathpacts.Where(a => a.PlayerId != player.PlayerId).ToArray())
            {
                TargetArrow.Add(player.PlayerId, otherPlayerInPact.PlayerId);
            }
        }
    }
    public static void SetDeathpactVision(PlayerControl player, IGameOptions opt)
    {
        if (!ReduceVisionWhileInPact.GetBool())
            return;

        if (PlayersInDeathpact.TryGetValue(player.PlayerId, out var deathpactPlayers) &&
            deathpactPlayers.Count == NumberOfPlayersInPact.GetInt())
        {
            opt.SetVision(false);
            float visionMod = VisionWhileInPact.GetFloat();
            opt.SetFloat(FloatOptionNames.CrewLightMod, visionMod);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, visionMod);
        }
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        var playerId = player.PlayerId;
        if (!ActiveDeathpacts.Contains(playerId) || CheckCancelDeathpact(player))
            return;

        if (DeathpactTime.TryGetValue(playerId, out var deathpactEndTime) &&
            deathpactEndTime < GetTimeStamp() && deathpactEndTime != 0)
        {
            if (PlayersInDeathpact.TryGetValue(playerId, out var deathpactPlayers))
            {
                foreach (var playerInDeathpact in deathpactPlayers)
                {
                    KillPlayerInDeathpact(player, playerInDeathpact);
                }
            }

            ClearDeathpact(playerId);
            player.Notify(GetString("DeathpactExecuted"));
        }
    }

    public override bool OnCheckStartMeeting(PlayerControl reporter)
        => PlayersInDeathpactCanCallMeeting.GetBool() || !IsInActiveDeathpact(reporter);

    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
        => PlayersInDeathpactCanCallMeeting.GetBool() || !IsInActiveDeathpact(reporter);

    private static bool CheckCancelDeathpact(PlayerControl deathpact)
    {
        if (PlayersInDeathpact.TryGetValue(deathpact.PlayerId, out var deathpactPlayers) &&
            deathpactPlayers.Any(a => a.Data.Disconnected || a.Data.IsDead))
        {
            ClearDeathpact(deathpact.PlayerId);
            deathpact.Notify(GetString("DeathpactAverted"));
            return true;
        }

        float range = NormalGameOptionsV08.KillDistances[Mathf.Clamp(deathpact.Is(Reach.IsReach) ? 2 : Main.NormalOptions.KillDistance, 0, 2)] + 0.5f;
        bool cancelDeathpact = true;

        foreach (var player in deathpactPlayers)
        {
            foreach (var otherPlayerInPact in deathpactPlayers.Where(a => a.PlayerId != player.PlayerId))
            {
                float dis = Vector2.Distance(player.transform.position, otherPlayerInPact.transform.position);
                if (dis > range)
                {
                    cancelDeathpact = false;
                    break;
                }
            }
            if (!cancelDeathpact)
                break;
        }

        if (cancelDeathpact)
        {
            ClearDeathpact(deathpact.PlayerId);
            deathpact.Notify(GetString("DeathpactAverted"));
        }

        return cancelDeathpact;
    }

    private static void KillPlayerInDeathpact(PlayerControl deathpact, PlayerControl target)
    {
        if (target?.Data.Disconnected == true || !target.IsAlive())
            return;

        target.SetDeathReason(PlayerState.DeathReason.Suicide);
        target.RpcMurderPlayer(target);
        target.SetRealKiller(deathpact);
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (!seer.Is(CustomRoles.Deathpact) || !IsInDeathpact(seer.PlayerId, seen))
            return string.Empty;

        return ColorString(Palette.ImpostorRed, "◀");
    }

    public override string GetSuffix(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (isForMeeting || !ShowArrowsToOtherPlayersInPact.GetBool())
            return string.Empty;

        if (seer.PlayerId != seen.PlayerId || !IsInActiveDeathpact(seer))
            return string.Empty;

        var arrows = new StringBuilder();
        var activeDeathpactsForPlayer = PlayersInDeathpact
            .Where(a => ActiveDeathpacts.Contains(a.Key) && a.Value.Any(b => b.PlayerId == seer.PlayerId))
            .ToArray();

        foreach (var deathpact in activeDeathpactsForPlayer)
        {
            foreach (var otherPlayerInPact in deathpact.Value.Where(a => a.PlayerId != seer.PlayerId))
            {
                arrows.Append(ColorString(GetRoleColor(CustomRoles.CrewmateTOHE), TargetArrow.GetArrows(seer, otherPlayerInPact.PlayerId)));
            }
        }

        return arrows.ToString();
    }

    public static bool IsInActiveDeathpact(PlayerControl player)
    {
        return ActiveDeathpacts.Any(deathpact =>
            PlayersInDeathpact.TryGetValue(deathpact, out var players) &&
            players.Any(p => p.PlayerId == player.PlayerId));
    }

    private static bool IsInDeathpact(byte deathpactId, PlayerControl target)
        => deathpactId != target.PlayerId &&
           PlayersInDeathpact.TryGetValue(deathpactId, out var targets) &&
           targets.Any(a => a.PlayerId == target.PlayerId);

    public static string GetDeathpactString(PlayerControl player)
    {
        var result = new StringBuilder();

        var activeDeathpactsForPlayer = PlayersInDeathpact
            .Where(a => ActiveDeathpacts.Contains(a.Key) && a.Value.Any(b => b.PlayerId == player.PlayerId))
            .ToArray();

        foreach (var deathpact in activeDeathpactsForPlayer)
        {
            var otherPlayerNames = string.Join(",",
                deathpact.Value
                .Where(a => a.PlayerId != player.PlayerId)
                .Select(a => a.name.ToUpper()));

            int countdown = (int)(DeathpactTime[deathpact.Key] - GetTimeStamp());

            result.Append(ColorString(GetRoleColor(CustomRoles.Impostor),
                string.Format(GetString("DeathpactActiveDeathpact"), otherPlayerNames, countdown)));
        }

        return result.ToString();
    }

    private static void ClearDeathpact(byte deathpact)
    {
        if (ShowArrowsToOtherPlayersInPact.GetBool() && PlayersInDeathpact.TryGetValue(deathpact, out var deathpactPlayers))
        {
            foreach (var player in deathpactPlayers)
            {
                foreach (var otherPlayerInPact in deathpactPlayers.Where(a => a.PlayerId != player.PlayerId))
                {
                    TargetArrow.Remove(player.PlayerId, otherPlayerInPact.PlayerId);
                }
            }
        }

        DeathpactTime[deathpact] = 0;
        ActiveDeathpacts.Remove(deathpact);
        PlayersInDeathpact[deathpact].Clear();

        if (ReduceVisionWhileInPact.GetBool())
        {
            MarkEveryoneDirtySettings();
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        foreach (var deathpact in ActiveDeathpacts.ToArray())
        {
            if (KillDeathpactPlayersOnMeeting.GetBool())
            {
                var deathpactPlayer = Main.AllAlivePlayerControls
                    .FirstOrDefault(a => a.PlayerId == deathpact);

                if (deathpactPlayer.IsAlive())
                {
                    if (PlayersInDeathpact.TryGetValue(deathpact, out var playersInPact))
                    {
                        foreach (var player in playersInPact)
                        {
                            KillPlayerInDeathpact(deathpactPlayer, player);
                        }
                    }
                }
            }

            ClearDeathpact(deathpact);
        }
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("DeathpactButtonText"));
    }
}