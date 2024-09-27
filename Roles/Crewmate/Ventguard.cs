﻿using AmongUs.GameOptions;
using System;
using System.Text;
using UnityEngine;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Ventguard : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 30000;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Ventguard);
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem MaxGuards;
    private static OptionItem BlockVentCooldown;
    private static OptionItem BlockDoesNotAffectCrew;
    private static OptionItem BlocksResetOnMeeting;

    private readonly HashSet<int> BlockedVents = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Ventguard);
        MaxGuards = IntegerOptionItem.Create(Id + 10, "Ventguard_MaxGuards", new(1, 30, 1), 3, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Ventguard]);
        BlockVentCooldown = IntegerOptionItem.Create(Id + 11, "Ventguard_BlockVentCooldown", new(1, 250, 1), 15, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Ventguard])
            .SetValueFormat(OptionFormat.Seconds);
        BlockDoesNotAffectCrew = BooleanOptionItem.Create(Id + 12, "Ventguard_BlockDoesNotAffectCrew", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Ventguard]);
        BlocksResetOnMeeting = BooleanOptionItem.Create(Id + 13, "Ventguard_BlocksResetOnMeeting", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Ventguard]);
        VentguardAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 14, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.05f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Ventguard])
            .SetValueFormat(OptionFormat.Times);
    }

    public override void Init()
    {
        BlockedVents.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(MaxGuards.GetInt());
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = BlockVentCooldown.GetInt();
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.buttonLabelText.text = GetString("VentguardVentButtonText");
    }

    public override void OnEnterVent(PlayerControl ventguard, Vent vent)
    {
        if (ventguard.GetAbilityUseLimit() > 0)
        {
            ventguard.RpcRemoveAbilityUse();

            var ventId = vent.Id;
            BlockedVents.Add(ventId);
            foreach (var player in Main.AllPlayerControls)
            {
                if (!player.IsAlive()) continue;
                if (player.NotUnlockVent(ventId)) continue;
                if (ventguard.PlayerId != player.PlayerId && BlockDoesNotAffectCrew.GetBool() && player.Is(Custom_Team.Crewmate)) continue;

                CustomRoleManager.BlockedVentsList[player.PlayerId].Add(ventId);
                player.RpcSetVentInteraction();
            }
            ventguard.Notify(GetString("VentIsBlocked"));
            _ = new LateTask(() => ventguard?.MyPhysics?.RpcBootFromVent(ventId), 0.5f, $"Ventguard {ventguard.PlayerId} Boot From Vent");
        }
        else
        {
            ventguard.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
        }
    }
    public override void AfterMeetingTasks()
    {
        if (BlocksResetOnMeeting.GetBool() && BlockedVents.Any())
        {
            foreach (var ventId in BlockedVents)
            {
                foreach (var player in Main.AllPlayerControls)
                {
                    if (!player.IsAlive()) continue;
                    if (player.NotUnlockVent(ventId)) continue;
                    if (player.PlayerId != _Player?.PlayerId && BlockDoesNotAffectCrew.GetBool() && player.Is(Custom_Team.Crewmate)) continue;

                    CustomRoleManager.BlockedVentsList[player.PlayerId].Remove(ventId);
                }
            }
            BlockedVents.Clear();
        }
    }
}