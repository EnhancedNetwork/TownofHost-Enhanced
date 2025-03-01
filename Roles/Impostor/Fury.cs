using AmongUs.GameOptions;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

//Reference to part of the code
// 部分代码参考：https://github.com/TOHOptimized/TownofHost-Optimized
// Button Sprite
//贴图来源 : https://github.com/Dolly1016/Nebula-Public
internal class Fury : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Fury;
    const int Id = 31400;
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    public bool FuryAngry = false;
    private LateTask _;
    private float tmpSpeed;
    private float tmpKillCooldown;
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
            if (!CanStartMeetingWhenAngry.GetBool() && FuryAngry == true)
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

    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => FuryAngry ? CustomButton.Get("Calm") : CustomButton.Get("Rage");

    public override void SetAbilityButtonText(HudManager hud, byte playerid)
    {
        if (!FuryAngry)
            hud.AbilityButton.OverrideText(GetString("FuryAngryShapeshiftTextBeforeDisguise"));
        else
            hud.AbilityButton.OverrideText(GetString("FuryAngryShapeshiftTextAfterDisguise"));
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = FuryShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
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

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        FuryAngry = false;
    }
    public override void UnShapeShiftButton(PlayerControl player)
    {
        FuryAngry = !FuryAngry;
        if (FuryAngry)
        {
            EnterFuryMode(player);
        }
        else
        {
            ExitFuryMode(player);
        }
        if (Main.MeetingIsStarted)
        {
            FuryAngry = false;
            _?.Cancel();
        }
    }

    private void EnterFuryMode(PlayerControl player)
    {
        player.SetKillCooldown(FuryKillCooldownWhenAngry.GetFloat(), null, false);
        player.Notify(GetString("FuryInRage"), FuryAngryDuration.GetFloat(), true);
        tmpSpeed = Main.AllPlayerSpeed[player.PlayerId];
        tmpKillCooldown = Main.AllPlayerKillCooldown[player.PlayerId];
        foreach (PlayerControl playerControl in Main.AllPlayerControls)
        {
            playerControl.KillFlash(true);
            RPC.PlaySoundRPC(player.PlayerId, Sounds.ImpTransform);
            playerControl.Notify(GetString("SeerFuryInRage"), 5f, true);
        }
        Main.AllPlayerSpeed[player.PlayerId] = FuryMaxSpeedWhenAngry.GetFloat();
        Main.AllPlayerKillCooldown[player.PlayerId] = FuryKillCooldownWhenAngry.GetFloat();
        AURoleOptions.ShapeshifterCooldown = 0;
        player.MarkDirtySettings();

        _ = new LateTask(() =>
        {
            if (FuryAngry)
            {
                FuryAngry = false;
                ExitFuryMode(player);
            }
        }, FuryAngryDuration.GetFloat(), "FuryAngryTask", true);
    }

    private void ExitFuryMode(PlayerControl player)
    {
        FuryAngry = false;
        Main.AllPlayerSpeed[player.PlayerId] = Main.AllPlayerSpeed[player.PlayerId] - FuryMaxSpeedWhenAngry.GetFloat() + tmpSpeed;
        Main.AllPlayerKillCooldown[player.PlayerId] = Main.AllPlayerKillCooldown[player.PlayerId] - FuryKillCooldownWhenAngry.GetFloat() + tmpKillCooldown;
        player.MarkDirtySettings();
        player.Notify(GetString("FuryCalmDown"), FuryAngryDuration.GetFloat(), true);
        foreach (PlayerControl playerControl in Main.AllPlayerControls)
        {
            playerControl.KillFlash(false);
            RPC.PlaySoundRPC(player.PlayerId, Sounds.KillSound);
            playerControl.Notify(GetString("SeerFuryCalmDown"), 5f, true);
        }
        _?.Cancel();
    }
}
