using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;

namespace TOHE.Roles.Crewmate;

internal class Knight : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Knight;
    private const int Id = 10800;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Knight);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem CanVent;
    private static OptionItem KillCooldown;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Knight);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 60f, 2.5f), 15f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Knight])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Knight]);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(1);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public static bool CheckCanUseVent(PlayerControl player) => player.Is(CustomRoles.Knight) && CanVent.GetBool();
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CheckCanUseVent(pc);

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = IsKilled(id) ? 300f : KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc)
        => !IsKilled(pc.PlayerId);

    private static bool IsKilled(byte playerId) => playerId.GetAbilityUseLimit() <= 0;
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl banana)
    {
        killer.RpcRemoveAbilityUse();
        Logger.Info($"{killer.GetNameWithRole()} : " + "Kill chance used", "Knight");
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        return true;
    }
}
