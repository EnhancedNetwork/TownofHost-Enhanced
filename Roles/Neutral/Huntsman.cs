using AmongUs.GameOptions;
using Hazel;
using System;
using TOHE.Modules.Rpc;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Huntsman : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Huntsman;
    private const int Id = 16500;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Huntsman);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    private static OptionItem SuccessKillCooldown;
    private static OptionItem FailureKillCooldown;
    private static OptionItem NumOfTargets;
    private static OptionItem MinKCD;
    private static OptionItem MaxKCD;

    private bool IsDead = false;
    private readonly HashSet<byte> Targets = [];
    private float KCD = 25;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Huntsman, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Huntsman])
            .SetValueFormat(OptionFormat.Seconds);
        SuccessKillCooldown = FloatOptionItem.Create(Id + 11, "HHSuccessKCDDecrease", new(0f, 180f, 0.5f), 5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Huntsman])
            .SetValueFormat(OptionFormat.Seconds);
        FailureKillCooldown = FloatOptionItem.Create(Id + 12, "HHFailureKCDIncrease", new(0f, 180f, 0.5f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Huntsman])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 13, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Huntsman]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 14, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Huntsman]);
        NumOfTargets = IntegerOptionItem.Create(Id + 15, "HHNumOfTargets", new(0, 10, 1), 3, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Huntsman])
            .SetValueFormat(OptionFormat.Times);
        MaxKCD = FloatOptionItem.Create(Id + 16, "HHMaxKCD", new(0f, 180f, 2.5f), 60f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Huntsman])
            .SetValueFormat(OptionFormat.Seconds);
        MinKCD = FloatOptionItem.Create(Id + 17, "HHMinKCD", new(0f, 180f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Huntsman])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add(byte playerId)
    {
        KCD = KillCooldown.GetFloat();
        IsDead = false;

        _ = new LateTask(() =>
        {
            ResetTargets(isStartedGame: true);
        }, 8f, "Huntsman Reset Targets");
    }

    public void SendRPC(bool isSetTarget, byte targetId = byte.MaxValue)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(isSetTarget);
        if (isSetTarget)
        {
            writer.Write(targetId);
        }
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        bool isSetTarget = reader.ReadBoolean();
        if (!isSetTarget)
        {
            Targets.Clear();
            return;
        }
        byte targetId = reader.ReadByte();
        Targets.Add(targetId);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override void OnReportDeadBody(PlayerControl Ronaldo, NetworkedPlayerInfo IsTheGoat)
    {
        ResetTargets(isStartedGame: false);
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        float tempkcd = KCD;
        if (Targets.Contains(target.PlayerId)) KCD = Math.Clamp(tempkcd -= SuccessKillCooldown.GetFloat(), MinKCD.GetFloat(), MaxKCD.GetFloat());
        else KCD = Math.Clamp(tempkcd += FailureKillCooldown.GetFloat(), MinKCD.GetFloat(), MaxKCD.GetFloat());
        if (KCD != tempkcd)
        {
            killer.ResetKillCooldown();
            killer.SyncSettings();
        }
        return true;
    }
    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        Targets.Clear();
        SendRPC(isSetTarget: false);
        IsDead = true;
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KCD;
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();

    public override string GetLowerText(PlayerControl player, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (isForMeeting || IsDead) return string.Empty;

        var targetId = player.PlayerId;
        string output = string.Empty;
        byte item = 0;
        foreach (var playerId in Targets)
        {
            if (item != 0) output += ", ";
            output += Utils.GetPlayerById(playerId).GetRealName();
            item++;
        }
        return targetId != 0xff ? GetString("Targets") + $"<b><color=#ff1919>{output}</color></b>" : string.Empty;
    }
    private static bool PotentialTargets(PlayerControl player, PlayerControl target)
    {
        if (target == null || player == null) return false;

        if (player.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers)) return false;

        if (target.Is(CustomRoles.Romantic)
            && Romantic.BetPlayer.TryGetValue(target.PlayerId, out byte romanticPartner) && romanticPartner == player.PlayerId) return false;

        if (target.Is(CustomRoles.Lawyer)
            && Lawyer.TargetList.Contains(player.PlayerId) && Lawyer.TargetKnowLawyer) return false;

        if (player.Is(CustomRoles.Charmed)
            && (target.Is(CustomRoles.Cultist) || (target.Is(CustomRoles.Charmed) && Cultist.TargetKnowOtherTargets))) return false;

        if (player.Is(CustomRoles.Infected)
            && (target.Is(CustomRoles.Infectious) || (target.Is(CustomRoles.Infected) && Infectious.TargetKnowOtherTargets))) return false;

        if (player.Is(CustomRoles.Recruit)
            && (target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.Recruit) || target.Is(CustomRoles.Sidekick))) return false;

        if (player.Is(CustomRoles.Contagious)
            && target.Is(CustomRoles.Virus) || (target.Is(CustomRoles.Contagious) && Virus.TargetKnowOtherTarget.GetBool())) return false;

        if (player.Is(CustomRoles.Admired)
            && target.Is(CustomRoles.Admirer) || target.Is(CustomRoles.Admired)) return false;

        if (player.Is(CustomRoles.Soulless)
            && target.Is(CustomRoles.CursedSoul) || target.Is(CustomRoles.Soulless)) return false;

        if (player.Is(CustomRoles.Madmate)
            && target.GetCustomRole().IsImpostor()
            || ((target.GetCustomRole().IsMadmate() || target.Is(CustomRoles.Madmate)) && Madmate.MadmateKnowWhosMadmate.GetBool())) return false;

        return true;

    }
    private void ResetTargets(bool isStartedGame = false)
    {
        if (!AmongUsClient.Instance.AmHost || IsDead || _Player == null) return;

        Targets.Clear();
        SendRPC(isSetTarget: false);

        int potentialTargetCount = Main.AllAlivePlayerControls.Length - 1;
        if (potentialTargetCount < 0) potentialTargetCount = 0;
        int maxLimit = Math.Min(potentialTargetCount, NumOfTargets.GetInt());
        for (var i = 0; i < maxLimit; i++)
        {
            try
            {
                var cTargets = new List<PlayerControl>(Main.AllAlivePlayerControls.Where(pc => !Targets.Contains(pc.PlayerId) && PotentialTargets(_Player, pc) && pc.GetCustomRole() is not CustomRoles.Huntsman and not CustomRoles.Solsticer));
                var rand = IRandom.Instance;
                var target = cTargets.RandomElement();
                var targetId = target.PlayerId;
                Targets.Add(targetId);
                SendRPC(isSetTarget: true, targetId: targetId);

                if (isStartedGame)
                    Utils.NotifyRoles(SpecifySeer: _Player, SpecifyTarget: target);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Not enough targets for Head Hunter could be assigned. This may be due to a low player count or the following error:\n\n{ex}", "HuntsmanAssignTargets");
                break;
            }
        }
    }

    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target)
        => !IsDead && Targets.Contains(target.PlayerId) ? "6e5524" : string.Empty;
}
