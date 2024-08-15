﻿using Hazel;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;


namespace TOHE.Roles.Crewmate;

internal class Captain : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 26300;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static OptionItem OptionCrewCanFindCaptain;
    private static OptionItem OptionMadmateCanFindCaptain;
    private static OptionItem OptionTaskRequiredToReveal;
    private static OptionItem OptionTaskRequiredToSlow;
    private static OptionItem OptionReducedSpeed;
    private static OptionItem OptionReducedSpeedTime;
    private static OptionItem CaptainCanTargetNB;
    private static OptionItem CaptainCanTargetNC;
    private static OptionItem CaptainCanTargetNE;
    private static OptionItem CaptainCanTargetNK;
    private static OptionItem CaptainCanTargetNA;

    private static readonly Dictionary<byte, float> OriginalSpeed = [];
    private static readonly Dictionary<byte, List<byte>> CaptainVoteTargets = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Captain);
        OptionCrewCanFindCaptain = BooleanOptionItem.Create(Id + 11, "CrewCanFindCaptain", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain]);
        OptionMadmateCanFindCaptain = BooleanOptionItem.Create(Id + 12, "MadmateCanFindCaptain", false, TabGroup.CrewmateRoles, false).SetParent(OptionCrewCanFindCaptain);
        OptionTaskRequiredToReveal = IntegerOptionItem.Create(Id + 13, "CaptainRevealTaskRequired", new(0, 15, 1), 5, TabGroup.CrewmateRoles, false).SetParent(OptionCrewCanFindCaptain);
        OptionTaskRequiredToSlow = IntegerOptionItem.Create(Id + 14, "CaptainSlowTaskRequired", new(0, 15, 1), 5, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain]);
        OptionReducedSpeed = FloatOptionItem.Create(Id + 15, "ReducedSpeed", new(0.1f, 5f, 0.1f), 0.5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain])
            .SetValueFormat(OptionFormat.Times);
        OptionReducedSpeedTime = FloatOptionItem.Create(Id + 16, "ReducedSpeedTime", new(1f, 60f, 1f), 5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain])
            .SetValueFormat(OptionFormat.Seconds);
        CaptainCanTargetNB = BooleanOptionItem.Create(Id + 17, "CaptainCanTargetNB", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain]);
        CaptainCanTargetNC = BooleanOptionItem.Create(Id + 18, "CaptainCanTargetNC", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain]);
        CaptainCanTargetNE = BooleanOptionItem.Create(Id + 19, "CaptainCanTargetNE", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain]);
        CaptainCanTargetNK = BooleanOptionItem.Create(Id + 20, "CaptainCanTargetNK", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain]);
        CaptainCanTargetNA = BooleanOptionItem.Create(Id + 21, "CaptainCanTargetNA", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain]);
        OverrideTasksData.Create(Id + 22, TabGroup.CrewmateRoles, CustomRoles.Captain);
    }

    public override void Init()
    {
        playerIdList.Clear();
        OriginalSpeed.Clear();
        CaptainVoteTargets.Clear();
    }

    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    private static void SendRPCSetSpeed(byte targetId)
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
    private static void SendRPCRevertSpeed(byte targetId)
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
    private static void SendRPCRevertAllSpeed()
    {
        MessageWriter writer;
        writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RevertCaptainAllTargetSpeed, SendOption.Reliable, -1);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        return;
    }
    public static void ReceiveRPCRevertAllSpeed()
    {
        OriginalSpeed.Clear();
    }

    public static void SendRPCVoteAdd(byte playerId, byte targetId)
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
        if (!CaptainVoteTargets.ContainsKey(playerId)) CaptainVoteTargets[playerId] = [];
        CaptainVoteTargets[playerId].Add(targetId);
    }
    private static void SendRPCVoteRemove(byte captainTarget = byte.MaxValue, CustomRoles? SelectedAddon = null)
    {
        MessageWriter writer;
        writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RevertCaptainVoteRemove, SendOption.Reliable, -1);
        writer.Write(captainTarget);
        if (captainTarget != byte.MaxValue) writer.Write((int)SelectedAddon);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        return;
    }
    public static void ReceiveRPCVoteRemove(MessageReader reader)
    {
        byte captainTarget = reader.ReadByte();
        if (captainTarget != byte.MaxValue) 
        {
            int? SelectedAddon = reader.ReadInt32();
            if (SelectedAddon != null) Main.PlayerStates[captainTarget].SubRoles.Remove((CustomRoles)SelectedAddon);
        }
        else CaptainVoteTargets.Clear();
    }

    public static bool CrewCanFindCaptain() => OptionCrewCanFindCaptain.GetBool();

    public override bool OnTaskComplete(PlayerControl pc, int completedTaskCount, int totalTaskCount)
    {
        if (pc == null || !pc.IsAlive()) return true;
        if (pc.GetPlayerTaskState().CompletedTasksCount >= OptionTaskRequiredToReveal.GetInt()) NotifyRoles(SpecifyTarget: pc, ForceLoop: true);
        if (pc.GetPlayerTaskState().CompletedTasksCount < OptionTaskRequiredToSlow.GetInt()) return true;
        var allTargets = Main.AllAlivePlayerControls.Where(x => (x != null) && (!OriginalSpeed.ContainsKey(x.PlayerId)) &&
                                                           (x.GetCustomRole().IsImpostorTeamV3() ||
                                                           (CaptainCanTargetNB.GetBool() && x.GetCustomRole().IsNB()) ||
                                                           (CaptainCanTargetNE.GetBool() && x.GetCustomRole().IsNE()) ||
                                                           (CaptainCanTargetNC.GetBool() && x.GetCustomRole().IsNC()) ||
                                                           (CaptainCanTargetNK.GetBool() && x.GetCustomRole().IsNeutralKillerTeam()) 
                              ||                           (CaptainCanTargetNA.GetBool() && x.GetCustomRole().IsNA()))).ToList();

        Logger.Info($"Total Number of Potential Target {allTargets.Count}", "Total Captain Target");
        if (allTargets.Count == 0) return true;
        var rand = IRandom.Instance;
        var targetPC = allTargets.RandomElement();
        var target = targetPC.PlayerId;
        OriginalSpeed[target] = Main.AllPlayerSpeed[target];
        SendRPCSetSpeed(target);
        Logger.Info($"{targetPC.GetNameWithRole().RemoveHtmlTags()} is chosen as the captain's target", "Captain Target");
        Main.AllPlayerSpeed[target] = OptionReducedSpeed.GetFloat();
        targetPC.SyncSettings();
        targetPC.Notify(GetString("CaptainSpeedReduced"), OptionReducedSpeedTime.GetFloat());
        _ = new LateTask(() =>
        {
            if (!GameStates.IsInTask) return;
            Main.AllPlayerSpeed[target] = OriginalSpeed[target];
            targetPC.SyncSettings();
            OriginalSpeed.Remove(target);
            SendRPCRevertSpeed(target);
        }, OptionReducedSpeedTime.GetFloat(), "Captain Revert Speed");

        return true;
    }
    private static CustomRoles? SelectRandomAddon(byte targetId)
    {
        if (!AmongUsClient.Instance.AmHost) return null;
        var AllSubRoles = Main.PlayerStates[targetId].SubRoles.ToList();
        for (int i = AllSubRoles.Count - 1; i >= 0; i--)
        {
            var role = AllSubRoles[i];
            if (role == CustomRoles.Cleansed ||
                role == CustomRoles.LastImpostor ||
                role == CustomRoles.Lovers || // Causes issues involving Lovers Suicide
                role.IsBetrayalAddon())
            {
                Logger.Info($"Removed {role} from list of addons", "Captain");
                AllSubRoles.Remove(role);
            }
        }

        if (AllSubRoles.Count == 0)
        {
            Logger.Info("No removable addons found on the target.", "Captain");
            return null;
        }
        var addon = AllSubRoles.RandomElement();
        return addon;
    }
    public override void OnPlayerExiled(PlayerControl captain, NetworkedPlayerInfo exiled)
    {
        if (exiled == null || (exiled.GetCustomRole() is not CustomRoles.Captain)) return;

        byte playerId = exiled.PlayerId;
        if (playerId == byte.MaxValue) return;
        if (!CaptainVoteTargets.ContainsKey(playerId)) return;
        for (int i = 0; i < CaptainVoteTargets[playerId].Count; i++)
        {
            var captainTarget = CaptainVoteTargets[playerId][i];
            if (captainTarget == byte.MaxValue || !GetPlayerById(captainTarget).IsAlive()) continue; 
            var SelectedAddOn = SelectRandomAddon(captainTarget);
            if (SelectedAddOn == null) continue;
            Main.PlayerStates[captainTarget].RemoveSubRole((CustomRoles)SelectedAddOn);
            Logger.Info($"Successfully removed {SelectedAddOn} addon from {GetPlayerById(captainTarget).GetNameWithRole()}", "Captain");
            SendRPCVoteRemove(captainTarget: captainTarget, SelectedAddOn) ;
        }
        CaptainVoteTargets.Clear();
        SendRPCVoteRemove();
    }
    public override void OnReportDeadBody(PlayerControl y, NetworkedPlayerInfo x)
    {
        foreach (byte target in OriginalSpeed.Keys.ToArray())
        {
            PlayerControl targetPC = GetPlayerById(target);
            if (targetPC == null) continue;
            Main.AllPlayerSpeed[target] = OriginalSpeed[target];
            targetPC.SyncSettings();
        }

        OriginalSpeed.Clear();
        SendRPCRevertAllSpeed();
    }

    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (target.Is(CustomRoles.Captain) && OptionCrewCanFindCaptain.GetBool() &&
                (target.GetPlayerTaskState().CompletedTasksCount >= OptionTaskRequiredToReveal.GetInt()) &&
                ((seer.Is(Custom_Team.Crewmate) && !seer.Is(CustomRoles.Madmate)) || (seer.Is(CustomRoles.Madmate) && OptionMadmateCanFindCaptain.GetBool())))
        {
            return ColorString(GetRoleColor(CustomRoles.Captain), " ☆");
        }
        return string.Empty;
    }
    public override void OnVoted(PlayerControl votedPlayer, PlayerControl votedTarget)
    {
        if (votedPlayer.Is(CustomRoles.Captain))
        {
            if (!CaptainVoteTargets.ContainsKey(votedPlayer.PlayerId)) CaptainVoteTargets[votedPlayer.PlayerId] = [];

            if (!CaptainVoteTargets[votedPlayer.PlayerId].Contains(votedTarget.PlayerId))
            {
                CaptainVoteTargets[votedPlayer.PlayerId].Add(votedTarget.PlayerId);
                SendRPCVoteAdd(votedPlayer.PlayerId, votedTarget.PlayerId);
            }
        }
    }
}

