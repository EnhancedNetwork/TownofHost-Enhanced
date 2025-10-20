using AmongUs.GameOptions;
using TOHE.Roles.Core;
using TOHE.Modules;
using static TOHE.Utils;
using static TOHE.Options;

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
    private static OptionItem UsesUsedWhenFixingReactorOrO2;
    private static OptionItem UsesUsedWhenFixingLightsOrComms;

    private bool DoorsProgressing = false;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Mechanic);
        SkillLimit = IntegerOptionItem.Create(Id + 10, "MechanicSkillLimit", new(0, 100, 1), 10, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mechanic])
            .SetValueFormat(OptionFormat.Times);
        FixesDoors = BooleanOptionItem.Create(Id + 11, "MechanicFixesDoors", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mechanic]);
        FixesReactors = BooleanOptionItem.Create(Id + 12, "MechanicFixesReactors", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mechanic]);
        FixesOxygens = BooleanOptionItem.Create(Id + 13, "MechanicFixesOxygens", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mechanic]);
        FixesComms = BooleanOptionItem.Create(Id + 14, "MechanicFixesCommunications", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mechanic]);
        FixesElectrical = BooleanOptionItem.Create(Id + 15, "MechanicFixesElectrical", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mechanic]);
        MechanicAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 16, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mechanic])
            .SetValueFormat(OptionFormat.Times);
        UsesUsedWhenFixingReactorOrO2 = FloatOptionItem.Create(Id + 17, "SMUsesUsedWhenFixingReactorOrO2", new(0f, 5f, 0.1f), 4f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mechanic])
            .SetValueFormat(OptionFormat.Times);
        UsesUsedWhenFixingLightsOrComms = FloatOptionItem.Create(Id + 18, "SMUsesUsedWhenFixingLightsOrComms", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mechanic])
            .SetValueFormat(OptionFormat.Times);
        OverrideTasksData.Create(Id + 19, TabGroup.CrewmateRoles, CustomRoles.Mechanic);
    }
    public override void Add(byte playerId)
    {
        DoorsProgressing = false;
        playerId.SetAbilityUseLimit(SkillLimit.GetInt());
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = 1f;
        AURoleOptions.EngineerInVentMaxTime = 0f;
    }
    public override void UpdateSystem(ShipStatus __instance, SystemTypes systemType, byte amount, PlayerControl player)
    {
        var abilityUse = player.GetAbilityUseLimit();
        switch (systemType)
        {
            case SystemTypes.Laboratory:
            case SystemTypes.Reactor:
                if (!FixesReactors.GetBool()) break;
                if (abilityUse <= 0 || abilityUse - UsesUsedWhenFixingReactorOrO2.GetFloat() <= 0) break;
                if (amount is 64 or 65)
                {
                    if (systemType is SystemTypes.Reactor)
                    {
                        __instance.RpcUpdateSystem(SystemTypes.Reactor, 16);
                        __instance.RpcUpdateSystem(SystemTypes.Reactor, 17);
                    }
                    else
                    {
                        __instance.RpcUpdateSystem(SystemTypes.Laboratory, 67);
                        __instance.RpcUpdateSystem(SystemTypes.Laboratory, 66);
                    }
                    player.SetAbilityUseLimit(abilityUse - UsesUsedWhenFixingReactorOrO2.GetFloat());
                }
                break;
            case SystemTypes.LifeSupp:
                if (!FixesOxygens.GetBool()) break;
                if (abilityUse <= 0 || abilityUse - UsesUsedWhenFixingReactorOrO2.GetFloat() <= 0) break;
                if (amount is 64 or 65)
                {
                    __instance.RpcUpdateSystem(SystemTypes.LifeSupp, 67);
                    __instance.RpcUpdateSystem(SystemTypes.LifeSupp, 66);
                    player.SetAbilityUseLimit(abilityUse - UsesUsedWhenFixingReactorOrO2.GetFloat());
                }
                break;
            case SystemTypes.Comms:
                if (!FixesComms.GetBool()) break;
                if (abilityUse <= 0 || abilityUse - UsesUsedWhenFixingReactorOrO2.GetFloat() <= 0) break;
                if (amount is 64 or 65)
                {
                    __instance.RpcUpdateSystem(SystemTypes.Comms, 16);
                    __instance.RpcUpdateSystem(SystemTypes.Comms, 17);
                    player.SetAbilityUseLimit(abilityUse - UsesUsedWhenFixingLightsOrComms.GetFloat());
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

        var playerId = player.PlayerId;

        var abilityUse = playerId.GetAbilityUseLimit();
        if (abilityUse <= 0 ||
            abilityUse - UsesUsedWhenFixingLightsOrComms.GetFloat() <= 0)
            return;

        __instance.ActualSwitches = 0;
        __instance.ExpectedSwitches = 0;

        playerId.SetAbilityUseLimit(abilityUse - UsesUsedWhenFixingLightsOrComms.GetFloat());

        Logger.Info($"{player.GetNameWithRole().RemoveHtmlTags()} instant - fix-lights", "SwitchSystem");
    }
}
