
namespace TOHE.Roles.Coven;

internal class Ritualist : CovenManager
{
    //===========================SETUP================================\\
    private const int Id = 29900;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Ritualist);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenPower;
    //==================================================================\\

    public static OptionItem MaxRitsPerRound;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.Ritualist, 1, zeroOne: false);
        MaxRitsPerRound = IntegerOptionItem.Create(Id + 10, "RitualistMaxRitsPerRound", new(1, 15, 1), 2, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ritualist])
            .SetValueFormat(OptionFormat.Times);
    }

    public override void Add(byte PlayerId)
    {
        AbilityLimit = MaxRitsPerRound.GetInt();
    }

    public static void ReceiveRPC_Custom(MessageReader reader, PlayerControl pc)
    {
        int PlayerId = reader.ReadByte();
        RitualistMsgCheck(pc, $"/rt {PlayerId}", true);
    }
    public override bool CanUseKillButton(PlayerControl pc) => HasNecronomicon(pc);
    public override string GetProgressText(byte playerId, bool comms)
        => ColorString(AbilityLimit >= 1 ? GetRoleColor(CustomRoles.CovenLeader).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");
    public override void OnReportDeadBody(PlayerControl hatsune, NetworkedPlayerInfo miku)
    {
        AbilityLimit = MaxRitsPerRound.GetInt();
    }
    public static bool RitualistMsgCheck(PlayerControl pc, string msg, bool isUI = false)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.Ritualist)) return false;
        msg = msg.Trim().ToLower();
        if (msg.Length < 3 || msg[..4] != "/rt") return false;


        if (msg == "/rt")
        {
            string text = GetString("PlayerIdList");
            foreach (var npc in Main.AllAlivePlayerControls)
                text += "\n" + npc.PlayerId.ToString() + " → " + npc.GetRealName();
            SendMessage(text, pc.PlayerId);
            return true;
        }


        if (AbilityLimit <= 0)
        {
            pc.ShowInfoMessage(isUI, GetString("RitualistRitualMax"));
            return true;
        }

        if (!MsgToPlayerAndRole(msg, out byte targetId, out CustomRoles role, out string error))
        {
            pc.ShowInfoMessage(isUI, error);
            return true;
        }
        var target = Utils.GetPlayerById(targetId);

        if (!target.Is(role))
        {
            pc.ShowInfoMessage(isUI, GetString("RitualistRitualFail"));
            AbilityLimit = 0;
            return true;
        }
        if (target.IsTransformedNeutralApocalypse() || (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini) && Mini.Age < 18) || target.Is(CustomRoles.Solsticer) || !target.IsAlive() || target.Is(CustomRoles.Loyal) || !(target.GetCustomSubRoles().Contains(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool())
        {
            pc.ShowInfoMessage(isUI, GetString("RitualistRitualImpossible"));
            return true;
        }

        Logger.Info($"{pc.GetNameWithRole()} enchant {target.GetNameWithRole()}", "Ritualist");

        string Name = target.GetRealName();

        AbilityLimit--;

        target.RpcSetCustomRole(CustomRoles.Enchanted);
        SendMessage(string.Format(GetString("RitualistConvertNotif"), CustomRoles.Ritualist.ToColoredString()), target.PlayerId);
        SendMessage(string.Format(GetString("RitualistRitualSuccess"), target.GetRealName()), pc.PlayerId);
        return true;
    }
    private static bool MsgToPlayerAndRole(string msg, out byte id, out CustomRoles role, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        Regex r = new("\\d+");
        MatchCollection mc = r.Matches(msg);
        string result = string.Empty;
        for (int i = 0; i < mc.Count; i++)
        {
            result += mc[i];//匹配结果是完整的数字，此处可以不做拼接的
        }

        if (int.TryParse(result, out int num))
        {
            id = Convert.ToByte(num);
        }
        else
        {
            id = byte.MaxValue;
            error = GetString("RitualistCommandHelp");
            role = new();
            return false;
        }

        PlayerControl target = Utils.GetPlayerById(id);
        if (target == null || target.Data.IsDead)
        {
            error = GetString("GuessNull");
            role = new();
            return false;
        }

        if (!ChatCommands.GetRoleByName(msg, out role))
        {
            error = GetString("RitualistCommandHelp");
            return false;
        }

        error = string.Empty;
        return true;
    }
}