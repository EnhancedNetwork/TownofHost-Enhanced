using AmongUs.GameOptions;
using System;
using Hazel;
using InnerNet;
using System.Text;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Protector : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 31200;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();

    public override CustomRoles Role => CustomRoles.Protector;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static OptionItem MaxShields;
    private static OptionItem ShieldDuration;
    private static OptionItem ShieldIsOneTimeUse;

    private long? TimeStamp;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Protector);
        MaxShields = IntegerOptionItem.Create(Id + 10, "ProtectorMaxShields", new(1, 14, 1), 3, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Protector])
            .SetValueFormat(OptionFormat.Votes);
        ShieldDuration = FloatOptionItem.Create(Id + 11, "ProtectorShieldDuration", new(1, 30, 1), 10, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Protector])
            .SetValueFormat(OptionFormat.Seconds);
        ShieldIsOneTimeUse = BooleanOptionItem.Create(Id + 12, "ShieldIsOneTimeUse", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Protector]);
        Options.OverrideTasksData.Create(Id + 13, TabGroup.CrewmateRoles, CustomRoles.Protector);
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = MaxShields.GetInt();
        TimeStamp = 0;

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    private void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(playerId);
        writer.Write(TimeStamp.ToString());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte PlayerId = reader.ReadByte();
        string Time = reader.ReadString();
        TimeStamp = long.Parse(Time);
    }
    private bool ProtectorInProtect(byte playerId) => TimeStamp > Utils.GetTimeStamp();

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.IsAlive())
            if (AbilityLimit >= 1)
            {
                AbilityLimit--;
                TimeStamp = Utils.GetTimeStamp() + (long)ShieldDuration.GetFloat();
                SendRPC(player.PlayerId);
                player.Notify(GetString("ProtectorInProtect"));
            }
        return true;
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (ProtectorInProtect(target.PlayerId))
        {
            killer.RpcGuardAndKill(target);
            if (!DisableShieldAnimations.GetBool()) target.RpcGuardAndKill();
            target.Notify(GetString("ProtectorShield"));
            if (ShieldIsOneTimeUse.GetBool())
            {
                TimeStamp = 0;
                Logger.Info($"{target.GetNameWithRole()} shield broken", "ProtectorShieldBroken");
            }
            return false;
        }
        else if (killer.GetCustomRole() == target.GetCustomRole()) return false;
        return true;
    }
    public override void OnFixedUpdateLowLoad(PlayerControl pc)
    {
        if (TimeStamp < Utils.GetTimeStamp() && TimeStamp != 0)
        {
            TimeStamp = 0;
            pc.Notify(GetString("ProtectorShieldBrokenOrEnded"), sendInLog: false);
        }
    }
    public override string GetLowerText(PlayerControl pc, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (pc == null || isForMeeting || !isForHud || !pc.IsAlive()) return string.Empty;

        var str = new StringBuilder();
        if (ProtectorInProtect(pc.PlayerId))
        {
            var remainTime = TimeStamp - Utils.GetTimeStamp();
            str.Append(string.Format(GetString("ProtectorSkillTimeRemain"), remainTime));
        }
        else
        {
            str.Append(GetString("ProtectorSkillNotice"));
        }
        return str.ToString();
    }
}
