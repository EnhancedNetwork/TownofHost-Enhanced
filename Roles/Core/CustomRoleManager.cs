using AmongUs.GameOptions;
using System;
using System.Text;
using TOHE.Roles.AddOns;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using TOHE.Roles.Vanilla;

namespace TOHE.Roles.Core;

public static class CustomRoleManager
{
    public static readonly Dictionary<CustomRoles, RoleBase> RoleClass = [];
    public static readonly Dictionary<CustomRoles, IAddon> AddonClasses = [];
    public static RoleBase GetStaticRoleClass(this CustomRoles role) => RoleClass.TryGetValue(role, out var roleClass) & roleClass != null ? roleClass : new DefaultSetup();
    public static List<RoleBase> AllEnabledRoles => Main.PlayerStates.Values.Select(x => x.RoleClass).ToList(); //Since there are classes which use object attributes and playerstate is not removed.
    public static bool HasEnabled(this CustomRoles role) => role.GetStaticRoleClass().IsEnable;

    public static bool OtherCollectionsSet = false;
    public static List<RoleBase> GetNormalOptions(Custom_RoleType type)
    {
        List<RoleBase> roles = [];
        foreach (var role in RoleClass.Values)
        {
            if (IsOptBlackListed(role.GetType()) || role.IsExperimental) continue;

            if (role.ThisRoleType == type)
            {
                roles.Add(role);
            }
        }
        return roles;
    }
    public static List<RoleBase> GetExperimentalOptions(Custom_Team team)
    {
        List<RoleBase> roles = [];
        switch (team)
        {
            case Custom_Team.Crewmate:
                roles = RoleClass.Where(r => r.Value.IsExperimental && r.Key.IsCrewmate()).Select(r => r.Value).ToList();
                break;

            case Custom_Team.Impostor:
                roles = RoleClass.Where(r => r.Value.IsExperimental && r.Key.IsImpostorTeam()).Select(r => r.Value).ToList();
                break;

            case Custom_Team.Neutral:
                roles = RoleClass.Where(r => r.Value.IsExperimental && r.Key.IsNeutralTeamV2()).Select(r => r.Value).ToList();
                break;

            default:
                Logger.Info("Unsupported team was sent.", "GetExperimentalOptions");
                break;
        }
        return roles;
    }
    public static bool IsOptBlackListed(this Type role) => CustomRolesHelper.DuplicatedRoles.ContainsValue(role);
    public static RoleBase GetRoleClass(this PlayerControl player) => GetRoleClassById(player.PlayerId);
    public static RoleBase GetRoleClassById(this byte playerId) => Main.PlayerStates.TryGetValue(playerId, out var statePlayer) && statePlayer != null ? statePlayer.RoleClass : new DefaultSetup();

    public static RoleBase CreateRoleClass(this CustomRoles role) 
    {
        return (RoleBase)Activator.CreateInstance(role.GetStaticRoleClass().GetType()); // Converts this.RoleBase back to its type and creates an unique one.
    }

    public static bool HasDesyncRole(this PlayerControl player) => player != null && (player.GetRoleClass().IsDesyncRole || Main.DesyncPlayerList.Contains(player.Data.PlayerId));

    /// <summary>
    /// If the role protect others players
    /// </summary>
    public static bool OnCheckMurderAsTargetOnOthers(PlayerControl killer, PlayerControl target)
    {
        // return true when need to cancel the kill target
        // "Any()" defines a function that returns true, and converts to false to cancel the kill
        return !AllEnabledRoles.Any(RoleClass => RoleClass.CheckMurderOnOthersTarget(killer, target) == true);
    }

    /// <summary>
    /// Builds Modified GameOptions
    /// </summary>
    public static void BuildCustomGameOptions(this PlayerControl player, ref IGameOptions opt)
    {
        if (player.IsAnySubRole(x => x is CustomRoles.EvilSpirit))
        {
            AURoleOptions.GuardianAngelCooldown = Spiritcaller.SpiritAbilityCooldown.GetFloat();
        }

        player.GetRoleClass()?.ApplyGameOptions(opt, player.PlayerId);

        if (NoisemakerTOHE.HasEnabled) NoisemakerTOHE.ApplyGameOptionsForOthers(player);

        if (DollMaster.HasEnabled && DollMaster.IsDoll(player.PlayerId))
        {
            DollMaster.ApplySettingsToDoll(opt, player);
            return;
        }

        if (Grenadier.HasEnabled) Grenadier.ApplyGameOptionsForOthers(opt, player);
        if (Dazzler.HasEnabled) Dazzler.SetDazzled(player, opt);
        if (Deathpact.HasEnabled) Deathpact.SetDeathpactVision(player, opt);
        if (Spiritcaller.HasEnabled) Spiritcaller.ReduceVision(opt, player);
        if (Pitfall.HasEnabled) Pitfall.SetPitfallTrapVision(opt, player);

        var playerSubRoles = player.GetCustomSubRoles();

        if (playerSubRoles.Any())
            foreach (var subRole in playerSubRoles.ToArray())
            {
                switch (subRole)
                {
                    case CustomRoles.Watcher:
                        Watcher.RevealVotes(opt);
                        break;
                    case CustomRoles.Flash:
                        Flash.SetSpeed(player.PlayerId, false);
                        break;
                    case CustomRoles.Torch:
                        Torch.ApplyGameOptions(opt);
                        break;
                    case CustomRoles.Tired:
                        Tired.ApplyGameOptions(opt, player);
                        break;
                    case CustomRoles.Bewilder:
                        Bewilder.ApplyVisionOptions(opt);
                        break;
                    case CustomRoles.Reach:
                        Reach.ApplyGameOptions(opt);
                        break;
                    case CustomRoles.Madmate:
                        Madmate.ApplyGameOptions(opt);
                        break;
                    case CustomRoles.Mare:
                        Mare.ApplyGameOptions(player.PlayerId);
                        break;
                }
            }

        // Add-ons
        if (Glow.IsEnable) Glow.ApplyGameOptions(opt, player); //keep this at last
        if (Bewilder.IsEnable) Bewilder.ApplyGameOptions(opt, player);
        if (Ghoul.IsEnable) Ghoul.ApplyGameOptions(player);
    }

    /// <summary>
    /// Check Murder as Killer in target
    /// </summary>
    public static bool OnCheckMurder(ref PlayerControl killer, ref PlayerControl target, ref bool __state)
    {
        if (killer == target) return true;

        if (target != null && target.Is(CustomRoles.Fragile))
        {
            if (Fragile.KillFragile(killer, target))
            {
                Logger.Info("Fragile killed in OnCheckMurder, returning false", "Fragile");
                return false;
            }
        }
        var canceled = false;
        var cancelbutkill = false;

        var killerRoleClass = killer.GetRoleClass();
        var killerSubRoles = killer.GetCustomSubRoles();

        // If Target is possessed by Dollmaster swap controllers.
        target = DollMaster.SwapPlayerInfo(target);   

        Logger.Info("Start", "PlagueBearer.CheckAndInfect");

        if (PlagueBearer.HasEnabled && !killer.Is(CustomRoles.PlagueBearer))
        {
            PlagueBearer.CheckAndInfect(killer, target);
        }

        Logger.Info("Start", "ForcedCheckMurderAsKiller");

        // Forced check
        if (killerRoleClass.ForcedCheckMurderAsKiller(killer, target) == false)
        {
            __state = true;
            Logger.Info("Cancels because for killer no need kill target", "ForcedCheckMurderAsKiller");
            return false;
        }

        Logger.Info("Start", "OnCheckMurder.RpcCheckAndMurder");

        // Check in target
        if (killer.RpcCheckAndMurder(target, true) == false)
        {
            __state = true;
            Logger.Info("Cancels because target cancel kill", "OnCheckMurder.RpcCheckAndMurder");
            return false;
        }

        Logger.Info("Start foreach", "KillerSubRoles");

        if (killerSubRoles.Any())
            foreach (var killerSubRole in killerSubRoles.ToArray())
            {
                switch (killerSubRole)
                {
                    case CustomRoles.Madmate when target.Is(Custom_Team.Impostor) && !Madmate.MadmateCanKillImp.GetBool():
                    case CustomRoles.Infected when target.Is(CustomRoles.Infected) && !Infectious.TargetKnowOtherTargets:
                    case CustomRoles.Infected when target.Is(CustomRoles.Infectious):
                        canceled = true;
                        break;

                    case CustomRoles.Unlucky:
                        if (Unlucky.SuicideRand(killer, Unlucky.StateSuicide.TryKill))
                            canceled = true;
                        break;

                    case CustomRoles.Tired:
                        Tired.AfterActionTasks(killer);
                        break;

                    case CustomRoles.Mare:
                        if (Mare.IsLightsOut)
                            canceled = true;
                        break;

                    case CustomRoles.Clumsy:
                        if (!Clumsy.OnCheckMurder(killer))
                            canceled = true;
                        break;

                    case CustomRoles.Swift:
                        if (!Swift.OnCheckMurder(killer, target))
                            cancelbutkill = true;
                        break;
                }
            }

        Logger.Info("Start", "OnCheckMurderAsKiller");

        // Check murder as killer
        if (killerRoleClass.OnCheckMurderAsKiller(killer, target) == false)
        {
            __state = true;
            if (cancelbutkill && target.IsAlive() 
                && !DoubleTrigger.FirstTriggerTimer.TryGetValue(killer.PlayerId, out _)) // some roles have an internal rpcmurderplayer, but still had to cancel
            {
                target.RpcMurderPlayer(target);
                target.SetRealKiller(killer);
                Oiiai.OnMurderPlayer(killer, target);
            }

            Logger.Info("Cancels because for killer no need kill target", "OnCheckMurderAsKiller");
            return false;
        }

        // Swap controllers if Sheriff shots Dollmasters main body.
        if (DollMaster.HasEnabled && killer.Is(CustomRoles.Sheriff) && target == DollMaster.DollMasterTarget)
        {
            target = DollMaster.SwapPlayerInfo(target);
        }

        // Check if killer is a true killing role and Target is possessed by Dollmaster
        if (DollMaster.HasEnabled && DollMaster.IsControllingPlayer)
            if (!(DollMaster.DollMasterTarget == null || DollMaster.controllingTarget == null))
                if (target == DollMaster.DollMasterTarget || target == DollMaster.controllingTarget)
                {
                    DollMaster.CheckMurderAsPossessed(killer, target);
                    return false;
                }

        if (canceled)
            return false;

        if (cancelbutkill)
        {
            target.RpcMurderPlayer(target);
            target.SetRealKiller(killer);
            Oiiai.OnMurderPlayer(killer, target);
            return false;
        }
                
        return true;
    }
    /// <summary>
    /// Tasks after killer murder target
    /// </summary>
    public static void OnMurderPlayer(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        // ############-INFO-##############
        // When using this code, keep in mind that killer and target can be equal (Suicide)
        // And the player can also die during the Meeting
        // ################################

        PlayerControl trueDMKiller = killer; // Save real killer.
        killer = DollMaster.SwapPlayerInfo(killer); // If "killer" is possessed by the Dollmaster swap each other's controllers.

        var killerRoleClass = killer.GetRoleClass();
        var targetRoleClass = target.GetRoleClass();

        var killerSubRoles = killer.GetCustomSubRoles();
        var targetSubRoles = target.GetCustomSubRoles();

        // Check suicide
        var isSuicide = killer.PlayerId == target.PlayerId;

        // target was murder by killer
        targetRoleClass.OnMurderPlayerAsTarget(killer, target, inMeeting, isSuicide);

        // Check target add-ons
        if (targetSubRoles.Any())
            foreach (var subRole in targetSubRoles.ToArray())
            {
                switch (subRole)
                {
                    case CustomRoles.Cyber:
                        Cyber.AfterCyberDeadTask(target, inMeeting);
                        break;

                    case CustomRoles.Bait when !inMeeting:
                        Bait.BaitAfterDeathTasks(trueDMKiller, target); // Use trueDMKiller to any roles that needs the Dollmaster to be the killer!
                        break;

                    case CustomRoles.Trapper when !inMeeting && !isSuicide && !killer.Is(CustomRoles.KillingMachine):
                        killer.TrapperKilled(target);
                        break;

                    case CustomRoles.Avanger when !inMeeting && !isSuicide:
                        Avanger.OnMurderPlayer(target);
                        break;

                    case CustomRoles.Burst when killer.IsAlive() && !inMeeting && !isSuicide && !killer.Is(CustomRoles.KillingMachine):
                        Burst.AfterBurstDeadTasks(killer, target);
                        break;

                    case CustomRoles.Oiiai when !isSuicide:
                        Oiiai.OnMurderPlayer(killer, target);
                        break;

                    case CustomRoles.EvilSpirit when !inMeeting && !isSuicide:
                        target.RpcSetRole(RoleTypes.GuardianAngel);
                        break;

                    case CustomRoles.Spurt:
                        Spurt.DeathTask(target);
                        break;

                }
            }

        // Killer murder target
        killerRoleClass.OnMurderPlayerAsKiller(killer, target, inMeeting, isSuicide);

        // Check killer add-ons
        if (killerSubRoles.Any())
            foreach (var subRole in killerSubRoles.ToArray())
            {
                switch (subRole)
                {
                    case CustomRoles.Stealer when !inMeeting && !isSuicide:
                        Stealer.OnMurderPlayer(killer);
                        break;

                    case CustomRoles.Tricky:
                        Tricky.AfterPlayerDeathTasks(target);
                        break;
                }
            }

        // Check dead body for others roles
        CheckDeadBody(killer, target, inMeeting);

        if (!(killer.PlayerId == target.PlayerId && target.IsDisconnected()))
        {
            // Check Lovers Suicide
            FixedUpdateInNormalGamePatch.LoversSuicide(target.PlayerId, inMeeting);
        }
    }
    
    /// <summary>
    /// Check if this task is marked by a role and do something.
    /// </summary>
    public static void OthersCompleteThisTask(PlayerControl player, PlayerTask task)
        => AllEnabledRoles.Do(RoleClass => RoleClass.OnOthersTaskComplete(player, task)); //
    

    public static HashSet<Action<PlayerControl, PlayerControl, bool>> CheckDeadBodyOthers = [];
    /// <summary>
    /// If the role need check a present dead body
    /// </summary>
    public static void CheckDeadBody(PlayerControl killer, PlayerControl deadBody, bool inMeeting)
    {
        if (!CheckDeadBodyOthers.Any()) return;
        //Execute other viewpoint processing if any
        foreach (var checkDeadBodyOthers in CheckDeadBodyOthers.ToArray())
        {
            checkDeadBodyOthers(killer, deadBody, inMeeting);
        }
    }

    public static HashSet<Action<PlayerControl>> OnFixedUpdateOthers = [];
    /// <summary>
    /// Function always called in a task turn
    /// For interfering with other roles
    /// Registered with OnFixedUpdateOthers+= at initialization
    /// </summary>
    public static void OnFixedUpdate(PlayerControl player)
    {
        player.GetRoleClass()?.OnFixedUpdate(player);

        if (!OnFixedUpdateOthers.Any()) return;
        //Execute other viewpoint processing if any
        foreach (var onFixedUpdate in OnFixedUpdateOthers.ToArray())
        {
            onFixedUpdate(player);
        }
    }
    public static HashSet<Action<PlayerControl>> OnFixedUpdateLowLoadOthers = [];
    public static void OnFixedUpdateLowLoad(PlayerControl player)
    {
        player.GetRoleClass()?.OnFixedUpdateLowLoad(player);

        if (!OnFixedUpdateLowLoadOthers.Any()) return;
        //Execute other viewpoint processing if any
        foreach (var onFixedUpdateLowLoad in OnFixedUpdateLowLoadOthers.ToArray())
        {
            onFixedUpdateLowLoad(player);
        }
    }

    /// <summary>
    /// When others players on entered to vent
    /// </summary>
    public static bool OthersCoEnterVent(PlayerPhysics physics, int ventId)
    {
        return AllEnabledRoles.Any(RoleClass => RoleClass.OnCoEnterVentOthers(physics, ventId));
    }

    private static HashSet<Func<PlayerControl, PlayerControl, bool, string>> MarkOthers = [];
    /// <summary>
    /// If seer == seen then GetMarkOthers called from FixedUpadte or MeetingHud or NotifyRoles
    /// </summary>
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        var sb = new StringBuilder(100);
        foreach (var marker in MarkOthers)
        {
            sb.Append(marker(seer, seen, isForMeeting));
        }
        return sb.ToString();
    }

    private static HashSet<Func<PlayerControl, PlayerControl, bool, bool, string>> LowerOthers = [];
    /// <summary>
    /// If seer == seen then GetMarkOthers called from FixedUpadte or NotifyRoles
    /// </summary>
    public static string GetLowerTextOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false, bool isForHud = false)
    {
        var sb = new StringBuilder(100);
        foreach (var lower in LowerOthers)
        {
            sb.Append(lower(seer, seen, isForMeeting, isForHud));
        }

        return sb.ToString();
    }

    private static HashSet<Func<PlayerControl, PlayerControl, bool, string>> SuffixOthers = [];
    /// <summary>
    /// If seer == seen then GetMarkOthers called from FixedUpadte or NotifyRoles
    /// </summary>
    public static string GetSuffixOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {

        var sb = new StringBuilder(100);
        foreach (var suffix in SuffixOthers)
        {
            sb.Append(suffix(seer, seen, isForMeeting));
        }
        return sb.ToString();
    }

    public static readonly Dictionary<byte, HashSet<int>> BlockedVentsList = [];

    public static void Initialize()
    {
        OtherCollectionsSet = false;
        OnFixedUpdateOthers.Clear();
        OnFixedUpdateLowLoadOthers.Clear();
        CheckDeadBodyOthers.Clear();
        BlockedVentsList.Clear();
    }

    public static void Add()
    {
        MarkOthers = AllEnabledRoles.Select(mark => (Func<PlayerControl, PlayerControl, bool, string>)mark.GetMarkOthers).FilterDuplicates();
        LowerOthers = AllEnabledRoles.Select(lower => (Func<PlayerControl, PlayerControl, bool, bool, string>)lower.GetLowerTextOthers).FilterDuplicates();
        SuffixOthers = AllEnabledRoles.Select(suffix => (Func<PlayerControl, PlayerControl, bool, string>)suffix.GetSuffixOthers).FilterDuplicates();
        OtherCollectionsSet = true;
    }

    // ADDONS ////////////////////////////

    public static void OnFixedAddonUpdate(this PlayerControl pc, bool lowload) => pc.GetCustomSubRoles().Do(x => {
        if (AddonClasses.TryGetValue(x, out var IAddon) && IAddon != null)
            IAddon.OnFixedUpdate(pc);
        else return;

        if (!lowload)
            IAddon.OnFixedUpdateLowLoad(pc);
    });
}
