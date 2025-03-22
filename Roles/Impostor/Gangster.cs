using TOHE.Modules;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
using TOHE.Roles.Coven;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Gangster : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Gangster;
    private const int Id = 3300;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Gangster);
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem RecruitLimitOpt;
    private static OptionItem KillCooldown;
    public static OptionItem SheriffCanBeMadmate;
    public static OptionItem MayorCanBeMadmate;
    public static OptionItem NGuesserCanBeMadmate;
    public static OptionItem JudgeCanBeMadmate;
    public static OptionItem RetributionistCanBeMadmate;
    public static OptionItem MarshallCanBeMadmate;
    public static OptionItem OverseerCanBeMadmate;
    public static OptionItem CovenCanBeMadmate;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Gangster);
        KillCooldown = FloatOptionItem.Create(Id + 10, "GangsterRecruitCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster])
            .SetValueFormat(OptionFormat.Seconds);
        RecruitLimitOpt = IntegerOptionItem.Create(Id + 12, "GangsterRecruitLimit", new(1, 15, 1), 2, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster])
            .SetValueFormat(OptionFormat.Times);

        CovenCanBeMadmate = BooleanOptionItem.Create(Id + 21, "GanCovenCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        SheriffCanBeMadmate = BooleanOptionItem.Create(Id + 14, "GanSheriffCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        MayorCanBeMadmate = BooleanOptionItem.Create(Id + 15, "GanMayorCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        NGuesserCanBeMadmate = BooleanOptionItem.Create(Id + 16, "GanNGuesserCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        JudgeCanBeMadmate = BooleanOptionItem.Create(Id + 17, "GanJudgeCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        MarshallCanBeMadmate = BooleanOptionItem.Create(Id + 18, "GanMarshallCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        RetributionistCanBeMadmate = BooleanOptionItem.Create(Id + 20, "GanRetributionistCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        OverseerCanBeMadmate = BooleanOptionItem.Create(Id + 19, "GanOverseerCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(RecruitLimitOpt.GetInt());
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanRecruit(id) ? KillCooldown.GetFloat() : Options.DefaultKillCooldown;
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (CanRecruit(playerId))
            HudManager.Instance.KillButton.OverrideText(GetString("GangsterButtonText"));
        else
            HudManager.Instance.KillButton.OverrideText(GetString("KillButtonText"));
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!CanRecruit(killer.PlayerId))
        {
            return true;
        }

        if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Gangster), GetString("CantRecruit")));
            return true;
        }

        if (CanRecruit(killer.PlayerId))
        {
            if (!killer.Is(CustomRoles.Admired) && !killer.Is(CustomRoles.Recruit) && !killer.Is(CustomRoles.Charmed)
                && !killer.Is(CustomRoles.Infected) && !killer.Is(CustomRoles.Contagious) && !killer.Is(CustomRoles.Enchanted) && target.CanBeMadmate(forGangster: true)
               && CanBeGansterRecruit(target))
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Madmate.ToString(), "Ganster Assign");
                target.RpcSetCustomRole(CustomRoles.Madmate);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("GangsterSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("BeRecruitedByGangster")));
            }
            else if (killer.Is(CustomRoles.Admired) && Admirer.CanBeAdmired(target, killer))
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Admired.ToString(), "Ganster Assign");
                target.RpcSetCustomRole(CustomRoles.Admired);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admired), GetString("GangsterSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admired), GetString("BeRecruitedByGangster")));
                Admirer.AdmiredList[killer.PlayerId].Add(target.PlayerId);
                Admirer.SendRPC(killer.PlayerId, target.PlayerId);
            }
            else if (killer.Is(CustomRoles.Enchanted) && Ritualist.CanBeConverted(target))
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Enchanted.ToString(), "Gangster Assign");
                target.RpcSetCustomRole(CustomRoles.Enchanted);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Enchanted), GetString("GangsterSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Enchanted), GetString("BeRecruitedByGangster")));
            }
            else if (killer.Is(CustomRoles.Recruit) && Jackal.CanBeSidekick(target))
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Recruit.ToString(), "Ganster Assign");
                target.RpcSetCustomRole(CustomRoles.Recruit);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Recruit), GetString("GangsterSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Recruit), GetString("BeRecruitedByGangster")));
            }
            else if (killer.Is(CustomRoles.Charmed) && Cultist.CanBeCharmed(target))
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Charmed.ToString(), "Ganster Assign");
                target.RpcSetCustomRole(CustomRoles.Charmed);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Charmed), GetString("GangsterSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Charmed), GetString("BeRecruitedByGangster")));
            }
            else if (killer.Is(CustomRoles.Infected) && Infectious.CanBeBitten(target))
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

            killer.RpcRemoveAbilityUse();

            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);

            killer.ResetKillCooldown();
            killer.SetKillCooldown(forceAnime: true);
            target.ResetKillCooldown();
            target.SetKillCooldown(forceAnime: true);
            return false;
        }

    GangsterFailed:

        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Gangster), GetString("GangsterRecruitmentFailure")));
        Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
        Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);
        return true;
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CanRecruit(player.PlayerId) ? CustomButton.Get("Sidekick") : null;
    private static bool CanRecruit(byte id) => id.GetAbilityUseLimit() >= 1;

    private static bool CanBeGansterRecruit(PlayerControl pc)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsImpostor() || pc.GetCustomRole().IsCoven())
            && !pc.Is(CustomRoles.Soulless) && !pc.Is(CustomRoles.Lovers) && !pc.Is(CustomRoles.Loyal)
            && !((pc.Is(CustomRoles.NiceMini) || pc.Is(CustomRoles.EvilMini)) && Mini.Age < 18)
            && !(pc.GetCustomSubRoles().Contains(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool()) && !(CovenManager.HasNecronomicon(pc.PlayerId) && pc.Is(CustomRoles.CovenLeader));
    }
}
