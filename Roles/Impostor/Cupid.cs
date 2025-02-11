using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;
internal class Cupid : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Cupid;
    private const int Id = 33800;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\
    public static Dictionary<byte, byte> ActiveLinks = [];
    public static HashSet<byte> Queue;

    private static OptionItem KillCooldown;
    private static OptionItem ShapeshiftCooldown;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Cupid);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cupid])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 11, "LinkCooldown338", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cupid])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        if (shapeshifter.PlayerId == target.PlayerId) return false;
        if (Queue.Contains(target.PlayerId))
            return false;
        if (Queue == null)
        {
            Queue.Add(target.PlayerId);
            return false;
        }
        else
        {
            foreach (var playerid in Queue)
            {
                ActiveLinks[playerid] = target.PlayerId; 
                ActiveLinks[target.PlayerId] = playerid;
                Queue.Remove(playerid);
            }
        }
        return false;
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        foreach (var linkedid in ActiveLinks)
        {
            if (linkedid.Value == target.PlayerId)
            {
                var linked = Utils.GetPlayer(linkedid.Key);
                target.RpcMurderPlayer(linked);
                linked.SetDeathReason(PlayerState.DeathReason.FollowingSuicide);
            }
        }
        if (Queue.Contains(target.PlayerId)) Queue.Remove(target.PlayerId);
        return true;
    }
}
