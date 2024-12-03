using AmongUs.GameOptions;
using TOHE.Roles.Double;
using UnityEngine;

using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Infectious : RoleBase
{
    //===========================SETUP================================\\
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

    private static int BiteLimit;

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
        BiteLimit = 0;
        PlayerIds.Clear();
    }
    public override void Add(byte playerId)
    {
        BiteLimit = BiteMax.GetInt();
        PlayerIds.Add(playerId);
        var pc = Utils.GetPlayerById(playerId);
        pc?.AddDoubleTrigger();
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(HasImpostorVision.GetBool());

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = BiteCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl player) => BiteLimit >= 1;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();

    private static bool InfectOrMurder(PlayerControl killer, PlayerControl target)
    {
        if (CanBeBitten(target))
        {
            BiteLimit--;
            target.RpcSetCustomRole(CustomRoles.Infected);

            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
            Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Infectious), GetString("InfectiousBittenPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Infectious), GetString("BittenByInfectious")));

            killer.ResetKillCooldown();
            killer.SetKillCooldown();

            if (!DisableShieldAnimations.GetBool())
                killer.RpcGuardAndKill(target);

            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);
            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Infected.ToString(), "Assign " + CustomRoles.Infected.ToString());

            if (BiteLimit < 0)
            {
                HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
            }

            Logger.Info($"{killer.GetNameWithRole()} : 剩余{BiteLimit}次招募机会", "Infectious");
            return true;
        }

        if (!CanBeBitten(target) && !target.Is(CustomRoles.Infected))
        {
            killer.RpcMurderPlayer(target);
        }

        if (BiteLimit < 0)
        {
            HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
        }

        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Infectious), GetString("InfectiousInvalidTarget")));

        Logger.Info($"{killer.GetNameWithRole()} : 剩余{BiteLimit}次招募机会", "Infectious");
        return false;
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.IsTransformedNeutralApocalypse()) return true;
        if (target.Is(CustomRoles.Infectious)) return true;
        if (target.Is(CustomRoles.SerialKiller)) return true;

        if (BiteLimit < 1) return false;
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CantRecruit")));
            return false;
        }
        if (DoubleClickKill.GetBool())
        {
            bool check = killer.CheckDoubleTrigger(target, () => { InfectOrMurder(killer, target); });
            //Logger.Warn("VALUE OF CHECK IS")
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
        if (!player.IsAlive())
        {
            foreach (var alivePlayer in Main.AllAlivePlayerControls.Where(pc => pc.Is(CustomRoles.Infected)))
            {
                alivePlayer.SetDeathReason(PlayerState.DeathReason.Infected);
                alivePlayer.RpcMurderPlayer(alivePlayer);
                alivePlayer.SetRealKiller(Utils.GetPlayerById(PlayerIds.First()));
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

    public override string GetProgressText(byte playerid, bool cooms) => Utils.ColorString(BiteLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Infectious).ShadeColor(0.25f) : Color.gray, $"({BiteLimit})");

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
            && !pc.Is(CustomRoles.Infectious) && !pc.Is(CustomRoles.Virus);
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("InfectiousKillButtonText"));
    }
}
