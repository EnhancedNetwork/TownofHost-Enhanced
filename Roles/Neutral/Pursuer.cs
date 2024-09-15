using AmongUs.GameOptions;
using UnityEngine;
using TOHE.Modules;
using static TOHE.Translator;
using TOHE.Roles.Core;

namespace TOHE.Roles.Neutral;

internal class Pursuer : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 13400;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Pursuer);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    private static OptionItem PursuerSkillCooldown;
    private static OptionItem PursuerSkillLimitTimes;

    private readonly HashSet<byte> notActiveList = [];
    private readonly HashSet<byte> clientList = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Pursuer);
        PursuerSkillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.AbilityCooldown, new(2.5f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pursuer])
            .SetValueFormat(OptionFormat.Seconds);
        PursuerSkillLimitTimes = IntegerOptionItem.Create(Id + 11, GeneralOption.SkillLimitTimes, new(1, 20, 1), 2, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pursuer])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        notActiveList.Clear();
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = PursuerSkillLimitTimes.GetInt();
    }
    public override bool CanUseKillButton(PlayerControl pc) => CanUseKillButton(pc.PlayerId);
    
    public bool CanUseKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && AbilityLimit >= 1;
    public override string GetProgressText(byte playerId, bool cooms) => Utils.ColorString(CanUseKillButton(playerId) ? Utils.GetRoleColor(CustomRoles.Pursuer) : Color.gray, $"({AbilityLimit})");
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? PursuerSkillCooldown.GetFloat() : 300f;
    public bool IsClient(byte playerId)
    {
        return clientList.Contains(playerId);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(true);
    public bool CanBeClient(PlayerControl pc) => pc != null && pc.IsAlive() && !GameStates.IsMeeting && !IsClient(pc.PlayerId);
    public bool CanSeel(byte playerId) => AbilityLimit > 0;
    public override bool OnCheckMurderAsKiller(PlayerControl pc, PlayerControl target)
    {
        if (pc == null || target == null || !pc.Is(CustomRoles.Pursuer)) return false;
        if (target.Is(CustomRoles.Pestilence) || target.Is(CustomRoles.SerialKiller)) return false;
        if (!(CanBeClient(target) && CanSeel(pc.PlayerId))) return false;

        AbilityLimit--;
        SendSkillRPC();
        if (target.Is(CustomRoles.KillingMachine)) 
        {
            Logger.Info("target is Killing Machine, ability used count reduced, but target will not die", "Purser");
            return false; 
        }

        clientList.Add(target.PlayerId);

        if (!Options.DisableShieldAnimations.GetBool())
            pc.RpcGuardAndKill(pc);

        notActiveList.Add(pc.PlayerId);

        pc.SetKillCooldown();
        pc.RPCPlayCustomSound("Bet");

        Utils.NotifyRoles(SpecifySeer: pc);
        Logger.Info($"Counterfeiters {pc.GetRealName()} sell counterfeits to {target.GetRealName()}", "Pursuer");
        return false;
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl pc, PlayerControl _)  // Target of Pursuer attempt to murder someone
    {
        if (!IsClient(pc.PlayerId) || notActiveList.Contains(pc.PlayerId)) return false;
        
        byte cfId = byte.MaxValue;
        foreach (var cf in clientList)
            if (cf == pc.PlayerId) cfId = cf;
        
        if (cfId == byte.MaxValue) return false;
        
        var killer = Utils.GetPlayerById(cfId);
        var target = pc;
        if (killer == null) return false;

        target.SetDeathReason(PlayerState.DeathReason.Misfire);
        target.RpcMurderPlayer(target);
        target.SetRealKiller(killer);

        Logger.Info($"赝品商 {pc.GetRealName()} 的客户 {target.GetRealName()} 因使用赝品走火自杀", "Pursuer");
        return true;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("PursuerButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Pursuer");
}