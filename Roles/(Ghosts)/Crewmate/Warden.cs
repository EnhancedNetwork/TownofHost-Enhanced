using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles._Ghosts_.Crewmate;

internal class Warden : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Warden;
    private const int Id = 27800;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Warden);
    public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateGhosts;
    //==================================================================\\

    private static OptionItem AbilityCooldown;
    private static OptionItem IncreaseSpeed;
    private static OptionItem WardenCanAlertNum;

    private readonly HashSet<byte> IsAffected = [];
    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Warden);
        AbilityCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.AbilityCooldown, new(0f, 120f, 2.5f), 25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Warden])
            .SetValueFormat(OptionFormat.Seconds);
        IncreaseSpeed = FloatOptionItem.Create(Id + 11, "WardenIncreaseSpeed", new(1f, 3f, 0.5f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Warden])
            .SetValueFormat(OptionFormat.Times);
        WardenCanAlertNum = IntegerOptionItem.Create(Id + 12, "WardenNotifyLimit", new(1, 20, 1), 2, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Warden])
               .SetValueFormat(OptionFormat.Players);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(WardenCanAlertNum.GetInt());
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.GuardianAngelCooldown = AbilityCooldown.GetFloat();
        AURoleOptions.ProtectionDurationSeconds = 0f;
    }
    public override bool OnCheckProtect(PlayerControl killer, PlayerControl target)
    {
        var getTargetRole = target.GetCustomRole();
        if (killer.GetAbilityUseLimit() > 0)
        {
            if (getTargetRole.IsSpeedRole() || target.IsAnySubRole(x => x.IsSpeedRole()) || IsAffected.Contains(target.PlayerId)) goto Notifiers; // Incompactible speed-roles 

            IsAffected.Add(target.PlayerId);
            target.MarkDirtySettings();
            var tmpSpeed = Main.AllPlayerSpeed[target.PlayerId];
            Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] + IncreaseSpeed.GetFloat();


            _ = new LateTask(() =>
            {
                Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - Main.AllPlayerSpeed[target.PlayerId] + tmpSpeed;
                target.MarkDirtySettings();
                if (IsAffected.Contains(target.PlayerId)) IsAffected.Remove(target.PlayerId);
            }, 2f, "Warden: Set Speed to default");

        Notifiers:
            RPC.PlaySoundRPC(Sounds.SabotageSound, target.PlayerId);
            target.Notify(Utils.ColorString(new Color32(179, 0, 0, byte.MaxValue), GetString("WardenWarn")));

            killer.RpcResetAbilityCooldown();
            killer.RpcRemoveAbilityUse();
        }
        return false;
    }
}
