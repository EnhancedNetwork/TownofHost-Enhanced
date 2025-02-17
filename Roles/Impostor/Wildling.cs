using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System;
using System.Text;
using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class Wildling : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Wildling;
    private const int Id = 5200;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Wildling);
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem ProtectDuration;
    private static OptionItem ShapeshiftCD;
    private static OptionItem ShapeshiftDur;

    private long TimeStamp;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Wildling, 1, zeroOne: false);
        ProtectDuration = FloatOptionItem.Create(Id + 14, "BKProtectDuration", new(1f, 180f, 1f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wildling])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCD = FloatOptionItem.Create(Id + 15, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(1f, 180f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wildling])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftDur = FloatOptionItem.Create(Id + 16, GeneralOption.ShapeshifterBase_ShapeshiftDuration, new(1f, 180f, 1f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wildling])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        TimeStamp = 0;
    }
    private void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable);
        writer.WriteNetObject(_Player);
        writer.Write(TimeStamp.ToString());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        string Time = reader.ReadString();
        TimeStamp = long.Parse(Time);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCD.GetFloat();
        AURoleOptions.ShapeshifterDuration = ShapeshiftDur.GetFloat();
    }

    public override bool CanUseImpostorVentButton(PlayerControl pc) => false;
    private bool InProtect() => TimeStamp > Utils.GetTimeStamp(DateTime.Now);

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (InProtect())
        {
            killer.RpcGuardAndKill(target);
            if (!DisableShieldAnimations.GetBool()) target.RpcGuardAndKill();
            target.Notify(Translator.GetString("BKOffsetKill"));
            return false;
        }
        return true;
    }

    public override void OnMurderPlayerAsKiller(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (inMeeting || isSuicide) return;

        TimeStamp = Utils.GetTimeStamp(DateTime.Now) + (long)ProtectDuration.GetFloat();
        SendRPC();

        killer.Notify(Translator.GetString("BKInProtect"));
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (!lowLoad && TimeStamp != 0 && TimeStamp < nowTime)
        {
            TimeStamp = 0;
            player.Notify(Translator.GetString("BKProtectOut"));
        }
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (seer == null || isForMeeting || !isForHud || !seer.IsAlive()) return string.Empty;

        var str = new StringBuilder();
        if (InProtect())
        {
            var remainTime = TimeStamp - Utils.GetTimeStamp(DateTime.Now);
            str.Append(string.Format(Translator.GetString("BKSkillTimeRemain"), remainTime));
        }
        else
        {
            str.Append(Translator.GetString("BKSkillNotice"));
        }
        return str.ToString();
    }

}
