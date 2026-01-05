using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Infectious : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Infectious;
    private const int Id = 16600;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem BiteCooldown;
    private static OptionItem BiteMax;
    private static OptionItem KnowTargetRole;
    private static OptionItem TargetKnowOtherTarget;
    private static OptionItem HasImpostorVision;
    private static OptionItem CanVent;
    private static OptionItem DoubleClickKill;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Infectious, 1, zeroOne: false);
        BiteCooldown = FloatOptionItem.Create(Id + 10, "InfectiousBiteCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Infectious])
            .SetValueFormat(OptionFormat.Seconds);
        BiteMax = IntegerOptionItem.Create(Id + 12, "InfectiousBiteMax", new(1, 15, 1), 15, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Infectious])
            .SetValueFormat(OptionFormat.Times);
        KnowTargetRole = BooleanOptionItem.Create(Id + 13, "InfectiousKnowTargetRole", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Infectious]);
        TargetKnowOtherTarget = BooleanOptionItem.Create(Id + 14, "InfectiousTargetKnowOtherTarget", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Infectious]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 15, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Infectious]);
        CanVent = BooleanOptionItem.Create(Id + 17, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Infectious]);
        DoubleClickKill = BooleanOptionItem.Create(Id + 18, "DoubleClickKill", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Infectious]);
    }
    public override void Init()
    {
        PlayerIds.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(BiteMax.GetInt());

        if (!PlayerIds.Contains(playerId))
            PlayerIds.Add(playerId);

        var pc = Utils.GetPlayerById(playerId);
        pc?.AddDoubleTrigger();
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(HasImpostorVision.GetBool());

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = BiteCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl player) => player.GetAbilityUseLimit() >= 1;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();

    private static bool InfectOrMurder(PlayerControl killer, PlayerControl target)
    {
        var addon = killer.GetBetrayalAddon(true);
        if (target.CanBeRecruitedBy(killer))
        {
            killer.RpcRemoveAbilityUse();
            target.RpcSetCustomRole(addon);

            if (addon is CustomRoles.Admired)
            {
                Admirer.AdmiredList[killer.PlayerId].Add(target.PlayerId);
                Admirer.SendRPC(killer.PlayerId, target.PlayerId); //Sync playerId list
            }

            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
            Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(addon), GetString("InfectiousBittenPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(addon), GetString("BittenByInfectious")));

            killer.ResetKillCooldown();
            killer.SetKillCooldown();

            if (!DisableShieldAnimations.GetBool())
                killer.RpcGuardAndKill(target);

            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);
            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + addon.ToString(), "Assign " + addon.ToString());

            if (killer.GetAbilityUseLimit() < 0)
            {
                HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
            }

            return true;
        }

        if (!target.CanBeRecruitedBy(killer) && !target.Is(addon) && !target.IsTransformedNeutralApocalypse())
        {
            killer.RpcMurderPlayer(target);
        }

        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Infectious), GetString("InfectiousInvalidTarget")));

        return false;
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.Infectious)) return true;
        if (target.Is(CustomRoles.SerialKiller)) return true;

        if (killer.GetAbilityUseLimit() <= 0) return false;
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CantRecruit")));
            return false;
        }
        if (DoubleClickKill.GetBool())
        {
            bool check = killer.CheckDoubleTrigger(target, () => { InfectOrMurder(killer, target); });
            if (check)
            {
                killer.RpcMurderPlayer(target);
            }
        }
        else
        {
            InfectOrMurder(killer, target);
        }
        return false;
    }
    public override void OnPlayerExiled(PlayerControl player, NetworkedPlayerInfo exiled)
    {
        if (!player.IsAlive() && player.GetBetrayalAddon(true) is CustomRoles.Infected)
        {
            foreach (var alivePlayer in Main.AllAlivePlayerControls.Where(pc => pc.Is(CustomRoles.Infected)))
            {
                alivePlayer.SetDeathReason(PlayerState.DeathReason.Infected);
                alivePlayer.RpcMurderPlayer(alivePlayer);
                alivePlayer.SetRealKiller(player);
            }
        }
    }

    public static bool TargetKnowOtherTargets => TargetKnowOtherTarget.GetBool();

    public static bool KnowRole(PlayerControl player, PlayerControl target) // Addons know each-other
    {
        if (player.Is(CustomRoles.Infected) && target.Is(CustomRoles.Infectious)) return true;
        if (KnowTargetRole.GetBool() && player.Is(CustomRoles.Infectious) && target.Is(CustomRoles.Infected)) return true;
        if (TargetKnowOtherTarget.GetBool() && player.Is(CustomRoles.Infected) && target.Is(CustomRoles.Infected)) return true;
        return false;
    }
    public static bool InfectedKnowColorOthersInfected(PlayerControl player, PlayerControl target)
    {
        if (player.Is(CustomRoles.Infected) && target.Is(CustomRoles.Infectious)) return true;
        if (player.Is(CustomRoles.Infectious) && target.Is(CustomRoles.Infected)) return true;
        if (TargetKnowOtherTarget.GetBool() && player.Is(CustomRoles.Infected) && target.Is(CustomRoles.Infected)) return true;
        return false;
    }
    public static bool CanBeBitten(PlayerControl pc)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate()
            || pc.GetCustomRole().IsImpostor()
            || pc.GetCustomRole().IsNK()
            || pc.GetCustomRole().IsCoven()) && !pc.Is(CustomRoles.Infected)
            && !pc.Is(CustomRoles.Admired)
            && !pc.Is(CustomRoles.Loyal)
            && !pc.Is(CustomRoles.Cultist)
            && !pc.Is(CustomRoles.Enchanted)
            && !pc.Is(CustomRoles.Infectious) && !pc.Is(CustomRoles.Virus) && !pc.IsTransformedNeutralApocalypse() && !(CovenManager.HasNecronomicon(pc.PlayerId) && pc.Is(CustomRoles.CovenLeader));
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (playerId.GetAbilityUseLimit() > 0)
            hud.KillButton.OverrideText(GetString("InfectiousKillButtonText"));
        else
            hud.KillButton.OverrideText($"{GetString("KillButtonText")}");
    }
}
