using Hazel;
using Il2CppSystem;
using InnerNet;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;


namespace TOHE.Roles.Neutral;

internal class Follower : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Follower;
    private const int Id = 12800;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    private static OptionItem MaxBetTimes;
    private static OptionItem BetCooldown;
    private static OptionItem BetCooldownIncrese;
    private static OptionItem MaxBetCooldown;
    private static OptionItem KnowTargetRole;
    private static OptionItem BetTargetKnowFollower;

    private static readonly Dictionary<byte, int> BetTimes = [];
    public static readonly Dictionary<byte, byte> BetPlayer = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Follower, 1, zeroOne: false);
        MaxBetTimes = IntegerOptionItem.Create(Id + 10, "FollowerMaxBetTimes", new(1, 20, 1), 3, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Follower])
            .SetValueFormat(OptionFormat.Times);
        BetCooldown = FloatOptionItem.Create(Id + 12, "FollowerBetCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Follower])
            .SetValueFormat(OptionFormat.Seconds);
        BetCooldownIncrese = FloatOptionItem.Create(Id + 14, "FollowerBetCooldownIncrese", new(0f, 60f, 1f), 4f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Follower])
            .SetValueFormat(OptionFormat.Seconds);
        MaxBetCooldown = FloatOptionItem.Create(Id + 16, "FollowerMaxBetCooldown", new(0f, 180f, 2.5f), 50f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Follower])
            .SetValueFormat(OptionFormat.Seconds);
        KnowTargetRole = BooleanOptionItem.Create(Id + 18, "FollowerKnowTargetRole", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Follower]);
        BetTargetKnowFollower = BooleanOptionItem.Create(Id + 20, "FollowerBetTargetKnowFollower", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Follower]);
    }
    public override void Init()
    {
        BetTimes.Clear();
        BetPlayer.Clear();
    }
    public override void Add(byte playerId)
    {
        BetTimes.Add(playerId, MaxBetTimes.GetInt());
    }
    private void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player); //SyncFollowerTargetAndTimes
        writer.Write(playerId);
        writer.Write(BetTimes.TryGetValue(playerId, out var times) ? times : MaxBetTimes.GetInt());
        writer.Write(BetPlayer.TryGetValue(playerId, out var player) ? player : byte.MaxValue);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        byte PlayerId = reader.ReadByte();
        int Times = reader.ReadInt32();
        byte Target = reader.ReadByte();
        BetTimes.Remove(PlayerId);
        BetPlayer.Remove(PlayerId);
        BetTimes.Add(PlayerId, Times);
        if (Target != byte.MaxValue)
            BetPlayer.Add(PlayerId, Target);
    }

    private static bool CanKillButton(PlayerControl player) => !player.Data.IsDead && (!BetTimes.TryGetValue(player.PlayerId, out var times) || times >= 1);
    public override bool CanUseKillButton(PlayerControl player) => CanKillButton(player);
    public override void SetKillCooldown(byte id)
    {
        if (BetTimes.TryGetValue(id, out var times) && times < 1)
        {
            Main.AllPlayerKillCooldown[id] = 300;
            return;
        }
        float cd = BetCooldown.GetFloat();
        cd += Main.AllPlayerControls.Count(x => !x.IsAlive()) * BetCooldownIncrese.GetFloat();
        cd = Math.Min(cd, MaxBetCooldown.GetFloat());
        Main.AllPlayerKillCooldown[id] = cd;
    }
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
    {
        if (!KnowTargetRole.GetBool()) return false;
        return seer.Is(CustomRoles.Follower) && BetPlayer.TryGetValue(seer.PlayerId, out var tar) && tar == target.PlayerId;
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.PlayerId == target.PlayerId) return true;
        if (BetPlayer.TryGetValue(killer.PlayerId, out var tar) && tar == target.PlayerId) return false;
        if (!BetTimes.TryGetValue(killer.PlayerId, out var times) || times < 1) return false;

        BetTimes[killer.PlayerId]--;

        if (BetPlayer.TryGetValue(killer.PlayerId, out var originalTarget) && Utils.GetPlayerById(originalTarget) != null)
        {
            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: Utils.GetPlayerById(originalTarget), ForceLoop: true);
            Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(originalTarget), SpecifyTarget: killer, ForceLoop: true);
        }

        BetPlayer.Remove(killer.PlayerId);
        BetPlayer.Add(killer.PlayerId, target.PlayerId);
        SendRPC(killer.PlayerId);

        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        killer.RPCPlayCustomSound("Bet");

        killer.Notify(GetString("FollowerBetPlayer"));

        if (BetTargetKnowFollower.GetBool())
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Follower), GetString("FollowerBetOnYou")));

        Logger.Info($" {killer.GetNameWithRole()} => {target.GetNameWithRole()}", "Follower");
        return false;
    }
    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (target == null) return string.Empty;

        if (!seer.Is(CustomRoles.Follower))
        {
            if (!BetTargetKnowFollower.GetBool()) return "";
            return (BetPlayer.TryGetValue(target.PlayerId, out var x) && seer.PlayerId == x) ?
                Utils.ColorString(Utils.GetRoleColor(CustomRoles.Follower), "âŠ") : "";
        }
        var GetValue = BetPlayer.TryGetValue(seer.PlayerId, out var targetId);
        return GetValue && targetId == target.PlayerId ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Follower), "âŠ") : "";
    }
    public override string GetProgressText(byte playerId, bool coooms)
    {
        var player = Utils.GetPlayerById(playerId);
        if (player == null) return null;
        return Utils.ColorString(CanKillButton(player) ? Utils.GetRoleColor(CustomRoles.Follower) : Color.gray, $"({(BetTimes.TryGetValue(playerId, out var times) ? times : "0")})");
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("FollowerKillButtonText"));
    }
}
