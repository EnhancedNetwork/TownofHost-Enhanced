using Hazel;

namespace TOHE;

public static class CustomWinnerHolder
{
    // 勝者のチームが格納されます。
    // リザルトの背景色の決定などに使用されます。
    // 注: この変数を変更する時、WinnerRoles・WinnerIdsを同時に変更しないと予期せぬ勝者が現れる可能性があります。
    public static CustomWinner WinnerTeam;
    // 追加勝利するプレイヤーのチームが格納されます。
    // リザルトの表示に使用されます。
    public static HashSet<AdditionalWinners> AdditionalWinnerTeams = [];
    // 勝者の役職が格納され、この変数に格納されている役職のプレイヤーは全員勝利となります。
    // チームとなるニュートラルの処理に最適です。
    public static HashSet<CustomRoles> WinnerRoles = [];
    // 勝者のPlayerIDが格納され、このIDを持つプレイヤーは全員勝利します。
    // 単独勝利するニュートラルの処理に最適です。
    public static HashSet<byte> WinnerIds = [];

    public static void Reset()
    {
        WinnerTeam = CustomWinner.Default;
        AdditionalWinnerTeams.Clear();
        WinnerRoles.Clear();
        WinnerIds.Clear();
    }
    public static void ClearWinners()
    {
        WinnerRoles.Clear();
        WinnerIds.Clear();
    }
    /// <summary><para>WinnerTeamに値を代入します。</para><para>すでに代入されている場合、AdditionalWinnerTeamsに追加します。</para></summary>
    public static void SetWinnerOrAdditonalWinner(CustomWinner winner)
    {
        if (WinnerTeam == CustomWinner.Default) WinnerTeam = winner;
        else AdditionalWinnerTeams.Add((AdditionalWinners)winner);
    }
    /// <summary><para>WinnerTeamに値を代入します。</para><para>すでに代入されている場合、既存の値をAdditionalWinnerTeamsに追加してから代入します。</para></summary>
    public static void ShiftWinnerAndSetWinner(CustomWinner winner)
    {
        if (WinnerTeam != CustomWinner.Default)
            AdditionalWinnerTeams.Add((AdditionalWinners)WinnerTeam);
        WinnerTeam = winner;
    }
    /// <summary><para>既存の値をすべて削除してから、WinnerTeamに値を代入します。</para></summary>
    public static void ResetAndSetWinner(CustomWinner winner)
    {
        Reset();
        WinnerTeam = winner;
    }
    public static bool CheckForConvertedWinner(byte playerId)
    {
        foreach (var role in playerId.GetPlayer()?.GetCustomSubRoles().ToArray())
        {
            if (!(role == CustomRoles.Madmate || role == CustomRoles.Admired || role.IsConverted())) continue;
            switch (role)
            {
                case CustomRoles.Admired:
                    ResetAndSetWinner(CustomWinner.Crewmate);
                    return true;
                case CustomRoles.Madmate:
                    ResetAndSetWinner(CustomWinner.Impostor); 
                    return true;
                case CustomRoles.Recruit:
                    ResetAndSetWinner(CustomWinner.Jackal); 
                    return true;
                case CustomRoles.Charmed:
                    ResetAndSetWinner(CustomWinner.Cultist); 
                    return true;
                case CustomRoles.Infectious:
                    ResetAndSetWinner(CustomWinner.Infectious); 
                    return true;
                case CustomRoles.Contagious:
                    ResetAndSetWinner(CustomWinner.Virus); 
                    return true;
            }
        }
        return false;
    }

    public static MessageWriter WriteTo(MessageWriter writer)
    {
        writer.WritePacked((int)WinnerTeam);

        writer.WritePacked(AdditionalWinnerTeams.Count);
        foreach (var wt in AdditionalWinnerTeams)
            writer.WritePacked((int)wt);

        writer.WritePacked(WinnerRoles.Count);
        foreach (var wr in WinnerRoles)
            writer.WritePacked((int)wr);

        writer.WritePacked(WinnerIds.Count);
        foreach (var id in WinnerIds)
            writer.Write(id);

        return writer;
    }
    public static void ReadFrom(MessageReader reader)
    {
        WinnerTeam = (CustomWinner)reader.ReadPackedInt32();

        AdditionalWinnerTeams.Clear();
        int AdditionalWinnerTeamsCount = reader.ReadPackedInt32();
        for (int i = 0; i < AdditionalWinnerTeamsCount; i++)
            AdditionalWinnerTeams.Add((AdditionalWinners)reader.ReadPackedInt32());

        WinnerRoles.Clear();
        int WinnerRolesCount = reader.ReadPackedInt32();
        for (int i = 0; i < WinnerRolesCount; i++)
            WinnerRoles.Add((CustomRoles)reader.ReadPackedInt32());

        WinnerIds.Clear();
        int WinnerIdsCount = reader.ReadPackedInt32();
        for (int i = 0; i < WinnerIdsCount; i++)
            WinnerIds.Add(reader.ReadByte());
    }
}