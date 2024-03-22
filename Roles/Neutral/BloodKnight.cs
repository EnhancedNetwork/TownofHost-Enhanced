using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using System.Text;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class BloodKnight : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 16100;
    private static List<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    private static OptionItem ProtectDuration;

    private static Dictionary<byte, long> TimeStamp = [];

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.BloodKnight, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.BloodKnight])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.BloodKnight]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.BloodKnight]);
        ProtectDuration = FloatOptionItem.Create(Id + 14, "BKProtectDuration", new(1f, 180f, 1f), 15f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.BloodKnight])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        playerIdList = [];
        TimeStamp = [];
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        TimeStamp.TryAdd(playerId, 0);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.BloodKnight);
        writer.Write(playerId);
        writer.Write(TimeStamp[playerId].ToString());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        string Time = reader.ReadString();
        TimeStamp.TryAdd(PlayerId, long.Parse(Time));
        TimeStamp[PlayerId] = long.Parse(Time);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());

    private static bool InProtect(byte playerId) => TimeStamp.TryGetValue(playerId, out var time) && time > Utils.GetTimeStamp();
    
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (InProtect(target.PlayerId))
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

        TimeStamp[killer.PlayerId] = Utils.GetTimeStamp() + (long)ProtectDuration.GetFloat();
        SendRPC(killer.PlayerId);
        killer.Notify(GetString("BKInProtect"));
    }

    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override bool CanUseKillButton(PlayerControl pc) => true;

    public override void OnFixedUpdateLowLoad(PlayerControl pc)
    {
        if (TimeStamp[pc.PlayerId] < Utils.GetTimeStamp() && TimeStamp[pc.PlayerId] != 0)
        {
            TimeStamp[pc.PlayerId] = 0;
            pc.Notify(GetString("BKProtectOut"), sendInLog: false);
        }
    }
    public override string GetLowerText(PlayerControl pc, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (pc == null || isForMeeting || !isForHud || !pc.IsAlive()) return string.Empty;

        var str = new StringBuilder();
        if (InProtect(pc.PlayerId))
        {
            var remainTime = TimeStamp[pc.PlayerId] - Utils.GetTimeStamp();
            str.Append(string.Format(GetString("BKSkillTimeRemain"), remainTime));
        }
        else
        {
            str.Append(GetString("BKSkillNotice"));
        }
        return str.ToString();
    }
}