using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;
using Hazel;
using InnerNet;

namespace TOHE.Roles.Neutral;

internal class Masochist : RoleBase// bad roll, plz don't use this hosts
{
    //===========================SETUP================================\\
    private const int Id = 14500;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;
    //==================================================================\\

    private static OptionItem MasochistKillMax;
    
    private static readonly Dictionary<byte, int> MasochistMax = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Masochist);
        MasochistKillMax = IntegerOptionItem.Create(Id + 2, "MasochistKillMax", new(1, 30, 1), 5, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Masochist])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        PlayerIds.Clear();
        MasochistMax.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
        MasochistMax.Add(playerId, 0);
    }

    private void SendRPC(byte masochistId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(masochistId);
        writer.Write(MasochistMax[masochistId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        var masochistId = reader.ReadByte();
        var count = reader.ReadInt32();

        MasochistMax[masochistId] = count;
    }

    public override string GetProgressText(byte playerId, bool comms)
        => ColorString(GetRoleColor(CustomRoles.Masochist).ShadeColor(0.25f), $"({(MasochistMax.TryGetValue(playerId, out var count) ? count : 0)}/{MasochistKillMax.GetInt()})");
    
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        killer.SetKillCooldown(target: target, forceAnime: true);

        MasochistMax[target.PlayerId]++;
        SendRPC(target.PlayerId);

        target.Notify(string.Format(GetString("MasochistKill"), MasochistMax[target.PlayerId]));
        if (MasochistMax[target.PlayerId] >= MasochistKillMax.GetInt())
        {
            if (!CustomWinnerHolder.CheckForConvertedWinner(target.PlayerId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Masochist);
                CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
            }
        }
        return false;
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl pc, CustomRoles role, ref bool guesserSuicide)
    {
        if (!isUI) SendMessage(GetString("GuessMasochist"), pc.PlayerId);
        else pc.ShowPopUp(GetString("GuessMasochist"));

        MasochistMax[target.PlayerId]++;
        SendRPC(target.PlayerId);

        if (MasochistMax[target.PlayerId] >= MasochistKillMax.GetInt())
        {
            if (!CustomWinnerHolder.CheckForConvertedWinner(target.PlayerId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Masochist);
                CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
            }
        }
        return true;
    }
    public override bool GuessCheck(bool isUI, PlayerControl pc, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        if (!isUI) SendMessage(GetString("GuessMasochistBlocked"), pc.PlayerId);
        else pc.ShowPopUp(GetString("GuessMasochistBlocked"));
        return true;
    }
}
