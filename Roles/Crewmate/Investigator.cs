using AmongUs.GameOptions;
using Hazel;
using TOHE.Roles.Coven;
using UnityEngine;
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

    private static readonly Dictionary<byte, int> MaxInvestigateLimit = [];
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
        MaxInvestigateLimit.Clear();
        RoundInvestigateLimit.Clear();
    }
    public override void Add(byte playerId)
    {
        MaxInvestigateLimit[playerId] = InvestigateMax.GetInt();
        RoundInvestigateLimit[playerId] = InvestigateRoundMax.GetInt();
        InvestigatedList[playerId] = [];
    }
    public override void Remove(byte playerId)
    {
        MaxInvestigateLimit.Remove(playerId);
        RoundInvestigateLimit.Remove(playerId);
        InvestigatedList.Remove(playerId);
    }

    private static void SendRPC(int operate, byte playerId = byte.MaxValue, byte targetId = byte.MaxValue)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetInvestgatorLimit, SendOption.Reliable, -1);
        writer.Write(operate);
        if (operate == 0)
        {
            writer.Write(playerId);
            writer.Write(targetId);
            writer.Write(MaxInvestigateLimit[playerId]);
            writer.Write(RoundInvestigateLimit[playerId]);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        int operate = reader.ReadInt32();
        if (operate == 0)
        {
            byte investigatorID = reader.ReadByte();
            byte targetID = reader.ReadByte();
            if (!InvestigatedList.ContainsKey(investigatorID)) InvestigatedList[investigatorID] = [];
            InvestigatedList[investigatorID].Add(targetID);

            int maxLimit = reader.ReadInt32();
            MaxInvestigateLimit[investigatorID] = maxLimit;

            int roundLimit = reader.ReadInt32();
            MaxInvestigateLimit[investigatorID] = roundLimit;
        }
        if (operate == 1)
        {
            foreach (var playerid in RoundInvestigateLimit.Keys)
            {
                RoundInvestigateLimit[playerid] = InvestigateRoundMax.GetInt();
                InvestigatedList[playerid] = [];
            }
        }
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetVision(false);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = InvestigateCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl player)
    {
        if (player == null) return false;
        byte pid = player.PlayerId;
        if (!MaxInvestigateLimit.ContainsKey(pid)) MaxInvestigateLimit[pid] = InvestigateMax.GetInt();
        if (!RoundInvestigateLimit.ContainsKey(pid)) RoundInvestigateLimit[pid] = InvestigateRoundMax.GetInt();
        return !player.Data.IsDead && MaxInvestigateLimit[player.PlayerId] >= 1 && RoundInvestigateLimit[player.PlayerId] >= 1;
    }
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;

        if (!MaxInvestigateLimit.ContainsKey(killer.PlayerId)) MaxInvestigateLimit[killer.PlayerId] = InvestigateMax.GetInt();
        if (!RoundInvestigateLimit.ContainsKey(killer.PlayerId)) RoundInvestigateLimit[killer.PlayerId] = InvestigateRoundMax.GetInt();

        if (MaxInvestigateLimit[killer.PlayerId] < 1 || RoundInvestigateLimit[killer.PlayerId] < 1) return false;
        if (target.Is(CustomRoles.Stubborn))
        {
            killer.Notify(Translator.GetString("StubbornNotify"));
            return true;
        }

        MaxInvestigateLimit[killer.PlayerId]--;
        RoundInvestigateLimit[killer.PlayerId]--;
        if (!InvestigatedList.ContainsKey(killer.PlayerId)) InvestigatedList[killer.PlayerId] = [];
        InvestigatedList[killer.PlayerId].Add(target.PlayerId);

        SendRPC(operate: 0, playerId: killer.PlayerId, targetId: target.PlayerId);
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
        SendRPC(1);
    }
    public override string GetProgressText(byte playerId, bool comms) => Utils.ColorString(MaxInvestigateLimit[playerId] >= 1 ? Utils.GetRoleColor(CustomRoles.Investigator).ShadeColor(0.25f) : Color.gray, $"({MaxInvestigateLimit[playerId]})");

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(Translator.GetString("InvestigatorButtonText")); ;
    }
}
