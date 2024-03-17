using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Sheriff : RoleBase
{
    private const int Id = 11200;
    private static bool On = false;
    public override bool IsEnable => On;
    public static bool HasEnabled => CustomRoles.Sheriff.IsClassEnable();
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    private static OptionItem KillCooldown;
    private static OptionItem MisfireKillsTarget;
    private static OptionItem ShotLimitOpt;
    private static OptionItem ShowShotLimit;
    private static OptionItem CanKillAllAlive;
    private static OptionItem CanKillNeutrals;
    private static OptionItem CanKillNeutralsMode;
    private static OptionItem CanKillMadmate;
    private static OptionItem CanKillCharmed;
    private static OptionItem CanKillLovers;
    private static OptionItem CanKillSidekicks;
    private static OptionItem CanKillEgoists;
    private static OptionItem CanKillInfected;
    private static OptionItem CanKillContagious;
    private static OptionItem SidekickSheriffCanGoBerserk;
    private static OptionItem SetNonCrewCanKill;
    private static OptionItem NonCrewCanKillCrew;
    private static OptionItem NonCrewCanKillImp;
    private static OptionItem NonCrewCanKillNeutral;

    private static List<byte> playerIdList = [];
    private static Dictionary<byte, int> ShotLimit = [];
    private static Dictionary<byte, float> CurrentKillCooldown = [];

    private static readonly Dictionary<CustomRoles, OptionItem> KillTargetOptions = [];

    private enum KillOption
    {
        SheriffCanKillAll,
        SheriffCanKillSeparately
    }

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
        CanKillNeutralsMode = StringOptionItem.Create(Id + 14, "SheriffCanKillNeutralsMode", EnumHelper.GetAllNames<KillOption>(), 0, TabGroup.CrewmateRoles, false).SetParent(CanKillNeutrals);
        SetUpNeutralOptions(Id + 30);
        SidekickSheriffCanGoBerserk = BooleanOptionItem.Create(Id + 28, "SidekickSheriffCanGoBerserk", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        SetNonCrewCanKill = BooleanOptionItem.Create(Id + 18, "SheriffSetMadCanKill", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        NonCrewCanKillImp = BooleanOptionItem.Create(Id + 19, "SheriffMadCanKillImp", true, TabGroup.CrewmateRoles, false).SetParent(SetNonCrewCanKill);
        NonCrewCanKillCrew = BooleanOptionItem.Create(Id + 21, "SheriffMadCanKillCrew", true, TabGroup.CrewmateRoles, false).SetParent(SetNonCrewCanKill);
        NonCrewCanKillNeutral = BooleanOptionItem.Create(Id + 20, "SheriffMadCanKillNeutral", true, TabGroup.CrewmateRoles, false).SetParent(SetNonCrewCanKill);
    }
    public override void Init()
    {
        playerIdList = [];
        ShotLimit = [];
        CurrentKillCooldown = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CurrentKillCooldown.Add(playerId, KillCooldown.GetFloat());
        On = true;

        ShotLimit.TryAdd(playerId, ShotLimitOpt.GetInt());
        Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : limit: {ShotLimit[playerId]}", "Sheriff");

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    public override void Remove(byte playerId)
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
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Sheriff);
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
            ShotLimit.Add(SheriffId, Limit);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = IsUseKillButton(Utils.GetPlayerById(id)) ? CurrentKillCooldown[id] : 300f;

    public override bool CanUseKillButton(PlayerControl pc) => IsUseKillButton(pc);
    public static bool IsUseKillButton(PlayerControl pc)
        => !Main.PlayerStates[pc.PlayerId].IsDead
        && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
        && (!ShotLimit.TryGetValue(pc.PlayerId, out var x) || x > 0);

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        ShotLimit[killer.PlayerId]--;
        Logger.Info($"{killer.GetNameWithRole()} : Number of kills left: {ShotLimit[killer.PlayerId]}", "Sheriff");
        SendRPC(killer.PlayerId);
        if ((CanBeKilledBySheriff(target) && !(SetNonCrewCanKill.GetBool() && killer.IsNonCrewSheriff() || SidekickSheriffCanGoBerserk.GetBool() && killer.Is(CustomRoles.Recruit)))
            || (SidekickSheriffCanGoBerserk.GetBool() && killer.Is(CustomRoles.Recruit))
            || (SetNonCrewCanKill.GetBool() && killer.IsNonCrewSheriff()
                 && ((target.GetCustomRole().IsImpostor() && NonCrewCanKillImp.GetBool()) || (target.GetCustomRole().IsCrewmate() && NonCrewCanKillCrew.GetBool()) || (target.GetCustomRole().IsNeutral() && NonCrewCanKillNeutral.GetBool())))
            )
        {
            killer.ResetKillCooldown();
            return true;
        }
        Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
        killer.RpcMurderPlayerV3(killer);
        return MisfireKillsTarget.GetBool();
    }
    public override string GetProgressText(byte playerId, bool computervirus)
        => ShowShotLimit.GetBool() ? Utils.ColorString(IsUseKillButton(Utils.GetPlayerById(playerId)) ? Utils.GetRoleColor(CustomRoles.Sheriff).ShadeColor(0.25f) : Color.gray, ShotLimit.TryGetValue(playerId, out var shotLimit) ? $"({shotLimit})" : "Invalid") : "";
    public static bool CanBeKilledBySheriff(PlayerControl player)
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
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.KillButton.OverrideText(GetString("SheriffKillButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Kill");
}