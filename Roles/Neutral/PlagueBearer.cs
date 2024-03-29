using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using TOHE.Roles.Impostor;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral;

internal class PlagueBearer : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 17600;
    public static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    //==================================================================\\

    private static OptionItem PlagueBearerCDOpt;
    private static OptionItem PestilenceCDOpt;
    private static OptionItem PestilenceCanVent;
    private static OptionItem PestilenceHasImpostorVision;

    private static readonly Dictionary<byte, HashSet<byte>> PlaguedList = [];
    private static readonly Dictionary<byte, float> PlagueBearerCD = [];
    //private static readonly Dictionary<byte, int> PestilenceCD = [];
    private static readonly HashSet<byte> PestilenceList = [];

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.PlagueBearer, 1, zeroOne: false);
        PlagueBearerCDOpt = FloatOptionItem.Create(Id + 10, "PlagueBearerCD", new(0f, 180f, 2.5f), 22.5f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.PlagueBearer])
                .SetValueFormat(OptionFormat.Seconds);
        PestilenceCDOpt = FloatOptionItem.Create(Id + 11, "PestilenceCD", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.PlagueBearer])
                .SetValueFormat(OptionFormat.Seconds);
        PestilenceCanVent = BooleanOptionItem.Create(Id + 12, "PestilenceCanVent", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.PlagueBearer]);
        PestilenceHasImpostorVision = BooleanOptionItem.Create(Id + 13, "PestilenceHasImpostorVision", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.PlagueBearer]);
    }

    public override void Init()
    {
        playerIdList.Clear();
        PlaguedList.Clear();
        PlagueBearerCD.Clear();
        PestilenceList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        PlagueBearerCD.Add(playerId, PlagueBearerCDOpt.GetFloat());
        PlaguedList[playerId] = [];

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    public override void SetKillCooldown(byte id)
    {
        if (!PestilenceList.Contains(id))
            Main.AllPlayerKillCooldown[id] = PlagueBearerCD[id];
        else
            Main.AllPlayerKillCooldown[id] = PestilenceCDOpt.GetFloat();
    }

    private static bool IsPlagued(byte pc, byte target)
    {
        return PlaguedList[pc].Contains(target);
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        => PlaguedList[seer.PlayerId].Contains(seen.PlayerId) ? $"<color={GetRoleColorCode(seer.GetCustomRole())}>●</color>" : "";
    
    public static void SendRPC(PlayerControl player, PlayerControl target)
    {
        MessageWriter writer;
        writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);//RPCによる同期
        writer.WritePacked((int)CustomRoles.PlagueBearer); // setPlaguedPlayer
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        if (PestilenceList.Contains(playerId))
            opt.SetVision(PestilenceHasImpostorVision.GetBool());
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl pc, CustomRoles role, ref bool guesserSuicide)
    {
        if (target.Is(CustomRoles.Pestilence))
        {
            if (!isUI) SendMessage(GetString("GuessPestilence"), pc.PlayerId);
            else pc.ShowPopUp(GetString("GuessPestilence"));
            guesserSuicide = true;
            Logger.Msg($"Is Active: {guesserSuicide}", "guesserSuicide - Pestilence");
        }
        return false;
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte PlagueBearerId = reader.ReadByte();
        byte PlaguedId = reader.ReadByte();
        PlaguedList[PlagueBearerId].Add(PlaguedId);
    }
    private static (int, int) PlaguedPlayerCount(byte playerId)
    {
        int plagued = 0, all = 0;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.PlayerId == playerId) continue;

            all++;
            if (IsPlagued(playerId, pc.PlayerId))
                plagued++;
        }
        return (plagued, all);
    }
    public static void PlaguerNotify(PlayerControl seer)
    {
        if (IsPlaguedAll(seer))
        {
            seer.RpcSetCustomRole(CustomRoles.Pestilence);
            seer.Notify(GetString("PlagueBearerToPestilence"));
            seer.RpcGuardAndKill(seer);
            if (!PestilenceList.Contains(seer.PlayerId))
                    PestilenceList.Add(seer.PlayerId);
            seer.ResetKillCooldown();
            playerIdList.Remove(seer.PlayerId);
        }
    }
    private static bool IsPlaguedAll(PlayerControl player)
    {
        if (!player.Is(CustomRoles.PlagueBearer)) return false;
        
        var (countItem1, countItem2) = PlaguedPlayerCount(player.PlayerId);
        return countItem1 >= countItem2;
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetCustomRole() == CustomRoles.Pestilence) return true;

        if (IsPlagued(killer.PlayerId, target.PlayerId))
        {
            killer.Notify(GetString("PlagueBearerAlreadyPlagued"));
            return false;
        }
        PlaguedList[killer.PlayerId].Add(target.PlayerId);
        SendRPC(killer, target);
        NotifyRoles(SpecifySeer: killer);

        killer.ResetKillCooldown();
        killer.SetKillCooldown();

        Logger.Info($"kill cooldown {PlagueBearerCD[killer.PlayerId]}", "PlagueBearer");
        return false;
    }

    public override string GetProgressText(byte playerId, bool comms)
    {
        var plagued = PlaguedPlayerCount(playerId);
        return !PestilenceList.Contains(playerId) ? ColorString(GetRoleColor(CustomRoles.PlagueBearer).ShadeColor(0.25f), $"({plagued.Item1}/{plagued.Item2})") : "";
    }
    private static bool IsIndirectKill(PlayerControl killer)
    {
        return Puppeteer.PuppetIsActive(killer.PlayerId) ||
            Shroud.ShroudIsActive(killer.PlayerId) ||
            Warlock.CursedIsActive(killer) ||
            Sniper.SnipeIsActive(killer.PlayerId);
    }
    public override bool CanUseImpostorVentButton(PlayerControl pc)
        => pc.Is(CustomRoles.Pestilence) && PestilenceCanVent.GetBool();
    public override bool CanUseKillButton(PlayerControl pc) => pc.IsAlive();
    
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (!PestilenceList.Contains(target.PlayerId)) return false;

        killer.SetRealKiller(target);
        target.RpcMurderPlayer(killer);
        return true;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("InfectiousKillButtonText"));
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!IsPlaguedAll(player) || player.Is(CustomRoles.Pestilence)) return;

        player.RpcSetCustomRole(CustomRoles.Pestilence);
        player.Notify(GetString("PlagueBearerToPestilence"));
        player.RpcGuardAndKill(player);

        var playerId = player.PlayerId;

        if (!PestilenceList.Contains(playerId))
            PestilenceList.Add(playerId);

        player.ResetKillCooldown();
        playerIdList.Remove(playerId);
    }
}
