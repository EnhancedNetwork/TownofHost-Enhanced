using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class Knight : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Knight;
    private const int Id = 10800;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Knight);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    public static OptionItem CanVent;
    public static OptionItem KillCooldown;
    public static OptionItem RequiterChance;
    public static OptionItem RequiterCanKillTNA;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Knight);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 60f, 2.5f), 15f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Knight])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Knight]);
        RequiterChance = IntegerOptionItem.Create(Id + 12, "RequiterChance", new(0, 100, 5), 0, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Knight])
            .SetValueFormat(OptionFormat.Percent);
        RequiterCanKillTNA = BooleanOptionItem.Create(Id + 13, "CanKillTNA", false, TabGroup.CrewmateRoles, false)
            .SetParent(RequiterChance);        
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(1);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public static bool CheckCanUseVent(PlayerControl player) => player.Is(CustomRoles.Knight) && CanVent.GetBool();
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CheckCanUseVent(pc);

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = IsKilled(id) ? 300f : KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc)
        => !IsKilled(pc.PlayerId);

    private static bool IsKilled(byte playerId) => playerId.GetAbilityUseLimit() <= 0;
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl banana)
    {
        killer.RpcRemoveAbilityUse();
        Logger.Info($"{killer.GetNameWithRole()} : " + "Kill chance used", "Knight");
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        return true;
    }
}

internal class Requiter : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Requiter;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(0);
    }

    public static bool CheckSpawn()
    {
        var Rand = IRandom.Instance;
        return Rand.Next(1, 100) <= Knight.RequiterChance.GetInt();
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override bool CanUseImpostorVentButton(PlayerControl pc) => Knight.CanVent.GetBool();

    public override void SetKillCooldown(byte id) => Knight.KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc)
        => pc.GetAbilityUseLimit() > 0;

    public override void OnPlayerExiled(PlayerControl player, NetworkedPlayerInfo exiled)
    { 
        if (exiled == null || exiled.Object == null || exiled.Object == player || !player.IsAlive()) return;
        if (exiled.Object.IsPlayerCrewmateTeam())
            player.RpcIncreaseAbilityUseLimitBy(1);
    }

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.Solsticer)) return true;
        if ((target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)) && Mini.Age < 18) return true;
        if (Knight.RequiterCanKillTNA.GetBool())
        {
            killer.RpcMurderPlayer(target);
            killer.ResetKillCooldown();
            return false;
        }
        return true;
    }

    public override void OnMurderPlayerAsKiller(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    { 
        if (inMeeting || isSuicide) return;
        killer.RpcRemoveAbilityUse();
        target.SetDeathReason(PlayerState.DeathReason.Retribution);
    }
}
