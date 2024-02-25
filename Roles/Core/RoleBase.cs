using AmongUs.GameOptions;
using Il2CppSystem.Runtime.Remoting.Messaging;
using Il2CppSystem.Text;
using System.Collections.Generic;

namespace TOHE;

public abstract class RoleBase
{
    // This is a base class for all roles. It contains some common methods and properties that are used by all roles.
    public abstract void Init();
    public abstract void Add(byte playerId);
    // if role exists in game
    public abstract bool IsEnable { get; }
    public abstract CustomRoles ThisRoleBase { get; }
    // Used to Determine the CustomRole's BASE
    
    // Some virtual methods that trigger actions, like venting, petting, CheckMurder, etc. These are not abstract because they have a default implementation. These should also have the same name as the methods in the derived classes.
    public virtual void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Options.DefaultKillCooldown;

    /// <summary>
    /// A local method to determine if (base)imp/SS can use kill button
    /// </summary>
    public virtual bool CanUseKillButton(PlayerControl pc) => pc.Is(CustomRoleTypes.Impostor) && pc.IsAlive();

    /// <summary>
    /// A local method to determine if (base)imp/SS can use vent button
    /// </summary>
    public virtual bool CanUseImpostorVentButton(PlayerControl pc) => pc.IsAlive() && pc.GetCustomRole().GetRoleTypes() is RoleTypes.Impostor or RoleTypes.Shapeshifter;

    /// <summary>
    /// A Local method to determine if (base)imp/SS can sabotage
    /// </summary>
    public virtual bool CanUseSabotage(PlayerControl pc) =>  pc.Is(CustomRoleTypes.Impostor);

    //public virtual void SetupCustomOption()
    //{ }

    /// <summary>
    /// A method to set Role's game options during gameplay
    /// </summary>
    public virtual void ApplyGameOptions(IGameOptions opt, byte playerId)
    { }

    /// <summary>
    /// A local method to check conditions during gameplay, 30 times each second
    /// </summary>
    public virtual void OnFixedUpdate(PlayerControl pc)
    { }

    /// <summary>
    /// A local method to check conditions during gameplay, which aren't prioritized
    /// </summary>
    public virtual void OnFixedUpdateLowLoad(PlayerControl pc)
    { }

    /// <summary>
    /// OnTaskComplete overload with taskcount included
    /// </summary>
    public virtual void OnTaskComplete(PlayerControl pc, int completedTaskCount = 0, int totalTaskCount = 0)
    { }

    /// <summary>
    /// A method to determine actions once role completes a task
    /// </summary>
    public virtual void OnTaskComplete(PlayerControl pc)
    { }

    public virtual void OnCoEnterVent(PlayerPhysics physics, Vent vent)
    { }

    public virtual void OnEnterVent(PlayerControl pc, Vent vent)
    { }

    public virtual void OnExitVent(PlayerControl pc, Vent vent)
    { }

    /// <summary>
    /// A method to run actions on kill button usage
    /// </summary>
    public virtual bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        return target != null && killer != null;
    }

    /// <summary>
    /// A method to set conditions when attempted kill
    /// </summary>
    public virtual bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        return target != null && killer != null;
    }

    public virtual void OnMurder(PlayerControl killer, PlayerControl target)
    { }

    /// <summary>
    /// A method to run actions when playerrole is confirmed dead
    /// </summary>
    public virtual void OnPlayerDead(PlayerControl killer, PlayerControl target)
    { }

    /// <summary>
    /// A method to set conditions when playerole shapeshifts
    /// </summary>
    public virtual void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool shapeshifting, bool shapeshiftIsHidden)
    { }

    public virtual bool OnCheckReportDeadBody(PlayerControl reporter, PlayerControl target) => reporter.IsAlive();
    public virtual bool OnPressReportButton(PlayerControl reporter, PlayerControl target) => reporter.IsAlive();

    /// <summary>
    /// OnReportDeadBody Overload with reporter and target
    /// </summary>
    public virtual void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    { }

    /// <summary>
    /// A method to determine actions once meeting is called
    /// </summary>
    public virtual void OnReportDeadBody()
    { }

    public virtual void NotifyAfterMeeting()
    { }

    /// <summary>
    /// A method to determine actions which happen post-meeting
    /// </summary>
    public virtual void AfterMeetingTasks()
    { }

    public virtual void CheckExileTarget(PlayerControl player, bool DecidedWinner)
    { }

    public virtual void OnPlayerExiled(PlayerControl Bard, GameData.PlayerInfo exiled)
    { }

    /// <summary>
    /// A method to run actions once playerole is confirmed ejected(exiled)
    /// </summary>
    public virtual void OnPlayerExiled(GameData.PlayerInfo exiled)
    { }

    //public virtual string GetProgressText(byte playerId, bool comms)
    //{
    //    var sb = new StringBuilder();
    //    sb.Append(Utils.GetTaskCount(playerId, comms));
    //    sb.Append(Utils.GetAbilityUseLimitDisplay(playerId));
    //    return sb.ToString();
    //}

    public virtual void SetAbilityButtonText(HudManager hud, byte id) => hud.KillButton?.OverrideText(Translator.GetString("KillButtonText"));

    public virtual void OnCoEndGame()
    { }

    /// <summary>
    /// A method to determine actions once Guardian angel tries protecting
    /// </summary>
    public virtual void OnCheckProtect(PlayerControl angel, PlayerControl target)
    { }

    /// <summary>
    /// A method to determine conditions when attempt to guess role
    /// </summary>
    public virtual bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser) 
    {
        return target == null;
    }

    /// <summary>
    /// A method to determine a playermark for himself or others
    /// </summary>
    public virtual void NotifyRoleMark(PlayerControl seer, PlayerControl target, System.Text.StringBuilder Mark)
    { }

    /// <summary>
    /// NotifyRoleMark overload for roles that change the PlayerName or Text
    /// </summary>
    public virtual string NotifyRoleMark(PlayerControl seer, PlayerControl target, string TargetPlayerName = "", bool IsForMeeting = false) => string.Empty;
   

    /// <summary>
    /// A method to determine conditions on voter/targetvote
    /// </summary>
    public virtual void OnVote(PlayerControl pc, PlayerControl voteTarget)
    { }


}
