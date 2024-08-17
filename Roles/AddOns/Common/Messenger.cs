using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;
using System;
using static TOHE.MeetingHudStartPatch;
using TOHE.Roles._Ghosts_.Crewmate;
using TOHE.Roles.Core;
using System.Diagnostics.Contracts;

namespace TOHE.Roles.AddOns.Common;

public class Messenger : IAddon
{
    private const int Id = 29200;
    private static OptionItem ImpostorsHearMessage;
    private static OptionItem NeutralsHearMessage;
    private static OptionItem KnowMessenger;
    public AddonTypes Type => AddonTypes.Helpful;
    public static Dictionary<byte, Dictionary<int, string>> Determinemessage = [];
    public static Dictionary<byte, bool> DidSay = [];
    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Messenger, canSetNum: true, teamSpawnOptions: true);
        ImpostorsHearMessage = BooleanOptionItem.Create(Id + 10, "MessengersImpsHear", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Messenger]);
        NeutralsHearMessage = BooleanOptionItem.Create(Id + 11, "MessengersNeutsHear", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Messenger]);
        KnowMessenger = BooleanOptionItem.Create(Id + 12, "EveryoneKnowsMessenger", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Messenger]);
    }
    public void Init()
    {
        DidSay.Clear();
        Determinemessage.Clear();
    }
    public static string GetSuffix(PlayerControl seen, bool ismeeting)
    {
        if (!seen.Is(CustomRoles.Messenger) || !ismeeting || !KnowMessenger.GetBool() || seen.IsAlive() || DidSay.TryGetValue(seen.PlayerId, out var say) && say) return string.Empty;

        return ColorString(GetRoleColor(CustomRoles.Messenger), "⌘");
    }
    public static void NotifyAddonOnMeeting(PlayerControl pc) 
    {
        if (!pc.Is(CustomRoles.Messenger) || DidSay.TryGetValue(pc.PlayerId, out _) || pc.IsAlive()) return;

        var msg = DetermineSetMessage(pc, pc.GetRealKiller(), out var det);
        Determinemessage[pc.PlayerId] = det;
        AddMsg(string.Format(GetString("Messenger.Msg"), msg), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Messenger), GetString("MessengerTitle")));
    
    }
    public static bool CheckMessage(PlayerControl pc, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost || !GameStates.IsMeeting || pc == null || pc.IsAlive()) return false;
        if (!Determinemessage.TryGetValue(pc.PlayerId, out var determineMessage)) return false;
        if (!pc.Is(CustomRoles.Messenger) || DidSay.TryGetValue(pc.PlayerId, out bool said) && said) return false;
        if (args.Length < 2) return false;

        string[] cmds = { "/mms" }; // Here you can add custom cmds
        if (!cmds.Any(x => x.Equals(args[0], StringComparison.OrdinalIgnoreCase))) return false;


        if (!int.TryParse(args[1], out int id) || !determineMessage.ContainsKey(id))
        {
            SendMessage(GetString("MessengerUsage"), pc.PlayerId, title: ColorString(GetRoleColor(CustomRoles.Messenger), GetString("MessengerTitle")));
            return false;
        }
        DidSay[pc.PlayerId] = true;

        List<byte> ExceptList = [];
        if (!ImpostorsHearMessage.GetBool())
            ExceptList.AddRange(Main.AllPlayerControls.Where(x => x.GetCustomRole().IsImpostorTeamV2()).Select(x => x.PlayerId));
        if (!NeutralsHearMessage.GetBool())
            ExceptList.AddRange(Main.AllPlayerControls.Where(x => x.GetCustomRole().IsNeutralTeamV2()).Select(x => x.PlayerId));

        foreach (var par in Main.AllAlivePlayerControls.ExceptBy(ExceptList, x => x.PlayerId))
        {
            SendMessage(determineMessage[id], par.PlayerId, title: ColorString(GetRoleColor(CustomRoles.Messenger), GetString("MessengerTitleTarget")));
        }
        SendMessage(GetString("TelepathyConfirmSelf"), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Messenger), GetString("MessengerTitle")));


        return true;

    }
}