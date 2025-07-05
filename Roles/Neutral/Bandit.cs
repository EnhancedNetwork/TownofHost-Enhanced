using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Bandit : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Bandit;
    private const int Id = 16000;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Bandit);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldownOpt;
    private static OptionItem StealCooldown;
    private static OptionItem MaxSteals;
    private static OptionItem StealMode;
    private static OptionItem CanStealBetrayalAddon;
    private static OptionItem CanStealImpOnlyAddon;
    private static OptionItem CanUsesSabotage;
    private static OptionItem CanVent;

    private float killCooldown;
    private readonly Dictionary<byte, CustomRoles> Targets = [];

    [Obfuscation(Exclude = true)]
    private enum BanditStealModeOptList
    {
        BanditStealMode_OnMeeting,
        BanditStealMode_Instantly
    }

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Bandit);
        MaxSteals = IntegerOptionItem.Create(Id + 10, "BanditMaxSteals", new(1, 20, 1), 6, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bandit]);
        KillCooldownOpt = FloatOptionItem.Create(Id + 11, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bandit])
            .SetValueFormat(OptionFormat.Seconds);
        StealCooldown = FloatOptionItem.Create(Id + 17, "BanditStealCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bandit])
            .SetValueFormat(OptionFormat.Seconds);
        StealMode = StringOptionItem.Create(Id + 12, "BanditStealMode", EnumHelper.GetAllNames<BanditStealModeOptList>(), 0, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bandit]);
        CanStealBetrayalAddon = BooleanOptionItem.Create(Id + 13, "BanditCanStealBetrayalAddon", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bandit]);
        CanStealImpOnlyAddon = BooleanOptionItem.Create(Id + 14, "BanditCanStealImpOnlyAddon", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bandit]);
        CanUsesSabotage = BooleanOptionItem.Create(Id + 15, GeneralOption.CanUseSabotage, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bandit]);
        CanVent = BooleanOptionItem.Create(Id + 16, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bandit]);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(MaxSteals.GetInt());
        killCooldown = KillCooldownOpt.GetFloat();

        var pc = playerId.GetPlayer();
        pc?.AddDoubleTrigger();
    }
    public override void SetKillCooldown(byte id)
    {
        Main.AllPlayerKillCooldown[id] = killCooldown;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(false);
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override bool CanUseSabotage(PlayerControl pc) => CanUsesSabotage.GetBool();
    public override bool CanUseKillButton(PlayerControl pc) => true;

    private static CustomRoles? SelectRandomAddon(PlayerControl killer, PlayerControl Target)
    {
        if (!AmongUsClient.Instance.AmHost) return null;

        var AllSubRoles = Target.GetCustomSubRoles().ToList();
        killer.CheckConflictedAddOnsFromList(ref AllSubRoles);

        foreach (var subRole in AllSubRoles.ToList())
        {
            if (subRole is CustomRoles.Cleansed or // making Bandit unable to steal Cleansed for obvious reasons. Although it can still be cleansed by cleanser.
                CustomRoles.LastImpostor or
                CustomRoles.Lovers or // Causes issues involving Lovers Suicide
                CustomRoles.Narc
                || (subRole is CustomRoles.Nimble && CanVent.GetBool())
                || (subRole.IsImpOnlyAddon() && !CanStealImpOnlyAddon.GetBool())
                || ((subRole.IsBetrayalAddon() || subRole is CustomRoles.Lovers) && !CanStealBetrayalAddon.GetBool()))
            {
                Logger.Info($"Removed {subRole} from list of stealable addons", "Bandit");
                AllSubRoles.Remove(subRole);
            }
        }

        if (AllSubRoles.Count == 0)
        {
            Logger.Info("No stealable addons found on the target.", "Bandit");
            return null;
        }
        var addon = AllSubRoles.RandomElement();
        return addon;
    }
    public void SendRPC(byte targetId, CustomRoles SelectedAddOn, bool removeNow)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(removeNow);
        if (removeNow)
        {
            writer.Write(targetId);
            writer.WritePacked((int)SelectedAddOn);
        }
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        bool removeNow = reader.ReadBoolean();
        if (removeNow)
        {
            byte targetId = reader.ReadByte();
            var SelectedAddOn = (CustomRoles)reader.ReadPackedInt32();

            Main.PlayerStates[targetId].RemoveSubRole(SelectedAddOn);
        }
    }
    private void StealAddon(PlayerControl killer, PlayerControl target, CustomRoles? SelectedAddOn)
    {
        target.AddInSwitchAddons(killer, SelectedAddOn ?? CustomRoles.NotAssigned);

        if (StealMode.GetValue() == 1)
        {
            Main.PlayerStates[target.PlayerId].RemoveSubRole((CustomRoles)SelectedAddOn);
            Logger.Info($"Successfully removed {SelectedAddOn} addon from {target.GetNameWithRole()}", "Bandit");

            killer.RpcSetCustomRole((CustomRoles)SelectedAddOn, false, false);
            Logger.Info($"Successfully Added {SelectedAddOn} addon to {killer.GetNameWithRole()}", "Bandit");
        }
        else
        {
            Targets[target.PlayerId] = (CustomRoles)SelectedAddOn;
            Logger.Info($"{killer.GetNameWithRole()} will steal {SelectedAddOn} addon from {target.GetNameWithRole()} after meeting starts", "Bandit");
        }
        killer.RpcRemoveAbilityUse();
        SendRPC(target.PlayerId, (CustomRoles)SelectedAddOn, StealMode.GetValue() == 1);

        Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
        Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);

        killer.ResetKillCooldown();
        killer.SetKillCooldown();

        if (!DisableShieldAnimations.GetBool())
            killer.RpcGuardAndKill(target);

        return;
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        bool flag = false;
        if (!target.HasSubRole() || target.Is(CustomRoles.Stubborn)) flag = true;

        var SelectedAddOn = SelectRandomAddon(killer, target);
        if (SelectedAddOn == null || flag) // no stealable addons found on the target.
        {
            killer.Notify(Translator.GetString("Bandit_NoStealableAddons"));
            killCooldown = KillCooldownOpt.GetFloat();
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            return true;
        }
        if (killer.GetAbilityUseLimit() < 1)
        {
            Logger.Info("Max steals reached killing the player", "Bandit");
            killCooldown = KillCooldownOpt.GetFloat();
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            return true;
        }

        if (killer.CheckDoubleTrigger(target, () => { StealAddon(killer, target, SelectedAddOn); }))
        {
            // Double click
            killCooldown = KillCooldownOpt.GetFloat();
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            return true;
        }
        else
        {
            // Single click
            killCooldown = StealCooldown.GetFloat();
            return false;
        }
    }

    public override void OnReportDeadBody(PlayerControl reportash, NetworkedPlayerInfo panagustava)
    {
        if (StealMode.GetValue() == 1 || _Player == null) return;
        foreach (var kvp2 in Targets)
        {
            byte targetId = kvp2.Key;
            var target = Utils.GetPlayerById(targetId);
            if (target == null) continue;

            CustomRoles role = kvp2.Value;
            Main.PlayerStates[targetId].RemoveSubRole(role);
            Logger.Info($"Successfully removed {role} addon from {target.GetNameWithRole()}", "Bandit");
            SendRPC(targetId, role, true);

            _Player.RpcSetCustomRole(role, false, false);
            Logger.Info($"Successfully Added {role} addon to {_Player?.GetNameWithRole()}", "Bandit");
        }
    }
}
