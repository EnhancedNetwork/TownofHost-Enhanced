using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles._Ghosts_.Crewmate;

internal class Cursebearer : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 32500;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Cursebearer);
    public override CustomRoles Role => CustomRoles.Cursebearer;
    public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateGhosts;
    //==================================================================\\

    public static OptionItem CBRevealCooldown;
    public int KeepCount = 0;
    public bool KnowTargetRole = false;
    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Cursebearer);
        CBRevealCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 120f, 2.5f), 25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cursebearer]);
    }
    public override void Init()
    {
        KeepCount = 0;
        KnowTargetRole = false;
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(1);

        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.IsAnySubRole(x => x.IsConverted()))
            {
                KeepCount++;
            }
        }

    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.GuardianAngelCooldown = CBRevealCooldown.GetFloat();
        AURoleOptions.ProtectionDurationSeconds = 0f;
    }
    public override bool OnCheckProtect(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() <= 0) return false;
        else
        {
            killer.RpcRemoveAbilityUse();
            target.RpcSetCustomRole(CustomRoles.Revealed);
            return true;
        }
    }


    public static bool KnowRole(PlayerControl seer, PlayerControl target)
    {
        if (target.Is(CustomRoles.Revealed)) return true;
        return false;
    }
}
