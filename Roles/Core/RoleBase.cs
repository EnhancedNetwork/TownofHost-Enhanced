using AmongUs.GameOptions;
using MS.Internal.Xml.XPath;
using static UnityEngine.GraphicsBuffer;
//using System.Text;

namespace TOHE;

public abstract class RoleBase
{
    // This is a base class for all roles. It contains some common methods and properties that are used by all roles.

    /// <summary>
    /// Variable resets when the game starts.
    /// </summary>
    public abstract void Init();

    /// <summary>
    /// When role is applied in the game, beginning or during the game.
    /// </summary>
    public abstract void Add(byte playerId);
    
    /// <summary>
    /// Make a bool and apply IsEnable => {Bool};
    /// </summary>
    // if role exists in game
    public abstract bool IsEnable { get; }

    // Some virtual methods that trigger actions, like venting, petting, CheckMurder, etc. These are not abstract because they have a default implementation. These should also have the same name as the methods in the derived classes.
    public virtual void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Options.DefaultKillCooldown;

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

    //public virtual void SetupCustomOption()
    //{ }

    /// <summary>
    /// A generic method to send a CustomRole's Gameoptions.
    /// </summary>
    public virtual void ApplyGameOptions(IGameOptions opt, byte playerId)
    { }

    /// <summary>
    /// Fixed Update for local role
    /// </summary>
    public virtual void OnFixedUpdate(PlayerControl pc)
    { }
    
    /// <summary>
    /// A generic method to check conditions during gameplay, which aren't prioritized.
    /// </summary>
    public virtual void OnFixedUpdateLowLoad(PlayerControl pc)
    { }

    public virtual void OnTaskComplete(PlayerControl pc, int completedTaskCount, int totalTaskCount)
    { }

    public virtual void OnCoEnterVent(PlayerPhysics physics, Vent vent)
    { }

    public virtual void OnEnterVent(PlayerControl pc, Vent vent)
    { }

    /// <summary>
    /// A generic method to activate actions once (CustomRole)player exists vent.
    /// </summary>
    public virtual void OnExitVent(PlayerControl pc, Vent vent)
    { }

    /// <summary>
    /// A generic method to activate actions once (base)impostor uses kill button.
    /// </summary>
    public virtual bool OnCheckMurderOnTarget(PlayerControl killer, PlayerControl target)
    {
        return target != null && killer != null;
    }

    /// <summary>
    /// A generic method to check (base)impostor kill button use.
    /// </summary>
    public virtual bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        return target != null && killer != null;
    }

    public virtual void OnMurder(PlayerControl killer, PlayerControl target)
    { }
   
    /// <summary>
    /// A generic method to activate actions for when a (CustomRole)player is confirmed dead.
    /// </summary>
    public virtual void OnPlayerDead(PlayerControl killer, PlayerControl target)
    { }

    /// <summary>
    /// A generic method to do tasks for when a (CustomRole)player is shapeshifting.
    /// </summary>
    public virtual void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool shapeshifting, bool shapeshiftIsHidden)
    { }

    /// <summary>
    /// A generic method to activate actions right when a meeting starts.
    /// </summary>
    public virtual void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    { }
  
    public virtual void NotifyAfterMeeting()
    { }

    /// <summary>
    /// A generic method to activate actions after a meeting has ended.
    /// </summary>
    public virtual void AfterMeetingTasks()
    { }

    public virtual void CheckExileTarget(PlayerControl player, bool DecidedWinner)
    { }

    public virtual void OnPlayerExiled(PlayerControl Bard, GameData.PlayerInfo exiled)
    { }

    //public virtual string GetProgressText(byte playerId, bool comms)
    //{
    //    var sb = new StringBuilder();
    //    sb.Append(Utils.GetTaskCount(playerId, comms));
    //    sb.Append(Utils.GetAbilityUseLimitDisplay(playerId));
    //    return sb.ToString();
    //}

    /// <summary>
    /// A generic method to replace Modded Client Customrole kill button text.
    /// </summary>
    public virtual void SetAbilityButtonText(HudManager hud, byte id) => hud.KillButton?.OverrideText(Translator.GetString("KillButtonText"));

    public virtual void OnCoEndGame()
    { }
    
    /// <summary>
    /// A generic method to check a Guardian Angel protecting someone.
    /// </summary>
    public virtual bool OnCheckProtect(PlayerControl angel, PlayerControl target)
    {
        return target != null && angel != null;
    }
    /// <summary>
    /// A generic method to receive Customrole's RPC.
    /// </summary>
    public virtual void OReceiveRPC(MessageReader reader) // If Override 
    { }
}
