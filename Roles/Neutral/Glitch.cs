using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System.Text;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Glitch : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Glitch;
    private const int Id = 16300;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Glitch);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static readonly Dictionary<byte, long> hackedIdList = [];

    public static OptionItem KillCooldown;
    private static OptionItem HackCooldown;
    private static OptionItem HackDuration;
    private static OptionItem MimicCooldown;
    private static OptionItem MimicDuration;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;

    public int HackCDTimer;
    public int KCDTimer;
    public int MimicCDTimer;
    public int MimicDurTimer;
    public long LastHack;
    public long LastKill;
    public long LastMimic;

    public bool NotSetCD = false;
    private bool IsShifted = false;
    private long LastRpcSend = 0;

    public override void SetupCustomOption()
    {
        //GlitchÒü»1õ║║Õø║Õ«Ü
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Glitch, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 1f), 20, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Glitch])
            .SetValueFormat(OptionFormat.Seconds);
        HackCooldown = IntegerOptionItem.Create(Id + 11, "Glitch_HackCooldown", new(0, 180, 1), 20, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Glitch])
            .SetValueFormat(OptionFormat.Seconds);
        HackDuration = FloatOptionItem.Create(Id + 14, "Glitch_HackDuration", new(0f, 60f, 1f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Glitch])
            .SetValueFormat(OptionFormat.Seconds);
        MimicCooldown = IntegerOptionItem.Create(Id + 15, "Glitch_MimicCooldown", new(0, 180, 1), 15, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Glitch])
            .SetValueFormat(OptionFormat.Seconds);
        MimicDuration = FloatOptionItem.Create(Id + 16, "Glitch_MimicDuration", new(0f, 60f, 1f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Glitch])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 12, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Glitch]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Glitch]);
    }
    public override void Add(byte playerId)
    {
        HackCDTimer = 10;
        KCDTimer = 10;
        MimicCDTimer = 10;
        MimicDurTimer = 0;

        NotSetCD = false;
        IsShifted = false;

        var ts = Utils.GetTimeStamp();

        LastKill = ts;
        LastHack = ts;
        LastMimic = ts;
        LastRpcSend = ts;

        // Double Trigger
        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = 1f;
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public void Mimic(PlayerControl pc)
    {
        if (pc == null) return;
        if (!pc.IsAlive()) return;
        if (MimicCDTimer > 0) return;
        if (IsShifted) return;

        var playerlist = Main.AllAlivePlayerControls.Where(a => a.PlayerId != pc.PlayerId).ToList();

        try
        {
            pc.RpcShapeshift(playerlist[IRandom.Instance.Next(0, playerlist.Count)], false);

            IsShifted = true;
            LastMimic = Utils.GetTimeStamp();
            MimicCDTimer = MimicCooldown.GetInt();
            MimicDurTimer = MimicDuration.GetInt();
        }
        catch (System.Exception ex)
        {
            Logger.Error(ex.ToString(), "Glitch.Mimic.RpcShapeshift");
        }
    }
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseSabotage(PlayerControl pc) => true;

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;

        if (KCDTimer > 0 && HackCDTimer > 0) return false;

        if (killer.CheckDoubleTrigger(target, () =>
        {
            if (HackCDTimer <= 0)
            {
                Utils.NotifyRoles(SpecifySeer: killer);
                HackCDTimer = HackCooldown.GetInt();
                hackedIdList.TryAdd(target.PlayerId, Utils.GetTimeStamp());
                LastHack = Utils.GetTimeStamp();
            }
        }))
        {
            if (KCDTimer > 0) return false;
            LastKill = Utils.GetTimeStamp();
            KCDTimer = KillCooldown.GetInt();
            return true;
        }
        else return false;
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (lowLoad || !Main.IntroDestroyed) return;

        if (HackCDTimer > 180 || HackCDTimer < 0) HackCDTimer = 0;
        if (KCDTimer > 180 || KCDTimer < 0) KCDTimer = 0;
        if (MimicCDTimer > 180 || MimicCDTimer < 0) MimicCDTimer = 0;
        if (MimicDurTimer > 180 || MimicDurTimer < 0) MimicDurTimer = 0;

        bool change = false;
        foreach (var pc in hackedIdList)
        {
            if (pc.Value + HackDuration.GetInt() < nowTime)
            {
                hackedIdList.Remove(pc.Key);
                change = true;
            }
        }

        if (change) { Utils.NotifyRoles(SpecifySeer: player, ForceLoop: false); }

        if (!player.IsAlive())
        {
            HackCDTimer = 0;
            KCDTimer = 0;
            MimicCDTimer = 0;
            MimicDurTimer = 0;

            if (LastRpcSend <= nowTime + 500)
            {
                SendRPC();
                LastRpcSend += 9999;
            }
            return;
        }

        if (MimicDurTimer > 0)
        {
            try { MimicDurTimer = (int)(MimicDuration.GetInt() - (nowTime - LastMimic)); }
            catch { MimicDurTimer = 0; }
            if (MimicDurTimer > 180) MimicDurTimer = 0;
        }
        if ((MimicDurTimer <= 0 || !GameStates.IsInTask) && IsShifted)
        {
            try
            {
                player.RpcShapeshift(player, false);
                IsShifted = false;
            }
            catch (System.Exception ex)
            {
                Logger.Error(ex.ToString(), "Glitch.Mimic.RpcRevertShapeshift");
            }
            if (!GameStates.IsInTask)
            {
                MimicDurTimer = 0;
            }
        }

        if (HackCDTimer <= 0 && KCDTimer <= 0 && MimicCDTimer <= 0 && MimicDurTimer <= 0) return;

        try { HackCDTimer = (int)(HackCooldown.GetInt() - (nowTime - LastHack)); }
        catch { HackCDTimer = 0; }
        if (HackCDTimer > 180 || HackCDTimer < 0) HackCDTimer = 0;

        try { KCDTimer = (int)(KillCooldown.GetInt() - (nowTime - LastKill)); }
        catch { KCDTimer = 0; }
        if (KCDTimer > 180 || KCDTimer < 0) KCDTimer = 0;

        try { MimicCDTimer = (int)(MimicCooldown.GetInt() - (nowTime - LastMimic)); }
        catch { MimicCDTimer = 0; }
        if (MimicCDTimer > 180 || MimicCDTimer < 0) MimicCDTimer = 0;

        if (!player.IsModded())
        {
            var Pname = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Glitch), player.GetRealName(isMeeting: true));
            if (!NameNotifyManager.Notice.TryGetValue(player.PlayerId, out var a) || a.Text != Pname) player.Notify(Pname, 1.1f);
        }
        if (player.IsNonHostModdedClient()) // For mooded non host players, sync kcd per second
        {
            if (LastRpcSend < nowTime)
            {
                SendRPC();
                LastRpcSend = nowTime;
            }
        }
    }

    public override string GetLowerText(PlayerControl player, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (!player.IsAlive() || isForMeeting) return string.Empty;

        var sb = new StringBuilder(string.Empty);

        if (MimicDurTimer > 0) sb.Append($"{string.Format(GetString("Glitch_MimicDur"), MimicDurTimer)}\n");
        if (MimicCDTimer > 0 && MimicDurTimer <= 0) sb.Append($"{string.Format(GetString("Glitch_MimicCD"), MimicCDTimer)}\n");
        if (HackCDTimer > 0) sb.Append($"{string.Format(GetString("Glitch_HackCD"), HackCDTimer)}\n");
        if (KCDTimer > 0) sb.Append($"{string.Format(GetString("Glitch_KCD"), KCDTimer)}\n");

        return sb.ToString();

    }
    public override void AfterMeetingTasks()
    {
        var timestamp = Utils.GetTimeStamp();
        LastKill = timestamp;
        LastHack = timestamp;
        LastMimic = timestamp;
        KCDTimer = 10;
        HackCDTimer = 10;
        MimicCDTimer = 10;
        MimicDurTimer = 0;
        NotSetCD = true;
        SendRPC();
    }
    public override bool OnCoEnterVentOthers(PlayerPhysics physics, int ventId)
    {
        if (hackedIdList.ContainsKey(physics.myPlayer.PlayerId))
        {
            _ = new LateTask(() =>
            {
                physics.myPlayer?.Notify(string.Format(GetString("HackedByGlitch"), GetString("GlitchVent")));
                physics?.RpcBootFromVent(ventId);
            }, 0.5f, "Player Boot From Vent By Glith");
            return true;
        }
        return false;
    }
    public static bool OnCheckFixedUpdateReport(byte id) => hackedIdList.ContainsKey(id);
    public static void CancelReportInFixedUpdate(PlayerControl __instance, byte id)
    {
        __instance.Notify(string.Format(GetString("HackedByGlitch"), "Report"));
        Logger.Info("Dead Body Report Blocked (player is hacked by Glitch)", "FixedUpdate.ReportDeadBody");
        ReportDeadBodyPatch.WaitReport[id].Clear();
    }
    public static bool OnCheckMurderOthers(PlayerControl killer, PlayerControl target)
    {
        if (killer == target || killer == null) return true;
        if (hackedIdList.ContainsKey(killer.PlayerId))
        {
            killer.Notify(string.Format(GetString("HackedByGlitch"), GetString("GlitchKill")));
            return false;
        }
        return true;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("KillButtonText"));
        hud.SabotageButton.OverrideText(GetString("Glitch_MimicButtonText"));
    }

    private void SendRPC()
    {
        var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.None, -1);
        writer.WriteNetObject(_Player);
        writer.Write(HackCDTimer);
        writer.Write(KCDTimer);
        writer.Write(MimicCDTimer);
        writer.Write(MimicDurTimer);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        HackCDTimer = reader.ReadInt32();
        KCDTimer = reader.ReadInt32();
        MimicCDTimer = reader.ReadInt32();
        MimicDurTimer = reader.ReadInt32();
    }
}
