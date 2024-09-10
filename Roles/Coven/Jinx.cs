using AmongUs.GameOptions;
using UnityEngine;
using static TOHE.Options;
using TOHE.Roles.Core;

namespace TOHE.Roles.Coven;

internal class Jinx : CovenManager
{
    //===========================SETUP================================\\
    private const int Id = 16800;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Jinx);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    private static OptionItem JinxSpellTimes;
    private static OptionItem killAttacker;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.Jinx, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jinx])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jinx]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jinx]);
        JinxSpellTimes = IntegerOptionItem.Create(Id + 14, "JinxSpellTimes", new(1, 15, 1), 3, TabGroup.CovenRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Jinx])
        .SetValueFormat(OptionFormat.Times);
        killAttacker = BooleanOptionItem.Create(Id + 15, GeneralOption.KillAttackerWhenAbilityRemaining, true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jinx]);

    }
    public override void Add(byte playerId)
    {
        AbilityLimit = JinxSpellTimes.GetInt();
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (AbilityLimit <= 0) return true;
        if (killer.IsTransformedNeutralApocalypse()) return true;
        if (killer == target) return true;
        
        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(target);
       
        AbilityLimit -= 1;
        SendSkillRPC();

        if (killAttacker.GetBool() && target.RpcCheckAndMurder(killer, true))
        {
            Logger.Info($"{target.GetNameWithRole()}: ability left {AbilityLimit}", "Jinx");
            killer.SetDeathReason(PlayerState.DeathReason.Jinx);
            killer.RpcMurderPlayer(killer);
            killer.SetRealKiller(target);
        }
        return false;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte babushka) => opt.SetVision(HasImpostorVision.GetBool());

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl player) => CanVent.GetBool();


    public override string GetProgressText(byte playerId, bool comms) 
        => Utils.ColorString(CanJinx(playerId) ? Utils.GetRoleColor(CustomRoles.Gangster).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");
    
    private bool CanJinx(byte id) => AbilityLimit > 0;
}
