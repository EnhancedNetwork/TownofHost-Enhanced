using System;
using System.Collections.Generic;
using static TOHE.Options;
using System.Linq;

namespace TOHE;

public static class Reverie
{
    private static readonly int Id = 11100;
    public static List<byte> playerIdList = [];
    public static bool IsEnable = false;

    public static OptionItem DefaultKillCooldown;
    public static OptionItem ReduceKillCooldown;
    public static OptionItem IncreaseKillCooldown;
    public static OptionItem MinKillCooldown;
    public static OptionItem MaxKillCooldown;
    public static OptionItem MisfireSuicide;
    public static OptionItem ResetCooldownMeeting;
    public static OptionItem ConvertedReverieRogue;

    public static Dictionary<byte, float> NowCooldown;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Reverie);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, "SansDefaultKillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie])
            .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(Id + 11, "SansReduceKillCooldown", new(0f, 180f, 2.5f), 7.5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie])
            .SetValueFormat(OptionFormat.Seconds);
        MinKillCooldown = FloatOptionItem.Create(Id + 12, "SansMinKillCooldown", new(0f, 180f, 2.5f), 2.5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie])
            .SetValueFormat(OptionFormat.Seconds);
        IncreaseKillCooldown = FloatOptionItem.Create(Id + 13, "ReverieIncreaseKillCooldown", new(0f, 180f, 2.5f), 5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie])
            .SetValueFormat(OptionFormat.Seconds);
        MaxKillCooldown = FloatOptionItem.Create(Id + 14, "ReverieMaxKillCooldown", new(0f, 180f, 2.5f), 40f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie])
            .SetValueFormat(OptionFormat.Seconds);
        MisfireSuicide =  BooleanOptionItem.Create(Id + 15, "ReverieMisfireSuicide", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie]);
        ResetCooldownMeeting =  BooleanOptionItem.Create(Id + 16, "ReverieResetCooldownMeeting", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie]);
        ConvertedReverieRogue = BooleanOptionItem.Create(Id + 17, "ConvertedReverieKillAll", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie]);
    }
    public static void Init()
    {
        playerIdList = [];
        NowCooldown = [];
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        NowCooldown.TryAdd(playerId, DefaultKillCooldown.GetFloat());
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static void OnReportDeadBody()
    {
        foreach(var playerId in NowCooldown.Keys)
        {
            if (ResetCooldownMeeting.GetBool())
            {
                NowCooldown[playerId] = DefaultKillCooldown.GetFloat();
            }
        }
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown[id];
    public static void OnCheckMurder(PlayerControl killer,PlayerControl target)
    {
        if (killer == null || target == null) return;
        if (!IsEnable || !killer.Is(CustomRoles.Reverie)) return;
        float kcd;
        if ((!target.GetCustomRole().IsCrewmate() && !target.Is(CustomRoles.Trickster)) || (ConvertedReverieRogue.GetBool() && killer.GetCustomSubRoles().Any(subrole => subrole.IsConverted() || subrole == CustomRoles.Madmate))) // if killed non crew or if converted
                kcd = NowCooldown[killer.PlayerId] - ReduceKillCooldown.GetFloat();
        else kcd = NowCooldown[killer.PlayerId] + IncreaseKillCooldown.GetFloat();
        NowCooldown[killer.PlayerId] = Math.Clamp(kcd, MinKillCooldown.GetFloat(), MaxKillCooldown.GetFloat());
        killer.ResetKillCooldown();
        killer.SyncSettings();
        if (NowCooldown[killer.PlayerId] >= MaxKillCooldown.GetFloat() && MisfireSuicide.GetBool())
        {
            Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
            killer.RpcMurderPlayerV3(killer);
        }
    }
}