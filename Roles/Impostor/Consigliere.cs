using Hazel;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class Consigliere : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Consigliere;
    private const int Id = 3100;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem DivinationMaxCount;
    private static OptionItem ImpsCanSeeReveals;

    private static readonly Dictionary<byte, HashSet<byte>> DivinationTarget = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Consigliere);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Consigliere])
            .SetValueFormat(OptionFormat.Seconds);
        DivinationMaxCount = IntegerOptionItem.Create(Id + 11, "ConsigliereDivinationMaxCount", new(1, 15, 1), 5, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Consigliere])
            .SetValueFormat(OptionFormat.Times);
        ImpsCanSeeReveals = BooleanOptionItem.Create(Id + 12, "ConsigliereImpsCanSeeReveals", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Consigliere]);
    }
    public override void Init()
    {
        DivinationTarget.Clear();

    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(DivinationMaxCount.GetInt());
        DivinationTarget[playerId] = [];

        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }

    private static void SendRPC(byte playerId, byte targetId)
    {
        var msg = new RpcSetConsigliere(PlayerControl.LocalPlayer.NetId, playerId, targetId);
        RpcUtils.LateBroadcastReliableMessage(msg);

    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();

        if (DivinationTarget.ContainsKey(playerId))
            DivinationTarget[playerId].Add(reader.ReadByte());
        else
            DivinationTarget.Add(playerId, []);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() > 0)
        {
            return killer.CheckDoubleTrigger(target, () => { SetDivination(killer, target); });
        }
        else return true;
    }

    private static bool IsDivination(byte seerId, byte target) => DivinationTarget.TryGetValue(seerId, out var targets) && targets.Contains(target);

    private static void SetDivination(PlayerControl killer, PlayerControl target)
    {
        if (IsDivination(killer.PlayerId, target.PlayerId)) return;

        killer.RpcRemoveAbilityUse();
        DivinationTarget[killer.PlayerId].Add(target.PlayerId);

        Logger.Info($"{killer.GetNameWithRole()}：{target.GetNameWithRole()}", "Consigliere");
        Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);

        SendRPC(killer.PlayerId, target.PlayerId);
        killer.SetKillCooldown();
    }
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
        => seer.PlayerId != target.PlayerId && seer.IsAlive() && IsDivination(seer.PlayerId, target.PlayerId);

    public static bool ImpKnowRoleTarget(PlayerControl imp, PlayerControl target)
    {
        if (imp == null || !(imp.IsPlayerImpostorTeam() || imp.IsPolice())) return false;
        if (!ImpsCanSeeReveals.GetBool()) return false;
        bool result = false;
        foreach (var cs in DivinationTarget.Keys)
        {
            if (DivinationTarget[cs].Contains(target.PlayerId)) result = true;

            // when seer is Sheriff or ChiefOfPolice and Consigliere is not Narc,seer shouldn't see target's role
            if (imp.IsPolice() && !cs.GetPlayer().Is(CustomRoles.Narc)) result = false;
        }
        return result;
    }
}
