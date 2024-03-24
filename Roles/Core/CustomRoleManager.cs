using AmongUs.GameOptions;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;

namespace TOHE.Roles.Core;

public static class CustomRoleManager
{
    //public static Dictionary<byte, RoleBase> AllActiveRoles = new(15);
    public static bool HasEnabled(this CustomRoles role) => Utils.IsRoleClass(role).IsEnable;
    public static RoleBase GetRoleClass(this PlayerControl player) => GetRoleClassById(player.PlayerId);
    public static RoleBase GetRoleClassById(this byte playerId) => Main.PlayerStates.TryGetValue(playerId, out var statePlayer) && statePlayer != null ? statePlayer.RoleClass : new VanillaRole();

    private static string GetNameSpace(CustomRoles role)
    {
        if (role.IsGhostRole())
        {
            if (role.IsImpostor())
                return "TOHE.Roles._Ghosts_.Impostor.";
            if (role.IsCrewmate())
                return "TOHE.Roles._Ghosts_.Crewmate.";
        }
        if (role is CustomRoles.Mini)
            return "TOHE.Roles.Double.";
        if (role.IsImpostor())
            return "TOHE.Roles.Impostor.";
        if (role.IsCrewmate())
            return "TOHE.Roles.Crewmate.";

        else return "TOHE.Roles.Neutral.";
    }
    public static RoleBase CreateRoleClass(this CustomRoles role, bool IsToAccess = false) // CHATGPT COOKED 🔥🔥🗿☕
    { 
        role = role switch // Switch role to FatherRole (Double Classes)
        {
            CustomRoles.Vampiress => CustomRoles.Vampire,
            CustomRoles.Sunnyboy => CustomRoles.Jester,
            CustomRoles.Pestilence => CustomRoles.PlagueBearer,
            CustomRoles.Nuker => CustomRoles.Bomber,
            CustomRoles.NiceMini or CustomRoles.EvilMini => CustomRoles.Mini,
            _ => role
        };
        if (!IsToAccess) Logger.Info($"Attempting to Create new {role}()", "CreateRoleClass");

        string RoleNameSpace = GetNameSpace(role);
        string className = $"{RoleNameSpace}" + role.ToString(); 
        Type classType = Type.GetType(className);

        if (classType == null || !typeof(RoleBase).IsAssignableFrom(classType))
        {
            if (!IsToAccess) Logger.Info("An unknown RoleType or RoleClass was given, assigning new VanillaRole()", "CreateRoleClass");
            return new VanillaRole();
        }

        if (!IsToAccess) Logger.Info($"Succesfully Created new {role}()", "CreateRoleClass");
        return (RoleBase)Activator.CreateInstance(classType);
    }

    /// <summary>
    /// If the role protect others players
    /// </summary>
    public static bool OnCheckMurderAsTargetOnOthers(PlayerControl killer, PlayerControl target)
    {
        bool cancel = false;
        foreach (var player in Main.PlayerStates.Values.ToArray())
        {
            var playerRoleClass = player.RoleClass;
            if (player == null || playerRoleClass == null) continue;

            if (!playerRoleClass.CheckMurderOnOthersTarget(killer, target))
            {
                cancel = true;
            }
        }
        return !cancel;
    }
    /// <summary>
    /// Check Murder as Killer in target
    /// </summary>
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        var killerRoleClass = killer.GetRoleClass();

        if (killer == target) return true;

        // Forced check
        if (!killerRoleClass.ForcedCheckMurderAsKiller(killer, target))
        {
            return false;
        }

        // Check in target
        if (!killer.RpcCheckAndMurder(target, true))
        {
            return false;
        }

        foreach (var killerSubRole in killer.GetCustomSubRoles().ToArray())
        {
            switch (killerSubRole)
            {
                case CustomRoles.Madmate when target.Is(CustomRoleTypes.Impostor) && !Madmate.MadmateCanKillImp.GetBool():
                case CustomRoles.Infected when target.Is(CustomRoles.Infected) && !Infectious.TargetKnowOtherTargets:
                case CustomRoles.Infected when target.Is(CustomRoles.Infectious):
                    return false;

                case CustomRoles.Mare:
                    if (Mare.IsLightsOut)
                        return false;
                    break;

                case CustomRoles.Unlucky:
                    Unlucky.SuicideRand(killer);
                    if (Unlucky.UnluckCheck[killer.PlayerId]) return false;
                    break;

                case CustomRoles.Tired:
                    Tired.AfterActionTasks(killer);
                    break;

                case CustomRoles.Clumsy:
                    if (!Clumsy.OnCheckMurder(killer))
                        return false;
                    break;

                case CustomRoles.Swift:
                    if (!Swift.OnCheckMurder(killer, target))
                        return false;
                    break;
            }
        }

        // Check murder as killer
        if (!killerRoleClass.OnCheckMurderAsKiller(killer, target))
        {
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

                    case CustomRoles.Bait when !inMeeting && !isSuicide:
                        Bait.BaitAfterDeathTasks(killer, target);
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

                    case CustomRoles.Tricky:
                        Tricky.AfterPlayerDeathTasks(target);
                        break;

                    case CustomRoles.EvilSpirit when !inMeeting && !isSuicide:
                        target.RpcSetRole(RoleTypes.GuardianAngel);
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
                    case CustomRoles.TicketsStealer when !inMeeting && !isSuicide:
                        killer.Notify(string.Format(Translator.GetString("TicketsStealerGetTicket"), ((Main.AllPlayerControls.Count(x => x.GetRealKiller()?.PlayerId == killer.PlayerId) + 1) * Stealer.TicketsPerKill.GetFloat()).ToString("0.0#####")));
                        break;
                }
            }

        // Check dead body for others roles
        CheckDeadBody(target, killer, inMeeting);

        // Check Lovers Suicide
        FixedUpdateInNormalGamePatch.LoversSuicide(target.PlayerId, inMeeting);
    }
    
    /// <summary>
    /// Check if this task is marked by a role and do something.
    /// </summary>
    public static void OthersCompleteThisTask(PlayerControl player, PlayerTask task)
        => Main.PlayerStates.Values.ToArray().Do(PlrState => PlrState.RoleClass.OnOthersTaskComplete(player, task));
    

    public static HashSet<Action<PlayerControl, PlayerControl, bool>> CheckDeadBodyOthers = [];
    /// <summary>
    /// If the role need check a present dead body
    /// </summary>
    public static void CheckDeadBody(PlayerControl deadBody, PlayerControl killer, bool inMeeting)
    {
        if (!CheckDeadBodyOthers.Any()) return;
        //Execute other viewpoint processing if any
        foreach (var checkDeadBodyOthers in CheckDeadBodyOthers.ToArray())
        {
            checkDeadBodyOthers(deadBody, killer, inMeeting);
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

        if (OnFixedUpdateOthers.Count <= 0) return;
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
        foreach (var player in Main.PlayerStates.Values.ToArray())
        {
            var playerRoleClass = player.RoleClass;
            if (player == null || playerRoleClass == null) continue;

            if (playerRoleClass.OnCoEnterVentOthers(physics, ventId))
            {
                return true;
            }
        }
        return false;
    }

    public static HashSet<Func<PlayerControl, PlayerControl, bool, string>> MarkOthers = [];
    public static HashSet<Func<PlayerControl, PlayerControl, bool, bool, string>> LowerOthers = [];
    public static HashSet<Func<PlayerControl, PlayerControl, bool, string>> SuffixOthers = [];

    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (!MarkOthers.Any()) return string.Empty;

        var sb = new StringBuilder(100);
        foreach (var marker in MarkOthers)
        {
            sb.Append(marker(seer, seen, isForMeeting));
        }
        return sb.ToString();
    }

    public static string GetLowerTextOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (!LowerOthers.Any()) return string.Empty;

        var sb = new StringBuilder(100);
        foreach (var lower in LowerOthers)
        {
            sb.Append(lower(seer, seen, isForMeeting, isForHud));
        }
        return sb.ToString();
    }

    public static string GetSuffixOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (!SuffixOthers.Any()) return string.Empty;

        var sb = new StringBuilder(100);
        foreach (var suffix in SuffixOthers)
        {
            sb.Append(suffix(seer, seen, isForMeeting));
        }
        return sb.ToString();
    }

    public static void Initialize()
    {
        MarkOthers.Clear();
        LowerOthers.Clear();
        SuffixOthers.Clear();
        OnFixedUpdateOthers.Clear();
        OnFixedUpdateLowLoadOthers.Clear();
        CheckDeadBodyOthers.Clear();
    }
}
