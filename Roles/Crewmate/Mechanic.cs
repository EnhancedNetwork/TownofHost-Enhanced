using AmongUs.GameOptions;
using System;
using System.Text;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Mechanic : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Mechanic;
    private const int Id = 8500;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Mechanic);
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem SkillLimit;
    private static OptionItem FixesDoors;
    private static OptionItem FixesReactors;
    private static OptionItem FixesOxygens;
    private static OptionItem FixesComms;
    private static OptionItem FixesElectrical;
    private static OptionItem SMAbilityUseGainWithEachTaskCompleted;
    private static OptionItem UsesUsedWhenFixingReactorOrO2;
    private static OptionItem UsesUsedWhenFixingLightsOrComms;

    private bool DoorsProgressing = false;

    public override void SetupCustomOption()
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
    public override void Add(byte playerId)
    {
        AbilityLimit = SkillLimit.GetInt();
    }
    public override void UpdateSystem(ShipStatus __instance, SystemTypes systemType, byte amount, PlayerControl player)
    {
        switch (systemType)
        {
            case SystemTypes.Reactor:
                if (!FixesReactors.GetBool()) break;
                if (AbilityLimit <= 0 || AbilityLimit - UsesUsedWhenFixingReactorOrO2.GetFloat() <= 0) break;
                if (amount is 64 or 65)
                {
                    __instance.RpcUpdateSystem(SystemTypes.Reactor, 16);
                    __instance.RpcUpdateSystem(SystemTypes.Reactor, 17);
                    AbilityLimit -= UsesUsedWhenFixingReactorOrO2.GetFloat();
                    SendSkillRPC();
                }
                break;
            case SystemTypes.Laboratory:
                if (!FixesReactors.GetBool()) break;
                if (AbilityLimit <= 0 || AbilityLimit - UsesUsedWhenFixingReactorOrO2.GetFloat() <= 0) break;
                if (amount is 64 or 65)
                {
                    __instance.RpcUpdateSystem(SystemTypes.Laboratory, 67);
                    __instance.RpcUpdateSystem(SystemTypes.Laboratory, 66);
                    AbilityLimit -= UsesUsedWhenFixingReactorOrO2.GetFloat();
                    SendSkillRPC();
                }
                break;
            case SystemTypes.LifeSupp:
                if (!FixesOxygens.GetBool()) break;
                if (AbilityLimit <= 0 || AbilityLimit - UsesUsedWhenFixingReactorOrO2.GetFloat() <= 0) break;
                if (amount is 64 or 65)
                {
                    __instance.RpcUpdateSystem(SystemTypes.LifeSupp, 67);
                    __instance.RpcUpdateSystem(SystemTypes.LifeSupp, 66);
                    AbilityLimit -= UsesUsedWhenFixingReactorOrO2.GetFloat();
                    SendSkillRPC();
                }
                break;
            case SystemTypes.Comms:
                if (!FixesComms.GetBool()) break;
                if (AbilityLimit <= 0 || AbilityLimit - UsesUsedWhenFixingLightsOrComms.GetFloat() <= 0) break;
                if (amount is 64 or 65)
                {
                    __instance.RpcUpdateSystem(SystemTypes.Comms, 16);
                    __instance.RpcUpdateSystem(SystemTypes.Comms, 17);
                    AbilityLimit -= UsesUsedWhenFixingLightsOrComms.GetFloat();
                    SendSkillRPC();
                }
                break;
            case SystemTypes.Doors:
                if (!FixesDoors.GetBool()) break;
                if (DoorsProgressing == true) break;

                int mapId = GetActiveMapId();
                if (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay) mapId = AmongUsClient.Instance.TutorialMapId;

                DoorsProgressing = true;
                if (mapId == 2)
                {
                    //Polus
                    UpdateSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 71, 72);
                    UpdateSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 67, 68);
                    UpdateSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 64, 66);
                    UpdateSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 73, 74);
                }
                else if (mapId == 4)
                {
                    //Airship
                    UpdateSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 64, 67);
                    UpdateSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 71, 73);
                    UpdateSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 74, 75);
                    UpdateSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 76, 78);
                    UpdateSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 68, 70);
                    UpdateSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 83, 84);
                }
                else if (mapId == 5)
                {
                    // Fungle
                    UpdateSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 64, 71);
                }
                
                DoorsProgressing = false;
                break;
        }
        NotifyRoles(SpecifySeer: player);
    }
    public override void SwitchSystemUpdate(SwitchSystem __instance, byte amount, PlayerControl player)
    {
        if (!FixesElectrical.GetBool()) return;

        //var playerId = player.PlayerId;

        if (AbilityLimit <= 0 ||
            AbilityLimit - UsesUsedWhenFixingLightsOrComms.GetFloat() <= 0)
            return;

        __instance.ActualSwitches = 0;
        __instance.ExpectedSwitches = 0;

        AbilityLimit -= UsesUsedWhenFixingLightsOrComms.GetFloat();
        SendSkillRPC();

        Logger.Info($"{player.GetNameWithRole().RemoveHtmlTags()} instant - fix-lights", "SwitchSystem");
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
        if (AbilityLimit <= 1) TextColor101 = Color.red;
        else TextColor101 = Color.white;
        ProgressText.Append(ColorString(TextColor10, $"({Completed10}/{taskState10.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor101, $" <color=#ffffff>-</color> {Math.Round(AbilityLimit, 1)}"));
        return ProgressText.ToString();
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.IsAlive())
        {
            AbilityLimit += SMAbilityUseGainWithEachTaskCompleted.GetFloat();
            SendSkillRPC();
        }
        return true;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = 1f;
        AURoleOptions.EngineerInVentMaxTime = 0f;
    }
}
