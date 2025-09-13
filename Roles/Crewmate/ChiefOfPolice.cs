using TOHE.Modules;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class ChiefOfPolice : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.ChiefOfPolice;
    private const int Id = 12600;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.ChiefOfPolice);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static OptionItem SkillCooldown;
    private static OptionItem CanRecruitImpostor;
    private static OptionItem CanRecruitNeutral;
    private static OptionItem CanRecruitCoven;
    private static OptionItem ChiefOfPoliceCanSeePolice;
    private static OptionItem PreventRecruitNonKiller;
    private static OptionItem SuidiceWhenTargetNotKiller;
    private static OptionItem PassConverted;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.ChiefOfPolice);
        SkillCooldown = FloatOptionItem.Create(Id + 10, "ChiefOfPoliceSkillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.ChiefOfPolice])
            .SetValueFormat(OptionFormat.Seconds);
        CanRecruitImpostor = BooleanOptionItem.Create(Id + 11, "PolicCanImpostor", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.ChiefOfPolice]);
        CanRecruitNeutral = BooleanOptionItem.Create(Id + 12, "PolicCanNeutral", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.ChiefOfPolice]);
        CanRecruitCoven = BooleanOptionItem.Create(Id + 13, "PolicCanCoven", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.ChiefOfPolice]);
        ChiefOfPoliceCanSeePolice = BooleanOptionItem.Create(Id + 14, "ChiefOfPoliceCanSeePolice", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.ChiefOfPolice]);
        PreventRecruitNonKiller = BooleanOptionItem.Create(Id + 15, "PolicPreventRecruitNonKiller", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.ChiefOfPolice]);
        SuidiceWhenTargetNotKiller = BooleanOptionItem.Create(Id + 16, "PolicSuidiceWhenTargetNotKiller", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.ChiefOfPolice]);
        PassConverted = BooleanOptionItem.Create(Id + 17, "PolicPassConverted", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.ChiefOfPolice]);
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(1);
    }

    public override bool CanUseKillButton(PlayerControl pc) => pc.GetAbilityUseLimit() > 0;

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = id.GetAbilityUseLimit() > 0 ? SkillCooldown.GetFloat() : 999f;

    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
    {
        if (seer.IsAnySubRole(x => x.IsConverted()) || target.IsAnySubRole(x => x.IsConverted()))
            return false;
        if (seer.Is(CustomRoles.ChiefOfPolice) && target.Is(CustomRoles.Sheriff) && ChiefOfPoliceCanSeePolice.GetBool())
            return true;
        if (seer.Is(CustomRoles.Sheriff) && target.Is(CustomRoles.ChiefOfPolice) && ChiefOfPoliceCanSeePolice.GetBool())
            return true;
        return false;
    }

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() < 1) return false;

        bool suidice = false;
        bool isSuccess = false;

        if (target.IsPlayerCrewmateTeam())
        {
            if (PreventRecruitNonKiller.GetBool() && !target.CanUseKillButton())
            {
                suidice = true;
            }
            else
            {
                killer.RpcRemoveAbilityUse();
                killer.RpcGuardAndKill(target);
                killer.ResetKillCooldown();
                killer.SetKillCooldown();

                target.GetRoleClass()?.OnRemove(target.PlayerId);
                target.RpcChangeRoleBasis(CustomRoles.Sheriff);
                target.RpcSetCustomRole(CustomRoles.Sheriff);
                target.GetRoleClass()?.OnAdd(target.PlayerId);

                target.ResetKillCooldown();
                target.SetKillCooldown(forceAnime: true);

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.ChiefOfPolice), GetString("SheriffSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.ChiefOfPolice), GetString("BeSheriffByPolice")));

                Utils.NotifyRoles(killer);
                Utils.NotifyRoles(target);
                isSuccess = true;
            }
        }
        else
        {
            if (!CanRecruitCoven.GetBool() && target.IsPlayerCovenTeam() || !CanRecruitNeutral.GetBool() && target.IsPlayerNeutralTeam() || !CanRecruitImpostor.GetBool() && target.IsPlayerImpostorTeam() || target.IsNeutralApocalypse())
            {
                suidice = true;
            }
            else if (target.Is(CustomRoles.Zombie) || target.Is(CustomRoles.EvilMini) || target.Is(CustomRoles.Loyal))
            {
                suidice = true;
            }
            else
            {
                if (PreventRecruitNonKiller.GetBool() && !target.CanUseKillButton())
                {
                    suidice = true;
                }
                else
                {
                    killer.RpcRemoveAbilityUse();
                    killer.RpcGuardAndKill(target);
                    killer.ResetKillCooldown();
                    killer.SetKillCooldown();

                    target.GetRoleClass()?.OnRemove(target.PlayerId);
                    target.RpcChangeRoleBasis(CustomRoles.Sheriff);
                    target.RpcSetCustomRole(CustomRoles.Sheriff);
                    target.GetRoleClass()?.OnAdd(target.PlayerId);
                    if (Main.PlayerStates[target.PlayerId].IsNecromancer) Main.PlayerStates[target.PlayerId].IsNecromancer = false;

                    target.ResetKillCooldown();
                    target.SetKillCooldown(forceAnime: true);

                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.ChiefOfPolice), GetString("SheriffSuccessfullyRecruited")));
                    target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.ChiefOfPolice), GetString("BeSheriffByPolice")));

                    Utils.NotifyRoles(killer);
                    Utils.NotifyRoles(target);
                    isSuccess = true;
                }
            }
        }

        if (suidice && SuidiceWhenTargetNotKiller.GetBool())
        {
            killer.RpcRemoveAbilityUse();
            killer.SetDeathReason(PlayerState.DeathReason.Misfire);
            killer.SetRealKiller(killer);
            killer.RpcMurderPlayer(killer);
        }
        else if (isSuccess)
        {
            if (PassConverted.GetBool())
            {
                if (killer.IsAnySubRole(x => x.IsConverted() && x is not CustomRoles.Egoist))
                {
                    var role = killer.GetCustomSubRoles().FirstOrDefault(x => (x.IsConverted() || x is CustomRoles.Admired) && x is not CustomRoles.Egoist);
                    Logger.Info($"Giving addon {role} to {target.GetNameWithRole()}", "ChiefOfPolice");
                    target.RpcSetCustomRole(role);
                }
            }
        }
        else
        {
            killer.ResetKillCooldown();
            killer.SetKillCooldown(forceAnime: true);
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.ChiefOfPolice), GetString("PoliceFailedRecruit")));
            Utils.NotifyRoles(killer);
        }

        return false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("ChiefOfPoliceKillButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("CoPKill");
}
