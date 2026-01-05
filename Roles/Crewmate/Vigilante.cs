using TOHE.Roles.AddOns.Common;
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
    private static OptionItem ConvertedVigilanteCanVent;
    private static OptionItem CanSabotageAsRecruit;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Vigilante);
        VigilanteKillCooldown = FloatOptionItem.Create(Id + 2, GeneralOption.KillCooldown, new(5f, 180f, 2.5f), 30f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Vigilante])
            .SetValueFormat(OptionFormat.Seconds);
        ConvertedVigilanteCanVent = BooleanOptionItem.Create(Id + 3, "ConvertedVigilanteCanVent", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Vigilante]);
        CanSabotageAsRecruit = BooleanOptionItem.Create(Id + 4, "CanSabotageAsRecruit", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Vigilante]);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = VigilanteKillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public static bool CheckCanUseVent(PlayerControl player) => player.Is(CustomRoles.Madmate) || player.IsAnySubRole(sub => sub.IsConverted() && ConvertedVigilanteCanVent.GetBool());
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CheckCanUseVent(pc);
    public override bool CanUseSabotage(PlayerControl player) => player.Is(CustomRoles.Madmate) || player.Is(CustomRoles.Recruit) && CanSabotageAsRecruit.GetBool();
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!killer.IsPlayerCrewmateTeam()) return true;
        if (target.IsPlayerCrewmateTeam())
        {
            killer.RpcSetCustomRole(CustomRoles.Madmate);
            _ = new LateTask(() =>
            {
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("VigilanteNotify")));
            }, target.Is(CustomRoles.Burst) ? Burst.BurstKillDelay.GetFloat() : 0f, "BurstKillCheck");
            Utils.MarkEveryoneDirtySettings();
        }
        return true;
    }
}
