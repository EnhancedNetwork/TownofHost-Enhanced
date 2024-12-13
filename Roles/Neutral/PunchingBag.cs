using Hazel;
using InnerNet;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral;

internal class PunchingBag : RoleBase// bad roll, plz don't use this hosts
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.PunchingBag;
    private const int Id = 14500;

    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;
    //==================================================================\\

    private static OptionItem PunchingBagKillMax;

    private readonly Dictionary<byte, int> PunchingBagMax = [];
    private readonly HashSet<byte> BlockGuess = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.PunchingBag);
        PunchingBagKillMax = IntegerOptionItem.Create(Id + 2, "PunchingBagKillMax", new(1, 30, 1), 5, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.PunchingBag])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        PunchingBagMax.Clear();
        BlockGuess.Clear();
    }
    public override void Add(byte playerId)
    {
        PunchingBagMax.Add(playerId, 0);
    }

    private void SendRPC(byte punchingbagId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(punchingbagId);
        writer.Write(PunchingBagMax[punchingbagId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        var punchingbagId = reader.ReadByte();
        var count = reader.ReadInt32();

        PunchingBagMax[punchingbagId] = count;
    }

    public override string GetProgressText(byte playerId, bool comms)
        => ColorString(GetRoleColor(CustomRoles.PunchingBag).ShadeColor(0.25f), $"({(PunchingBagMax.TryGetValue(playerId, out var count) ? count : 0)}/{PunchingBagKillMax.GetInt()})");

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        killer.SetKillCooldown(target: target, forceAnime: true);

        PunchingBagMax[target.PlayerId]++;
        SendRPC(target.PlayerId);

        target.Notify(string.Format(GetString("PunchingBagKill"), PunchingBagMax[target.PlayerId]));
        CheckWin();
        return false;
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl pc, CustomRoles role, ref bool guesserSuicide)
    {
        if (role != CustomRoles.PunchingBag) return false;
        if (BlockGuess.Contains(pc.PlayerId))
        {
            pc.ShowInfoMessage(isUI, GetString("GuessPunchingBagAgain"));
            return true;
        }

        pc.ShowInfoMessage(isUI, GetString("GuessPunchingBag"));

        BlockGuess.Add(pc.PlayerId);
        PunchingBagMax[target.PlayerId]++;
        SendRPC(target.PlayerId);

        CheckWin();
        return true;
    }
    private void CheckWin()
    {
        var punchingBagId = _Player.PlayerId;

        if (PunchingBagMax[punchingBagId] >= PunchingBagKillMax.GetInt())
        {
            if (!CustomWinnerHolder.CheckForConvertedWinner(punchingBagId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.PunchingBag);
                CustomWinnerHolder.WinnerIds.Add(punchingBagId);
            }
        }
    }
    public override bool GuessCheck(bool isUI, PlayerControl pc, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        pc.ShowInfoMessage(isUI, GetString("GuessPunchingBagBlocked"));
        return true;
    }
}
