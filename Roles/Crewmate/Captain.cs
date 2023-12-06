using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

public static class Captain
{
    private static readonly int Id = 11900;
    private static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Marshall);

    private static Dictionary<byte, float> OriginalSpeed = new();

    public static OptionItem OptionCrewCanFindCaptain;
    public static OptionItem OptionMadmateCanFindCaptain;
    public static OptionItem OptionReducedSpeed;
    public static OptionItem OptionReducedSpeedTime;
    public static OptionItem CaptainCanTargetNB;
    public static OptionItem CaptainCanTargetNC;
    public static OptionItem CaptainCanTargetNE;
    public static OptionItem CaptainCanTargetNK;


    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Captain);
        OptionCrewCanFindCaptain = BooleanOptionItem.Create(Id + 11, "CrewCanFindCaptain", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain]);
        OptionMadmateCanFindCaptain = BooleanOptionItem.Create(Id + 12, "MadmateCanFindCaptain", false, TabGroup.CrewmateRoles, false).SetParent(OptionCrewCanFindCaptain);
        OptionReducedSpeed = FloatOptionItem.Create(Id + 13, "ReducedSpeed", new(0.1f, 5f, 0.1f), 0.5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain])
            .SetValueFormat(OptionFormat.Multiplier);
        OptionReducedSpeedTime = FloatOptionItem.Create(Id + 14, "ReducedSpeedTime", new(1f, 60f, 1f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain])
            .SetValueFormat(OptionFormat.Seconds);
        CaptainCanTargetNB = BooleanOptionItem.Create(Id + 15, "CaptainCanTargetNB", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain]);
        CaptainCanTargetNC = BooleanOptionItem.Create(Id + 16, "CaptainCanTargetNC", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain]);
        CaptainCanTargetNE = BooleanOptionItem.Create(Id + 17, "CaptainCanTargetNE", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain]);
        CaptainCanTargetNK = BooleanOptionItem.Create(Id + 18, "CaptainCanTargetNK", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain]);
        OverrideTasksData.Create(Id + 19, TabGroup.CrewmateRoles, CustomRoles.Captain);
    }

    public static void Init()
    {
        playerIdList = new();
        OriginalSpeed = new();
    }

    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;
    }
    private static void sendRPCSetSpeed(byte targetId)
    {
        MessageWriter writer;
        writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCaptainTargetSpeed, SendOption.Reliable, -1);
        writer.Write(targetId);
        writer.Write(OriginalSpeed[targetId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        return;
    }
    private static void sendRPCRevertSpeed(byte targetId)
    {
        MessageWriter writer;
        writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RevertCaptainTargetSpeed, SendOption.Reliable, -1);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        return;
    }
    private static void sendRPCRevertAllSpeed()
    {
        MessageWriter writer;
        writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RevertCaptainTargetSpeed, SendOption.Reliable, -1);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        return;
    }
    public static void OnTaskComplete(PlayerControl pc)
    {
        if (pc == null) return;
        if (!IsEnable) return;
        if (!pc.Is(CustomRoles.Captain)) return;
        var allTargets = Main.AllAlivePlayerControls.Where(x => (x != null) && (!OriginalSpeed.ContainsKey(x.PlayerId)) && 
                                                           (x.GetCustomRole().IsImpostorTeamV3() ||
                                                           (CaptainCanTargetNB.GetBool() && x.GetCustomRole().IsNB()) ||
                                                           (CaptainCanTargetNE.GetBool() && x.GetCustomRole().IsNE()) ||
                                                           (CaptainCanTargetNC.GetBool() && x.GetCustomRole().IsNC()) ||
                                                           (CaptainCanTargetNK.GetBool() && x.GetCustomRole().IsNeutralKillerTeam()))).ToList();
        var rand = IRandom.Instance;
        var targetPC = allTargets[rand.Next(allTargets.Count)];
        var target = targetPC.PlayerId;
        OriginalSpeed[target] = Main.AllPlayerSpeed[target];
        sendRPCSetSpeed(target);
        Main.AllPlayerSpeed[target] = OptionReducedSpeed.GetFloat();
        targetPC.SyncSettings();
        _ = new LateTask(() =>
        {
            if (!GameStates.IsInTask) return;
            Main.AllPlayerSpeed[target] = OriginalSpeed[target];
            targetPC.SyncSettings();
            OriginalSpeed.Remove(target);
            sendRPCRevertSpeed(target);
        }, OptionReducedSpeedTime.GetFloat(), "Captain Revert Speed");
    }
    public static void OnReportDeadBody()
    {
        OriginalSpeed.Clear();
        sendRPCRevertAllSpeed();
    }
}

