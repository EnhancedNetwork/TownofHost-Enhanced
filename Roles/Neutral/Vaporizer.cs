using System;
using TOHE.Roles.Core;
using static TOHE.Options;
namespace TOHE.Roles.Neutral;

internal class Vaporizer : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 32100;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Vaporizer);
    public override CustomRoles Role => CustomRoles.Vaporizer;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    public static OptionItem VaporizerKillCooldown;
    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Vaporizer);
        VaporizerKillCooldown = FloatOptionItem.Create(Id + 2, GeneralOption.KillCooldown, new(1f, 60f, 1f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Vaporizer])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Vaporized;
        killer.RpcGuardAndKill(target);
        target.RpcExileV2();
        Main.PlayerStates[target.PlayerId].SetDead();
        target.Data.IsDead = true;
        target.SetRealKiller(killer);
        target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Vaporizer), GetString("VaporizedTarget")));
        killer.SetKillCooldown();
        return false;
    }

    public override bool CanUseKillButton(PlayerControl pc) => true;

    private string GetString(string v)
    {
        throw new NotImplementedException();
    }
}
