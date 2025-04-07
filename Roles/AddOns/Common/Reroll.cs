using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Reroll : IAddon
{
    public CustomRoles Role => CustomRoles.Reroll;
    private const int Id = 35200;
    public AddonTypes Type => AddonTypes.Mixed;
    public static CustomRoles switchTo = CustomRoles.NotAssigned;
    public static bool isSwitching = false;
    public static PlayerControl switchPlayer = null;
    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Reroll, canSetNum: true, teamSpawnOptions: true);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    public static bool OnCheckVote(PlayerControl voter, PlayerControl target)
    {
        if (voter.Is(CustomRoles.Reroll))
        if (voter == target)
        {
            List<CustomRoles> PotentialRoles = [];
            foreach (var role in EnumHelper.GetAllValues<CustomRoles>())
            {
                if (role.GetCustomRoleTeam() == voter.GetCustomRole().GetCustomRoleTeam())
                if (role.IsEnable())
                if (role != voter.GetCustomRole())
                {
                    PotentialRoles.Add(role);
                }
            }
            var assign = PotentialRoles.RandomElement();
            isSwitching = true;
            switchTo = assign;
            switchPlayer = voter;

            return false;
        }
        return true;
    }
    public static void AfterMeetingTasks()
    {
        if (isSwitching)
        {  
            switchPlayer.RpcChangeRoleBasis(switchTo);
            switchPlayer.RpcSetCustomRole(switchTo);
            Main.PlayerStates[switchPlayer.PlayerId].RemoveSubRole(CustomRoles.Reroll);
            isSwitching = false;
        }
    }
}
