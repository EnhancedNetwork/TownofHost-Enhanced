using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static TOHE.Translator;
using static TOHE.Options;
using UnityEngine;
using MS.Internal.Xml.XPath;

namespace TOHE.Roles.Neutral;

internal class Glitch : RoleBase
{

    //===========================SETUP================================\\
    private const int Id = 16300;
    public static List<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    //==================================================================\\

    public static Dictionary<byte, long> hackedIdList = [];

    public static OptionItem KillCooldown;
    public static OptionItem HackCooldown;
    public static OptionItem HackDuration;
    public static OptionItem MimicCooldown;
    public static OptionItem MimicDuration;
    public static OptionItem CanVent;
    private static OptionItem HasImpostorVision;

    public static int HackCDTimer;
    public static int KCDTimer;
    public static int MimicCDTimer;
    public static int MimicDurTimer;
    public static long LastHack;
    public static long LastKill;
    public static long LastMimic;

    private static bool isShifted = false;
    //    public static OptionItem CanUseSabotage;

    public static void SetupCustomOption()
    {
        //Glitchは1人固定
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Glitch, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 1f), 20, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Glitch])
            .SetValueFormat(OptionFormat.Seconds);
        HackCooldown = IntegerOptionItem.Create(Id + 11, "HackCooldown", new(0, 180, 1), 20, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Glitch])
            .SetValueFormat(OptionFormat.Seconds);
        HackDuration = FloatOptionItem.Create(Id + 14, "HackDuration", new(0f, 60f, 1f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Glitch])
            .SetValueFormat(OptionFormat.Seconds);
        MimicCooldown = IntegerOptionItem.Create(Id + 15, "MimicCooldown", new(0, 180, 1), 15, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Glitch])
            .SetValueFormat(OptionFormat.Seconds);
        MimicDuration = FloatOptionItem.Create(Id + 16, "MimicDuration", new(0f, 60f, 1f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Glitch])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 12, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Glitch]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Glitch]);
    }
    public override void Init()
    {
        playerIdList = [];
        hackedIdList = [];
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        HackCDTimer = 10;
        KCDTimer = 10;
        MimicCDTimer = 10;
        MimicDurTimer = 0;

        isShifted = false;

        var ts = Utils.GetTimeStamp();

        LastKill = ts;
        LastHack = ts;
        LastMimic = ts;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);

        // Double Trigger
        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }
    public override void SetHudActive(HudManager hud, bool isActive)
    {
        hud.SabotageButton.ToggleVisible(isActive);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = 1f;
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public static void Mimic(PlayerControl pc)
    {
        if (pc == null) return;
        if (!pc.IsAlive()) return;
        if (MimicCDTimer > 0) return;
        if (isShifted) return;

        var playerlist = Main.AllAlivePlayerControls.Where(a => a.PlayerId != pc.PlayerId).ToList();

        try
        {
            pc.RpcShapeshift(playerlist[IRandom.Instance.Next(0, playerlist.Count)], false);

            isShifted = true;
            LastMimic = Utils.GetTimeStamp();
            MimicCDTimer = MimicCooldown.GetInt();
            MimicDurTimer = MimicDuration.GetInt();
        }
        catch (System.Exception ex)
        {
            Logger.Error(ex.ToString(), "Glitch.Mimic.RpcShapeshift");
        }
    }
    public override bool CanUseImpostorVentButton(PlayerControl pc) => Glitch.CanVent.GetBool();
    public override bool CanUseKillButton(PlayerControl pc) => pc.IsAlive();
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null) return false;
        if (target == null) return false;

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
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (HackCDTimer > 180 || HackCDTimer < 0) HackCDTimer = 0;
        if (KCDTimer > 180 || KCDTimer < 0) KCDTimer = 0;
        if (MimicCDTimer > 180 || MimicCDTimer < 0) MimicCDTimer = 0;
        if (MimicDurTimer > 180 || MimicDurTimer < 0) MimicDurTimer = 0;

        bool change = false;
        foreach (var pc in hackedIdList)
        {
            if (pc.Value + HackDuration.GetInt() < Utils.GetTimeStamp())
            {
                hackedIdList.Remove(pc.Key);
                change = true;
            }
        }

        if (player == null) return;
        if (!player.Is(CustomRoles.Glitch)) return;

        if (change) { Utils.NotifyRoles(SpecifySeer: player); }

        if (!player.IsAlive())
        {
            HackCDTimer = 0;
            KCDTimer = 0;
            MimicCDTimer = 0;
            MimicDurTimer = 0;
            return;
        }

        if (MimicDurTimer > 0)
        {
            try { MimicDurTimer = (int)(MimicDuration.GetInt() - (Utils.GetTimeStamp() - LastMimic)); }
            catch { MimicDurTimer = 0; }
            if (MimicDurTimer > 180) MimicDurTimer = 0;
        }
        if ((MimicDurTimer <= 0 || !GameStates.IsInTask) && isShifted)
        {
            try
            {
                player.RpcShapeshift(player, false);
                isShifted = false;
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

        try { HackCDTimer = (int)(HackCooldown.GetInt() - (Utils.GetTimeStamp() - LastHack)); }
        catch { HackCDTimer = 0; }
        if (HackCDTimer > 180 || HackCDTimer < 0) HackCDTimer = 0;

        try { KCDTimer = (int)(KillCooldown.GetInt() - (Utils.GetTimeStamp() - LastKill)); }
        catch { KCDTimer = 0; }
        if (KCDTimer > 180 || KCDTimer < 0) KCDTimer = 0;

        try { MimicCDTimer = (int)(MimicCooldown.GetInt() + MimicDuration.GetInt() - (Utils.GetTimeStamp() - LastMimic)); }
        catch { MimicCDTimer = 0; }
        if (MimicCDTimer > 180 || MimicCDTimer < 0) MimicCDTimer = 0;

        if (!player.IsModClient())
        {
            var sb = new StringBuilder();

            if (MimicDurTimer > 0) sb.Append($"\n{string.Format(Translator.GetString("MimicDur"), MimicDurTimer)}");
            if (MimicCDTimer > 0 && MimicDurTimer <= 0) sb.Append($"\n{string.Format(Translator.GetString("MimicCD"), MimicCDTimer)}");
            if (HackCDTimer > 0) sb.Append($"\n{string.Format(Translator.GetString("HackCD"), HackCDTimer)}");
            if (KCDTimer > 0) sb.Append($"\n{string.Format(Translator.GetString("KCD"), KCDTimer)}");

            string ns = sb.ToString();

            if ((!NameNotifyManager.Notice.TryGetValue(player.PlayerId, out var a) || a.Item1 != ns) && ns != string.Empty) player.Notify(ns, 1.1f);
        }
    }
    public override string GetLowerText(PlayerControl player, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (player == null) return string.Empty;
        if (!player.Is(CustomRoles.Glitch)) return string.Empty;
        if (!player.IsAlive()) return string.Empty;

        var sb = new StringBuilder();

        if (MimicDurTimer > 0) sb.Append($"{string.Format(Translator.GetString("MimicDur"), MimicDurTimer)}\n");
        if (MimicCDTimer > 0 && MimicDurTimer <= 0) sb.Append($"{string.Format(Translator.GetString("MimicCD"), MimicCDTimer)}\n");
        if (HackCDTimer > 0) sb.Append($"{string.Format(Translator.GetString("HackCD"), HackCDTimer)}\n");
        if (KCDTimer > 0) sb.Append($"{string.Format(Translator.GetString("KCD"), KCDTimer)}\n");

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
    }
    public static bool OnCoEnterVentOthers(PlayerPhysics __instance, int id)
    {
        if (hackedIdList.ContainsKey(__instance.myPlayer.PlayerId))
        {
            _ = new LateTask(() =>
            {
                __instance.myPlayer?.Notify(string.Format(GetString("HackedByGlitch"), GetString("GlitchVent")));
                __instance.myPlayer?.MyPhysics?.RpcBootFromVent(id);
            }, 0.5f, "Player Boot From Vent By Glith");
            return true;
        }
        return false;
    }
    public static bool OnCheckFixedUpdateReport(PlayerControl __instance, byte id) 
    {
        if (hackedIdList.ContainsKey(id))
        {
            __instance.Notify(string.Format(GetString("HackedByGlitch"), "Report"));
            Logger.Info("Dead Body Report Blocked (player is hacked by Glitch)", "FixedUpdate.ReportDeadBody");
            ReportDeadBodyPatch.WaitReport[id].Clear();
            return false;
        }
        return true;
    }
    public static bool OnCheckMurderOthers(PlayerControl killer, PlayerControl target)
    {
        if (killer == target || killer == null) return true;
        if (HasEnabled && hackedIdList.ContainsKey(killer.PlayerId))
        {
            killer.Notify(string.Format(GetString("HackedByGlitch"), GetString("GlitchKill")));
            return false;
        }
        return true;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("KillButtonText"));
        hud.SabotageButton.OverrideText(GetString("MimicButtonText"));
    }
}
