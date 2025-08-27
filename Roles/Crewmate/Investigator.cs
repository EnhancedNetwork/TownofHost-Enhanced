using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Roles.Coven;
using TOHE.Roles.Neutral;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class Investigator : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Investigator;
    private const int Id = 24900;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem InvestigateCooldown;
    private static OptionItem InvestigateMax;
    private static OptionItem InvestigateRoundMax;

    private static readonly Dictionary<byte, int> RoundInvestigateLimit = [];
    private static readonly Dictionary<byte, HashSet<byte>> InvestigatedList = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Investigator, 1, zeroOne: false);
        InvestigateCooldown = FloatOptionItem.Create(Id + 10, "InvestigateCooldown", new(0f, 180f, 2.5f), 27.5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Investigator])
            .SetValueFormat(OptionFormat.Seconds);
        InvestigateMax = IntegerOptionItem.Create(Id + 11, "InvestigateMax", new(1, 15, 1), 5, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Investigator])
            .SetValueFormat(OptionFormat.Times);
        InvestigateRoundMax = IntegerOptionItem.Create(Id + 12, "InvestigateRoundMax", new(1, 15, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Investigator])
            .SetValueFormat(OptionFormat.Times);
    }

    public override void Init()
    {
        InvestigatedList.Clear();
        RoundInvestigateLimit.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(InvestigateMax.GetInt());
        RoundInvestigateLimit[playerId] = InvestigateRoundMax.GetInt();
        InvestigatedList[playerId] = [];
    }
    public override void Remove(byte playerId)
    {
        RoundInvestigateLimit.Remove(playerId);
        InvestigatedList.Remove(playerId);
    }

    private static void SendRPC(bool setTarget, byte playerId = byte.MaxValue, byte targetId = byte.MaxValue)
    {
        var msg = new RpcSetInvestigatorLimit(PlayerControl.LocalPlayer.NetId, setTarget, playerId, targetId);
        RpcUtils.LateBroadcastReliableMessage(msg);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        var setTarget = reader.ReadBoolean();
        byte investigatorID = reader.ReadByte();
        byte targetId = reader.ReadByte();

        if (setTarget)
            InvestigatedList[investigatorID].Add(targetId);
        else
            InvestigatedList[investigatorID] = [];
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetVision(false);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = InvestigateCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl player)
    {
        byte pid = player.PlayerId;
        return pid.GetAbilityUseLimit() >= 1 && RoundInvestigateLimit[pid] >= 1;
    }
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;

        if (killer.GetAbilityUseLimit() < 1 || RoundInvestigateLimit[killer.PlayerId] < 1) return false;

        killer.RpcRemoveAbilityUse();
        RoundInvestigateLimit[killer.PlayerId]--;
        InvestigatedList[killer.PlayerId].Add(target.PlayerId);

        SendRPC(setTarget: true, killer.PlayerId, target.PlayerId);
        Utils.NotifyRoles(SpecifySeer: killer, ForceLoop: true);

        killer.ResetKillCooldown();
        killer.SetKillCooldown();

        if (!DisableShieldAnimations.GetBool())
            killer.RpcGuardAndKill(target);

        return false;
    }
    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target)
    {
        if (seer == null || target == null) return string.Empty;
        if (!InvestigatedList.TryGetValue(seer.PlayerId, out var targetList)) return string.Empty;
        if (!targetList.Contains(target.PlayerId)) return string.Empty;

        if (Lich.IsCursed(target)) return "#FF1919";
        if (Illusionist.IsCovIllusioned(target.PlayerId)) return "#8CFFFF";
        if (Illusionist.IsNonCovIllusioned(target.PlayerId) || target.HasKillButton() || CopyCat.playerIdList.Contains(target.PlayerId)) return "#FF1919";
        else return "#8CFFFF";
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        foreach (var playerid in RoundInvestigateLimit.Keys)
        {
            RoundInvestigateLimit[playerid] = InvestigateRoundMax.GetInt();
            InvestigatedList[playerid] = [];
        }
        SendRPC(setTarget: false);
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(Translator.GetString("InvestigatorButtonText"));
    }
}
