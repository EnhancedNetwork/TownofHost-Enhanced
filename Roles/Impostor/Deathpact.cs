using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

public static class Deathpact
{
    private static readonly int Id = 1200;
    public static List<byte> playerIdList = [];
    public static bool IsEnable = false;

    public static Dictionary<byte, List<PlayerControl>> PlayersInDeathpact = [];
    public static Dictionary<byte, long> DeathpactTime = [];

    public static List<byte> ActiveDeathpacts = [];

    private static OptionItem KillCooldown;
    private static OptionItem ShapeshiftCooldown;
//      private static OptionItem ShapeshiftDuration;
    private static OptionItem DeathpactDuration;
    private static OptionItem NumberOfPlayersInPact;
    private static OptionItem ShowArrowsToOtherPlayersInPact;
    private static OptionItem ReduceVisionWhileInPact;
    private static OptionItem VisionWhileInPact;
    private static OptionItem KillDeathpactPlayersOnMeeting;
    public static OptionItem PlayersInDeathpactCanCallMeeting;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Deathpact);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 11, "DeathPactCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact])
            .SetValueFormat(OptionFormat.Seconds);
     //   ShapeshiftDuration = FloatOptionItem.Create(Id + 12, "ShapeshiftDuration", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact])
      //      .SetValueFormat(OptionFormat.Seconds);
        DeathpactDuration = FloatOptionItem.Create(Id + 13, "DeathpactDuration", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact])
            .SetValueFormat(OptionFormat.Seconds);
        NumberOfPlayersInPact = IntegerOptionItem.Create(Id + 14, "DeathpactNumberOfPlayersInPact", new(2, 5, 1), 2, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact])
            .SetValueFormat(OptionFormat.Times);
        ShowArrowsToOtherPlayersInPact = BooleanOptionItem.Create(Id + 15, "DeathpactShowArrowsToOtherPlayersInPact", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact]);
        ReduceVisionWhileInPact = BooleanOptionItem.Create(Id + 16, "DeathpactReduceVisionWhileInPact", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact]);
        VisionWhileInPact = FloatOptionItem.Create(Id + 17, "DeathpactVisionWhileInPact", new(0f, 5f, 0.05f), 0.65f, TabGroup.ImpostorRoles, false).SetParent(ReduceVisionWhileInPact)
            .SetValueFormat(OptionFormat.Multiplier);
        KillDeathpactPlayersOnMeeting = BooleanOptionItem.Create(Id + 18, "DeathpactKillPlayersInDeathpactOnMeeting", false, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact]);
        PlayersInDeathpactCanCallMeeting = BooleanOptionItem.Create(Id + 19, "DeathpactPlayersInDeathpactCanCallMeeting", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact]);
    }

    public static void Init()
    {
        playerIdList = [];
        PlayersInDeathpact = [];
        DeathpactTime = [];
        ActiveDeathpacts = [];
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        PlayersInDeathpact.TryAdd(playerId, []);
        DeathpactTime.TryAdd(playerId, 0);
        IsEnable = true;
    }

    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public static void OnShapeshift(PlayerControl pc, PlayerControl target)
    {
        if (!pc.IsAlive() || Pelican.IsEaten(pc.PlayerId)) return;

        if (!target.IsAlive() || Pelican.IsEaten(target.PlayerId))
        {
            pc.Notify(GetString("DeathpactCouldNotAddTarget"));
            return;
        }

        if (!PlayersInDeathpact[pc.PlayerId].Any(b => b.PlayerId == target.PlayerId))
        {
            PlayersInDeathpact[pc.PlayerId].Add(target);
        }

        if (PlayersInDeathpact[pc.PlayerId].Count < NumberOfPlayersInPact.GetInt())
        {
            return;
        }

        if (ReduceVisionWhileInPact.GetBool())
        {
            MarkEveryoneDirtySettings();
        }

        pc.Notify(GetString("DeathpactComplete"));
        DeathpactTime[pc.PlayerId] = GetTimeStamp() + DeathpactDuration.GetInt();
        ActiveDeathpacts.Add(pc.PlayerId);

        foreach (var player in PlayersInDeathpact[pc.PlayerId])
        {
            if (!ShowArrowsToOtherPlayersInPact.GetBool())
            {
                continue;
            }

            foreach (var otherPlayerInPact in PlayersInDeathpact[pc.PlayerId].Where(a => a.PlayerId != player.PlayerId).ToArray())
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

        if (PlayersInDeathpact.Any(a => a.Value.Any(b => b.PlayerId == player.PlayerId) && a.Value.Count == NumberOfPlayersInPact.GetInt() ))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, VisionWhileInPact.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, VisionWhileInPact.GetFloat());
        }
    }

    public static void OnFixedUpdate(PlayerControl player)
    {
        if (!ActiveDeathpacts.Contains(player.PlayerId)) return;
        if (CheckCancelDeathpact(player)) return;

        if (DeathpactTime[player.PlayerId] < GetTimeStamp() && DeathpactTime[player.PlayerId] != 0)
        {
            foreach (var playerInDeathpact in PlayersInDeathpact[player.PlayerId])
            {
                KillPlayerInDeathpact(player, playerInDeathpact);
            }

            ClearDeathpact(player.PlayerId);
            player.Notify(GetString("DeathpactExecuted"));
        }
    }

    public static bool CheckCancelDeathpact(PlayerControl deathpact)
    {
        if (PlayersInDeathpact[deathpact.PlayerId].Any(a => a.Data.Disconnected || a.Data.IsDead))
        {
            ClearDeathpact(deathpact.PlayerId);
            deathpact.Notify(GetString("DeathpactAverted"));
            return true;
        }

        bool cancelDeathpact = true;

        foreach (var player in PlayersInDeathpact[deathpact.PlayerId])
        {
            float range = NormalGameOptionsV07.KillDistances[Mathf.Clamp(player.Is(CustomRoles.Reach) ? 2 : Main.NormalOptions.KillDistance, 0, 2)] + 0.5f;
            foreach (var otherPlayerInPact in PlayersInDeathpact[deathpact.PlayerId].Where(a => a.PlayerId != player.PlayerId).ToArray())
            {
                float dis = Vector2.Distance(player.transform.position, otherPlayerInPact.transform.position);
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

    public static void KillPlayerInDeathpact(PlayerControl deathpact, PlayerControl target)
    {
        if (deathpact == null || target == null || target.Data.Disconnected) return;
        if (!target.IsAlive()) return;
        
        Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
        target.SetRealKiller(deathpact);
        target.RpcMurderPlayerV3(target);
    }

    public static string GetDeathpactPlayerArrow(PlayerControl seer, PlayerControl target = null)
    {
        if (!IsEnable) return "";
        if (GameStates.IsMeeting) return "";
        if (!ShowArrowsToOtherPlayersInPact.GetBool()) return "";
        if (target != null && seer.PlayerId != target.PlayerId) return "";
        if (!IsInActiveDeathpact(seer)) return "";

        string arrows = string.Empty;
        var activeDeathpactsForPlayer = PlayersInDeathpact.Where(a => ActiveDeathpacts.Contains(a.Key) && a.Value.Any(b => b.PlayerId == seer.PlayerId)).ToArray();
        foreach (var deathpact in activeDeathpactsForPlayer)
        {
            foreach (var otherPlayerInPact in deathpact.Value.Where(a => a.PlayerId != seer.PlayerId).ToArray())
            {
                var arrow = TargetArrow.GetArrows(seer, otherPlayerInPact.PlayerId);
                arrows += ColorString(GetRoleColor(CustomRoles.Crewmate), arrow); 
            }
        }

        return arrows;
    }

    public static string GetDeathpactMark(PlayerControl seer, PlayerControl target)
    {
        if (!seer.Is(CustomRoles.Deathpact) || !Deathpact.IsInDeathpact(seer.PlayerId, target)) return string.Empty;
        return ColorString(Palette.ImpostorRed, "◀");
    }

    public static bool IsInActiveDeathpact(PlayerControl player)
    {
        if (ActiveDeathpacts.Count == 0) return false;
        if (PlayersInDeathpact.Any(a => ActiveDeathpacts.Contains(a.Key) && a.Value.Any(b => b.PlayerId == player.PlayerId))) return true;
        return false;
    }

    public static bool IsInDeathpact(byte deathpact, PlayerControl target)
    {
        return PlayersInDeathpact.ContainsKey(deathpact) && PlayersInDeathpact[deathpact].Any(a => a.PlayerId == target.PlayerId);
    }

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

    public static void ClearDeathpact(byte deathpact)
    {
        if (ShowArrowsToOtherPlayersInPact.GetBool())
        {
            foreach (var player in PlayersInDeathpact[deathpact])
            {
                foreach (var otherPlayerInPact in PlayersInDeathpact[deathpact].Where(a => a.PlayerId != player.PlayerId).ToArray())
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

    public static void OnReportDeadBody()
    {
        foreach (var deathpact in ActiveDeathpacts.ToArray())
        {
            if (KillDeathpactPlayersOnMeeting.GetBool())
            {
                var deathpactPlayer = Main.AllPlayerControls.FirstOrDefault(a => a.PlayerId == deathpact);
                if (deathpactPlayer == null || deathpactPlayer.Data.IsDead)
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
}