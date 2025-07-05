using AmongUs.GameOptions;
using Hazel;
using System.Text;
using TOHE.Modules.Rpc;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class BloodKnight : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.BloodKnight;
    private const int Id = 16100;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.BloodKnight);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    private static OptionItem ProtectDuration;

    private long TimeStamp;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.BloodKnight, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.BloodKnight])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.BloodKnight]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.BloodKnight]);
        ProtectDuration = FloatOptionItem.Create(Id + 14, "BKProtectDuration", new(1f, 180f, 1f), 15f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.BloodKnight])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add(byte playerId)
    {
        TimeStamp = 0;
    }
    private void SendRPC()
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(TimeStamp.ToString());
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        string Time = reader.ReadString();
        TimeStamp = long.Parse(Time);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());

    private bool InProtect() => TimeStamp > Utils.GetTimeStamp();

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (InProtect())
        {
            killer.RpcGuardAndKill(target);
            if (!DisableShieldAnimations.GetBool()) target.RpcGuardAndKill();
            target.Notify(GetString("BKOffsetKill"));
            return false;
        }
        else if (killer.GetCustomRole() == target.GetCustomRole()) return false;
        return true;
    }
    public override void OnMurderPlayerAsKiller(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (inMeeting || isSuicide) return;

        TimeStamp = Utils.GetTimeStamp() + (long)ProtectDuration.GetFloat();
        SendRPC();
        _ = new LateTask(() =>
        {
            killer.Notify(GetString("BKInProtect"));
        }, target.Is(CustomRoles.Burst) ? Burst.BurstKillDelay.GetFloat() : 0f, "BurstKillCheck");
    }

    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override bool CanUseKillButton(PlayerControl pc) => true;

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (!lowLoad && TimeStamp < nowTime && TimeStamp != 0)
        {
            TimeStamp = 0;
            player.Notify(GetString("BKProtectOut"), sendInLog: false);
        }
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen, bool isForMeeting = false, bool isForHud = false)
    {
        if (!seer.IsAlive() || seer.PlayerId != seen.PlayerId || isForMeeting || !isForHud) return string.Empty;

        var str = new StringBuilder();
        if (InProtect())
        {
            var remainTime = TimeStamp - Utils.GetTimeStamp();
            str.Append(string.Format(GetString("BKSkillTimeRemain"), remainTime));
        }
        else
        {
            str.Append(GetString("BKSkillNotice"));
        }
        return str.ToString();
    }
}
