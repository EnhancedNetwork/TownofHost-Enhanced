using AmongUs.GameOptions;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.AddOns.Common;

public class Rat : IAddon
{
    public CustomRoles Role => CustomRoles.Rat;
    private const int Id = 22900;
    public AddonTypes Type => AddonTypes.Helpful;
    private static readonly HashSet<byte> playerList = [];
    public static bool IsEnable = false;
    public static OptionItem ratRoleCount;
    public static OptionItem canFindCrew;
    public static OptionItem canFindImp;
    public static OptionItem canFindNeutral;
    public static OptionItem canFindCoven;
    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Rat, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
        ratRoleCount = IntegerOptionItem.Create(Id + 10, "RatRoleCount", new(1, 10, 1), 3, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rat]);
        canFindCrew = BooleanOptionItem.Create(Id + 11, "RatCanFindCrew", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rat]);
        canFindImp = BooleanOptionItem.Create(Id + 12, "RatCanFindImp", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rat]);
        canFindNeutral = BooleanOptionItem.Create(Id + 13, "RatCanFindNeutral", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rat]);
        canFindCoven = BooleanOptionItem.Create(Id + 14, "RatCanFindCoven", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rat]);
    }
    public void Init()
    {
        IsEnable = false;
        playerList.Clear();
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    {
        playerList.Add(playerId);
        IsEnable = true;
    }
    public void Remove(byte playerId)
    {
        playerList.Remove(playerId);

        if (!playerList.Any())
            IsEnable = false;
    }
    public static void GetMessage()
    {
        if (!MeetingStates.FirstMeeting) return;

        int n = ratRoleCount.GetInt();
        int i = 0;
        List<CustomRoles> listOfRoles = [.. CustomRolesHelper.AllRoles.Where(role => !role.IsGhostRole() && role.IsEnable() && !role.RoleExist(countDead: true) && ((role.IsCrewmate()&&canFindCrew.GetBool())||(role.IsImpostor()&&canFindImp.GetBool())||(role.IsNeutral()&&canFindNeutral.GetBool())||(role.IsCoven()&&canFindCoven.GetBool()))).Shuffle()];
        string separator = TranslationController.Instance.currentLanguage.languageID is SupportedLangs.English or SupportedLangs.Russian ? "], [" : "】, 【";
        if (n > listOfRoles.Count) n = listOfRoles.Count;
        
        foreach (var ratId in playerList)
        {
            List<CustomRoles> ratRoles = [.. listOfRoles.Take(i..(i + n))];

            HashSet<string> ratRoleList = [];
            foreach (var role in ratRoles)
            {
                ratRoleList.Add(role.GetActualRoleName());
            }

            MeetingHudStartPatch.AddMsg(string.Format(GetString("RatRoleList"), string.Join(separator, ratRoleList)), ratId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Rat), GetString("RatMsgTitle")));

            i += n;
            if (i + n > listOfRoles.Count) i = 0;
        }
    }
}