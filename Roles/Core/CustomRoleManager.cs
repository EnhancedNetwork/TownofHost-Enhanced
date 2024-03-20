using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;

namespace TOHE.Roles.Core;

public static class CustomRoleManager
{
    //public static Dictionary<byte, RoleBase> AllActiveRoles = new(15);
    public static bool IsClassEnable(this CustomRoles role) => Main.PlayerStates.Any(x => x.Value.MainRole == role && x.Value.RoleClass.IsEnable);

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
        if (role.IsImpostor())
            return "TOHE.Roles.Impostor.";
        if (role.IsCrewmate())
            return "TOHE.Roles.Crewmate.";
        
        else return "TOHE.Roles.Neutral.";
    }
    public static RoleBase CreateRoleClass(this CustomRoles role) // CHATGPT COOKED 🔥🔥🗿☕
    { 
        role = role switch // Switch role to FatherRole (Double Classes)
        {
            CustomRoles.Vampiress => CustomRoles.Vampire,
            CustomRoles.Sunnyboy => CustomRoles.Jester,
            CustomRoles.Pestilence => CustomRoles.PlagueBearer,
            CustomRoles.Nuker => CustomRoles.Bomber,
            _ => role
        };
        Logger.Info($"Attempting to Create new {role}()", "CreateRoleClass");

        string RoleNameSpace = GetNameSpace(role);
        string className = $"{RoleNameSpace}" + role.ToString(); 
        Type classType = Type.GetType(className);

        if (classType == null || !typeof(RoleBase).IsAssignableFrom(classType))
        {
            Logger.Info("An unknown RoleType or RoleClass was given, assigning new VanillaRole()", "CreateRoleClass");
            return new VanillaRole();
        }

        Logger.Info($"Succesfully Created new {role}()", "CreateRoleClass");
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
    /// If the role does tasks after target death.
    /// </summary>
    public static void OthersAfterPlayerDead(PlayerControl player)
    {
        Main.PlayerStates.Values.ToArray().Do(PlrState => PlrState.RoleClass.OthersAfterPlayerDeathTask(player));
    }
    /// <summary>
    /// Check if this task is marked by a role and do something.
    /// </summary>
    public static void OthersCompleteThisTask(PlayerControl player, PlayerTask task)
    {
        Main.PlayerStates.Values.ToArray().Do(PlrState => PlrState.RoleClass.OnOthersTaskComplete(player, task));
    }

    public static HashSet<Action<PlayerControl, PlayerControl>> CheckDeadBodyOthers = [];
    /// <summary>
    /// If the role need check a present dead body
    /// </summary>
    public static void CheckDeadBody(PlayerControl deadBody, PlayerControl killer)
    {
        if (!CheckDeadBodyOthers.Any()) return;
        //Execute other viewpoint processing if any
        foreach (var checkDeadBodyOthers in CheckDeadBodyOthers.ToArray())
        {
            checkDeadBodyOthers(deadBody, killer);
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
