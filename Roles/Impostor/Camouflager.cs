using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules.Rpc;
using TOHE.Roles.AddOns.Common;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Camouflager : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Camouflager;
    private const int Id = 2900;
    public static readonly HashSet<byte> Playerids = [];
    public static bool HasEnabled => Playerids.Any();

    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem CamouflageCooldownOpt;
    private static OptionItem CamouflageDurationOpt;
    private static OptionItem CanUseCommsSabotagOpt;
    private static OptionItem DisableReportWhenCamouflageIsActiveOpt;
    private static OptionItem ShowShapeshiftAnimationsOpt;

    public static bool AbilityActivated = false;
    private static float CamouflageCooldown;
    private static float CamouflageDuration;
    private static bool CanUseCommsSabotage;
    private static bool DisableReportWhenCamouflageIsActive;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Camouflager);
        CamouflageCooldownOpt = FloatOptionItem.Create(Id + 2, "CamouflageCooldown", new(1f, 180f, 1f), 25f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager])
            .SetValueFormat(OptionFormat.Seconds);
        CamouflageDurationOpt = FloatOptionItem.Create(Id + 4, "CamouflageDuration", new(1f, 180f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager])
            .SetValueFormat(OptionFormat.Seconds);
        CanUseCommsSabotagOpt = BooleanOptionItem.Create(Id + 6, "CanUseCommsSabotage", false, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager]);
        DisableReportWhenCamouflageIsActiveOpt = BooleanOptionItem.Create(Id + 8, "DisableReportWhenCamouflageIsActive", false, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager]);
        ShowShapeshiftAnimationsOpt = BooleanOptionItem.Create(Id + 9, GeneralOption.ShowShapeshiftAnimations, true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager]);

    }
    public override void Init()
    {
        AbilityActivated = false;
        Playerids.Clear();
    }
    public override void Add(byte playerId)
    {
        CamouflageCooldown = CamouflageCooldownOpt.GetFloat();
        CamouflageDuration = CamouflageDurationOpt.GetFloat();
        CanUseCommsSabotage = CanUseCommsSabotagOpt.GetBool();
        DisableReportWhenCamouflageIsActive = DisableReportWhenCamouflageIsActiveOpt.GetBool();

        if (!Playerids.Contains(playerId))
            Playerids.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        ClearCamouflage();
        Playerids.Remove(playerId);
    }

    private void SendRPC()
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(AbilityActivated);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        AbilityActivated = reader.ReadBoolean();
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = CamouflageCooldown;
        AURoleOptions.ShapeshifterDuration = CamouflageDuration;
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Camo");
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (AbilityActivated)
            hud.AbilityButton.OverrideText(GetString("CamouflagerShapeshiftTextAfterDisguise"));
        else
            hud.AbilityButton.OverrideText(GetString("CamouflagerShapeshiftTextBeforeDisguise"));
    }
    public override bool OnCheckShapeshift(PlayerControl camouflager, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        if (ShowShapeshiftAnimationsOpt.GetBool()) return true;

        shouldAnimate = false;
        return true;
    }
    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool IsAnimate, bool shapeshifting)
    {
        if (!shapeshifting)
        {
            ClearCamouflage();
            return;
        }

        AbilityActivated = true;
        SendRPC();

        var timer = ShowShapeshiftAnimationsOpt.GetBool() ? 1.2f : 0.5f;

        _ = new LateTask(() =>
        {
            if (!Main.MeetingIsStarted && GameStates.IsInTask)
            {
                Camouflage.CheckCamouflage();
            }
        }, timer, "Camouflager Use Shapeshift");
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        ClearCamouflage();
    }

    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (inMeeting || !AbilityActivated) return;

        ClearCamouflage();
    }

    public static bool CantPressCommsSabotageButton(PlayerControl player) => player.Is(CustomRoles.Camouflager) && !CanUseCommsSabotage;

    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        if (deadBody.Object != null && deadBody.Object.Is(CustomRoles.Bait) && Bait.BaitCanBeReportedUnderAllConditions.GetBool()) return true;

        return !(DisableReportWhenCamouflageIsActive && AbilityActivated && !(Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool()));
    }

    private void ClearCamouflage()
    {
        AbilityActivated = false;
        SendRPC();
        Camouflage.CheckCamouflage();
    }
}
