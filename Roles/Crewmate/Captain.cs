using System.Collections.Generic;
using Epic.OnlineServices;
using TOHE.Patches;

namespace TOHE.Roles.Crewmate;
public static class Captain
{
    public static Dictionary<byte, int> CaptainVotes = new();
    public static OptionItem CaptainAbilityUses;
    public static OptionItem CaptainDies;
    public static bool IsEnable = false;


    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(26000, TabGroup.CrewmateRoles, CustomRoles.Captain);
        CaptainAbilityUses = IntegerOptionItem.Create(26002, "CaptainAbilityUses", new(1, 20, 1), 3, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Captain])
            .SetValueFormat(OptionFormat.Votes);
        CaptainDies = BooleanOptionItem.Create(26003, "CaptainDies", true, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Captain]);
    }

    public static void Init()
    {
        CaptainVotes = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        IsEnable = true;
        CaptainVotes.TryAdd(playerId, CaptainAbilityUses.GetInt());
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static string GetUses(byte playerId) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.Captain).ShadeColor(0.25f), CaptainVotes.TryGetValue(playerId, out var uses) ? $"({uses})" : "Invalid");
}
