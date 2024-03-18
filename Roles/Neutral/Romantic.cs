using HarmonyLib;
using Hazel;
using MS.Internal.Xml.XPath;
using Sentry.Internal.Extensions;
using System.Collections.Generic;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Neutral;

internal class Romantic : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 13500;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    //==================================================================\\

    private static readonly int MaxBetTimes = 1;
    public static bool isProtect = false;
    public static bool isRomanticAlive = true;
    public static bool isPartnerProtected = false;

    public static OptionItem BetCooldown;
    private static OptionItem ProtectCooldown;
    private static OptionItem ProtectDuration;
    private static OptionItem KnowTargetRole;
    private static OptionItem BetTargetKnowRomantic;
    public static OptionItem VengefulKCD;
    public static OptionItem VengefulCanVent;
    public static OptionItem RuthlessKCD;
    public static OptionItem RuthlessCanVent;

    public static byte VengefulTargetId;
    private static Dictionary<byte, int> BetTimes = [];
    public static Dictionary<byte, byte> BetPlayer = [];

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Romantic, 1, zeroOne: false);
        BetCooldown = FloatOptionItem.Create(Id + 10, "RomanticBetCooldown", new(0f, 60f, 1f), 7f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Romantic])
            .SetValueFormat(OptionFormat.Seconds);
        ProtectCooldown = FloatOptionItem.Create(Id + 11, "RomanticProtectCooldown", new(0f, 60f, 2.5f), 25f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Romantic])
            .SetValueFormat(OptionFormat.Seconds);
        ProtectDuration = FloatOptionItem.Create(Id + 12, "RomanticProtectDuration", new(0f, 60f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Romantic])
            .SetValueFormat(OptionFormat.Seconds);
        KnowTargetRole = BooleanOptionItem.Create(Id + 13, "RomanticKnowTargetRole", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Romantic]);
        BetTargetKnowRomantic = BooleanOptionItem.Create(Id + 14, "RomanticBetTargetKnowRomantic", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Romantic]);
        VengefulKCD = FloatOptionItem.Create(Id + 15, "VengefulKCD", new(0f, 60f, 2.5f), 22.5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Romantic])
            .SetValueFormat(OptionFormat.Seconds);
        VengefulCanVent = BooleanOptionItem.Create(Id + 16, "VengefulCanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Romantic]);
        RuthlessKCD = FloatOptionItem.Create(Id + 17, "RuthlessKCD", new(0f, 60f, 2.5f), 22.5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Romantic])
            .SetValueFormat(OptionFormat.Seconds);
        RuthlessCanVent = BooleanOptionItem.Create(Id + 18, "RuthlessCanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Romantic]);
    }
    public override void Init()
    {
        VengefulTargetId = byte.MaxValue;
        playerIdList.Clear();
        BetTimes.Clear();
        BetPlayer.Clear();
        isProtect = false;
        isPartnerProtected = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        BetTimes.Add(playerId, MaxBetTimes);
        CustomRoleManager.MarkOthers.Add(TargetMark);
        CustomRoleManager.OthersAfterDeathTask.Add(ChangeRole);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Romantic);
        writer.Write(playerId);
        writer.Write(BetTimes.TryGetValue(playerId, out var times) ? times : MaxBetTimes);
        writer.Write(BetPlayer.TryGetValue(playerId, out var player) ? player : byte.MaxValue);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
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
    public override bool CanUseKillButton(PlayerControl player) => true;
    public override void SetKillCooldown(byte id)
    {
        if (BetTimes.TryGetValue(id, out var times) && times < 1)
        {
            Main.AllPlayerKillCooldown[id] = ProtectCooldown.GetFloat();
            return;
        }
        else Main.AllPlayerKillCooldown[id] = BetCooldown.GetFloat();
        //float cd = BetCooldown.GetFloat();
        //cd += Main.AllPlayerControls.Count(x => !x.IsAlive()) * BetCooldownIncrese.GetFloat();
        //cd = Math.Min(cd, MaxBetCooldown.GetFloat());
        //Main.AllPlayerKillCooldown[id] = cd;
    }
    public override bool KnowRoleTarget(PlayerControl player, PlayerControl target)
    {
        if (!KnowTargetRole.GetBool()) return false;
        return player.Is(CustomRoles.Romantic) && BetPlayer.TryGetValue(player.PlayerId, out var tar) && tar == target.PlayerId;
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.PlayerId == target.PlayerId) return true;
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Succubus), GetString("CantRecruit")));
            return false;
        }
        //if (BetPlayer.TryGetValue(killer.PlayerId, out var tar) && tar == target.PlayerId) return false;
        if (!BetTimes.TryGetValue(killer.PlayerId, out var times) || times < 1) isProtect = true;

        if (!isProtect)
        {
            BetTimes[killer.PlayerId]--;

            //if (BetPlayer.TryGetValue(killer.PlayerId, out var originalTarget) && Utils.GetPlayerById(originalTarget) != null)
            //{
            //    Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: Utils.GetPlayerById(originalTarget), ForceLoop: true);
            //    Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(originalTarget), SpecifyTarget: killer, ForceLoop: true);
            //}

            BetPlayer.Remove(killer.PlayerId);
            BetPlayer.Add(killer.PlayerId, target.PlayerId);
            SendRPC(killer.PlayerId);

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.RPCPlayCustomSound("Bet");

            killer.Notify(GetString("RomanticBetPlayer"));

            if (BetTargetKnowRomantic.GetBool())
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Romantic), GetString("RomanticBetOnYou")));

            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
            Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);

            Logger.Info($"Romantic：{killer.GetNameWithRole().RemoveHtmlTags()} bet player => {target.GetNameWithRole().RemoveHtmlTags()}", "Romantic");
        }
        else
        {
            if (BetPlayer.TryGetValue(killer.PlayerId, out var originalTarget))
            {
                var tpc = Utils.GetPlayerById(originalTarget);
                isPartnerProtected = true;
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.RPCPlayCustomSound("Shield");
                killer.Notify(GetString("RomanticProtectPartner"));
                tpc.Notify(GetString("RomanticIsProtectingYou"));
                
                _ = new LateTask(() =>
                {
                    if (!GameStates.IsInTask || !tpc.IsAlive()) return;
                    isPartnerProtected = false;
                    killer.Notify(GetString("ProtectingOver"));
                    tpc.Notify(GetString("ProtectingOver"));
                    killer.SetKillCooldown();
                }, ProtectDuration.GetFloat(), "Romantic Protecting Is Over");
            }
        }

        return false;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (seer == null || seer.Is(CustomRoles.Romantic)) return string.Empty;
        if (!BetPlayer.ContainsValue(seer.PlayerId)) return string.Empty;
        if (!BetTargetKnowRomantic.GetBool()) return string.Empty;

        return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Romantic), "♥");
    }
    public static bool OnCheckMurderOthers(PlayerControl target)
    {
        return isPartnerProtected && BetPlayer.ContainsValue(target.PlayerId);
    }
    public override void AfterPlayerDeathTask(PlayerControl target) => Romantic.isRomanticAlive = false;
    private static string TargetMark(PlayerControl seer, PlayerControl target, bool IsForMeeting = false)
    {
        if (!seer.Is(CustomRoles.Romantic))
        {
            if (!BetTargetKnowRomantic.GetBool()) return "";
            return (BetPlayer.TryGetValue(target.PlayerId, out var x) && seer.PlayerId == x) ?
                Utils.ColorString(Utils.GetRoleColor(CustomRoles.Romantic), "♥") : "";
        }
        var GetValue = BetPlayer.TryGetValue(seer.PlayerId, out var targetId);
        return GetValue && targetId == target.PlayerId ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Romantic), "♥") : "";
    }
    public override string GetProgressText(byte playerId, bool cooms)
    {
        var player = Utils.GetPlayerById(playerId);
        if (player == null) return null;
        return Utils.ColorString(BetTimes.TryGetValue(playerId, out var timesV1) && timesV1 >= 1 ? Color.white : Utils.GetRoleColor(CustomRoles.Romantic), $"<color=#ffffff>-</color> {(BetTimes.TryGetValue(playerId, out var timesV2) && timesV2 >= 1 && timesV2 >= 1 ? "♡" : "♥")}");
    }
    public override void OnReportDeadBody(PlayerControl ugandan, PlayerControl knuckles)
    {
        isPartnerProtected = false;
    }
    private static void ChangeRole(PlayerControl player)
    {
        var playerId = player.PlayerId;
        if (!Romantic.BetPlayer.ContainsValue(playerId) || player == null) return;

        byte romantic = 0x73;
        BetPlayer.Do(x =>
        {
            if (x.Value == playerId)
                romantic = x.Key;
        });
        if (romantic == 0x73) return;
        var pc = Utils.GetPlayerById(romantic);
        if (pc == null) return;
        if (player.GetCustomRole().IsImpostorTeamV3())
        {
            Logger.Info($"Impostor Romantic Partner Died changing {pc.GetNameWithRole()} to Refugee", "Romantic");
            pc.RpcSetCustomRole(CustomRoles.Refugee);
            Utils.NotifyRoles(ForceLoop: true);
            pc.ResetKillCooldown();
            pc.SetKillCooldown();
        }
        else if (player.IsNeutralKiller())
        {
            Logger.Info($"Neutral Romantic Partner Died changing {pc.GetNameWithRole()} to Ruthless Romantic", "Romantic");
            pc.RpcSetCustomRole(CustomRoles.RuthlessRomantic);
            pc.GetRoleClass().Add(pc.PlayerId);
            Utils.NotifyRoles(ForceLoop: true);
            pc.ResetKillCooldown();
            pc.SetKillCooldown();
        }
        else
        {
            _ = new LateTask(() =>
            {
                Logger.Info($"Crew/nnk Romantic Partner Died changing {pc.GetNameWithRole().RemoveHtmlTags()} to Vengeful romantic", "Romantic");
                var killer = player.GetRealKiller();
                if (killer == null) //change role to RuthlessRomantic if there is no killer for partner in game
                {
                    pc.RpcSetCustomRole(CustomRoles.RuthlessRomantic);
                    pc.GetRoleClass().Add(pc.PlayerId);
                    Logger.Info($"No real killer for {player.GetRealName().RemoveHtmlTags()}, role changed to ruthless romantic", "Romantic");
                }
                else 
                {
                    VengefulTargetId = killer.PlayerId;

                    VengefulRomantic.SendRPC(pc.PlayerId);
                    pc.RpcSetCustomRole(CustomRoles.VengefulRomantic);
                    pc.GetRoleClass().Add(pc.PlayerId);
                    Logger.Info($"Vengeful romantic target: {killer.GetRealName().RemoveHtmlTags()}, [{VengefulTargetId}]", "Vengeful Romantic");
                }
                Utils.NotifyRoles(ForceLoop: true);
                pc.ResetKillCooldown();
                pc.SetKillCooldown();
            }, 0.2f, "Convert to Vengeful Romantic");
        }
    }
}

internal class VengefulRomantic : RoleBase
{

    //===========================SETUP================================\\
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => new Romantic().ThisRoleBase; 
    //==================================================================\\

    public static bool hasKilledKiller = false;
    public static Dictionary<byte, byte> VengefulTarget = [];

    public override void Init()
    {
        playerIdList.Clear();
        VengefulTarget.Clear();
        hasKilledKiller = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        VengefulTarget.Add(playerId, Romantic.VengefulTargetId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    public override bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && !hasKilledKiller;

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.PlayerId == target.PlayerId) return true; //set it to true coz shaman can do this, and killer shd die

        if (VengefulTarget.TryGetValue(killer.PlayerId, out var PartnerKiller) && target.PlayerId == PartnerKiller)
        {
            hasKilledKiller = true;
            return true;
        }
        else
        {
            killer.RpcMurderPlayerV3(killer);
            Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
            return false;
        }
    }
    public override string GetProgressText(byte playerId, bool cooms)
    {
        var player = Utils.GetPlayerById(playerId);
        if (player == null) return null;
        return Utils.ColorString(hasKilledKiller ? Color.green : Utils.GetRoleColor(CustomRoles.VengefulRomantic), $"<color=#777777>-</color> {((hasKilledKiller) ? "♥" : "♡")}");
    }
    public static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.VengefulRomantic); //SyncVengefulRomanticTarget
        writer.Write(playerId);
        //writer.Write(BetTimes.TryGetValue(playerId, out var times) ? times : MaxBetTimes);
        writer.Write(VengefulTarget.TryGetValue(playerId, out var player) ? player : byte.MaxValue);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        byte Target = reader.ReadByte();
        VengefulTarget.Remove(PlayerId);
        if (Target != byte.MaxValue)
            VengefulTarget.Add(PlayerId, Target);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Romantic.VengefulKCD.GetFloat();
    public override bool CanUseImpostorVentButton(PlayerControl pc) => Romantic.VengefulCanVent.GetBool();
}

internal class RuthlessRomantic : RoleBase
{

    //===========================SETUP================================\\
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => new Romantic().ThisRoleBase;

    //==================================================================\\
    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Romantic.RuthlessKCD.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => Romantic.RuthlessCanVent.GetBool();
}
