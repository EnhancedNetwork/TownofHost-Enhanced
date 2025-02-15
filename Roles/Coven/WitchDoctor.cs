using static TOHE.Options;
using TOHE.Modules;
using TOHE.Roles.AddOns;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Coven;

internal class WitchDoctor : CovenManager
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.WitchDoctor;
    private const int Id = 33800;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenUtility;
    //==================================================================\\

    public static readonly List<byte> ProtectList = [];
    private static List<CustomRoles> addons = [];
    public static int Uses;
    public static OptionItem AmountOfRecruits;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.WitchDoctor, 1, zeroOne: false);
        AmountOfRecruits = IntegerOptionItem.Create(Id + 10, "AmountRecruits338", new(1, 5, 1), 2, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.WitchDoctor])
            .SetValueFormat(OptionFormat.Times);
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;

    public override void Init()
    {
        Uses = AmountOfRecruits.GetInt();
    }

    public override void Add(byte playerId)
    {
        // Double Trigger
        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();
        addons.AddRange(GroupedAddons[AddonTypes.Harmful]);
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (Uses <= 0) return true;
        Uses -= 1;
        if (HasNecronomicon(killer))
        {
            if (killer.CheckDoubleTrigger(target, () => { }))
            {
                return true;
            }
            ProtectList.Add(target.PlayerId);
        }
        var AllSubRoles = Main.PlayerStates[target.PlayerId].SubRoles.ToList();
        foreach (var role in AllSubRoles)
        {
            if (addons.Contains(role))
            {
                AllSubRoles.Remove(role);
            }
        }
        target.RpcSetCustomRole(CustomRoles.Enchanted);
        killer.RpcGuardAndKill(killer);
        return false;
    }

    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (ProtectList.Contains(target.PlayerId))
        {
            killer.RpcGuardAndKill(killer);
            return false;
        }
        else return true;
    }
    public override void AfterMeetingTasks()
    {
        ProtectList.Clear();
    }
}
