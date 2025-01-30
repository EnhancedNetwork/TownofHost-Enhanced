using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Vigilante : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Vigilante;
    private const int Id = 11400;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem VigilanteKillCooldown;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Vigilante);
        VigilanteKillCooldown = FloatOptionItem.Create(Id + 2, GeneralOption.KillCooldown, new(5f, 180f, 2.5f), 30f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Vigilante])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = VigilanteKillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.Is(CustomRoles.Madmate)) return true;
        if (target.GetCustomRole().IsCrewmate() && !target.Is(CustomRoles.Madmate) && !target.GetCustomRole().IsConverted())
        {
            killer.RpcSetCustomRole(CustomRoles.Madmate);
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("VigilanteNotify")));
            //Utils.NotifyRoles(SpecifySeer: killer);
            Utils.MarkEveryoneDirtySettings();
        }
        return true;
    }
}
