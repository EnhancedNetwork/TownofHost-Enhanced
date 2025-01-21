using System.Text;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.AddOns.Common;

internal class Reliable : IAddon
{
    //===========================SETUP================================\\
    public CustomRoles Role => CustomRoles.Reliable;
    private const int Id = 33100;
    public AddonTypes Type => AddonTypes.Helpful;
    //==================================================================\\

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Reliable, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
    }
    public string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var TextColor = GetRoleColor(CustomRoles.Reliable);

        ProgressText.Append(GetTaskCount(playerId, comms));
        ProgressText.Append(ColorString(TextColor, ColorString(Color.white, " - ") + $"({GameData.Instance.CompletedTasks}/{GameData.Instance.TotalTasks})"));
        return ProgressText.ToString();
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}
