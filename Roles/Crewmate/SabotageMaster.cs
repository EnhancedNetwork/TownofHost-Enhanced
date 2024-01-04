using Hazel;
using System.Collections.Generic;

namespace TOHE.Roles.Crewmate;

public static class SabotageMaster
{
    private static readonly int Id = 8500;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static OptionItem SkillLimit;
    private static OptionItem FixesDoors;
    private static OptionItem FixesReactors;
    private static OptionItem FixesOxygens;
    private static OptionItem FixesComms;
    private static OptionItem FixesElectrical;
    public static OptionItem SMAbilityUseGainWithEachTaskCompleted;
    private static OptionItem UsesUsedWhenFixingReactorOrO2;
    private static OptionItem UsesUsedWhenFixingLightsOrComms;
    public static Dictionary<byte, float> UsedSkillCount = new();

    private static bool DoorsProgressing = false;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.SabotageMaster);
        SkillLimit = IntegerOptionItem.Create(Id + 10, "SabotageMasterSkillLimit", new(0, 100, 1), 10, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster])
            .SetValueFormat(OptionFormat.Times);
        FixesDoors = BooleanOptionItem.Create(Id + 11, "SabotageMasterFixesDoors", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
        FixesReactors = BooleanOptionItem.Create(Id + 12, "SabotageMasterFixesReactors", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
        FixesOxygens = BooleanOptionItem.Create(Id + 13, "SabotageMasterFixesOxygens", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
        FixesComms = BooleanOptionItem.Create(Id + 14, "SabotageMasterFixesCommunications", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
        FixesElectrical = BooleanOptionItem.Create(Id + 15, "SabotageMasterFixesElectrical", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
        SMAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 16, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster])
            .SetValueFormat(OptionFormat.Times);
        UsesUsedWhenFixingReactorOrO2 = FloatOptionItem.Create(Id + 17, "SMUsesUsedWhenFixingReactorOrO2", new(0f, 5f, 0.1f), 4f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster])
            .SetValueFormat(OptionFormat.Times);
        UsesUsedWhenFixingLightsOrComms = FloatOptionItem.Create(Id + 18, "SMUsesUsedWhenFixingLightsOrComms", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster])
            .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = new();
        UsedSkillCount = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        UsedSkillCount.Add(playerId, 0);
        IsEnable = true;
    }
    public static void RepairSystem(ShipStatus __instance, SystemTypes systemType, byte amount, byte playerId)
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

                int mapId = Main.NormalOptions.MapId;
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
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncSabotageMasterSkill, SendOption.Reliable, -1);
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
}