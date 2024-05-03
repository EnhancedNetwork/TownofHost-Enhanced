using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class Amateur : RoleBase
{
    private static float RealTimeKillCooldown = 0f;
    private static PlayerControl SelfTarget = null;
    private static bool IsRevealed = false;
    private static bool IsInMeeting = false;
    private static bool SetUpForAirship = false;
    //===========================SETUP================================\\
    private const int Id = 28700;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    public static OptionItem IncreaseKillCooldownOnVent;
    public static OptionItem IncreaseKillCooldownAmount;
    public static OptionItem SetKillCooldownOffDistance;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Amateur);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amateur])
            .SetValueFormat(OptionFormat.Seconds);
        SetKillCooldownOffDistance = BooleanOptionItem.Create(Id + 11, "AmateurSetKillCooldownOffDistance", true, TabGroup.ImpostorRoles, false).SetParent(KillCooldown);
        IncreaseKillCooldownOnVent = BooleanOptionItem.Create(Id + 12, "AmateurIncreaseKillCooldownOnVent", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Amateur]);
        IncreaseKillCooldownAmount = FloatOptionItem.Create(Id + 13, "AmateurIncreaseKillCooldownAmount", new(0f, 180f, 2.5f), 5f, TabGroup.ImpostorRoles, false).SetParent(IncreaseKillCooldownOnVent)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        playerIdList.Clear();
        IsRevealed = false;
        IsInMeeting = false;
        SetUpForAirship = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        RealTimeKillCooldown = KillCooldown.GetFloat();
        SelfTarget = Utils.GetPlayerById(playerId);
        RealTimeKillCooldown = 99f;

        // Sync killcooldowns for all maps but Airshit.
        _ = new LateTask(() =>
        {
            if (!GameStates.InGame || GameStates.AirshipIsActive) return;
            RealTimeKillCooldown = KillCooldown.GetFloat();
            SelfTarget.SetKillCooldown(RealTimeKillCooldown);
        }, 10f, "Sync killcooldowns");
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    // Set cooldowns on kill.
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (SelfTarget == null) return true;
        if (killer == SelfTarget)
        {
            if (RealTimeKillCooldown >= 0)
            {
                SelfTarget.SetKillCooldown(RealTimeKillCooldown);
                return false;
            }
            _ = new LateTask(() =>
            {
                if (SetKillCooldownOffDistance.GetBool())
                    RealTimeKillCooldown = GetClosestPlayerDistance();
                else
                    RealTimeKillCooldown = KillCooldown.GetFloat();

                SelfTarget.SetKillCooldown(RealTimeKillCooldown);
            }, 0.2f, "Reset killcooldowns");
        }
        return true;
    }

    // Calculate closest alive player distance to killcooldown.
    private static float GetClosestPlayerDistance()
    {
        if (SelfTarget == null) return KillCooldown.GetFloat();

        var pcPos = SelfTarget.GetCustomPosition();
        var allPos = new List<Vector2>();

        // Collect all player positions
        foreach (var playerControl in Main.AllAlivePlayerControls)
        {
            if (playerControl != null && playerControl != SelfTarget)
            {
                var playerPos = playerControl.GetCustomPosition();
                allPos.Add(playerPos);
            }
        }

        // Find the closest player position
        float closestDistance = float.MaxValue;

        foreach (var pos in allPos)
        {
            float distance = Vector2.Distance(pcPos, pos);
            if (distance < closestDistance)
            {
                closestDistance = distance;
            }
        }

        if (closestDistance >= KillCooldown.GetFloat()) closestDistance = KillCooldown.GetFloat();

        return closestDistance;
    }

    // Setup Increase killcooldown on vent.
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (!IncreaseKillCooldownOnVent.GetBool()) return;
        RealTimeKillCooldown += IncreaseKillCooldownAmount.GetFloat();
    }

    // Set Increase killcooldown on exit vent.
    public override void OnExitVent(PlayerControl pc, int ventId)
    {
        pc.SetKillCooldown(RealTimeKillCooldown);
    }

    // Set stuff up for meeting.
    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    {
        if (SelfTarget == null) return;
        IsInMeeting = true;
        RealTimeKillCooldown = 99f;
        if (!IsRevealed) return;
        {
            SelfTarget.MyPhysics.SetBodyType(PlayerBodyTypes.Normal);
            Camouflage.RpcSetSkin(SelfTarget, RevertToDefault: true, ForceRevert: true);
            IsRevealed = false;
        }
    }

    // After meeting sync killcooldowns for all maps but Airshit.
    public override void AfterMeetingTasks()
    {
        if (SelfTarget == null) return;
        if (!GameStates.AirshipIsActive)
        {
            RealTimeKillCooldown = KillCooldown.GetFloat();
            SelfTarget.SetKillCooldown(RealTimeKillCooldown);
        }
        else SetUpForAirship = false;
        IsInMeeting = false;
    }

    // Check if player should be revealed or not.
    public override void OnFixedUpdate(PlayerControl pc)
    {
        if (IsInMeeting) return;

        // Sync killcooldowns for Airshit.
        if (GameStates.AirshipIsActive && !SetUpForAirship)
        {
            // Check if the player is not within the specified range.
            if (Vector2.Distance(pc.GetTruePosition(), new(-25, 40)) > 2.0f && !IsInMeeting)
            {
                _ = new LateTask(() =>
                {
                    RealTimeKillCooldown = KillCooldown.GetFloat();
                    SelfTarget.SetKillCooldown(RealTimeKillCooldown);
                }, 0.5f, "Sync killcooldowns");
                SetUpForAirship = true;
            }
        }

        if (RealTimeKillCooldown >= 0 && !pc.inVent) RealTimeKillCooldown -= Time.deltaTime;

        if (RealTimeKillCooldown <= 0) // Set as seeker.
        {
            if (IsRevealed) return;

            if (pc.inMovingPlat && GameStates.FungleIsActive // Make sure that the player is not in certain states to reveal.
            || pc.MyPhysics.Animations.IsPlayingEnterVentAnimation()
            || pc.onLadder
            || pc.MyPhysics.Animations.IsPlayingAnyLadderAnimation()
            || Pelican.IsEaten(pc.PlayerId)) return;
            
            pc.MyPhysics.SetBodyType(PlayerBodyTypes.Seeker);
            pc.RpcSetVisor("visor_Mouth");
            pc.RpcSetSkin("");
            pc.KillFlash();
            RPC.PlaySoundRPC(pc.PlayerId, Sounds.ImpTransform);
            IsRevealed = true;
        }
        else // Set as Normal.
        {
            if (!IsRevealed) return;
            if (pc.inVent || pc.walkingToVent) return;
            _ = new LateTask(() =>
            {
                pc.MyPhysics.SetBodyType(PlayerBodyTypes.Normal);
                Camouflage.RpcSetSkin(pc, RevertToDefault: true, ForceRevert: true);
            }, 0.3f, "Set player to normal");
            IsRevealed = false;
        }
    }

    // When revealed make player name red for all other players.
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target)
    {
        if (IsRevealed) return true;
        return false;
    }
}
