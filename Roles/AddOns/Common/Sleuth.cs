namespace TOHE.Roles.AddOns.Common;

public class Sleuth : IAddon
{
    private const int Id = 20100;
    public AddonTypes Type => AddonTypes.Helpful;

    public static OptionItem SleuthCanKnowKillerRole;
    
    public static Dictionary<byte, string> SleuthNotify = [];

    public void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Sleuth, canSetNum: true);
        SleuthCanKnowKillerRole = BooleanOptionItem.Create(Id + 13, "SleuthCanKnowKillerRole", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sleuth]);
    }

    public static void Init()
    {
        SleuthNotify = [];
    }
    public static void Clear()
    {
        SleuthNotify.Clear();
    }

    public static void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody)
    {
        if (reporter.Is(CustomRoles.Sleuth) && deadBody != null && deadBody.Object != null && !deadBody.Object.IsAlive() && reporter.PlayerId != deadBody.PlayerId)
        {
            string msg;
            msg = string.Format(Translator.GetString("SleuthNoticeVictim"), deadBody.Object.GetRealName(), deadBody.Object.GetDisplayRoleAndSubName(deadBody.Object, false));
            if (SleuthCanKnowKillerRole.GetBool())
            {
                var realKiller = deadBody.Object.GetRealKiller();
                if (realKiller == null) msg += "；" + Translator.GetString("SleuthNoticeKillerNotFound");
                else msg += "；" + string.Format(Translator.GetString("SleuthNoticeKiller"), realKiller.GetDisplayRoleAndSubName(realKiller, false));
            }
            SleuthNotify.Add(reporter.PlayerId, msg);
        }
    }
}
