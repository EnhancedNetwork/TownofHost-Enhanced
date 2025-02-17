using AmongUs.GameOptions;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Hater : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Hater;
    private const int Id = 12900;
    public static readonly HashSet<byte> playerIdList = [];

    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    private static OptionItem ChooseConverted;
    private static OptionItem MisFireKillTarget;
    private static OptionItem CanKillLovers;
    private static OptionItem CanKillMadmate;
    private static OptionItem CanKillCharmed;
    private static OptionItem CanKillAdmired;
    private static OptionItem CanKillSidekicks;
    private static OptionItem CanKillEgoists;
    private static OptionItem CanKillInfected;
    private static OptionItem CanKillContagious;
    private static OptionItem CanKillEnchanted;

    public static bool isWon = false; // There's already a playerIdList, so replaced this with a boolean value

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Hater, zeroOne: false);
        MisFireKillTarget = BooleanOptionItem.Create(Id + 11, "HaterMisFireKillTarget", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hater]);
        ChooseConverted = BooleanOptionItem.Create(Id + 12, "HaterChooseConverted", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hater]);
        CanKillMadmate = BooleanOptionItem.Create(Id + 13, "HaterCanKillMadmate", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
        CanKillCharmed = BooleanOptionItem.Create(Id + 14, "HaterCanKillCharmed", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
        CanKillLovers = BooleanOptionItem.Create(Id + 15, "HaterCanKillLovers", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
        CanKillSidekicks = BooleanOptionItem.Create(Id + 16, "HaterCanKillSidekick", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
        CanKillEgoists = BooleanOptionItem.Create(Id + 17, "HaterCanKillEgoist", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
        CanKillInfected = BooleanOptionItem.Create(Id + 18, "HaterCanKillInfected", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
        CanKillContagious = BooleanOptionItem.Create(Id + 19, "HaterCanKillContagious", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
        CanKillAdmired = BooleanOptionItem.Create(Id + 20, "HaterCanKillAdmired", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
        CanKillEnchanted = BooleanOptionItem.Create(Id + 21, "HaterCanKillEnchanted", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
    }

    public override void Init()
    {
        playerIdList.Clear();
        isWon = false;
    }

    public override void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (killer.PlayerId == target.PlayerId) return true;  // Return true to allow suicides

        if (target.GetCustomSubRoles().Any(addOn => addOn.IsConverted() || addOn is CustomRoles.Madmate or CustomRoles.Admired or CustomRoles.Lovers)
            || IsConvertedMainRole(target.GetCustomRole()))
        {
            if (!ChooseConverted.GetBool())
            {
                isWon = true; // Only win if target can be killed - this kills the target if they can be killed
                Logger.Info($"{killer.GetRealName()} killed right target case 1", "Hater");
                return true;
            }
            else if (
                ((target.Is(CustomRoles.Madmate) || target.Is(CustomRoles.Gangster)) && CanKillMadmate.GetBool())
                || ((target.Is(CustomRoles.Charmed) || target.Is(CustomRoles.Cultist)) && CanKillCharmed.GetBool())
                || (target.Is(CustomRoles.Lovers) && CanKillLovers.GetBool())
                || ((target.Is(CustomRoles.Romantic) || target.Is(CustomRoles.RuthlessRomantic) || target.Is(CustomRoles.VengefulRomantic)
                || Romantic.BetPlayer.ContainsValue(target.PlayerId)) && CanKillLovers.GetBool())
                || ((target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.Recruit)) && CanKillSidekicks.GetBool())
                || (target.Is(CustomRoles.Egoist) && CanKillEgoists.GetBool())
                || ((target.Is(CustomRoles.Infected) || target.Is(CustomRoles.Infectious)) && CanKillInfected.GetBool())
                || ((target.Is(CustomRoles.Contagious) || target.Is(CustomRoles.Virus)) && CanKillContagious.GetBool())
                || ((target.Is(CustomRoles.Admired) || target.Is(CustomRoles.Admirer)) && CanKillAdmired.GetBool())
                || ((target.Is(CustomRoles.Enchanted) || target.Is(CustomRoles.Ritualist)) && CanKillEnchanted.GetBool())
                )
            {
                isWon = true; // Only win if target can be killed - this kills the target if they can be killed
                Logger.Info($"{killer.GetRealName()} killed right target case 2", "Hater");
                return true;
            }
        }
        if (MisFireKillTarget.GetBool())
        {
            target.SetDeathReason(PlayerState.DeathReason.Misfire);
            killer.RpcMurderPlayer(target); // Murder the target only if the setting is on and the target can be killed
        }

        killer.SetDeathReason(PlayerState.DeathReason.Sacrifice);
        killer.RpcMurderPlayer(killer);

        Logger.Info($"{killer.GetRealName()} killed incorrect target => misfire", "Hater");
        return false;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(true);
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("HaterButtonText"));
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = 1f;
    private static bool IsConvertedMainRole(CustomRoles role)
    {
        return role switch  // Use the switch expression whenever possible instead of the switch statement to improve performance
        {
            CustomRoles.Gangster or
            CustomRoles.Cultist or
            CustomRoles.Romantic or
            CustomRoles.RuthlessRomantic or
            CustomRoles.VengefulRomantic or
            CustomRoles.Sidekick or
            CustomRoles.Jackal or
            CustomRoles.Virus or
            CustomRoles.Infectious or
            CustomRoles.Admirer or
            CustomRoles.Ritualist
            => true,

            _ => false,
        };
    }
}
