using AmongUs.GameOptions;
using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles._Ghosts_.Crewmate;

internal class GuardianAngelTOHE : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.GuardianAngelTOHE;
    private const int Id = 20900;

    public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateVanillaGhosts;
    //==================================================================\\

    private static OptionItem AbilityCooldown;
    private static OptionItem ProtectDur;
    private static OptionItem ImpVis;

    private readonly Dictionary<byte, long> PlayerShield = [];
    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.GuardianAngelTOHE);
        AbilityCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.GuardianAngelBase_ProtectCooldown, new(2.5f, 120f, 2.5f), 35f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.GuardianAngelTOHE])
            .SetValueFormat(OptionFormat.Seconds);
        ProtectDur = IntegerOptionItem.Create(Id + 11, GeneralOption.GuardianAngelBase_ProtectionDuration, new(1, 120, 1), 10, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.GuardianAngelTOHE])
            .SetValueFormat(OptionFormat.Seconds);
        ImpVis = BooleanOptionItem.Create(Id + 12, GeneralOption.GuardianAngelBase_ImpostorsCanSeeProtect, true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.GuardianAngelTOHE]);
    }
    public override void Init()
    {
        PlayerShield.Clear();
    }
    public override void Add(byte playerId)
    {
        CustomRoleManager.OnFixedUpdateOthers.Add(OnOthersFixedUpdate);
        CustomRoleManager.CheckDeadBodyOthers.Add(CheckDeadBody);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.GuardianAngelCooldown = AbilityCooldown.GetFloat();
        AURoleOptions.ProtectionDurationSeconds = ProtectDur.GetFloat();
        AURoleOptions.ImpostorsCanSeeProtect = ImpVis.GetBool();
    }
    public override bool OnCheckProtect(PlayerControl angel, PlayerControl target)
    {
        PlayerShield[target.PlayerId] = Utils.GetTimeStamp();
        return true;
    }
    private void CheckDeadBody(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (inMeeting) return;

        var targetId = target.PlayerId;
        if (PlayerShield.ContainsKey(targetId))
            PlayerShield.Remove(targetId);
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (PlayerShield.ContainsKey(target.PlayerId))
        {
            if (ImpVis.GetBool())
            {
                killer.SetKillCooldown();
                killer.RpcGuardAndKill(target);
            }
            return true;
        }
        return false;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        PlayerShield.Clear();
    }
    private void OnOthersFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (PlayerShield.TryGetValue(player.PlayerId, out var timer) && timer + ProtectDur.GetInt() <= nowTime)
        {
            PlayerShield.Remove(player.PlayerId);
        }
    }
}
