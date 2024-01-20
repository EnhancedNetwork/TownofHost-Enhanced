using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TOHE.Roles.Crewmate;

public static class Sheriff
{
    private static readonly int Id = 11200;
    public static bool IsEnable = false;
    public static List<byte> playerIdList = [];
    public static Dictionary<CustomRoles, OptionItem> KillTargetOptions = [];
    public static Dictionary<byte, int> ShotLimit = [];
    public static Dictionary<byte, float> CurrentKillCooldown = [];

    public static OptionItem KillCooldown;
    private static OptionItem MisfireKillsTarget;
    public static OptionItem ShotLimitOpt;
    public static OptionItem ShowShotLimit;
    private static OptionItem CanKillAllAlive;
    public static OptionItem CanKillNeutrals;
    public static OptionItem CanKillNeutralsMode;
    public static OptionItem CanKillMadmate;
    public static OptionItem CanKillCharmed;
    public static OptionItem CanKillLovers;
    public static OptionItem CanKillSidekicks;
    public static OptionItem CanKillEgoists;
    public static OptionItem CanKillInfected;
    public static OptionItem CanKillContagious;
    public static OptionItem SidekickSheriffCanGoBerserk;
    public static OptionItem SetNonCrewCanKill;
    public static OptionItem NonCrewCanKillCrew;
    public static OptionItem NonCrewCanKillImp;
    public static OptionItem NonCrewCanKillNeutral;

    public static readonly string[] KillOption =
    [
        "SheriffCanKillAll", "SheriffCanKillSeparately"
    ];
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Sheriff);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 60f, 2.5f), 15f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff])
            .SetValueFormat(OptionFormat.Seconds);
        MisfireKillsTarget = BooleanOptionItem.Create(Id + 11, "SheriffMisfireKillsTarget", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        ShotLimitOpt = IntegerOptionItem.Create(Id + 12, "SheriffShotLimit", new(1, 15, 1), 6, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff])
            .SetValueFormat(OptionFormat.Times);
        ShowShotLimit = BooleanOptionItem.Create(Id + 13, "SheriffShowShotLimit", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillAllAlive = BooleanOptionItem.Create(Id + 15, "SheriffCanKillAllAlive", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillMadmate = BooleanOptionItem.Create(Id + 17, "SheriffCanKillMadmate", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillCharmed = BooleanOptionItem.Create(Id + 22, "SheriffCanKillCharmed", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillLovers = BooleanOptionItem.Create(Id + 24, "SheriffCanKillLovers", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillSidekicks = BooleanOptionItem.Create(Id + 23, "SheriffCanKillSidekick", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillEgoists = BooleanOptionItem.Create(Id + 25, "SheriffCanKillEgoist", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillInfected = BooleanOptionItem.Create(Id + 26, "SheriffCanKillInfected", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillContagious = BooleanOptionItem.Create(Id + 27, "SheriffCanKillContagious", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillNeutrals = BooleanOptionItem.Create(Id + 16, "SheriffCanKillNeutrals", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillNeutralsMode = StringOptionItem.Create(Id + 14, "SheriffCanKillNeutralsMode", KillOption, 0, TabGroup.CrewmateRoles, false).SetParent(CanKillNeutrals);
        SetUpNeutralOptions(Id + 30);
        SidekickSheriffCanGoBerserk = BooleanOptionItem.Create(Id + 28, "SidekickSheriffCanGoBerserk", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        SetNonCrewCanKill = BooleanOptionItem.Create(Id + 18, "SheriffSetMadCanKill", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        NonCrewCanKillImp = BooleanOptionItem.Create(Id + 19, "SheriffMadCanKillImp", true, TabGroup.CrewmateRoles, false).SetParent(SetNonCrewCanKill);
        NonCrewCanKillCrew = BooleanOptionItem.Create(Id + 21, "SheriffMadCanKillCrew", true, TabGroup.CrewmateRoles, false).SetParent(SetNonCrewCanKill);
        NonCrewCanKillNeutral = BooleanOptionItem.Create(Id + 20, "SheriffMadCanKillNeutral", true, TabGroup.CrewmateRoles, false).SetParent(SetNonCrewCanKill);
    }
    public static void Init()
    {
        playerIdList = [];
        ShotLimit = [];
        CurrentKillCooldown = [];
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CurrentKillCooldown.Add(playerId, KillCooldown.GetFloat());
        IsEnable = true;

        ShotLimit.TryAdd(playerId, ShotLimitOpt.GetInt());
        Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit[playerId]}発", "Sheriff");

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    public static void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        CurrentKillCooldown.Remove(playerId);
        ShotLimit.Remove(playerId);
    }
    public static void SetUpNeutralOptions(int Id)
    {
        foreach (var neutral in CustomRolesHelper.AllRoles.Where(x => x.IsNeutral() && x is not CustomRoles.Konan && x is not CustomRoles.Pestilence && x is not CustomRoles.Glitch).ToArray())
        {
            SetUpKillTargetOption(neutral, Id, true, CanKillNeutralsMode);
            Id++;
        }
    }
    public static void SetUpKillTargetOption(CustomRoles role, int Id, bool defaultValue = true, OptionItem parent = null)
    {
        parent ??= Options.CustomRoleSpawnChances[CustomRoles.Sheriff];
        var roleName = Utils.GetRoleName(role);
        Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), roleName) } };
        KillTargetOptions[role] = BooleanOptionItem.Create(Id, "SheriffCanKill%role%", defaultValue, TabGroup.CrewmateRoles, false).SetParent(parent);
        KillTargetOptions[role].ReplacementDictionary = replacementDic;
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSheriffShotLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(ShotLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte SheriffId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (ShotLimit.ContainsKey(SheriffId))
            ShotLimit[SheriffId] = Limit;
        else
            ShotLimit.Add(SheriffId, ShotLimitOpt.GetInt());
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? CurrentKillCooldown[id] : 300f;
    public static bool CanUseKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
        && (!ShotLimit.TryGetValue(playerId, out var x) || x > 0);

    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        ShotLimit[killer.PlayerId]--;
        Logger.Info($"{killer.GetNameWithRole()} : Number of kills left: {ShotLimit[killer.PlayerId]}", "Sheriff");
        SendRPC(killer.PlayerId);
        if ((target.CanBeKilledBySheriff() && !(SetNonCrewCanKill.GetBool() && killer.IsNonCrewSheriff() || SidekickSheriffCanGoBerserk.GetBool() && killer.Is(CustomRoles.Recruit)))
            || (SidekickSheriffCanGoBerserk.GetBool() && killer.Is(CustomRoles.Recruit))
            || (SetNonCrewCanKill.GetBool() && killer.IsNonCrewSheriff()
                 && ((target.GetCustomRole().IsImpostor() && NonCrewCanKillImp.GetBool()) || (target.GetCustomRole().IsCrewmate() && NonCrewCanKillCrew.GetBool()) || (target.GetCustomRole().IsNeutral() && NonCrewCanKillNeutral.GetBool())))
            )
        {
            SetKillCooldown(killer.PlayerId);
            return true;
        }
        Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
        killer.RpcMurderPlayerV3(killer);
        return MisfireKillsTarget.GetBool();
    }
    public static string GetShotLimit(byte playerId) => Utils.ColorString(CanUseKillButton(playerId) ? Utils.GetRoleColor(CustomRoles.Sheriff).ShadeColor(0.25f) : Color.gray, ShotLimit.TryGetValue(playerId, out var shotLimit) ? $"({shotLimit})" : "Invalid");
    public static bool CanBeKilledBySheriff(this PlayerControl player)
    {
        var cRole = player.GetCustomRole();
        var subRole = player.GetCustomSubRoles();
        bool CanKill = false;
        foreach (var SubRoleTarget in subRole)
        {
            if (SubRoleTarget == CustomRoles.Madmate)
                CanKill = CanKillMadmate.GetBool();
            if (SubRoleTarget == CustomRoles.Charmed)
                CanKill = CanKillCharmed.GetBool();
            if (SubRoleTarget == CustomRoles.Lovers)
                CanKill = CanKillLovers.GetBool();
            if (SubRoleTarget == CustomRoles.Recruit)
                CanKill = CanKillSidekicks.GetBool();
            if (SubRoleTarget == CustomRoles.Egoist)
                CanKill = CanKillEgoists.GetBool();
            if (SubRoleTarget == CustomRoles.Infected)
                CanKill = CanKillInfected.GetBool();
            if (SubRoleTarget == CustomRoles.Contagious)
                CanKill = CanKillContagious.GetBool();
            if (SubRoleTarget == CustomRoles.Rascal)
                CanKill = true;
            if (SubRoleTarget == CustomRoles.Admired)
                CanKill = false;
        }


        return cRole switch
        {
            CustomRoles.Trickster => false,
            CustomRoles.Pestilence => true,
            _ => cRole.GetCustomRoleTypes() switch
            {
                CustomRoleTypes.Impostor => true,
                CustomRoleTypes.Neutral => CanKillNeutrals.GetBool() && (CanKillNeutralsMode.GetValue() == 0 || (!KillTargetOptions.TryGetValue(cRole, out var option) || option.GetBool())),
                _ => CanKill,
            }
        };
    }
}