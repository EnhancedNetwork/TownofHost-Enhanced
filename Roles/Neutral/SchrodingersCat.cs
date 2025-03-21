using Hazel;
using InnerNet;
using TOHE.Roles.Core;


namespace TOHE.Roles.Neutral;

internal class SchrodingersCat : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.SchrodingersCat;
    private const int Id = 6900;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.SchrodingersCat);
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    public static readonly Dictionary<byte, byte> teammate = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.SchrodingersCat);
    }

    public override void Init()
    {
        teammate.Clear();
    }

    public override void Add(byte playerId)
    {
        teammate[playerId] = byte.MaxValue;
    }

    private void SendRPC(byte catID)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
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
        if (killer.Is(CustomRoles.Taskinator)) return true;
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


    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (seer != target && seer.IsAlive() && teammate.ContainsKey(seer.PlayerId) && teammate.ContainsValue(target.PlayerId))
        {
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.SchrodingersCat), " ☜");
        }
        else if (seer != target && !seer.IsAlive() && teammate.ContainsValue(target.PlayerId))
        {
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.SchrodingersCat), " ☜");
        }
        return string.Empty;
    }

    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target)
    {
        if (teammate.TryGetValue(seer.PlayerId, out var temmate) && target.PlayerId == temmate)
        {
            if (target.GetCustomRole().IsCrewmate()) return Main.roleColors[CustomRoles.CrewmateTOHE];
            else return Main.roleColors[target.GetCustomRole()];
        }
        return string.Empty;
    }
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target)
    {
        if (teammate.TryGetValue(target.PlayerId, out var killer) && killer == seer.PlayerId)
        {
            return true;
        }
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
