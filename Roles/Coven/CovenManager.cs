using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE;
public abstract class CovenManager : RoleBase
{
    public static byte necroHolder = byte.MaxValue;

    public enum VisOptionList
    {
        On,
        CovenPerRole
    }
    public enum VentOptionList
    {
        On,
        CovenPerRole
    }

    private static readonly Dictionary<CustomRoles, OptionItem> CovenImpVisOptions = [];
    private static readonly Dictionary<CustomRoles, OptionItem> CovenVentOptions = [];

    public static readonly Dictionary<byte, byte> necroVotes = [];
    public static void RunSetUpImpVisOptions(int Id)
    {
            foreach (var cov in CustomRolesHelper.AllRoles.Where(x => x.IsCoven()).ToArray())
            {
                SetUpImpVisOption(cov, Id, true, CovenImpVisMode);
                Id++;
            }
    }
    public static void RunSetUpVentOptions(int Id)
    {
            foreach (var cov in CustomRolesHelper.AllRoles.Where(x => x.IsCoven()).ToArray())
            {
                SetUpVentOption(cov, Id, true, CovenVentMode);
                Id++;
            }
    }
    private static void SetUpImpVisOption(CustomRoles role, int Id, bool defaultValue = true, OptionItem parent = null)
    {
        var roleName = GetRoleName(role);
        Dictionary<string, string> replacementDic = new() { { "%role%", ColorString(GetRoleColor(role), roleName) } };
        CovenImpVisOptions[role] = BooleanOptionItem.Create(Id, "%role%HasImpVis", defaultValue, TabGroup.CovenRoles, false).SetParent(parent);
        CovenImpVisOptions[role].ReplacementDictionary = replacementDic;
    }
    private static void SetUpVentOption(CustomRoles role, int Id, bool defaultValue = true, OptionItem parent = null)
    {
        var roleName = GetRoleName(role);
        Dictionary<string, string> replacementDic = new() { { "%role%", ColorString(GetRoleColor(role), roleName) } };
        CovenVentOptions[role] = BooleanOptionItem.Create(Id, "%role%CanVent", defaultValue, TabGroup.CovenRoles, false).SetParent(parent);
        CovenVentOptions[role].ReplacementDictionary = replacementDic;
    }
    /*
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    => HasNecronomicon(seen) ? ColorString(GetRoleColor(CustomRoles.CovenLeader), "♣") : string.Empty;
    
    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (HasNecronomicon(target) && seer.IsPlayerCoven())
        {
            return ColorString(GetRoleColor(CustomRoles.CovenLeader), "♣");
        }
        return string.Empty;
    }
    */
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.Necronomicon, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveNecroRPC(MessageReader reader)
    {
        byte NecroId = reader.ReadByte();
        necroHolder = NecroId;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        if (!CovenHasImpVis.GetBool())
            opt.SetVision(false);
        else if (CovenImpVisMode.GetValue() == 0)
            opt.SetVision(true);
        else
        {
            CovenImpVisOptions.TryGetValue(GetPlayerById(playerId).GetCustomRole(), out var option);
            opt.SetVision(option.GetBool());
        }
    }
    public override bool CanUseImpostorVentButton(PlayerControl pc)
    {
        if (!CovenCanVent.GetBool())
            return false;
        else if (CovenVentMode.GetValue() == 0)
            return true;
        else
        {
            CovenVentOptions.TryGetValue(pc.GetCustomRole(), out var option);
            return option.GetBool();
        }
    }
    /*
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => target.IsPlayerCoven() && seer.IsPlayerCoven();
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => KnowRoleTarget(seer, target);
    */
    
    public static void GiveNecronomicon()
    {
        var pcList = Main.AllAlivePlayerControls.Where(pc => pc.IsPlayerCoven() && pc.IsAlive()).ToList();
        if (pcList.Any())
        {
            byte rp = pcList.RandomElement().PlayerId;
            necroHolder = rp;
            GetPlayerById(necroHolder).Notify(GetString("NecronomiconNotification"));
            SendRPC(necroHolder);
        }
    }
    public static void GiveNecronomicon(byte target)
    {
        necroHolder = target;
        GetPlayerById(necroHolder).Notify(GetString("NecronomiconNotification"));
        SendRPC(necroHolder);
    }
    public static void GiveNecronomicon(PlayerControl target)
    {
        necroHolder = target.PlayerId;
        GetPlayerById(necroHolder).Notify(GetString("NecronomiconNotification"));
        SendRPC(necroHolder);
    }
    public static void CheckNecroVotes()
    {
        Dictionary<byte, int> voteCount = new Dictionary<byte, int>();
        byte currentResult = byte.MinValue;
        byte lastResult = byte.MinValue;
        foreach (byte voter in necroVotes.Keys)
        {
            if (!voteCount.ContainsKey(necroVotes[voter]))
                voteCount.Add(necroVotes[voter], 0);
        }
        foreach (byte voter in necroVotes.Keys) {
            voteCount[necroVotes[voter]]++;
            Logger.Info($"{voteCount[necroVotes[voter]]} votes tallied for {GetPlayerById(currentResult).GetRealName()} Necronomicon", "Coven");
        }
        currentResult = voteCount.Keys.First();
        foreach (byte vote in voteCount.Keys) {
            if (voteCount[vote] >= voteCount[currentResult] && currentResult != vote) {
                lastResult = currentResult;
                currentResult = vote;
                Logger.Info($"{GetPlayerById(currentResult).GetRealName()} has more votes than {GetPlayerById(lastResult).GetRealName()}", "Coven");
            }
        }
        if (currentResult == byte.MinValue && !necroVotes.ContainsKey(byte.MinValue)) {
            Logger.Info($"currentResult == byte.MinValue, return", "Coven");
            return;
        }
        else if (voteCount[currentResult] == voteCount[lastResult] && currentResult != lastResult)
        {
            Logger.Info($"{GetPlayerById(currentResult).GetRealName()} and {GetPlayerById(lastResult).GetRealName()} had equal Necronomicon votes, not changing Necronomicon", "Coven");
        }
        else
        {
            GiveNecronomicon(currentResult);
            Logger.Info($"{GetPlayerById(currentResult).GetRealName()} had the most Necronomicon votes, giving them Necronomicon", "Coven");
        }
        necroVotes.Clear();
    }

    public static void NecronomiconCheck()
    {
        if (necroHolder == byte.MaxValue || !GetPlayerById(necroHolder).IsAlive() || !GetPlayerById(necroHolder).IsPlayerCoven())
        {
            GiveNecronomicon();
        }
    }
    public static bool HasNecronomicon(PlayerControl pc) => necroHolder == pc.PlayerId;
}
