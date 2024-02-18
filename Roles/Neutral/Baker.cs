using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Impostor;
using static TOHE.Options;
using static TOHE.Translator;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace TOHE.Roles.Neutral;
public static class Baker
{
    private static readonly int Id = 27800;
    public static List<byte> playerIdList = [];
    public static bool IsEnable = false;
    public static Dictionary<byte, List<byte>> BreadList = [];
    public static bool CanUseAbility;

    public static OptionItem BreadNeededToTransform;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Baker);
        BreadNeededToTransform = IntegerOptionItem.Create(Id + 10, "BakerBreadNeededToTransform", new(1, 5, 1), 3, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Baker])
            .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = [];
        BreadList = [];
        IsEnable = false;
        CanUseAbility = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        BreadList[playerId] = [];
        IsEnable = true;
        CanUseAbility = true;
    }

    public static void SendRPC(PlayerControl player, PlayerControl target)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetBreadedPlayer, SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte BakerId = reader.ReadByte();
        byte BreadHolderId = reader.ReadByte();
        BreadList[BakerId].Add(BreadHolderId);
    }
    public static string GetProgressText(byte playerId) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.Baker).ShadeColor(0.25f),  $"({BreadedPlayerCount(playerId).Item1}/{BreadedPlayerCount(playerId).Item2})");
    public static bool HasBread(byte pc, byte target)
    {
        return BreadList[pc].Contains(target);  
    }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
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

        Logger.Info($"Bread given to "+target.GetRealName(), "Baker");
        CanUseAbility = false;
        return false;
    }
    public static void OnReportDeadBody()
    {
        CanUseAbility = true;
    }
    public static void OnPlayerDead(PlayerControl deadPlayer)
    {
        foreach (var playerId in BreadList.Keys.ToArray())
        {
            if (deadPlayer.PlayerId == playerId)
            {
                BreadList[playerId].Remove(playerId);
            }
        }
    }
    public static (int, int) BreadedPlayerCount(byte playerId)
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
    public static bool AllHasBread(PlayerControl player)
    {
        if (!player.Is(CustomRoles.Baker)) return false;

        var (countItem1, countItem2) = BreadedPlayerCount(player.PlayerId);
        return countItem1 >= countItem2;
    }
    public static void OnFixedUpdate(PlayerControl player)
    {
        if (!AllHasBread(player)) return;

        player.RpcSetCustomRole(CustomRoles.Famine);
        player.Notify(GetString("BakerToFamine"));
        player.RpcGuardAndKill(player);
        KillIfNotEjected(player);

    }
    public static void KillIfNotEjected(PlayerControl player)
    {
        var deathList = new List<byte>();
        var baker = player.PlayerId;
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
        }
        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Starved, [.. deathList]);
    }
}

