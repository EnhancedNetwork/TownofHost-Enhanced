using AmongUs.GameOptions;
using Hazel;
using TOHE.Roles.Core;
using TOHE.Roles.Impostor;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Fury : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Fury;
    const int Id = 31400;
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    public static bool FuryAngry = false;
    private static OptionItem FuryKillCooldownWhenAngry;
    private static OptionItem FuryShapeshiftCooldown;
    private static OptionItem FuryMaxSpeedWhenAngry;
    private static OptionItem FuryMaxVisionWhenAngry;
    private static OptionItem FuryAngryDuration;
    private static OptionItem CanStartMeetingWhenAngry;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Fury);
        FuryKillCooldownWhenAngry = FloatOptionItem.Create(Id + 9, "FuryKillCooldownWhenAngry", new(1f, 180f, 1f), 2f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Seconds);
        FuryShapeshiftCooldown = FloatOptionItem.Create(Id + 10, "FuryShapeshiftCooldown", new(1f, 180f, 0.5f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Seconds);
        FuryMaxSpeedWhenAngry = FloatOptionItem.Create(Id + 11, "FuryMaxSpeedWhenAngry", new(0.25f, 5f, 0.25f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Multiplier);
        FuryMaxVisionWhenAngry = FloatOptionItem.Create(Id + 12, "FuryMaxVisionWhenAngry", new(0f, 5f, 0.05f), 0.25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Multiplier);
        FuryAngryDuration = FloatOptionItem.Create(Id + 13, "FuryAngryDuration", new(1f, 30f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Seconds);
        CanStartMeetingWhenAngry = BooleanOptionItem.Create(Id + 14, "CanStartMeetingWhenAngry", false, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury]);
    }

    public override bool OnCheckStartMeeting(PlayerControl reporter)
    {
        foreach (PlayerControl playerControl in Main.AllPlayerControls)
        {
            if (!CanStartMeetingWhenAngry.GetBool() & FuryAngry == true)
            {
                return false;
            }
            if (CanStartMeetingWhenAngry.GetBool() & FuryAngry == true)
            {
                return true;
            }
        }
        return true;
    }

    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Fury");

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("FuryAngryShapeshiftTextAfterDisguise"));
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = FuryShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = FuryAngryDuration.GetFloat();
        if (FuryAngry)
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, FuryMaxVisionWhenAngry.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, FuryMaxVisionWhenAngry.GetFloat());
        }
        else
        {
            opt.SetVision(true);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
        }
    }

    public override void UnShapeShiftButton(PlayerControl player)
    {
        player.SetKillCooldown(FuryKillCooldownWhenAngry.GetFloat(), null, false);
        player.Notify(GetString("FuryInRage", null, false, true, false), FuryAngryDuration.GetFloat(), true);
        FuryAngry = true;
        foreach (PlayerControl playerControl in Main.AllPlayerControls)
        {
            if (FuryAngry)
            {
                playerControl.KillFlash(true);
                //CustomSoundsManager.RPCPlayCustomSoundAll("ImpTransform");
                RPC.PlaySoundRPC(player.PlayerId, Sounds.ImpTransform);
                playerControl.Notify(GetString("SeerFuryInRage", null, false, true, false), 5f, true);
            }
        }
        player.MarkDirtySettings();
        float tmpSpeed = Main.AllPlayerSpeed[player.PlayerId];
        float tmpKillCooldown = Main.AllPlayerKillCooldown[player.PlayerId];
        Main.AllPlayerSpeed[player.PlayerId] = FuryMaxSpeedWhenAngry.GetFloat();
        Main.AllPlayerKillCooldown[player.PlayerId] = FuryKillCooldownWhenAngry.GetFloat();

        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[player.PlayerId] = Main.AllPlayerSpeed[player.PlayerId] - FuryMaxSpeedWhenAngry.GetFloat() + tmpSpeed;
            Main.AllPlayerKillCooldown[player.PlayerId] = Main.AllPlayerKillCooldown[player.PlayerId] - FuryKillCooldownWhenAngry.GetFloat() + tmpKillCooldown;
            FuryAngry = false;
            player.MarkDirtySettings();
        }, FuryAngryDuration.GetFloat(), "No Name Task", true);
    }
}
