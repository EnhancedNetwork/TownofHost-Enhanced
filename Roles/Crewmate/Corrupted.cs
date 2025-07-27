using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Corrupted : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Corrupted;
    private const int Id = 36200;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Corrupted);
        OverrideTasksData.Create(Id + 10, TabGroup.CrewmateRoles, CustomRoles.Corrupted);
    }
    public override bool OnTaskComplete(PlayerControl pc, int completedTaskCount, int totalTaskCount)
    {        
        var taskState = pc.GetPlayerTaskState();
        if (taskState.IsTaskFinished)
        {
            var role = SelectRandomImpostor();
            pc.RpcSetRoleDesync(role);
            pc.RpcSetCustomRole(CustomRoles.CorruptedA);
            pc.RpcSetCustomRole(role);
        }
        return true;
    }
    private static CustomRoles SelectRandomImpostor()
    {
        HashSet<CustomRoles> rolelist = [];

        var role = CustomRoles.Corrupted;
            
        foreach (var impostor in EnumHelper.GetAllValues<CustomRoles>().Where(x => x.IsImpostor()))
        {
            rolelist.Add(impostor);
        }
        role = rolelist.RandomElement();
        
        return role;
    }
}
