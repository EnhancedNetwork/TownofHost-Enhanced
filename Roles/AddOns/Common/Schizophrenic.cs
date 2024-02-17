using Rewired.Utils.Classes.Data;
using static TOHE.Options;
using System.Collections.Generic;

namespace TOHE.Roles.AddOns.Common;

public static class Schizophrenic
{
    private static readonly int Id = 22400;

    public static OptionItem CanBeImp;
    public static OptionItem CanBeCrew;
    public static OptionItem DualVotes;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Schizophrenic, canSetNum: true);
        CanBeImp = BooleanOptionItem.Create(Id + 10, "ImpCanBeSchizophrenic", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Schizophrenic]);
        CanBeCrew = BooleanOptionItem.Create(Id + 11, "CrewCanBeSchizophrenic", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Schizophrenic]);
        DualVotes = BooleanOptionItem.Create(Id + 12, "DualVotes", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Schizophrenic]);
    }

    public static void CheckEndGameReason(int crewCount, int impCount, PlayerControl[] apcList, Dictionary<CountTypes, int> neutralRoleCounts)
    {
        foreach (var pc in apcList)
        {
            if (pc == null) continue;

            int dual = pc.Is(CustomRoles.Schizophrenic) ? 1 : 0;
            var countType = Main.PlayerStates[pc.PlayerId].countTypes;
            switch (countType)
            {
                case CountTypes.OutOfGame:
                case CountTypes.None:
                    continue;
                case CountTypes.Impostor:
                    impCount++;
                    impCount += dual;
                    break;
                case CountTypes.Crew:
                    crewCount++;
                    crewCount += dual;
                    break;
                default:
                    if (neutralRoleCounts.ContainsKey(countType))
                        neutralRoleCounts[countType]++;
                    else
                        neutralRoleCounts[countType] = 1;
                    neutralRoleCounts[countType] += dual;
                    break;
            }
        }
    }
}

