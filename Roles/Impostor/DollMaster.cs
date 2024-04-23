using AmongUs.GameOptions;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class DollMaster : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 28500;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    public override bool IsEnable => HasEnabled;
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\
    private static readonly HashSet<byte> ReducedVisionPlayers = [];
    public static bool isEnabled = false;
    private static bool IsControllingPlayer = false;
    private static bool ResetPlayerSpeed = false;
    private static bool WaitToUnPossess = false;
    private static int ControllingPlayerId = -99; // Possessed player ID for reference.
    private static int DollMasterPlayerId = -99; // Killer player ID for reference.
    private static PlayerControl controllingTarget = null; // Personal possessed player identifier for reference.
    private static PlayerControl DollMasterTarget = null; // Personal possessed player identifier for reference.
    private static float originalSpeed = 0;
    private static Vector2 controllingTargetPos = new(0, 0);
    private static Vector2 DollMasterPos = new(0, 0);

    private static OptionItem DefaultKillCooldown;
    private static OptionItem ShapeshiftCooldown;
    private static OptionItem ShapeshiftDuration;
    public static OptionItem CanKillAsMainBody;

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

    public override void OnFixedUpdate(PlayerControl pc) // Setup settings for main body when possessing and more.
    {
        if (controllingTarget != null && DollMasterTarget != null && Main.AllPlayerSpeed.ContainsKey(controllingTarget.PlayerId))
        {
            var TempDollMasterTarget = Utils.GetPlayerById(DollMasterTarget.PlayerId);
            var TempControllingTarget = Utils.GetPlayerById(controllingTarget.PlayerId);
            // Set settings.
            if (IsControllingPlayer)
            {
                Main.AllPlayerSpeed[controllingTarget.PlayerId] = Main.MinSpeed;
                ReportDeadBodyPatch.CanReport[controllingTarget.PlayerId] = false;
                controllingTarget.MarkDirtySettings();
            }
            else if (ResetPlayerSpeed)
            {
                Main.AllPlayerSpeed[controllingTarget.PlayerId] = originalSpeed;
                ReportDeadBodyPatch.CanReport[controllingTarget.PlayerId] = true;
                controllingTarget.MarkDirtySettings();
            }
            ReducedVisionPlayers.Remove(DollMasterTarget.PlayerId);

            // Boot Possessed Player from vent if inside of a vent and if waiting.
            if (IsControllingPlayer && TempControllingTarget.inVent && !TempControllingTarget.walkingToVent)
            {
                _ = new LateTask(() =>
                {
                    if (!TempControllingTarget.inVent || TempControllingTarget.walkingToVent) return;
                    TempControllingTarget.MyPhysics.RpcBootFromVent(GetPlayerVentId(TempControllingTarget));
                }, 0.25f, "Boot Possessed Player from vent: " + GetPlayerVentId(TempControllingTarget));
            }

            // If DollMaster can't be teleported start waiting to unpossess.
            if (IsControllingPlayer && WaitToUnPossess)
            {
                // Boot DollMaster from vent if inside of a vent and if waiting.
                if (TempDollMasterTarget.inVent && !TempDollMasterTarget.walkingToVent)
                {
                    _ = new LateTask(() =>
                    {
                        if (!TempDollMasterTarget.inVent || TempDollMasterTarget.walkingToVent) return;
                        TempDollMasterTarget.MyPhysics.RpcBootFromVent(GetPlayerVentId(TempDollMasterTarget));
                    }, 0.25f, "Boot DollMaster from vent: " + GetPlayerVentId(TempDollMasterTarget));
                }
                // Unpossessed after waiting for DollMaster.
                if (TempDollMasterTarget.CanBeTeleported())
                {
                    _ = new LateTask(() =>
                    {
                        if (!WaitToUnPossess) return;
                        UnPossess(TempDollMasterTarget, TempControllingTarget);
                        GetPlayersPositions(TempDollMasterTarget);
                        SwapPlayersPositions(TempDollMasterTarget);
                    }, 0.15f, "UnPossess");
                }
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
        if (IsControllingPlayer && controllingTarget != null)
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
        if (target.PlayerId == ControllingPlayerId)
        {
            PlayerControl playertarget = Utils.GetPlayerById(ControllingPlayerId);
            PlayerControl dollmaster = Utils.GetPlayerById(DollMasterPlayerId);
            UnPossess(dollmaster, playertarget);
            GetPlayersPositions(dollmaster);
            SwapPlayersPositions(dollmaster);
            if (killer == DollMasterTarget) controllingTarget.RpcTeleport(DollMasterTarget.GetCustomPosition());
            killer.RpcMurderPlayer(dollmaster);
            return true;
        }
        // If DollMaster gets killed as possessed Target, kill possessed Target instead.
        else if (target.PlayerId == DollMasterPlayerId)
        {
            PlayerControl playertarget = Utils.GetPlayerById(ControllingPlayerId);
            PlayerControl dollmaster = Utils.GetPlayerById(DollMasterPlayerId);
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
            pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DollMaster), target.IsAlive() ? GetString("CouldNotSwap") : GetString("CanNotSwapWithDead")));
            return false;
        }

        ReducedVisionPlayers.Add(target.PlayerId);

        // Possess Player & UnPossess Player.
        if (!IsControllingPlayer)
        {
            if (target != null) controllingTarget = Utils.GetPlayerById(target.PlayerId);
            ControllingPlayerId = target.PlayerId;
            DollMasterPlayerId = pc.PlayerId;
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
        pc.RpcShapeshift(target, shouldAnimate);
        target.RpcShapeshift(pc, shouldAnimate);
        pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DollMaster), GetString("PossessedTarget")));
    }

    // UnPossess Player
    private static void UnPossess(PlayerControl pc, PlayerControl target, bool shouldAnimate = false)
    {
        WaitToUnPossess = false;
        pc.RpcShapeshift(pc, shouldAnimate);
        target.RpcShapeshift(target, shouldAnimate);
        pc.RpcResetAbilityCooldown();

        IsControllingPlayer = false;
        ResetPlayerSpeed = true;
        _ = new LateTask(() =>
        {
            ReducedVisionPlayers.Clear();
        }, 0.35f);
    }

    // Set name Suffix for Doll and Main Body under name.
    public override string GetSuffix(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
    {
        if (!seer.Is(CustomRoles.DollMaster)) return string.Empty;
        if (GameStates.IsMeeting) return string.Empty;
        if (controllingTarget == null) return string.Empty;
        if (target != null && seer.PlayerId == controllingTarget.PlayerId) return string.Empty;
        if (target != null && seer.PlayerId != target.PlayerId && IsControllingPlayer) return "<color=#ffea00>" + GetString("MainBody");
        if (!IsControllingPlayer) return string.Empty;
        return "<color=#ffea00>" + GetString("Doll");
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

    public override void SetAbilityButtonText(HudManager hud, byte playerId) => hud.AbilityButton.OverrideText(GetString(IsControllingPlayer ? "DollMasterUnPossessionButtonText" : "DollMasterPossessionButtonText"));

    // public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Puttpuer");
}
