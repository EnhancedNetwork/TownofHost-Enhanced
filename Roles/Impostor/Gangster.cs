using Hazel;
using System.Collections.Generic;
using TOHE.Modules;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

public static class Gangster
{
    private static readonly int Id = 3300;
    private static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    private static OptionItem RecruitLimitOpt;
    public static OptionItem KillCooldown;
    public static OptionItem SheriffCanBeMadmate;
    public static OptionItem MayorCanBeMadmate;
    public static OptionItem NGuesserCanBeMadmate;
    public static OptionItem JudgeCanBeMadmate;
    public static OptionItem MarshallCanBeMadmate;
    public static OptionItem FarseerCanBeMadmate;
    public static OptionItem RetributionistCanBeMadmate;

    public static Dictionary<byte, int> RecruitLimit = new();

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Gangster);
        KillCooldown = FloatOptionItem.Create(Id + 10, "GangsterRecruitCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster])
            .SetValueFormat(OptionFormat.Seconds);
        RecruitLimitOpt = IntegerOptionItem.Create(Id + 12, "GangsterRecruitLimit", new(1, 15, 1), 2, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster])
            .SetValueFormat(OptionFormat.Times);

        SheriffCanBeMadmate = BooleanOptionItem.Create(Id + 14, "GanSheriffCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        MayorCanBeMadmate = BooleanOptionItem.Create(Id + 15, "GanMayorCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        NGuesserCanBeMadmate = BooleanOptionItem.Create(Id + 16, "GanNGuesserCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        JudgeCanBeMadmate = BooleanOptionItem.Create(Id + 17, "GanJudgeCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        MarshallCanBeMadmate = BooleanOptionItem.Create(Id + 18, "GanMarshallCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        FarseerCanBeMadmate = BooleanOptionItem.Create(Id + 19, "GanFarseerCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        RetributionistCanBeMadmate = BooleanOptionItem.Create(Id + 20, "GanRetributionistCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);

    }
    public static void Init()
    {
        playerIdList = new();
        RecruitLimit = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        RecruitLimit.TryAdd(playerId, RecruitLimitOpt.GetInt());
        IsEnable = true;
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetGangsterRecruitLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(RecruitLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        RecruitLimit.TryAdd(PlayerId, Limit);
        RecruitLimit[PlayerId] = Limit;
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanRecruit(id) ? KillCooldown.GetFloat() : Options.DefaultKillCooldown;
    public static bool CanRecruit(byte id) => RecruitLimit.TryGetValue(id, out var x) && x > 0;
    public static void SetKillButtonText(byte playerId)
    {
        if (CanRecruit(playerId))
            HudManager.Instance.KillButton.OverrideText(GetString("GangsterButtonText"));
        else
            HudManager.Instance.KillButton.OverrideText(GetString("KillButtonText"));
    }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (RecruitLimit[killer.PlayerId] < 1)
        {
            if (killer.AmOwner)
            {
                HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
            } //Why do we even need this?
            return false;
        }

        if ((target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)) && Mini.Age < 18)
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Gangster), GetString("CantRecruit")));
            return false;
        }

        var ConvertSubRole = ConvertManager.GetConvertSubRole(killer, CustomRoles.Madmate);

        if (ConvertManager.CanBeConvertSubRole(target, ConvertSubRole, killer))
        {
            Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + ConvertSubRole.ToString(), "Gangster Assign");
            target.RpcSetCustomRole(ConvertSubRole);
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(ConvertSubRole), GetString("GangsterSuccessfullyRecruited")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(ConvertSubRole), GetString("BeRecruitedByGangster")));

            RecruitLimit[killer.PlayerId]--;

            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);

            killer.ResetKillCooldown();
            killer.SetKillCooldown(forceAnime: true);
            target.ResetKillCooldown();
            target.SetKillCooldown(forceAnime: true);

            Logger.Info($"{killer.GetNameWithRole()} : 剩余{RecruitLimit[killer.PlayerId]}次招募机会", "Gangster");
            SendRPC(killer.PlayerId);
            return true;
        }
        else
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Gangster), GetString("GangsterRecruitmentFailure")));
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{RecruitLimit[killer.PlayerId]}次招募机会", "Gangster");
            SendRPC(killer.PlayerId);
            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
            return false;
        }
    }
    public static string GetRecruitLimit(byte playerId) => Utils.ColorString(CanRecruit(playerId) ? Utils.GetRoleColor(CustomRoles.Gangster).ShadeColor(0.25f) : Color.gray, RecruitLimit.TryGetValue(playerId, out var recruitLimit) ? $"({recruitLimit})" : "Invalid");

    public static bool CanBeGansterRecruit(PlayerControl pc)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsImpostor() || pc.GetCustomRole().IsNeutral())
            && !pc.Is(CustomRoles.Soulless) && !pc.Is(CustomRoles.Lovers) && !pc.Is(CustomRoles.Loyal)
            && !((pc.Is(CustomRoles.NiceMini) || pc.Is(CustomRoles.EvilMini)) && Mini.Age < 18)
            && !(pc.Is(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool());
    }
}