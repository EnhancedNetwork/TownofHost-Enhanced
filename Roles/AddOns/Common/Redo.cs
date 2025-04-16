using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Redo : IAddon
{
    public CustomRoles Role => CustomRoles.Redo;
    private const int Id = 35200;
    public AddonTypes Type => AddonTypes.Mixed;
    public static CustomRoles switchTo = CustomRoles.NotAssigned;
    public static bool isSwitching = false;
    public static PlayerControl switchPlayer = null;
    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Redo, canSetNum: true, teamSpawnOptions: true);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    public static bool OnCheckVote(PlayerControl voter, PlayerControl target)
    {
        if (voter.Is(CustomRoles.Redo))
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
        if (!switchPlayer.IsAlive()) return;
        if (isSwitching)
        {
            switchPlayer.GetRoleClass()?.OnRemove(switchPlayer.PlayerId);
            switchPlayer.RpcChangeRoleBasis(switchTo);
            switchPlayer.RpcSetCustomRole(switchTo);
            switchPlayer.GetRoleClass()?.OnAdd(switchPlayer.PlayerId);
            Main.PlayerStates[switchPlayer.PlayerId].RemoveSubRole(CustomRoles.Redo);
            isSwitching = false;
        }
    }
}
