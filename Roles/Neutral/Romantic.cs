using Hazel;
using InnerNet;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;


namespace TOHE.Roles.Neutral;

internal class Romantic : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Romantic;
    private const int Id = 13500;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Romantic);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

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
    private static readonly Dictionary<byte, int> BetTimes = [];
    public static readonly Dictionary<byte, byte> BetPlayer = [];

    public override void SetupCustomOption()
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
        BetTimes.Clear();
        BetPlayer.Clear();
        isProtect = false;
        isPartnerProtected = false;
    }
    public override void Add(byte playerId)
    {
        BetTimes.Add(playerId, 1);

        CustomRoleManager.CheckDeadBodyOthers.Add(OthersAfterPlayerDeathTask);
    }
    public override void Remove(byte playerId)
    {
        BetTimes.Remove(playerId);

        CustomRoleManager.CheckDeadBodyOthers.Remove(OthersAfterPlayerDeathTask);
    }

    private void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(playerId);
        writer.Write(BetTimes.TryGetValue(playerId, out var times) ? times : 1);
        writer.Write(BetPlayer.TryGetValue(playerId, out var player) ? player : byte.MaxValue);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
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
    }
    public override bool KnowRoleTarget(PlayerControl player, PlayerControl target)
    {
        if (!KnowTargetRole.GetBool()) return false;
        return player.Is(CustomRoles.Romantic) && BetPlayer.TryGetValue(player.PlayerId, out var tar) && tar == target.PlayerId;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (!isProtect)
            hud.KillButton.OverrideText(GetString("RomanticPartnerButtonText"));
        else
            hud.KillButton.OverrideText(GetString("RomanticProtectButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("RomanticProtect");

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.PlayerId == target.PlayerId) return true;
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CantRecruit")));
            return false;
        }
        if (!BetTimes.TryGetValue(killer.PlayerId, out var times) || times < 1) isProtect = true;

        if (!isProtect)
        {
            BetTimes[killer.PlayerId]--;

            BetPlayer.Remove(killer.PlayerId);
            BetPlayer.Add(killer.PlayerId, target.PlayerId);
            SendRPC(killer.PlayerId);

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.RPCPlayCustomSound("Bet");

            killer.Notify(GetString("RomanticBetPlayer"));

            if (BetTargetKnowRomantic.GetBool())
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Romantic), GetString("RomanticBetOnYou")));

            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target);
            Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer);

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
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
        => isPartnerProtected && BetPlayer.ContainsValue(target.PlayerId);

    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide) => isRomanticAlive = false;

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (seer == seen) return string.Empty;

        return BetPlayer.ContainsValue(seen.PlayerId)
            ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Romantic), "♥") : string.Empty;
    }

    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (!seer.Is(CustomRoles.Romantic) && BetTargetKnowRomantic.GetBool())
        {
            if (seer == target && seer.IsAlive() && BetPlayer.ContainsValue(seer.PlayerId))
            {
                return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Romantic), "♥");
            }
            else if (seer != target && seer.IsAlive() && BetPlayer.ContainsKey(target.PlayerId) && BetPlayer.ContainsValue(seer.PlayerId))
            {
                return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Romantic), "♥");
            }
            else if (seer != target && !seer.IsAlive() && BetPlayer.ContainsValue(target.PlayerId))
            {
                return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Romantic), "♥");
            }
        }
        return string.Empty;
    }
    public override string GetProgressText(byte playerId, bool cooms)
    {
        var player = Utils.GetPlayerById(playerId);
        if (player == null) return null;
        return Utils.ColorString(BetTimes.TryGetValue(playerId, out var timesV1) && timesV1 >= 1 ? Color.white : Utils.GetRoleColor(CustomRoles.Romantic), $"<color=#ffffff>-</color> {(BetTimes.TryGetValue(playerId, out var timesV2) && timesV2 >= 1 && timesV2 >= 1 ? "♡" : "♥")}");
    }
    public override void OnReportDeadBody(PlayerControl ugandan, NetworkedPlayerInfo knuckles)
    {
        isPartnerProtected = false;
    }
    public override void OnPlayerExiled(PlayerControl player, NetworkedPlayerInfo exiled)
    {
        if (exiled == null) return;

        var exiledId = exiled.PlayerId;
        if (BetPlayer.ContainsValue(exiledId))
        {
            player = Utils.GetPlayerById(exiledId);
            if (player == null) return;

            ChangeRole(player);
        }
    }
    private void OthersAfterPlayerDeathTask(PlayerControl killer, PlayerControl player, bool inMeeting)
    {
        ChangeRole(player);
    }
    private static void ChangeRole(PlayerControl player)
    {
        var playerId = player.PlayerId;
        if (!BetPlayer.ContainsValue(playerId) || player == null) return;

        byte romantic = 0x73;
        BetPlayer.Do(x =>
        {
            if (x.Value == playerId)
                romantic = x.Key;
        });
        if (romantic == 0x73) return;
        var pc = Utils.GetPlayerById(romantic);
        if (pc == null) return;
        if (player.GetRealKiller() == pc)
        {
            pc.SetDeathReason(PlayerState.DeathReason.FollowingSuicide);
            pc.RpcMurderPlayer(pc);
            return;
        }
        if (player.GetCustomRole().IsImpostorTeamV3())
        {
            Logger.Info($"Impostor Romantic Partner Died changing {pc.GetNameWithRole()} to Refugee", "Romantic");
            pc.GetRoleClass()?.OnRemove(pc.PlayerId);
            pc.RpcSetCustomRole(CustomRoles.Refugee);
            pc.GetRoleClass()?.OnAdd(pc.PlayerId);
            Utils.NotifyRoles(SpecifyTarget: pc);
            pc.ResetKillCooldown();
            pc.SetKillCooldown();
        }
        else if (player.IsNeutralKiller())
        {
            Logger.Info($"Neutral Romantic Partner Died changing {pc.GetNameWithRole()} to Ruthless Romantic", "Romantic");
            pc.GetRoleClass()?.OnRemove(pc.PlayerId);
            pc.RpcSetCustomRole(CustomRoles.RuthlessRomantic);
            pc.GetRoleClass().OnAdd(pc.PlayerId);
            Utils.NotifyRoles(SpecifyTarget: pc);
            pc.ResetKillCooldown();
            pc.SetKillCooldown();
        }
        else
        {
            _ = new LateTask(() =>
            {
                Logger.Info($"Crew/nnk Romantic Partner Died changing {pc.GetNameWithRole().RemoveHtmlTags()} to Vengeful romantic", "Romantic");
                var killer = player.GetRealKiller();
                if (killer == null //if no killer
                    || Main.PlayerStates[player.PlayerId].deathReason == PlayerState.DeathReason.Vote //or if partner is ejected
                    || killer == player //or if partner dies by suicide
                    || !killer.IsAlive()) //or if killer is dead,romantic will become ruthless romantic
                {
                    pc.GetRoleClass()?.OnRemove(pc.PlayerId);
                    pc.RpcSetCustomRole(CustomRoles.RuthlessRomantic);
                    pc.GetRoleClass().OnAdd(pc.PlayerId);
                    Logger.Info($"No real killer for {player.GetRealName().RemoveHtmlTags()}, role changed to ruthless romantic", "Romantic");
                }
                else
                {
                    VengefulTargetId = killer.PlayerId;

                    pc.GetRoleClass()?.OnRemove(pc.PlayerId);
                    pc.RpcSetCustomRole(CustomRoles.VengefulRomantic);
                    pc.GetRoleClass().OnAdd(pc.PlayerId);
                    if (pc.GetRoleClass() is VengefulRomantic VR) VR.SendRPC(pc.PlayerId);
                    Logger.Info($"Vengeful romantic target: {killer.GetRealName().RemoveHtmlTags()}, [{VengefulTargetId}]", "Vengeful Romantic");
                }
                Utils.NotifyRoles(SpecifyTarget: pc);
                pc.ResetKillCooldown();
                pc.SetKillCooldown();
            }, 0.2f, "Convert to Vengeful Romantic");
        }
    }
}

internal class VengefulRomantic : RoleBase
{

    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.VengefulRomantic;
    public override bool IsDesyncRole => new Romantic().IsDesyncRole;
    public override CustomRoles ThisRoleBase => new Romantic().ThisRoleBase;
    public override Custom_RoleType ThisRoleType => new Romantic().ThisRoleType;
    //==================================================================\\

    public static bool hasKilledKiller = false;
    public static Dictionary<byte, byte> VengefulTarget = [];

    public override void Init()
    {
        VengefulTarget.Clear();
        hasKilledKiller = false;
    }
    public override void Add(byte playerId)
    {
        VengefulTarget.Add(playerId, Romantic.VengefulTargetId);
    }

    public override bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && !hasKilledKiller;

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.PlayerId == target.PlayerId) return true; //set it to true coz Shaman can do this, and killer should die

        if (VengefulTarget.TryGetValue(killer.PlayerId, out var PartnerKiller) && target.PlayerId == PartnerKiller)
        {
            hasKilledKiller = true;
            return true;
        }
        else
        {
            killer.SetDeathReason(PlayerState.DeathReason.Misfire);
            killer.RpcMurderPlayer(killer);
            return false;
        }
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("VengefulRomanticButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("RomanticKill");

    public override string GetProgressText(byte playerId, bool cooms)
    {
        var player = Utils.GetPlayerById(playerId);
        if (player == null) return null;
        return Utils.ColorString(hasKilledKiller ? Color.green : Utils.GetRoleColor(CustomRoles.VengefulRomantic), $"<color=#777777>-</color> {((hasKilledKiller) ? "♥" : "♡")}");
    }
    public void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player); //SyncVengefulRomanticTarget
        writer.Write(playerId);
        writer.Write(VengefulTarget.TryGetValue(playerId, out var player) ? player : byte.MaxValue);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
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
    public override CustomRoles Role => CustomRoles.RuthlessRomantic;
    public override bool IsDesyncRole => new Romantic().IsDesyncRole;
    public override CustomRoles ThisRoleBase => new Romantic().ThisRoleBase;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\
    public override void Init()
    {

    }
    public override void Add(byte playerId)
    {

    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Romantic.RuthlessKCD.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => Romantic.RuthlessCanVent.GetBool();
}
