using Hazel;
using System.Collections.Generic;
using TOHE.Roles.Impostor;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;
public static class PlagueBearer
{
    private static readonly int Id = 26000;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;
    public static Dictionary<byte, List<byte>> PlaguedList = new();
    public static Dictionary<byte, float> PlagueBearerCD = new();
    public static Dictionary<byte, int> PestilenceCD = new();
    public static List<byte> PestilenceList = new();

    public static OptionItem PlagueBearerCDOpt;
    public static OptionItem PestilenceCDOpt;
    public static OptionItem PestilenceCanVent;
    public static OptionItem PestilenceHasImpostorVision;


    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.PlagueBearer , 1, zeroOne: false);
        PlagueBearerCDOpt = FloatOptionItem.Create(Id + 10, "PlagueBearerCD", new(0f, 180f, 2.5f), 22.5f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.PlagueBearer])
                .SetValueFormat(OptionFormat.Seconds);
        PestilenceCDOpt = FloatOptionItem.Create(Id + 11, "PestilenceCD", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.PlagueBearer])
                .SetValueFormat(OptionFormat.Seconds);
        PestilenceCanVent = BooleanOptionItem.Create(Id + 12, "PestilenceCanVent", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.PlagueBearer]);
        PestilenceHasImpostorVision = BooleanOptionItem.Create(Id + 13, "PestilenceHasImpostorVision", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.PlagueBearer]);
    }

    public static void Init()
    {
        playerIdList = new();
        PlaguedList = new();
        PlagueBearerCD = new();
        PestilenceList = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        PlagueBearerCD.Add(playerId, PlagueBearerCDOpt.GetFloat());
        PlaguedList[playerId] = new();
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = PlagueBearerCD[id];
    public static void SetKillCooldownPestilence(byte id) => Main.AllPlayerKillCooldown[id] = PestilenceCDOpt.GetFloat();

    public static bool isPlagued(byte pc, byte target)
    {
        return PlaguedList[pc].Contains(target);
    }
    public static void SendRPC(PlayerControl player, PlayerControl target)
    {
        MessageWriter writer;
        writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.setPlaguedPlayer, SendOption.Reliable, -1);//RPCによる同期
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void receiveRPC(MessageReader reader)

    {
        byte PlagueBearerId = reader.ReadByte();
        byte PlaguedId = reader.ReadByte();
        PlaguedList[PlagueBearerId].Add(PlaguedId);
    }
    public static (int, int) PlaguedPlayerCount(byte playerId)
    {
        int plagued = 0, all = 0; //学校で習った書き方
                                  //多分この方がMain.isDousedでforeachするより他のアーソニストの分ループ数少なくて済む
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.PlayerId == playerId) continue; //塗れない人は除外 (死んでたり切断済みだったり あとアーソニスト自身も)

            all++;
            if (isPlagued(playerId, pc.PlayerId))
                //塗れている場合
                plagued++;
        }
        return (plagued, all);
    }

    public static bool IsPlaguedAll(PlayerControl player)
    {
        if (!player.Is(CustomRoles.PlagueBearer)) return false;
        
        var (countItem1, countItem2) = PlaguedPlayerCount(player.PlayerId);
        return countItem1 >= countItem2;
    }

    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (isPlagued(killer.PlayerId, target.PlayerId))
        {
            killer.Notify(GetString("PlagueBearerAlreadyPlagued"));
            return false;
        }
        PlaguedList[killer.PlayerId].Add(target.PlayerId);
        Utils.NotifyRoles(SpecifySeer: killer);
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        Logger.Msg($"kill cooldown {PlagueBearerCD[killer.PlayerId]}", "PlagueBearer");
        return false;
    }

    public static bool IsIndirectKill(PlayerControl killer)
    {
        return Puppeteer.PuppeteerList.ContainsKey(killer.PlayerId) ||
            NWitch.TaglockedList.ContainsKey(killer.PlayerId) ||
            Shroud.ShroudList.ContainsKey(killer.PlayerId) ||
            Main.CursedPlayers.ContainsValue(killer) ||
            Sniper.snipeTarget.ContainsValue(killer.PlayerId);
    }

    public static bool OnCheckMurderPestilence(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (!PestilenceList.Contains(target.PlayerId)) return false;
        if (target.Is(CustomRoles.Guardian) && target.AllTasksCompleted()) return true;
        if (target.Is(CustomRoles.Opportunist) && target.AllTasksCompleted()) return true;
        if (target.Is(CustomRoles.Veteran) && Main.VeteranInProtect.ContainsKey(target.PlayerId)) return true;
        if (target.Is(CustomRoles.TimeMaster) && Main.TimeMasterInProtect.ContainsKey(target.PlayerId)) return true;
        if (IsIndirectKill(killer)) return false;
        killer.SetRealKiller(target);
        target.RpcMurderPlayerV3(killer);
        return true;
    }
    public static void OnFixedUpdate(PlayerControl player)
    {
        if (!IsPlaguedAll(player)) return;

        player.RpcSetCustomRole(CustomRoles.Pestilence);
        player.Notify(GetString("PlagueBearerToPestilence"));
        player.RpcGuardAndKill(player);

        var playerId = player.PlayerId;

        if (!PestilenceList.Contains(playerId))
            PestilenceList.Add(playerId);

        SetKillCooldownPestilence(playerId);
        playerIdList.Remove(playerId);

    }
}
