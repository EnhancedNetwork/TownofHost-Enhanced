using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Crewmate;

internal class Astral : RoleBase
{
    //===========================SETUP================================\\

    public static bool InRevival = false;
    public override CustomRoles Role => CustomRoles.Astral;
    private const int Id = 33200;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\
    public bool Revived = false;
    Vector2 teleportPosition;

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = 1;
    }

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Astral);
        OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.Astral);
    }

    public override void OnEnterVent(PlayerControl shapeshifter, Vent vent)
    {
        teleportPosition = vent.transform.position;
        shapeshifter.RpcMurderPlayer(shapeshifter);
        InRevival = true;
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("AstralShapeshiftText"));
    }

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        var astralTask = player.GetPlayerTaskState();

        if (InRevival == true && astralTask.CompletedTasksCount >= astralTask.AllTasksCount)
        {
            player.RpcRevive();
            player.RpcChangeRoleBasis(CustomRoles.Phantasm);
            player.RpcSetCustomRole(CustomRoles.Phantasm, true);
            player.RpcTeleport(teleportPosition);
            InRevival = false;
        }
        if (!InRevival)
        {
            if (player.IsAlive())
            {
                teleportPosition = player.transform.position;
                player.RpcMurderPlayer(player);
                InRevival = true;
            }
        }
        return true;
    }
    public override void AfterMeetingTasks()
    {
        InRevival = false;
    }

}
internal class Phantasm : RoleBase
{
    public override CustomRoles Role => CustomRoles.Phantasm;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    public override bool CanUseKillButton(PlayerControl player) => true;
}
