﻿using AmongUs.GameOptions;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
namespace TOHE.Roles.Impostor;

internal class Hangman : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 24500;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Hangman);
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem ShapeshiftCooldown;
    private static OptionItem ShapeshiftDuration;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Hangman);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 2, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(1f, 180f, 1f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hangman])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftDuration = FloatOptionItem.Create(Id + 4, GeneralOption.ShapeshifterBase_ShapeshiftDuration, new(1f, 60f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hangman])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = ShapeshiftDuration.GetFloat();
    }
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.IsTransformedNeutralApocalypse())
            return true;

        if (target.Is(CustomRoles.Madmate) && !Madmate.ImpCanKillMadmate.GetBool())
            return false;

        if (Main.CheckShapeshift.TryGetValue(killer.PlayerId, out var isShapeshift) && isShapeshift)
        {
            target.SetDeathReason(PlayerState.DeathReason.LossOfHead);
            target.RpcExileV2();
            Main.PlayerStates[target.PlayerId].SetDead();
            target.Data.IsDead = true;
            target.SetRealKiller(killer);

            killer.SetKillCooldown();
            //MurderPlayerPatch.AfterPlayerDeathTasks(killer, target, false);
            return false;
        }
        return true;
    }

    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => shapeshifting ? CustomButton.Get("Hangman") : null;
}