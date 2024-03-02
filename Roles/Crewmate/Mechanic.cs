using Hazel;
using System;
using System.Collections.Generic;
using System.Text;
using static TOHE.Utils;
using UnityEngine;
using AmongUs.GameOptions;

namespace TOHE.Roles.Crewmate;

internal class Mechanic : RoleBase
{
    private static readonly int Id = 8500;
    public static bool On = false;
    public static List<byte> playerIdList = [];
    public static Dictionary<byte, float> UsedSkillCount = [];
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;

    public static OptionItem SkillLimit;
    private static OptionItem FixesDoors;
    private static OptionItem FixesReactors;
    private static OptionItem FixesOxygens;
    private static OptionItem FixesComms;
    private static OptionItem FixesElectrical;
    public static OptionItem SMAbilityUseGainWithEachTaskCompleted;
    private static OptionItem UsesUsedWhenFixingReactorOrO2;
    private static OptionItem UsesUsedWhenFixingLightsOrComms;

    private static bool DoorsProgressing = false;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Mechanic);
        SkillLimit = IntegerOptionItem.Create(Id + 10, "MechanicSkillLimit", new(0, 100, 1), 10, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mechanic])
            .SetValueFormat(OptionFormat.Times);
        FixesDoors = BooleanOptionItem.Create(Id + 11, "MechanicFixesDoors", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mechanic]);
        FixesReactors = BooleanOptionItem.Create(Id + 12, "MechanicFixesReactors", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mechanic]);
        FixesOxygens = BooleanOptionItem.Create(Id + 13, "MechanicFixesOxygens", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mechanic]);
        FixesComms = BooleanOptionItem.Create(Id + 14, "MechanicFixesCommunications", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mechanic]);
        FixesElectrical = BooleanOptionItem.Create(Id + 15, "MechanicFixesElectrical", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mechanic]);
        SMAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 16, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mechanic])
            .SetValueFormat(OptionFormat.Times);
        UsesUsedWhenFixingReactorOrO2 = FloatOptionItem.Create(Id + 17, "SMUsesUsedWhenFixingReactorOrO2", new(0f, 5f, 0.1f), 4f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mechanic])
            .SetValueFormat(OptionFormat.Times);
        UsesUsedWhenFixingLightsOrComms = FloatOptionItem.Create(Id + 18, "SMUsesUsedWhenFixingLightsOrComms", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mechanic])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        playerIdList = [];
        UsedSkillCount = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        UsedSkillCount.Add(playerId, 0);
        On = true;
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        UsedSkillCount.Remove(playerId);
    }
    public static void UpdateSystem(ShipStatus __instance, SystemTypes systemType, byte amount, byte playerId)
    {
        switch (systemType)
        {
            case SystemTypes.Reactor:
                if (!FixesReactors.GetBool()) break;
                if (SkillLimit.GetFloat() > 0 && UsedSkillCount[playerId] + UsesUsedWhenFixingReactorOrO2.GetFloat() - 1 >= SkillLimit.GetFloat()) break;
                if (amount is 64 or 65)
                {
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Reactor, 16);
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Reactor, 17);
                    UsedSkillCount[playerId] += UsesUsedWhenFixingReactorOrO2.GetFloat();
                    SendRPC(playerId);
                }
                break;
            case SystemTypes.Laboratory:
                if (!FixesReactors.GetBool()) break;
                if (SkillLimit.GetFloat() > 0 && UsedSkillCount[playerId] + UsesUsedWhenFixingReactorOrO2.GetFloat() - 1 >= SkillLimit.GetFloat()) break;
                if (amount is 64 or 65)
                {
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Laboratory, 67);
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Laboratory, 66);
                    UsedSkillCount[playerId] += UsesUsedWhenFixingReactorOrO2.GetFloat();
                    SendRPC(playerId);
                }
                break;
            case SystemTypes.LifeSupp:
                if (!FixesOxygens.GetBool()) break;
                if (SkillLimit.GetFloat() > 0 && UsedSkillCount[playerId] + UsesUsedWhenFixingReactorOrO2.GetFloat() - 1 >= SkillLimit.GetFloat()) break;
                if (amount is 64 or 65)
                {
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.LifeSupp, 67);
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.LifeSupp, 66);
                    UsedSkillCount[playerId] += UsesUsedWhenFixingReactorOrO2.GetFloat();
                    SendRPC(playerId);
                }
                break;
            case SystemTypes.Comms:
                if (!FixesComms.GetBool()) break;
                if (SkillLimit.GetFloat() > 0 && UsedSkillCount[playerId] + UsesUsedWhenFixingLightsOrComms.GetFloat() - 1 >= SkillLimit.GetFloat()) break;
                if (amount is 64 or 65)
                {
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, 16);
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, 17);
                    UsedSkillCount[playerId] += UsesUsedWhenFixingLightsOrComms.GetFloat();
                    SendRPC(playerId);
                }
                break;
            case SystemTypes.Doors:
                if (!FixesDoors.GetBool()) break;
                if (DoorsProgressing == true) break;

                int mapId = Utils.GetActiveMapId();
                if (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay) mapId = AmongUsClient.Instance.TutorialMapId;

                DoorsProgressing = true;
                if (mapId == 2)
                {
                    //Polus
                    RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 71, 72);
                    RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 67, 68);
                    RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 64, 66);
                    RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 73, 74);
                }
                else if (mapId == 4)
                {
                    //Airship
                    RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 64, 67);
                    RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 71, 73);
                    RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 74, 75);
                    RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 76, 78);
                    RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 68, 70);
                    RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 83, 84);
                }
                DoorsProgressing = false;
                break;
        }
        Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(playerId));
    }
    public static void SwitchSystemRepair(SwitchSystem __instance, byte amount, byte playerId)
    {
        if (!FixesElectrical.GetBool()) return;
        if (SkillLimit.GetFloat() > 0 &&
            UsedSkillCount[playerId] + UsesUsedWhenFixingLightsOrComms.GetFloat() - 1 >= SkillLimit.GetFloat())
            return;

        if (amount is >= 0 and <= 4)
        {
            __instance.ActualSwitches = 0;
            __instance.ExpectedSwitches = 0;
            UsedSkillCount[playerId] += UsesUsedWhenFixingLightsOrComms.GetFloat();
            SendRPC(playerId);
        }
    }

    public static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Mechanic);
        writer.Write(playerId);
        writer.Write(UsedSkillCount[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        float count = reader.ReadSingle();

        if (!UsedSkillCount.ContainsKey(playerId))
        {
            UsedSkillCount.Add(playerId, count);
        }
        else UsedSkillCount[playerId] = count;
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var taskState10 = Main.PlayerStates?[playerId].TaskState;
        Color TextColor10;
        var TaskCompleteColor10 = Color.green;
        var NonCompleteColor10 = Color.yellow;
        var NormalColor10 = taskState10.IsTaskFinished ? TaskCompleteColor10 : NonCompleteColor10;
        TextColor10 = comms ? Color.gray : NormalColor10;
        string Completed10 = comms ? "?" : $"{taskState10.CompletedTasksCount}";
        Color TextColor101;
        if (Mechanic.SkillLimit.GetFloat() - Mechanic.UsedSkillCount[playerId] < 1) TextColor101 = Color.red;
        else TextColor101 = Color.white;
        ProgressText.Append(ColorString(TextColor10, $"({Completed10}/{taskState10.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor101, $" <color=#ffffff>-</color> {Math.Round(Mechanic.SkillLimit.GetFloat() - Mechanic.UsedSkillCount[playerId], 1)}"));
        return ProgressText.ToString();
    }
    public override void OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (!player.IsAlive()) return;
        Mechanic.UsedSkillCount[player.PlayerId] -= Mechanic.SMAbilityUseGainWithEachTaskCompleted.GetFloat();
        Mechanic.SendRPC(player.PlayerId);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = 0f;
        AURoleOptions.EngineerInVentMaxTime = 0f;
    }
}