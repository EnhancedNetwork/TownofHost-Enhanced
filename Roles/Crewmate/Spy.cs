using Hazel;
using InnerNet;
using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Spy : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Spy;
    private const int Id = 9700;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem SpyRedNameDur;
    private static OptionItem UseLimitOpt;
    private static OptionItem SpyInteractionBlocked;

    private readonly Dictionary<byte, long> SpyRedNameList = [];
    private bool change = false;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Spy);
        UseLimitOpt = IntegerOptionItem.Create(Id + 10, "AbilityUseLimit", new(1, 20, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spy])
            .SetValueFormat(OptionFormat.Times);
        SpyRedNameDur = FloatOptionItem.Create(Id + 11, "SpyRedNameDur", new(0f, 70f, 1f), 3f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spy])
            .SetValueFormat(OptionFormat.Seconds);
        SpyAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 12, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 0.5f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Spy])
            .SetValueFormat(OptionFormat.Times);
        SpyInteractionBlocked = BooleanOptionItem.Create(Id + 13, "SpyInteractionBlocked", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spy]).SetHidden(true);
        OverrideTasksData.Create(Id + 14, TabGroup.CrewmateRoles, CustomRoles.Spy);
    }
    public override void Init()
    {
        SpyRedNameList.Clear();
        change = false;
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(UseLimitOpt.GetInt());

        if (!SpyInteractionBlocked.GetBool())
        {
            SpyInteractionBlocked.SetValue(1, false);
        }
    }
    public void SendRPC(byte susId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write((byte)1);
        writer.Write(susId);
        writer.Write(SpyRedNameList[susId].ToString());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public void SendRPC(byte susId, bool changeColor)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write((byte)2);
        writer.Write(susId);
        writer.Write(changeColor);
        Logger.Info($"RPC to remove player {susId} from red name list and change `change` to {changeColor}", "Spy");
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl player)
    {
        bool isRemove = reader.ReadByte() == 2;
        if (isRemove)
        {
            SpyRedNameList.Remove(reader.ReadByte());
            change = reader.ReadBoolean();
            return;
        }
        byte susId = reader.ReadByte();
        string stimeStamp = reader.ReadString();
        if (long.TryParse(stimeStamp, out long timeStamp)) SpyRedNameList[susId] = timeStamp;
    }
    private bool OnKillAttempt(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (killer.PlayerId == target.PlayerId) return true;

        if (killer.GetAbilityUseLimit() > 0 && killer.IsAlive())
        {
            killer.RpcRemoveAbilityUse();

            SpyRedNameList[killer.PlayerId] = GetTimeStamp();
            SendRPC(killer.PlayerId);

            if (SpyInteractionBlocked.GetBool())
            {
                killer.SetKillCooldown(time: 10f, target, forceAnime: true);
                NotifyRoles(SpecifySeer: target, ForceLoop: true);
                killer.ResetKillCooldown();
                killer.SyncSettings();
                return false;
            }
        }
        return true;
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
        => OnKillAttempt(killer, target);

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (lowLoad || !SpyRedNameList.Any()) return;

        change = false;
        foreach (var x in SpyRedNameList)
        {
            if (x.Value + SpyRedNameDur.GetInt() < nowTime)
            {
                if (SpyRedNameList.ContainsKey(x.Key))
                {
                    SpyRedNameList.Remove(x.Key);
                    change = true;
                    SendRPC(x.Key, change);
                }
            }
        }
        if (change) { NotifyRoles(SpecifySeer: player, ForceLoop: true); }
    }
    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target) => (seer.Is(CustomRoles.Spy) && SpyRedNameList.ContainsKey(target.PlayerId)) ? "#BA4A00" : "";
}
