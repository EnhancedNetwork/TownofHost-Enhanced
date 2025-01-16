namespace TOHE.Roles.Impostor;

internal class Ludopath : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Ludopath;
    private const int Id = 1800;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem LudopathRandomKillCD;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Ludopath);
        LudopathRandomKillCD = IntegerOptionItem.Create(Id + 2, "LudopathRandomKillCD", new(1, 100, 1), 45, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Ludopath])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = LudopathRandomKillCD.GetFloat();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        var ran = IRandom.Instance;
        int KillCD = ran.Next(1, LudopathRandomKillCD.GetInt());
        {
            Main.AllPlayerKillCooldown[killer.PlayerId] = KillCD;
        }
        return true;
    }
}
