using AmongUs.GameOptions;
using Hazel;
using System;
using System.Text;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class Wildling : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 5200;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem ProtectDuration;
    private static OptionItem ShapeshiftCD;
    private static OptionItem ShapeshiftDur;

    private static readonly Dictionary<byte, long> TimeStamp = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Wildling, 1, zeroOne: false);
        ProtectDuration = FloatOptionItem.Create(Id + 14, "BKProtectDuration", new(1f, 180f, 1f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wildling])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCD = FloatOptionItem.Create(Id + 15, "ShapeshiftCooldown", new(1f, 180f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wildling])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftDur = FloatOptionItem.Create(Id + 16, "ShapeshiftDuration", new(1f, 180f, 1f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wildling])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        playerIdList.Clear();
        TimeStamp.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        TimeStamp.TryAdd(playerId, 0);
    }

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Wildling);
        writer.Write(playerId);
        writer.Write(TimeStamp[playerId].ToString());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte PlayerId = reader.ReadByte();
        string Time = reader.ReadString();
        TimeStamp.TryAdd(PlayerId, long.Parse(Time));
        TimeStamp[PlayerId] = long.Parse(Time);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCD.GetFloat();
        AURoleOptions.ShapeshifterDuration = ShapeshiftDur.GetFloat();
    }

    private static bool InProtect(byte playerId) => TimeStamp.TryGetValue(playerId, out var time) && time > Utils.GetTimeStamp(DateTime.Now);

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (InProtect(target.PlayerId))
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

        TimeStamp[killer.PlayerId] = Utils.GetTimeStamp(DateTime.Now) + (long)ProtectDuration.GetFloat();
        SendRPC(killer.PlayerId);

        killer.Notify(Translator.GetString("BKInProtect"));
    }
    public override void OnFixedUpdateLowLoad(PlayerControl pc)
    {
        if (TimeStamp.TryGetValue(pc.PlayerId, out var time) && time != 0 && time < Utils.GetTimeStamp(DateTime.Now))
        {
            TimeStamp[pc.PlayerId] = 0;
            pc.Notify(Translator.GetString("BKProtectOut"));
        }
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (seer == null || isForMeeting || !isForHud || !seer.IsAlive()) return string.Empty;

        var str = new StringBuilder();
        if (InProtect(seer.PlayerId))
        {
            var remainTime = TimeStamp[seer.PlayerId] - Utils.GetTimeStamp(DateTime.Now);
            str.Append(string.Format(Translator.GetString("BKSkillTimeRemain"), remainTime));
        }
        else
        {
            str.Append(Translator.GetString("BKSkillNotice"));
        }
        return str.ToString();
    }

}