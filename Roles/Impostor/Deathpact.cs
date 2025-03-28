using AmongUs.GameOptions;
using System.Data;
using System.Text;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Core;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class Deathpact : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Deathpact;
    private const int Id = 1200;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Deathpact);
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
    }
    public override void Add(byte playerId)
    {
        PlayersInDeathpact[playerId] = [];
        DeathpactTime[playerId] = 0;
    }
    public override void Remove(byte playerId)
    {
        PlayersInDeathpact.Remove(playerId);
        DeathpactTime.Remove(playerId);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        if (ShowShapeshiftAnimationsOpt.GetBool() || shapeshifter.PlayerId == target.PlayerId) return true;

        DoDeathpact(shapeshifter, target);
        return false;
    }
    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool IsAnimate, bool shapeshifting)
    {
        if (!shapeshifting) return;

        DoDeathpact(shapeshifter, target);
    }
    private static void DoDeathpact(PlayerControl shapeshifter, PlayerControl target)
    {
        if (!target.IsAlive() || Pelican.IsEaten(target.PlayerId))
        {
            shapeshifter.Notify(GetString("DeathpactCouldNotAddTarget"));
            return;
        }

        if (!PlayersInDeathpact[shapeshifter.PlayerId].Any(b => b.PlayerId == target.PlayerId))
        {
            PlayersInDeathpact[shapeshifter.PlayerId].Add(target);
        }

        if (PlayersInDeathpact[shapeshifter.PlayerId].Count < NumberOfPlayersInPact.GetInt())
        {
            return;
        }

        if (ReduceVisionWhileInPact.GetBool())
        {
            MarkEveryoneDirtySettings();
        }

        shapeshifter.Notify(GetString("DeathpactComplete"));
        DeathpactTime[shapeshifter.PlayerId] = GetTimeStamp() + DeathpactDuration.GetInt();
        ActiveDeathpacts.Add(shapeshifter.PlayerId);

        foreach (var player in PlayersInDeathpact[shapeshifter.PlayerId])
        {
            if (!ShowArrowsToOtherPlayersInPact.GetBool())
            {
                continue;
            }

            foreach (var otherPlayerInPact in PlayersInDeathpact[shapeshifter.PlayerId].Where(a => a.PlayerId != player.PlayerId).ToArray())
            {
                TargetArrow.Add(player.PlayerId, otherPlayerInPact.PlayerId);
            }
        }
    }
    public static void SetDeathpactVision(PlayerControl player, IGameOptions opt)
    {
        if (!ReduceVisionWhileInPact.GetBool())
        {
            return;
        }

        if (PlayersInDeathpact.Any(a => a.Value.Any(b => b.PlayerId == player.PlayerId) && a.Value.Count == NumberOfPlayersInPact.GetInt()))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, VisionWhileInPact.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, VisionWhileInPact.GetFloat());
        }
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (lowLoad || !ActiveDeathpacts.Contains(player.PlayerId)) return;
        if (CheckCancelDeathpact(player)) return;

        if (DeathpactTime.TryGetValue(player.PlayerId, out var time) && time < nowTime && time != 0)
        {
            foreach (var playerInDeathpact in PlayersInDeathpact[player.PlayerId])
            {
                KillPlayerInDeathpact(player, playerInDeathpact);
            }

            ClearDeathpact(player.PlayerId);
            player.Notify(GetString("DeathpactExecuted"));
        }
    }

    public override bool OnCheckStartMeeting(PlayerControl reporter)
    {
        if (PlayersInDeathpactCanCallMeeting.GetBool()) return true;
        return !IsInActiveDeathpact(reporter);
    }
    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        if (PlayersInDeathpactCanCallMeeting.GetBool()) return true;
        return !IsInActiveDeathpact(reporter);
    }

    private static bool CheckCancelDeathpact(PlayerControl deathpact)
    {
        if (!PlayersInDeathpact.TryGetValue(deathpact.PlayerId, out var playerList)) return false;

        if (playerList.Any(a => a.Data.Disconnected || a.Data.IsDead))
        {
            ClearDeathpact(deathpact.PlayerId);
            deathpact.Notify(GetString("DeathpactAverted"));
            return true;
        }

        bool cancelDeathpact = true;

        foreach (var player in playerList)
        {
            float range = ExtendedPlayerControl.GetKillDistances(ovverideValue: player.Is(Reach.IsReach), newValue: 2) + 0.5f;
            foreach (var otherPlayerInPact in playerList.Where(a => a.PlayerId != player.PlayerId).ToArray())
            {
                float dis = GetDistance(player.transform.position, otherPlayerInPact.transform.position);
                cancelDeathpact = cancelDeathpact && (dis <= range);
            }
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
        if (deathpact == null || target == null || target.Data.Disconnected) return;
        if (!target.IsAlive()) return;
        if (target.IsTransformedNeutralApocalypse()) return;

        target.SetDeathReason(PlayerState.DeathReason.Suicide);
        target.RpcMurderPlayer(target);
        target.SetRealKiller(deathpact);
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        => IsInDeathpact(seer.PlayerId, seen) ? ColorString(Palette.ImpostorRed, "◀") : string.Empty;

    public override string GetSuffix(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (isForMeeting || !ShowArrowsToOtherPlayersInPact.GetBool()) return string.Empty;
        if (seer.PlayerId != seen.PlayerId) return string.Empty;
        if (!IsInActiveDeathpact(seer)) return string.Empty;

        var arrows = new StringBuilder();
        var activeDeathpactsForPlayer = PlayersInDeathpact.Where(a => ActiveDeathpacts.Contains(a.Key) && a.Value.Any(b => b.PlayerId == seer.PlayerId)).ToArray();

        foreach (var deathpact in activeDeathpactsForPlayer)
        {
            foreach (var otherPlayerInPact in deathpact.Value.Where(a => a.PlayerId != seer.PlayerId).ToArray())
            {
                arrows.Append(ColorString(GetRoleColor(CustomRoles.CrewmateTOHE), TargetArrow.GetArrows(seer, otherPlayerInPact.PlayerId)));
            }
        }

        return arrows.ToString();
    }

    public static bool IsInActiveDeathpact(PlayerControl player)
    {
        if (!ActiveDeathpacts.Any() || !PlayersInDeathpact.Any()) return false;
        if (PlayersInDeathpact.Any(a => ActiveDeathpacts.Contains(a.Key) && a.Value.Any(b => b.PlayerId == player.PlayerId))) return true;
        return false;
    }

    private static bool IsInDeathpact(byte deathpactId, PlayerControl target)
        => deathpactId != target.PlayerId && PlayersInDeathpact.TryGetValue(deathpactId, out var targets) && targets.Any(a => a.PlayerId == target.PlayerId);

    public static string GetDeathpactString(PlayerControl player)
    {
        string result = string.Empty;

        var activeDeathpactsForPlayer = PlayersInDeathpact.Where(a => ActiveDeathpacts.Contains(a.Key) && a.Value.Any(b => b.PlayerId == player.PlayerId)).ToArray();
        foreach (var deathpact in activeDeathpactsForPlayer)
        {
            string otherPlayerNames = string.Empty;
            foreach (var otherPlayerInPact in deathpact.Value.Where(a => a.PlayerId != player.PlayerId).ToArray())
            {
                otherPlayerNames += otherPlayerInPact.name.ToUpper() + ",";
            }

            otherPlayerNames = otherPlayerNames.Remove(otherPlayerNames.Length - 1);

            int countdown = (int)(DeathpactTime[deathpact.Key] - GetTimeStamp());

            result +=
                $"{ColorString(GetRoleColor(CustomRoles.Impostor), string.Format(GetString("DeathpactActiveDeathpact"), otherPlayerNames, countdown))}";
        }

        return result;
    }

    private static void ClearDeathpact(byte deathpact)
    {
        if (ShowArrowsToOtherPlayersInPact.GetBool() && PlayersInDeathpact.TryGetValue(deathpact, out var playerList))
        {
            foreach (var player in playerList)
            {
                foreach (var otherPlayerInPact in playerList.Where(a => a.PlayerId != player.PlayerId).ToArray())
                {
                    TargetArrow.Remove(player.PlayerId, otherPlayerInPact.PlayerId);
                }
            }
        }

        DeathpactTime[deathpact] = 0;
        ActiveDeathpacts.Remove(deathpact);
        PlayersInDeathpact[deathpact] = [];

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
                var deathpactPlayer = deathpact.GetPlayer();
                if (!deathpactPlayer.IsAlive())
                {
                    continue;
                }

                foreach (var player in PlayersInDeathpact[deathpact])
                {
                    KillPlayerInDeathpact(deathpactPlayer, player);
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
