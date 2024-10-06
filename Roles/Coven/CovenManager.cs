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
            foreach (var cov in CustomRolesHelper.AllRoles.Where(x => x.IsCoven() && (x is not CustomRoles.Medusa or CustomRoles.PotionMaster /* or CustomRoles.Sacrifist */)).ToArray())
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
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    => HasNecronomicon(seen) ? ColorString(GetRoleColor(CustomRoles.CovenLeader), "♣") : string.Empty;
    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if ((seer != target) && HasNecronomicon(target) && seer.IsPlayerCoven())
        {
            return ColorString(GetRoleColor(CustomRoles.CovenLeader), "♣");
        }
        return string.Empty;
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.Necronomicon, SendOption.Reliable, -1);
        writer.WriteNetObject(GetPlayerById(playerId)); 
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
    
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => target.IsPlayerCoven() && seer.IsPlayerCoven();
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => KnowRoleTarget(seer, target);
    
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
    public override void OnCoEndGame()
    {
        necroHolder = byte.MaxValue;
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (necroHolder == byte.MaxValue || !GetPlayerById(necroHolder).IsAlive() || !GetPlayerById(necroHolder).IsPlayerCoven())
        {
            GiveNecronomicon();
        }
    }
    public static bool HasNecronomicon(PlayerControl pc) => necroHolder == pc.PlayerId;
}
