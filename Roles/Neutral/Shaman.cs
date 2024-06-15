using static TOHE.Translator;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Shaman : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 13600;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    private static OptionItem VoodooCooldown;

    private static byte ShamanTarget = byte.MaxValue;
    private static bool ShamanTargetChoosen = false;

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
        PlayerIds.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
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

        PlayerControl ChoosenTarget = ChangeTarget(target);

        if (killer.CheckForInvalidMurdering(ChoosenTarget) && killer.RpcCheckAndMurder(ChoosenTarget, check: true))
        {
            killer.RpcMurderPlayer(ChoosenTarget);
            ChoosenTarget.SetRealKiller(target);
        }
        else
        {
            target.Notify(GetString("Shaman_KillerCannotMurderChosenTarget"), time: 10f);
        }
        ShamanTarget = byte.MaxValue;
        return false;
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (ShamanTargetChoosen == false)
        {
            ShamanTarget = target.PlayerId;
            killer.RpcGuardAndKill(killer);
            ShamanTargetChoosen = true;
        }
        else killer.Notify(GetString("ShamanTargetAlreadySelected"));
        return false;

    }
    public static PlayerControl ChangeTarget(PlayerControl target)
        => target.IsAlive() && ShamanTargetChoosen ? Utils.GetPlayerById(ShamanTarget) : target;
    
}
