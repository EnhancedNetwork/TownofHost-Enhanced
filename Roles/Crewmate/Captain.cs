using Hazel;
using System.Collections.Generic;
using System.Linq;

using static TOHE.Options;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Crewmate;

public static class Captain
{
    private static readonly int Id = 26300;
    //private static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    private static Dictionary<byte, float> OriginalSpeed = new();
    public static Dictionary<byte, List<byte>> CaptainVoteTargets = new();

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
        OptionReducedSpeedTime = FloatOptionItem.Create(Id + 14, "ReducedSpeedTime", new(1f, 60f, 1f), 5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain])
            .SetValueFormat(OptionFormat.Seconds);
        CaptainCanTargetNB = BooleanOptionItem.Create(Id + 15, "CaptainCanTargetNB", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain]);
        CaptainCanTargetNC = BooleanOptionItem.Create(Id + 16, "CaptainCanTargetNC", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain]);
        CaptainCanTargetNE = BooleanOptionItem.Create(Id + 17, "CaptainCanTargetNE", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain]);
        CaptainCanTargetNK = BooleanOptionItem.Create(Id + 18, "CaptainCanTargetNK", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain]);
        OverrideTasksData.Create(Id + 19, TabGroup.CrewmateRoles, CustomRoles.Captain);
    }

    public static void Init()
    {
        //playerIdList = new();
        IsEnable = false;
        OriginalSpeed = new();
        CaptainVoteTargets = new();
    }

    public static void Add(byte playerId)
    {
        //playerIdList.Add(playerId);
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
    public static void ReceiveRPCSetSpeed(MessageReader reader)
    {
        byte targetId = reader.ReadByte();
        float speed = reader.ReadSingle();
        OriginalSpeed[targetId] = speed;
    }
    private static void sendRPCRevertSpeed(byte targetId)
    {
        MessageWriter writer;
        writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RevertCaptainTargetSpeed, SendOption.Reliable, -1);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        return;
    }
    public static void ReceiveRPCRevertSpeed(MessageReader reader)
    {
        byte targetId = reader.ReadByte();
        if (OriginalSpeed.ContainsKey(targetId)) OriginalSpeed.Remove(targetId);
    }
    private static void sendRPCRevertAllSpeed()
    {
        MessageWriter writer;
        writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RevertCaptainAllTargetSpeed, SendOption.Reliable, -1);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        return;
    }
    public static void ReceiveRPCRevertAllSpeed(MessageReader reader)
    {
        OriginalSpeed.Clear();
    }

    public static void sendRPCVoteAdd(byte playerId, byte targetId)
    {
        MessageWriter writer;
        writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCaptainVotedTarget, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        return;
    }
    public static void ReceiveRPCVoteAdd(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        byte targetId = reader.ReadByte();
        if (!CaptainVoteTargets.ContainsKey(playerId)) CaptainVoteTargets[playerId] = new();
        CaptainVoteTargets[playerId].Add(targetId);
    }
    private static void sendRPCVoteRemove()
    {
        MessageWriter writer;
        writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RevertCaptainVoteRemove, SendOption.Reliable, -1);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        return;
    }
    public static void ReceiveRPCVoteRemove(MessageReader reader)
    {
        CaptainVoteTargets.Clear();
    }

    public static void OnTaskComplete(PlayerControl pc)
    {
        if (pc == null) return;
        if (!IsEnable) return;
        if (!pc.Is(CustomRoles.Captain) || !pc.IsAlive()) return;
        var allTargets = Main.AllAlivePlayerControls.Where(x => (x != null) && (!OriginalSpeed.ContainsKey(x.PlayerId)) &&
                                                           (x.GetCustomRole().IsImpostorTeamV3() ||
                                                           (CaptainCanTargetNB.GetBool() && x.GetCustomRole().IsNB()) ||
                                                           (CaptainCanTargetNE.GetBool() && x.GetCustomRole().IsNE()) ||
                                                           (CaptainCanTargetNC.GetBool() && x.GetCustomRole().IsNC()) ||
                                                           (CaptainCanTargetNK.GetBool() && x.GetCustomRole().IsNeutralKillerTeam()))).ToList();

        Logger.Info($"Total Number of Potential Target {allTargets.Count}", "Total Captain Target");
        if (!allTargets.Any()) return;
        var rand = IRandom.Instance;
        var targetPC = allTargets[rand.Next(allTargets.Count)];
        var target = targetPC.PlayerId;
        OriginalSpeed[target] = Main.AllPlayerSpeed[target];
        sendRPCSetSpeed(target);
        Logger.Info($"{targetPC.GetNameWithRole()} is chosen as the captain's target", "Captain Target");
        Main.AllPlayerSpeed[target] = OptionReducedSpeed.GetFloat();
        targetPC.SyncSettings();
        targetPC.Notify("CaptainSpeedReduced", OptionReducedSpeedTime.GetFloat());
        _ = new LateTask(() =>
        {
            if (!GameStates.IsInTask) return;
            Main.AllPlayerSpeed[target] = OriginalSpeed[target];
            targetPC.SyncSettings();
            OriginalSpeed.Remove(target);
            sendRPCRevertSpeed(target);
        }, OptionReducedSpeedTime.GetFloat(), "Captain Revert Speed");
    }
    private static CustomRoles? SelectRandomAddon(byte targetId)
    {
        if (!AmongUsClient.Instance.AmHost) return null;
        var AllSubRoles = Main.PlayerStates[targetId].SubRoles;
        for (int i = AllSubRoles.Count - 1; i >= 0; i--)
        {
            var role = AllSubRoles[i];
            if (role == CustomRoles.LastImpostor ||
                role == CustomRoles.Lovers) // Causes issues involving Lovers Suicide
            {
                Logger.Info($"Removed {role} from list of addons", "Captain");
                AllSubRoles.Remove(role);
            }
        }

        if (!AllSubRoles.Any())
        {
            Logger.Info("No removable addons found on the target.", "Bandit");
            return null;
        }
        var rand = IRandom.Instance;
        var addon = AllSubRoles[rand.Next(0, AllSubRoles.Count)];
        return addon;
    }
    public static void OnExile(byte playerId)
    {
        Logger.Info("Captain on exile executing", "Captain on exile");
        if (playerId == byte.MaxValue) return;
        if (!CaptainVoteTargets.ContainsKey(playerId)) return;
        for (int i = 0; i < CaptainVoteTargets[playerId].Count; i++)
        {
            var captainTarget = CaptainVoteTargets[playerId][i];
            if (captainTarget == byte.MaxValue || !Utils.GetPlayerById(captainTarget).IsAlive()) continue;
            var SelectedAddOn = SelectRandomAddon(captainTarget);
            if (SelectedAddOn == null) continue;
            Main.PlayerStates[captainTarget].RemoveSubRole((CustomRoles)SelectedAddOn);
            Logger.Info($"Successfully removed {SelectedAddOn} addon from {Utils.GetPlayerById(captainTarget).GetNameWithRole()}", "Captain");

        }
        CaptainVoteTargets.Clear();
        sendRPCVoteRemove();
    }
    public static void OnReportDeadBody()
    {
        foreach (byte target in OriginalSpeed.Keys)
        {
            PlayerControl targetPC = Utils.GetPlayerById(target);
            if (targetPC == null) continue;
            Main.AllPlayerSpeed[target] = OriginalSpeed[target];
            targetPC.SyncSettings();
        }

        OriginalSpeed.Clear();
        sendRPCRevertAllSpeed();
    }
}

