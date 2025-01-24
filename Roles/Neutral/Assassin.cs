using TOHE.Modules;
using static TOHE.Translator;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Assassin : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 31900;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();

    public override CustomRoles Role => CustomRoles.Assassin;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem AssassinKillCooldown;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Assassin);
        AssassinKillCooldown = FloatOptionItem.Create(Id + 2, GeneralOption.KillCooldown, new(5f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Assassin])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(1);

        playerIdList.Add(playerId);

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = AssassinKillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() < 1) return false;
        if (killer.Is(CustomRoles.Madmate)) return true;
        if (target.GetCustomRole().IsCrewmate() && !target.Is(CustomRoles.Madmate) && !target.GetCustomRole().IsConverted())
        {
            killer.RpcSetCustomRole(CustomRoles.Madmate);
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.ImpostorTOHO), GetString("AssassinImpostorNotify")));
            //Utils.NotifyRoles(SpecifySeer: killer);
            Utils.MarkEveryoneDirtySettings();
            killer.SetAbilityUseLimit(0);
        }
        if (target.GetCustomRole().IsImpostor() && !target.Is(CustomRoles.Madmate) && !target.GetCustomRole().IsConverted())
        {
            killer.RpcSetCustomRole(CustomRoles.Sheriff);
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Sheriff), GetString("AssassinSheriffNotify")));
            //Utils.NotifyRoles(SpecifySeer: killer);
            Utils.MarkEveryoneDirtySettings();
            killer.SetAbilityUseLimit(0);
        }
        if (target.GetCustomRole().IsNeutral() && !target.Is(CustomRoles.Madmate) && !target.GetCustomRole().IsConverted())
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Assassin), GetString("AssassinNeutralNotify")));
            //Utils.NotifyRoles(SpecifySeer: killer);
            Utils.MarkEveryoneDirtySettings();
            killer.SetAbilityUseLimit(0);
        }
        return true;
    }
}
