using Hazel;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class Consigliere : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 3100;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem DivinationMaxCount;

    private static readonly Dictionary<byte, int> DivinationCount = [];
    private static readonly Dictionary<byte, HashSet<byte>> DivinationTarget = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Consigliere);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Consigliere])
            .SetValueFormat(OptionFormat.Seconds);
        DivinationMaxCount = IntegerOptionItem.Create(Id + 11, "ConsigliereDivinationMaxCount", new(1, 15, 1), 5, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Consigliere])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        DivinationCount.Clear();
        DivinationTarget.Clear();

    }
    public override void Add(byte playerId)
    {
        DivinationCount.TryAdd(playerId, DivinationMaxCount.GetInt());
        DivinationTarget.TryAdd(playerId, []);


        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }

    private static void SendRPC(byte playerId, byte targetId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetConsigliere, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(DivinationCount[playerId]);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        {
            if (DivinationCount.ContainsKey(playerId))
                DivinationCount[playerId] = reader.ReadInt32();
            else
                DivinationCount.Add(playerId, DivinationMaxCount.GetInt());
        }
        {
            if (DivinationCount.ContainsKey(playerId))
                DivinationTarget[playerId].Add(reader.ReadByte());
            else
                DivinationTarget.Add(playerId, []);
        }
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (DivinationCount[killer.PlayerId] > 0)
        {
            return killer.CheckDoubleTrigger(target, () => { SetDivination(killer, target); });
        }
        else return true;
    }

    private static bool IsDivination(byte seer, byte target)
    {
        if (DivinationTarget[seer].Contains(target))
        {
            return true;
        }
        return false;
    }
    private static void SetDivination(PlayerControl killer, PlayerControl target)
    {
        if (!IsDivination(killer.PlayerId, target.PlayerId))
        {
            DivinationCount[killer.PlayerId]--;
            DivinationTarget[killer.PlayerId].Add(target.PlayerId);
            Logger.Info($"{killer.GetNameWithRole()}：Checked→{target.GetNameWithRole()} || Remaining Ability: {DivinationCount[killer.PlayerId]}", "Consigliere");
            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);

            SendRPC(killer.PlayerId, target.PlayerId);
            killer.SetKillCooldown(target: target, forceAnime: true);
        }
    }
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
    {
        var IsWatch = false;
        DivinationTarget.Do(x =>
        {
            if (x.Value != null && seer.PlayerId == x.Key && x.Value.Contains(target.PlayerId) && Utils.GetPlayerById(x.Key).IsAlive())
                IsWatch = true;
        });
        return IsWatch;
    }
    public override string GetProgressText(byte playerId, bool comms)
        => Utils.ColorString(DivinationCount[playerId] > 0 ? Utils.GetRoleColor(CustomRoles.Consigliere).ShadeColor(0.25f) : Color.gray, DivinationCount.TryGetValue(playerId, out var shotLimit) ? $"({shotLimit})" : "Invalid");
}
