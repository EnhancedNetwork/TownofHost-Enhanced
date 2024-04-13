using Hazel;

namespace TOHE.Roles.Neutral;

internal class SchrodingersCat : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 6900;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    public static readonly Dictionary<byte, byte> teammate = [];

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.SchrodingersCat);
    }

    public override void Init()
    {
        teammate.Clear();
        PlayerIds.Clear();
    }

    public override void Add(byte playerId)
    {
        teammate[playerId] = byte.MaxValue;
        PlayerIds.Add(playerId);
    }

    private static void SendRPC(byte catID)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.SchrodingersCat);
        writer.Write(catID);
        writer.Write(teammate[catID]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte catID = reader.ReadByte();
        byte teammateID = reader.ReadByte();
        teammate[catID] = teammateID;
    }
    public override string GetProgressText(byte catID, bool computervirus) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.SchrodingersCat).ShadeColor(0.25f), $"({(teammate.TryGetValue(catID, out var value) ? (value != byte.MaxValue ? "0" : "1") : "0")})");

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return true;
        if (teammate[target.PlayerId] != byte.MaxValue) return true;

        teammate[target.PlayerId] = killer.PlayerId;
        SendRPC(target.PlayerId);

        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill();

        Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
        Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);

        killer.SetKillCooldown();

        return false;
    }

    public static void SchrodingerWinCondition(PlayerControl pc)
    {
        if (!HasEnabled) return;
        if (pc == null) return;
        if (!pc.Is(CustomRoles.SchrodingersCat)) return;
        if (!teammate.ContainsKey(pc.PlayerId) || teammate[pc.PlayerId] == byte.MaxValue) return;
        if (CustomWinnerHolder.WinnerIds.Contains(teammate[pc.PlayerId]) || Main.PlayerStates.TryGetValue(teammate[pc.PlayerId], out var ps) && CustomWinnerHolder.WinnerRoles.Contains(ps.MainRole))
        {
            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.SchrodingersCat);
        }
    }
}
