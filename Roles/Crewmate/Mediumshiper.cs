using System.Collections.Generic;
using System.Linq;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

public static class Mediumshiper
{
    private static readonly int Id = 8700;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static OptionItem ContactLimitOpt;
    public static OptionItem OnlyReceiveMsgFromCrew;
    public static OptionItem MediumAbilityUseGainWithEachTaskCompleted;

    public static Dictionary<byte, byte> ContactPlayer = new();
    public static Dictionary<byte, float> ContactLimit = new();

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Mediumshiper);
        ContactLimitOpt = IntegerOptionItem.Create(Id + 10, "MediumshiperContactLimit", new(0, 15, 1), 1, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mediumshiper])
            .SetValueFormat(OptionFormat.Times);
        OnlyReceiveMsgFromCrew = BooleanOptionItem.Create(Id + 11, "MediumshiperOnlyReceiveMsgFromCrew", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mediumshiper]);
        MediumAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 12, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mediumshiper])
            .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = new();
        ContactPlayer = new();
        ContactLimit = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        ContactLimit.Add(playerId, ContactLimitOpt.GetInt());
        IsEnable = true;
    }
    public static void OnReportDeadBody(GameData.PlayerInfo target)
    {
        ContactPlayer = new();
        if (target == null) return;
        foreach (var pc in Main.AllAlivePlayerControls.Where(x => playerIdList.Contains(x.PlayerId) && x.PlayerId != target.PlayerId))
        {
            if (ContactLimit[pc.PlayerId] < 1) continue;
            ContactLimit[pc.PlayerId] -= 1;
            ContactPlayer.TryAdd(target.PlayerId, pc.PlayerId);
            Logger.Info($"通灵师建立联系：{pc.GetNameWithRole()} => {target.PlayerName}", "Mediumshiper");
        }
    }
    public static bool MsMsg(PlayerControl pc, string msg)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || pc == null) return false;
        if (!ContactPlayer.ContainsKey(pc.PlayerId)) return false;
        if (OnlyReceiveMsgFromCrew.GetBool() && !pc.GetCustomRole().IsCrewmate()) return false;
        if (pc.IsAlive()) return false;
        msg = msg.ToLower().Trim();
        if (!CheckCommond(ref msg, "通灵|ms|mediumship|medium", false)) return false;

        bool ans;
        if (msg.Contains("n") || msg.Contains(GetString("No")) || msg.Contains("错") || msg.Contains("不是")) ans = false;
        else if (msg.Contains("y") || msg.Contains(GetString("Yes")) || msg.Contains("对")) ans = true;
        else
        {
            Utils.SendMessage(GetString("MediumshipHelp"), pc.PlayerId);
            return true;
        }

        Utils.SendMessage(GetString("Mediumship" + (ans ? "Yes" : "No")), ContactPlayer[pc.PlayerId], Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mediumshiper), GetString("MediumshipTitle")));
        Utils.SendMessage(GetString("MediumshipDone"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mediumshiper), GetString("MediumshipTitle")));

        ContactPlayer.Remove(pc.PlayerId);

        return true;
    }
    public static bool CheckCommond(ref string msg, string command, bool exact = true)
    {
        var comList = command.Split('|');
        for (int i = 0; i < comList.Count(); i++)
        {
            if (exact)
            {
                if (msg == "/" + comList[i]) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comList[i]))
                {
                    msg = msg.Replace("/" + comList[i], string.Empty);
                    return true;
                }
            }
        }
        return false;
    }
}