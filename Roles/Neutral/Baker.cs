using AmongUs.GameOptions;
using Hazel;
using System.Linq;
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
    private static readonly int Id = 28400;
    public static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralApocalypse;
    //==================================================================\\

    private static OptionItem BreadNeededToTransform;
    private static OptionItem FamineStarveCooldown;

    private static readonly Dictionary<byte, List<byte>> BreadList = [];
    private static readonly Dictionary<byte, List<byte>> FamineList = [];
    private static bool CanUseAbility;
    private static bool StarvedNonBreaded;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Baker, 1, zeroOne: false);
        BreadNeededToTransform = IntegerOptionItem.Create(Id + 10, "BakerBreadNeededToTransform", new(1, 5, 1), 3, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Baker])
            .SetValueFormat(OptionFormat.Times);
        FamineStarveCooldown = FloatOptionItem.Create(Id + 11, "FamineStarveCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Baker])
                .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        playerIdList.Clear();
        BreadList.Clear();
        FamineList.Clear();
        CanUseAbility = false;
        StarvedNonBreaded = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        BreadList[playerId] = [];
        FamineList[playerId] = [];
        CanUseAbility = true;
        StarvedNonBreaded = false;
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
    {
        if (seer.GetCustomRole() is CustomRoles.Baker || !StarvedNonBreaded)
            return BreadList[seer.PlayerId].Contains(seen.PlayerId) ? $"<color={GetRoleColorCode(seer.GetCustomRole())}>●</color>" : "";
        else
            return FamineList[seer.PlayerId].Contains(seen.PlayerId) ? $"<color={GetRoleColorCode(seer.GetCustomRole())}>●</color>" : "";
    }
    public override bool CanUseKillButton(PlayerControl pc) => pc.IsAlive();
    public override void SetKillCooldown(byte id)
    {
        PlayerControl baker = Utils.GetPlayerById(playerIdList.First());
        if (baker.GetCustomRole() is CustomRoles.Famine)
            Main.AllPlayerKillCooldown[id] = FamineStarveCooldown.GetFloat();
        else
            Main.AllPlayerKillCooldown[id] = Main.AllPlayerKillCooldown[id];
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (CustomRoles.Baker.RoleExist())
            hud.KillButton.OverrideText(GetString("BakerKillButtonText"));
        else if (CustomRoles.Famine.RoleExist())
            hud.KillButton.OverrideText(GetString("FamineKillButtonText"));
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
        PlayerControl baker = Utils.GetPlayerById(playerIdList.First());
        if (baker.GetCustomRole() is CustomRoles.Famine)
        {
            foreach (var pc in FamineList)
            {
                foreach (var tar in pc.Value)
                {
                    var target = Utils.GetPlayerById(tar);
                    var killer = Utils.GetPlayerById(pc.Key);
                    if (killer == null || target == null) continue;
                    target.RpcExileV2();
                    target.SetRealKiller(killer);
                    Main.PlayerStates[tar].deathReason = PlayerState.DeathReason.Starved;
                    Main.PlayerStates[tar].SetDead();
                    MurderPlayerPatch.AfterPlayerDeathTasks(killer, target, true);
                    Logger.Info($"{killer.GetRealName()} has starved {target.GetRealName()}", "Famine");
                }
            }
        }
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
        foreach (var playerId in FamineList.Keys.ToArray())
        {
            if (deadPlayer.PlayerId == playerId)
            {
                FamineList[playerId].Remove(playerId);
            }
        }
    }
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetCustomRole() == CustomRoles.Famine && !target.IsNeutralApocalypse()) {
            if (StarvedNonBreaded) { 
                FamineList[killer.PlayerId].Add(target.PlayerId);
                SendRPC(killer, target);
                Utils.NotifyRoles(SpecifySeer: killer);
                killer.Notify(GetString("FamineStarved"));
                Logger.Info(target.GetRealName() + $" has been starved", "Famine");
                CanUseAbility = true;
                return false; 
            }
        }
        if (!CanUseAbility)
        {
            killer.Notify(GetString("BakerBreadUsedAlready"));
            return false;
        }
        if (target.IsNeutralApocalypse())
        {
            if (killer.GetCustomRole() == CustomRoles.Baker)
                killer.Notify(GetString("BakerCantBreadApoc"));
            else
                killer.Notify(GetString("FamineCantStarveApoc"));
            return false;
        }
        if (HasBread(killer.PlayerId, target.PlayerId))
        {
            killer.Notify(GetString("BakerAlreadyBreaded"));
            return false;
        }
        if (FamineList[killer.PlayerId].Contains(target.PlayerId))
        {
            killer.Notify(GetString("FamineAlreadyStarved"));
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
    }
    public static void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
        if (!HasEnabled || deathReason != PlayerState.DeathReason.Vote) return;
        if (!CustomRoles.Famine.RoleExist()) return;
        if (exileIds.Contains(playerIdList.First())) return;
        if (StarvedNonBreaded) return;
        var deathList = new List<byte>();
        PlayerControl baker = Utils.GetPlayerById(playerIdList.First());
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.IsNeutralApocalypse() || HasBread(baker.PlayerId, pc.PlayerId)) continue;
            if (baker != null && baker.IsAlive())
            {
                if (!Main.AfterMeetingDeathPlayers.ContainsKey(pc.PlayerId))
                {
                    pc.SetRealKiller(baker);
                    deathList.Add(pc.PlayerId);
                }
            }
            else
            {
                Main.AfterMeetingDeathPlayers.Remove(pc.PlayerId);
            }
        }
        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Starved, [.. deathList]);
        BreadList.Clear();
        StarvedNonBreaded = true;
        CanUseAbility = true;
    }
}
