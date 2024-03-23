using System.Collections.Generic;
using System.Linq;
using static TOHE.Translator;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Shaman : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 13600;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    //==================================================================\\
    
    private static OptionItem VoodooCooldown;

    private static byte ShamanTarget = byte.MaxValue;
    private static bool ShamanTargetChoosen = false;

    public static void SetupCustomOptions()
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
        => ShamanTarget != byte.MaxValue && target.IsAlive() && ShamanTargetChoosen ? Utils.GetPlayerById(ShamanTarget) : target;
    
}
