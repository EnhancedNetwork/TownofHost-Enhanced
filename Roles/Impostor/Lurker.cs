using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class Lurker : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 1900;

    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem DefaultKillCooldown;
    private static OptionItem ReduceKillCooldown;


    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Lurker);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.DefaultKillCooldown, new(20f, 180f, 1f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lurker])
            .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(Id + 11, GeneralOption.ReduceKillCooldown, new(0f, 10f, 1f), 2f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lurker])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }

    public  override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = DefaultKillCooldown.GetFloat();

    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        float newCd = Main.AllPlayerKillCooldown[pc.PlayerId] - ReduceKillCooldown.GetFloat();
        if (newCd <= 0)
        {
            return;
        }

        Main.AllPlayerKillCooldown[pc.PlayerId] = newCd;
        pc.SyncSettings();
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        killer.ResetKillCooldown();
        killer.SyncSettings();
        return true;
    }
}
