using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class PotionMaster : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 17700;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.PotionMaster);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem RitualMaxCount;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;

    private static readonly Dictionary<byte, List<byte>> RitualTarget = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.PotionMaster, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 14, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PotionMaster])
            .SetValueFormat(OptionFormat.Seconds);
        RitualMaxCount = IntegerOptionItem.Create(Id + 11, "RitualMaxCount", new(1, 15, 1), 5, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PotionMaster])
            .SetValueFormat(OptionFormat.Times);
        CanVent = BooleanOptionItem.Create(Id + 12, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PotionMaster]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PotionMaster]);
    }
    public override void Init()
    {
        RitualTarget.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(RitualMaxCount.GetInt());
        RitualTarget.TryAdd(playerId, []);

        var pc = Utils.GetPlayerById(playerId);
        pc?.AddDoubleTrigger();
    }

    private void SendRPC(byte playerId, byte targetId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(playerId);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte playerId = reader.ReadByte();

        RitualTarget[playerId].Add(reader.ReadByte()); 
    }
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override bool CanUseSabotage(PlayerControl pc) => true;

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() > 0)
        {
            return killer.CheckDoubleTrigger(target, () => { SetRitual(killer, target); });
        }
        else return true;
    }

    public static bool IsRitual(byte seer, byte target)
    {
        if (RitualTarget[seer].Contains(target))
        {
            return true;
        }
        return false;
    }
    private void SetRitual(PlayerControl killer, PlayerControl target)
    {
        if (!IsRitual(killer.PlayerId, target.PlayerId))
        {
            killer.RpcRemoveAbilityUse();
            RitualTarget[killer.PlayerId].Add(target.PlayerId);

            Utils.NotifyRoles(SpecifySeer: killer);
            SendRPC(killer.PlayerId, target.PlayerId);

            killer.SetKillCooldown();
        }
    }
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
    {
        var IsWatch = false;
        RitualTarget.Do(x =>
        {
            if (x.Value != null && seer.PlayerId == x.Key && x.Value.Contains(target.PlayerId) && Utils.GetPlayerById(x.Key).IsAlive())
                IsWatch = true;
        });
        return IsWatch;
    }
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target)
        => KnowRoleTarget(seer, target);
}