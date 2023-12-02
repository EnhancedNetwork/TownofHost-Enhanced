using Hazel;
using System.Collections.Generic;
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

        if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Gangster), GetString("CantRecruit")));
            return false;
        }

        if (CanBeGansterRecruit(target))
        {
            if (!killer.Is(CustomRoles.Admired) && !killer.Is(CustomRoles.Recruit) && !killer.Is(CustomRoles.Charmed)
                && !killer.Is(CustomRoles.Infected) && !killer.Is(CustomRoles.Contagious) && target.CanBeMadmate(inGame: true))
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Madmate.ToString(), "Ganster Assign");
                target.RpcSetCustomRole(CustomRoles.Madmate);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("GangsterSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("BeRecruitedByGangster")));
            }
            else if (killer.Is(CustomRoles.Admired) && target.CanBeAdmired(killer))
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Admired.ToString(), "Ganster Assign");
                target.RpcSetCustomRole(CustomRoles.Admired);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admired), GetString("GangsterSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admired), GetString("BeRecruitedByGangster")));
                Admirer.AdmiredList[killer.PlayerId].Add(target.PlayerId);
                Admirer.SendRPC(killer.PlayerId, target.PlayerId);
            }
            else if (killer.Is(CustomRoles.Recruit) && target.CanBeSidekick())
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Recruit.ToString(), "Ganster Assign");
                target.RpcSetCustomRole(CustomRoles.Recruit);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Recruit), GetString("GangsterSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Recruit), GetString("BeRecruitedByGangster")));
            }
            else if (killer.Is(CustomRoles.Charmed) && target.CanBeCharmed())
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Charmed.ToString(), "Ganster Assign");
                target.RpcSetCustomRole(CustomRoles.Charmed);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Charmed), GetString("GangsterSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Charmed), GetString("BeRecruitedByGangster")));
            }
            else if (killer.Is(CustomRoles.Infected) && target.CanBeInfected())
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Infected.ToString(), "Ganster Assign");
                target.RpcSetCustomRole(CustomRoles.Infected);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Infected), GetString("GangsterSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Infected), GetString("BeRecruitedByGangster")));
            }
            else if (killer.Is(CustomRoles.Contagious) && target.CanBeInfected())
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Contagious.ToString(), "Ganster Assign");
                target.RpcSetCustomRole(CustomRoles.Contagious);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Contagious), GetString("GangsterSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Contagious), GetString("BeRecruitedByGangster")));
            }
            else goto GangsterFailed;

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
    GangsterFailed:
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Gangster), GetString("GangsterRecruitmentFailure")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{RecruitLimit[killer.PlayerId]}次招募机会", "Gangster");
        SendRPC(killer.PlayerId);
        Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
        Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);
        return false;
    }
    public static string GetRecruitLimit(byte playerId) => Utils.ColorString(CanRecruit(playerId) ? Utils.GetRoleColor(CustomRoles.Gangster).ShadeColor(0.25f) : Color.gray, RecruitLimit.TryGetValue(playerId, out var recruitLimit) ? $"({recruitLimit})" : "Invalid");

    public static bool CanBeGansterRecruit(PlayerControl pc)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsImpostor() || pc.GetCustomRole().IsNeutral())
            && !pc.Is(CustomRoles.Soulless) && !pc.Is(CustomRoles.Lovers) && !pc.Is(CustomRoles.Loyal)
            && !((pc.Is(CustomRoles.NiceMini) || pc.Is(CustomRoles.EvilMini)) && Mini.Age < 18)
            && !(pc.GetCustomSubRoles().Contains(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool());
    }
}