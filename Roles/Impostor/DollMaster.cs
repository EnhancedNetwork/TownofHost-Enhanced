using AmongUs.GameOptions;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class DollMaster : RoleBase
{
    private static readonly HashSet<byte> ReducedVisionPlayers = [];
    public static bool isEnabled = false;
    private static bool IsControllingPlayer = false;
    private static bool ResetPlayerSpeed = false;
    private static bool WaitToUnPossess = false;
    private static PlayerControl controllingTarget = null; // Personal possessed player identifier for reference.
    private static PlayerControl DollMasterTarget = null; // Personal possessed player identifier for reference.
    private static float originalSpeed = float.MinValue;
    private static Vector2 controllingTargetPos = new(0, 0);
    private static Vector2 DollMasterPos = new(0, 0);
    //===========================SETUP================================\\
    private const int Id = 28500;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    public override bool IsExperimental => true;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem DefaultKillCooldown;
    private static OptionItem ShapeshiftCooldown;
    private static OptionItem ShapeshiftDuration;
    public static OptionItem CanKillAsMainBody;
    public static OptionItem TargetDiesAfterPossession;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.DollMaster);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DollMaster])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 11, "DollMasterPossessionCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DollMaster])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftDuration = FloatOptionItem.Create(Id + 12, "DollMasterPossessionDuration", new(0f, 180f, 2.5f), 10f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DollMaster])
            .SetValueFormat(OptionFormat.Seconds);
        CanKillAsMainBody = BooleanOptionItem.Create(Id + 13, "DollMasterCanKillAsMainBody", true, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.DollMaster]);
        TargetDiesAfterPossession = BooleanOptionItem.Create(Id + 14, "DollMasterTargetDiesAfterPossession", false, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.DollMaster]);
    }

    public override void Init()
    {
        ReducedVisionPlayers.Clear();
        PlayerIds.Clear();
        isEnabled = true;
    }

    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
        DollMasterTarget = Utils.GetPlayerById(playerId);
        IsControllingPlayer = false;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = ShapeshiftDuration.GetFloat() + 0.35f;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = DefaultKillCooldown.GetFloat();

    // A quick check if a player is being possessed.
    public static bool IsDoll(byte id)
    {
        if (ReducedVisionPlayers.Contains(id))
            return true;
        return false;
    }

    // Set Vision to 0 for possessed Target.
    public static void SetVision(IGameOptions opt, PlayerControl target)
    {
        if (ReducedVisionPlayers.Contains(target.PlayerId))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, 0f * 0);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0f * 0);
        }
    }

    public override void OnFixedUpdate(PlayerControl pc) // Setup settings for main body when possessing and booting from vent.
    {
        if (controllingTarget != null && DollMasterTarget != null)
        {
            // Set settings.
            SetSettingsUpdate(controllingTarget);
            // Boot Possessed Player from vent if inside of a vent and if waiting.
            BootPossessedPlayerFromVentUpdate(controllingTarget);
            // If DollMaster can't be teleported start waiting to unpossess.
            WaitToUnPossessUpdate(DollMasterTarget, controllingTarget);
        }
    }

    private static void SetSettingsUpdate(PlayerControl target) // Set settings.
    {
        if (IsControllingPlayer)
        {
            Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
            ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
            target.MarkDirtySettings();
        }
        else if (ResetPlayerSpeed)
        {
            Main.AllPlayerSpeed[target.PlayerId] = originalSpeed;
            ReportDeadBodyPatch.CanReport[target.PlayerId] = true;
            target.MarkDirtySettings();
        }
        ReducedVisionPlayers.Remove(DollMasterTarget.PlayerId);
    }

    private static void BootPossessedPlayerFromVentUpdate(PlayerControl target) // Boot Possessed Player from vent if inside of a vent and if waiting.
    {
        if (IsControllingPlayer && target.inVent && !target.walkingToVent)
        {
            _ = new LateTask(() =>
            {
                if (!target.inVent || target.walkingToVent) return;
                target.MyPhysics.RpcBootFromVent(GetPlayerVentId(target));
            }, 0.25f, "Boot Possessed Player from vent: " + GetPlayerVentId(target));
        }
    }

    private static void WaitToUnPossessUpdate(PlayerControl pc, PlayerControl target) // If DollMaster can't be teleported start waiting to unpossess.
    {
        if (IsControllingPlayer && WaitToUnPossess)
        {
            // Boot DollMaster from vent if inside of a vent and if waiting.
            if (pc.inVent && !pc.walkingToVent && !pc.MyPhysics.Animations.IsPlayingEnterVentAnimation())
            {
                _ = new LateTask(() =>
                {
                    if (!pc.inVent || pc.walkingToVent || pc.MyPhysics.Animations.IsPlayingEnterVentAnimation()) return;
                    pc.MyPhysics.RpcBootFromVent(GetPlayerVentId(pc));
                }, 0.3f, "Boot DollMaster from vent: " + GetPlayerVentId(pc));
            }
            // Unpossessed after waiting for DollMaster.
            if (pc.CanBeTeleported())
            {
                _ = new LateTask(() =>
                {
                    if (!WaitToUnPossess) return;
                    UnPossess(pc, target);
                    GetPlayersPositions(pc);
                    SwapPlayersPositions(pc);
                }, 0.15f, "UnPossess");
            }
        }
    }

    // Get vent Id that the player is in.
    private static int GetPlayerVentId(PlayerControl pc)
    {
        if (!(ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out var systemType) &&
              systemType.TryCast<VentilationSystem>() is VentilationSystem ventilationSystem))
            return 0;

        return ventilationSystem.PlayersInsideVents.TryGetValue(pc.PlayerId, out var playerIdVentId) ? playerIdVentId : 0;
    }

    // Prepare for a meeting if possessing.
    public override void OnReportDeadBody(PlayerControl pc, PlayerControl target) // Fix crap when meeting gets called.
    {
        if (IsControllingPlayer && controllingTarget != null && DollMasterTarget != null)
        {
            bool shouldAnimate = false;
            Utils.GetPlayerById(DollMasterTarget.PlayerId).RpcShapeshift(Utils.GetPlayerById(DollMasterTarget.PlayerId), shouldAnimate);
            Utils.GetPlayerById(controllingTarget.PlayerId).RpcShapeshift(Utils.GetPlayerById(controllingTarget.PlayerId), shouldAnimate);
            UnPossess(Utils.GetPlayerById(DollMasterTarget.PlayerId), Utils.GetPlayerById(controllingTarget.PlayerId));
            Main.AllPlayerSpeed[controllingTarget.PlayerId] = originalSpeed;
            ReportDeadBodyPatch.CanReport[controllingTarget.PlayerId] = true;
            ReducedVisionPlayers.Clear();
        }
    }


    // Prevent possessed player from reporting body.
    public override bool OnCheckReportDeadBody(PlayerControl reporter, GameData.PlayerInfo deadBody, PlayerControl killer)
    {
        if (ReducedVisionPlayers.Contains(reporter.PlayerId)) return false;
        return true;
    }

    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target) // Swap player kill interactions to each other when possessing.
    {
        if (!IsControllingPlayer || controllingTarget == null || DollMasterTarget == null) return false;

        // If Target as DollMaster Main Body gets killed, kill DollMaster instead.
        if (target.PlayerId == controllingTarget.PlayerId)
        {
            PlayerControl playertarget = Utils.GetPlayerById(controllingTarget.PlayerId);
            PlayerControl dollmaster = Utils.GetPlayerById(DollMasterTarget.PlayerId);
            dollmaster.RpcRemovePet();
            UnPossess(dollmaster, playertarget);
            GetPlayersPositions(dollmaster);
            SwapPlayersPositions(dollmaster);
            if (killer == DollMasterTarget) controllingTarget.RpcTeleport(DollMasterTarget.GetCustomPosition());
            killer.RpcMurderPlayer(dollmaster);
            return true;
        }
        // If DollMaster gets killed as possessed Target, kill possessed Target instead.
        else if (target.PlayerId == DollMasterTarget.PlayerId)
        {
            PlayerControl playertarget = Utils.GetPlayerById(controllingTarget.PlayerId);
            PlayerControl dollmaster = Utils.GetPlayerById(DollMasterTarget.PlayerId);
            target.RpcRemovePet();
            UnPossess(dollmaster, playertarget);
            GetPlayersPositions(dollmaster);
            SwapPlayersPositions(dollmaster);
            killer.RpcMurderPlayer(playertarget);
            return true;
        }

        return false;
    }

    public override bool CanUseKillButton(PlayerControl pc) => CanKillAsMainBody.GetBool() || IsControllingPlayer;

    public override bool OnCheckShapeshift(PlayerControl pc, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate) // Possession stuff.
    {
        if (WaitToUnPossess) return false;
        if (target == null) return false;
        shouldAnimate = false;
        resetCooldown = false;

        // If DollMaster can't be tp wait to UnPosses.
        if (IsControllingPlayer && !pc.CanBeTeleported())
        {
            WaitToUnPossess = true;
            return false;
        }

        // If players can be taken over.
        if (!target.IsAlive() || !target.CanBeTeleported() || Pelican.IsEaten(pc.PlayerId) || Pelican.IsEaten(target.PlayerId))
        {
            AURoleOptions.ShapeshifterCooldown = 0;
            pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DollMaster), target.IsAlive() ? GetString("CouldNotSwapWithTarget") : GetString("CanNotSwapWithDeadTarget")));
            return false;
        }

        ReducedVisionPlayers.Add(target.PlayerId);

        // Possess Player & UnPossess Player.
        if (!IsControllingPlayer)
        {
            if (target != null) controllingTarget = Utils.GetPlayerById(target.PlayerId);
            originalSpeed = Main.AllPlayerSpeed[target.PlayerId];
            IsControllingPlayer = true;

            _ = new LateTask(() =>
            {
                Possess(pc, target);
                GetPlayersPositions(pc);
                SwapPlayersPositions(pc);
            }, 0.35f);
            return false;
        }
        else if (controllingTarget != null)
        {
            UnPossess(pc, Utils.GetPlayerById(controllingTarget.PlayerId));
            GetPlayersPositions(pc);
            SwapPlayersPositions(pc);
        }
        return false;
    }

    // Possess Player
    private static void Possess(PlayerControl pc, PlayerControl target, bool shouldAnimate = false)
    {
        (target.MyPhysics.FlipX, pc.MyPhysics.FlipX) = (pc.MyPhysics.FlipX, target.MyPhysics.FlipX); // Copy the players directions that they are facing, Note this only works for modded clients!
        pc.RpcShapeshift(target, shouldAnimate);
        target.RpcShapeshift(pc, shouldAnimate);
        pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DollMaster), GetString("DollMaster_PossessedTarget")));
    }

    // UnPossess Player
    private static void UnPossess(PlayerControl pc, PlayerControl target, bool shouldAnimate = false)
    {
        WaitToUnPossess = false;
        (target.MyPhysics.FlipX, pc.MyPhysics.FlipX) = (pc.MyPhysics.FlipX, target.MyPhysics.FlipX); // Copy the players directions that they are facing, Note this only works for modded clients!
        pc.RpcShapeshift(pc, shouldAnimate);
        target.RpcShapeshift(target, shouldAnimate);
        pc.RpcResetAbilityCooldown();

        IsControllingPlayer = false;
        ResetPlayerSpeed = true;
        _ = new LateTask(() =>
        {
            ReducedVisionPlayers.Clear();
            if (TargetDiesAfterPossession.GetBool() && !GameStates.IsMeeting) target.RpcMurderPlayer(target);
        }, 0.35f);
    }

    // Get players locations.
    private static void GetPlayersPositions(PlayerControl pc)
    {
        if (controllingTarget == null) return;
        controllingTargetPos = controllingTarget.GetCustomPosition();
        DollMasterPos = pc.GetCustomPosition();
    }

    // Swap location with DollMaster and Target.
    private static void SwapPlayersPositions(PlayerControl pc)
    {
        if (controllingTarget == null) return;
        controllingTarget.RpcTeleport(DollMasterPos);
        pc.RpcTeleport(controllingTargetPos);
    }

    // Set name Suffix for Doll and Main Body under name.
    public override string GetSuffix(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
    {
        if (!IsControllingPlayer) return string.Empty;
        if (GameStates.IsMeeting) return string.Empty;
        if (target == null) return string.Empty;
        if (controllingTarget == null) return string.Empty;
        if (DollMasterTarget == null) return string.Empty;
        if (seer.PlayerId != target.PlayerId && target.PlayerId == controllingTarget.PlayerId) return "<color=#ffea00>" + GetString("DollMaster_MainBody");
        if (seer.PlayerId == target.PlayerId && target.PlayerId == DollMasterTarget.PlayerId) return "<color=#ffea00>" + GetString("DollMaster_Doll");
        return string.Empty;
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId) => hud.AbilityButton.OverrideText(GetString(IsControllingPlayer ? "DollMasterUnPossessionButtonText" : "DollMasterPossessionButtonText"));

    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Puttpuer");
}