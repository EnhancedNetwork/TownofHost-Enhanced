using AmongUs.GameOptions;
using UnityEngine;

namespace TOHE;

public abstract class RoleBase
{
    /// <summary>
    /// Variable resets when the game starts.
    /// </summary>
    public abstract void Init();

    /// <summary>
    /// When role is applied in the game, beginning or during the game.
    /// </summary>
    public abstract void Add(byte playerId);

    /// <summary>
    /// If roles has to be removed from player
    /// </summary>
    public abstract void Remove(byte playerId);
    /// <summary>
    /// Make a bool and apply IsEnable => {Bool};
    /// </summary>
    public abstract bool IsEnable { get; }

    /// <summary>
    /// Used to Determine the CustomRole's BASE
    /// </summary>
    public abstract CustomRoles ThisRoleBase { get; }

    /// <summary>
    /// A generic method to set if a impostor/SS base may use kill button.
    /// </summary>
    public virtual bool CanUseKillButton(PlayerControl pc) => pc.Is(CustomRoleTypes.Impostor) && pc.IsAlive();

    /// <summary>
    /// A generic method to set if a impostor/SS base may vent.
    /// </summary>
    public virtual bool CanUseImpostorVentButton(PlayerControl pc) => pc.IsAlive() && pc.GetCustomRole().GetRoleTypes() is RoleTypes.Impostor or RoleTypes.Shapeshifter;

    /// <summary>
    /// A generic method to set if the role can use sabotage.
    /// </summary>
    public virtual bool CanUseSabotage(PlayerControl pc) =>  pc.Is(CustomRoleTypes.Impostor);

    /// <summary>
    /// When the player presses the sabotage button
    /// </summary>
    public virtual bool OnSabotage(PlayerControl pc) => pc != null;

    //public virtual void SetupCustomOption()
    //{ }

    /// <summary>
    /// A generic method to send a CustomRole's Gameoptions.
    /// </summary>
    public virtual void ApplyGameOptions(IGameOptions opt, byte playerId)
    { }

    /// <summary>
    /// Set a specific kill cooldown
    /// </summary>
    public virtual void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Options.DefaultKillCooldown;

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
    /// Player completes a task
    /// </summary>
    public virtual void OnTaskComplete(PlayerControl pc, int completedTaskCount, int totalTaskCount)
    { }

    /// <summary>
    /// A method for activating actions where the role starts playing an animation when entering a vent
    /// </summary>
    public virtual void OnEnterVent(PlayerControl pc, Vent vent)
    { }

    /// <summary>
    /// A method for activating actions when role is already in vent
    /// </summary>
    public virtual void OnCoEnterVent(PlayerPhysics physics, Vent vent)
    { }

    /// <summary>
    /// A generic method to activate actions once (CustomRole)player exists vent.
    /// </summary>
    public virtual void OnExitVent(PlayerControl pc, Vent vent)
    { }
    /// <summary>
    /// A generic method to check a Guardian Angel protecting someone.
    /// </summary>
    public virtual void OnCheckProtect(PlayerControl angel, PlayerControl target)
    { }

    /// <summary>
    /// When role the target requires a kill check
    /// </summary>
    public virtual bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target) => target != null && killer != null;

    /// <summary>
    ///  When role the killer requires a kill check
    /// </summary>
    public virtual bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target) => target != null && killer != null;

    /// <summary>
    /// When the killer murder his target
    /// </summary>
    public virtual void OnMurder(PlayerControl killer, PlayerControl target)
    { }

    /// <summary>
    /// When the target role died
    /// </summary>
    public virtual void OnPlayerDead(PlayerControl killer, PlayerControl target)
    { }

    /// <summary>
    /// A generic method to do tasks for when a (CustomRole)player is shapeshifting.
    /// </summary>
    public virtual void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool shapeshifting, bool shapeshiftIsHidden)
    { }

    /// <summary>
    /// Checking that a dead body can be reported
    /// </summary>
    public virtual bool CheckReportDeadBody(PlayerControl reporter, GameData.PlayerInfo deadBody, PlayerControl killer) => reporter.IsAlive();

    /// <summary>
    /// When reporter press report button
    /// </summary>
    public virtual bool OnPressReportButton(PlayerControl reporter, PlayerControl target) => reporter.IsAlive();

    /// <summary>
    /// When the meeting start by report dead body
    /// </summary>
    public virtual void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    { }

    /// <summary>
    /// When player trying guess a role
    /// </summary>
    public virtual bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser) => target == null;

    /// <summary>
    /// Check exile role
    /// </summary>
    public virtual void CheckExileTarget(PlayerControl player, bool DecidedWinner)
    { }

    /// <summary>
    /// When player was exiled
    /// </summary>
    public virtual void OnPlayerExiled(PlayerControl Bard, GameData.PlayerInfo exiled)
    { }

    /// <summary>
    /// Notify a specific role about something after the meeting was ended.
    /// </summary>
    public virtual void NotifyAfterMeeting()
    { }

    /// <summary>
    /// A generic method to activate actions after a meeting has ended.
    /// </summary>
    public virtual void AfterMeetingTasks()
    { }

    /// <summary>
    /// When the game starts to ending
    /// </summary>
    public virtual void OnCoEndGame()
    { }

    /// <summary>
    /// Set PlayerName text for the role
    /// </summary>
    public virtual string NotifyPlayerName(PlayerControl seer, PlayerControl target, string TargetPlayerName = "", bool IsForMeeting = false) => string.Empty;
   
    /// <summary>
    /// A method to determine conditions on voter/targetvote
    /// </summary>
    public virtual void OnVote(PlayerControl pc, PlayerControl voteTarget)
    { }

    /// <summary>
    /// Set text for Kill/Shapeshift/Report/Vent/Protect button
    /// </summary>
    public virtual void SetAbilityButtonText(HudManager hud, byte id) => hud.KillButton?.OverrideText(Translator.GetString("KillButtonText"));

    /// <summary>
    /// Set button sprite, return "Kill" or "Vent" or "Ability" or "Report" respectively.
    /// </summary>
    public virtual (string, Sprite, string, Sprite) SetAbilityButtonSprite() => (string.Empty, CustomButton.Get("Happy"), string.Empty, CustomButton.Get("Happy"));

    public virtual string GetProgressText(byte playerId, bool comms) => string.Empty;
    public virtual string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => string.Empty;
    public virtual string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false) => string.Empty;
    public virtual string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => string.Empty;
    public virtual bool KnowRoletarget(PlayerControl seer, PlayerControl target) => false;
    public virtual bool KnowTargetRoleColor(PlayerControl seer, PlayerControl target) => false;

    public virtual int CalcVote(PlayerVoteArea PVA) => 0;
}
