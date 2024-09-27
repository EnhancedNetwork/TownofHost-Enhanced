﻿using System;
using UnityEngine;
using AmongUs.GameOptions;
using TOHE.Roles.AddOns.Crewmate;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.MeetingHudStartPatch;
using TOHE.Roles.Core;

namespace TOHE.Roles.Neutral;

internal class Virus : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 18300;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Virus);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem InfectMax;
    private static OptionItem CanVent;
    private static OptionItem ImpostorVision;
    private static OptionItem KnowTargetRole;
    public static OptionItem TargetKnowOtherTarget;
    private static OptionItem KillInfectedPlayerAfterMeeting;
    public static OptionItem ContagiousCountMode;

    private readonly HashSet<byte> InfectedBodies = [];
    private readonly HashSet<byte> InfectedPlayer = [];
    private readonly Dictionary<byte, string> VirusNotify = [];

    private enum ContagiousCountModeSelectList
    {
        Virus_ContagiousCountMode_None,
        Virus_ContagiousCountMode_Virus,
        Virus_ContagiousCountMode_Original
    }

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Virus);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
        ImpostorVision = BooleanOptionItem.Create(Id + 16, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
        InfectMax = IntegerOptionItem.Create(Id + 19, "VirusInfectMax", new(1, 15, 1), 5, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus])
            .SetValueFormat(OptionFormat.Times);
        KnowTargetRole = BooleanOptionItem.Create(Id + 13, "VirusKnowTargetRole", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
        TargetKnowOtherTarget = BooleanOptionItem.Create(Id + 14, "VirusTargetKnowOtherTarget", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
        KillInfectedPlayerAfterMeeting = BooleanOptionItem.Create(Id + 15, "VirusKillInfectedPlayerAfterMeeting", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
        ContagiousCountMode = StringOptionItem.Create(Id + 18, "Virus_ContagiousCountMode", EnumHelper.GetAllNames<ContagiousCountModeSelectList>(), 1, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
    }

    public override void Init()
    {
        InfectedBodies.Clear();
        VirusNotify.Clear();
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = InfectMax.GetInt();
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override void OnOthersMeetingHudStart(PlayerControl pc)
    {
        if (VirusNotify.ContainsKey(pc.PlayerId))
            AddMsg(VirusNotify[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Virus), GetString("VirusNoticeTitle")));
    }
    public override void MeetingHudClear() => VirusNotify.Clear();
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(ImpostorVision.GetBool());
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (AbilityLimit < 1) return true;
        InfectedBodies.Add(target.PlayerId);
        return true;
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (target == null || !InfectedBodies.Contains(target.PlayerId)) return;
        if (reporter == null || !reporter.CanBeInfected()) return;

        AbilityLimit--;
        SendSkillRPC();

        if (KillInfectedPlayerAfterMeeting.GetBool())
        {
            InfectedPlayer.Add(reporter.PlayerId);
            VirusNotify[reporter.PlayerId] = GetString("VirusNoticeMessage2");
        }
        else
        {
            reporter.RpcSetCustomRole(CustomRoles.Contagious);
            VirusNotify[reporter.PlayerId] = GetString("VirusNoticeMessage");
        }

        Logger.Info("Setting up a career:" + reporter?.Data?.PlayerName + " = " + reporter.GetCustomRole().ToString() + " + " + CustomRoles.Contagious.ToString(), "Assign " + CustomRoles.Contagious.ToString());
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
        if (!_Player.IsAlive() || !KillInfectedPlayerAfterMeeting.GetBool()) return;

        var virus = _Player;
        if (exileIds.Contains(virus.PlayerId)) 
        {
            InfectedPlayer.Clear();
            return;
        } 

        var infectedIdList = new List<byte>();
        foreach (var infectedId in InfectedPlayer)
        {
            var infected =  infectedId.GetPlayer();
            if (virus.IsAlive() && infected != null)
            {
                if (!Main.AfterMeetingDeathPlayers.ContainsKey(infectedId))
                {
                    infected.SetRealKiller(virus);
                    infectedIdList.Add(infectedId);
                }
            }
            else
            {
                Main.AfterMeetingDeathPlayers.Remove(infectedId);
            }
        }

        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Infected, [.. infectedIdList]);
        RemoveInfectedPlayer();
    }

    private void RemoveInfectedPlayer()
    {
        InfectedPlayer.Clear();
    }

    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        if (player.Is(CustomRoles.Contagious) && target.Is(CustomRoles.Virus)) return true;
        if (KnowTargetRole.GetBool() && player.Is(CustomRoles.Virus) && target.Is(CustomRoles.Contagious)) return true;
        if (TargetKnowOtherTarget.GetBool() && player.Is(CustomRoles.Contagious) && target.Is(CustomRoles.Contagious)) return true;
        return false;
    }
    public static string KnowRoleColor(PlayerControl seer, PlayerControl target)
    {
        if (seer.Is(CustomRoles.Contagious) && target.Is(CustomRoles.Virus)) return Main.roleColors[CustomRoles.Virus];
        if (seer.Is(CustomRoles.Virus) && target.Is(CustomRoles.Contagious)) return Main.roleColors[CustomRoles.Contagious];
        if (seer.Is(CustomRoles.Contagious) && target.Is(CustomRoles.Contagious) && TargetKnowOtherTarget.GetBool()) return Main.roleColors[CustomRoles.Virus];
        return "";
    }
    public override string GetProgressText(byte id, bool coooms) => Utils.ColorString(AbilityLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Virus).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");

}
public static class VirusPlayerControls
{
    public static bool CanBeInfected(this PlayerControl pc)
    {
        return true && !pc.Is(CustomRoles.Virus) && !pc.Is(CustomRoles.Contagious) && !pc.Is(CustomRoles.Loyal)
            && !pc.Is(CustomRoles.Admired) && !pc.Is(CustomRoles.Cultist) && !pc.Is(CustomRoles.Infectious) && !pc.Is(CustomRoles.Specter)
            && !(pc.GetCustomSubRoles().Contains(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool());
    }
}