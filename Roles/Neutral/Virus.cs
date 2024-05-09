using Hazel;
using System;
using UnityEngine;
using AmongUs.GameOptions;
using TOHE.Roles.AddOns.Crewmate;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.MeetingHudStartPatch;

namespace TOHE.Roles.Neutral;

internal class Virus : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 18300;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
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

    private static readonly HashSet<byte> InfectedBodies = [];
    private static readonly HashSet<byte> InfectedPlayer = [];
    private static readonly Dictionary<byte, string> VirusNotify = [];

    private static int InfectLimit = new();

    private enum ContagiousCountModeSelect
    {
        Virus_ContagiousCountMode_None,
        Virus_ContagiousCountMode_Virus,
        Virus_ContagiousCountMode_Original
    }

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Virus, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
        ImpostorVision = BooleanOptionItem.Create(Id + 16, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
        InfectMax = IntegerOptionItem.Create(Id + 19, "VirusInfectMax", new(1, 15, 1), 5, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus])
            .SetValueFormat(OptionFormat.Times);
        KnowTargetRole = BooleanOptionItem.Create(Id + 13, "VirusKnowTargetRole", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
        TargetKnowOtherTarget = BooleanOptionItem.Create(Id + 14, "VirusTargetKnowOtherTarget", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
        KillInfectedPlayerAfterMeeting = BooleanOptionItem.Create(Id + 15, "VirusKillInfectedPlayerAfterMeeting", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
        ContagiousCountMode = StringOptionItem.Create(Id + 18, "Virus_ContagiousCountMode", EnumHelper.GetAllNames<ContagiousCountModeSelect>(), 1, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
    }

    public override void Init()
    {
        playerIdList.Clear();
        InfectedBodies.Clear();
        VirusNotify.Clear();
        InfectLimit = new();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        InfectLimit = InfectMax.GetInt();

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override void OnOthersMeetingHudStart(PlayerControl pc)
    {
        if (VirusNotify.ContainsKey(pc.PlayerId))
            AddMsg(VirusNotify[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Virus), GetString("VirusNoticeTitle")));
    }
    public override void MeetingHudClear() => VirusNotify.Clear();
    private static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Virus); //SetVirusInfectLimit
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

    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        InfectLimit = reader.ReadInt32();
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(ImpostorVision.GetBool());
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (InfectLimit < 1) return true;
        InfectedBodies.Add(target.PlayerId);
        return true;
    }

    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    {
        if (target == null || !target.CanBeInfected()) return;
        if (!InfectedBodies.Contains(target.PlayerId)) return;

        InfectLimit--;
        SendRPC();

        if (KillInfectedPlayerAfterMeeting.GetBool())
        {
            InfectedPlayer.Add(target.PlayerId);

            VirusNotify.Add(target.PlayerId, GetString("VirusNoticeMessage2"));
        }
        else
        {
            target.RpcSetCustomRole(CustomRoles.Contagious);

            VirusNotify.Add(target.PlayerId, GetString("VirusNoticeMessage"));
        }

        Logger.Info("Setting up a career:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Contagious.ToString(), "Assign " + CustomRoles.Contagious.ToString());
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
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

    private static void RemoveInfectedPlayer(PlayerControl virus)
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
    public static string KnowRoleColor(PlayerControl seer, PlayerControl target)
    {
        if (seer.Is(CustomRoles.Contagious) && target.Is(CustomRoles.Virus)) return Main.roleColors[CustomRoles.Virus];
        if (seer.Is(CustomRoles.Virus) && target.Is(CustomRoles.Contagious)) return Main.roleColors[CustomRoles.Contagious];
        if (seer.Is(CustomRoles.Contagious) && target.Is(CustomRoles.Contagious) && Virus.TargetKnowOtherTarget.GetBool()) return Main.roleColors[CustomRoles.Virus];
        return "";
    }
    public override string GetProgressText(byte id, bool coooms) => Utils.ColorString(InfectLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Virus).ShadeColor(0.25f) : Color.gray, $"({InfectLimit})");

}
public static class VirusPlayerControls
{
    public static bool CanBeInfected(this PlayerControl pc)
    {
        return true && !pc.Is(CustomRoles.Virus) && !pc.Is(CustomRoles.Contagious) && !pc.Is(CustomRoles.Loyal)
            && !pc.Is(CustomRoles.Admired) && !pc.Is(CustomRoles.Cultist) && !pc.Is(CustomRoles.Infectious)
            && !(pc.GetCustomSubRoles().Contains(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool());
    }
}