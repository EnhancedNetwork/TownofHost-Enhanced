using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Psychic : RoleBase
{
    private static readonly int Id = 9400;
    private static List<byte> playerIdList = [];
    public static bool On = false;
    public override bool IsEnable => On;
    public static bool HasEnabled => CustomRoles.Psychic.IsClassEnable();
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

    private static OptionItem CanSeeNum;
    private static OptionItem Fresh;
    private static OptionItem CkshowEvil;
    private static OptionItem NBshowEvil;
    private static OptionItem NEshowEvil;
    private static OptionItem NCshowEvil;

    private static List<byte> RedPlayer = [];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Psychic);
        CanSeeNum = IntegerOptionItem.Create(Id + 2, "PsychicCanSeeNum", new(1, 15, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic])
            .SetValueFormat(OptionFormat.Pieces);
        Fresh = BooleanOptionItem.Create(Id + 6, "PsychicFresh", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
        CkshowEvil = BooleanOptionItem.Create(Id + 3, "CrewKillingRed", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
        NBshowEvil = BooleanOptionItem.Create(Id + 4, "NBareRed", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
        NEshowEvil = BooleanOptionItem.Create(Id + 5, "NEareRed", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
        NCshowEvil = BooleanOptionItem.Create(Id + 7, "NCareRed", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
    }
    public override void Init()
    {
        playerIdList = [];
        RedPlayer = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        On = true;
    }
    private static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncPsychicRedList, SendOption.Reliable, -1);
        writer.Write(RedPlayer.Count);
        foreach (var pc in RedPlayer)
            writer.Write(pc);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        int count = reader.ReadInt32();
        RedPlayer = [];
        for (int i = 0; i < count; i++)
            RedPlayer.Add(reader.ReadByte());
    }
    public static bool IsRedForPsy(PlayerControl target, PlayerControl seer)
    {
        if (target == null || seer == null) return false;
        var targetRole = target.GetCustomRole();
        if (seer.Is(CustomRoles.Madmate)) return targetRole.IsNK() || targetRole.IsNE() || targetRole.IsCK();
        else return RedPlayer != null && RedPlayer.Contains(target.PlayerId);
    }
    public override void OnReportDeadBody(PlayerControl reported, PlayerControl target)
    {
        if (Fresh.GetBool() || RedPlayer == null || RedPlayer.Count < 1)
            GetRedName();
    }
    public static void GetRedName()
    {
        if (!HasEnabled || !AmongUsClient.Instance.AmHost) return;

        List<PlayerControl> BadListPc = Main.AllAlivePlayerControls.Where(x =>
        x.Is(CustomRoleTypes.Impostor)  && !x.Is(CustomRoles.Trickster) || x.Is(CustomRoles.Madmate) || x.Is(CustomRoles.Rascal) || x.Is(CustomRoles.Recruit) || x.Is(CustomRoles.Charmed) || x.Is(CustomRoles.Infected) || !x.Is(CustomRoles.Admired) || x.Is(CustomRoles.Contagious) ||
        (x.GetCustomRole().IsCK() && CkshowEvil.GetBool()) ||
        (x.GetCustomRole().IsNE() && NEshowEvil.GetBool()) ||
        (x.GetCustomRole().IsNC() && NCshowEvil.GetBool()) ||
        (x.GetCustomRole().IsNB() && NBshowEvil.GetBool())
        ).ToList();

        List<byte> BadList = [];
        BadListPc.Do(x => BadList.Add(x.PlayerId));
        List<byte> AllList = [];
        Main.AllAlivePlayerControls.Where(x => !BadList.Contains(x.PlayerId) && !x.Is(CustomRoles.Psychic)).Do(x => AllList.Add(x.PlayerId));

        int ENum = 1;
        for (int i = 1; i < CanSeeNum.GetInt(); i++)
            if (IRandom.Instance.Next(0, 100) < 18) ENum++;
        int BNum = CanSeeNum.GetInt() - ENum;
        ENum = Math.Min(ENum, BadList.Count);
        BNum = Math.Min(BNum, AllList.Count);

        if (ENum < 1) goto EndOfSelect;

        RedPlayer = [];
        for (int i = 0; i < ENum && BadList.Count >= 1; i++)
        {
            RedPlayer.Add(BadList[IRandom.Instance.Next(0, BadList.Count)]);
            BadList.RemoveAll(RedPlayer.Contains);
        }

        AllList.RemoveAll(RedPlayer.Contains);
        for (int i = 0; i < BNum && AllList.Count >= 1; i++)
        {
            RedPlayer.Add(AllList[IRandom.Instance.Next(0, AllList.Count)]);
            AllList.RemoveAll(RedPlayer.Contains);
        }

    EndOfSelect:

        Logger.Info($"需要{CanSeeNum.GetInt()}个红名，其中需要{ENum}个邪恶。计算后显示红名{RedPlayer.Count}个", "Psychic");
        RedPlayer.Do(x => Logger.Info($"红名：{x}: {Main.AllPlayerNames[x]}", "Psychic"));
        SendRPC(); //RPC同步红名名单

    }
    public override string NotifyPlayerName(PlayerControl seer, PlayerControl target, string TargetPlayerName = "", bool IsForMeeting = false)
    {
        if(IsForMeeting)
            if (Psychic.IsRedForPsy(target, seer) && seer.IsAlive())
                return ColorString(GetRoleColor(CustomRoles.Impostor), TargetPlayerName);
        return string.Empty;
    }
    public override string PVANameText(PlayerVoteArea pva, PlayerControl target)
    {
        var seer = PlayerControl.LocalPlayer;
        if (Psychic.IsRedForPsy(target, seer) && !seer.Data.IsDead)
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), pva.NameText.text);
        return string.Empty;
    }
}