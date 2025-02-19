using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class DollMaster : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.DollMaster;
    private const int Id = 28500;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.DollMaster);
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\
    private static readonly HashSet<byte> ReducedVisionPlayers = [];
    public static bool IsControllingPlayer = false;
    private static bool ResetPlayerSpeed = false;
    private static bool WaitToUnPossess = false;
    public static PlayerControl controllingTarget = null; // Personal possessed player identifier for reference.
    public static PlayerControl DollMasterTarget = null; // Personal possessed player identifier for reference.
    private static float originalSpeed = float.MinValue;
    private static Vector2 controllingTargetPos = new(0, 0);
    private static Vector2 DollMasterPos = new(0, 0);

    private static OptionItem DefaultKillCooldown;
    private static OptionItem ShapeshiftCooldown;
    private static OptionItem ShapeshiftDuration;
    public static OptionItem CanKillAsMainBody;
    public static OptionItem TargetDiesAfterPossession;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.DollMaster);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DollMaster])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 11, "DollMasterPossessionCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DollMaster])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftDuration = FloatOptionItem.Create(Id + 12, "DollMasterPossessionDuration", new(0f, 180f, 2.5f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DollMaster])
            .SetValueFormat(OptionFormat.Seconds);
        CanKillAsMainBody = BooleanOptionItem.Create(Id + 13, "DollMasterCanKillAsMainBody", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.DollMaster]);
        TargetDiesAfterPossession = BooleanOptionItem.Create(Id + 14, "DollMasterTargetDiesAfterPossession", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.DollMaster]);
    }

    public override void Init()
    {
        ReducedVisionPlayers.Clear();
        DollMasterTarget = null;
        controllingTarget = null;
    }

    public override void Add(byte playerId)
    {
        DollMasterTarget = Utils.GetPlayerById(playerId);
        IsControllingPlayer = false;
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = ShapeshiftDuration.GetFloat() + 0.35f;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = DefaultKillCooldown.GetFloat();

    // A quick check if a player is being possessed.
    public static bool IsDoll(byte id) => ReducedVisionPlayers.Contains(id);

    // Set Vision and Speed to 0 for possessed Target.
    public static void ApplySettingsToDoll(IGameOptions opt)
    {
        opt.SetVision(false);
        opt.SetFloat(FloatOptionNames.CrewLightMod, 0f * 0);
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0f * 0);
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (lowLoad) return;
        if (controllingTarget != null && DollMasterTarget != null)
        {
            // If DollMaster can't be teleported start waiting to unpossess.
            WaitToUnPossessUpdate(DollMasterTarget, controllingTarget);
        }
        if (IsControllingPlayer && (controllingTarget == null || DollMasterTarget == null))
        {
            ReducedVisionPlayers.Clear();

            if (controllingTarget != null)
            {
                Main.AllPlayerSpeed[controllingTarget.PlayerId] = originalSpeed;
                controllingTarget.MarkDirtySettings();
            }

            DollMasterTarget?.RpcShapeshift(DollMasterTarget, false);
            controllingTarget?.ResetPlayerOutfit();

            IsControllingPlayer = false;
            ResetPlayerSpeed = true;
        }
    }
    private static void OnFixedUpdateOthers(PlayerControl target, bool lowLoad, long nowTime)
    {
        if (lowLoad) return;
        if (controllingTarget != null && target == controllingTarget)
        {
            // Boot Possessed Player from vent if inside of a vent and if waiting.
            BootPossessedPlayerFromVentUpdate(target);

            // Set speed
            if (IsControllingPlayer && Main.AllPlayerSpeed[target.PlayerId] >= Main.MinSpeed)
            {
                Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
                target.MarkDirtySettings();
            }
            else if (ResetPlayerSpeed)
            {
                Main.AllPlayerSpeed[target.PlayerId] = originalSpeed;
                target.MarkDirtySettings();
            }
        }
    }

    // Boot Possessed Player from vent if inside of a vent and if waiting.
    private static void BootPossessedPlayerFromVentUpdate(PlayerControl target)
    {
        if (IsControllingPlayer && target.inVent && !target.walkingToVent)
        {
            _ = new LateTask(() =>
            {
                if (!target.inVent || target.walkingToVent) return;
                target.MyPhysics.RpcBootFromVent(target.GetPlayerVentId());
            }, 0.25f, "Boot Possessed Player from vent: " + target.GetPlayerVentId());
        }
    }

    // If DollMaster can't be teleported start waiting to unpossess.
    private static void WaitToUnPossessUpdate(PlayerControl pc, PlayerControl target)
    {
        if (IsControllingPlayer && WaitToUnPossess)
        {
            // Boot DollMaster from vent if inside of a vent and if waiting.
            if (pc.inVent && !pc.walkingToVent && !pc.MyPhysics.Animations.IsPlayingEnterVentAnimation())
            {
                _ = new LateTask(() =>
                {
                    if (!pc.inVent || pc.walkingToVent || pc.MyPhysics.Animations.IsPlayingEnterVentAnimation()) return;
                    pc.MyPhysics.RpcBootFromVent(pc.GetPlayerVentId());
                }, 0.3f, "Boot DollMaster from vent: " + pc.GetPlayerVentId());
            }

            // Unpossessed after waiting for DollMaster.
            if (pc.CanBeTeleported() && target.CanBeTeleported())
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

    // Prepare for a meeting if possessing.
    public override void OnReportDeadBody(PlayerControl pc, NetworkedPlayerInfo target) // Fix crap when meeting gets called.
    {
        if (IsControllingPlayer && controllingTarget != null && DollMasterTarget != null)
        {
            UnPossess(DollMasterTarget, controllingTarget);
            Main.AllPlayerSpeed[controllingTarget.PlayerId] = originalSpeed;
            ReducedVisionPlayers.Clear();
        }
    }

    // If Dollmaster reports a body or is forced to while possessing redirect it to possessed player
    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        if (controllingTarget == null || DollMasterTarget == null) return true;

        if (IsControllingPlayer && IsDoll(reporter.PlayerId)) return false; // Prevent possessed player from reporting body.

        if (IsControllingPlayer && reporter.Is(CustomRoles.DollMaster))
        {
            UnPossess(DollMasterTarget, controllingTarget);
            GetPlayersPositions(DollMasterTarget);
            SwapPlayersPositions(DollMasterTarget);
            controllingTarget.CmdReportDeadBody(deadBody);
            return false;
        }

        return true;
    }

    // If Dollmaster starts a meeting while possessing redirect it to possessed player
    public override bool OnCheckStartMeeting(PlayerControl reporter)
    {
        if (controllingTarget == null || DollMasterTarget == null) return true;

        if (IsControllingPlayer && IsDoll(reporter.PlayerId)) return false; // Prevent possessed player from starting meeting.

        if (IsControllingPlayer && reporter.Is(CustomRoles.DollMaster))
        {
            UnPossess(DollMasterTarget, controllingTarget);
            GetPlayersPositions(DollMasterTarget);
            SwapPlayersPositions(DollMasterTarget);
            controllingTarget.CmdReportDeadBody(null);
            return false;
        }

        return true;
    }

    // Prevent Dollmaster from killing main body
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target) => (!(IsControllingPlayer && target == DollMasterTarget));

    // Handle specific killing roles when interacting with a Dollmaster or Player while possessing.
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (IsControllingPlayer && DollMasterTarget != null && controllingTarget != null)
        {
            if (!CanKillerUseAbility(killer)) return true;

            if (killer.GetCustomRole().IsImpostorTeam() && target == controllingTarget) return true;

            if (killer.Is(CustomRoles.Sheriff) && killer != DollMasterTarget && target == DollMasterTarget)
            {
                CheckMurderAsPossessed(killer, target);
                return true;
            }
        }
        return false;
    }

    // Check if Killer can use Ability on Target.
    // This is a list of roles that are Buggy when interacting with a possessed player, therefore their ability will be canceled out.
    private static bool CanKillerUseAbility(PlayerControl player)
    {
        var CanUseAbility = true;
        var cRole = player.GetCustomRole();
        //var subRoles = player.GetCustomSubRoles(); // May use later on!

        switch (cRole) // Check role.
        {
            case CustomRoles.Pelican:
                CanUseAbility = false;
                break;
            case CustomRoles.Penguin:
                CanUseAbility = false;
                break;
            default:
                break;
        }

        if (!CanUseAbility) player.Notify(Utils.ColorString(player.GetRoleColor(), GetString("DollMaster_UnableToUseAbility")));

        return CanUseAbility;
    }

    // Uno reverse kill.
    public static void CheckMurderAsPossessed(PlayerControl killer, PlayerControl target) // Swap player kill interactions to each other when possessing.
    {
        if (!IsControllingPlayer || controllingTarget == null || DollMasterTarget == null) return;

        // If Target as DollMaster Main Body gets killed, kill DollMaster instead.
        if (target == controllingTarget)
        {
            target.RpcSetPet("");
            UnPossess(DollMasterTarget, controllingTarget);
            GetPlayersPositions(DollMasterTarget);
            SwapPlayersPositions(DollMasterTarget);
            killer.RpcMurderPlayer(controllingTarget);
            return;
        }
        // If DollMaster gets killed as possessed Target, kill possessed Target instead.
        else if (target == DollMasterTarget)
        {
            DollMasterTarget.RpcSetPet("");
            UnPossess(DollMasterTarget, controllingTarget);
            GetPlayersPositions(DollMasterTarget);
            SwapPlayersPositions(DollMasterTarget);
            killer.RpcMurderPlayer(DollMasterTarget);
            return;
        }
    }

    public override bool CanUseKillButton(PlayerControl pc) => CanKillAsMainBody.GetBool() || IsControllingPlayer;

    public override bool OnCheckShapeshift(PlayerControl pc, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate) // Possession stuff.
    {
        if (WaitToUnPossess) return false;
        if (target == null) return false;
        shouldAnimate = false;
        resetCooldown = false;

        // If DollMaster can't be tp wait to UnPosses.
        if (!pc.CanBeTeleported() && IsControllingPlayer)
        {
            WaitToUnPossess = true;
            return false;
        }

        // If Target can't be tp wait to UnPosses.
        if (controllingTarget != null && !controllingTarget.CanBeTeleported() && IsControllingPlayer)
        {
            WaitToUnPossess = true;
            return false;
        }

        // If target is on imp team return.
        if (!IsControllingPlayer && target.GetCustomRole().IsImpostorTeam())
        {
            AURoleOptions.ShapeshifterCooldown = 0;
            pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DollMaster), GetString("DollMaster_CannotPossessImpTeammate")));
            return false;
        }

        // If players can be taken over.
        if (!target.CanBeTeleported() || Pelican.IsEaten(pc.PlayerId) || Pelican.IsEaten(target.PlayerId))
        {
            AURoleOptions.ShapeshifterCooldown = 0;
            pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DollMaster), target.IsAlive() ? GetString("DollMaster_CouldNotSwapWithTarget") : GetString("DollMaster_CanNotSwapWithDeadTarget")));
            return false;
        }

        ReducedVisionPlayers.Add(target.PlayerId);

        // Possess Player & UnPossess Player.
        if (!IsControllingPlayer)
        {
            if (target != null) controllingTarget = target;
            originalSpeed = Main.AllPlayerSpeed[target.PlayerId];
            IsControllingPlayer = true;

            _ = new LateTask(() =>
            {
                if (GameStates.IsMeeting || GameStates.IsExilling)
                {
                    IsControllingPlayer = false;
                    return;
                }

                Possess(pc, target);
                GetPlayersPositions(pc);
                SwapPlayersPositions(pc);
            }, 0.35f);
            return false;
        }
        else if (controllingTarget != null)
        {
            UnPossess(pc, controllingTarget);
            GetPlayersPositions(pc);
            SwapPlayersPositions(pc);
        }
        return false;
    }

    // Possess Player
    private static void Possess(PlayerControl pc, PlayerControl target)
    {
        (target.MyPhysics.FlipX, pc.MyPhysics.FlipX) = (pc.MyPhysics.FlipX, target.MyPhysics.FlipX); // Copy the players directions that they are facing, Note this only works for modded clients!
        pc?.RpcShapeshift(target, false);

        pc?.ResetPlayerOutfit(Main.PlayerStates[target.PlayerId].NormalOutfit);
        target?.ResetPlayerOutfit(Main.PlayerStates[pc.PlayerId].NormalOutfit);

        pc?.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DollMaster), GetString("DollMaster_PossessedTarget")));
    }

    // UnPossess Player
    private static void UnPossess(PlayerControl pc, PlayerControl target)
    {
        (target.MyPhysics.FlipX, pc.MyPhysics.FlipX) = (pc.MyPhysics.FlipX, target.MyPhysics.FlipX); // Copy the players directions that they are facing, Note this only works for modded clients!
        pc?.ResetPlayerOutfit(force: true);
        pc?.RpcShapeshift(pc, false);

        pc?.ResetPlayerOutfit(force: true);
        target?.ResetPlayerOutfit(force: true);

        IsControllingPlayer = false;
        ResetPlayerSpeed = true;
        _ = new LateTask(() =>
        {
            ReducedVisionPlayers.Clear();
            if (TargetDiesAfterPossession.GetBool() && !GameStates.IsMeeting) target?.RpcMurderPlayer(target);
        }, 0.45f);
    }

    // Swap Dollmaster and possessed player info for functions.
    public static PlayerControl SwapPlayerInfo(PlayerControl player)
    {
        if (IsControllingPlayer && HasEnabled)
        {
            if (DollMasterTarget != null && controllingTarget != null)
            {
                if (player == DollMasterTarget)
                    return controllingTarget;
                else if (player == controllingTarget)
                    return DollMasterTarget;
            }
        }

        return player;
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
        controllingTarget?.RpcTeleport(DollMasterPos);
        pc?.RpcTeleport(controllingTargetPos);
    }

    // Set name Suffix for Doll and Main Body under name.
    public override string GetSuffix(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
    {
        if (!IsControllingPlayer || GameStates.IsMeeting || isForMeeting) return string.Empty;
        if (controllingTarget == null || DollMasterTarget == null || target == null) return string.Empty;

        if (seer.PlayerId != target.PlayerId && target.PlayerId == controllingTarget.PlayerId)
            return "<color=#ffea00>" + GetString("DollMaster_MainBody") + "</color>";

        if (seer.PlayerId == target.PlayerId && target.PlayerId == DollMasterTarget.PlayerId)
            return "<color=#ffea00>" + GetString("DollMaster_Doll") + "</color>";

        return string.Empty;
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId) => hud.AbilityButton.OverrideText(GetString(IsControllingPlayer ? "DollMasterUnPossessionButtonText" : "DollMasterPossessionButtonText"));

    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Puttpuer");
}
