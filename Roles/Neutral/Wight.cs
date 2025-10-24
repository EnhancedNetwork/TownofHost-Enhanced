using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Options;
using System;
using System.Diagnostics.Metrics;

namespace TOHE.Roles.Neutral;

internal class Wight : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Wight;
    private const int Id = 36400;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Wight);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem CanVent;
    public static OptionItem KillCooldownSK;
    public static OptionItem CanVentSK;
    public static OptionItem ReducedSpeedRound;

    public static HashSet<byte> UndeadIds = [];
    public static Dictionary<PlayerControl, PlayerControl> RealKillerW = [];
    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Wight, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wight])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wight]);
        KillCooldownSK = FloatOptionItem.Create(Id + 12, "UndeadKillCD364", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wight])
            .SetValueFormat(OptionFormat.Seconds);
        CanVentSK = BooleanOptionItem.Create(Id + 13, "UndeadVent364", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wight]);
        ReducedSpeedRound = FloatOptionItem.Create(Id + 14, "ReducedSpeedRound364", new(0.2f, 2f, 0.2f), 1f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Wight]) 
            .SetValueFormat(OptionFormat.Multiplier);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseSabotage(PlayerControl pc) => false;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        UndeadIds.Add(target.PlayerId);
        RealKillerW[target] = killer;
        return true;
    }

    public override void AfterMeetingTasks()
    {
        foreach (var id in UndeadIds)
        {
            var player = Utils.GetPlayerById(id);
            if (RealKillerW[player].IsAlive() && !player.IsAlive())
            {
                player.RpcRevive();
                player.RpcChangeRoleBasis(CustomRoles.Undead);
                player.RpcSetCustomRole(CustomRoles.Undead, true);
                var allAddons = player.GetCustomSubRoles();
                foreach (var role in allAddons)
                {
                    Main.PlayerStates[id].RemoveSubRole(role);
                }
            }

            else if (RealKillerW[player].IsAlive() && player.IsAlive())
            {
                Main.AllPlayerSpeed[id] -= (float)Math.Clamp(ReducedSpeedRound.GetFloat(), 0, (double)Main.AllPlayerSpeed[id] - 0.5);
                player.SetKillCooldown(KillCooldownSK.GetFloat());
            }

            else if (!RealKillerW[player].IsAlive() && player.IsAlive())
            {
                player.RpcExileV2();
                Main.PlayerStates[player.PlayerId].SetDead();
                player.Data.IsDead = true;
            }
        }
    }

    public static bool WightKnowRole(PlayerControl seer, PlayerControl target)
    {
        if (seer.Is(CustomRoles.Undead) && (target.Is(CustomRoles.Wight) || target.Is(CustomRoles.Undead))) return true;
        return false;
    }
}

internal class Undead : RoleBase
{
    public override CustomRoles Role => CustomRoles.Undead;

    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;

    public override void Add(byte playerId)
    {
        Main.PlayerStates[playerId].taskState.hasTasks = false;
        playerId.SetAbilityUseLimit(0);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Wight.KillCooldownSK.GetFloat();
    public override bool CanUseKillButton(PlayerControl player) => true;
    public override bool CanUseImpostorVentButton(PlayerControl player) => Wight.CanVentSK.GetBool();
    public override bool CanUseSabotage(PlayerControl player) => false;
    public override string GetProgressText(byte playerId, bool comms) => string.Empty;

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        Wight.UndeadIds.Remove(target.PlayerId);
        return true;
    }
}
