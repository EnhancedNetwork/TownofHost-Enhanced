using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Prohibited : IAddon
{
    private const int Id = 29900;
    public AddonTypes Type => AddonTypes.Harmful;

    private static OptionItem CountBlockedVentsInSkeld;
    private static OptionItem CountBlockedVentsInMira;
    private static OptionItem CountBlockedVentsInPolus;
    private static OptionItem CountBlockedVentsInDleks;
    private static OptionItem CountBlockedVentsInAirship;
    private static OptionItem CountBlockedVentsInFungle;

    private static readonly Dictionary<byte, int> RememberBlokcedVents = [];

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Prohibited, canSetNum: true, teamSpawnOptions: true);
        CountBlockedVentsInSkeld = IntegerOptionItem.Create(Id + 10, "Prohibited_CountBlockedVentsInSkeld", new(0, 14, 1), 4, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Prohibited]);
        CountBlockedVentsInMira = IntegerOptionItem.Create(Id + 11, "Prohibited_CountBlockedVentsInMira", new(0, 11, 1), 4, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Prohibited]);
        CountBlockedVentsInPolus = IntegerOptionItem.Create(Id + 12, "Prohibited_CountBlockedVentsInPolus", new(0, 12, 1), 4, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Prohibited]);
        CountBlockedVentsInDleks = IntegerOptionItem.Create(Id + 13, "Prohibited_CountBlockedVentsInDleks", new(0, 14, 1), 0, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Prohibited]);
        CountBlockedVentsInAirship = IntegerOptionItem.Create(Id + 14, "Prohibited_CountBlockedVentsInAirship", new(0, 12, 1), 4, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Prohibited]);
        CountBlockedVentsInFungle = IntegerOptionItem.Create(Id + 15, "Prohibited_CountBlockedVentsInFungle", new(0, 10, 1), 4, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Prohibited]);
    }

    public static void Init()
    {
        RememberBlokcedVents.Clear();
    }
    public static void Add(byte playerId)
    {
        var coutBlokedVents = Utils.GetActiveMapName() switch
        {
            MapNames.Skeld => CountBlockedVentsInSkeld.GetInt(),
            MapNames.Mira => CountBlockedVentsInMira.GetInt(),
            MapNames.Polus => CountBlockedVentsInPolus.GetInt(),
            MapNames.Dleks => CountBlockedVentsInDleks.GetInt(),
            MapNames.Airship => CountBlockedVentsInAirship.GetInt(),
            MapNames.Fungle => CountBlockedVentsInFungle.GetInt(),
            _ => 0
        };

        if (coutBlokedVents <= 0) return;
        var allVents = ShipStatus.Instance.AllVents.ToList();
        var allVentsCount = allVents.Count;

        if (coutBlokedVents > allVentsCount)
        {
            coutBlokedVents = allVentsCount;
        }

        for (int i = 0; i < coutBlokedVents; i++)
        {
            var vent = allVents.RandomElement();
            RememberBlokcedVents[playerId] = vent.Id;
            CustomRoleManager.BlockedVentsList[playerId].Add(vent.Id);
            allVents.Remove(vent);
        }
    }
    public static void Remove(byte playerId)
    {
        if (!RememberBlokcedVents.ContainsKey(playerId)) return;

        foreach (var (pcId, ventId) in RememberBlokcedVents)
        {
            if (pcId != playerId) continue;

            CustomRoleManager.BlockedVentsList[playerId].Remove(ventId);
        }
        RememberBlokcedVents.Remove(playerId);
    }
}
