using TOHE.Modules;
using TOHE.Roles.Core;

namespace TOHE.Roles.Crewmate;

internal class Savior : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 32400;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Savior);
    public override CustomRoles Role => CustomRoles.Savior;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem ResetCooldown;

    public static readonly List<byte> ProtectList = [];

    private static byte TempMarkProtected;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Savior);
        ResetCooldown = FloatOptionItem.Create(Id + 30, "SaviorResetCooldown", new(0f, 120f, 1f), 10f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Savior])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        ProtectList.Clear();
        TempMarkProtected = byte.MaxValue;
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(1);

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (ProtectList.Contains(target.PlayerId)) return false;
        if (killer.GetAbilityUseLimit() <= 0) return false;

        killer.RpcRemoveAbilityUse();
        ProtectList.Add(target.PlayerId);
        TempMarkProtected = target.PlayerId;

        if (!Options.DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill();

        return false;
    }

    public override bool CanUseKillButton(PlayerControl pc) => true;

    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        var Saviors = Utils.GetPlayerListByRole(CustomRoles.Savior);
        if (killer == null || target == null || Saviors == null || !Saviors.Any()) return true;
        if (ProtectList.Contains(target.PlayerId))
        {
            killer.RpcGuardAndKill(killer);
            return false;
        }
        Logger.Info($"{target.GetNameWithRole()} : Shield Shatter from the Savior", "Savior");
        return true;
    }
    public override void AfterMeetingTasks()
    {
        ProtectList.Clear();

        if (_Player == null) return;
        _Player.SetAbilityUseLimit(1);
    }
}
