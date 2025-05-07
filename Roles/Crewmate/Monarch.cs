using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Monarch : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Monarch;
    private const int Id = 12100;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Monarch);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static OptionItem KnightCooldown;
    private static OptionItem KnightMax;
    public static OptionItem HideAdditionalVotesForKnighted;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Monarch, 1);
        KnightCooldown = FloatOptionItem.Create(Id + 10, "MonarchKnightCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Monarch])
            .SetValueFormat(OptionFormat.Seconds);
        KnightMax = IntegerOptionItem.Create(Id + 12, "MonarchKnightMax", new(1, 15, 1), 3, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Monarch])
            .SetValueFormat(OptionFormat.Times);
        HideAdditionalVotesForKnighted = BooleanOptionItem.Create(Id + 13, "HideAdditionalVotesForKnighted", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Monarch]);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(KnightMax.GetInt());
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = id.GetAbilityUseLimit() > 0 ? KnightCooldown.GetFloat() : 300f;
    public override bool CanUseKillButton(PlayerControl player) => player.GetAbilityUseLimit() > 0;

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        return !CustomRoles.Knighted.RoleExist();
    }
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() <= 0) return false;
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CantRecruit")));
            return false;
        }
        if (CanBeKnighted(target))
        {
            killer.RpcRemoveAbilityUse();
            target.RpcSetCustomRole(CustomRoles.Knighted);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Monarch), GetString("MonarchKnightedPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Monarch), GetString("KnightedByMonarch")));

            killer.ResetKillCooldown();
            killer.SetKillCooldown();

            target.RpcGuardAndKill(killer);
            target.SetKillCooldown(forceAnime: true);

            Logger.Info(target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Knighted.ToString(), "Assign " + CustomRoles.Knighted.ToString());
            return false;
        }

        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Monarch), GetString("MonarchInvalidTarget")));
        return false;
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser, CustomRoles role, ref bool guesserSuicide)
    {
        if (role == CustomRoles.Monarch && CustomRoles.Knighted.RoleExist())
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessMonarch"));
            return true;
        }
        return false;
    }
    private static bool CanBeKnighted(PlayerControl pc)
    {
        return pc != null && !pc.GetCustomRole().IsNotKnightable() &&
            !pc.IsAnySubRole(x => x is CustomRoles.Knighted or CustomRoles.Stubborn or CustomRoles.Stealer);
    }
    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target) => seer.Is(CustomRoles.Monarch) && target.Is(CustomRoles.Knighted) ? Main.roleColors[CustomRoles.Knighted] : "";

    public override bool GuessCheck(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        if (role == CustomRoles.Knighted)
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessKnighted"));
            return true;
        }
        return false;
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (playerId.GetAbilityUseLimit() > 0)
            hud.KillButton.OverrideText(GetString("MonarchKillButtonText"));
    }
}
