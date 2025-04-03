using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Main;
using TOHE.Modules;
using MS.Internal.Xml.XPath;

namespace TOHE.Roles.Impostor;
internal class Kidnapper : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Kidnapper;
    private const int Id = 34900;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem KidnapCooldown;
    private static OptionItem AbilityUses;
    private static byte TargetId;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Kidnapper);
        KidnapCooldown = FloatOptionItem.Create(Id + 10, "KidnapCooldown349", new(0f, 60f, 1f), 20f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Kidnapper])
            .SetValueFormat(OptionFormat.Seconds);
        AbilityUses = IntegerOptionItem.Create(Id + 11, "KidnapUses349", new(0, 10, 1), 4, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Kidnapper])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(AbilityUses.GetInt());

        // Double Trigger
        var pc = GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }
    public override bool OnCheckMurderAsKiller(PlayerControl player, PlayerControl target)
    {
        if (player.GetAbilityUseLimit() < 1)
        {
            return true;
        }

        if (player.CheckDoubleTrigger(target, () => { }))
        {
            return true;
        }

        TargetId = target.PlayerId;
        player.RpcGuardAndKill(player);
        player.SetKillCooldown(KidnapCooldown.GetFloat());
        return false;
    }
    public override void UnShapeShiftButton(PlayerControl shapeshifter)
    {
        shapeshifter.RpcRemoveAbilityUse();
        if (TargetId == byte.MaxValue) return;
        var target = GetPlayerById(TargetId);
        var positionTarget = shapeshifter.GetCustomPosition();
        target.RpcTeleport(positionTarget);
        shapeshifter.RpcMurderPlayer(target);
        TargetId = byte.MaxValue;
    }
    public override void SetKillCooldown(byte id)
    {
        AllPlayerKillCooldown[id] = KidnapCooldown.GetFloat();
    }
}
