using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Imitator : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Imitator;
    private const int Id = 13000;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Imitator);
    public override bool IsExperimental => true;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    private static OptionItem RememberCooldown;
    private static OptionItem IncompatibleNeutralMode;

    [Obfuscation(Exclude = true)]
    private enum ImitatorIncompatibleNeutralModeSelectList
    {
        Role_Imitator,
        Role_Pursuer,
        Role_Follower,
        Role_Maverick,
        Role_Amnesiac
    }

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Imitator);
        RememberCooldown = FloatOptionItem.Create(Id + 10, "RememberCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Imitator])
                .SetValueFormat(OptionFormat.Seconds);
        IncompatibleNeutralMode = StringOptionItem.Create(Id + 12, "IncompatibleNeutralMode", EnumHelper.GetAllNames<ImitatorIncompatibleNeutralModeSelectList>(), 0, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Imitator]);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(1);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = RememberCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl player) => player.GetAbilityUseLimit() > 0;
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        var role = target.GetCustomRole();

        if (role is CustomRoles.Jackal
            or CustomRoles.HexMaster
            or CustomRoles.Poisoner
            or CustomRoles.Juggernaut
            or CustomRoles.BloodKnight
            or CustomRoles.Sheriff)
        {
            killer.RpcRemoveAbilityUse();
            killer.RpcSetCustomRole(role);
            killer.GetRoleClass().OnAdd(killer.PlayerId);

            if (role.IsCrewmate())
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedCrewmate")));
            else
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedNeutralKiller")));

            // Notify target
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
        }
        else if (role.IsAmneMaverick())
        {
            killer.RpcRemoveAbilityUse();

            switch (IncompatibleNeutralMode.GetInt())
            {
                case 0:
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedImitator")));
                    break;
                case 1:
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedPursuer")));
                    killer.RpcSetCustomRole(CustomRoles.Pursuer);
                    killer.GetRoleClass().OnAdd(killer.PlayerId);
                    break;
                case 2:
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedFollower")));
                    killer.RpcSetCustomRole(CustomRoles.Follower);
                    killer.GetRoleClass().OnAdd(killer.PlayerId);
                    break;
                case 3:
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedMaverick")));
                    killer.RpcSetCustomRole(CustomRoles.Maverick);
                    killer.GetRoleClass().OnAdd(killer.PlayerId);
                    break;
                case 4: //....................................................................................x100
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedAmnesiac")));
                    killer.RpcSetCustomRole(CustomRoles.Amnesiac);
                    killer.GetRoleClass().OnAdd(killer.PlayerId);
                    break;
            }

        }
        else if (role.IsCrewmate())
        {
            killer.RpcRemoveAbilityUse();
            killer.RpcSetCustomRole(CustomRoles.Sheriff);
            killer.GetRoleClass().OnAdd(killer.PlayerId);
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedCrewmate")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
        }
        else if (role.IsImpostor())
        {
            killer.RpcRemoveAbilityUse();
            killer.RpcSetCustomRole(CustomRoles.Refugee);
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedImpostor")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
        }

        var killerRole = killer.GetCustomRole();

        if (killerRole != CustomRoles.Imitator)
        {
            killer.ResetKillCooldown();
            killer.SetKillCooldown(forceAnime: true);

            Logger.Info("Imitator remembered: " + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString(), "Imitator Assign");

            Utils.NotifyRoles(SpecifySeer: killer);
        }
        else if (killerRole == CustomRoles.Imitator)
        {
            killer.SetKillCooldown(forceAnime: true);
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorInvalidTarget")));
        }

        return false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("ImitatorKillButtonText"));
    }

}
