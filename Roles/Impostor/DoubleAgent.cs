using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using TOHE.Modules;
using TOHE.Roles.Neutral;

namespace TOHE.Roles.Impostor;
internal class DoubleAgent : RoleBase
{
    private static readonly List<byte> CurrentBombedPlayers = [];
    private static float CurrentBombedTime = float.MaxValue;
    public static bool BombIsActive = false;
    public static bool CanBombInMeeting = true;

    //===========================SETUP================================\\
    private const int Id = 28900;
    private static readonly List<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.Madmate;
    //==================================================================\\

    public static OptionItem DoubleAgentCanDiffuseBombs;
    private static OptionItem ClearBombedOnMeetingCall;
    private static OptionItem CanUseAbilityInCalledMeeting;
    private static OptionItem BombExplosionTimer;
    private static OptionItem ExplosionRadius;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.DoubleAgent);
        DoubleAgentCanDiffuseBombs = BooleanOptionItem.Create(Id + 10, "DoubleAgentCanDiffuseBombs", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DoubleAgent]);
        ClearBombedOnMeetingCall = BooleanOptionItem.Create(Id + 11, "DoubleAgentClearBombOnMeetingCall", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DoubleAgent]);
        CanUseAbilityInCalledMeeting = BooleanOptionItem.Create(Id + 12, "DoubleAgentCanUseAbilityInCalledMeeting", false, TabGroup.ImpostorRoles, false).SetParent(ClearBombedOnMeetingCall);
        BombExplosionTimer = FloatOptionItem.Create(Id + 13, "DoubleAgentBombExplosionTimer", new(10f, 60f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DoubleAgent])
            .SetValueFormat(OptionFormat.Seconds);
        ExplosionRadius = FloatOptionItem.Create(Id + 14, "DoubleAgentExplosionRadius", new(0.5f, 2f, 0.1f), 1.0f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DoubleAgent])
            .SetValueFormat(OptionFormat.Multiplier);
    }
    public override void Init()
    {
        playerIdList.Clear();
        CurrentBombedPlayers.Clear();
        CurrentBombedTime = float.MaxValue;
        BombIsActive = false;
        CanBombInMeeting = true;
    }

    private static void ClearBomb()
    {
        CurrentBombedPlayers.Clear();
        CurrentBombedTime = 999f;
        BombIsActive = false;
    }

    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
    }

    // On vent diffuse Bastion & Agitator Bomb if DoubleAgentCanDiffuseBombs is enabled.
    // Dev Note: Add role check for OnCoEnterVentOthers and make BombedVents public in Bastion.cs.
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (DoubleAgentCanDiffuseBombs.GetBool())
        {
            if (pc.PlayerId == Agitater.CurrentBombedPlayer)
            {
                Agitater.ResetBomb();
                PlaySoundForAll("Boom");
                _ = new LateTask(() =>
                {
                    if (pc.inVent) pc.MyPhysics.RpcBootFromVent(vent.Id);
                    pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DoubleAgent), GetString("DoubleAgent_DiffusedAgitaterBomb")));
                }, 0.8f, "Boot Player from vent: " + vent.Id);
                return;
            }

            if (Bastion.BombedVents.Contains(vent.Id))
            {
                Bastion.BombedVents.Remove(vent.Id);
                _ = new LateTask(() =>
                {
                    if (pc.inVent) pc.MyPhysics.RpcBootFromVent(vent.Id);
                    pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DoubleAgent), GetString("DoubleAgent_DiffusedBastionBomb")));
                }, 0.5f, "Boot Player from vent: " + vent.Id);
            }
        }
    }

    public override bool CanUseKillButton(PlayerControl pc) => false;

    // Plant bomb on first vote that isn't another imposter.
    // Dev Note: Add this to CastVotePatch in MeetingHudPatch.cs like it is for keeper
    public static bool OnVotes(PlayerControl Unused_DX, PlayerControl target)
    {
        if (!CanBombInMeeting) return true;

        if (!BombIsActive)
        {
            if (target.GetCustomRole().GetCustomRoleTeam() == Custom_Team.Impostor) return false;

            CurrentBombedTime = 999f;
            CurrentBombedPlayers.Add(target.PlayerId);
            BombIsActive = true;
            return false;
        }
        return true;
    }

    // Clear active bombed players on meeting call if ClearBombedOnMeetingCall is enabled.
    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    {
        if (BombIsActive && ClearBombedOnMeetingCall.GetBool())
        {
            ClearBomb();
            if (ClearBombedOnMeetingCall.GetBool() && !CanUseAbilityInCalledMeeting.GetBool()) CanBombInMeeting = false;
        }
        else
            CurrentBombedTime = 999f;
    }

    // If bomb is active set timer after meeting.
    public override void AfterMeetingTasks()
    {
        CurrentBombedTime = BombExplosionTimer.GetFloat() + 1f;
        CanBombInMeeting = true;
    }

    // Active bomb timer update and check.
    private static void OnFixedUpdateOthers(PlayerControl player)
    {
        if (!CurrentBombedPlayers.Contains(player.PlayerId)) return;

        if (!player.IsAlive()) // If Player is dead clear bomb.
        {
            ClearBomb();
        }

        if (BombIsActive && (GameStates.IsInTask && GameStates.IsInGame) && !(GameStates.IsMeeting && GameStates.IsExilling))
        {
            CurrentBombedTime -= Time.deltaTime;

            if (CurrentBombedTime < 1)
            {
                BoomBoom(player);
            }
        }
    }

    // Player go bye bye ¯\_(ツ)_/¯
    private static void BoomBoom(PlayerControl player)
    {
        if (player.inVent) player.MyPhysics.RpcBootFromVent(GetPlayerVentId(player));

        foreach (PlayerControl target in Main.AllAlivePlayerControls) // Get players in radius of bomb that are not in a vent.
        {
            if (CheckForPlayersInRadius(player, target) <= ExplosionRadius.GetFloat())
            {
                if (player.inVent) return;
                Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                target.RpcMurderPlayer(target);
            }
        }

        PlaySoundForAll("Boom");
        ClearBomb();

        foreach (byte playerId in playerIdList) // Get Double Agent.
        {
            var DAplayer = Utils.GetPlayerById(playerId);
            DAplayer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DoubleAgent), GetString("DoubleAgent_BombExploded")));
        }
    }

    private static float CheckForPlayersInRadius(PlayerControl player, PlayerControl target) => Vector2.Distance(player.GetCustomPosition(), target.GetCustomPosition());

    // Play specific sound for all players.
    private static void PlaySoundForAll(string Sound)
    {
        foreach (PlayerControl player in Main.AllPlayerControls)
        {
            player.RPCPlayCustomSound(Sound);
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

    // Set bomb mark on player.
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (seen == null ) return string.Empty;
        if (CurrentBombedPlayers.Contains(seen.PlayerId)) return Utils.ColorString(Color.red, " Ⓑ"); // L Rizz :)
        return string.Empty;
    }


    // Set timer on Double Agent.
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (seen == null) return string.Empty;
        if (!seer.Is(CustomRoles.DoubleAgent)) return string.Empty;
        if (seer == seen && !(GameStates.IsInTask && GameStates.IsInGame) || (int)CurrentBombedTime >= (int)BombExplosionTimer.GetFloat() + 1) return string.Empty;
        if (seer == seen && BombIsActive) return Utils.ColorString(Color.red, GetString("DoubleAgent_BombExplodesIn") + (int)CurrentBombedTime + "s");
        return string.Empty;
    }
}

// FieryFlower was here ඞ