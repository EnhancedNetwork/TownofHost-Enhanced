using AmongUs.GameOptions;
using UnityEngine;
using static TOHE.Options;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Convict : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Convict;
    private const int Id = 31500;

    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.Madmate;
    //==================================================================\\

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Convict);
    }

    public override void Init()
    {}
    public override void Add(byte playerId)
    {}
    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => !ForRecompute;

    public override void SetKillCooldown(byte id) 
        => Main.AllPlayerKillCooldown[id] = 1f;//have to set kill CD here,otherwise the starting CD for vanilla Renegade will be 300s

    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(true);

    public override void AfterMeetingTasks()
    { 
        var convict = _Player;
        var taskstate = convict.GetPlayerTaskState();

        int completed = taskstate.CompletedTasksCount;//total number of Convict's tasks
        int required = convict.Is(CustomRoles.LastImpostor) ? taskstate.AllTasksCount / 2 : taskstate.AllTasksCount;//required number of tasks for Convict to become Refugee
 
        if (convict.IsAlive() && completed >= required)
        {
            convict.RpcChangeRoleBasis(CustomRoles.Refugee);
            convict.RpcSetCustomRole(CustomRoles.Refugee);
            convict.SyncSettings();
            convict.SetKillCooldown();
            convict.Notify(string.Format(GetString("ConvictToRefugeeMsg"), CustomRoles.Refugee.ToString()));
        }
    }
}
