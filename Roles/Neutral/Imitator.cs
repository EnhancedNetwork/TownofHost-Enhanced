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
        AbilityLimit = 1;
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = RememberCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl player) => AbilityLimit > 0;
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (AbilityLimit < 1) return false;
        CustomRoles ChangeRole = CustomRoles.Imitator;
        var role = target.GetCustomRole();

        if (target.IsAnySubRole(x => x.IsBetrayalAddonV2()))
        {
            foreach (var subrole in target.GetCustomSubRoles().Where(x => x.IsBetrayalAddonV2()))
            {
                // role returns respective recruiting role for subrole,returns sheriff for narc,taskinator for egoist
                role = subrole switch
                {
                    CustomRoles.Madmate => CustomRoles.Gangster,
                    CustomRoles.Charmed => CustomRoles.Cultist,
                    CustomRoles.Recruit => CustomRoles.Jackal,
                    CustomRoles.Infected => CustomRoles.Infectious,
                    CustomRoles.Contagious => CustomRoles.Virus,
                    CustomRoles.Admired => CustomRoles.Admirer,
                    CustomRoles.Enchanted => CustomRoles.Ritualist,
                    CustomRoles.Egoist => CustomRoles.Taskinator,            
                    CustomRoles.Narc => CustomRoles.Sheriff,
                    _ => role
                };
            }
        }

        if (role.IsCrewmate()) ChangeRole = CustomRoles.Sheriff;
        else if (role.IsImpostor() || role == CustomRoles.Refugee) ChangeRole = CustomRoles.Refugee;
        else if (role.IsMadmate() && role != CustomRoles.Refugee) 
            ChangeRole = role.GetVNRole() is CustomRoles.Impostor or CustomRoles.Shapeshifter ? 
            CustomRoles.Apprentice : CustomRoles.Convict; 
        else if (role.IsNK()) ChangeRole = role is CustomRoles.Jackal ? CustomRoles.Sidekick : role; 
        else if (role.IsNA()) ChangeRole = CustomRoles.Berserker; 
        else if (role.IsCoven()) ChangeRole = CustomRoles.Sacrifist;         
        else if (role.IsNonNK() && role != CustomRoles.Imitator)
        {
            switch (IncompatibleNeutralMode.GetInt())
            {
                case 0:
                    break;
                case 1:
                    ChangeRole = CustomRoles.Pursuer;
                    break;
                case 2:
                    ChangeRole = CustomRoles.Follower;
                    break;
                case 3:
                    ChangeRole = CustomRoles.Maverick;
                    break;
                case 4: //....................................................................................x100
                    ChangeRole = CustomRoles.Amnesiac;
                    break;
            }

        }

        if (ChangeRole != CustomRoles.Imitator)
        {
            AbilityLimit--;
            SendSkillRPC();
            killer.RpcChangeRoleBasis(ChangeRole);
            killer.RpcSetCustomRole(ChangeRole);
            killer.GetRoleClass().OnAdd(killer.PlayerId);
            killer.SyncSettings();
            killer.SetKillCooldown();
            Main.PlayerStates[killer.PlayerId].InitTask(killer);
            {
                if (ChangeRole is CustomRoles.Amnesiac)
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedAmnesiac")));
                else killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), string.Format(GetString("AmnesiacRemembered"), Utils.GetRoleName(ChangeRole))));
            }
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));

            Logger.Info("Imitator remembered: " + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString(), "Imitator Assign");
            Logger.Info($"{killer.GetNameWithRole()} : {AbilityLimit} remember limits left", "Imitator");
        }
        else 
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
