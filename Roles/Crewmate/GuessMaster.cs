using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class GuessMaster : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.GuessMaster;
    private const int Id = 26800;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();

    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    //==================================================================\\

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.GuessMaster);
    }

    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
    }

    public static void OnGuess(CustomRoles role, bool isMisguess = false, PlayerControl dp = null)
    {
        if (!HasEnabled) return;
        foreach (var gmID in playerIdList)
        {
            var gmPC = Utils.GetPlayerById(gmID);
            if (gmPC == null || !gmPC.IsAlive()) continue;
            if (isMisguess && dp != null)
            {
                _ = new LateTask(() =>
                {
                    Utils.SendMessage(string.Format(GetString("GuessMasterMisguess"), dp.GetRealName()), gmID, Utils.ColorString(Utils.GetRoleColor(CustomRoles.GuessMaster), GetString("GuessMasterTitle")));
                }, 1f, "GuessMaster On Miss Guess");
            }
            else
            {
                _ = new LateTask(() =>
                {
                    Utils.SendMessage(string.Format(GetString("GuessMasterTargetRole"), Utils.GetRoleName(role)), gmID, Utils.ColorString(Utils.GetRoleColor(CustomRoles.GuessMaster), GetString("GuessMasterTitle")));
                }, 1f, "GuessMaster Target Role");

            }
        }
    }
}
