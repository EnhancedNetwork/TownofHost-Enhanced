using AmongUs.GameOptions;
using static UnityEngine.GraphicsBuffer;
//using System.Text;

namespace TOHE;

public abstract class RoleBase
{
    // This is a base class for all roles. It contains some common methods and properties that are used by all roles.
    public abstract void Init();
    public abstract void Add(byte playerId);

    // if role exists in game
    public abstract bool IsEnable { get; }

    // Some virtual methods that trigger actions, like venting, petting, CheckMurder, etc. These are not abstract because they have a default implementation. These should also have the same name as the methods in the derived classes.
    public virtual void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Options.DefaultKillCooldown;

    public virtual bool CanUseKillButton(PlayerControl pc) => pc.IsAlive();

    public virtual bool CanUseImpostorVentButton(PlayerControl pc) => pc.IsAlive() && pc.GetCustomRole().GetRoleTypes() is not RoleTypes.Crewmate and not RoleTypes.Engineer;

    public virtual bool CanUseSabotage(PlayerControl pc) =>  pc.Is(CustomRoleTypes.Impostor);

    //public virtual void SetupCustomOption()
    //{ }

    public virtual void ApplyGameOptions(IGameOptions opt, byte playerId)
    { }

    public virtual void OnFixedUpdate(PlayerControl pc)
    { }
    public virtual void OnFixedUpdateLowLoad(PlayerControl pc)
    { }

    public virtual void OnTaskComplete(PlayerControl pc, int completedTaskCount, int totalTaskCount)
    { }

    public virtual void OnCoEnterVent(PlayerPhysics physics, Vent vent)
    { }

    public virtual void OnEnterVent(PlayerControl pc, Vent vent)
    { }

    public virtual void OnExitVent(PlayerControl pc, Vent vent)
    { }

    public virtual bool OnCheckMurderOnTarget(PlayerControl killer, PlayerControl target)
    {
        return target != null && killer != null;
    }

    public virtual bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        return target != null && killer != null;
    }

    public virtual void OnMurder(PlayerControl killer, PlayerControl target)
    { }

    public virtual void OnPlayerDead(PlayerControl killer, PlayerControl target)
    { }

    public virtual void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool shapeshifting, bool shapeshiftIsHidden)
    { }

    public virtual void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    { }

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

    public virtual void SetButtonTexts(HudManager hud, byte id) => hud.KillButton?.OverrideText(Translator.GetString("KillButtonText"));

    public virtual void OnCoEndGame()
    { }

    public virtual void OnCheckProtect(PlayerControl angel, PlayerControl target)
    { }
}
