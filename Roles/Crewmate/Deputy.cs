﻿using AmongUs.GameOptions;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Deputy : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Deputy;
    private const int Id = 7800;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem HandcuffCooldown;
    private static OptionItem HandcuffMax;
    private static OptionItem DeputyHandcuffCDForTarget;
    private static OptionItem HandcuffBrokenAfterMeeting;

    private static readonly Dictionary<byte, List<byte>> RoleblockedPlayers = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Deputy);
        HandcuffCooldown = FloatOptionItem.Create(Id + 10, "DeputyHandcuffCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deputy])
            .SetValueFormat(OptionFormat.Seconds);
        DeputyHandcuffCDForTarget = FloatOptionItem.Create(Id + 14, "DeputyHandcuffCDForTarget", new(0f, 180f, 2.5f), 45f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deputy])
            .SetValueFormat(OptionFormat.Seconds);
        HandcuffMax = IntegerOptionItem.Create(Id + 12, "DeputyHandcuffMax", new(1, 30, 1), 15, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deputy])
            .SetValueFormat(OptionFormat.Times);
        HandcuffBrokenAfterMeeting = BooleanOptionItem.Create(Id + 16, "HandcuffBrokenAfterMeeting", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deputy]);
    }
    public override void Init()
    {
        RoleblockedPlayers.Clear();
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = HandcuffMax.GetInt();
        RoleblockedPlayers[playerId] = [];
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = HandcuffCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && AbilityLimit >= 1;
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (AbilityLimit < 1) return false;
        if (killer == null || target == null) return false;

        Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} : Limit {AbilityLimit}", "Deputy");

        if (target.PlayerId != _Player.PlayerId)
        {
            if (!RoleblockedPlayers[killer.PlayerId].Contains(target.PlayerId))
            {
                RoleblockedPlayers[killer.PlayerId].Add(target.PlayerId);
                AbilityLimit--;
                SendSkillRPC();

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Deputy), GetString("DeputyHandcuffedPlayer")));
                killer.SetKillCooldown();
            }
            else
            {
                // Target already have a handcuff
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Deputy), GetString("DeputyInvalidTarget")));
            }
        }
        
        return false;
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (!IsRoleblocked(killer.PlayerId) && killer.GetCustomRole() is not CustomRoles.SerialKiller or CustomRoles.Pursuer or CustomRoles.Deputy or CustomRoles.Deceiver or CustomRoles.Poisoner) return false; // I was told these roles should be roleblock immune
        if (killer == null) return false;
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Deputy), GetString("HandcuffedByDeputy")));
        killer.SetKillCooldownV3(DeputyHandcuffCDForTarget.GetFloat(), forceAnime: !DisableShieldAnimations.GetBool());
        killer.ResetKillCooldown();

        RemoveRoleblock(killer.PlayerId);
        Logger.Info($"{killer.GetRealName()} fail ability because roleblocked", "Deputy");
        return true;
    }
    public override void AfterMeetingTasks()
    {
        if (HandcuffBrokenAfterMeeting.GetBool())
        {
            foreach (var player in RoleblockedPlayers.Keys)
            {
                RoleblockedPlayers[player].Clear();
            }
        }
    }
    private static bool IsRoleblocked(byte target)
    {
        if (RoleblockedPlayers.Count < 1) return false;
        foreach (var player in RoleblockedPlayers.Keys)
        {
            if (RoleblockedPlayers[player].Contains(target)) return true;
        }
        return false;
    }

    private static void RemoveRoleblock(byte target)
    {
        if (RoleblockedPlayers.Count < 1) return;
        foreach (var player in RoleblockedPlayers.Keys)
        {
            if (RoleblockedPlayers[player].Contains(target)) 
                RoleblockedPlayers[player].Remove(target);
        }
    }
    public override string GetProgressText(byte PlayerId, bool comms) => Utils.ColorString(AbilityLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Deputy) : Color.gray, $"({AbilityLimit})");
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.KillButton.OverrideText(GetString("DeputyHandcuffText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Deputy");
}
