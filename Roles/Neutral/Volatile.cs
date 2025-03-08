using static TOHE.Translator;
using System;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Volatile : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Volatile;
    private const int Id = 34400;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem DefaultKillCooldown;
    private static OptionItem IncreaseKillCooldown;
    private static OptionItem ProtectDuration;

    private long TimeStamp;
    private bool InProtect() => TimeStamp > Utils.GetTimeStamp();

    private static readonly Dictionary<byte, float> NowCooldown = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Volatile, 1, zeroOne: false);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.DefaultKillCooldown, new(0f, 60f, 2f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Volatile])
            .SetValueFormat(OptionFormat.Seconds);
        IncreaseKillCooldown = FloatOptionItem.Create(Id + 11, "IncreaseKillCooldown344", new(0f, 10f, 1f), 3f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Volatile])
            .SetValueFormat(OptionFormat.Seconds);
        ProtectDuration = FloatOptionItem.Create(Id + 13, "ProtectDuration344", new(1f, 180f, 1f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Volatile])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        NowCooldown.Clear();
    }
    public override void Add(byte playerId)
    {
        TimeStamp = 0;
        NowCooldown.TryAdd(playerId, DefaultKillCooldown.GetFloat());
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown[id];
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
   
        Main.AllPlayerKillCooldown[killer.PlayerId] += IncreaseKillCooldown.GetFloat();
        killer.SyncSettings();
        return true;
    }
    public override void OnMurderPlayerAsKiller(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (inMeeting || isSuicide) return;

        TimeStamp = Utils.GetTimeStamp() + (long)ProtectDuration.GetFloat();
        killer.Notify(GetString("InProtect344"));
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (InProtect())
        {
            killer.RpcGuardAndKill(target);
            if (!DisableShieldAnimations.GetBool()) target.RpcGuardAndKill();
            target.Notify(GetString("OffsetKill344"));
            return false;
        }
        else if (killer.GetCustomRole() == target.GetCustomRole()) return false;
        return true;
    }
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;
    public override bool CanUseKillButton(PlayerControl pc) => true;

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (!lowLoad && TimeStamp < nowTime && TimeStamp != 0)
        {
            TimeStamp = 0;
            player.Notify(GetString("ProtectOut344"), sendInLog: false);
        }
    }
}
