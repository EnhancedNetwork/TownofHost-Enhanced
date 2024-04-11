using AmongUs.GameOptions;
using Hazel;
using TOHE.Roles.Core;
using TOHE.Roles.Impostor;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace TOHE.Roles.Neutral;

internal class Baker : RoleBase
{
    //===========================SETUP================================\\
    private static readonly int Id = 28200;
    public static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    //==================================================================\\

    private static OptionItem BreadNeededToTransform;

    private static readonly Dictionary<byte, List<byte>> BreadList = [];
    private static bool CanUseAbility;

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Baker, 1, zeroOne: false);
        BreadNeededToTransform = IntegerOptionItem.Create(Id + 10, "BakerBreadNeededToTransform", new(1, 5, 1), 3, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Baker])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        playerIdList.Clear();
        BreadList.Clear();
        CanUseAbility = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        BreadList[playerId] = [];
        CanUseAbility = true;
        CustomRoleManager.CheckDeadBodyOthers.Add(OnPlayerDead);
    }

    private static (int, int) BreadedPlayerCount(byte playerId)
    {
        int breaded = 0, all = BreadNeededToTransform.GetInt();
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.PlayerId == playerId) continue;

            if (HasBread(playerId, pc.PlayerId))
                breaded++;
        }
        return (breaded, all);
    }
    public static void SendRPC(PlayerControl player, PlayerControl target)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Baker);
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte BakerId = reader.ReadByte();
        byte BreadHolderId = reader.ReadByte();
        BreadList[BakerId].Add(BreadHolderId);
    }
    public override string GetProgressText(byte playerId, bool comms) => CustomRoles.Baker.RoleExist() ? ColorString(GetRoleColor(CustomRoles.Baker).ShadeColor(0.25f), $"({BreadedPlayerCount(playerId).Item1}/{BreadedPlayerCount(playerId).Item2})") : "";
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => KnowRoleTarget(seer, target);
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
        => (target.IsNeutralApocalypse() && seer.IsNeutralApocalypse());
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        => BreadList[seer.PlayerId].Contains(seen.PlayerId) ? $"<color={GetRoleColorCode(seer.GetCustomRole())}>●</color>" : "";
    public override bool CanUseKillButton(PlayerControl pc) => pc.IsAlive();
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("BakerKillButtonText"));
    }
    private static bool HasBread(byte pc, byte target)
    {
        return BreadList[pc].Contains(target);
    }
    private static bool AllHasBread(PlayerControl player)
    {
        if (!player.Is(CustomRoles.Baker)) return false;

        var (countItem1, countItem2) = BreadedPlayerCount(player.PlayerId);
        return countItem1 >= countItem2;
    }

    public override void OnReportDeadBody(PlayerControl marg, PlayerControl iscute)
    {
        CanUseAbility = true;
    }
    private void OnPlayerDead(PlayerControl killer, PlayerControl deadPlayer, bool inMeeting)
    {
        foreach (var playerId in BreadList.Keys.ToArray())
        {
            if (deadPlayer.PlayerId == playerId)
            {
                BreadList[playerId].Remove(playerId);
            }
        }
    }
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!CanUseAbility)
        {
            killer.Notify(GetString("BakerBreadUsedAlready"));
            return false;
        }
        if (target.IsNeutralApocalypse())
        {
            killer.Notify(GetString("BakerCantBreadApoc"));
            return false;
        }
        if (HasBread(killer.PlayerId, target.PlayerId))
        {
            killer.Notify(GetString("BakerAlreadyBreaded"));
            return false;
        }
        BreadList[killer.PlayerId].Add(target.PlayerId);
        SendRPC(killer, target);
        Utils.NotifyRoles(SpecifySeer: killer);
        killer.Notify(GetString("BakerBreaded"));
        Logger.Info($"Bread given to " + target.GetRealName(), "Baker");
        CanUseAbility = false;
        return false;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AllHasBread(player) || player.Is(CustomRoles.Famine)) return;

        player.RpcSetCustomRole(CustomRoles.Famine);
        player.Notify(GetString("BakerToFamine"));
        player.RpcGuardAndKill(player);
        KillIfNotEjected(player);

    }
    public static void KillIfNotEjected(PlayerControl player)
    {
        var deathList = new List<byte>();
        var baker = player.PlayerId;
        if (Main.AfterMeetingDeathPlayers.ContainsKey(baker)) return;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            var notBaker = pc.PlayerId;
            if (pc.IsNeutralApocalypse() || HasBread(baker, notBaker)) continue;
            if (player != null && player.IsAlive())
            {
                if (!Main.AfterMeetingDeathPlayers.ContainsKey(pc.PlayerId))
                {
                    pc.SetRealKiller(player);
                    deathList.Add(pc.PlayerId);
                }
                else
                {
                    Main.AfterMeetingDeathPlayers.Remove(pc.PlayerId);
                }
            }
            else return;
        }
        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Starved, [.. deathList]);
    }
}
