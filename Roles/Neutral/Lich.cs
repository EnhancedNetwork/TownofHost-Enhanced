using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using TOHE.Roles.AddOns.Impostor;
using static TOHE.Options;
using static TOHE.Translator;
using TOHE.Roles.Coven;
using TOHE.Roles.Crewmate;
using TOHE.Roles.AddOns.Common;

namespace TOHE.Roles.Neutral;

internal class Lich : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Lich;
    private const int Id = 32100;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Lich);
    public override bool IsExperimental => true;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralApocalypse;
    //==================================================================\\
    private static OptionItem LichPointsOpt;
    private static OptionItem GetPassiveCharges;
    public static OptionItem LichCanVent;
    private static OptionItem LichHasImpostorVision;

    private static byte TargetId;
    private static PlayerControl LichPlayer;

    // Roles Decieved (that are implemented so far): Necroview, Sleuth, CopyCat, Forensic, Enigma, FortuneTeller, Inspector, Investigator, Oracle, Psychic, Snitch, Consigliere, Visionary
    // Probably Decieved (needs testing): Mimic, Potion Master, Overseer, etc.

    // Not Decieved: Apoc, Lovers, Admirer, Executioner, Lawyer, Follower, Infectious, Jackal, Virus, Romantic, SchrodingersCat

    // Not Decided Yet: God, Revolutionist

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Lich, 1, zeroOne: false);
        LichPointsOpt = IntegerOptionItem.Create(Id + 10, "LichPointsToWin", new(1, 14, 1), 6, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lich])
            .SetValueFormat(OptionFormat.Times);
        GetPassiveCharges = BooleanOptionItem.Create(Id + 11, "GetPassiveCharges", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lich]);
        LichCanVent = BooleanOptionItem.Create(Id + 12, "LichCanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lich]);
        // Bind to SoulCollector setting
        // _ = IntegerOptionItem.Create(15313, "DeathMeetingTimeIncrease", new(0, 120, 1), 0, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SoulCollector])
        //     .SetValueFormat(OptionFormat.Seconds); 
        LichHasImpostorVision = BooleanOptionItem.Create(Id + 14, "LichHasImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lich]);
    }

    public override void Init()
    {
        TargetId = byte.MaxValue;
    }

    public override void Add(byte playerId)
    {
        TargetId = byte.MaxValue;
        playerId.SetAbilityUseLimit(0);
        LichPlayer = _Player;
    }

    public override void Remove(byte playerId)
    {
        LichPlayer = null;
    }

    public override string GetProgressText(byte playerId, bool comms) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lich).ShadeColor(0.25f), $"({playerId.GetAbilityUseLimit()}/{LichPointsOpt.GetInt()})");

    private void SendRPC()
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(TargetId);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte target = reader.ReadByte();

        TargetId = target;
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        => TargetId == seen.PlayerId ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lich), "§") : string.Empty;

    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (_Player == null) return string.Empty;
        if (TargetId == target.PlayerId && seer.IsNeutralApocalypse() && seer.PlayerId != _Player.PlayerId && !Main.PlayerStates[seer.PlayerId].IsNecromancer)
        {
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lich), "§");
        }
        return string.Empty;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
        => opt.SetVision(LichHasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => pc.Is(CustomRoles.Lich);
    public override bool CanUseImpostorVentButton(PlayerControl pc) => LichCanVent.GetBool();
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (target.IsNeutralApocalypse())
        {
            killer.Notify(string.Format(GetString("LichCantTargetApoc"), target.GetRealName()));
            return false;
        }
        if (TargetId != byte.MaxValue)
        {
            killer.Notify(string.Format(GetString("LichTargetUsed"), target.GetRealName()));
        }
        else
        {
            killer.Notify(string.Format(GetString("LichTarget"), target.GetRealName()));
        }
        TargetId = target.PlayerId;
        SendRPC();
        Logger.Info($"{killer.GetNameWithRole()} cursed {target.GetNameWithRole()}", "Lich");
        return false;
    }
    public override void OnReportDeadBody(PlayerControl player, NetworkedPlayerInfo netInf)
    {
        if (_Player == null || !_Player.IsAlive() || !GetPassiveCharges.GetBool()) return;

        _Player.RpcIncreaseAbilityUseLimitBy(1);
    }
    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (!pc.IsAlive() || !GetPassiveCharges.GetBool()) return;

        MeetingHudStartPatch.AddMsg(GetString("PassiveChargeGained"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lich), GetString("Lich").ToUpper()));
    }

    private void OnTargetVote()
    {
        if (_Player == null || !_Player.IsAlive()) return;
        if (TargetId == byte.MaxValue) return;

        var playerId = _Player.PlayerId;

        TargetId = byte.MaxValue;
        _Player.RpcIncreaseAbilityUseLimitBy(1);

        SendRPC();
        _Player.Notify(GetString("LichChargeGained"));
    }

    public static void OnTargetVote(PlayerControl target)
    {
        if (LichPlayer == null || !LichPlayer.IsAlive()) return;

        if (target.PlayerId == TargetId)
            (LichPlayer.GetRoleClass() as Lich).OnTargetVote();
    }

    public static bool IsCursed(PlayerControl player) => player.PlayerId == TargetId;
    public static bool IsDeceived(PlayerControl seer, PlayerControl target)
    {
        if (seer == null || target == null) return false;
        if (seer == target) return false;
        if (!seer.IsAlive()) return false;

        if (seer.Is(CustomRoles.GM) || target.Is(CustomRoles.GM)) return false;

        bool targetHasState = Main.PlayerStates.TryGetValue(target.PlayerId, out var targetState);
        bool seerHasState = Main.PlayerStates.TryGetValue(seer.PlayerId, out var seerState);

        if (seer.IsNeutralApocalypse()) return false;
        if (target.GetCustomRole().IsRevealingRole(seer) || target.IsAnySubRole(role => role.IsRevealingRole(seer))) return false;

        // Imposter Team
        if (seer.CheckImpCanSeeAllies(CheckAsSeer: true) && target.CheckImpCanSeeAllies(CheckAsTarget: true)) return false;
        if (seer.Is(CustomRoles.Madmate) && target.CheckImpCanSeeAllies(CheckAsTarget: true) && Madmate.MadmateKnowWhosImp.GetBool()) return false;
        if (seer.CheckImpCanSeeAllies(CheckAsSeer: true) && target.Is(CustomRoles.Madmate) && Madmate.ImpKnowWhosMadmate.GetBool()) return false;
        if (seer.CheckImpCanSeeAllies(CheckAsSeer: true) && target.GetCustomRole().IsGhostRole() && target.GetCustomRole().IsImpostor()) return false;
        if (seer.Is(CustomRoles.Madmate) && target.Is(CustomRoles.Madmate) && Madmate.MadmateKnowWhosMadmate.GetBool()) return false;

        // Coven Team
        if (seer.Is(Custom_Team.Coven) && target.Is(Custom_Team.Coven)) return false;
        if (seer.Is(CustomRoles.Enchanted) && target.Is(Custom_Team.Coven) && Ritualist.EnchantedKnowsCoven.GetBool()) return false;
        if (seerHasState && seerState.IsNecromancer && target.Is(Custom_Team.Coven)) return false;
        if (targetHasState && targetState.IsNecromancer && seer.Is(Custom_Team.Coven)) return false;
        if (seer.Is(Custom_Team.Coven) && target.Is(CustomRoles.Enchanted)) return false;
        if (seerHasState && seerState.IsNecromancer && target.Is(CustomRoles.Enchanted)) return false;
        if (targetHasState && targetState.IsNecromancer && seer.Is(CustomRoles.Enchanted)) return false;
        if (seer.Is(CustomRoles.Enchanted) && target.Is(CustomRoles.Enchanted) && Ritualist.EnchantedKnowsEnchanted.GetBool()) return false;

        // Cultist
        if (Cultist.NameRoleColor(seer, target)) return false;

        // Admirer
        if (seer.Is(CustomRoles.Admirer) && !(seerHasState && seerState.IsNecromancer) && target.Is(CustomRoles.Admired)) return false;
        if (seer.Is(CustomRoles.Admired) && target.Is(CustomRoles.Admirer) && !(seerHasState && seerState.IsNecromancer)) return false;

        // Infectious
        if (Infectious.InfectedKnowColorOthersInfected(seer, target)) return false;

        // Jackal recruit
        if (Jackal.JackalKnowRole(seer, target)) return false;

        //Virus
        if (Virus.KnowRoleColor(seer, target) != "") return false;

        // Narc & Sheriff/ChiefOfPolice
        if (NarcManager.KnowRoleOfTarget(seer, target)) return false;

        if (Main.GodMode.Value && seer.IsHost()) return false;
        if (CurrentGameMode == CustomGameMode.FFA) return false;

        if (target.GetRoleClass().OthersKnowTargetRoleColor(seer, target)) return false;
        if (Workaholic.OthersKnowWorka(target)) return false;
        if (target.Is(CustomRoles.Gravestone) && targetHasState && targetState.IsDead) return false;

        // if (player.GetBetrayalAddon(forRecruiter: true) != CustomRoles.NotAssigned && player.GetBetrayalAddon(true) == target.GetBetrayalAddon(true)) return false;
        if (seer.IsLoverWith(target)) return false;

        if (seer.Is(CustomRoles.Executioner) && (seer.GetRoleClass() as Executioner).IsTarget(target.PlayerId)) return false;
        if (seer.Is(CustomRoles.Lawyer) && (seer.GetRoleClass() as Lawyer).IsTarget(target.PlayerId)) return false;

        if (seer.Is(CustomRoles.Follower) && Follower.BetPlayer.TryGetValue(seer.PlayerId, out var followed) && followed == target.PlayerId) return false;
        if (Follower.BetPlayer.TryGetValue(seer.PlayerId, out var followed2) && followed2 == seer.PlayerId && target.Is(CustomRoles.Follower)) return false;

        if (seer.Is(CustomRoles.Romantic) && seer.GetRoleClass().KnowRoleTarget(seer, target)) return false;
        if (seer.Is(CustomRoles.SchrodingersCat) && seer.GetRoleClass().KnowRoleTarget(seer, target)) return false;

        return true;
    }

    public override void AfterMeetingTasks()
    {
        if (_Player == null || !_Player.IsAlive()) return;
        TargetId = byte.MaxValue;
        SendRPC();

        var player = _Player;
        if (player.GetAbilityUseLimit() >= LichPointsOpt.GetInt() && !player.Is(CustomRoles.Death))
        {
            player.RpcSetCustomRole(CustomRoles.Death);
            player.GetRoleClass()?.OnAdd(player.PlayerId);

            player.Notify(GetString("SoulCollectorToDeath"));
            player.RpcGuardAndKill(player);
        }
    }
    
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.KillButton.OverrideText(GetString("LichKillButtonText"));
    }
}