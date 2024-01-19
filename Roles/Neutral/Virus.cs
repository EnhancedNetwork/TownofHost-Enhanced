using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using TOHE.Roles.AddOns.Crewmate;

namespace TOHE.Roles.Neutral;

public static class Virus
{
    private static readonly int Id = 18300;
    private static List<byte> playerIdList = [];
    public static bool IsEnable = false;
    private static int InfectLimit = [];
    public static List<byte> InfectedPlayer = [];

    private static OptionItem KillCooldown;
    private static OptionItem InfectMax;
    public static OptionItem CanVent;
    public static OptionItem ImpostorVision;
    public static OptionItem KnowTargetRole;
    public static OptionItem TargetKnowOtherTarget;
    public static OptionItem KillInfectedPlayerAfterMeeting;
    public static OptionItem ContagiousCountMode;

    public static readonly string[] contagiousCountMode =
    [
        "ContagiousCountMode.None",
        "ContagiousCountMode.Virus",
        "ContagiousCountMode.Original",
    ];

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Virus, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "VirusKillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "VirusCanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
        ImpostorVision = BooleanOptionItem.Create(Id + 16, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
        InfectMax = IntegerOptionItem.Create(Id + 19, "VirusInfectMax", new(1, 15, 1), 5, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus])
            .SetValueFormat(OptionFormat.Times);
        KnowTargetRole = BooleanOptionItem.Create(Id + 13, "VirusKnowTargetRole", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
        TargetKnowOtherTarget = BooleanOptionItem.Create(Id + 14, "VirusTargetKnowOtherTarget", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
        KillInfectedPlayerAfterMeeting = BooleanOptionItem.Create(Id + 15, "VirusKillInfectedPlayerAfterMeeting", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
        ContagiousCountMode = StringOptionItem.Create(Id + 18, "ContagiousCountMode", contagiousCountMode, 1, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
    }

    public static void Init()
    {
        playerIdList = [];
        InfectLimit = [];
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        InfectLimit = InfectMax.GetInt();
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    private static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetVirusInfectLimit, SendOption.Reliable, -1);
        writer.Write(InfectLimit);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    private static void SendRPCInfectKill(byte virusId, byte target = 255)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DoSpell, SendOption.Reliable, -1);
        writer.Write(virusId);
        writer.Write(target);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        InfectLimit = reader.ReadInt32();
    }

    public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (InfectLimit < 1) return;
        Main.InfectedBodies.Add(target.PlayerId);
    }

    public static void OnKilledBodyReport(PlayerControl target)
    {
        if (!CanBeInfected(target)) return;

        InfectLimit--;
        SendRPC();

        if (KillInfectedPlayerAfterMeeting.GetBool())
        {
            InfectedPlayer.Add(target.PlayerId);

            Main.VirusNotify.Add(target.PlayerId, GetString("VirusNoticeMessage2"));
        }
        else
        {
            target.RpcSetCustomRole(CustomRoles.Contagious);

            Main.VirusNotify.Add(target.PlayerId, GetString("VirusNoticeMessage"));
        }

        Logger.Info("Setting up a career:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Contagious.ToString(), "Assign " + CustomRoles.Contagious.ToString());
    }

    public static void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
        if (!KillInfectedPlayerAfterMeeting.GetBool()) return;

        PlayerControl virus =
            Main.AllAlivePlayerControls.FirstOrDefault(a => a.GetCustomRole() == CustomRoles.Virus);
        if (virus == null || deathReason != PlayerState.DeathReason.Vote) return;

        if (exileIds.Contains(virus.PlayerId)) 
        {
            InfectedPlayer.Clear();
            return;
        } 

        var infectedIdList = new List<byte>();
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            bool isInfected = InfectedPlayer.Contains(pc.PlayerId);
            if (!isInfected) continue;

            if (virus.IsAlive())
            {
                if (!Main.AfterMeetingDeathPlayers.ContainsKey(pc.PlayerId))
                {
                    pc.SetRealKiller(virus);
                    infectedIdList.Add(pc.PlayerId);
                }
            }
            else
            {
                Main.AfterMeetingDeathPlayers.Remove(pc.PlayerId);
            }
        }

        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Infected, [.. infectedIdList]);
        RemoveInfectedPlayer(virus);
    }

    public static void RemoveInfectedPlayer(PlayerControl virus)
    {
        InfectedPlayer.Clear();
        SendRPCInfectKill(virus.PlayerId);
    }

    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        if (player.Is(CustomRoles.Contagious) && target.Is(CustomRoles.Virus)) return true;
        if (KnowTargetRole.GetBool() && player.Is(CustomRoles.Virus) && target.Is(CustomRoles.Contagious)) return true;
        if (TargetKnowOtherTarget.GetBool() && player.Is(CustomRoles.Contagious) && target.Is(CustomRoles.Contagious)) return true;
        return false;
    }
    public static string GetInfectLimit() => Utils.ColorString(InfectLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Virus).ShadeColor(0.25f) : Color.gray, $"({InfectLimit})");

    public static bool CanBeInfected(this PlayerControl pc)
    {
        return true && !pc.Is(CustomRoles.Virus) && !pc.Is(CustomRoles.Contagious) && !pc.Is(CustomRoles.Loyal) 
            && !pc.Is(CustomRoles.Admired) && !pc.Is(CustomRoles.Succubus) && !pc.Is(CustomRoles.Infectious)
            && !(pc.GetCustomSubRoles().Contains(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool());
    }
}