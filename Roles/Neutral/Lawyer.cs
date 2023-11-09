using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

public static class Lawyer
{
    private static readonly int Id = 13100;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;
    public static byte WinnerID;

    private static OptionItem CanTargetImpostor;
    private static OptionItem CanTargetNeutralKiller;
    private static OptionItem CanTargetCrewmate;
    private static OptionItem CanTargetJester;
    public static OptionItem ChangeRolesAfterTargetKilled;
    public static OptionItem LawyerVision;
    public static OptionItem KnowTargetRole;
    public static OptionItem TargetKnowsLawyer;


    /// <summary>
    /// Key: エクスキューショナーのPlayerId, Value: ターゲットのPlayerId
    /// </summary>
    public static Dictionary<byte, byte> Target = new();
    public static readonly string[] ChangeRoles =
    {
        "Role.Crewmate",
        "Role.Jester",
        "Role.Opportunist",
        "Role.Convict",
        "Role.Celebrity",
        "Role.Bodyguard",
        "Role.Dictator",
        "Role.Mayor",
        "Role.Doctor",
     //   CustomRoles.Crewmate.ToString(), CustomRoles.Jester.ToString(), CustomRoles.Opportunist.ToString(),
    };
    public static readonly CustomRoles[] CRoleChangeRoles =
    {
        CustomRoles.CrewmateTOHE, CustomRoles.Jester, CustomRoles.Opportunist, CustomRoles.Convict, CustomRoles.CyberStar, CustomRoles.Bodyguard, CustomRoles.Dictator, CustomRoles.Mayor, CustomRoles.Doctor,
    };

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Lawyer);
    //    LawyerVision = FloatOptionItem.Create(Id + 14, "LawyerVision", new(0f, 5f, 0.05f), 1.25f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer])
    //        .SetValueFormat(OptionFormat.Multiplier);
        CanTargetImpostor = BooleanOptionItem.Create(Id + 10, "LawyerCanTargetImpostor", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        CanTargetNeutralKiller = BooleanOptionItem.Create(Id + 11, "LawyerCanTargetNeutralKiller", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        CanTargetCrewmate = BooleanOptionItem.Create(Id + 12, "LawyerCanTargetCrewmate", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        CanTargetJester = BooleanOptionItem.Create(Id + 13, "LawyerCanTargetJester", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        KnowTargetRole = BooleanOptionItem.Create(Id + 14, "KnowTargetRole", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        TargetKnowsLawyer = BooleanOptionItem.Create(Id + 15, "TargetKnowsLawyer", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        ChangeRolesAfterTargetKilled = StringOptionItem.Create(Id + 16, "LawyerChangeRolesAfterTargetKilled", ChangeRoles, 1, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
    }
    public static void Init()
    {
        playerIdList = new();
        Target = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;

        //ターゲット割り当て
        if (AmongUsClient.Instance.AmHost)
        {
            List<PlayerControl> targetList = new();
            var rand = IRandom.Instance;
            foreach (var target in Main.AllPlayerControls)
            {
                if (playerId == target.PlayerId) continue;
                else if (!CanTargetImpostor.GetBool() && target.Is(CustomRoleTypes.Impostor)) continue;
                else if (!CanTargetNeutralKiller.GetBool() && target.IsNeutralKiller()) continue;
                else if (!CanTargetCrewmate.GetBool() && target.Is(CustomRoleTypes.Crewmate)) continue;
                else if (!CanTargetJester.GetBool() && target.Is(CustomRoles.Jester)) continue;
                else if (target.Is(CustomRoleTypes.Neutral) && !target.IsNeutralKiller() && !target.Is(CustomRoles.Jester)) continue;
                if (target.GetCustomRole() is CustomRoles.GM or CustomRoles.SuperStar or CustomRoles.NiceMini or CustomRoles.EvilMini) continue;
                if (Utils.GetPlayerById(playerId).Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers)) continue;

                targetList.Add(target);
            }
            var SelectedTarget = targetList[rand.Next(targetList.Count)];
            Target.Add(playerId, SelectedTarget.PlayerId);
            SendRPC(playerId, SelectedTarget.PlayerId, "SetTarget");
            Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()}:{SelectedTarget.GetNameWithRole()}", "Lawyer");
        }
    }
    public static void SendRPC(byte lawyerId, byte targetId = 0x73, string Progress = "")
    {
        MessageWriter writer;
        switch (Progress)
        {
            case "SetTarget":
                writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetLawyerTarget, SendOption.Reliable);
                writer.Write(lawyerId);
                writer.Write(targetId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                break;
            case "":
                if (!AmongUsClient.Instance.AmHost) return;
                writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RemoveLawyerTarget, SendOption.Reliable);
                writer.Write(lawyerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                break;
            
        }
    }
    public static void ReceiveRPC(MessageReader reader, bool SetTarget)
    {
        if (SetTarget)
        {
            byte LawyerId = reader.ReadByte();
            byte TargetId = reader.ReadByte();
            Target[LawyerId] = TargetId;
        }
        else
            Target.Remove(reader.ReadByte());
    }
    public static void ChangeRoleByTarget(PlayerControl target)
    {
        byte Lawyer = 0x73;
        Target.Do(x =>
        {
            if (x.Value == target.PlayerId)
                Lawyer = x.Key;
        });
        Utils.GetPlayerById(Lawyer).RpcSetCustomRole(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()]);
        Target.Remove(Lawyer);
        SendRPC(Lawyer);
        Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(Lawyer));
    }
    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        if (!KnowTargetRole.GetBool()) return false;
        return player.Is(CustomRoles.Lawyer) && Target.TryGetValue(player.PlayerId, out var tar) && tar == target.PlayerId;
    }
    public static string LawyerMark(PlayerControl seer, PlayerControl target)
    {
        if (!seer.Is(CustomRoles.Lawyer))
        {
            if (!TargetKnowsLawyer.GetBool()) return "";
            return (Target.TryGetValue(target.PlayerId, out var x) && seer.PlayerId == x) ?
                Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lawyer), "♦") : "";
        }
        var GetValue = Target.TryGetValue(seer.PlayerId, out var targetId);
        return GetValue && targetId == target.PlayerId ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lawyer), "♦") : "";
    }

    public static void ChangeRole(PlayerControl lawyer)
    {
        lawyer.RpcSetCustomRole(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()]);
        Target.Remove(lawyer.PlayerId);
        SendRPC(lawyer.PlayerId);
        var text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lawyer), Translator.GetString(""));
        text = string.Format(text, Utils.ColorString(Utils.GetRoleColor(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()]), Translator.GetString(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()].ToString())));
        lawyer.Notify(text);
    }
    public static string TargetMark(PlayerControl seer, PlayerControl target)
    {
        if (!seer.Is(CustomRoles.Lawyer) || seer.Data.IsDead) return ""; //エクスキューショナー以外処理しない

        var GetValue = Target.TryGetValue(seer.PlayerId, out var targetId);
        return GetValue && targetId == target.PlayerId ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lawyer), "♦") : "";
    }
    public static bool CheckExileTarget(GameData.PlayerInfo exiled, bool DecidedWinner, bool Check = false)
    {
        if (!IsEnable) return false;

        foreach (var kvp in Target.Where(x => x.Value == exiled.PlayerId).ToArray())
        {
            var lawyer = Utils.GetPlayerById(kvp.Key);
            if (lawyer == null || lawyer.Data.Disconnected) continue;
            return true;
        }
        return false;
    }
}