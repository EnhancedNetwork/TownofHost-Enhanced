using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Shaman : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Shaman;
    private const int Id = 13600;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Shaman);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    private static OptionItem VoodooCooldown;

    private byte ShamanTarget = byte.MaxValue;
    private bool ShamanTargetChoosen = false;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Shaman);
        VoodooCooldown = FloatOptionItem.Create(Id + 10, "VoodooCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Shaman])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        ShamanTarget = byte.MaxValue;
        ShamanTargetChoosen = false;
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override void AfterMeetingTasks()
    {
        ShamanTarget = byte.MaxValue;
        ShamanTargetChoosen = false;
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = VoodooCooldown.GetFloat();
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("ShamanButtonText"));
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (ShamanTarget == byte.MaxValue) return true;

        PlayerControl ChoosenTarget = ChangeTarget(_Player);

        if (killer.CheckForInvalidMurdering(ChoosenTarget) && killer.RpcCheckAndMurder(ChoosenTarget, check: true))
        {
            killer.RpcMurderPlayer(ChoosenTarget);
            ChoosenTarget.SetRealKiller(_Player);
        }
        else
        {
            _Player.Notify(GetString("Shaman_KillerCannotMurderChosenTarget"), time: 10f);
        }
        ShamanTarget = byte.MaxValue;
        return false;
    }
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (ShamanTargetChoosen == false)
        {
            ShamanTarget = target.PlayerId;
            _Player.RpcGuardAndKill(target);
            ShamanTargetChoosen = true;
        }
        else _Player.Notify(GetString("ShamanTargetAlreadySelected"));
        return false;
    }
    private PlayerControl ChangeTarget(PlayerControl target)
        => target.IsAlive() && ShamanTargetChoosen ? Utils.GetPlayerById(ShamanTarget) : target;

}
