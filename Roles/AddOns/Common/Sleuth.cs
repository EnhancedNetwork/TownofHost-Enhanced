namespace TOHE.Roles.AddOns.Common;

public class Sleuth : IAddon
{
    private const int Id = 20100;
    public AddonTypes Type => AddonTypes.Helpful;

    public static OptionItem ImpCanBeSleuth;
    public static OptionItem CrewCanBeSleuth;
    public static OptionItem NeutralCanBeSleuth;
    public static OptionItem SleuthCanKnowKillerRole;
    
    public static readonly Dictionary<byte, string> SleuthNotify = [];

    public void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Sleuth, canSetNum: true);
        ImpCanBeSleuth = BooleanOptionItem.Create(Id + 10, "ImpCanBeSleuth", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sleuth]);
        CrewCanBeSleuth = BooleanOptionItem.Create(Id + 11, "CrewCanBeSleuth", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sleuth]);
        NeutralCanBeSleuth = BooleanOptionItem.Create(Id + 12, "NeutralCanBeSleuth", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sleuth]);
        SleuthCanKnowKillerRole = BooleanOptionItem.Create(Id + 13, "SleuthCanKnowKillerRole", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sleuth]);
    }

    public void Init()
    {
        SleuthNotify.Clear();
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
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
