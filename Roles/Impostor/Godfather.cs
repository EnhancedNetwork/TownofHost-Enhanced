using System.Collections.Generic;
using TOHE.Roles.Core;

namespace TOHE.Roles.Impostor;

internal class Godfather : RoleBase
{
    private const int Id = 3400;
    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    private static OptionItem GodfatherChangeOpt;

    private static List<byte> GodfatherTarget = [];

    private enum GodfatherChangeMode
    {
        GodfatherCount_Refugee,
        GodfatherCount_Madmate
    }

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Godfather);
        GodfatherChangeOpt = StringOptionItem.Create(Id + 2, "GodfatherTargetCountMode", EnumHelper.GetAllNames<GodfatherChangeMode>(), 0, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Godfather]);
    }

    public override void Init()
    {
        On = false;
        GodfatherTarget = [];
    }
    public override void Add(byte playerId)
    {
        On = true;

        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.CheckDeadBodyOthers.Add(CheckDeadBody);
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target) => GodfatherTarget.Clear();
    private void CheckDeadBody(PlayerControl target, PlayerControl killer)
    {
        if (GodfatherTarget.Contains(target.PlayerId) && !(killer.GetCustomRole().IsImpostor() || killer.GetCustomRole().IsMadmate() || killer.Is(CustomRoles.Madmate)))
        {
            if (GodfatherChangeOpt.GetValue() == 0) killer.RpcSetCustomRole(CustomRoles.Refugee);
            else killer.RpcSetCustomRole(CustomRoles.Madmate);
        }
    }

    public override void OnVote(PlayerControl votePlayer, PlayerControl voteTarget)
    {
        if (votePlayer == null || voteTarget == null) return;

        GodfatherTarget.Add(voteTarget.PlayerId);
    }
}
