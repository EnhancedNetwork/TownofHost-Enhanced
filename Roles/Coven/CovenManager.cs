using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Neutral;
using static TOHE.Options;
using static UnityEngine.GraphicsBuffer;

namespace TOHE;
public abstract class CovenManager : RoleBase
{
    public static PlayerControl necroHolder;

    private static 
    public override void SetupCustomOption()
    {
        base.SetupCustomOption();
    }
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => target.IsPlayerCoven() && seer.IsPlayerCoven();
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => KnowRoleTarget(seer, target);
    public static void GiveNecronomicon()
    {
        var pcList = Main.AllAlivePlayerControls.Where(pc => pc.IsPlayerCoven() && pc.IsAlive()).ToList();
        if (pcList.Any())
        {
            PlayerControl rp = pcList.RandomElement();
            necroHolder = rp;
        }
    }
    public static bool HasNecronomicon(PlayerControl pc) => necroHolder == pc;
}
